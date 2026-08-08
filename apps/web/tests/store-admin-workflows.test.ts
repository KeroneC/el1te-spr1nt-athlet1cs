import { describe, expect, it } from "vitest";
import { validateInventoryOperation } from "../components/admin/store-inventory-workspace";
import { parseMoneyInput, validateStoreProductDraft, type StoreProductDraft } from "../components/admin/store-product-wizard";
import type { AdminInventoryVariant } from "../lib/admin/types";
import { isAllowedAdminMutation, isAllowedAdminRead } from "../lib/admin/mutation-policy";

const draft = (overrides: Partial<StoreProductDraft> = {}): StoreProductDraft => ({
  categoryId: null, name: "Team hoodie", shortDescription: "", description: "",
  basePriceMinor: 5000, status: "Draft", isFeatured: false, displayOrder: 0,
  allowsSpecialRequests: false, media: [], options: [], variants: [],
  modifierGroups: [], visualizerLayers: [], ...overrides
});
const inventory: AdminInventoryVariant = {
  productId: "product", productName: "Hoodie", variantId: "variant", variantName: "Medium",
  sku: "HOOD-M", onHandQuantity: 5, reservedQuantity: 2, availableQuantity: 3,
  lowStockThreshold: 3, isLowStock: true, isSoldOut: false, isActive: true,
  rowVersion: "", updatedAtUtc: null
};

describe("store product wizard", () => {
  it("accepts typed dollar amounts without relying on number-field steppers", () => {
    expect(parseMoneyInput("25")).toBe(2500);
    expect(parseMoneyInput("25.5")).toBe(2550);
    expect(parseMoneyInput("25.50")).toBe(2550);
    expect(parseMoneyInput("0.99")).toBe(99);
  });

  it("rejects invalid prices and more than two decimal places", () => {
    expect(parseMoneyInput("")).toBeNull();
    expect(parseMoneyInput("-1")).toBeNull();
    expect(parseMoneyInput("12.345")).toBeNull();
    expect(parseMoneyInput("not a price")).toBeNull();
  });

  it("allows an incomplete draft to be saved for later", () => {
    expect(validateStoreProductDraft(draft())).toEqual([]);
  });

  it("requires media and an active SKU before publishing", () => {
    expect(validateStoreProductDraft(draft({ status: "Published" }))).toEqual(expect.arrayContaining([
      "Published products need at least one tracked variant.",
      "Published products need at least one image.",
      "Published products need an active variant."
    ]));
  });

  it("rejects duplicate SKU values regardless of casing", () => {
    const variant = (id: string, sku: string) => ({
      id, name: id, sku, priceOverrideMinor: null, onHandQuantity: 0, reservedQuantity: 0,
      availableQuantity: 0, lowStockThreshold: 3, isActive: true, squareCatalogObjectId: null,
      squareCatalogVersion: null, rowVersion: "", optionValueIds: []
    });
    expect(validateStoreProductDraft(draft({ variants: [variant("one", "SKU-1"), variant("two", "sku-1")] })))
      .toContain("Variant SKUs must be unique.");
  });
});

describe("store inventory workspace", () => {
  it("requires positive whole-number receiving quantities", () => {
    expect(validateInventoryOperation("receive", null, [inventory], { variant: "0" })).toBe("Received quantities must be greater than zero.");
    expect(validateInventoryOperation("receive", null, [inventory], { variant: "2.5" })).toBe("Quantities must be whole numbers.");
    expect(validateInventoryOperation("receive", null, [inventory], { variant: "4" })).toBeNull();
  });

  it("protects reserved inventory during physical counts", () => {
    expect(validateInventoryOperation("count", null, [inventory], { variant: "1" })).toBe("A physical count cannot be lower than reserved inventory.");
    expect(validateInventoryOperation("count", null, [inventory], { variant: "2" })).toBeNull();
  });

  it("requires a selected non-zero manual adjustment", () => {
    expect(validateInventoryOperation("adjust", null, [inventory], { variant: "-1" })).toBe("Choose a variant to adjust.");
    expect(validateInventoryOperation("adjust", "variant", [inventory], { variant: "0" })).toBe("An adjustment cannot be zero.");
    expect(validateInventoryOperation("adjust", "variant", [inventory], { variant: "-1" })).toBeNull();
  });
});

describe("store order proxy policy", () => {
  const id = "11111111-1111-1111-1111-111111111111";
  const emailId = "22222222-2222-2222-2222-222222222222";

  it("allows only the documented order reads", () => {
    expect(isAllowedAdminRead(["store", "orders"])).toBe(true);
    expect(isAllowedAdminRead(["store", "orders", id])).toBe(true);
    expect(isAllowedAdminRead(["store", "orders", "not-a-guid"])).toBe(false);
  });

  it("allows guarded order actions without opening a generic mutation proxy", () => {
    expect(isAllowedAdminMutation(["store", "orders", id, "transitions"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["store", "orders", id, "refunds"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["store", "orders", id, "emails", emailId, "retry"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["store", "orders", id, "refunds", emailId, "retry"], "POST")).toBe(true);
    expect(isAllowedAdminMutation(["store", "orders", id], "DELETE")).toBe(false);
  });
});
