// Pure presentation/validation helpers. Drafts belong to the mounted form only.
export const applicationSteps = [
  { slug: "", label: "Athlete", title: "Who’s your athlete?", note: "Have their school information, home address, and an emergency contact handy." },
  { slug: "guardian", label: "Guardian", title: "How can we reach you?", note: "Tell us which parent or guardian we should contact about this application." },
  { slug: "household", label: "Household", title: "Where does your athlete live?", note: "Use the athlete’s current household address." },
  { slug: "emergency", label: "Emergency contact", title: "Who should we call in an emergency?", note: "Ask this person’s permission before sharing their details." },
  { slug: "track", label: "Track background", title: "Track background", note: "Tell us about your athlete’s track experience and any previous club." },
  { slug: "review", label: "Review", title: "Ready to send?", note: "Check the details below. Nothing has been submitted yet." }
] as const;

export const stepFields = [
  ["athleteFirstName", "athleteLastName", "dateOfBirth", "schoolGrade", "schoolDistrict", "gender"],
  ["guardianName", "guardianEmail", "guardianPhone", "secondaryGuardianName", "secondaryGuardianEmail", "secondaryGuardianPhone"],
  ["streetAddress", "addressLine2", "city", "state", "postalCode"],
  ["emergencyContactName", "emergencyContactPhone", "emergencyContactRelationship"],
  ["experience", "priorClub", "clubName", "affiliations", "lastMeetMonth"]
] as const;

export type Field = typeof stepFields[number][number];
export type Draft = Partial<Record<Exclude<Field, "affiliations"> | "website", string>> & { affiliations?: string[] };
export type Errors = Record<string, string[]>;
export const applicationGenders = ["Female", "Male"] as const;
export function initialDraft(application: boolean): Draft {
  return application ? { state: "PA" } : {};
}
export const reviewStep = applicationSteps.length - 1;
export function stepForField(field: string) {
  return stepFields.findIndex(fields => fields.some(item => item === field));
}
export function stepHash(step: number) {
  return step === 0 ? "#apply" : "#apply/" + applicationSteps[step].slug;
}
export function stepFromHash(hash: string) {
  return applicationSteps.findIndex((_, i) => hash === stepHash(i));
}
export function easternToday() {
  const parts = new Intl.DateTimeFormat("en-US", {
    year: "numeric", month: "2-digit", day: "2-digit", timeZone: "America/New_York"
  }).formatToParts(new Date());
  const part = (name: string) => parts.find(item => item.type === name)?.value;
  return [part("year"), part("month"), part("day")].join("-");
}
function validDob(value: string, today: string) {
  const date = new Date(value + "T00:00:00Z");
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value.startsWith("0000") || isNaN(date.valueOf()) ||
      date.toISOString().slice(0, 10) !== value || value > today)
    return "Enter a valid date of birth that is not in the future.";
}

export function validateStep(step: number | "returning", draft: Draft, today: string): Errors {
  const errors: Errors = {};
  const required: Partial<Record<Field, string>> = {
    athleteFirstName: "Enter your athlete’s first name.", athleteLastName: "Enter your athlete’s last name.",
    dateOfBirth: "Enter a date of birth.", schoolGrade: "Choose a school grade or Not applicable.",
    schoolDistrict: "Enter a school district, Not sure, or Not applicable.", guardianName: "Enter a parent or guardian name.",
    guardianEmail: "Enter a parent or guardian email.", guardianPhone: "Enter a valid phone number.",
    streetAddress: "Enter a street address.", city: "Enter a city.", state: "Choose a state.", postalCode: "Enter a ZIP code.",
    emergencyContactName: "Enter an emergency contact name.", emergencyContactPhone: "Enter a valid phone number.",
    emergencyContactRelationship: "Enter the relationship to the athlete.", priorClub: "Choose Yes, No, or Not sure.",
    ...(draft.priorClub === "Yes" ? {
      clubName: "Enter the current or most recent club.", lastMeetMonth: "Enter the last meet month and year."
    } : {})
  };
  const fields: readonly Field[] = step === "returning" ? ["guardianEmail", "athleteFirstName"] : (stepFields[step] ?? []);
  for (const name of fields) {
    if (name === "affiliations") {
      if (draft.priorClub === "Yes" && !draft.affiliations?.length)
        errors[name] = ["Choose the governing bodies or select Not sure on its own."];
      continue;
    }
    const value = draft[name]?.trim() ?? "";
    let message = !value ? required[name] : undefined;
    if (name === "schoolDistrict" && value.length > 120) message = "Enter a school district name of up to 120 characters.";
    if (name === "gender" && !applicationGenders.some(gender => gender === value))
      message = "Choose Female or Male.";
    if (value && name === "dateOfBirth") message = validDob(value, today);
    if (value && ["guardianEmail", "secondaryGuardianEmail"].includes(name) && !/^[^\s@]+@[^\s@]+$/.test(value))
      message = "Enter a valid email address.";
    if (value && ["guardianPhone", "secondaryGuardianPhone", "emergencyContactPhone"].includes(name) &&
        (!/^[\d +().-]+$/.test(value) || !/^\d{7,15}$/.test(value.replace(/\D/g, ""))))
      message = "Enter a valid phone number.";
    if (name === "lastMeetMonth" && value && draft.priorClub === "Yes" &&
        (!/^\d{4}-(0[1-9]|1[0-2])$/.test(value) || value < "2000-01" || value > today.slice(0, 7)))
      message = "Enter a valid past or current month and year.";
    if (message) errors[name] = [message];
  }
  return errors;
}

export function firstIncompleteStep(draft: Draft, today: string) {
  const first = stepFields.findIndex((_, i) => Object.keys(validateStep(i, draft, today)).length > 0);
  return first < 0 ? reviewStep : first;
}

export function applicationFields(draft: Draft) {
  return Object.fromEntries(stepFields.flat().map(name => {
    if (name === "affiliations") return [name, draft.priorClub === "Yes" ? draft.affiliations ?? [] : []];
    if ((name === "clubName" || name === "lastMeetMonth") && draft.priorClub !== "Yes") return [name, null];
    return [name, draft[name]?.trim() || null];
  }));
}
