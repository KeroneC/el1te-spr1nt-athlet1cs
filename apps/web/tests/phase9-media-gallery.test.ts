import { describe, expect, it } from "vitest";
import { isAllowedAdminMutation } from "../lib/admin/mutation-policy";
import { sizedPublicMediaUrl } from "../lib/public/media";

const id = "6d825db4-5d30-4eb6-8dd0-828dfa8ce0ac";
const mediaId = "db79b0ef-7682-49ea-8d03-d365f0961f2a";

describe("Phase 9 protected mutation policy", () => {
  it("allows media uploads and metadata lifecycle operations", () => {
    expect(isAllowedAdminMutation(["media"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["media", id], "PUT")).toBe(true);
    expect(isAllowedAdminMutation(["media", id], "DELETE")).toBe(true);
  });

  it("allows album media assignment and accessible ordering", () => {
    expect(isAllowedAdminMutation(["gallery-albums", id, "media"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["gallery-albums", id, "media", mediaId], "PUT")).toBe(true);
    expect(isAllowedAdminMutation(["gallery-albums", id, "media", "order"], "PUT")).toBe(true);
  });

  it("rejects malformed identifiers and unrelated nested routes", () => {
    expect(isAllowedAdminMutation(["media", ".."], "DELETE")).toBe(false);
    expect(isAllowedAdminMutation(["gallery-albums", id, "publish"], "POST")).toBe(false);
  });
});

describe("public media display variants", () => {
  it("adds an approved width to API media URLs", () => {
    expect(sizedPublicMediaUrl(`https://api.example.test/media/${mediaId}`, 1200))
      .toBe(`https://api.example.test/media/${mediaId}?width=1200`);
  });

  it("leaves administrator-provided external images unchanged", () => {
    expect(sizedPublicMediaUrl("https://images.example.test/team.jpg", 800))
      .toBe("https://images.example.test/team.jpg");
  });
});
