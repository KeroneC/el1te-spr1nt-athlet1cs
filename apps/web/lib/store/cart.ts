import type { StoreProduct } from "@/lib/public/types";

const storageKey = "el1te-store-cart-v1";

export interface StoreCartLine {
  id: string;
  productSlug: string;
  productName: string;
  imageUrl: string | null;
  imageAlt: string;
  variantId: string;
  variantName: string;
  optionLabels: string[];
  modifierLabels: string[];
  modifierValueIds: string[];
  customInputGroupIds: string[];
  unitPriceMinor: number;
  currency: string;
  quantity: number;
}

export function readStoreCart(storage: Pick<Storage, "getItem"> = localStorage): StoreCartLine[] {
  try {
    const parsed = JSON.parse(storage.getItem(storageKey) ?? "[]") as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed.filter(isCartLine).slice(0, 50).map(line => ({
      ...line,
      quantity: Math.min(10, Math.max(1, Math.trunc(line.quantity)))
    }));
  } catch {
    return [];
  }
}

export function writeStoreCart(
  lines: StoreCartLine[],
  storage: Pick<Storage, "setItem"> = localStorage
) {
  storage.setItem(storageKey, JSON.stringify(lines.slice(0, 50)));
  if (typeof window !== "undefined") window.dispatchEvent(new Event("el1te-cart-updated"));
}

export function addStoreCartLine(line: StoreCartLine) {
  const lines = readStoreCart();
  const matching = lines.find(value =>
    value.productSlug === line.productSlug &&
    value.variantId === line.variantId &&
    JSON.stringify(value.optionLabels) === JSON.stringify(line.optionLabels) &&
    JSON.stringify(value.modifierLabels) === JSON.stringify(line.modifierLabels) &&
    JSON.stringify(value.customInputGroupIds) === JSON.stringify(line.customInputGroupIds));
  if (matching) matching.quantity = Math.min(10, matching.quantity + line.quantity);
  else lines.push(line);
  writeStoreCart(lines);
}

export function cartTotalMinor(lines: StoreCartLine[]): number {
  return lines.reduce((total, line) => total + line.unitPriceMinor * line.quantity, 0);
}

export function cartConfigurationIsCurrent(line: StoreCartLine, product: StoreProduct): boolean {
  const modifierValues = new Set(product.modifierGroups.flatMap(group => group.values.map(value => value.id)));
  if (line.modifierValueIds.some(value => !modifierValues.has(value))) return false;
  const selectedModifierIds = new Set(line.modifierValueIds);
  return product.modifierGroups.every(group => {
    if (!group.isRequired) return true;
    if (group.type === "ShortText" || group.type === "Number")
      return line.customInputGroupIds.includes(group.id);
    const selectedCount = group.values.filter(value => selectedModifierIds.has(value.id)).length;
    return selectedCount >= Math.max(1, group.minimumSelections);
  });
}

function isCartLine(value: unknown): value is StoreCartLine {
  if (!value || typeof value !== "object") return false;
  const line = value as Partial<StoreCartLine>;
  return typeof line.id === "string" &&
    typeof line.productSlug === "string" &&
    typeof line.productName === "string" &&
    typeof line.variantId === "string" &&
    typeof line.variantName === "string" &&
    typeof line.unitPriceMinor === "number" &&
    Number.isSafeInteger(line.unitPriceMinor) &&
    line.unitPriceMinor >= 0 &&
    typeof line.currency === "string" &&
    typeof line.quantity === "number" &&
    Array.isArray(line.optionLabels) &&
    Array.isArray(line.modifierLabels) &&
    Array.isArray(line.modifierValueIds) &&
    Array.isArray(line.customInputGroupIds);
}
