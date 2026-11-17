"use client";

import { useEffect, useId, useRef, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import { registrationDate, type PublicRegistrationSeason, type RegistrationChannel } from "@/lib/registration";
import styles from "./registration.module.css";

type Kind = "Application" | "Returning";
const yesNo = [["Yes", "Yes"], ["No", "No"], ["Unsure", "Not sure"]];
const grades = [["Pre-K", "Pre-K"], ["K", "Kindergarten"],
  ...Array.from({ length: 12 }, (_, index) => [String(index + 1), `Grade ${index + 1}`]),
  ["Not applicable", "Not applicable"]];
const states = "AL AK AZ AR CA CO CT DE DC FL GA HI ID IL IN IA KS KY LA ME MD MA MI MN MS MO MT NE NV NH NJ NM NY NC ND OH OK OR PA RI SC SD TN TX UT VT VA WA WV WI WY AS GU MP PR VI"
  .split(" ").map(state => [state, state]);
const genderOptions = [["Female", "Female"], ["Male", "Male"], ["Prefer not to answer", "Prefer not to answer"]];
const fullApplicationFields = [
  "athleteLastName", "dateOfBirth", "schoolGrade", "schoolDistrict", "gender", "guardianName", "guardianPhone",
  "streetAddress", "addressLine2", "city", "state", "postalCode",
  "secondaryGuardianName", "secondaryGuardianEmail", "secondaryGuardianPhone", "emergencyContactName",
  "emergencyContactPhone", "emergencyContactRelationship", "availability", "experience", "priorClub", "clubName",
  "lastMeetMonth"
];
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
  const [district, setDistrict] = useState("");
  const [affiliations, setAffiliations] = useState<Affiliation[]>([]);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState("");
  const [reference, setReference] = useState("");
  const [support, setSupport] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const [closed, setClosed] = useState(false);
  const [conflict, setConflict] = useState(false);
  const [outdated, setOutdated] = useState(false);
  const [copied, setCopied] = useState<"copied" | "manual" | null>(null);
  const last = useRef<{ key: string; payload: string } | null>(null);
  const locked = useRef(false);
  const formRef = useRef<HTMLFormElement>(null);
  const noticeRef = useRef<HTMLDivElement>(null);
  const referenceRef = useRef<HTMLElement>(null);
  const blocked = closed || (intakeClosed && !uncertain);
  const statusHref = channel === "direct" ? "/registration" : `/registration?src=${channel}`;
  const lockedFields = busy || uncertain || blocked || conflict || outdated;

  function props(name: string, hasHint = false) {
    const descriptions = [hasHint && `${prefix}-${name}-hint`, errors[name] && `${prefix}-${name}-error`].filter(Boolean);
    return { id: `${prefix}-${name}`, name, "aria-invalid": Boolean(errors[name]),
      "aria-describedby": descriptions.length ? descriptions.join(" ") : undefined };
  }

  function field(name: string, label: string, control: ReactNode, hint?: string) {
    return <div className={styles.field}>
      <label htmlFor={`${prefix}-${name}`}>{label}</label>
      {control}{hint && <p id={`${prefix}-${name}-hint`} className={styles.fieldHint}>{hint}</p>}
      {errors[name] && <p id={`${prefix}-${name}-error`} className="field-error">{errors[name][0]}</p>}
    </div>;
  }

  function select(name: string, options: string[][], control?: { value: string; onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void }, required = true) {
    return <select {...props(name)} required={required} {...control}><option value="">{required ? "Choose one" : "Leave blank"}</option>
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
    setDistrict("");
    setAffiliations([]);
    setCopied(null);
    window.requestAnimationFrame(() => {
      document.getElementById(`${prefix}-${application ? "athleteFirstName" : "guardianEmail"}`)?.focus();
    });
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locked.current || blocked || conflict || outdated) return;
    const form = new FormData(event.currentTarget);
    const payload = uncertain && last.current ? last.current.payload : JSON.stringify({
      seasonYear: season.year, kind, channel,
      guardianEmail: form.get("guardianEmail"), athleteFirstName: form.get("athleteFirstName"), website: form.get("website"),
      ...(application && { applicationVersion: 2,
        ...Object.fromEntries(fullApplicationFields.map(name => [name, form.get(name)])),
        affiliations: form.getAll("affiliations") })
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
        if (response.status === 409 && result.code === "form_outdated") setOutdated(true);
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
        const disclosure = invalid?.closest("details");
        if (disclosure) disclosure.open = true;
        (invalid ?? noticeRef.current)?.focus();
      });
    }
  }

  if (reference) return <div className={styles.receipt}>
    <div ref={noticeRef} tabIndex={-1} role="status" className={styles.receiptSummary}>
      <p className={styles.pathOverline}>Saved</p>
      <h3>{application ? "Application received" : "Check-in received"}</h3>
      <p>Save this reference: <strong ref={referenceRef}>{reference}</strong></p>
      <p>No automatic confirmation email is sent. This does not reserve a spot. The club will follow up by email. {season.replyWindow}</p>
      <p>{application ? "Please wait for an offer before sending completed paperwork." : "Returning athletes do not need to submit paperwork."}</p>
    </div>
    <div className="registration-actions">
      <button type="button" className={styles.primaryAction} onClick={() => void copyReference()}>Copy reference</button>
      {!intakeClosed && <button type="button" className={styles.secondaryAction} onClick={startAnother}>
        {application ? "Apply for another athlete" : "Check in another athlete"}
      </button>}
    </div>
    <p className="registration-copy-status text-sm" aria-live="polite">
      {copied === "copied" ? "Reference copied." : copied === "manual" ? "Copy the reference above manually." : ""}
    </p>
  </div>;

  return <form method="post" className={styles.form} ref={formRef} onSubmit={submit} aria-busy={busy} onInvalidCapture={event => {
    const disclosure = (event.target as HTMLElement).closest("details");
    if (disclosure) disclosure.open = true;
  }}>
    <fieldset disabled={lockedFields} className={styles.formFields}>
      <legend className="sr-only">{application ? "New athlete application" : "Returning athlete check-in"}</legend>
      {application ? <>
        <fieldset className={styles.group}>
          <legend><span aria-hidden="true">01</span> Athlete details</legend>
          <div className={styles.fieldGrid}>
            {field("athleteFirstName", "Athlete’s legal first name", <input {...props("athleteFirstName")} maxLength={80} required />)}
            {field("athleteLastName", "Athlete’s legal last name", <input {...props("athleteLastName")} maxLength={80} required />)}
            {field("dateOfBirth", "Date of birth", <input {...props("dateOfBirth", true)} type="date" required />,
              season.firstPracticeDate ? `Athletes must turn six by first practice on ${registrationDate(season.firstPracticeDate)}.` : "Athletes must turn six by first practice.")}
            {field("schoolGrade", "Current school grade", select("schoolGrade", grades))}
            {field("schoolDistrict", "School district", <>
              <input {...props("schoolDistrict", true)} maxLength={120} required value={district} onChange={event => setDistrict(event.target.value)} />
              <button type="button" className={styles.inlineAction} onClick={() => setDistrict("Not sure / not applicable")}>Not sure / not applicable</button>
            </>, "Enter your district, or use the option above if you aren’t sure.")}
            {field("gender", "Gender (optional)", select("gender", genderOptions, undefined, false))}
          </div>
        </fieldset>
        <fieldset className={styles.group}>
          <legend><span aria-hidden="true">02</span> Guardian and household</legend>
          <div className={styles.fieldGrid}>
            {field("guardianName", "Parent or guardian name", <input {...props("guardianName")} autoComplete="section-guardian name" maxLength={100} required />)}
            {field("guardianEmail", "Parent or guardian email", <input {...props("guardianEmail")} type="email" autoComplete="section-guardian email" maxLength={254} required />)}
            {field("guardianPhone", "Parent or guardian phone", <input {...props("guardianPhone")} type="tel" autoComplete="section-guardian tel" maxLength={32} required />)}
            {field("streetAddress", "Street address", <input {...props("streetAddress")} autoComplete="address-line1" maxLength={160} required />)}
            {field("addressLine2", "Address line 2 (optional)", <input {...props("addressLine2")} autoComplete="address-line2" maxLength={120} />)}
            {field("city", "City", <input {...props("city")} autoComplete="address-level2" maxLength={80} required />)}
            {field("state", "State", select("state", states))}
            {field("postalCode", "ZIP code", <input {...props("postalCode")} autoComplete="postal-code" maxLength={20} required />)}
          </div>
          <details className={styles.optionalDetails}>
            <summary>Add a second guardian (optional)</summary>
            <div className={styles.fieldGrid}>
              {field("secondaryGuardianName", "Second guardian name (optional)", <input {...props("secondaryGuardianName")} autoComplete="section-secondary name" maxLength={100} />)}
              {field("secondaryGuardianEmail", "Second guardian email (optional)", <input {...props("secondaryGuardianEmail")} type="email" autoComplete="section-secondary email" maxLength={254} />)}
              {field("secondaryGuardianPhone", "Second guardian phone (optional)", <input {...props("secondaryGuardianPhone")} type="tel" autoComplete="section-secondary tel" maxLength={32} />)}
            </div>
          </details>
        </fieldset>
        <fieldset className={styles.group}>
          <legend><span aria-hidden="true">03</span> Emergency contact</legend>
          <p className={styles.groupHelp}>Provide someone the club can call in an emergency. Ask their permission before sharing their details.</p>
          <div className={styles.fieldGrid}>
            {field("emergencyContactName", "Emergency contact name", <input {...props("emergencyContactName")} autoComplete="section-emergency name" maxLength={100} required />)}
            {field("emergencyContactPhone", "Emergency contact phone", <input {...props("emergencyContactPhone")} type="tel" autoComplete="section-emergency tel" maxLength={32} required />)}
            {field("emergencyContactRelationship", "Relationship to athlete", <input {...props("emergencyContactRelationship")} maxLength={80} required />)}
          </div>
        </fieldset>
        <fieldset className={styles.group}>
          <legend><span aria-hidden="true">04</span> Track background</legend>
          {season.practiceSchedule && <p className={styles.groupHelp}>Practice schedule: {season.practiceSchedule}</p>}
          <div className={styles.fieldGrid}>
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
          </div>
        </fieldset>
      </> : <div className={styles.fieldGrid}>
        {field("guardianEmail", "Parent or guardian email", <input {...props("guardianEmail")} type="email" autoComplete="email" maxLength={254} required />)}
        {field("athleteFirstName", "Athlete’s first name", <input {...props("athleteFirstName")} maxLength={80} required />)}
      </div>}
      <div className="sr-only" aria-hidden="true"><label htmlFor={`${prefix}-website`}>Leave blank</label>
        <input id={`${prefix}-website`} name="website" tabIndex={-1} autoComplete="off" /></div>
      <p className={styles.groupHelp}>For parents and guardians. This {application ? "application" : "check-in"} doesn’t reserve a spot. <a href="/privacy#registration-privacy">Registration privacy notice</a></p>
    </fieldset>
    {(message || blocked) && <div ref={noticeRef} tabIndex={-1} role="alert" className="form-status error">
      {message || "Applications are not open. No new submission was saved."}<SupportReference referenceId={support} />
    </div>}
    {blocked ? <a className={styles.secondaryAction} href={statusHref}>Check registration status</a> :
      outdated ? <a className={styles.secondaryAction} href={`${statusHref}#apply`} onClick={event => {
        // A same-path hash only moves the viewport; explicitly fetch the updated form.
        event.preventDefault();
        window.location.reload();
      }}>Reload application</a> :
      conflict ? <a className={styles.secondaryAction} href="/contact">Contact the club</a> :
      <button type="submit" className={styles.primaryAction} disabled={busy}>
        {busy ? "Submitting…" : uncertain ? "Retry submission" : application ? "Submit application" : "Submit check-in"}
      </button>}
  </form>;
}
