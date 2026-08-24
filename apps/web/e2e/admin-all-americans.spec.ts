import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { signInAsE2eSuperAdmin } from "./helpers/admin-auth";

async function uploadArchiveImage(page: Page, title: string) {
  await page.goto("/admin/media");
  await page.getByLabel("Images").setInputFiles("public/images/track-hero.png");
  await page.getByLabel("Title").fill(title);
  await page.getByLabel("Alt text").fill(`${title} archival photograph`);
  const response = page.waitForResponse(value => value.url().endsWith("/api/admin/media") && value.request().method() === "POST");
  await page.getByRole("button", { name: "Upload queue" }).click();
  expect((await response).status()).toBe(201);
}

test("Admin publishes a completed annual story with individual and relay medals", async ({ page }) => {
  test.setTimeout(120_000);
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  const heroTitle = `E2E Archive Hero ${suffix}`;
  const galleryTitle = `E2E Archive Gallery ${suffix}`;

  await signInAsE2eSuperAdmin(page);
  const years = await page.request.get("/api/admin/all-americans?pageSize=100");
  expect(years.ok()).toBeTruthy();
  const used = new Set(((await years.json()) as { items: Array<{ year: number }> }).items.map(item => item.year));
  const year = Array.from({ length: 11 }, (_, index) => 2090 + index).find(value => !used.has(value));
  test.skip(!year, "The reusable E2E database has no free archive year.");

  await uploadArchiveImage(page, heroTitle);
  await uploadArchiveImage(page, galleryTitle);

  await page.goto("/admin/all-americans/new");
  await page.getByLabel("Year").fill(String(year));
  await page.getByLabel("Title").fill(`${year} E2E Junior Olympic Games`);
  await page.getByLabel("Summary").fill("A disposable annual story used to verify the complete archive workflow.");
  await page.getByLabel("Verified athlete total").fill("2");
  await page.getByLabel("Verified medal total").fill("3");
  await page.getByLabel("Hero image", { exact: true }).selectOption({ label: heroTitle });
  const create = page.waitForResponse(value => value.url().endsWith("/api/admin/all-americans") && value.request().method() === "POST");
  await page.getByRole("button", { name: "Create" }).click();
  const created = await create;
  expect(created.status()).toBe(201);
  const yearId = ((await created.json()) as { id: string }).id;
  await expect(page).toHaveURL(new RegExp(`/admin/all-americans/${yearId}/edit`));

  const mediaSection = page.getByRole("heading", { name: "Annual media" }).locator("..");
  await mediaSection.getByLabel("Search media").fill(heroTitle);
  await mediaSection.getByRole("button", { name: `Add image ${heroTitle}` }).click();
  await expect(page.getByText("Position 1")).toBeVisible();
  await mediaSection.getByLabel("Search media").fill(galleryTitle);
  await mediaSection.getByRole("button", { name: `Add image ${galleryTitle}` }).click();
  await expect(page.getByText("Position 2")).toBeVisible();
  await page.getByRole("button", { name: "Move up" }).last().click();
  await expect(page.getByText("Archive updated.")).toBeVisible();

  const roster = page.getByRole("heading", { name: "Annual athlete roster" }).locator("..");
  for (const [firstName, lastName] of [["E2E Alexis", suffix], ["E2E Javon", suffix]]) {
    await roster.getByLabel("First name").first().fill(firstName);
    await roster.getByLabel("Last name").first().fill(lastName);
    await roster.getByRole("button", { name: "Add athlete" }).click();
    await expect(roster.getByText(`${firstName} ${lastName}`)).toBeVisible();
  }

  const performances = page.getByRole("heading", { name: "Verified performances" }).locator("..");
  await performances.getByLabel("Event").first().fill("Long jump");
  await performances.getByLabel(`E2E Alexis ${suffix}`).first().check();
  await performances.getByRole("button", { name: "Add performance" }).click();
  await expect(performances.getByText(/Long jump/)).toBeVisible();

  await performances.getByLabel("Event").first().fill("4x100 relay");
  await performances.getByLabel("Relay performance").first().check();
  await performances.getByLabel(`E2E Alexis ${suffix}`).first().check();
  await performances.getByLabel(`E2E Javon ${suffix}`).first().check();
  await performances.getByRole("button", { name: "Add performance" }).click();
  await expect(performances.getByText(/4x100 relay/)).toBeVisible();

  await page.getByLabel("Published").check();
  await page.getByLabel("Athlete and result details complete").check();
  const publish = page.waitForResponse(value => value.url().endsWith(`/api/admin/all-americans/${yearId}`) && value.request().method() === "PUT");
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await publish).status()).toBe(200);

  await page.goto(`/all-americans/${year}`);
  await expect(page.getByRole("heading", { name: `${year} E2E Junior Olympic Games` })).toBeVisible();
  await expect(page.getByText(`E2E Alexis ${suffix}`)).toBeVisible();
  await expect(page.getByText(`E2E Javon ${suffix}`)).toBeVisible();
  await expect(page.getByText("Long jump")).toBeVisible();
  await expect(page.getByText("4x100 relay")).toHaveCount(2);
  const accessibility = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa"]).analyze();
  expect(accessibility.violations.filter(value => ["serious", "critical"].includes(value.impact ?? ""))).toEqual([]);
});
