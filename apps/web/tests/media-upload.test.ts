import { describe, expect, it } from "vitest";
import { MAX_MEDIA_FILE_SIZE, runWithConcurrency, titleFromFileName, validateMediaFile } from "../lib/admin/media-upload";

describe("media upload queue helpers", () => {
  it("turns common file names into editable titles", () => {
    expect(titleFromFileName("regional-meet_finish-line.webp")).toBe("regional meet finish line");
    expect(titleFromFileName("photo.jpg")).toBe("photo");
  });

  it("rejects unsupported and oversized files before upload", () => {
    expect(validateMediaFile({ type: "image/jpeg", size: MAX_MEDIA_FILE_SIZE })).toBeNull();
    expect(validateMediaFile({ type: "image/gif", size: 100 })).toContain("JPEG");
    expect(validateMediaFile({ type: "image/png", size: MAX_MEDIA_FILE_SIZE + 1 })).toContain("10 MB");
  });

  it("limits parallel queue work without dropping items", async () => {
    let active = 0;
    let maximum = 0;
    const completed: number[] = [];
    await runWithConcurrency([0, 1, 2, 3, 4, 5], 3, async item => {
      active++;
      maximum = Math.max(maximum, active);
      await new Promise(resolve => setTimeout(resolve, 2));
      completed.push(item);
      active--;
    });
    expect(maximum).toBe(3);
    expect(completed.sort()).toEqual([0, 1, 2, 3, 4, 5]);
  });
});
