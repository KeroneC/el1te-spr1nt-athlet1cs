import { createElement } from "react";
import { readFileSync, statSync } from "node:fs";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import {
  ALL_AMERICAN_SLIDES,
  HomeAllAmericanShowcase,
  shouldAutoplay,
  showcaseReducer,
  visibleSlideIndexes
} from "../components/public/home-all-american-showcase";

describe("homepage All-American showcase", () => {
  it("advances automatically and wraps to the first photograph", () => {
    let state = { activeIndex: 7, userPaused: false };
    state = showcaseReducer(state, { type: "tick", slideCount: 8 });
    expect(state).toEqual({ activeIndex: 0, userPaused: false });
  });

  it("pauses after manual navigation and supports previous/next wraparound", () => {
    expect(showcaseReducer({ activeIndex: 0, userPaused: false }, { type: "previous", slideCount: 8 }))
      .toEqual({ activeIndex: 7, userPaused: true });
    expect(showcaseReducer({ activeIndex: 7, userPaused: false }, { type: "next", slideCount: 8 }))
      .toEqual({ activeIndex: 0, userPaused: true });
    expect(showcaseReducer({ activeIndex: 2, userPaused: true }, { type: "tick", slideCount: 8 }))
      .toEqual({ activeIndex: 2, userPaused: true });
  });

  it("resumes only after an explicit play action", () => {
    expect(showcaseReducer({ activeIndex: 3, userPaused: true }, { type: "play" }))
      .toEqual({ activeIndex: 3, userPaused: false });
    expect(showcaseReducer({ activeIndex: 3, userPaused: false }, { type: "pause" }))
      .toEqual({ activeIndex: 3, userPaused: true });
  });

  it("stops autoplay for hover, focus, hidden pages, and reduced motion", () => {
    const base = { userPaused: false, hovered: false, focusWithin: false, pageVisible: true, reducedMotion: false };
    expect(shouldAutoplay(base)).toBe(true);
    expect(shouldAutoplay({ ...base, hovered: true })).toBe(false);
    expect(shouldAutoplay({ ...base, focusWithin: true })).toBe(false);
    expect(shouldAutoplay({ ...base, pageVisible: false })).toBe(false);
    expect(shouldAutoplay({ ...base, reducedMotion: true })).toBe(false);
  });

  it("renders only the active and adjacent photographs with accessible controls", () => {
    expect(visibleSlideIndexes(0, 8)).toEqual([7, 0, 1]);
    expect(ALL_AMERICAN_SLIDES).toHaveLength(8);

    const markup = renderToStaticMarkup(createElement(HomeAllAmericanShowcase));
    expect(markup.match(/<figure/g)).toHaveLength(3);
    expect(markup).toContain("2026 AAU Junior Olympic Games");
    expect(markup).toContain("9</strong> All-Americans");
    expect(markup).toContain("11 All-American performances");
    expect(markup).toContain('aria-label="Show previous photograph"');
    expect(markup).toContain('aria-label="Pause photograph showcase"');
    expect(markup).toContain('aria-label="Show next photograph"');
    expect(markup).toContain('aria-hidden="true"');
    expect(markup).not.toContain("All-American, event");
  });

  it("ships both responsive WebP sizes within the mobile transfer budget", () => {
    const paths = ALL_AMERICAN_SLIDES.flatMap(slide => [
      `public${slide.src}-480.webp`,
      `public${slide.src}-960.webp`
    ]);
    for (const path of paths) expect(statSync(path).size).toBeGreaterThan(0);

    const largestThreeMobileImages = ALL_AMERICAN_SLIDES
      .map(slide => statSync(`public${slide.src}-480.webp`).size)
      .sort((left, right) => right - left)
      .slice(0, 3)
      .reduce((total, size) => total + size, 0);
    expect(largestThreeMobileImages).toBeLessThanOrEqual(1_500_000);
  });

  it("enables the draft on demo while keeping production disabled", () => {
    const demoWorkflow = readFileSync("../../.github/workflows/deploy-azure.yml", "utf8");
    const productionWorkflow = readFileSync("../../.github/workflows/deploy-production.yml", "utf8");
    const infrastructure = readFileSync("../../infra/main.bicep", "utf8");
    const homepage = readFileSync("app/(public)/page.tsx", "utf8");

    expect(demoWorkflow.match(/homeAllAmericanShowcaseEnabled=true/g)).toHaveLength(2);
    expect(productionWorkflow.match(/homeAllAmericanShowcaseEnabled=false/g)).toHaveLength(2);
    expect(infrastructure).toContain("param homeAllAmericanShowcaseEnabled bool = false");
    expect(homepage).toContain('export const dynamic = "force-dynamic"');
    expect(homepage).toContain("process.env.HOME_ALL_AMERICAN_SHOWCASE_ENABLED");
  });
});
