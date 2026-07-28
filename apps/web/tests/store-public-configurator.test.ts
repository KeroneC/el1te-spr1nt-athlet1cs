import { describe, expect, it } from "vitest";
import type { StoreProduct } from "../lib/public/types";
import {
  cartConfigurationIsCurrent,
  cartTotalMinor,
  readStoreCart,
  type StoreCartLine
} from "../lib/store/cart";
import {
  configurationIsComplete,
  configurationPriceMinor,
  findMatchingVariant,
  formatStoreMoney,
  optionValueCanBePurchased
} from "../lib/store/configurator";

const product: StoreProduct = {
  name: "Team hoodie", slug: "team-hoodie", shortDescription: null, description: null,
  categoryName: "Apparel", basePriceMinor: 5000, currency: "USD", allowsSpecialRequests: true,
  availability: "InStock", media: [],
  options: [
    { id: "size", name: "Size", displayOrder: 0, values: [
      { id: "small", name: "Small", slug: "small", colorHex: null, swatchImageUrl: null, displayOrder: 0 },
      { id: "large", name: "Large", slug: "large", colorHex: null, swatchImageUrl: null, displayOrder: 1 }
    ] },
    { id: "color", name: "Color", displayOrder: 1, values: [
      { id: "red", name: "Red", slug: "red", colorHex: "#dc2626", swatchImageUrl: null, displayOrder: 0 },
      { id: "black", name: "Black", slug: "black", colorHex: "#171717", swatchImageUrl: null, displayOrder: 1 }
    ] }
  ],
  variants: [
    { id: "small-red", name: "Small / Red", priceMinor: 5000, availability: "InStock", optionValueIds: ["small", "red"] },
    { id: "small-black", name: "Small / Black", priceMinor: 5200, availability: "LowStock", optionValueIds: ["small", "black"] },
    { id: "large-red", name: "Large / Red", priceMinor: 5000, availability: "SoldOut", optionValueIds: ["large", "red"] }
  ],
  modifierGroups: [{
    id: "logo", name: "Logo treatment", type: "Choice", isRequired: true,
    minimumSelections: 1, maximumSelections: 1, displayOrder: 0,
    values: [{ id: "white-logo", name: "White logo", priceAdjustmentMinor: 500, colorHex: "#fff", overlayImageUrl: null, displayOrder: 0 }]
  }],
  visualizerLayers: []
};

describe("public store configuration", () => {
  it("matches only the complete tracked option combination", () => {
    expect(findMatchingVariant(product.variants, { size: "small", color: "black" })?.id).toBe("small-black");
    expect(findMatchingVariant(product.variants, { size: "small" })).toBeNull();
  });

  it("disables only combinations that have no purchasable variant", () => {
    expect(optionValueCanBePurchased(product, "size", "large", { size: "small", color: "red" })).toBe(false);
    expect(optionValueCanBePurchased(product, "color", "black", { size: "small", color: "red" })).toBe(true);
  });

  it("prices the chosen variant and untracked modifier in minor units", () => {
    const variant = product.variants[1];
    expect(configurationPriceMinor(product, variant, { logo: ["white-logo"] })).toBe(5700);
    expect(formatStoreMoney(5700)).toBe("$57.00");
  });

  it("requires tracked options and required customization", () => {
    expect(configurationIsComplete(product, { size: "small", color: "red" }, {}, {})).toBe(false);
    expect(configurationIsComplete(product, { size: "small", color: "red" }, { logo: ["white-logo"] }, {})).toBe(true);
  });
});

describe("privacy-safe cart persistence", () => {
  const line: StoreCartLine = {
    id: "line", productSlug: "team-hoodie", productName: "Team hoodie", imageUrl: null,
    imageAlt: "Team hoodie", variantId: "small-red", variantName: "Small / Red",
    optionLabels: ["Size: Small", "Color: Red"], modifierLabels: ["Logo treatment: White"],
    modifierValueIds: ["white-logo"], customInputGroupIds: [], unitPriceMinor: 5500,
    currency: "USD", quantity: 2
  };

  it("calculates totals without floating point money", () => {
    expect(cartTotalMinor([line])).toBe(11000);
  });

  it("rejects malformed browser data and caps quantities", () => {
    const storage = { getItem: () => JSON.stringify([{ ...line, quantity: 999 }, { customerEmail: "do-not-store" }]) };
    const result = readStoreCart(storage);
    expect(result).toHaveLength(1);
    expect(result[0].quantity).toBe(10);
  });

  it("does not accept free-form personal data in persisted cart lines", () => {
    const storage = { getItem: () => JSON.stringify([{
      ...line,
      customInputLabels: ["Athlete name: Private Name"],
      customInputGroupIds: undefined
    }]) };
    expect(readStoreCart(storage)).toEqual([]);
  });

  it("flags stale or newly incomplete customizations before checkout", () => {
    expect(cartConfigurationIsCurrent(line, product)).toBe(true);
    expect(cartConfigurationIsCurrent({ ...line, modifierValueIds: ["removed-choice"] }, product)).toBe(false);
    expect(cartConfigurationIsCurrent({ ...line, modifierValueIds: [] }, product)).toBe(false);
  });
});
