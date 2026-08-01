import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

for (const route of ["/", "/gallery", "/privacy", "/admin/login", "/admin/password-recovery", "/shop"]) {
  test(`${route} has no automatically detectable serious accessibility violations`, async ({ page }) => {
    await page.goto(route);
    const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
    expect(results.violations.filter(value => ["serious", "critical"].includes(value.impact ?? ""))).toEqual([]);
  });
}

test("SuperAdmin verification-code state is keyboard accessible", async ({ page }) => {
  await page.route("**/api/admin-session/login", route => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ requiresMfa: true }) }));
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill("superadmin@example.test");
  await page.locator("#password").fill("Valid-Password-42!");
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByLabel("Verification code")).toBeFocused();
  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa"]).analyze();
  expect(results.violations.filter(value => ["serious", "critical"].includes(value.impact ?? ""))).toEqual([]);
});
