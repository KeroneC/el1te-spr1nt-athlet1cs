import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { RegistrationWorkspace, type RegistrationOverview } from "@/components/admin/registration-workspace";
import { applicationReviewName, RegistrationReviewDetails, type RegistrationReviewDetailsRecord } from "@/components/admin/registration-review-details";

const overview: RegistrationOverview = { seasons: [], counts: [], closeoutEligibleYears: [] };
const currentSeason: RegistrationOverview["seasons"][number] = {
  year: 2027, revision: "test-revision", title: "Test season", openingDate: "2026-11-27", endDate: "2027-06-30",
  firstPracticeDate: "2027-02-06", practiceLocation: "Test track", practiceSchedule: "Saturdays", fees: "None",
  replyWindow: "Within a week", paperworkEmail: "paperwork@example.test", paperworkHandoff: "At practice",
  reviewOwner: "Staff member", retentionOwner: "Staff member", isOpen: false, purgedAtUtc: null,
  availability: { publicState: "Closed", openingBlockedReason: null }
};
const completeApplication: RegistrationReviewDetailsRecord = {
  kind: "Application", athleteFirstName: "Casey", athleteLastName: "Test", dateOfBirth: "2021-02-06",
  schoolGrade: "K", schoolDistrict: "Example district", gender: "Prefer not to answer", guardianName: "Jordan Test",
  guardianEmail: "guardian@example.test", guardianPhone: "+15550101010", streetAddress: "15 Test Lane",
  addressLine2: "Unit 2", city: "Sampletown", state: "PA", postalCode: "12345", secondaryGuardianName: "Sam Test",
  secondaryGuardianEmail: "second@example.test", secondaryGuardianPhone: "+15550101011",
  emergencyContactName: "Avery Test", emergencyContactPhone: "+15550101012", emergencyContactRelationship: "Aunt",
  availability: "Yes", experience: "School track", priorClub: "No"
};

describe("registration staff workspace", () => {
  it("explains when first practice is required without asking for age groups", () => {
    const markup = renderToStaticMarkup(<RegistrationWorkspace initial={overview} canCloseout={true} />);
    expect(markup).toContain("First practice date (Eastern)");
    expect(markup).toContain('aria-describedby="firstPracticeDate-hint"');
    expect(markup).toMatch(/<input(?=[^>]*\bname="firstPracticeDate")(?=[^>]*\btype="date")[^>]*>/);
    expect(markup).toContain("Required before opening.");
    expect(markup).not.toContain('name="ageGroups"');
  });

  it("omits age filters and expanded fields from the review queue", () => {
    const markup = renderToStaticMarkup(<RegistrationWorkspace initial={{ ...overview, seasons: [currentSeason] }} canCloseout={false} />);
    expect(markup).toContain("New Applications");
    expect(markup).toContain("Search name, email, or reference");
    expect(markup).toContain("Transfer review");
    expect(markup).not.toContain("Age group");
    expect(markup).not.toMatch(/Date of birth|School district|Household address|Emergency contact/);
  });

  it("renders the expanded values only in the opened application detail", () => {
    const markup = renderToStaticMarkup(<RegistrationReviewDetails detail={completeApplication} />);
    expect(applicationReviewName(completeApplication)).toBe("Casey Test");
    for (const text of ["Casey Test", "2021-02-06", "Example district", "Prefer not to answer", "+15550101010", "15 Test Lane",
      "Unit 2", "Sampletown, PA 12345", "second@example.test", "Avery Test", "+15550101012", "Aunt", "School track"]) {
      expect(markup).toContain(text);
    }
    expect(markup).not.toContain("Age group (legacy)");
    const returning = { ...completeApplication, kind: "Returning" } as const;
    expect(applicationReviewName(returning)).toBe("Casey");
    const returningMarkup = renderToStaticMarkup(<RegistrationReviewDetails detail={returning} />);
    expect(returningMarkup).toContain("guardian@example.test");
    expect(returningMarkup).not.toMatch(/Casey Test|2021-02-06|Example district|15 Test Lane|second@example\.test|Avery Test/);
  });

  it("keeps the historical age group readable without inventing fields that were not collected", () => {
    const markup = renderToStaticMarkup(<RegistrationReviewDetails detail={{ kind: "Application", athleteFirstName: "Older",
      guardianEmail: "guardian@example.test", ageGroup: "12U" }} />);
    expect(markup).toContain("Age group (legacy)");
    expect(markup).toContain("12U");
    expect(markup).toContain("Not collected on this record.");
    expect(markup).not.toMatch(/Date of birth|Legal name|Household address/);
  });
});
