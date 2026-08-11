import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import { checkoutFieldErrors } from "../components/store/store-checkout";

describe("store checkout stabilization", () => {
  it("maps API phone validation to the checkout phone field", () => {
    expect(checkoutFieldErrors({ errors: { customerPhone: ["Enter a valid U.S. phone number."] } }))
      .toEqual({ customerPhone: "Enter a valid U.S. phone number." });
    expect(checkoutFieldErrors({ errors: { CustomerPhone: ["Provider rejected this number."] } }))
      .toEqual({ customerPhone: "Provider rejected this number." });
  });

  it("keeps consent sentences together and exposes accessible phone validation", () => {
    const source = readFileSync("components/store/store-checkout.tsx", "utf8");
    expect(source).toContain('<span>I reviewed and accept the <Link href="/store-policy"');
    expect(source).toContain('aria-invalid={fieldErrors.customerPhone ? "true" : undefined}');
    expect(source).toContain('aria-describedby={fieldErrors.customerPhone ? "checkout-phone-error" : undefined}');
    expect(source).toContain("phoneInput.current?.focus()");
  });

  it("uses a fixed checkbox column and a flexible sentence column", () => {
    const css = readFileSync("app/globals.css", "utf8");
    expect(css).toContain("grid-template-columns: 1.25rem minmax(0,1fr)");
  });
});
