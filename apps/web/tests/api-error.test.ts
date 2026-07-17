import { describe, expect, it } from "vitest";
import { safeProblem } from "../lib/admin/api-error";

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
});
