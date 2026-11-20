import { mkdir } from "node:fs/promises";
import path from "node:path";
import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test("upcoming poster: deep links, disclosure, lean artwork and accessible reflow", async ({ page, baseURL, browserName }) => {
  test.skip(baseURL !== "http://127.0.0.1:3014", "Requires the upcoming fixture on its dedicated port.");
  const dir = path.resolve("../../artifacts/registration-walkthrough", browserName);
  await mkdir(dir, { recursive: true });
  for (const width of [340, 390, 768, 1440]) {
    await page.setViewportSize({ width, height: width < 768 ? 844 : 900 });
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/registration#apply");
    const poster = page.getByRole("region", { name: "On your marks." });
    await expect(poster).toBeVisible();
    await expect(page.locator("form, a[href='#apply'], a[href='#returning']")).toHaveCount(0);
    await expect(poster.locator("img")).toHaveCount(0);
    await expect(poster.locator("path").last()).toHaveCSS("animation-name", "none");
    await page.evaluate(() => document.fonts.ready);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    await page.screenshot({ path: path.join(dir, `poster-${width}.png`), animations: "disabled", scale: "css" });
    const disclosure = poster.locator("summary");
    await disclosure.focus();
    await page.keyboard.press("Enter");
    await expect(poster.getByText(/No paperwork/)).toBeVisible();
    const audit = await new AxeBuilder({ page }).include("[data-registration-hub]")
      .withTags(["wcag2a", "wcag2aa", "wcag21aa"]).analyze();
    expect(audit.violations.map(item => item.id)).toEqual([]);
    await disclosure.press("Enter");
    await expect(poster.getByText(/No paperwork/)).toBeHidden();
  }
});
