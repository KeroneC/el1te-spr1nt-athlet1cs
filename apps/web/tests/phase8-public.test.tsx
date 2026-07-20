import { afterEach, describe, expect, it, vi } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { POST as submitContact } from "../app/api/public/contact/route";
import { hasUsableSponsorLogo, SponsorLogoTile } from "../components/public/sponsor-logo-tile";
import { SponsorTierSection } from "../components/public/sponsor-tier-section";
import { PUBLIC_REVALIDATE_SECONDS, fallbackSettings, publicApiFetch } from "../lib/public/client";
import { CONTENT_KEYS, contentByKey } from "../lib/public/content";
import { robotsForEnvironment } from "../lib/public/deployment";
import { HEADER_NAV_ITEMS, HALL_OF_FAME_INDUCTEES, PRIMARY_NAV_LINKS, prioritizeSponsorPreviews, sponsorTierClass } from "../lib/public/site";
import { SCHOLARSHIP_COPY } from "../lib/public/scholarship";
import type { Sponsor } from "../lib/public/types";
import { buildAnnouncementQuery, buildEventQuery, validateContact } from "../lib/public/validation";

afterEach(() => vi.restoreAllMocks());

function json(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { "Content-Type": "application/json" } });
}

describe("Phase 8 public CMS helpers", () => {
  it("keeps the public demo out of search indexes", () => {
    expect(robotsForEnvironment("demo")).toEqual({ index: false, follow: false, nocache: true });
    expect(robotsForEnvironment("production")).toBeUndefined();
  });

  it("keeps secondary public pages discoverable without crowding the header", () => {
    expect(HEADER_NAV_ITEMS.map((item) => item.label)).toEqual(["Club", "Events", "Gallery", "Sponsors", "Resources", "Contact"]);
    expect(PRIMARY_NAV_LINKS.map((link) => link.label)).toEqual([
      "About", "Programs", "Coaches", "Team", "Hall of Fame", "Events", "Gallery", "Sponsors", "Forms", "Scholarship", "FAQs", "Contact"
    ]);
  });

  it("prioritizes Gold sponsor logos without changing tier policy", () => {
    const sponsor = (name: string, tier: Sponsor["tier"], logoUrl: string | null, displayOrder: number): Sponsor => ({ name, slug: name.toLowerCase(), tier, logoUrl, websiteUrl: null, description: null, displayOrder });
    const result = prioritizeSponsorPreviews([
      sponsor("Community", "Community", "/community.png", 1),
      sponsor("Gold text", "Gold", null, 1),
      sponsor("Silver", "Silver", "/silver.png", 1),
      sponsor("Gold logo", "Gold", "/gold.png", 2)
    ]);

    expect(result.map((item) => item.name)).toEqual(["Gold logo", "Gold text", "Community", "Silver"]);
    expect(sponsorTierClass("Other")).toBe("sponsor-tier-other");
  });

  it("renders linked sponsor logos as safe, accessible external links", () => {
    const sponsor: Sponsor = { name: "Example Partner", slug: "example-partner", tier: "Gold", logoUrl: "/logo.png", websiteUrl: "https://example.com/", description: null, displayOrder: 1 };
    const markup = renderToStaticMarkup(createElement(SponsorLogoTile, { sponsor }));

    expect(markup).toContain('aria-label="Visit Example Partner website (opens in a new tab)"');
    expect(markup).toContain('target="_blank"');
    expect(markup).toContain('rel="noreferrer noopener"');
    expect(markup).toContain('src="/logo.png"');
    expect(markup).toContain('alt=""');
  });

  it("keeps unlinked logos named and falls back to visible sponsor text", () => {
    const unlinked: Sponsor = { name: "The Beecher Family", slug: "beecher-family", tier: "Community", logoUrl: "/beecher.png", websiteUrl: null, description: null, displayOrder: 1 };
    const missing: Sponsor = { ...unlinked, name: "Logo Pending", slug: "logo-pending", logoUrl: null };

    expect(renderToStaticMarkup(createElement(SponsorLogoTile, { sponsor: unlinked }))).toContain('alt="The Beecher Family logo"');
    expect(renderToStaticMarkup(createElement(SponsorLogoTile, { sponsor: missing }))).toContain("Logo Pending");
    expect(hasUsableSponsorLogo("/logo.png", false)).toBe(true);
    expect(hasUsableSponsorLogo("/logo.png", true)).toBe(false);
  });

  it("renders a centered sponsor tier with stagger order", () => {
    const sponsors: Sponsor[] = [
      { name: "First Gold", slug: "first-gold", tier: "Gold", logoUrl: "/first.png", websiteUrl: "https://example.com/first", description: null, displayOrder: 1 },
      { name: "Second Gold", slug: "second-gold", tier: "Gold", logoUrl: "/second.png", websiteUrl: null, description: null, displayOrder: 2 }
    ];
    const markup = renderToStaticMarkup(createElement(SponsorTierSection, { sponsors, tier: "Gold" }));

    expect(markup).toContain("sponsor-tier-band sponsor-tier-gold");
    expect(markup).toContain("Gold Sponsors");
    expect(markup).not.toContain("sponsor-tier-kicker");
    expect(markup).toContain("--sponsor-reveal-index:1");
  });

  it("keeps Hall of Fame records ready for future profile routes", () => {
    expect(HALL_OF_FAME_INDUCTEES).toHaveLength(2);
    expect(HALL_OF_FAME_INDUCTEES.every((inductee) => inductee.slug && inductee.imageSrc.startsWith("/images/hall-of-fame/"))).toBe(true);
  });

  it("preserves the approved BVN memorial wording", () => {
    expect(SCHOLARSHIP_COPY.introduction).toContain("Beulah Veronica Newton");
    expect(SCHOLARSHIP_COPY.legacy).toContain("Miss Beulah");
    expect(SCHOLARSHIP_COPY.award).toContain("Click the logo or use the button below");
  });

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
