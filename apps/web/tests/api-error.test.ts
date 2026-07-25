import { describe, expect, it, vi } from "vitest";
import { AdminApiError, safeProblem } from "../lib/admin/api-error";
import { adminErrorResponse } from "../lib/admin/error-response";

describe("safe API errors", () => {
  it("preserves validation fields without exposing backend detail", () => {
    const error = safeProblem(400, { detail: "SQL internals", errors: { Title: ["Title is required."] } });
    expect(error.message).toBe("Please correct the highlighted fields.");
    expect(error.fieldErrors.Title).toEqual(["Title is required."]);
    expect(error.message).not.toContain("SQL");
  });

  it("shows controlled conflict details needed to resolve guarded changes", () => {
    expect(safeProblem(409, { detail: "The final active SuperAdmin cannot be deactivated." }).message)
      .toBe("The final active SuperAdmin cannot be deactivated.");
  });

  it("maps session and permission failures safely", () => {
    expect(safeProblem(401, {}).message).toContain("session");
    expect(safeProblem(403, {}).message).toContain("permission");
    expect(safeProblem(409, {}).message).toContain("conflicts");
  });

  it("keeps only valid references on unexpected failures", () => {
    expect(safeProblem(500, { referenceId: "ESA-0123456789ABCDEF" }).referenceId)
      .toBe("ESA-0123456789ABCDEF");
    expect(safeProblem(500, { referenceId: "attacker supplied" }).referenceId).toBeNull();
    expect(safeProblem(409, { referenceId: "ESA-0123456789ABCDEF" }).referenceId).toBeNull();
  });

  it("preserves valid upstream references in Next.js proxy responses", async () => {
    const response = adminErrorResponse(new AdminApiError(
      503,
      "The admin service is temporarily unavailable.",
      {},
      "ESA-0123456789ABCDEF"
    ));
    expect(response.headers.get("X-Reference-Id")).toBe("ESA-0123456789ABCDEF");
    expect((await response.json()).referenceId).toBe("ESA-0123456789ABCDEF");
  });

  it("creates a reference when an unexpected proxy error has none", async () => {
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    const response = adminErrorResponse(new AdminApiError(502, "The response was invalid."));
    const body = await response.json();
    expect(body.referenceId).toMatch(/^ESA-[0-9A-F]{16}$/);
    expect(response.headers.get("X-Reference-Id")).toBe(body.referenceId);
  });
});
