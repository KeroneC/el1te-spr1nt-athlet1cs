import { renderToStaticMarkup } from "react-dom/server";
import { createElement } from "react";
import { describe, expect, it } from "vitest";
import { SupportReference } from "../components/shared/support-reference";
import {
  supportReferenceFromDigest,
  validSupportReference
} from "../lib/observability/support-reference";

describe("support references", () => {
  it("accepts only the public reference format", () => {
    expect(validSupportReference("esa-0123456789abcdef")).toBe("ESA-0123456789ABCDEF");
    expect(validSupportReference("ESA-0123456789ABCDE")).toBeNull();
    expect(validSupportReference("<script>")).toBeNull();
  });

  it("sanitizes the Next.js digest fallback", () => {
    expect(supportReferenceFromDigest("918273645")).toBe("WEB-918273645");
    expect(supportReferenceFromDigest("bad digest<script>")).toBe("WEB-baddigestscript");
    expect(supportReferenceFromDigest(undefined)).toBeNull();
  });

  it("renders reference text and an accessible copy action", () => {
    const markup = renderToStaticMarkup(createElement(SupportReference, { referenceId: "ESA-0123456789ABCDEF" }));
    expect(markup).toContain("Reference:");
    expect(markup).toContain("ESA-0123456789ABCDEF");
    expect(markup).toContain("Copy support reference ESA-0123456789ABCDEF");
    expect(markup).toContain('aria-live="polite"');
  });

  it("renders nothing without a reference", () => {
    expect(renderToStaticMarkup(createElement(SupportReference, { referenceId: null }))).toBe("");
  });
});
