import type { InputHTMLAttributes, ReactNode } from "react";
import { registrationAgeLabel } from "@/lib/registration";
import type { Draft, Field, Errors } from "./starting-line-steps";
import styles from "./starting-line-form.module.css";

type TextField = Exclude<Field, "affiliations">;
type Options = ReadonlyArray<readonly [string, string]>;
const choices: Options = [["Yes", "Yes"], ["No", "No"], ["Unsure", "Not sure"]];
const grades: Options = [["Pre-K", "Pre-K"], ["K", "Kindergarten"],
  ...Array.from({ length: 12 }, (_, i) => [String(i + 1), "Grade " + (i + 1)] as const), ["Not applicable", "Not applicable"]];
const states = "AL AK AZ AR CA CO CT DE DC FL GA HI ID IL IN IA KS KY LA ME MD MA MI MN MS MO MT NE NV NH NJ NM NY NC ND OH OK OR PA RI SC SD TN TX UT VT VA WA WV WI WY AS GU MP PR VI".split(" ").map(s => [s, s] as const);
const genders: Options = [["Female", "Female"], ["Male", "Male"], ["Prefer not to answer", "Prefer not to answer"]];
const affiliationOptions = ["AAU", "USATF", "Other", "Unsure"];

type FieldsProps = { prefix: string; step: number | "returning"; draft: Draft;
  update: (name: Field, value: string | string[]) => void; errors: Errors; disabled: boolean;
  schedule: string | null };

export function RegistrationFields({ prefix, step, draft, update, errors, disabled, schedule }: FieldsProps) {
  function props(name: TextField, hasHint = false) {
    const id = prefix + "-" + name;
    const descriptions = [hasHint && id + "-hint", errors[name] && id + "-error"].filter(Boolean);
    return { id, name, value: draft[name] ?? "", "aria-invalid": !!errors[name],
      "aria-describedby": descriptions.length ? descriptions.join(" ") : undefined };
  }
  function field(name: TextField, label: string, control: ReactNode, hint?: string, full = false) {
    const id = prefix + "-" + name;
    return <div className={styles.field} data-full={full}>
      <label htmlFor={id}>{label}</label>{control}
      {hint && <p id={id + "-hint"} className={styles.hint}>{hint}</p>}
      {errors[name] && <p id={id + "-error"} className={styles.error}>{errors[name][0]}</p>}
    </div>;
  }
  function text(name: TextField, label: string, attr: InputHTMLAttributes<HTMLInputElement>, hint?: string, full = false) {
    return field(name, label, <input {...props(name, !!hint)} {...attr} onChange={e => update(name, e.target.value)} />, hint, full);
  }
  function select(name: TextField, label: string, options: Options, required = true, autoComplete?: string, full = false) {
    return field(name, label, <select {...props(name)} required={required} autoComplete={autoComplete}
      onChange={e => update(name, e.target.value)}><option value="">{required ? "Choose one" : "Leave blank"}</option>
      {options.map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select>, undefined, full);
  }
  const contents = step === "returning" ? <>
    {text("guardianEmail", "Parent or guardian email", { type: "email", autoComplete: "email", maxLength: 254, required: true })}
    {text("athleteFirstName", "Athlete’s first name", { maxLength: 80, required: true })}
  </> : step === 0 ? <>
    {text("athleteFirstName", "Athlete’s legal first name", { maxLength: 80, required: true, autoComplete: "section-athlete given-name" })}
    {text("athleteLastName", "Athlete’s legal last name", { maxLength: 80, required: true, autoComplete: "section-athlete family-name" })}
    {field("dateOfBirth", "Date of birth", <div className={styles.dateControl} data-empty={!draft.dateOfBirth}>
      <input {...props("dateOfBirth", true)} type="date" required autoComplete="section-athlete bday"
        onChange={e => update("dateOfBirth", e.target.value)} />
      <span aria-hidden="true">Choose a date</span>
    </div>, `${registrationAgeLabel} on the first practice date. Eligibility is checked when you submit.`)}
    {select("schoolGrade", "Current school grade", grades)}
    {field("schoolDistrict", "School district", <>
      <input {...props("schoolDistrict")} maxLength={120} required onChange={e => update("schoolDistrict", e.target.value)} />
      <button type="button" className={styles.inline} onClick={() => update("schoolDistrict", "Not sure / not applicable")}>Not sure / not applicable</button>
    </>)}
    {select("gender", "Gender (optional)", genders, false)}
  </> : step === 1 ? <>
    {text("guardianName", "Parent or guardian name", { autoComplete: "section-guardian name", maxLength: 100, required: true }, undefined, true)}
    {text("guardianEmail", "Parent or guardian email", { type: "email", autoComplete: "section-guardian email", maxLength: 254, required: true })}
    {text("guardianPhone", "Parent or guardian phone", { type: "tel", autoComplete: "section-guardian tel", maxLength: 32, required: true })}
    <details className={styles.optional}><summary>Add a second guardian (optional)</summary><div className={styles.grid}>
      {text("secondaryGuardianName", "Second guardian name (optional)", { autoComplete: "section-secondary name", maxLength: 100 }, undefined, true)}
      {text("secondaryGuardianEmail", "Second guardian email (optional)", { type: "email", autoComplete: "section-secondary email", maxLength: 254 })}
      {text("secondaryGuardianPhone", "Second guardian phone (optional)", { type: "tel", autoComplete: "section-secondary tel", maxLength: 32 })}
    </div></details>
  </> : step === 2 ? <>
    {text("streetAddress", "Street address", { autoComplete: "address-line1", maxLength: 160, required: true }, undefined, true)}
    {text("addressLine2", "Address line 2 (optional)", { autoComplete: "address-line2", maxLength: 120 }, undefined, true)}
    {text("city", "City", { autoComplete: "address-level2", maxLength: 80, required: true }, undefined, true)}
    {select("state", "State", states, true, "address-level1")}
    {text("postalCode", "ZIP code", { autoComplete: "postal-code", maxLength: 20, required: true })}
  </> : step === 3 ? <>
    {text("emergencyContactName", "Emergency contact name", { autoComplete: "section-emergency name", maxLength: 100, required: true }, undefined, true)}
    {text("emergencyContactPhone", "Emergency contact phone", { type: "tel", autoComplete: "section-emergency tel", maxLength: 32, required: true })}
    {text("emergencyContactRelationship", "Relationship to athlete", { maxLength: 80, required: true })}
  </> : <>
    {schedule && <p className={styles.schedule}><strong>Practice schedule</strong> {schedule}</p>}
    {select("availability", "Can your athlete attend practices?", choices, true, undefined, true)}
    {text("experience", "Track experience (optional)", { maxLength: 250 }, "A short answer is enough. Please do not enter medical or other sensitive information.", true)}
    {select("priorClub", "Previously trained or competed with another track club?", choices, true, undefined, true)}
    {draft.priorClub === "Yes" && <>
      {text("clubName", "Current or most recent club", { maxLength: 120, required: true }, undefined, true)}
      <fieldset className={styles.bodies} aria-describedby={prefix + "-affiliations-hint"}>
        <legend>Club affiliations</legend><p className={styles.hint} id={prefix + "-affiliations-hint"}>Choose all that apply, or choose Not sure.</p>
        <div>{affiliationOptions.map(value => <label key={value}><input type="checkbox" name="affiliations" value={value}
          checked={draft.affiliations?.includes(value) ?? false} aria-invalid={!!errors.affiliations}
          aria-describedby={errors.affiliations ? prefix + "-affiliations-error" : undefined}
          onChange={e => {
            const old = draft.affiliations ?? [];
            update("affiliations", !e.target.checked ? old.filter(a => a !== value)
              : value === "Unsure" ? ["Unsure"] : [...old.filter(a => a !== "Unsure" && a !== value), value]);
          }} />{value === "Unsure" ? "Not sure" : value}</label>)}</div>
        {errors.affiliations && <p id={prefix + "-affiliations-error"} className={styles.error}>{errors.affiliations[0]}</p>}
      </fieldset>
      {text("lastMeetMonth", "Last meet month and year (optional)", { type: "month", placeholder: "YYYY-MM" }, "If a date picker isn’t available, enter YYYY-MM.")}
    </>}
  </>;
  return <fieldset disabled={disabled} className={styles.grid}>
    <legend className="sr-only">{step === "returning" ? "Returning athlete check-in" : ["Athlete details", "Guardian", "Household address", "Emergency contact", "Track background"][step]}</legend>
    {contents}
  </fieldset>;
}

export function RegistrationReview({ index, draft: d }: { index: number; draft: Draft }) {
  const yn = (value?: string) => value === "Unsure" ? "Not sure" : value;
  const dob = d.dateOfBirth ? new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric", timeZone: "UTC" })
    .format(new Date(d.dateOfBirth + "T00:00:00Z")) : "";
  const rows: [string, string | undefined][] = index === 0 ? [
    ["Legal name", [d.athleteFirstName, d.athleteLastName].filter(Boolean).join(" ")],
    ["Date of birth", dob], ["Grade", grades.find(([k]) => k === d.schoolGrade)?.[1]],
    ["District", d.schoolDistrict], ["Gender", d.gender]
  ] : index === 1 ? [
    ["Guardian", d.guardianName], ["Email", d.guardianEmail], ["Phone", d.guardianPhone],
    ["Second guardian", d.secondaryGuardianName], ["Second email", d.secondaryGuardianEmail], ["Second phone", d.secondaryGuardianPhone]
  ] : index === 2 ? [["Address", [d.streetAddress, d.addressLine2, d.city, [d.state, d.postalCode].filter(Boolean).join(" ")].filter(Boolean).join(", ")]]
  : index === 3 ? [["Name", d.emergencyContactName], ["Relationship", d.emergencyContactRelationship], ["Phone", d.emergencyContactPhone]]
  : [["Practice", yn(d.availability)], ["Experience", d.experience], ["Prior club", yn(d.priorClub)],
    ["Club", d.priorClub === "Yes" ? d.clubName : ""], ["Affiliations", d.priorClub === "Yes" ? d.affiliations?.map(yn).join(", ") : ""],
    ["Last meet", d.priorClub === "Yes" ? d.lastMeetMonth : ""]];
  return <dl>{rows.filter(([, value]) => value).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl>;
}
