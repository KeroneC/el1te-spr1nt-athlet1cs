"use client";

import { useEffect, useId, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import type { PublicRegistrationSeason, RegistrationChannel } from "@/lib/registration";

type Kind = "Application" | "Returning";
const yesNo = [["Yes", "Yes"], ["No", "No"], ["Unsure", "Not sure"]];

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

export function RegistrationIntake({ season, channel, kind }: {
  season: PublicRegistrationSeason; channel: RegistrationChannel; kind: Kind;
}) {
  const prefix = useId();
  const application = kind === "Application";
  const [prior, setPrior] = useState("");
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState("");
  const [reference, setReference] = useState("");
  const [support, setSupport] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const last = useRef<{ key: string; payload: string } | null>(null);
  const locked = useRef(false);
  const formRef = useRef<HTMLFormElement>(null);
  const noticeRef = useRef<HTMLDivElement>(null);

  function props(name: string) {
    return { id: `${prefix}-${name}`, name, "aria-invalid": Boolean(errors[name]),
      "aria-describedby": errors[name] ? `${prefix}-${name}-error` : undefined };
  }

  function field(name: string, label: string, control: ReactNode, hint?: string) {
    return <div className="form-field">
      <label htmlFor={`${prefix}-${name}`}>{label}</label>
      {hint && <p className="text-sm text-slate-600">{hint}</p>}{control}
      {errors[name] && <p id={`${prefix}-${name}-error`} className="field-error">{errors[name][0]}</p>}
    </div>;
  }

  function select(name: string, options: string[][], control?: { value: string; onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void }) {
    return <select {...props(name)} required {...control}><option value="">Choose one</option>
      {options.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>;
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locked.current) return;
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

  if (reference) return <div className="contact-form" ref={noticeRef} tabIndex={-1} role="status">
    <h3>{application ? "Application received" : "Check-in received"}</h3>
    <p>Save this reference: <strong>{reference}</strong></p>
    <p>This does not reserve a spot. The club will follow up by email. {season.replyWindow}</p>
    <p>Please wait for an offer before sending completed paperwork.</p>
    <button type="button" className="button button-secondary" onClick={() => { last.current = null; setReference(""); setPrior(""); }}>
      {application ? "Apply for another athlete" : "Check in another athlete"}
    </button>
  </div>;

  return <form method="post" className="contact-form" ref={formRef} onSubmit={submit}>
    <fieldset disabled={busy || uncertain} className="grid min-w-0 gap-4">
      <legend className="sr-only">{application ? "New athlete application" : "Returning athlete check-in"}</legend>
      <div className="form-grid">
        {application && field("guardianName", "Parent or guardian name", <input {...props("guardianName")} autoComplete="name" maxLength={100} required readOnly={uncertain} />)}
        {field("guardianEmail", "Parent or guardian email", <input {...props("guardianEmail")} type="email" autoComplete="email" maxLength={254} required readOnly={uncertain} />)}
        {application && field("guardianPhone", "Parent or guardian phone (optional)", <input {...props("guardianPhone")} type="tel" autoComplete="tel" maxLength={32} readOnly={uncertain} />)}
        {field("athleteFirstName", "Athlete’s first name", <input {...props("athleteFirstName")} maxLength={80} required readOnly={uncertain} />)}
        {application && <>
          {field("ageGroup", "Age group", select("ageGroup", season.ageGroups.map(group => [group, group])))}
          {field("availability", "Can your athlete attend the listed practices?", select("availability", yesNo))}
          {field("experience", "Track experience (optional)", <input {...props("experience")} maxLength={250} readOnly={uncertain} />,
            "A short answer is enough. Please do not enter medical or other sensitive information.")}
          {field("priorClub", "Has your athlete trained or competed with another track and field club?", select("priorClub", yesNo, { value: prior, onChange: event => setPrior(event.target.value) }))}
          {prior === "Yes" && <>
            {field("clubName", "Current or most recent club", <input {...props("clubName")} maxLength={120} required readOnly={uncertain} />)}
            <fieldset className="min-w-0 space-y-2" aria-describedby={errors.affiliations ? `${prefix}-affiliations-error` : undefined}>
              <legend className="font-semibold">Governing bodies (select all that apply, or Not sure)</legend>
              {["AAU", "USATF", "Other", "Unsure"].map(value => <label key={value} className="flex items-center gap-2">
                <input type="checkbox" name="affiliations" value={value} aria-invalid={Boolean(errors.affiliations)} />
                {value === "Unsure" ? "Not sure" : value}
              </label>)}
              {errors.affiliations && <p id={`${prefix}-affiliations-error`} className="field-error">{errors.affiliations[0]}</p>}
            </fieldset>
            {field("lastMeetMonth", "Last meet month and year (optional)", <input {...props("lastMeetMonth")} type="month" placeholder="YYYY-MM" readOnly={uncertain} />)}
          </>}
        </>}
      </div>
      <div className="sr-only" aria-hidden="true"><label htmlFor={`${prefix}-website`}>Leave blank</label>
        <input id={`${prefix}-website`} name="website" tabIndex={-1} autoComplete="off" /></div>
      <p className="text-sm">For parents and guardians. This {application ? "application" : "check-in"} doesn’t reserve a spot.</p>
      <p className="text-sm">We use this information to review and follow up. Do not attach or include private documents. <a className="underline" href="/privacy">Privacy notice</a></p>
    </fieldset>
    {message && <div ref={noticeRef} tabIndex={-1} role="alert" className="form-status error">{message}<SupportReference referenceId={support} /></div>}
    <button type="submit" className="button button-primary" disabled={busy}>
      {busy ? "Submitting…" : uncertain ? "Retry submission" : application ? "Submit application" : "Submit check-in"}
    </button>
  </form>;
}
