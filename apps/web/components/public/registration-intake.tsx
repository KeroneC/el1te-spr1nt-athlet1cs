"use client";

import { useEffect, useId, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import type { PublicRegistrationSeason, RegistrationChannel } from "@/lib/registration";

type Kind = "Application" | "Returning";
const yesNo = [["Yes", "Yes"], ["No", "No"], ["Unsure", "Not sure"]];
const affiliationOptions = ["AAU", "USATF", "Other", "Unsure"] as const;
type Affiliation = typeof affiliationOptions[number];

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
  const [prior, setPrior] = useState("");
  const [affiliations, setAffiliations] = useState<Affiliation[]>([]);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState("");
  const [reference, setReference] = useState("");
  const [support, setSupport] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [closed, setClosed] = useState(false);
  const [conflict, setConflict] = useState(false);
  const [copied, setCopied] = useState<"copied" | "manual" | null>(null);
  const last = useRef<{ key: string; payload: string } | null>(null);
  const locked = useRef(false);
  const formRef = useRef<HTMLFormElement>(null);
  const noticeRef = useRef<HTMLDivElement>(null);
  const referenceRef = useRef<HTMLElement>(null);
  const blocked = closed || (intakeClosed && !uncertain);
  const statusHref = channel === "direct" ? "/registration" : `/registration?src=${channel}`;

  function props(name: string, hasHint = false) {
    const descriptions = [hasHint && `${prefix}-${name}-hint`, errors[name] && `${prefix}-${name}-error`].filter(Boolean);
    return { id: `${prefix}-${name}`, name, "aria-invalid": Boolean(errors[name]),
      "aria-describedby": descriptions.length ? descriptions.join(" ") : undefined };
  }

  function field(name: string, label: string, control: ReactNode, hint?: string) {
    return <div className="form-field">
      <label htmlFor={`${prefix}-${name}`}>{label}</label>
      {control}{hint && <p id={`${prefix}-${name}-hint`} className="text-sm text-slate-600">{hint}</p>}
      {errors[name] && <p id={`${prefix}-${name}-error`} className="field-error">{errors[name][0]}</p>}
    </div>;
  }

  function select(name: string, options: string[][], control?: { value: string; onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void }) {
    return <select {...props(name)} required {...control}><option value="">Choose one</option>
      {options.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>;
  }

  function toggleAffiliation(value: Affiliation, checked: boolean) {
    setAffiliations(previous => !checked ? previous.filter(item => item !== value) :
      value === "Unsure" ? [value] : [...previous.filter(item => item !== "Unsure" && item !== value), value]);
  }

  async function copyReference() {
    try {
      await navigator.clipboard.writeText(reference);
      setCopied("copied");
    } catch {
      const value = referenceRef.current;
      const selection = window.getSelection();
      if (value && selection) {
        const range = document.createRange();
        range.selectNodeContents(value);
        selection.removeAllRanges();
        selection.addRange(range);
      }
      setCopied("manual");
    }
  }

  function startAnother() {
    last.current = null;
    setReference("");
    setPrior("");
    setAffiliations([]);
    setCopied(null);
    window.requestAnimationFrame(() => {
      document.getElementById(`${prefix}-${application ? "guardianName" : "guardianEmail"}`)?.focus();
    });
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locked.current || blocked || conflict) return;
    const form = new FormData(event.currentTarget);
    const payload = uncertain && last.current ? last.current.payload : JSON.stringify({
      seasonYear: season.year, kind, channel,
      guardianEmail: form.get("guardianEmail"), athleteFirstName: form.get("athleteFirstName"), website: form.get("website"),
      ...(application && { guardianName: form.get("guardianName"), guardianPhone: form.get("guardianPhone"),
        ageGroup: form.get("ageGroup"), availability: form.get("availability"), experience: form.get("experience"),
        priorClub: form.get("priorClub"), clubName: form.get("clubName"), affiliations: form.getAll("affiliations"),
        lastMeetMonth: form.get("lastMeetMonth") })
    });
    const key = last.current?.payload === payload ? last.current.key : crypto.randomUUID();
    last.current = { key, payload };
    locked.current = true;
    setBusy(true); setMessage(""); setErrors({}); setSupport(null);
    try {
      const response = await fetch("/api/public/registration/applications", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...JSON.parse(payload), requestKey: key })
      });
      const result = await response.json();
      if (response.ok && result.reference) {
        setUncertain(false);
        setReference(result.reference);
      } else {
        if (response.status === 409 && result.code === "intake_closed") {
          setClosed(true);
          onIntakeClosed?.();
        }
        if (response.status === 409 && result.code === "retry_conflict") setConflict(true);
        setUncertain(response.status >= 500 || (uncertain && response.status === 429));
        setMessage(result.message ?? "We couldn’t confirm the submission.");
        setErrors(result.errors ?? {});
        setSupport(result.referenceId ?? null);
      }
    } catch {
      setUncertain(true);
      setMessage("We couldn’t confirm the submission. Retry this same form without changing the answers.");
    } finally {
      locked.current = false;
      setBusy(false);
      window.requestAnimationFrame(() => {
        const invalid = formRef.current?.querySelector<HTMLElement>("[aria-invalid=true]");
        (invalid ?? noticeRef.current)?.focus();
      });
    }
  }

  if (reference) return <div className="contact-form registration-receipt">
    <div ref={noticeRef} tabIndex={-1} role="status" className="registration-receipt-summary">
      <h3>{application ? "Application received" : "Check-in received"}</h3>
      <p>Save this reference: <strong ref={referenceRef}>{reference}</strong></p>
      <p>No automatic confirmation email is sent. This does not reserve a spot. The club will follow up by email. {season.replyWindow}</p>
      <p>{application ? "Please wait for an offer before sending completed paperwork." : "Returning athletes do not need to submit paperwork."}</p>
    </div>
    <div className="registration-actions">
      <button type="button" className="button button-primary" onClick={() => void copyReference()}>Copy reference</button>
      {!intakeClosed && <button type="button" className="button button-secondary" onClick={startAnother}>
        {application ? "Apply for another athlete" : "Check in another athlete"}
      </button>}
    </div>
    <p className="registration-copy-status text-sm" aria-live="polite">
      {copied === "copied" ? "Reference copied." : copied === "manual" ? "Copy the reference above manually." : ""}
    </p>
  </div>;

  return <form method="post" className="contact-form registration-form" ref={formRef} onSubmit={submit} aria-busy={busy}>
    <fieldset disabled={busy || uncertain || blocked || conflict} className="grid min-w-0 gap-4">
      <legend className="sr-only">{application ? "New athlete application" : "Returning athlete check-in"}</legend>
      <div className="form-grid">
        {application && field("guardianName", "Parent or guardian name", <input {...props("guardianName")} autoComplete="name" maxLength={100} required readOnly={uncertain} />)}
        {field("guardianEmail", "Parent or guardian email", <input {...props("guardianEmail")} type="email" autoComplete="email" maxLength={254} required readOnly={uncertain} />)}
        {application && field("guardianPhone", "Parent or guardian phone (optional)", <input {...props("guardianPhone")} type="tel" autoComplete="tel" maxLength={32} readOnly={uncertain} />)}
        {field("athleteFirstName", "Athlete’s first name", <input {...props("athleteFirstName")} maxLength={80} required readOnly={uncertain} />)}
        {application && <>
          {field("ageGroup", "Age group", select("ageGroup", season.ageGroups.map(group => [group, group])))}
          {field("availability", "Can your athlete attend the listed practices?", select("availability", yesNo))}
          {field("experience", "Track experience (optional)", <input {...props("experience", true)} maxLength={250} readOnly={uncertain} />,
            "A short answer is enough. Please do not enter medical or other sensitive information.")}
          {field("priorClub", "Has your athlete trained or competed with another track and field club?", select("priorClub", yesNo, { value: prior, onChange: event => setPrior(event.target.value) }))}
          {prior === "Yes" && <>
            {field("clubName", "Current or most recent club", <input {...props("clubName")} maxLength={120} required readOnly={uncertain} />)}
            <fieldset className="registration-affiliations" aria-describedby={`${prefix}-affiliations-hint${errors.affiliations ? ` ${prefix}-affiliations-error` : ""}`}>
              <legend className="font-semibold">Governing bodies</legend>
              <p id={`${prefix}-affiliations-hint`} className="text-sm text-slate-600">Select all that apply, or choose Not sure on its own.</p>
              {affiliationOptions.map(value => <label key={value}>
                <input type="checkbox" name="affiliations" value={value} checked={affiliations.includes(value)}
                  onChange={event => toggleAffiliation(value, event.target.checked)} aria-invalid={Boolean(errors.affiliations)}
                  aria-describedby={errors.affiliations ? `${prefix}-affiliations-error` : undefined} />
                {value === "Unsure" ? "Not sure" : value}
              </label>)}
              {errors.affiliations && <p id={`${prefix}-affiliations-error`} className="field-error">{errors.affiliations[0]}</p>}
            </fieldset>
            {field("lastMeetMonth", "Last meet month and year (optional)", <input {...props("lastMeetMonth", true)} type="month" placeholder="YYYY-MM" readOnly={uncertain} />,
              "If a date picker isn’t available, enter YYYY-MM.")}
          </>}
        </>}
      </div>
      <div className="sr-only" aria-hidden="true"><label htmlFor={`${prefix}-website`}>Leave blank</label>
        <input id={`${prefix}-website`} name="website" tabIndex={-1} autoComplete="off" /></div>
      <p className="text-sm">For parents and guardians. This {application ? "application" : "check-in"} doesn’t reserve a spot.</p>
      <p className="text-sm">We use this information to review and follow up. Do not attach or include private documents. <a className="underline" href="/privacy">Privacy notice</a></p>
    </fieldset>
    {(message || blocked) && <div ref={noticeRef} tabIndex={-1} role="alert" className="form-status error">
      {message || "Applications are not open. No new submission was saved."}<SupportReference referenceId={support} />
    </div>}
    {blocked ? <a className="button button-secondary" href={statusHref}>Check registration status</a> :
      conflict ? <a className="button button-secondary" href="/contact">Contact the club</a> :
      <button type="submit" className="button button-primary" disabled={busy}>
        {busy ? "Submitting…" : uncertain ? "Retry submission" : application ? "Submit application" : "Submit check-in"}
      </button>}
  </form>;
}
