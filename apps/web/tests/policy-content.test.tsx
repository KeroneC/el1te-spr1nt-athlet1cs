import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { isApprovedPolicyHref, parsePolicyBody, PolicyContent } from "@/components/public/policy-content";

describe("policy content", () => {
  it("parses the supported safe block syntax", () => {
    expect(parsePolicyBody("Last updated: today\n\n## Details\n\n- One\n- Two")).toEqual([
      { kind: "paragraph", text: "Last updated: today" },
      { kind: "heading", text: "Details" },
      { kind: "list", items: ["One", "Two"] }
    ]);
  });

  it("renders approved links without rendering raw HTML", () => {
    const html = renderToStaticMarkup(<PolicyContent body={'## Contact\n\nEmail [the club](mailto:club@example.test). <script>alert("no")</script>'} />);
    expect(html).toContain('href="mailto:club@example.test"');
    expect(html).toContain("&lt;script&gt;");
    expect(html).not.toContain("<script>");
  });

  it("rejects unsafe link schemes", () => {
    expect(isApprovedPolicyHref("javascript:alert(1)")).toBe(false);
    expect(isApprovedPolicyHref("data:text/html,bad")).toBe(false);
    expect(isApprovedPolicyHref("https://squareup.com")).toBe(true);
    expect(isApprovedPolicyHref("/store-policy")).toBe(true);
  });
});
