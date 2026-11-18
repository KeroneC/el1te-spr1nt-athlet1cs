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
  for (const route of ["/faqs", "/contact", "/forms/athlete-registration-form.pdf", "#registration-season-heading"])
    expect(html).toContain(`href="${route}"`);
  expect(html).toContain("Ages 6 and up");
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
    ["2026-11-17T12:00:00Z", "Applications open soon"],
    ["2026-11-28T12:00:00Z", "Applications are currently closed"]
  ])("shows accurate non-actionable status on %s", async (now, text) => {
    vi.useFakeTimers(); vi.setSystemTime(new Date(now));
    vi.mocked(getRegistrationSeason).mockResolvedValue({ state: "no-season", season: null });
    const html = await render();
    informationalOnly(html);
    expect(html).toContain(text);
  });
});
