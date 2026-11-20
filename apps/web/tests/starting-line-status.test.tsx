import { afterEach, describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import RegistrationPage from "@/app/(public)/registration/page";
import { getRegistrationSeason } from "@/lib/public/registration";

vi.mock("@/lib/public/registration", () => ({ getRegistrationSeason: vi.fn() }));
afterEach(() => { vi.useRealTimers(); vi.clearAllMocks(); });

async function render() {
  return renderToStaticMarkup(await RegistrationPage({ searchParams: Promise.resolve({ src: "instagram" }) }));
}
function informationalOnly(html: string) {
  expect(html).not.toMatch(/href="#(?:apply|returning)"/);
  expect(html).not.toMatch(/<form[\s>]/);
  for (const route of ["/faqs", "/contact", "/forms"])
    expect(html).toContain(`href="${route}"`);
  expect(html).not.toContain("Season at a glance");
  expect(html).toMatch(/ages 7–18/i);
}

describe("starting line when applications cannot be submitted", () => {
  it("keeps an outage distinct from a closed season and provides a retry", async () => {
    vi.mocked(getRegistrationSeason).mockResolvedValue({ state: "unavailable", season: null });
    const html = await render();
    informationalOnly(html);
    expect(html).toContain("Registration is temporarily unavailable");
    expect(html).toContain('href="/registration?src=instagram"');
    expect(html).not.toContain("Planned opening:");
  });
  it.each([
    ["2026-11-17T12:00:00Z", "Friday, November 27"],
    ["2026-11-27T05:00:00Z", "Registration is scheduled to open today"],
    ["2026-11-28T12:00:00Z", "Applications are currently closed"]
  ])("shows accurate non-actionable status on %s", async (now, text) => {
    vi.useFakeTimers(); vi.setSystemTime(new Date(now));
    vi.mocked(getRegistrationSeason).mockResolvedValue({ state: "no-season", season: null });
    const html = await render();
    informationalOnly(html);
    expect(html).toContain(text);
    if (!text.includes("closed")) {
      expect(html).toContain("<summary>How to prepare</summary>");
      expect(html).not.toMatch(/<img\b|__registration-preview/);
      expect(html.includes("Check registration status")).toBe(text.includes("today"));
    }
  });
  it("does not promise another opening after staff close on opening day", async () => {
    vi.useFakeTimers(); vi.setSystemTime(new Date("2026-11-27T17:00:00Z"));
    vi.mocked(getRegistrationSeason).mockResolvedValue({ state: "loaded", season: {
      year: 2027, title: "Previously opened", openingDate: "2026-11-27", firstPracticeDate: null,
      isOpen: false, practiceLocation: null, practiceSchedule: null, fees: null, replyWindow: null
    } });
    const html = await render();
    informationalOnly(html);
    expect(html).toContain("Applications are currently closed");
    expect(html).not.toContain("scheduled to open today");
  });
});
