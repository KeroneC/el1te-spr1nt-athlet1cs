export type RegistrationReviewDetailsRecord = {
  kind: "Application" | "Returning";
  athleteFirstName: string;
  athleteLastName?: string | null;
  dateOfBirth?: string | null;
  schoolGrade?: string | null;
  schoolDistrict?: string | null;
  gender?: string | null;
  ageGroup?: string | null;
  guardianName?: string | null;
  guardianEmail: string;
  guardianPhone?: string | null;
  streetAddress?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  secondaryGuardianName?: string | null;
  secondaryGuardianEmail?: string | null;
  secondaryGuardianPhone?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  availability?: string | null;
  experience?: string | null;
  priorClub?: string | null;
  clubName?: string | null;
  affiliations?: string | null;
  lastMeetMonth?: string | null;
};

type DetailFields = ReadonlyArray<readonly [label: string, value: string | null | undefined]>;

function Fields({ fields }: { fields: DetailFields }) {
  const populated = fields.filter(([, value]) => Boolean(value));
  return <dl className="grid min-w-0 gap-3 sm:grid-cols-2">{populated.map(([label, value]) =>
    <div className="min-w-0" key={label}>
      <dt className="font-bold">{label}</dt>
      <dd className="whitespace-pre-line [overflow-wrap:anywhere]">{value}</dd>
    </div>
  )}</dl>;
}

function DetailsSection({ heading, fields }: { heading: string; fields: DetailFields }) {
  return <section className="min-w-0 space-y-2 border-t border-slate-200 pt-3">
    <h3 className="text-base font-black text-track-ink">{heading}</h3>
    {fields.some(([, value]) => Boolean(value)) ? <Fields fields={fields} /> : <p className="text-sm text-slate-600">Not collected on this record.</p>}
  </section>;
}

export function applicationReviewName(detail: RegistrationReviewDetailsRecord) {
  return detail.kind === "Application" && detail.athleteLastName
    ? `${detail.athleteFirstName} ${detail.athleteLastName}` : detail.athleteFirstName;
}

// This component is rendered only after the protected POST detail request succeeds.
// Do not reuse it in queue rows or pass detail values to list/search requests.
export function RegistrationReviewDetails({ detail }: { detail: RegistrationReviewDetailsRecord }) {
  if (detail.kind === "Returning") return <Fields fields={[
    ["Guardian", detail.guardianName], ["Email", detail.guardianEmail], ["Phone", detail.guardianPhone]
  ]} />;

  const locality = [detail.city, [detail.state, detail.postalCode].filter(Boolean).join(" ")].filter(Boolean).join(", ");
  const address = [detail.streetAddress, detail.addressLine2, locality].filter(Boolean).join("\n");
  return <>
    <DetailsSection heading="Athlete details" fields={[
      [detail.athleteLastName ? "Legal name" : "First name", applicationReviewName(detail)], ["Date of birth", detail.dateOfBirth],
      ["School grade", detail.schoolGrade], ["School district", detail.schoolDistrict],
      ["Gender", detail.gender], ["Age group (legacy)", detail.ageGroup]
    ]} />
    <DetailsSection heading="Guardian and household" fields={[
      ["Primary guardian", detail.guardianName], ["Primary guardian email", detail.guardianEmail],
      ["Primary guardian phone", detail.guardianPhone], ["Household address", address],
      ["Second guardian", detail.secondaryGuardianName], ["Second guardian email", detail.secondaryGuardianEmail],
      ["Second guardian phone", detail.secondaryGuardianPhone]
    ]} />
    <DetailsSection heading="Emergency contact" fields={[
      ["Emergency contact name", detail.emergencyContactName], ["Emergency contact phone", detail.emergencyContactPhone],
      ["Relationship to athlete", detail.emergencyContactRelationship]
    ]} />
    <DetailsSection heading="Track background" fields={[
      ["Practice availability", detail.availability], ["Experience", detail.experience],
      ["Previous club", detail.clubName ?? detail.priorClub],
      ["Affiliations", detail.affiliations?.replaceAll(",", ", ")], ["Last meet", detail.lastMeetMonth]
    ]} />
  </>;
}
