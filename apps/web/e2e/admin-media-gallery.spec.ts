import { expect, test } from "@playwright/test";

test("admin can publish an uploaded image in a public gallery album", async ({ page }) => {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const mediaTitle = `E2E finish line ${suffix}`;
  const albumTitle = `E2E meet album ${suffix}`;
  const albumSlug = `e2e-meet-${suffix}`;
  let mediaId: string | undefined;
  let albumId: string | undefined;

  try {
    await page.goto("/admin/login");
    await page.getByLabel("Email").fill("e2e.admin@example.test");
    await page.locator("#password").fill("E2eAdmin!2026Pass");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page).toHaveURL(/\/admin$/);

    await page.goto("/admin/media");
    await page.getByLabel("Images").setInputFiles("public/images/track-hero.png");
    await page.getByLabel("Title").fill(mediaTitle);
    await page.getByLabel("Alt text").fill("Runner crossing the finish line");
    await page.getByLabel("Caption").fill("E2E gallery verification image");
    const uploadResponse = page.waitForResponse((response) => response.url().endsWith("/api/admin/media") && response.request().method() === "POST");
    await page.getByRole("button", { name: "Upload queue" }).click();
    const uploaded = await uploadResponse;
    expect(uploaded.status()).toBe(201);
    mediaId = (await uploaded.json() as { id: string }).id;
    await expect(page.getByText("1 image uploaded successfully.", { exact: true })).toBeVisible();
    await expect(page.getByText("Uploaded successfully.", { exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: mediaTitle })).toBeVisible();

    await page.goto("/admin/gallery/new");
    await page.getByLabel("Title").fill(albumTitle);
    await page.getByLabel("Slug (optional)").fill(albumSlug);
    await page.getByLabel("Description").fill("A complete cross-stack E2E gallery check.");
    await page.getByLabel("Published").check();
    const createResponse = page.waitForResponse((response) => response.url().endsWith("/api/admin/gallery-albums") && response.request().method() === "POST");
    await page.getByRole("button", { name: "Create" }).click();
    const created = await createResponse;
    expect(created.status()).toBe(201);
    albumId = (await created.json() as { id: string }).id;
    await expect(page).toHaveURL(new RegExp(`/admin/gallery/${albumId}/edit`));

    const albumImages = page.getByRole("region", { name: "Album images" });
    await albumImages.getByRole("button", { name: "Add images" }).click();
    await albumImages.getByLabel("Search media").fill(mediaTitle);
    await expect(albumImages.getByRole("button", { name: `Add ${mediaTitle}` })).toBeVisible();
    const addResponse = page.waitForResponse((response) => response.url().endsWith(`/api/admin/gallery-albums/${albumId}/media`) && response.request().method() === "POST");
    await albumImages.getByRole("button", { name: `Add ${mediaTitle}` }).click();
    expect((await addResponse).status()).toBe(201);
    await expect(albumImages.getByRole("heading", { name: mediaTitle })).toBeVisible();

    await page.goto("/gallery");
    await expect(page.getByRole("heading", { name: albumTitle })).toBeVisible();
    await page.getByRole("link", { name: new RegExp(albumTitle) }).click();
    await expect(page).toHaveURL(new RegExp(`/gallery/${albumSlug}$`));
    await expect(page.getByRole("heading", { name: albumTitle })).toBeVisible();
    await expect(page.getByAltText("Runner crossing the finish line")).toBeVisible();
    await expect(page.getByText("E2E gallery verification image")).toBeVisible();
  } finally {
    if (albumId) await page.request.delete(`/api/admin/gallery-albums/${albumId}`);
    if (mediaId) await page.request.delete(`/api/admin/media/${mediaId}`);
  }
});
