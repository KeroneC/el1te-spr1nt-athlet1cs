import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { POST } from "@/app/api/public/registration/[action]/route";
import { RegistrationIntake } from "@/components/public/registration-intake";
import { isAllowedAdminMutation, isAllowedAdminRead } from "@/lib/admin/mutation-policy";
import { registrationChannel, type PublicRegistrationSeason } from "@/lib/registration";

const season: PublicRegistrationSeason = {
  year: 2027, title: "Test season", openingDate: "2026-11-27", ageGroups: ["12U"], isOpen: true,
  practiceLocation: "Track", practiceSchedule: "Saturdays", fees: "None for test", replyWindow: "Within one week."
};
const response = (status: number, body: object) => new Response(JSON.stringify(body), { status });
const call = (action: string, body: object, site = "same-origin") => POST(new Request("https://site.test/api/public/registration/" + action, {
  method: "POST", headers: { "Content-Type": "application/json", "Sec-Fetch-Site": site }, body: JSON.stringify(body)
}), { params: Promise.resolve({ action }) });

describe("registration intake", () => {
  const upstream = vi.fn<typeof fetch>();
  beforeEach(() => { upstream.mockReset(); vi.stubGlobal("fetch", upstream); });
  afterEach(() => vi.unstubAllGlobals());

  it("renders a short returning check-in, distinct from the new application", () => {
    const returning = renderToStaticMarkup(<RegistrationIntake season={season} kind="Returning" channel="direct" />);
    const application = renderToStaticMarkup(<RegistrationIntake season={season} kind="Application" channel="facebook" />);
    expect(returning).toContain("Athlete’s first name");
    expect(returning).not.toContain('name="guardianPhone"');
    expect(returning).not.toContain('name="ageGroup"');
    expect(returning).not.toContain('type="checkbox"');
    expect(application).toContain("another track and field club");
    expect(application).toContain("12U");
    expect(application).not.toContain('type="file"');
  });

  it("passes the retry key and returns only a non-lookup receipt", async () => {
    upstream.mockResolvedValue(response(201, { reference: "EL-0123456789ABCDEF", extra: "never expose" }));
    const result = await call("applications", { requestKey: "retry-key", channel: "anything" });
    expect(result.status).toBe(201);
    expect(result.headers.get("cache-control")).toBe("no-store");
    expect(await result.json()).toEqual({ reference: "EL-0123456789ABCDEF" });
    expect(JSON.parse(String(upstream.mock.calls[0][1]?.body))).toEqual({ requestKey: "retry-key", channel: "direct" });
  });

  it("bounds request size, rejects cross-site forms and exposes no lookup action", async () => {
    expect((await call("applications", { big: "x".repeat(4096) })).status).toBe(413);
    expect((await call("applications", {}, "cross-site")).status).toBe(400);
    expect((await call("lookup", {})).status).toBe(404);
    expect(upstream).not.toHaveBeenCalled();
  });

  it("keeps closed-intake and rate-limit errors actionable", async () => {
    upstream.mockResolvedValueOnce(response(409, { title: "Applications are not open." }));
    upstream.mockResolvedValueOnce(response(429, {}));
    const result = await call("applications", {});
    expect(result.status).toBe(409);
    expect(await result.json()).toMatchObject({ message: expect.stringContaining("not open") });
    expect((await call("applications", {})).status).toBe(429);
  });

  it("restricts campaign labels and keeps search and detail off URL-based proxy routes", () => {
    expect(["facebook", "instagram", "fbclid-secret", undefined].map(registrationChannel))
      .toEqual(["facebook", "instagram", "direct", "direct"]);
    expect(isAllowedAdminRead(["registration", "detail"])).toBe(false);
    expect(isAllowedAdminMutation(["registration", "detail"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["registration", "closeout"], "DELETE")).toBe(false);
  });
});
