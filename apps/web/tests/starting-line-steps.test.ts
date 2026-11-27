import { describe, expect, it } from "vitest";
import { applicationFields, applicationSteps, firstIncompleteStep, initialDraft, reviewStep, stepFromHash, stepForField, stepHash, validateStep, type Draft } from "@/components/public/starting-line-steps";
import { otherSchoolDistrict, pittsburghSchoolDistricts, unknownSchoolDistrict } from "@/lib/pittsburgh-school-districts";

const today = "2026-11-17";
const complete: Draft = {
  athleteFirstName: "Sample", athleteLastName: "Runner", dateOfBirth: "2020-02-28",
  schoolGrade: "K", schoolDistrict: unknownSchoolDistrict, gender: "Female", guardianName: "Sample Guardian",
  guardianEmail: "family@example.test", guardianPhone: "(202) 555-0110",
  streetAddress: "100 Sample St", city: "Sample City", state: "PA", postalCode: "00000",
  emergencyContactName: "Test Relative", emergencyContactPhone: "202-555-0111",
  emergencyContactRelationship: "Relative", priorClub: "No"
};

describe("starting-line steps", () => {
  it("only recognizes fixed navigational fragments, never family data", () => {
    expect(applicationSteps.map((_, i) => stepFromHash(stepHash(i)))).toEqual([0, 1, 2, 3, 4, 5]);
    for (const fragment of ["#returning", "#apply/unknown", "#apply/review?email=family@example.test"])
      expect(stepFromHash(fragment)).toBe(-1);
  });

  it("cannot skip ahead of the first incomplete or invalid section", () => {
    expect(firstIncompleteStep({}, today)).toBe(0);
    expect(firstIncompleteStep({ ...complete, guardianPhone: "" }, today)).toBe(1);
    expect(firstIncompleteStep({ ...complete, postalCode: " " }, today)).toBe(2);
    expect(firstIncompleteStep({ ...complete, emergencyContactPhone: "call me" }, today)).toBe(3);
    expect(firstIncompleteStep({ ...complete, priorClub: "Yes" }, today)).toBe(4);
    expect(firstIncompleteStep(complete, today)).toBe(reviewStep);
  });

  it("validates birth dates without guessing the staff-only eligibility cutoff", () => {
    const birthday = (dob: string) => validateStep(0, { ...complete, dateOfBirth: dob }, today).dateOfBirth;
    expect(birthday("2020-02-29")).toBeUndefined();
    expect(birthday("2020-03-01")).toBeUndefined(); // Eligibility is enforced by the API.
    expect(birthday("2021-02-29")).toBeDefined();
    expect(birthday("2026-11-18")).toBeDefined();
  });

  it("keeps returning validation independent of application requirements", () => {
    const checkin = { guardianEmail: "parent@example.test", athleteFirstName: "Sample" };
    expect(validateStep("returning", checkin, today)).toEqual({});
    expect(Object.keys(validateStep("returning", {}, today)).sort()).toEqual(["athleteFirstName", "guardianEmail"]);
  });

  it("requires one of the two application gender choices", () => {
    for (const gender of [undefined, "", "Prefer not to answer", "Other"])
      expect(validateStep(0, { ...complete, gender }, today).gender).toEqual(["Choose Female or Male."]);
    for (const gender of ["Female", "Male"])
      expect(validateStep(0, { ...complete, gender }, today).gender).toBeUndefined();
  });

  it("defaults new applications to PA, while keeping other states selectable and returning drafts empty", () => {
    expect(initialDraft(true)).toEqual({ state: "PA" });
    expect(initialDraft(false)).toEqual({});
    expect(applicationFields({ ...complete, state: "OH" }).state).toBe("OH");
  });

  it("lists local districts once and only sends the district actually chosen or entered", () => {
    expect(new Set(pittsburghSchoolDistricts).size).toBe(pittsburghSchoolDistricts.length);
    expect(pittsburghSchoolDistricts).toEqual(expect.arrayContaining([
      "Cornell", "West Allegheny", "Pittsburgh", "Armstrong", "Beaver Area", "Butler Area",
      "Uniontown Area", "New Castle Area", "Canon-McMillan", "Greensburg Salem"
    ]));
    expect(validateStep(0, { ...complete, schoolDistrict: "" }, today).schoolDistrict).toBeDefined();
    const other = { ...complete, schoolDistrict: otherSchoolDistrict };
    expect(validateStep(0, { ...other, schoolDistrictOther: "  " }, today).schoolDistrict).toBeDefined();
    const typed = { ...other, schoolDistrictOther: "  Sample District  " };
    expect(validateStep(0, typed, today).schoolDistrict).toBeUndefined();
    const submitted = applicationFields(typed);
    expect(submitted.schoolDistrict).toBe("Sample District");
    expect(submitted).not.toHaveProperty("schoolDistrictOther");
    expect(applicationFields({ ...typed, schoolDistrict: "Cornell" }).schoolDistrict).toBe("Cornell");
    expect(applicationFields({ ...typed, schoolDistrict: unknownSchoolDistrict }).schoolDistrict).toBe(unknownSchoolDistrict);
  });

  it("only includes applicable prior-club answers in the final application", () => {
    const detail = { ...complete, clubName: "Example track", affiliations: ["AAU", "USATF"], lastMeetMonth: "2026-10" };
    expect(applicationFields({ ...detail, priorClub: "Yes" })).toMatchObject({
      clubName: "Example track", affiliations: ["AAU", "USATF"], lastMeetMonth: "2026-10"
    });
    for (const priorClub of ["No", "Unsure"])
      expect(applicationFields({ ...detail, priorClub })).toMatchObject({ clubName: null, affiliations: [], lastMeetMonth: null });
    expect(applicationFields(complete)).not.toHaveProperty("availability");
    expect(stepForField("secondaryGuardianEmail")).toBe(1);
    expect(stepForField("affiliations")).toBe(4);
  });

  it("requires a valid past or current meet month only when the previous club answer is Yes", () => {
    const prior = { ...complete, priorClub: "Yes", clubName: "Example track", affiliations: ["AAU"] };
    expect(validateStep(4, prior, today).lastMeetMonth).toEqual(["Enter the last meet month and year."]);
    for (const lastMeetMonth of ["2026-12", "2026-13", "November 2026"])
      expect(validateStep(4, { ...prior, lastMeetMonth }, today).lastMeetMonth).toBeDefined();
    for (const lastMeetMonth of ["2026-11", "2026-03"])
      expect(validateStep(4, { ...prior, lastMeetMonth }, today)).toEqual({});
    for (const priorClub of ["No", "Unsure"])
      expect(validateStep(4, { ...prior, priorClub }, today)).toEqual({});
  });
});
