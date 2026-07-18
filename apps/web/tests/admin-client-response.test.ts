import { describe, expect, it } from "vitest";
import { ADMIN_SESSION_EXPIRED_REDIRECT, getAdminResponseRedirect } from "../lib/admin/client-response";

describe("Admin client response redirects", () => {
  it("sends an expired session through logout and back to the login page", () => {
    expect(getAdminResponseRedirect(401)).toBe(ADMIN_SESSION_EXPIRED_REDIRECT);
  });

  it("preserves access-denied handling and ignores unrelated failures", () => {
    expect(getAdminResponseRedirect(403)).toBe("/admin/access-denied");
    expect(getAdminResponseRedirect(500)).toBeNull();
  });
});
