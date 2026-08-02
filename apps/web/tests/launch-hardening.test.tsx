import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { ResponsiveMediaImage, isManagedMediaUrl, mediaUrlAtWidth } from "@/components/public/responsive-media-image";
import { sanitizePublicRoute } from "@/lib/observability/browser-analytics";
import { isEnabledSetting } from "@/lib/runtime-config";
import { readFileSync } from "node:fs";

describe("launch hardening frontend", () => {
  it("emits responsive derivative candidates only for managed media", () => {
    const url = "https://api.example.test/media/11111111-1111-1111-1111-111111111111";
    expect(isManagedMediaUrl(url)).toBe(true);
    expect(mediaUrlAtWidth(url, 480)).toContain("width=480");
    const html = renderToStaticMarkup(<ResponsiveMediaImage src={url} alt="Team" width={1200} height={800} />);
    expect(html).toContain("srcSet=");
    expect(html).toContain("width=1600");
    expect(html).toContain("loading=\"lazy\"");
  });

  it("preserves external images without inventing derivative URLs", () => {
    const html = renderToStaticMarkup(<ResponsiveMediaImage src="https://cdn.example.test/logo.svg" alt="Sponsor" />);
    expect(html).not.toContain("srcSet=");
    expect(html).not.toContain("?width=");
  });

  it("sanitizes dynamic routes and rejects admin or malformed paths", () => {
    expect(sanitizePublicRoute("/news/private-athlete-name?email=test@example.com")).toBe("/news/[slug]");
    expect(sanitizePublicRoute("/shop/custom-shirt#name=Alex")).toBe("/shop/[slug]");
    expect(sanitizePublicRoute("/admin/users")).toBeNull();
    expect(sanitizePublicRoute("/api/public/contact")).toBeNull();
    expect(sanitizePublicRoute("/unknown/private-record-id")).toBeNull();
  });

  it("keeps the desktop Admin login card centered", () => {
    const source = readFileSync("app/admin/(auth)/login/page.tsx", "utf8");
    expect(source).toContain("items-center justify-center");
    expect(source).not.toContain("items-center justify-end");
  });

  it("accepts Azure's title-cased enabled setting without accepting other values", () => {
    expect(isEnabledSetting("True")).toBe(true);
    expect(isEnabledSetting(" true ")).toBe(true);
    expect(isEnabledSetting("False")).toBe(false);
    expect(isEnabledSetting(undefined)).toBe(false);
  });
});
