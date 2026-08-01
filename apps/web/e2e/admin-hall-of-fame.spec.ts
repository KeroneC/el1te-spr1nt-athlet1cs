import { expect, test } from "@playwright/test";
import { signInAsE2eSuperAdmin } from "./helpers/admin-auth";

test("Admin can draft, publish, edit, and deactivate a Hall of Fame inductee", async ({ page }) => {
  test.setTimeout(75_000);
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const mediaTitle = `E2E Hall of Fame ${suffix}`;
  const athleteName = `E2E Inductee ${suffix}`;

  await signInAsE2eSuperAdmin(page);

  await page.goto("/admin/media");
  await page.getByLabel("Images").setInputFiles("public/images/track-hero.png");
  await page.getByLabel("Title").fill(mediaTitle);
  await page.getByLabel("Alt text").fill(`${athleteName} standing on the track`);
  const uploadResponse = page.waitForResponse(response => response.url().endsWith("/api/admin/media") && response.request().method() === "POST");
  await page.getByRole("button", { name: "Upload queue" }).click();
  const uploaded = await uploadResponse;
  expect(uploaded.status()).toBe(201);
  const mediaId = (await uploaded.json() as { id: string }).id;

  await page.goto("/admin/hall-of-fame/new");
  await page.getByLabel("Name").fill(athleteName);
  await page.getByLabel("Affiliation").fill("El1te Development Team");
  await page.getByLabel("Summary").fill("A disposable profile used to verify the complete Hall of Fame workflow.");
  await page.getByLabel("Active").uncheck();
  await page.getByRole("button", { name: "Choose" }).click();
  await page.getByLabel("Search media").fill(mediaTitle);
  await page.getByRole("button", { name: `Choose ${mediaTitle}` }).click();
  await page.getByLabel("Photo alt text").fill(`${athleteName} standing on the track`);
  const createResponse = page.waitForResponse(response => response.url().endsWith("/api/admin/hall-of-fame-inductees") && response.request().method() === "POST");
  await page.getByRole("button", { name: "Create" }).click();
  const created = await createResponse;
  expect(created.status()).toBe(201);
  const inducteeId = (await created.json() as { id: string }).id;
  await expect(page).toHaveURL(new RegExp(`/admin/hall-of-fame/${inducteeId}/edit`));

  await page.goto("/hall-of-fame");
  await expect(page.getByRole("heading", { name: athleteName })).toHaveCount(0);

  await page.goto(`/admin/hall-of-fame/${inducteeId}/edit`);
  await page.getByLabel("Active").check();
  await page.getByLabel("Induction year (optional)").fill("2026");
  const activateResponse = page.waitForResponse(response => response.url().endsWith(`/api/admin/hall-of-fame-inductees/${inducteeId}`) && response.request().method() === "PUT");
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await activateResponse).status()).toBe(200);

  await page.goto("/hall-of-fame");
  await expect(page.getByRole("heading", { name: athleteName })).toBeVisible();
  await expect(page.getByText(/Class of 2026.*El1te Development Team/)).toBeVisible();
  await expect(page.getByAltText(`${athleteName} standing on the track`)).toBeVisible();

  await page.goto(`/admin/hall-of-fame/${inducteeId}/edit`);
  await page.getByLabel("Affiliation").fill("El1te Alumni");
  const editResponse = page.waitForResponse(response => response.url().endsWith(`/api/admin/hall-of-fame-inductees/${inducteeId}`) && response.request().method() === "PUT");
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await editResponse).status()).toBe(200);

  await page.goto(`/admin/hall-of-fame?search=${encodeURIComponent(athleteName)}`);
  await page.getByRole("button", { name: `Deactivate ${athleteName}` }).click();
  const deactivateResponse = page.waitForResponse(response => response.url().endsWith(`/api/admin/hall-of-fame-inductees/${inducteeId}`) && response.request().method() === "DELETE");
  await page.getByRole("button", { name: "Deactivate", exact: true }).click();
  expect((await deactivateResponse).status()).toBe(204);

  await page.goto("/hall-of-fame");
  await expect(page.getByRole("heading", { name: athleteName })).toHaveCount(0);

  await page.goto(`/admin/hall-of-fame/${inducteeId}/edit`);
  await page.getByRole("button", { name: "Clear image" }).click();
  const clearResponse = page.waitForResponse(response => response.url().endsWith(`/api/admin/hall-of-fame-inductees/${inducteeId}`) && response.request().method() === "PUT");
  await page.getByRole("button", { name: "Save changes" }).click();
  expect((await clearResponse).status()).toBe(200);
  expect((await page.request.delete(`/api/admin/media/${mediaId}`)).status()).toBe(204);
});
