import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test("the Coaches page has four accessible, circular Support Staff portraits in a responsive grid", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/coaches");

  const staff = page.getByRole("region", { name: "Support Staff" });
  await expect(staff.locator("figcaption")).toHaveText(["Nickay Creary", "Michelle Newton", "Denise Gibbs", "Valerie Flores"]);
  await expect(staff.getByRole("listitem")).toHaveCount(4);

  const portraits = staff.locator("img");
  await expect(portraits).toHaveCount(4);
  for (const portrait of await portraits.all()) {
    await portrait.scrollIntoViewIfNeeded();
    await expect(portrait).toHaveCSS("border-radius", "50%");
    await expect(portrait).toHaveAttribute("alt", "");
    await expect.poll(() => portrait.evaluate((image) => (image as HTMLImageElement).naturalWidth)).toBe(384);
  }

  const desktop = await portraits.evaluateAll((images) => images.map((image) => image.getBoundingClientRect().top));
  expect(new Set(desktop).size).toBe(1);

  await page.setViewportSize({ width: 640, height: 844 });
  const tablet = await portraits.evaluateAll((images) => images.map((image) => image.getBoundingClientRect().top));
  expect(tablet[0]).toBe(tablet[1]);
  expect(tablet[1]).toBeLessThan(tablet[2]);
  expect(tablet[2]).toBe(tablet[3]);

  for (const width of [390, 340]) {
    await page.setViewportSize({ width, height: 844 });
    const mobile = await portraits.evaluateAll((images) => images.map((image) => image.getBoundingClientRect().top));
    expect(mobile[0]).toBeLessThan(mobile[1]);
    expect(mobile[1]).toBeLessThan(mobile[2]);
    expect(mobile[2]).toBeLessThan(mobile[3]);
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
  }

  const results = await new AxeBuilder({ page }).include("section[aria-labelledby='support-staff-title']").analyze();
  expect(results.violations).toEqual([]);
});
