import { describe, expect, it } from "vitest";
import { applicationFields, applicationSteps, firstIncompleteStep, reviewStep, stepFromHash, stepForField, stepHash, validateStep, type Draft } from "@/components/public/starting-line-steps";

const today = "2026-11-17";
const complete: Draft = {
  athleteFirstName: "Sample", athleteLastName: "Runner", dateOfBirth: "2020-02-28",
  schoolGrade: "K", schoolDistrict: "Not sure / not applicable", guardianName: "Sample Guardian",
  guardianEmail: "family@example.test", guardianPhone: "(202) 555-0110",
  streetAddress: "100 Sample St", city: "Sample City", state: "PA", postalCode: "00000",
  emergencyContactName: "Test Relative", emergencyContactPhone: "202-555-0111",
  emergencyContactRelationship: "Relative", availability: "Yes", priorClub: "No"
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

  it("only includes applicable prior-club answers in the final application", () => {
    const detail = { ...complete, clubName: "Example track", affiliations: ["AAU", "USATF"], lastMeetMonth: "2026-10" };
    expect(applicationFields({ ...detail, priorClub: "Yes" })).toMatchObject({
      clubName: "Example track", affiliations: ["AAU", "USATF"], lastMeetMonth: "2026-10"
    });
    for (const priorClub of ["No", "Unsure"])
      expect(applicationFields({ ...detail, priorClub })).toMatchObject({ clubName: null, affiliations: [], lastMeetMonth: null });
    expect(stepForField("secondaryGuardianEmail")).toBe(1);
    expect(stepForField("affiliations")).toBe(4);
  });
});
