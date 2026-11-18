import { expect, it } from "vitest";
import { isLocalRegistrationArchivePreview } from "@/lib/local-registration-preview.mjs";

it("enables archive photographs only for the opt-in loopback development fixture", () => {
  const local: NodeJS.ProcessEnv = { NODE_ENV: "development", STARTING_LINE_ARCHIVE_PREVIEW: "true",
    SITE_URL: "http://127.0.0.1:3013", API_BASE_URL: "http://127.0.0.1:5130" };
  expect(isLocalRegistrationArchivePreview(local)).toBe(true);
  const disabled: Partial<NodeJS.ProcessEnv>[] = [{ NODE_ENV: "production" }, { STARTING_LINE_ARCHIVE_PREVIEW: "false" },
    { SITE_URL: "https://example.test" }, { API_BASE_URL: "https://example.test" }];
  for (const override of disabled)
    expect(isLocalRegistrationArchivePreview({ ...local, ...override })).toBe(false);
});
