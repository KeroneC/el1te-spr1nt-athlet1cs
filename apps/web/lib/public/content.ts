import type { ContentBlock } from "./types";

export const CONTENT_KEYS = {
  homeHero: "home.hero",
  homeMission: "home.mission",
  homePrograms: "home.programs",
  aboutStory: "about.story",
  aboutValues: "about.values",
  registrationIntro: "registration.intro",
  sponsorsIntro: "sponsors.intro"
} as const;

export function contentByKey(blocks: ContentBlock[]): Map<string, ContentBlock> {
  return new Map(blocks.map((block) => [block.key, block]));
}
