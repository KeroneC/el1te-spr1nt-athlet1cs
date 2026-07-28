import { describe, expect, it } from "vitest";
import { publicSiteUrl } from "../lib/public-site-url";

describe("publicSiteUrl", () => {
  it("uses the configured public site instead of an Azure container origin", () => {
    const destination = publicSiteUrl(
      "/admin/login?reason=expired",
      new URL("https://2b03f90088e0:8080/api/admin-session/logout?reason=expired"),
      "https://el1tesprint-demo-neauu2-web.azurewebsites.net"
    );

    expect(destination.href).toBe(
      "https://el1tesprint-demo-neauu2-web.azurewebsites.net/admin/login?reason=expired"
    );
  });

  it("uses the request origin for local development", () => {
    const destination = publicSiteUrl(
      "/admin/login",
      new URL("http://localhost:3000/api/admin-session/logout"),
      undefined
    );

    expect(destination.href).toBe("http://localhost:3000/admin/login");
  });

  it("does not accept a non-web configured URL", () => {
    const destination = publicSiteUrl(
      "/admin/login",
      new URL("http://localhost:3000/api/admin-session/logout"),
      "javascript:alert(1)"
    );

    expect(destination.href).toBe("http://localhost:3000/admin/login");
  });
});
