import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import { isAllowedAdminMutation, isAllowedAdminRead } from "../lib/admin/mutation-policy";
import { footerLinkGroups, headerNavItems } from "../lib/public/site";

const yearId = "58000000-0000-0000-0000-000000000001";
const childId = "58000000-0000-0000-0000-000000000002";

describe("All-American archive", () => {
  it("gates public navigation", () => {
    expect(JSON.stringify(headerNavItems(false))).not.toContain("All-Americans");
    expect(JSON.stringify(footerLinkGroups(false))).not.toContain("All-Americans");
    expect(JSON.stringify(headerNavItems(true))).toContain("All-Americans");
    expect(JSON.stringify(footerLinkGroups(true))).toContain("All-Americans");
  });

  it("allows only the intended Admin archive routes", () => {
    expect(isAllowedAdminRead(["all-americans"])).toBe(true);
    expect(isAllowedAdminRead(["all-americans", yearId])).toBe(true);
    expect(isAllowedAdminMutation(["all-americans"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", yearId], "PUT")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", yearId, "media"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", yearId, "media", "order"], "PUT")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", yearId, "recipients", childId], "DELETE")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", yearId, "performances", childId], "PUT")).toBe(true);
    expect(isAllowedAdminMutation(["all-americans", "not-an-id", "media"], "POST")).toBe(false);
  });

  it("hides incomplete athlete details while preserving annual summary and photography", () => {
    const detailPage = readFileSync("app/(public)/all-americans/[year]/page.tsx", "utf8");
    expect(detailPage).toContain("record.detailsComplete &&");
    expect(detailPage).toContain("record.images.map");
    expect(detailPage).toContain("record.summary");
  });
});
