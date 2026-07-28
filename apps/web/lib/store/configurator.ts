import type {
  StoreProduct,
  StoreProductModifierGroup,
  StoreProductVariant
} from "@/lib/public/types";

export type OptionSelections = Record<string, string>;
export type ModifierSelections = Record<string, string[]>;
export type CustomInputs = Record<string, string>;

export function selectedOptionValueIds(selections: OptionSelections): string[] {
  return Object.values(selections).filter(Boolean).sort();
}

export function findMatchingVariant(
  variants: StoreProductVariant[],
  selections: OptionSelections
): StoreProductVariant | null {
  const selected = selectedOptionValueIds(selections);
  return variants.find(variant => {
    const values = [...variant.optionValueIds].sort();
    return values.length === selected.length && values.every((value, index) => value === selected[index]);
  }) ?? null;
}

export function optionValueCanBePurchased(
  product: StoreProduct,
  optionId: string,
  valueId: string,
  selections: OptionSelections
): boolean {
  const otherOptionValueIds = product.options
    .filter(option => option.id !== optionId)
    .map(option => selections[option.id])
    .filter(Boolean);
  return product.variants.some(variant =>
    variant.availability !== "SoldOut" &&
    variant.optionValueIds.includes(valueId) &&
    otherOptionValueIds.every(selected => variant.optionValueIds.includes(selected))
  );
}

export function selectedModifierValues(
  product: StoreProduct,
  selections: ModifierSelections
) {
  const selectedIds = new Set(Object.values(selections).flat());
  return product.modifierGroups
    .flatMap(group => group.values)
    .filter(value => selectedIds.has(value.id));
}

export function configurationPriceMinor(
  product: StoreProduct,
  variant: StoreProductVariant | null,
  selections: ModifierSelections
): number {
  return (variant?.priceMinor ?? product.basePriceMinor) +
    selectedModifierValues(product, selections)
      .reduce((total, value) => total + value.priceAdjustmentMinor, 0);
}

export function configurationIsComplete(
  product: StoreProduct,
  optionSelections: OptionSelections,
  modifierSelections: ModifierSelections,
  customInputs: CustomInputs
): boolean {
  if (product.options.some(option => !optionSelections[option.id])) return false;
  return product.modifierGroups.every(group => modifierGroupIsComplete(group, modifierSelections, customInputs));
}

function modifierGroupIsComplete(
  group: StoreProductModifierGroup,
  selections: ModifierSelections,
  customInputs: CustomInputs
): boolean {
  if (!group.isRequired) return true;
  if (group.type === "ShortText" || group.type === "Number") {
    return Boolean(customInputs[group.id]?.trim());
  }
  return (selections[group.id]?.length ?? 0) >= Math.max(1, group.minimumSelections);
}

export function formatStoreMoney(minor: number, currency = "USD"): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(minor / 100);
}
