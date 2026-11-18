"use client";

import { useEffect, useId, useRef, useState, useSyncExternalStore } from "react";
import type { FormEvent } from "react";
import { ArrowLeft, ArrowRight, Check, Pencil } from "lucide-react";
import { SupportReference } from "@/components/shared/support-reference";
import { registrationDate, type PublicRegistrationSeason, type RegistrationChannel } from "@/lib/registration";
import { applicationFields, applicationSteps, easternToday, firstIncompleteStep, reviewStep, stepForField, stepFromHash, stepHash, validateStep } from "./starting-line-steps";
import type { Draft, Errors, Field } from "./starting-line-steps";
import { RegistrationFields, RegistrationReview } from "./starting-line-fields";
import styles from "./starting-line-form.module.css";

type Kind = "Application" | "Returning";
type Submission = { key: string; payload: string };

function subscribeHash(onChange: () => void) {
  window.addEventListener("hashchange", onChange);
  return () => window.removeEventListener("hashchange", onChange);
}
const readHash = () => window.location.hash;
const serverHash = () => "";

function go(step: number, replace = false) {
  const hash = stepHash(step);
  if (window.location.hash === hash) return;
  if (!replace) { window.location.hash = hash; return; }
  // Only a constant step slug enters history. Preserve Next’s own router state.
  window.history.replaceState(window.history.state, "", hash);
  window.dispatchEvent(new HashChangeEvent("hashchange"));
}

export function RegistrationVisit({ year, channel }: { year: number; channel: RegistrationChannel }) {
  const last = useRef("");
  useEffect(() => {
    const key = `${year}:${channel}`;
    if (last.current === key) return;
    last.current = key;
    void fetch("/api/public/registration/visit", {
      method: "POST", headers: { "Content-Type": "application/json" }, keepalive: true,
      body: JSON.stringify({ year, channel })
    }).catch(() => {});
  }, [year, channel]);
  return null;
}

export function RegistrationIntake({ season, channel, kind, intakeClosed = false, onIntakeClosed }: {
  season: PublicRegistrationSeason; channel: RegistrationChannel; kind: Kind;
  intakeClosed?: boolean; onIntakeClosed?: () => void;
}) {
  const prefix = useId();
  const application = kind === "Application";
  const hash = useSyncExternalStore(subscribeHash, readHash, serverHash);
  const [draft, setDraft] = useState<Draft>({});
  const [edit, setEdit] = useState<{ step: number; original: Draft } | null>(null);
  const [errors, setErrors] = useState<Errors>({});
  const [message, setMessage] = useState("");
  const [reference, setReference] = useState("");
  const [support, setSupport] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [closed, setClosed] = useState(false);
  const [conflict, setConflict] = useState(false);
  const [outdated, setOutdated] = useState(false);
  const [copied, setCopied] = useState<"copied" | "manual" | null>(null);
  const last = useRef<Submission | null>(null);
  const locked = useRef(false);
  const formRef = useRef<HTMLFormElement>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const noticeRef = useRef<HTMLDivElement>(null);
  const referenceRef = useRef<HTMLElement>(null);
  const [today] = useState(easternToday);
  const blocked = closed || (intakeClosed && !uncertain);
  const statusHref = channel === "direct" ? "/registration" : `/registration?src=${channel}`;
  const lockedFields = busy || uncertain || blocked || conflict || outdated;
  const requested = stepFromHash(hash);
  const step = requested < 0 ? 0 : reference || busy || uncertain || closed || conflict || outdated ? reviewStep :
    Math.min(requested, firstIncompleteStep(draft, season.firstPracticeDate, today));
  const info = applicationSteps[step];
  const isEditing = edit?.step === step;
  const waitingForFocus = useRef<string | null>(null);
  const previousStep = useRef<number | null>(null);

  useEffect(() => {
    function leaveEdit() {
      setEdit(old => old && stepFromHash(window.location.hash) !== old.step ? null : old);
    }
    window.addEventListener("hashchange", leaveEdit);
    return () => window.removeEventListener("hashchange", leaveEdit);
  }, []);

  function focusField(name: string) {
    const control = formRef.current?.querySelector<HTMLElement>(`[name="${name}"]`);
    if (!control) return;
    const details = control.closest("details");
    if (details) details.open = true;
    control.focus();
  }
  useEffect(() => {
    if (!application || requested < 0) { previousStep.current = null; return; }
    if (!reference && requested !== step) go(step, true);
    const pending = waitingForFocus.current;
    if (pending) { waitingForFocus.current = null; focusField(pending); }
    else if (previousStep.current !== step && !reference) {
      headingRef.current?.focus({ preventScroll: true });
      headingRef.current?.scrollIntoView({ block: "nearest", behavior: "instant" });
    }
    previousStep.current = step;
  }, [application, requested, step, reference]);

  function update(name: Field | "website", value: string | string[]) {
    if (lockedFields) return;
    setDraft(old => ({ ...old, [name]: value }));
    setErrors(old => {
      if (!old[name]) return old;
      const next = { ...old }; delete next[name]; return next;
    });
  }
  function showErrors(next: Errors, all = false) {
    setErrors(next);
    const field = Object.keys(next).find(name => stepForField(name) !== -1);
    const target = field ? stepForField(field) : -1;
    if (field && application && all && target !== step) {
      waitingForFocus.current = field;
      setEdit({ step: target, original: draft });
      go(target);
    } else if (field) {
      window.requestAnimationFrame(() => focusField(field));
    } else {
      window.requestAnimationFrame(() => noticeRef.current?.focus());
    }
  }
  async function copyReference() {
    try { await navigator.clipboard.writeText(reference); setCopied("copied"); }
    catch {
      const value = referenceRef.current, selection = window.getSelection();
      if (value && selection) {
        const range = document.createRange(); range.selectNodeContents(value);
        selection.removeAllRanges(); selection.addRange(range);
      }
      setCopied("manual");
    }
  }
  function startAnother() {
    setDraft({}); setEdit(null); setReference(""); setCopied(null); setMessage(""); setErrors({}); setSupport(null);
    if (application) { waitingForFocus.current = "athleteFirstName"; go(0, true); }
    window.requestAnimationFrame(() => focusField(application ? "athleteFirstName" : "guardianEmail"));
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locked.current || blocked || conflict || outdated) return;
    if (!uncertain) {
      const local = validateStep(application ? step : "returning", draft, season.firstPracticeDate, today);
      // Respect the browser's parsing, e.g. incomplete native dates and bad autofill.
      for (const el of event.currentTarget.elements) {
        if ((el instanceof HTMLInputElement || el instanceof HTMLSelectElement) && !el.validity.valid && !local[el.name])
          local[el.name] = [el.validationMessage];
      }
      if (Object.keys(local).length) { showErrors(local); return; }
      if (application && step !== reviewStep) {
        setMessage(""); setErrors({}); setSupport(null);
        if (isEditing) { setEdit(null); go(reviewStep); } else go(step + 1);
        return;
      }
    }
    const payload = uncertain && last.current ? last.current.payload : JSON.stringify({
      seasonYear: season.year, kind, channel, guardianEmail: draft.guardianEmail, athleteFirstName: draft.athleteFirstName,
      website: draft.website ?? "", ...(application && { applicationVersion: 2, ...applicationFields(draft) })
    });
    const key = last.current?.payload === payload ? last.current.key : crypto.randomUUID();
    last.current = { key, payload }; locked.current = true;
    setBusy(true); setMessage(""); setErrors({}); setSupport(null);
    try {
      const response = await fetch("/api/public/registration/applications", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...JSON.parse(payload), requestKey: key })
      });
      const result = await response.json();
      if (response.ok && result.reference) {
        setUncertain(false); setReference(result.reference); setDraft({}); setEdit(null); last.current = null;
        window.requestAnimationFrame(() => noticeRef.current?.focus());
      } else {
        if (response.status === 409 && result.code === "intake_closed") { setClosed(true); onIntakeClosed?.(); }
        if (response.status === 409 && result.code === "retry_conflict") setConflict(true);
        if (response.status === 409 && result.code === "form_outdated") setOutdated(true);
        setUncertain(response.status >= 500 || (uncertain && response.status === 429));
        setMessage(result.message ?? "We couldn’t confirm the submission.");
        setSupport(result.referenceId ?? null); showErrors(result.errors ?? {}, true);
      }
    } catch {
      setUncertain(true); setMessage("We couldn’t confirm the submission. Retry this same form without changing the answers.");
      window.requestAnimationFrame(() => noticeRef.current?.focus());
    } finally { locked.current = false; setBusy(false); }
  }
  if (reference) return <div className={styles.receipt}>
    <div ref={noticeRef} tabIndex={-1} role="status">
      <p className={styles.kicker}><Check size={16} aria-hidden="true" /> Saved</p>
      {application ? <h1>Application received</h1> : <h2>Check-in received</h2>}
      <p>Save this reference: <strong ref={referenceRef}>{reference}</strong></p>
      <p>No automatic confirmation email is sent. This does not reserve a spot. The club will follow up by email. {season.replyWindow}</p>
      <p>{application ? "Please wait for an offer before sending completed paperwork." : "Returning athletes do not need to submit paperwork."}</p>
    </div>
    <div className={styles.actions}>
      <button type="button" className={styles.next} onClick={() => void copyReference()}>Copy reference</button>
      {!intakeClosed && <button type="button" className={styles.back} onClick={startAnother}>
        {application ? "Apply for another athlete" : "Check in another athlete"}</button>}
    </div>
    <p aria-live="polite">{copied === "copied" ? "Reference copied." : copied === "manual" ? "Copy the reference above manually." : ""}</p>
  </div>;
  return <div className={application ? styles.layout : styles.shortForm}>
    {application && <aside className={styles.rail} aria-label="Application progress">
      <p className={styles.kicker}>New athlete application</p>
      <ol>{applicationSteps.map(({ label, slug }, i) => <li key={slug} aria-current={i === step ? "step" : undefined}
        data-complete={i < step}><span aria-hidden="true">{i < step ? <Check size={13} /> : String(i + 1).padStart(2, "0")}</span>{label}</li>)}</ol>
    </aside>}
    <form method="post" noValidate ref={formRef} onSubmit={submit} aria-busy={busy} className={styles.form}>
      {application && <header className={styles.stepHead}>
        <p className={styles.kicker}>Step {step + 1} of 6 <span aria-hidden="true">/</span> {info.label}</p>
        <div className={styles.rule} aria-hidden="true">{applicationSteps.map((s, i) => <i key={s.slug} data-on={i <= step} />)}</div>
        <h1 data-step-heading="" ref={headingRef} tabIndex={-1}>{info.title}</h1>
        <p>{info.note} {step === 0 && "Fields are required unless marked optional."}</p>
      </header>}
      {application && step === reviewStep ? <div className={styles.review}>
        {applicationSteps.slice(0, reviewStep).map(({ slug, label }, i) => <section key={slug}>
          <header><h2>{label}</h2><button type="button" disabled={lockedFields} aria-label={`Edit ${label.toLowerCase()}`}
            onClick={() => { setEdit({ step: i, original: draft }); setMessage(""); go(i); }}><Pencil size={13} aria-hidden="true" /> Edit</button></header>
          <RegistrationReview index={i} draft={draft} />
        </section>)}
      </div> : <RegistrationFields key={application ? step : "returning"} prefix={prefix} step={application ? step : "returning"}
        draft={draft} update={update} errors={errors} disabled={lockedFields}
        practiceDate={season.firstPracticeDate ? registrationDate(season.firstPracticeDate) : null} schedule={season.practiceSchedule} />}
      <div className="sr-only" aria-hidden="true"><label htmlFor={prefix + "-website"}>Leave blank</label>
        <input id={prefix + "-website"} name="website" tabIndex={-1} autoComplete="off" value={draft.website ?? ""}
          onChange={e => update("website", e.target.value)} /></div>
      {(message || blocked) && <div ref={noticeRef} tabIndex={-1} role="alert" className="form-status error">
        {message || "Applications are not open. No new submission was saved."}<SupportReference referenceId={support} /></div>}
      <div className={styles.actions}>
        {application && (step > 0 || isEditing) && !lockedFields && <button type="button" className={styles.back} onClick={() => {
          setMessage(""); setErrors({});
          if (isEditing && edit) { setDraft(edit.original); setEdit(null); go(reviewStep); } else go(step - 1);
        }}><ArrowLeft aria-hidden="true" size={16} />{isEditing ? "Cancel edit" : "Back"}</button>}
        {blocked ? <a className={styles.back} href={statusHref}>Check registration status</a> :
          outdated ? <a className={styles.back} href={statusHref + "#apply"} onClick={event => {
            event.preventDefault(); window.location.reload();
          }}>Reload application</a> : conflict ? <a className={styles.back} href="/contact">Contact the club</a> :
          <button type="submit" className={styles.next} disabled={busy}>
            {busy ? "Submitting…" : uncertain ? "Retry submission" : !application ? "Submit check-in" : step === reviewStep
              ? "Submit application" : isEditing ? "Return to review" : "Continue"}<ArrowRight aria-hidden="true" size={17} />
          </button>}
      </div>
      <p className={styles.privacy}>{!application || step === reviewStep ? `This ${application ? "application" : "check-in"} doesn’t reserve a spot. ` : ""}
        <a href="/privacy#registration-privacy" target="_blank" rel="noreferrer">Registration privacy notice<span className="sr-only"> (opens in a new tab)</span></a></p>
    </form>
  </div>;
}
