import { afterEach, expect, it, vi } from "vitest";
import { getRegistrationSeason } from "@/lib/public/registration";

vi.mock("server-only", () => ({}));
afterEach(() => vi.unstubAllGlobals());

it.each([{}, { firstPracticeDate: "2027-02-06", paperworkEmail: "private@example.test" }])(
  "only passes public season data to the page, including during a mixed-version deploy (%j)",
  async (legacy) => {
    const season = {
      year: 2027, title: "Indoor season", openingDate: "2026-11-27", isOpen: true,
      practiceLocation: "Sample track", practiceSchedule: "Details to follow", fees: "See club",
      replyWindow: "One week"
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json({ ...season, ...legacy })));
    expect(await getRegistrationSeason()).toEqual({ state: "loaded", season });
  }
);
