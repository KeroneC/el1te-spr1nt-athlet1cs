import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import { storeNavigationMode } from "../lib/public/deployment";

describe("store launch flow cleanup", () => {
  it("uses an explicit and fail-safe navigation mode", () => {
    expect(storeNavigationMode("internal")).toBe("internal");
    expect(storeNavigationMode(" INTERNAL ")).toBe("internal");
    expect(storeNavigationMode("external")).toBe("external");
    expect(storeNavigationMode(undefined)).toBe("external");
    expect(storeNavigationMode("unexpected")).toBe("external");

    const layout = readFileSync("app/(public)/layout.tsx", "utf8");
    expect(layout).toContain("process.env.STORE_NAVIGATION_MODE");
    expect(layout).not.toContain("isStoreEnabled()");
  });

  it("uses an accessible cancellation dialog instead of a browser confirmation", () => {
    const source = readFileSync("components/store/store-order-status.tsx", "utf8");
    expect(source).toContain("cancellationDialog.current?.showModal()");
    expect(source).toContain("Canceling and refunding…");
    expect(source).toContain("this page will update automatically");
    expect(source).not.toContain('confirm("Cancel the complete order');
  });

  it("shows the replacement tracking link once and supports copying it", () => {
    const source = readFileSync("components/admin/store-order-actions.tsx", "utf8");
    expect(source).toContain("result?.trackingUrl");
    expect(source).toContain("navigator.clipboard.writeText(trackingUrl)");
    expect(source).toContain("The replacement is shown only here");
  });
});
