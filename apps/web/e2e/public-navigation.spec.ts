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
