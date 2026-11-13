"use client";

import { useEffect, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import { FormNotice } from "./form-controls";

const labels = {
  New: "New", FollowUp: "Follow-up", Waitlisted: "Waitlisted", Offered: "Offered · paperwork pending",
  PaperworkReceived: "Paperwork received", Enrolled: "Enrolled", Closed: "Closed",
  NeedsReview: "Needs transfer review", NotNeeded: "No transfer needed", Cleared: "Transfer cleared",
  NotRecorded: "Not recorded", Granted: "Granted", Declined: "Declined"
} as const;
type Status = "New" | "FollowUp" | "Waitlisted" | "Offered" | "PaperworkReceived" | "Enrolled" | "Closed";
type Transfer = "NeedsReview" | "NotNeeded" | "Cleared";
type Consent = "NotRecorded" | "Granted" | "Declined";
type Kind = "Application" | "Returning";
const statusValues: Status[] = ["New", "FollowUp", "Waitlisted", "Offered", "PaperworkReceived", "Enrolled", "Closed"];
const transitions: Record<Status, Status[]> = {
  New: ["FollowUp", "Waitlisted", "Offered", "Closed"], FollowUp: ["Waitlisted", "Offered", "Closed"],
  Waitlisted: ["FollowUp", "Offered", "Closed"], Offered: ["PaperworkReceived", "Waitlisted", "Closed"],
  PaperworkReceived: ["Enrolled", "Offered", "Closed"], Enrolled: ["PaperworkReceived", "Closed"], Closed: ["FollowUp"]
};
type Season = {
  year: number; revision: string; title: string; openingDate: string; endDate: string; ageGroups: string;
  practiceLocation: string; practiceSchedule: string; fees: string; replyWindow: string; paperworkEmail: string;
  paperworkHandoff: string; reviewOwner: string; retentionOwner: string; isOpen: boolean; purgedAtUtc: string | null;
};
type Count = { seasonYear: number; channel: "Direct" | "Facebook" | "Instagram"; visits: number; applications: number; returning: number; enrolled: number };
export type RegistrationOverview = { seasons: Season[]; counts: Count[]; closeoutEligibleYears: number[] };
type Row = { id: string; receiptReference: string; athleteFirstName: string; ageGroup: string | null; status: Status; transfer: Transfer; competitionReady: boolean; submittedAtUtc: string };
type Review = { status: Status; transfer: Transfer; photoConsent: Consent; competitionReady: boolean };
type Detail = Row & Review & {
  kind: Kind; seasonYear: number; revision: string; guardianEmail: string; guardianName: string | null; guardianPhone: string | null;
  availability: string | null; experience: string | null; priorClub: string | null; clubName: string | null; affiliations: string | null;
  lastMeetMonth: string | null; history: (Review & { changedAtUtc: string; reviewer: string | null; changedByUserId: string })[];
};
type Queue = { items: Row[]; page: number; totalPages: number; count: number };
type Filters = { page: number; search: string; ageGroup: string; status: string; transfer: string };
const emptyFilters: Filters = { page: 1, search: "", ageGroup: "", status: "", transfer: "" };
const control = "min-h-11 w-full rounded-none border border-slate-300 bg-white px-3";
const button = "min-h-11 border border-slate-400 bg-white px-4 text-sm font-bold disabled:opacity-50";
const date = (value: string) => new Date(value).toLocaleString();
const field = (label: string, children: ReactNode) => <label className="grid min-w-0 gap-1 text-sm font-semibold">{label}{children}</label>;
const option = (value: keyof typeof labels) => <option key={value} value={value}>{labels[value]}</option>;

async function call<T>(path: string, method: "POST" | "PUT", body: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(`/api/admin/registration/${path}`, {
    method, body: JSON.stringify(body), headers: { "Content-Type": "application/json" }, cache: "no-store", signal
  });
  if (redirectForAdminResponse(response)) throw new Error("Sign in again to continue.");
  const result = await response.json().catch(() => null);
  if (!response.ok) {
    const errors: string[][] = Object.values(result?.errors ?? {});
    throw new Error(errors.flat()[0] ?? result?.message ?? "The request could not be completed.");
  }
  return result as T;
}

export function RegistrationWorkspace({ initial, canCloseout }: { initial: RegistrationOverview; canCloseout: boolean }) {
  const router = useRouter();
  const [year, setYear] = useState(initial.seasons[0]?.year ?? 0);
  const [view, setView] = useState<Kind | "Settings">(initial.seasons.length ? "Application" : "Settings");
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [queue, setQueue] = useState<Queue | null>(null);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [notice, setNotice] = useState({ message: "", success: false });
  const [busy, setBusy] = useState(false);
  const [loadingQueue, setLoadingQueue] = useState(false);
  const [refresh, setRefresh] = useState(0);
  const heading = useRef<HTMLHeadingElement>(null);
  const current = initial.seasons.find(season => season.year === year);

  useEffect(() => {
    if (view === "Settings" || !current?.year) return;
    const controller = new AbortController();
    void call<Queue>("queue", "POST", {
      ...filters, year: current.year, kind: view, status: filters.status || null, transfer: filters.transfer || null
    }, controller.signal).then(value => {
      if (controller.signal.aborted) return;
      setQueue(value); setLoadingQueue(false);
    }).catch((error: unknown) => {
      if (controller.signal.aborted) return;
      setLoadingQueue(false);
      setNotice({ message: error instanceof Error ? error.message : "The queue could not be loaded.", success: false });
    });
    return () => controller.abort();
  }, [current?.year, view, filters, refresh]);

  useEffect(() => { if (detail) heading.current?.focus(); }, [detail]);

  async function action(run: () => Promise<void>, message = "") {
    if (busy) return;
    setBusy(true); setNotice({ message: "", success: false });
    try {
      await run();
      if (message) setNotice({ message, success: true });
    } catch (error) { setNotice({ message: error instanceof Error ? error.message : "Try again.", success: false }); }
    finally { setBusy(false); }
  }
  const reload = () => { setRefresh(value => value + 1); router.refresh(); };
  function navigate(nextView: Kind | "Settings", nextYear = year) {
    setView(nextView); setYear(nextYear); setDetail(null); setQueue(null); setFilters({ ...emptyFilters });
    setNotice({ message: "", success: false }); setLoadingQueue(nextView !== "Settings" && nextYear !== 0);
  }

  async function openDetail(id: string) {
    await action(async () => { setDetail(await call<Detail>("detail", "POST", { id })); });
  }

  async function saveReview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const form = new FormData(event.currentTarget);
    await action(async () => {
      await call("review", "PUT", { id: detail.id, revision: detail.revision, status: form.get("status"),
        transfer: detail.kind === "Returning" ? "NotNeeded" : form.get("transfer"),
        photoConsent: form.get("photoConsent"), competitionReady: form.get("competitionReady") === "on" });
      setDetail(await call<Detail>("detail", "POST", { id: detail.id }));
      reload();
    }, "Review saved. No email was sent.");
  }

  async function saveSeason(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const body = Object.fromEntries(form);
    await action(async () => {
      const saved = await call<Season>("season", "PUT", { ...body, year: Number(body.year),
        revision: current?.revision ?? null, ageGroups: String(body.ageGroups).split(/\r?\n/).map(x => x.trim()).filter(Boolean) });
      setYear(saved.year); router.refresh();
    }, "Season saved. Intake is still closed.");
  }

  const handoff = current && detail ? [
    `Email PDFs or clear photos to ${current.paperworkEmail} with ${detail.receiptReference} in the subject, or hand them in person: ${current.paperworkHandoff}`,
    "Photo consent is optional and does not affect enrollment."
  ] : [];

  return <div className="space-y-5">
    <div className="flex flex-wrap items-end gap-3">
      {field("Season", <select className={control} value={year} disabled={busy} onChange={e => navigate(e.target.value === "0" ? "Settings" : view, Number(e.target.value))}>
        <option value={0}>Add a season</option>{initial.seasons.map(item => <option key={item.year} value={item.year}>{item.year} — {item.title}</option>)}
      </select>)}
      {([["Application", "New Applications"], ["Returning", "Returning Check-Ins"], ["Settings", "Season Settings"]] as const).map(([value, label]) =>
        <button type="button" key={value} className={button} aria-pressed={view === value} disabled={busy || (value !== "Settings" && !current)}
          onClick={() => navigate(value)}>{label}</button>)}
    </div>
    <FormNotice message={notice.message} success={notice.success} />
    {view === "Settings" ? <>
      <form method="post" className="grid gap-4 border border-slate-200 bg-white p-4 sm:grid-cols-2" onSubmit={saveSeason} key={current?.revision ?? "new"}>
        {current ? <input type="hidden" name="year" value={current.year} /> : field("Season year", <input name="year" type="number" min={2026} max={2100} defaultValue={2027} required className={control} />)}
        {field("Season title", <input name="title" maxLength={120} defaultValue={current?.title} required className={control} />)}
        {field("Opening date (Eastern)", <input name="openingDate" type="date" defaultValue={current?.openingDate ?? "2026-11-27"} required className={control} />)}
        {field("Season end date (Eastern)", <input name="endDate" type="date" defaultValue={current?.endDate} required className={control} />)}
        {field("Eligible age groups (one per line)", <textarea name="ageGroups" maxLength={400} defaultValue={current?.ageGroups} required className={control} />)}
        {field("Practice location", <input name="practiceLocation" maxLength={250} defaultValue={current?.practiceLocation} required className={control} />)}
        {field("Practice schedule", <textarea name="practiceSchedule" maxLength={500} defaultValue={current?.practiceSchedule} required className={control} />)}
        {field("Fees", <input name="fees" maxLength={250} defaultValue={current?.fees} required className={control} />)}
        {field("Expected reply window (shown to families)", <input name="replyWindow" maxLength={250} defaultValue={current?.replyWindow} required className={control} />)}
        {field("Designated club paperwork inbox (staff only)", <input name="paperworkEmail" type="email" maxLength={254} defaultValue={current?.paperworkEmail} required className={control} />)}
        {field("In-person handoff instructions (staff only)", <textarea name="paperworkHandoff" maxLength={500} defaultValue={current?.paperworkHandoff} required className={control} />)}
        {field("Review owner", <input name="reviewOwner" maxLength={120} defaultValue={current?.reviewOwner} required className={control} />)}
        {field("Retention owner", <input name="retentionOwner" maxLength={120} defaultValue={current?.retentionOwner} required className={control} />)}
        <div className="sm:col-span-2"><button className={button} disabled={busy || current?.isOpen || Boolean(current?.purgedAtUtc)}>Save season details</button></div>
      </form>
      {current && !current.purgedAtUtc && <form method="post" className="space-y-3 border bg-white p-4" key={current.revision} onSubmit={event => {
        event.preventDefault();
        const approved = new FormData(event.currentTarget).get("approved") === "on";
        void action(async () => {
          await call("opening", "PUT", { year, revision: current.revision, open: !current.isOpen, launchChecksApproved: approved });
          router.refresh();
        }, current.isOpen ? "Intake closed." : "Intake opened.");
      }}>
        <p className="font-bold">Intake switch: {current.isOpen ? "Open" : "Closed"}</p>
        {!current.isOpen && <label className="flex items-start gap-2 text-sm"><input type="checkbox" name="approved" className="mt-1" required />
          <span>The club approved season details, blank forms, the privacy notice, decision and handoff instructions, and review and retention owners. The public page and Admin queue have been checked.</span>
        </label>}
        <button className={button} disabled={busy}>{current.isOpen ? "Close intake" : "Open intake"}</button>
      </form>}
      {current && <section className="space-y-3 border bg-white p-4">
        <h2 className="font-bold">Season totals</h2><p className="text-sm">Aggregate page visits, not unique people. No ad-platform tracking.</p>
        <ul className="space-y-1 text-sm">{initial.counts.filter(x => x.seasonYear === year).map(count => <li key={count.channel}>
          {count.channel === "Direct" ? "Direct / other" : count.channel}: {count.visits} visits · {count.applications} applications · {count.returning} check-ins · {count.enrolled} enrolled
        </li>)}</ul>
        {current.purgedAtUtc && <p className="text-sm">Identifiable intake records removed on {date(current.purgedAtUtc)}.</p>}
      </section>}
      {current && canCloseout && initial.closeoutEligibleYears.includes(current.year) && <form method="post" className="space-y-3 border border-red-200 bg-white p-4" key={`closeout-${current.revision}`}
        onSubmit={event => {
          event.preventDefault();
          const confirmYear = new FormData(event.currentTarget).get("confirmYear");
          void action(async () => {
            await call("closeout", "POST", { year, revision: current.revision, confirmYear });
            router.refresh();
          }, "Identifiable submissions and their review history were removed. Aggregate counts were retained.");
        }}>
        <h2 className="font-bold">Season closeout</h2>
        <p className="text-sm">After 90 days, permanently remove this season’s applications, check-ins and review history. This does not delete paperwork in the club inbox.</p>
        {field("Type the season year to confirm", <input name="confirmYear" required pattern={String(year)} inputMode="numeric" className={control} />)}
        <button className={button} disabled={busy}>Remove identifiable intake records</button>
      </form>}
    </> : detail ? <section className="space-y-4 border bg-white p-4">
      <button className={button} disabled={busy} onClick={() => setDetail(null)}>Back to queue</button>
      <h2 ref={heading} tabIndex={-1} className="text-xl font-bold">{detail.athleteFirstName} · {detail.receiptReference}</h2>
      <dl className="grid gap-3 break-words sm:grid-cols-2">{[
        ["Guardian", detail.guardianName], ["Email", detail.guardianEmail], ["Phone", detail.guardianPhone], ["Age group", detail.ageGroup],
        ["Practice availability", detail.availability], ["Experience", detail.experience], ["Previous club", detail.clubName ?? detail.priorClub],
        ["Affiliations", detail.affiliations?.replaceAll(",", ", ")], ["Last meet", detail.lastMeetMonth]
      ].filter(([, value]) => value).map(([name, value]) => <div key={name}><dt className="font-bold">{name}</dt><dd>{value}</dd></div>)}</dl>
      <form method="post" className="grid gap-4 sm:grid-cols-2" onSubmit={saveReview} key={detail.revision}>
        {field("Application status", <select name="status" className={control} defaultValue={detail.status}>{[detail.status, ...transitions[detail.status]].map(option)}</select>)}
        {detail.kind === "Application" && field("Transfer review", <select name="transfer" className={control} defaultValue={detail.transfer}>{(["NeedsReview", "NotNeeded", "Cleared"] as const).map(option)}</select>)}
        {field("Optional photo consent", <select name="photoConsent" className={control} defaultValue={detail.photoConsent}>{(["NotRecorded", "Granted", "Declined"] as const).map(option)}</select>)}
        <label className="flex items-center gap-2"><input type="checkbox" name="competitionReady" defaultChecked={detail.competitionReady} />Competition-ready</label>
        <div><button className={button} disabled={busy}>Save review</button></div>
      </form>
      {detail.kind === "Application" && <p className="text-sm">Verify eligibility outside the app: <a className="underline" href="https://image.aausports.org/dnn/athletics/Club-Release-Form.pdf">AAU club release</a> · <a className="underline" href="https://www.usatf.org/home-normal/top-utility-nav-content/membership/affiliation-update-requests">USATF affiliation updates</a>.</p>}
      {["Offered", "PaperworkReceived", "Enrolled"].includes(detail.status) && <aside className="border p-3 text-sm"><h3 className="font-bold">Paperwork instructions for staff to send</h3>{handoff.map(line => <p className="mt-1 break-words" key={line}>{line}</p>)}</aside>}
      <div><h3 className="font-bold">Review history</h3>{detail.history.length ? <ol className="space-y-2">{detail.history.map((entry, index) =>
        <li key={index} className="text-sm">{date(entry.changedAtUtc)} · {entry.reviewer ?? entry.changedByUserId} · {labels[entry.status]} · {labels[entry.transfer]} · Photo consent: {labels[entry.photoConsent]}{entry.competitionReady ? " · Competition-ready" : ""}</li>)}</ol> : <p className="text-sm">No staff changes yet.</p>}</div>
    </section> : <>
      <h2 className="text-xl font-bold">{view === "Application" ? "New Applications" : "Returning Check-Ins"}</h2>
      <form method="post" className="grid gap-3 border bg-white p-4 sm:grid-cols-2 xl:grid-cols-5" key={`${year}:${view}`}
        onSubmit={event => {
          event.preventDefault();
          const values = new FormData(event.currentTarget);
          setLoadingQueue(true); setNotice({ message: "", success: false }); setFilters({ page: 1, search: String(values.get("search") ?? ""),
            status: String(values.get("status") ?? ""), ageGroup: String(values.get("ageGroup") ?? ""), transfer: String(values.get("transfer") ?? "") });
        }}>
        {field("Search name, email, or reference", <input name="search" maxLength={120} className={control} />)}
        {field("Status", <select name="status" className={control}><option value="">All statuses</option>{statusValues.map(option)}</select>)}
        {view === "Application" && <>
          {field("Age group", <select name="ageGroup" className={control}><option value="">All ages</option>
            {current?.ageGroups.split("\n").filter(Boolean).map(age => <option key={age}>{age}</option>)}</select>)}
          {field("Transfer review", <select name="transfer" className={control}><option value="">All transfers</option>{(["NeedsReview", "NotNeeded", "Cleared"] as const).map(option)}</select>)}
        </>}
        <button className={`${button} self-end`} disabled={loadingQueue}>Apply filters</button>
      </form>
      <p role="status" className="text-sm">{loadingQueue ? "Loading…" : queue ? `${queue.count} records` : ""}</p>
      {!loadingQueue && queue && (queue.items.length ? <ul className="space-y-2">{queue.items.map(item => <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 border bg-white p-4">
        <div className="min-w-0 break-words"><strong>{item.athleteFirstName}</strong><p className="text-sm">{item.receiptReference} · {labels[item.status]}{item.ageGroup && ` · ${item.ageGroup}`}</p>
          {item.transfer === "NeedsReview" && <p className="text-sm font-semibold">Needs transfer review</p>}</div>
        <button className={button} disabled={busy} onClick={() => void openDetail(item.id)}>Review {item.athleteFirstName}</button>
      </li>)}</ul> : <p>No matching records.</p>)}
      {queue && queue.totalPages > 1 && <nav className="flex items-center gap-3" aria-label="Registration queue pages">
        <button className={button} disabled={loadingQueue || queue.page <= 1} onClick={() => { setLoadingQueue(true); setFilters({ ...filters, page: queue.page - 1 }); }}>Previous</button>
        <span>Page {queue.page} of {queue.totalPages}</span>
        <button className={button} disabled={loadingQueue || queue.page >= queue.totalPages} onClick={() => { setLoadingQueue(true); setFilters({ ...filters, page: queue.page + 1 }); }}>Next</button>
      </nav>}
    </>}
  </div>;
}
