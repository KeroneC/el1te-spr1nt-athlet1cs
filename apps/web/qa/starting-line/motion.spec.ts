import { expect, test } from "@playwright/test";

test("the diagonal animates while labels, hit targets and form content stay still", async ({ page, browserName }, testInfo) => {
  test.skip(browserName !== "chromium", "Uses desktop hover and Chromium’s layout instability observer.");
  await page.emulateMedia({ reducedMotion: "no-preference" });
  await page.goto("/registration");
  const entrance = page.locator("#registration-paths");
  const apply = entrance.getByRole("link", { name: "Apply for a spot" });
  const returning = entrance.getByRole("link", { name: "Returning athlete? Check in" });
  await expect(returning).toBeVisible();
  await page.evaluate(() => document.fonts.ready);
  await entrance.evaluate(async el => { await Promise.allSettled(el.getAnimations({ subtree: true }).map(a => a.finished)); });
  const before = [await apply.boundingBox(), await returning.boundingBox(), await returning.locator("strong").boundingBox()];
  const seam = entrance.locator("svg").filter({ has: page.locator('path[vector-effect],path[pathLength="1"]') }).first();
  const transform = () => seam.evaluate(el => getComputedStyle(el).transform);
  const initial = await transform();
  await apply.hover();
  await expect.poll(transform).not.toEqual(initial);
  expect([await apply.boundingBox(), await returning.boundingBox(), await returning.locator("strong").boundingBox()]).toEqual(before);
  await page.evaluate(() => {
    // Start after the document is ready; measure the motion, not the dev loading shell.
    type Shift = PerformanceEntry & { hadRecentInput: boolean; value: number };
    let unexpected = 0;
    const motionShifts = new PerformanceObserver(list => { for (const row of list.getEntries() as Shift[]) if (!row.hadRecentInput) unexpected += row.value; });
    motionShifts.observe({ type: "layout-shift" });
    Object.assign(window, { motionMeasurement: () => {
      for (const row of motionShifts.takeRecords() as Shift[]) if (!row.hadRecentInput) unexpected += row.value;
      motionShifts.disconnect(); return unexpected;
    } });
  });
  await apply.click();
  const wizard = page.locator("#apply");
  await expect(wizard.getByRole("heading", { name: "Who’s your athlete?" })).toBeVisible();
  const heading = wizard.locator("[data-step-heading]");
  const firstRect = await heading.boundingBox();
  await wizard.evaluate(async el => { await Promise.allSettled(el.getAnimations({ subtree: true }).map(a => a.finished)); });
  expect(await heading.boundingBox()).toEqual(firstRect);
  const cls = await page.evaluate(() => (window as unknown as { motionMeasurement: () => number }).motionMeasurement());
  testInfo.annotations.push({ type: "transition CLS", description: String(cls) });
  expect(cls).toBe(0);
});
