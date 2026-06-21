import { describe, expect, it } from "vitest";
import { buildAnnouncementQuery, getAnnouncementState, isAdminRole, validateAnnouncement, validateLoginInput } from "../lib/admin/validation";

describe("admin authentication rules", () => {
  it("requires a valid email and password", () => {
    expect(validateLoginInput({ email: "bad", password: "" })).toEqual({
      email: ["Enter a valid email address."], password: ["Password is required."]
    });
  });

  it("allows only administrative roles", () => {
    expect(isAdminRole("Admin")).toBe(true);
    expect(isAdminRole("SuperAdmin")).toBe(true);
    expect(isAdminRole("Parent")).toBe(false);
  });
});

describe("announcement behavior", () => {
  it("builds API filters and bounds invalid pages", () => {
    expect(buildAnnouncementQuery({ search: " registration ", isPublished: "true", isFeatured: "false", includeExpired: "true", page: "-3" }))
      .toBe("search=registration&isPublished=true&isFeatured=false&includeExpired=true&page=1&pageSize=20");
  });

  it("validates required content, URL, and date ordering", () => {
    const errors = validateAnnouncement({
      title: "", summary: "", body: "", imageUrl: "javascript:alert(1)", isFeatured: false,
      isPublished: true, publishDateUtc: "2026-06-21T18:00:00Z", expirationDateUtc: "2026-06-21T17:00:00Z"
    });
    expect(Object.keys(errors)).toEqual(expect.arrayContaining(["title", "summary", "body", "imageUrl", "expirationDateUtc"]));
  });

  it.each([
    [{ isPublished: false, publishDateUtc: null, expirationDateUtc: null }, "Draft"],
    [{ isPublished: true, publishDateUtc: "2026-06-22T00:00:00Z", expirationDateUtc: null }, "Scheduled"],
    [{ isPublished: true, publishDateUtc: "2026-06-20T00:00:00Z", expirationDateUtc: null }, "Published"],
    [{ isPublished: true, publishDateUtc: "2026-06-19T00:00:00Z", expirationDateUtc: "2026-06-20T00:00:00Z" }, "Expired"]
  ] as const)("derives lifecycle state %#", (announcement, expected) => {
    expect(getAnnouncementState(announcement, new Date("2026-06-21T00:00:00Z"))).toBe(expected);
  });
});
