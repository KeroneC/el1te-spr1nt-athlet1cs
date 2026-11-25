import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { GET, PUT } from "@/app/api/admin/historical-photos/[id]/[action]/route";

const cookie = vi.hoisted(() => ({ value: "session-for-test" as string | undefined }));
vi.mock("next/headers", () => ({
  cookies: async () => ({ get: () => cookie.value ? { value: cookie.value } : undefined })
}));
vi.mock("@/lib/admin/server-api", () => ({ ADMIN_SESSION_COOKIE: "el1te_admin_session" }));

const id = "11111111-2222-4333-8444-555555555555";
const url = (action: string) => `https://example.test/api/admin/historical-photos/${id}/${action}`;
const context = (action: string) => ({ params: Promise.resolve({ id, action }) });

describe("historical photo proxy", () => {
  const upstream = vi.fn<typeof fetch>();
  beforeEach(() => {
    cookie.value = "session-for-test";
    vi.stubEnv("LEGACY_MEDIA_ARCHIVE_ENABLED", "true");
    vi.stubEnv("API_BASE_URL", "https://backend.example.test");
    vi.stubGlobal("fetch", upstream);
    upstream.mockReset();
  });
  afterEach(() => { vi.unstubAllEnvs(); vi.unstubAllGlobals(); });

  it("accepts Azure's boolean casing but remains disabled for False or unknown values", async () => {
    cookie.value = undefined;
    vi.stubEnv("LEGACY_MEDIA_ARCHIVE_ENABLED", "True");
    expect((await GET(new Request(url("preview")), context("preview"))).status).toBe(401);
    for (const value of ["False", "enabled", ""]) {
      vi.stubEnv("LEGACY_MEDIA_ARCHIVE_ENABLED", value);
      expect((await GET(new Request(url("preview")), context("preview"))).status).toBe(404);
    }
    expect(upstream).not.toHaveBeenCalled();
  });

  it("rejects disabled, unauthenticated, and original-file requests without fetching", async () => {
    vi.stubEnv("LEGACY_MEDIA_ARCHIVE_ENABLED", "");
    expect((await GET(new Request(url("preview")), context("preview"))).status).toBe(404);
    vi.stubEnv("LEGACY_MEDIA_ARCHIVE_ENABLED", "true");
    expect((await GET(new Request(url("original")), context("original"))).status).toBe(404);
    cookie.value = undefined;
    expect((await GET(new Request(url("preview")), context("preview"))).status).toBe(401);
    expect(upstream).not.toHaveBeenCalled();
  });

  it("preserves the upstream role and approval denials and drops their bodies", async () => {
    upstream.mockResolvedValueOnce(new Response("detail", { status: 403 }));
    upstream.mockResolvedValueOnce(new Response("detail", { status: 404 }));
    const forbidden = await GET(new Request(url("preview")), context("preview"));
    expect(forbidden.status).toBe(403);
    expect(await forbidden.text()).toBe("");
    const unapproved = await GET(new Request(url("download")), context("download"));
    expect(unapproved.status).toBe(404);
    expect(unapproved.headers.get("cache-control")).toBe("private, no-store");
  });

  it("forwards approved JPEG bytes privately as an attachment without upstream headers", async () => {
    const bytes = new Uint8Array([255, 216, 255, 217]);
    upstream.mockResolvedValue(new Response(bytes, { headers: { "Content-Type": "image/jpeg", "Cache-Control": "public", "X-Private": "drop" } }));
    const response = await GET(new Request(url("download")), context("download"));
    expect(upstream).toHaveBeenCalledWith(`https://backend.example.test/api/admin/historical-photos/${id}/download`,
      expect.objectContaining({ cache: "no-store", redirect: "error", headers: { Authorization: "Bearer session-for-test" } }));
    expect(response.headers.get("cache-control")).toBe("private, no-store");
    expect(response.headers.get("content-disposition")).toContain("attachment");
    expect(response.headers.get("x-private")).toBeNull();
    expect(new Uint8Array(await response.arrayBuffer())).toEqual(bytes);
  });

  it("only sends small same-site JSON review requests to the API", async () => {
    const body = JSON.stringify({ review: "ApprovedForSocial", permissionConfirmed: true });
    function request(text: string, site: string) {
      return new Request(url("review"), { method: "PUT", body: text,
        headers: { "Content-Type": "application/json", "Sec-Fetch-Site": site } });
    }
    expect((await PUT(request(body, "cross-site"), context("review"))).status).toBe(400);
    expect((await PUT(request("x".repeat(1025), "same-origin"), context("review"))).status).toBe(413);
    upstream.mockResolvedValue(new Response(null, { status: 204 }));
    expect((await PUT(request(body, "same-origin"), context("review"))).status).toBe(204);
    expect(upstream).toHaveBeenCalledOnce();
    expect(upstream).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({ method: "PUT", body }));
  });
});
