import { expect, test } from "@playwright/test";

test("grouped public navigation exposes secondary destinations on desktop", async ({ page }) => {
  await page.goto("/");

  const navigation = page.getByRole("navigation", { name: "Primary navigation" });
  const club = navigation.getByRole("button", { name: "Club" });
  await club.click();
  await expect(club).toHaveAttribute("aria-expanded", "true");
  await expect(navigation.getByRole("link", { name: "Hall of Fame" })).toBeVisible();

  await page.keyboard.press("Escape");
  await expect(club).toHaveAttribute("aria-expanded", "false");
  await expect(club).toBeFocused();

  const resources = navigation.getByRole("button", { name: "Resources" });
  await resources.click();
  await navigation.getByRole("link", { name: "Scholarship" }).click();
  await expect(page).toHaveURL(/\/scholarship$/);
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("In honor of Beulah Veronica Newton");
  await expect(navigation.getByRole("button", { name: "Resources" })).toHaveClass(/is-active/);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
});

test("mobile grouped navigation closes after navigating", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");

  const menu = page.getByRole("button", { name: "Open navigation" });
  await menu.click();
  const navigation = page.getByRole("navigation", { name: "Primary navigation" });
  await navigation.getByRole("button", { name: "Club" }).click();
  await navigation.getByRole("link", { name: "Hall of Fame" }).click();

  await expect(page).toHaveURL(/\/hall-of-fame$/);
  await expect(page.getByRole("button", { name: "Open navigation" })).toHaveAttribute("aria-expanded", "false");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText("RGN El1te Hall of Fame");
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
});

test("about page renders the approved mission and preserves club values", async ({ page }) => {
  await page.goto("/about");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText("Our Mission");
  await expect(page.getByText(/promoting track and field for youth ages 7 to 18/i)).toBeVisible();
  await expect(page.getByRole("heading", { name: "What We Value" })).toBeVisible();
  await expect(page.getByText("Our Story", { exact: true })).toHaveCount(0);
});

test("homepage All-American showcase is responsive, controllable, and motion safe", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/");

  const showcase = page.getByRole("region", { name: "9 All-Americans" });
  await expect(showcase).toBeVisible();
  await expect(showcase.locator("figure")).toHaveCount(3);
  await expect(showcase.getByRole("img")).toHaveCount(1);
  await expect(page.getByTestId("all-american-progress")).toHaveText(/01\s*\/\s*08/);

  await showcase.getByRole("button", { name: "Show next photograph" }).click();
  await expect(page.getByTestId("all-american-progress")).toHaveText(/02\s*\/\s*08/);
  await expect(showcase.getByRole("button", { name: "Autoplay unavailable with reduced motion" })).toBeDisabled();

  for (let index = 0; index < 6; index += 1) {
    await showcase.getByRole("button", { name: "Show next photograph" }).click();
  }
  await expect(page.getByTestId("all-american-progress")).toHaveText(/08\s*\/\s*08/);
  await expect(showcase.getByRole("img", { name: /Matthew, Rocco, Kingston, and Chase/ })).toBeVisible();

  for (const width of [1024, 800, 769, 768, 390, 340]) {
    await page.setViewportSize({ width, height: 844 });
    await expect.poll(() => page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    ), { message: `homepage overflow at ${width}px` }).toBeLessThanOrEqual(0);
  }

  await page.clock.install();
  await page.clock.runFor(6_000);
  await expect(page.getByTestId("all-american-progress")).toHaveText(/08\s*\/\s*08/);
});

test("homepage showcase resumes from its controls without requiring a click elsewhere", async ({ page }) => {
  await page.clock.install();
  await page.emulateMedia({ reducedMotion: "no-preference" });
  await page.goto("/");

  const showcase = page.getByRole("region", { name: "9 All-Americans" });
  const progress = page.getByTestId("all-american-progress");
  const next = showcase.getByRole("button", { name: "Show next photograph" });
  const previous = showcase.getByRole("button", { name: "Show previous photograph" });
  const play = showcase.getByRole("button", { name: "Play photograph showcase" });
  const pause = showcase.getByRole("button", { name: "Pause photograph showcase" });
  const check = (index: string) => expect(progress).toHaveText(new RegExp(`${index}\\s*/\\s*08`));

  await pause.click();
  await expect(play).toBeFocused();
  await page.clock.pauseAt(new Date(await page.evaluate(() => Date.now()) + 250));
  await next.click();
  await check("02");
  await page.clock.runFor(6_000);
  await check("02"); // An explicit pause survives manual navigation.

  await play.click();
  await expect(pause).toBeFocused();
  await page.clock.runFor(5_000);
  await check("03"); // The pointer is still over Play.
  await next.click();
  await check("04");
  await expect(next).toBeFocused();
  await page.clock.runFor(4_999);
  await check("04");
  await page.clock.runFor(1);
  await check("05");

  await page.mouse.move(0, 0);
  await page.getByRole("navigation", { name: "Primary navigation" }).getByRole("button", { name: "Club" }).focus();
  await previous.focus();
  await page.clock.runFor(6_000);
  await check("05"); // Entering with keyboard focus pauses before any control is pressed.
  await previous.press("Enter");
  await check("04");
  await expect(previous).toBeFocused();
  await page.clock.runFor(5_000);
  await check("05");
});
