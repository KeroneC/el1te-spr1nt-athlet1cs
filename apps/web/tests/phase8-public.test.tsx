import { afterEach, describe, expect, it, vi } from "vitest";
import { POST as submitContact } from "../app/api/public/contact/route";
import { PUBLIC_REVALIDATE_SECONDS, fallbackSettings, publicApiFetch } from "../lib/public/client";
import { CONTENT_KEYS, contentByKey } from "../lib/public/content";
import { buildAnnouncementQuery, buildEventQuery, validateContact } from "../lib/public/validation";

afterEach(() => vi.restoreAllMocks());

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { "Content-Type": "application/json" } });
}

describe("Phase 8 public CMS helpers", () => {
  it("looks up content by exact key", () => {
    const blocks = contentByKey([{ key: CONTENT_KEYS.homeHero, title: "Run", summary: null, body: "Fast", imageUrl: null, ctaText: null, ctaUrl: null, displayOrder: 1 }]);
    expect(blocks.get("home.hero")?.title).toBe("Run");
    expect(blocks.get("HOME.HERO")).toBeUndefined();
  });

  it("builds announcement pagination parameters", () => {
    expect(buildAnnouncementQuery(2, true)).toBe("page=2&pageSize=9&featured=true");
  });

  it("builds event filters with backend enum names", () => {
    expect(buildEventQuery(3, "Meet", false)).toBe("page=3&pageSize=9&upcomingOnly=false&eventType=Meet");
  });

  it("uses short public revalidation without authorization", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(json({ ok: true }));
    await publicApiFetch<{ ok: boolean }>("/site-settings");
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/public/site-settings"), {
      next: { revalidate: PUBLIC_REVALIDATE_SECONDS }
    });
    expect(fetchMock.mock.calls[0][1]).not.toHaveProperty("headers.Authorization");
  });

  it("provides a complete safe settings fallback", () => {
    expect(fallbackSettings).toMatchObject({ clubName: expect.any(String), primaryCtaUrl: "/registration" });
    expect(fallbackSettings.logoUrl).toBeNull();
  });
});

describe("Phase 8 contact flow", () => {
  it("validates required fields and email usability", () => {
    const errors = validateContact({ name: "", email: "invalid", phone: null, inquiryType: "General", message: "" });
    expect(errors).toMatchObject({ name: expect.any(String), email: expect.any(String), message: expect.any(String) });
  });

  it("submits the exact public contact contract without caching", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(json({ id: "1", message: "Received" }, 201));
    const payload = { name: "A Parent", email: "parent@test.invalid", phone: null, inquiryType: "Parent", message: "Hello" };
    const response = await submitContact(new Request("http://localhost/api/public/contact", { method: "POST", body: JSON.stringify(payload) }));
    expect(response.status).toBe(201);
    expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/public/contact-submissions"), expect.objectContaining({ method: "POST", cache: "no-store", body: JSON.stringify(payload) }));
  });

  it("preserves backend field errors without leaking internal details", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(json({ detail: "database detail", errors: { Email: ["Email is invalid."] } }, 400));
    const response = await submitContact(new Request("http://localhost/api/public/contact", { method: "POST", body: JSON.stringify({}) }));
    const result = await response.json();
    expect(response.status).toBe(400);
    expect(result.errors.Email).toEqual(["Email is invalid."]);
    expect(JSON.stringify(result)).not.toContain("database");
  });

  it("returns a retryable safe error when the API is unavailable", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("connection internals"));
    const response = await submitContact(new Request("http://localhost/api/public/contact", { method: "POST", body: JSON.stringify({}) }));
    expect(response.status).toBe(503);
    expect(await response.text()).not.toContain("internals");
  });
});
