import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

describe("mobile interior page typography", () => {
  it("keeps long hero and section headings inside narrow viewports", () => {
    const styles = readFileSync("app/globals.css", "utf8");

    expect(styles).toContain("font-size: clamp(2.25rem, 11vw, 4.2rem)");
    expect(styles).toContain("overflow-wrap: normal");
    expect(styles).toContain("word-break: normal");
    expect(styles).toContain("font-size: clamp(1.75rem, 9vw, 2.5rem)");
  });
});
