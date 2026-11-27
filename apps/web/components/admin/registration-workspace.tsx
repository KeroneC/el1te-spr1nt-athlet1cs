"use client";

import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { AdminApiError } from "@/lib/admin/api-error";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import { validSupportReference } from "@/lib/observability/support-reference";
import { registrationAgeLabel } from "@/lib/registration";
import { registration2027Copy } from "@/lib/registration-2027-copy.mjs";
import { Field, FormNotice, FormSection, SelectField, TextArea, fieldError } from "./form-controls";
import { applicationReviewName, RegistrationReviewDetails, type RegistrationReviewDetailsRecord } from "./registration-review-details";

const statusLabels = {
  New: "New", FollowUp: "Follow-up", Waitlisted: "Waitlisted", Offered: "Offered · paperwork pending",
  PaperworkReceived: "Paperwork received", Enrolled: "Enrolled", Closed: "Closed"
} as const;
const transferLabels = { NeedsReview: "Needs transfer review", NotNeeded: "No transfer needed", Cleared: "Transfer cleared" } as const;
const consentLabels = { NotRecorded: "Not recorded", Granted: "Granted", Declined: "Declined" } as const;
type Status = keyof typeof statusLabels;
type Transfer = keyof typeof transferLabels;
type Consent = keyof typeof consentLabels;
type Kind = "Application" | "Returning";
type View = Kind | "Settings";

const transitions: Record<Status, readonly Status[]> = {
  New: ["FollowUp", "Waitlisted", "Offered", "Closed"], FollowUp: ["Waitlisted", "Offered", "Closed"],
  Waitlisted: ["FollowUp", "Offered", "Closed"], Offered: ["PaperworkReceived", "Waitlisted", "Closed"],
  PaperworkReceived: ["Enrolled", "Offered", "Closed"], Enrolled: ["PaperworkReceived", "Closed"], Closed: ["FollowUp"]
};
type Season = {
  year: number; revision: string; title: string; openingDate: string; endDate: string; firstPracticeDate: string | null;
  practiceLocation: string; practiceSchedule: string; fees: string; replyWindow: string; paperworkEmail: string;
  paperworkHandoff: string; reviewOwner: string; retentionOwner: string; isOpen: boolean; purgedAtUtc: string | null;
  availability: { publicState: "Open" | "Closed" | "Upcoming" | "Ended"; openingBlockedReason: string | null };
};
type Count = { seasonYear: number; channel: "Direct" | "Facebook" | "Instagram"; visits: number; applications: number; returning: number; enrolled: number };
export type RegistrationOverview = { seasons: Season[]; counts: Count[]; closeoutEligibleYears: number[] };
type Row = { id: string; receiptReference: string; athleteFirstName: string; status: Status; transfer: Transfer; competitionReady: boolean; submittedAtUtc: string };
type Review = { status: Status; transfer: Transfer; photoConsent: Consent; competitionReady: boolean };
type Detail = Row & Review & RegistrationReviewDetailsRecord & {
  seasonYear: number; revision: string;
  history: (Review & { changedAtUtc: string; reviewer: string | null; changedByUserId: string })[];
};
type Queue = { items: Row[]; page: number; totalPages: number; count: number };
type QueueState = { state: "loading" } | { state: "ready"; data: Queue } | { state: "error"; notice: Notice };
type FilterValues = { search: string; status: string; transfer: string };
type Filters = FilterValues & { page: number };
type Area = "settings" | "opening" | "closeout" | "review" | "queue";
type Notice = { message: string; success?: boolean; referenceId?: string | null; errors?: Record<string, string[]> };

const emptyFilterValues: FilterValues = { search: "", status: "", transfer: "" };
const emptyFilters: Filters = { ...emptyFilterValues, page: 1 };
const baseButton = "inline-flex min-h-11 items-center justify-center px-4 py-2 text-sm font-bold outline-none focus-visible:ring-2 focus-visible:ring-track-red focus-visible:ring-offset-2 disabled:opacity-60";
const primary = `${baseButton} bg-track-red text-white enabled:hover:bg-red-800`;
const secondary = `${baseButton} border border-slate-300 bg-white text-track-ink enabled:hover:border-track-red`;
const destructive = `${baseButton} border border-red-700 bg-white text-red-800 enabled:hover:bg-red-50`;
const panel = "min-w-0 space-y-4 border border-slate-200 bg-white p-4 sm:p-5";
const date = (value: string) => new Date(value).toLocaleString();
const hasFilters = ({ search, status, transfer }: FilterValues) => Boolean(search.trim() || status || transfer);
const statusLabel = (status: Status, kind: Kind) => status === "Offered" && kind === "Returning" ? "Offered" : statusLabels[status];
const statusOptions = (statuses: readonly Status[], kind: Kind) => statuses.map(status => [status, statusLabel(status, kind)] as const);
const nextStatuses = (status: Status, kind: Kind): readonly Status[] =>
  kind === "Application" ? transitions[status] :
    status === "Offered" ? ["Enrolled", "Waitlisted", "Closed"] : transitions[status].filter(value => value !== "PaperworkReceived");
const eligible = (review: Review) => review.status === "Enrolled" && review.transfer !== "NeedsReview";
const noticeFrom = (error: unknown, fallback: string): Notice => error instanceof AdminApiError
  ? { message: error.message, referenceId: error.referenceId, errors: error.fieldErrors }
  : { message: fallback };

async function call<T>(path: string, method: "GET" | "POST" | "PUT", body?: unknown, signal?: AbortSignal): Promise<T> {
  const timeout = AbortSignal.timeout(15_000);
  const response = await fetch(`/api/admin/registration/${path}`, {
    method, cache: "no-store", signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
    ...(body !== undefined && { body: JSON.stringify(body), headers: { "Content-Type": "application/json" } })
  });
  const result = await response.json().catch(() => null);
  if (redirectForAdminResponse(response)) throw new AdminApiError(response.status, "Sign in again to continue.");
  if (!response.ok) throw new AdminApiError(response.status, result?.message ?? "The request could not be completed.",
    result?.errors, validSupportReference(result?.referenceId));
  return result as T;
}

function Feedback({ notice }: { notice?: Notice }) {
  return <FormNotice message={notice?.message ?? null} success={notice?.success} referenceId={notice?.referenceId} />;
}

export function RegistrationWorkspace({ initial, canCloseout }: { initial: RegistrationOverview; canCloseout: boolean }) {
  const [overview, setOverview] = useState(initial);
  const [year, setYear] = useState(initial.seasons[0]?.year ?? 0);
  const [view, setView] = useState<View>(initial.seasons.length ? "Application" : "Settings");
  const [draftFilters, setDraftFilters] = useState<FilterValues>(emptyFilterValues);
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [queue, setQueue] = useState<QueueState>({ state: "loading" });
  const [detail, setDetail] = useState<Detail | null>(null);
  const [draft, setDraft] = useState<Review | null>(null);
  const [feedback, setFeedback] = useState<Partial<Record<Area, Notice>>>({});
  const [conflict, setConflict] = useState<"record" | "season" | null>(null);
  const [approvedRevision, setApprovedRevision] = useState<string | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [refresh, setRefresh] = useState(0);
  const locked = useRef(false);
  const heading = useRef<HTMLHeadingElement>(null);
  const queueHeading = useRef<HTMLHeadingElement>(null);
  const focusRow = useRef<string | null>(null);
  const rowButtons = useRef(new Map<string, HTMLButtonElement>());
  const seasonForm = useRef<HTMLFormElement>(null);
  const reviewForm = useRef<HTMLFormElement>(null);
  const current = overview.seasons.find(season => season.year === year);
  const currentYear = current?.year;
  const nextYear = Math.max(2027, (overview.seasons[0]?.year ?? 2026) + 1);
  const initialLogistics = !current && nextYear === 2027 ? registration2027Copy : current;
  const otherOpenSeason = overview.seasons.find(season => season.isOpen && season.year !== year);
  const settingsLockedReason = current?.isOpen ? "Close the intake switch before editing this season." :
    current?.purgedAtUtc ? "Season details are read-only after closeout." :
      !current && otherOpenSeason ? "Close the current intake before adding a season." :
        conflict === "season" ? "Reload the season before making another change." : null;
  const openingReason = current?.availability?.openingBlockedReason ??
    (current && !current.firstPracticeDate ? "Set the first practice date before opening intake." : null);
  const publicState = current?.availability?.publicState ?? (current?.isOpen && !openingReason ? "Open" : "Closed");
  const launchApproved = !!current && approvedRevision === current.revision;

  useEffect(() => {
    if (view === "Settings" || !currentYear) return;
    const controller = new AbortController();
    void call<Queue>("queue", "POST", {
      ...filters, year: currentYear, kind: view, status: filters.status || null, transfer: filters.transfer || null
    }, controller.signal).then(data => {
      if (!controller.signal.aborted) setQueue({ state: "ready", data });
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setQueue({ state: "error", notice: noticeFrom(error, "Couldn’t load this queue. Try again.") });
    });
    return () => controller.abort();
  }, [currentYear, view, filters, refresh]);

  const detailId = detail?.id;
  useEffect(() => { if (detailId) heading.current?.focus(); }, [detailId]);
  useEffect(() => {
    if (detailId || !focusRow.current || queue.state === "loading" || pending) return;
    (rowButtons.current.get(focusRow.current) ?? queueHeading.current)?.focus();
    focusRow.current = null;
  }, [detailId, pending, queue]);
  useEffect(() => {
    if (pending) return;
    if (feedback.settings?.errors) seasonForm.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
    if (feedback.review?.errors) reviewForm.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
  }, [feedback.settings, feedback.review, pending]);

  const tell = (area: Area, notice?: Notice) => setFeedback(previous => ({ ...previous, [area]: notice }));
  async function action(area: Area, key: string, run: () => Promise<void>, fallback: string) {
    if (locked.current) return;
    locked.current = true;
    setPending(key);
    tell(area);
    try { await run(); }
    catch (error) {
      const uncertainWrite = !(error instanceof AdminApiError) || error.status >= 500;
      const recordChanged = error instanceof AdminApiError && (error.status === 409 || error.status === 404);
      if (uncertainWrite || recordChanged) {
        if (key === "review") setConflict("record");
        if (key === "settings" || key === "open-intake" || key === "close-intake" || key === "closeout") setConflict("season");
      }
      tell(area, noticeFrom(error, fallback));
    } finally { locked.current = false; setPending(null); }
  }

  function retryQueue() {
    setQueue({ state: "loading" });
    tell("queue");
    setRefresh(value => value + 1);
  }
  function backToQueue() {
    setDetail(null); setDraft(null); setConflict(null); tell("review");
  }
  function navigate(nextView: View, nextYear = year) {
    if (nextView === view && nextYear === year) {
      if (detail) backToQueue();
      return;
    }
    setView(nextView); setYear(nextYear); setDetail(null); setDraft(null); setConflict(null); setFeedback({});
    setFilters(emptyFilters); setDraftFilters(emptyFilterValues); setQueue({ state: "loading" }); focusRow.current = null;
  }
  function showDetail(item: Detail) {
    setDetail(item);
    setDraft({ status: item.status, transfer: item.transfer, photoConsent: item.photoConsent, competitionReady: item.competitionReady });
  }
  function changeDraft(change: Partial<Review>) {
    setDraft(previous => {
      if (!previous) return previous;
      const next = { ...previous, ...change };
      return eligible(next) ? next : { ...next, competitionReady: false };
    });
  }
  async function openDetail(id: string) {
    await action("queue", id, async () => {
      const item = await call<Detail>("detail", "POST", { id });
      focusRow.current = id;
      setConflict(null);
      tell("review");
      showDetail(item);
    }, "Couldn’t load this record. Try opening it again.");
  }
  async function saveReview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail || !draft || conflict === "record") return;
    await action("review", "review", async () => {
      await call("review", "PUT", { ...draft, id: detail.id, revision: detail.revision, competitionReady: eligible(draft) && draft.competitionReady });
      retryQueue();
      try {
        showDetail(await call<Detail>("detail", "POST", { id: detail.id }));
        tell("review", { message: "Review saved. No email was sent.", success: true });
      } catch (error) {
        setConflict("record");
        const notice = noticeFrom(error, "Couldn’t load the updated record.");
        tell("review", { message: "Review saved. No email was sent. Reload the record before editing again.", referenceId: notice.referenceId });
      }
    }, "Couldn’t confirm the review was saved. Reload the record before retrying.");
  }
  function updateSeason(saved: Season) {
    setOverview(previous => ({
      ...previous,
      seasons: [saved, ...previous.seasons.filter(season => season.year !== saved.year)].sort((a, b) => b.year - a.year),
      closeoutEligibleYears: saved.isOpen ? previous.closeoutEligibleYears.filter(value => value !== saved.year) : previous.closeoutEligibleYears
    }));
  }
  async function saveSeason(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (settingsLockedReason) return;
    const body = Object.fromEntries(new FormData(event.currentTarget));
    await action("settings", "settings", async () => {
      const saved = await call<Season>("season", "PUT", { ...body, year: Number(body.year), revision: current?.revision ?? null });
      updateSeason(saved); setYear(saved.year);
      tell("settings", { message: "Season saved. Intake is still closed.", success: true });
    }, "Couldn’t confirm the season was saved. Reload the season before retrying.");
  }
  async function reloadSeason(area: Area) {
    await action(area, "reload-season", async () => {
      setOverview(await call<RegistrationOverview>("seasons", "GET"));
      setConflict(null);
      tell(area, { message: "Season status reloaded.", success: true });
    }, "Couldn’t reload the season. Try again.");
  }
  async function changeOpening(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!current || conflict === "season") return;
    const open = !current.isOpen;
    if (open && (openingReason || !launchApproved)) return;
    await action("opening", open ? "open-intake" : "close-intake", async () => {
      const saved = await call<Season>("opening", "PUT", { year, revision: current.revision, open, launchChecksApproved: launchApproved });
      updateSeason(saved);
      tell("opening", { message: open ? "Intake opened." : "Intake closed.", success: true });
      try { setOverview(await call<RegistrationOverview>("seasons", "GET")); }
      catch (error) {
        const notice = noticeFrom(error, "Couldn’t update the season summary.");
        tell("opening", { message: `Intake ${open ? "opened" : "closed"}. Refresh season status and totals to see the latest summary.`, referenceId: notice.referenceId });
      }
    }, "Couldn’t confirm the intake change. Reload the season before retrying.");
  }

  const seasonError = (name: string) => fieldError(feedback.settings?.errors ?? {}, name);
  const reviewError = (name: string) => fieldError(feedback.review?.errors ?? {}, name);

  return <div className="space-y-5">
    <div className="grid gap-3 sm:grid-cols-[minmax(0,18rem)_1fr] sm:items-end">
      <SelectField label="Season" name="registrationSeason" value={year} disabled={Boolean(pending)}
        options={[["0", "Add a season"], ...overview.seasons.map(item => [String(item.year), `${item.year} — ${item.title}`] as const)]}
        onChange={event => navigate(event.target.value === "0" ? "Settings" : view, Number(event.target.value))} />
      <div className="flex flex-wrap gap-2" role="group" aria-label="Registration workspace views">
        {([["Application", "New Applications"], ["Returning", "Returning Check-Ins"], ["Settings", "Season Settings"]] as const).map(([value, label]) =>
          <button type="button" key={value} className={view === value ? primary : secondary} aria-pressed={view === value}
            disabled={Boolean(pending) || (value !== "Settings" && !current)} onClick={() => navigate(value)}>{label}</button>)}
      </div>
    </div>

    {view === "Settings" ? <>
      {current && <section className={panel} aria-labelledby="registration-intake-status-title">
        <h2 id="registration-intake-status-title" className="text-lg font-black text-track-ink">Intake status</h2>
        <dl className="grid gap-3 sm:grid-cols-2">
          <div><dt className="text-sm font-bold text-slate-600">Public registration</dt><dd className="font-bold">{publicState}</dd></div>
          <div><dt className="text-sm font-bold text-slate-600">Manual intake switch</dt><dd className="font-bold">{current.isOpen ? "On" : "Off"}</dd></div>
        </dl>
        {!current.purgedAtUtc && <form method="post" className="space-y-3" onSubmit={changeOpening} key={current.revision}>
          {openingReason && (!current.isOpen || publicState !== "Open") && <p className="text-sm font-semibold text-slate-700" id="registration-opening-reason">{openingReason}</p>}
          {!current.isOpen && !openingReason && <label className="flex min-h-11 items-start gap-3 text-sm">
            <input type="checkbox" name="approved" className="mt-0.5 h-5 w-5 shrink-0 accent-track-red" required
              checked={launchApproved} onChange={event => setApprovedRevision(event.target.checked ? current.revision : null)}
              disabled={Boolean(pending) || conflict === "season"} />
            <span>The club approved season details, blank forms, the updated privacy notice, post-offer paperwork and handoff instructions, and review and retention owners. The public page and Admin queue have been checked.</span>
          </label>}
          <button type="submit" className={current.isOpen ? secondary : primary}
            disabled={Boolean(pending) || conflict === "season" || (!current.isOpen && (Boolean(openingReason) || !launchApproved))}
            aria-describedby={!current.isOpen && openingReason ? "registration-opening-reason" : undefined}>
            {pending === "open-intake" ? "Opening intake…" : pending === "close-intake" ? "Closing intake…" : current.isOpen ? "Close intake" : "Open intake"}
          </button>
        </form>}
        <Feedback notice={feedback.opening} />
      </section>}
      {conflict === "season" && <div className={panel}>
        <button type="button" className={secondary} disabled={Boolean(pending)} onClick={() => void reloadSeason("opening")}>
          {pending === "reload-season" ? "Reloading season…" : "Reload season"}
        </button>
        {!current && <Feedback notice={feedback.opening} />}
      </div>}
      <form method="post" className="space-y-3" onSubmit={saveSeason} key={current?.revision ?? "new"} ref={seasonForm}>
        <fieldset className="min-w-0 space-y-4" disabled={Boolean(pending) || Boolean(settingsLockedReason)}
          aria-describedby={settingsLockedReason ? "registration-settings-locked" : undefined}>
          <legend className="sr-only">Season settings</legend>
          {current ? <input type="hidden" name="year" value={current.year} /> : null}
          <FormSection title="Season details">
            {!current && <Field label="Season year" name="year" type="number" min={2026} max={2100} defaultValue={nextYear} required error={seasonError("year")} />}
            <Field label="Season title" name="title" maxLength={120} defaultValue={current?.title} required error={seasonError("title")} />
            <Field label="Opening date (Eastern)" name="openingDate" type="date" defaultValue={current?.openingDate ?? "2026-11-27"} required error={seasonError("openingDate")} />
            <Field label="First practice date (Eastern)" name="firstPracticeDate" type="date" defaultValue={current?.firstPracticeDate ?? ""}
              error={seasonError("firstPracticeDate")} hint={`Staff only. Required before opening. New athletes must be ${registrationAgeLabel.toLowerCase()} on this date.`} />
            <Field label="Season end date (Eastern)" name="endDate" type="date" defaultValue={current?.endDate} required error={seasonError("endDate")} />
            <Field label="Practice location" name="practiceLocation" maxLength={250} defaultValue={initialLogistics?.practiceLocation} required error={seasonError("practiceLocation")} />
            <TextArea label="Practice schedule (shown to families)" name="practiceSchedule" maxLength={500} defaultValue={initialLogistics?.practiceSchedule} required error={seasonError("practiceSchedule")} />
            <Field label="Tuition" name="fees" maxLength={250} defaultValue={initialLogistics?.fees} required error={seasonError("fees")} />
            <Field label="Expected reply window (shown to families)" name="replyWindow" maxLength={250} defaultValue={current?.replyWindow} required error={seasonError("replyWindow")} />
          </FormSection>
          <FormSection title="Staff handoff and ownership" description="These details are for staff, not the public registration page.">
            <Field label="Designated club paperwork inbox (staff only)" name="paperworkEmail" type="email" maxLength={254} defaultValue={current?.paperworkEmail} required error={seasonError("paperworkEmail")} />
            <TextArea label="In-person handoff instructions (staff only)" name="paperworkHandoff" maxLength={500} defaultValue={current?.paperworkHandoff} required error={seasonError("paperworkHandoff")} />
            <Field label="Review owner" name="reviewOwner" maxLength={120} defaultValue={current?.reviewOwner} required error={seasonError("reviewOwner")} />
            <Field label="Retention owner" name="retentionOwner" maxLength={120} defaultValue={current?.retentionOwner} required error={seasonError("retentionOwner")} />
          </FormSection>
          <button type="submit" className={primary}>{pending === "settings" ? "Saving season…" : "Save season details"}</button>
        </fieldset>
        {settingsLockedReason && <p id="registration-settings-locked" className="text-sm font-semibold text-slate-700">{settingsLockedReason}</p>}
        <Feedback notice={feedback.settings} />
      </form>
      {current && <section className={panel}>
        <h2 className="text-lg font-black text-track-ink">Season totals</h2><p className="text-sm text-slate-600">Aggregate page visits, not unique people. No ad-platform tracking.</p>
        <ul className="space-y-1 text-sm">{overview.counts.filter(count => count.seasonYear === year).map(count => <li key={count.channel}>
          {count.channel === "Direct" ? "Direct / other" : count.channel}: {count.visits} visits · {count.applications} applications · {count.returning} check-ins · {count.enrolled} enrolled
        </li>)}</ul>
        {current.purgedAtUtc && <p className="text-sm">Identifiable intake records removed on {date(current.purgedAtUtc)}.</p>}
        <button type="button" className={secondary} disabled={Boolean(pending)} onClick={() => void reloadSeason("opening")}>
          {pending === "reload-season" ? "Refreshing…" : "Refresh season status and totals"}
        </button>
      </section>}
      {current && !current.purgedAtUtc && canCloseout && overview.closeoutEligibleYears.includes(current.year) && <form method="post"
        className={`${panel} border-red-200`} key={`closeout-${current.revision}`} onSubmit={event => {
          event.preventDefault();
          const confirmYear = new FormData(event.currentTarget).get("confirmYear");
          void action("closeout", "closeout", async () => {
            await call("closeout", "POST", { year, revision: current.revision, confirmYear });
            setOverview(previous => ({ ...previous, closeoutEligibleYears: previous.closeoutEligibleYears.filter(value => value !== year) }));
            tell("closeout", { message: "Identifiable submissions and their review history were removed. Aggregate counts were retained.", success: true });
            try { setOverview(await call<RegistrationOverview>("seasons", "GET")); }
            catch (error) {
              const notice = noticeFrom(error, "Couldn’t update the season summary.");
              tell("closeout", { message: "Closeout completed. Refresh season status and totals to see the updated summary.", referenceId: notice.referenceId });
            }
          }, "Couldn’t confirm closeout. Refresh the season before retrying.");
        }}>
        <h2 className="text-lg font-black text-track-ink">Season closeout</h2>
        <p className="text-sm">After 90 days, permanently remove this season’s applications, check-ins and review history. This does not delete paperwork in the club inbox.</p>
        <Field label="Type the season year to confirm" name="confirmYear" required pattern={String(year)} inputMode="numeric" disabled={Boolean(pending)} />
        <button type="submit" className={destructive} disabled={Boolean(pending) || conflict === "season"}>
          {pending === "closeout" ? "Removing records…" : "Remove identifiable intake records"}
        </button>
      </form>}
      <Feedback notice={feedback.closeout} />
    </> : detail && draft ? <section className={panel}>
      <button type="button" className={secondary} disabled={Boolean(pending)} onClick={backToQueue}>Back to queue</button>
      <h2 ref={heading} tabIndex={-1} className="scroll-mt-20 break-words text-xl font-black text-track-ink">{applicationReviewName(detail)} · {detail.receiptReference}</h2>
      <RegistrationReviewDetails detail={detail} />
      <form method="post" ref={reviewForm} onSubmit={saveReview}>
        <fieldset className="grid min-w-0 gap-4 sm:grid-cols-2" disabled={Boolean(pending) || conflict === "record"}>
          <legend className="sr-only">Review update</legend>
          <SelectField label={detail.kind === "Returning" ? "Check-in status" : "Application status"} name="status"
            value={draft.status} options={statusOptions([detail.status, ...nextStatuses(detail.status, detail.kind)], detail.kind)}
            onChange={event => changeDraft({ status: event.target.value as Status })} error={reviewError("status")} />
          {detail.kind === "Application" && <>
            <SelectField label="Transfer review" name="transfer" value={draft.transfer} options={Object.entries(transferLabels)}
              onChange={event => changeDraft({ transfer: event.target.value as Transfer })} error={reviewError("transfer")} />
            <SelectField label="Optional photo consent" name="photoConsent" value={draft.photoConsent} options={Object.entries(consentLabels)}
              onChange={event => changeDraft({ photoConsent: event.target.value as Consent })} error={reviewError("photoConsent")} />
          </>}
          <div>
            <label className="flex min-h-11 items-center gap-3 font-bold"><input type="checkbox" name="competitionReady"
              className="h-5 w-5 shrink-0 accent-track-red" checked={draft.competitionReady && eligible(draft)}
              disabled={!eligible(draft)} aria-describedby="competition-ready-help" aria-invalid={Boolean(reviewError("competitionReady"))}
              onChange={event => changeDraft({ competitionReady: event.target.checked })} />Competition-ready</label>
            <p id="competition-ready-help" className={`text-sm ${reviewError("competitionReady") ? "font-semibold text-red-700" : "text-slate-600"}`}>
              {reviewError("competitionReady") ?? (!eligible(draft) ? "Enrollment and resolved transfer review are required before marking competition-ready." : "Mark only when all applicable requirements are confirmed.")}</p>
          </div>
          <div className="sm:col-span-2"><button type="submit" className={primary}>{pending === "review" ? "Saving review…" : "Save review"}</button></div>
        </fieldset>
      </form>
      <Feedback notice={feedback.review} />
      {conflict === "record" && <button type="button" className={secondary} disabled={Boolean(pending)}
        onClick={() => void action("review", "reload-record", async () => {
          showDetail(await call<Detail>("detail", "POST", { id: detail.id }));
          setConflict(null); retryQueue();
          tell("review", { message: "Latest record loaded. Review the current values before saving.", success: true });
        }, "Couldn’t reload the record. Try again.")}>{pending === "reload-record" ? "Reloading record…" : "Reload record"}</button>}
      {detail.kind === "Application" && <p className="text-sm">Verify eligibility outside the app: <a className="underline" href="https://image.aausports.org/dnn/athletics/Club-Release-Form.pdf">AAU club release</a> · <a className="underline" href="https://www.usatf.org/home-normal/top-utility-nav-content/membership/affiliation-update-requests">USATF affiliation updates</a>.</p>}
      {detail.kind === "Application" && current && ["Offered", "PaperworkReceived", "Enrolled"].includes(detail.status) && <aside className="space-y-1 border border-slate-200 bg-slate-50 p-3 text-sm">
        <h3 className="font-bold">Paperwork instructions for staff to send</h3>
        <p className="break-words">Email PDFs or clear photos to {current.paperworkEmail} with {detail.receiptReference} in the subject, or hand them in person: {current.paperworkHandoff}</p>
        <p>Photo consent is optional and does not affect enrollment.</p>
      </aside>}
      <div><h3 className="font-bold">Review history</h3>{detail.history.length ? <ol className="space-y-2">{detail.history.map((entry, index) =>
        <li key={index} className="break-words text-sm"><time dateTime={entry.changedAtUtc}>{date(entry.changedAtUtc)}</time> · {entry.reviewer ?? entry.changedByUserId} · {statusLabel(entry.status, detail.kind)}{detail.kind === "Application" ? ` · ${transferLabels[entry.transfer]} · Photo consent: ${consentLabels[entry.photoConsent]}` : ""}{entry.competitionReady ? " · Competition-ready" : ""}</li>)}</ol> : <p className="text-sm">No staff changes yet.</p>}</div>
    </section> : <>
      <h2 ref={queueHeading} tabIndex={-1} className="scroll-mt-20 text-xl font-black text-track-ink">{view === "Application" ? "New Applications" : "Returning Check-Ins"}</h2>
      <form method="post" className="grid min-w-0 gap-4 border border-slate-200 bg-white p-4 sm:grid-cols-2 xl:grid-cols-3"
        onSubmit={event => { event.preventDefault(); setFilters({ ...draftFilters, page: 1 }); retryQueue(); }}>
        <Field label="Search name, email, or reference" name="search" maxLength={120} value={draftFilters.search}
          onChange={event => setDraftFilters(previous => ({ ...previous, search: event.target.value }))} />
        <SelectField label="Status" name="queueStatus" value={draftFilters.status}
          options={[["", "All statuses"], ...statusOptions((Object.keys(statusLabels) as Status[]).filter(status => view === "Application" || status !== "PaperworkReceived"), view)]}
          onChange={event => setDraftFilters(previous => ({ ...previous, status: event.target.value }))} />
        {view === "Application" &&
          <SelectField label="Transfer review" name="queueTransfer" value={draftFilters.transfer}
            options={[["", "All transfers"], ...Object.entries(transferLabels)]}
            onChange={event => setDraftFilters(previous => ({ ...previous, transfer: event.target.value }))} />}
        <div className="col-span-full flex flex-wrap gap-2">
          <button type="submit" className={primary} disabled={Boolean(pending) || queue.state === "loading"}>Apply filters</button>
          <button type="button" className={secondary} disabled={Boolean(pending) || (!hasFilters(draftFilters) && !hasFilters(filters))}
            onClick={() => { setDraftFilters(emptyFilterValues); setFilters(emptyFilters); retryQueue(); }}>Clear filters</button>
        </div>
      </form>
      <Feedback notice={feedback.queue} />
      {queue.state === "loading" && <p role="status" className="text-sm">Loading records…</p>}
      {queue.state === "error" && <div className="space-y-3">
        <Feedback notice={queue.notice} />
        <button type="button" className={secondary} onClick={retryQueue}>Retry</button>
      </div>}
      {queue.state === "ready" && <>
        <p role="status" className="text-sm">{queue.data.count === 0 ? hasFilters(filters) ? "No matching records. Clear filters to see all records." : "No submissions yet." :
          `${queue.data.count} ${queue.data.count === 1 ? "record" : "records"}${hasFilters(filters) ? " · filters applied" : ""}`}</p>
        <ul className="space-y-2">{queue.data.items.map(item => <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 border border-slate-200 bg-white p-4">
          <div className="min-w-0 break-words"><strong>{item.athleteFirstName}</strong><p className="text-sm">{item.receiptReference} · {statusLabel(item.status, view)}</p>
            {item.transfer === "NeedsReview" && <p className="text-sm font-semibold">Needs transfer review</p>}</div>
          <button type="button" className={secondary} ref={node => { if (node) rowButtons.current.set(item.id, node); else rowButtons.current.delete(item.id); }}
            disabled={Boolean(pending)} onClick={() => void openDetail(item.id)}>{pending === item.id ? "Opening review…" : `Review ${item.athleteFirstName}`}</button>
        </li>)}</ul>
        {queue.data.totalPages > 1 && <nav className="flex flex-wrap items-center gap-3" aria-label="Registration queue pages">
          <button type="button" className={secondary} disabled={Boolean(pending) || queue.data.page <= 1}
            onClick={() => { setFilters({ ...filters, page: queue.data.page - 1 }); retryQueue(); }}>Previous</button>
          <span className="text-sm">Page {queue.data.page} of {queue.data.totalPages}</span>
          <button type="button" className={secondary} disabled={Boolean(pending) || queue.data.page >= queue.data.totalPages}
            onClick={() => { setFilters({ ...filters, page: queue.data.page + 1 }); retryQueue(); }}>Next</button>
        </nav>}
      </>}
    </>}
  </div>;
}
