import { expect, test } from "@playwright/test";
import { signInAsE2eSuperAdmin } from "./helpers/admin-auth";

test("customer can browse live stock, configure gear, and review a privacy-safe cart", async ({ page }) => {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const productName = `E2E team crewneck ${suffix}`;
  let productId: string | undefined;

  try {
    await signInAsE2eSuperAdmin(page);

    await page.goto("/admin/media");
    await page.getByLabel("Images").setInputFiles("public/images/track-hero.png");
    await page.getByLabel("Title").fill(`${productName} image`);
    await page.getByLabel("Alt text").fill("Red El1te team crewneck preview");
    const uploadResponse = page.waitForResponse(response =>
      response.url().endsWith("/api/admin/media") && response.request().method() === "POST");
    await page.getByRole("button", { name: "Upload queue" }).click();
    const uploaded = await uploadResponse;
    expect(uploaded.status()).toBe(201);
    const mediaId = (await uploaded.json() as { id: string }).id;

    const optionId = crypto.randomUUID();
    const valueId = crypto.randomUUID();
    const variantId = crypto.randomUUID();
    const modifierGroupId = crypto.randomUUID();
    const modifierValueId = crypto.randomUUID();
    const createResponse = await page.request.post("/api/admin/store/products", {
      data: {
        categoryId: null,
        name: productName,
        shortDescription: "A configurable team crewneck for cross-stack verification.",
        description: "Official team gear prepared for practice handoff.",
        basePriceMinor: 4200,
        status: "Published",
        isFeatured: true,
        displayOrder: 0,
        allowsSpecialRequests: false,
        media: [{
          id: crypto.randomUUID(), mediaAssetId: mediaId, role: "MockupBase",
          altTextOverride: "Red El1te team crewneck preview", displayOrder: 0
        }],
        options: [{
          id: optionId, name: "Size", isTracked: true, displayOrder: 0, isActive: true,
          values: [{ id: valueId, name: "Medium", colorHex: null, swatchMediaAssetId: null, displayOrder: 0, isActive: true }]
        }],
        variants: [{
          id: variantId, name: "Medium", sku: `E2E-CREW-${suffix}`, priceOverrideMinor: null,
          lowStockThreshold: 3, isActive: true, rowVersion: null, optionValueIds: [valueId]
        }],
        modifierGroups: [{
          id: modifierGroupId,
          name: "Logo treatment",
          type: "Choice",
          isRequired: false,
          minimumSelections: 0,
          maximumSelections: 1,
          displayOrder: 0,
          isActive: true,
          values: [{
            id: modifierValueId,
            name: "Track red logo",
            priceAdjustmentMinor: 300,
            colorHex: "#ef1b22",
            overlayMediaAssetId: null,
            displayOrder: 0,
            isActive: true
          }]
        }],
        visualizerLayers: []
      }
    });
    expect(createResponse.status()).toBe(201);
    const created = await createResponse.json() as {
      id: string; slug: string; variants: Array<{ id: string; rowVersion: string }>;
    };
    productId = created.id;

    const receipt = await page.request.post("/api/admin/store/inventory/receipts", {
      data: {
        note: "E2E public store stock",
        lines: [{ variantId: created.variants[0].id, quantity: 2, rowVersion: created.variants[0].rowVersion }]
      }
    });
    expect(receipt.status()).toBe(201);

    await page.goto(`/shop?search=${encodeURIComponent(productName)}`);
    await expect(page.getByRole("heading", { level: 1, name: "Wear the work." })).toBeVisible();
    await expect(page.getByRole("heading", { name: productName })).toBeVisible();
    await page.getByRole("link", { name: new RegExp(productName) }).click();
    await expect(page).toHaveURL(new RegExp(`/shop/${created.slug}$`));
    await expect(page.getByRole("heading", { level: 1, name: productName })).toBeVisible();
    await expect(page.getByText("Low stock", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: /Medium/ })).toHaveAttribute("aria-pressed", "true");
    await expect(page.getByRole("radio", { name: "No customization" })).toBeChecked();
    await page.getByRole("radio", { name: /Track red logo/ }).check();
    await expect(page.getByText("$45.00", { exact: true })).toBeVisible();
    await page.getByRole("radio", { name: "No customization" }).check();
    await expect(page.getByText("$42.00", { exact: true })).toBeVisible();
    await page.getByRole("radio", { name: /Track red logo/ }).check();
    await page.getByRole("button", { name: "Add to cart" }).click();
    await expect(page.getByText("1 item added to your cart.")).toBeVisible();
    await page.getByRole("link", { name: "View cart" }).last().click();

    await expect(page.getByRole("heading", { name: "1 configured item" })).toBeVisible();
    const cartLine = page.locator("article.store-cart-line").filter({ hasText: productName });
    await expect(cartLine.getByRole("heading", { name: productName })).toBeVisible();
    await expect(cartLine.locator("dt").filter({ hasText: "Size" })).toBeVisible();
    await expect(cartLine.locator("dd").filter({ hasText: "Medium" })).toHaveCount(2);
    await expect(cartLine.locator("dt").filter({ hasText: "Logo treatment" })).toBeVisible();
    await expect(cartLine.locator("dd").filter({ hasText: "Track red logo" })).toBeVisible();
    await expect(cartLine.getByText("Low stock — checkout soon.")).toBeVisible();
    await expect(page.getByRole("link", { name: "Secure Square checkout" })).toHaveAttribute("href", "/shop/checkout");
    await page.getByRole("link", { name: "Secure Square checkout" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Checkout details" })).toBeVisible();
    await expect(page.getByLabel("Full name")).toBeVisible();
    await expect(page.getByRole("button", { name: "Continue to Square" })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`/shop/${created.slug}`);
    await expect(page.getByRole("heading", { level: 1, name: productName })).toBeVisible();
    await expect(page.getByRole("button", { name: "Add to cart" })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);

    await page.setViewportSize({ width: 340, height: 720 });
    await page.goto("/shop");
    await expect(page.getByRole("heading", { level: 1, name: "Wear the work." })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  } finally {
    if (productId) await page.request.delete(`/api/admin/store/products/${productId}`);
  }
});
