import { renderToStaticMarkup } from "react-dom/server";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CoachesPage from "@/app/(public)/coaches/page";
import { getCoaches, getContentBlock, PublicApiError } from "@/lib/public/client";
import type { ContentBlock } from "@/lib/public/types";

vi.mock("@/lib/public/client", async (importOriginal) => ({
  ...await importOriginal<typeof import("@/lib/public/client")>(),
  getCoaches: vi.fn(),
  getContentBlock: vi.fn()
}));

const photo = "https://api.example.test/media/11111111-1111-1111-1111-111111111111";
const block: ContentBlock = {
  key: "coaches.support-staff",
  title: "Support Staff",
  body: "Meet the volunteers who help keep the club running.",
  summary: "Club volunteers standing together at the track",
  imageUrl: photo,
  ctaText: null,
  ctaUrl: null,
  displayOrder: 0
};

describe("Coaches support staff section", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(getCoaches).mockResolvedValue([{
      firstName: "Test", lastName: "Coach", role: "Coach", imageUrl: null, email: null, bio: "Keep this off the page", displayOrder: 0
    }]);
  });

  it("places published copy and the uncropped, lazy responsive group photo below the coaches", async () => {
    vi.mocked(getContentBlock).mockResolvedValue(block);
    const html = renderToStaticMarkup(await CoachesPage());
    expect(getContentBlock).toHaveBeenCalledWith("coaches.support-staff");
    expect(html.indexOf("Test Coach")).toBeLessThan(html.indexOf("Support Staff"));
    expect(html).toContain(block.body);
    expect(html).toContain('alt="Club volunteers standing together at the track"');
    expect(html).toContain("object-contain");
    expect(html).toContain('loading="lazy"');
    expect(html).toContain(`${photo}?width=1600 1600w`);
    expect(html).not.toContain("Keep this off the page");
  });

  it.each([404, 503])("keeps the coaches page intact when the optional content endpoint returns %s", async (status) => {
    vi.mocked(getContentBlock).mockRejectedValue(new PublicApiError(status));
    const html = renderToStaticMarkup(await CoachesPage());
    expect(html).toContain("Test Coach");
    expect(html).not.toContain("support-staff-heading");
    expect(html).not.toContain("We could not load this content");
  });

  it("does not render a partial section with no group photo", async () => {
    vi.mocked(getContentBlock).mockResolvedValue({ ...block, imageUrl: null });
    const html = renderToStaticMarkup(await CoachesPage());
    expect(html).toContain("Test Coach");
    expect(html).not.toContain("Support Staff");
  });
});
