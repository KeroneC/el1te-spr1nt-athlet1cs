import { describe, expect, it } from "vitest";
import { validateInventoryOperation } from "../components/admin/store-inventory-workspace";
import { validateStoreProductDraft, type StoreProductDraft } from "../components/admin/store-product-wizard";
import type { AdminInventoryVariant } from "../lib/admin/types";

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
