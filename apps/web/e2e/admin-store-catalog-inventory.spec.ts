import { expect, test } from "@playwright/test";

test("Admin can create a draft SKU, receive it, and complete a physical count", async ({ page }) => {
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const productName = `E2E team hoodie ${suffix}`;
  let productId: string | undefined;

  try {
    await page.goto("/admin/login");
    await page.getByLabel("Email").fill("e2e.admin@example.test");
    await page.locator("#password").fill("E2eAdmin!2026Pass");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page).toHaveURL(/\/admin$/);

    await page.getByRole("link", { name: "Merchandise" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Merchandise operations" })).toBeVisible();
    await page.getByRole("link", { name: "Add product" }).click();
    await page.getByLabel("Product name").fill(productName);
    await page.getByLabel("Base price (USD)").fill("45.00");
    await page.getByRole("button", { name: /Variants/ }).click();
    await page.getByRole("button", { name: "Generate variant matrix" }).click();
    await expect(page.getByLabel("Variant name")).toHaveValue("Standard");
    const sku = await page.getByLabel("SKU for Standard").inputValue();

    const createResponse = page.waitForResponse(response =>
      response.url().endsWith("/api/admin/store/products") &&
      response.request().method() === "POST");
    await page.getByRole("button", { name: "Save product" }).click();
    const created = await createResponse;
    expect(created.status()).toBe(201);
    productId = (await created.json() as { id: string }).id;
    await expect(page).toHaveURL(new RegExp(`/admin/store/products/${productId}/edit`));

    await page.goto("/admin/store/inventory");
    await page.getByLabel("Search inventory").fill(sku);
    const receiveInput = page.getByLabel(`receive quantity for ${productName} Standard`);
    await expect(receiveInput).toBeVisible();
    await receiveInput.fill("3");
    const receiptResponse = page.waitForResponse(response =>
      response.url().endsWith("/api/admin/store/inventory/receipts") &&
      response.request().method() === "POST");
    await page.getByRole("button", { name: "Record receipt" }).click();
    expect((await receiptResponse).status()).toBe(201);
    await expect(page.getByText("Inventory receipt recorded.")).toBeVisible();
    await expect(page.getByRole("row").filter({ hasText: sku }).getByText("3", { exact: true }).first()).toBeVisible();

    await page.getByRole("button", { name: "Physical count" }).click();
    await page.getByLabel(`count quantity for ${productName} Standard`).fill("2");
    const stocktakeResponse = page.waitForResponse(response =>
      response.url().endsWith("/api/admin/store/inventory/stocktakes") &&
      response.request().method() === "POST");
    await page.getByRole("button", { name: "Complete stocktake" }).click();
    expect((await stocktakeResponse).status()).toBe(201);
    await expect(page.getByText("Physical stocktake completed.")).toBeVisible();
  } finally {
    if (productId) await page.request.delete(`/api/admin/store/products/${productId}`);
  }
});
