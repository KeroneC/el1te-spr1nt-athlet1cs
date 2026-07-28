"use client";
/* eslint-disable @next/next/no-img-element */

import { ArrowLeft, Check, ImageIcon, Minus, Plus, ShoppingBag, Sparkles } from "lucide-react";
import Link from "next/link";
import type { CSSProperties } from "react";
import { useMemo, useState } from "react";
import type {
  PublicStockStatus,
  StoreProduct,
  StoreProductModifierGroup
} from "@/lib/public/types";
import { addStoreCartLine } from "@/lib/store/cart";
import {
  configurationIsComplete,
  configurationPriceMinor,
  findMatchingVariant,
  formatStoreMoney,
  optionValueCanBePurchased,
  selectedModifierValues,
  type CustomInputs,
  type ModifierSelections,
  type OptionSelections
} from "@/lib/store/configurator";

export function StoreProductConfigurator({ product }: { product: StoreProduct }) {
  const purchasableVariant = product.variants.find(value => value.availability !== "SoldOut") ?? product.variants[0] ?? null;
  const [optionSelections, setOptionSelections] = useState<OptionSelections>(() =>
    initialOptionSelections(product, purchasableVariant?.optionValueIds ?? []));
  const [modifierSelections, setModifierSelections] = useState<ModifierSelections>({});
  const [customInputs, setCustomInputs] = useState<CustomInputs>({});
  const [quantity, setQuantity] = useState(1);
  const gallery = product.media.filter(value => value.role !== "LogoOverlay");
  const mockup = product.media.find(value => value.role === "MockupBase") ?? gallery[0] ?? null;
  const [selectedImageId, setSelectedImageId] = useState(gallery[0]?.mediaAssetId ?? "");
  const [previewMode, setPreviewMode] = useState<"photo" | "live">("photo");
  const [message, setMessage] = useState<string | null>(null);

  const variant = useMemo(
    () => findMatchingVariant(product.variants, optionSelections),
    [optionSelections, product.variants]);
  const selectedModifiers = useMemo(
    () => selectedModifierValues(product, modifierSelections),
    [modifierSelections, product]);
  const unitPrice = configurationPriceMinor(product, variant, modifierSelections);
  const complete = configurationIsComplete(product, optionSelections, modifierSelections, customInputs);
  const canAdd = complete && variant !== null && variant.availability !== "SoldOut";
  const selectedImage = gallery.find(value => value.mediaAssetId === selectedImageId) ?? gallery[0] ?? null;
  const selectedOptionIds = new Set(Object.values(optionSelections));
  const selectedModifierIds = new Set(selectedModifiers.map(value => value.id));
  const activeLayers = product.visualizerLayers.filter(layer =>
    (!layer.productOptionValueId || selectedOptionIds.has(layer.productOptionValueId)) &&
    (!layer.productModifierValueId || selectedModifierIds.has(layer.productModifierValueId)));
  const summary = configurationSummary(product, optionSelections, selectedModifiers.map(value => value.name), customInputs, variant?.availability);

  function chooseOption(optionId: string, valueId: string) {
    setOptionSelections(current => ({ ...current, [optionId]: valueId }));
    setMessage(null);
  }

  function chooseModifier(group: StoreProductModifierGroup, valueId: string, checked: boolean) {
    setModifierSelections(current => {
      const selected = current[group.id] ?? [];
      if (group.maximumSelections <= 1) return { ...current, [group.id]: checked ? [valueId] : [] };
      const next = checked
        ? [...selected.filter(value => value !== valueId), valueId].slice(-group.maximumSelections)
        : selected.filter(value => value !== valueId);
      return { ...current, [group.id]: next };
    });
    setMessage(null);
  }

  function addToCart() {
    if (!canAdd || !variant) {
      setMessage(variant?.availability === "SoldOut"
        ? "That combination is sold out. Choose another available option."
        : "Complete the required options before adding this item.");
      return;
    }
    const optionLabels = product.options.map(option => {
      const selected = option.values.find(value => value.id === optionSelections[option.id]);
      return `${option.name}: ${selected?.name ?? "Not selected"}`;
    });
    const modifierLabels = product.modifierGroups.flatMap(group =>
      (modifierSelections[group.id] ?? []).map(id => {
        const selected = group.values.find(value => value.id === id);
        return `${group.name}: ${selected?.name ?? "Selected"}`;
      }));
    const customInputGroupIds = product.modifierGroups
      .filter(group => customInputs[group.id]?.trim())
      .map(group => group.id);
    addStoreCartLine({
      id: crypto.randomUUID(),
      productSlug: product.slug,
      productName: product.name,
      imageUrl: selectedImage?.publicUrl ?? mockup?.publicUrl ?? null,
      imageAlt: selectedImage?.altText ?? product.name,
      variantId: variant.id,
      variantName: variant.name,
      optionLabels,
      modifierLabels,
      modifierValueIds: Object.values(modifierSelections).flat(),
      customInputGroupIds,
      unitPriceMinor: unitPrice,
      currency: product.currency,
      quantity
    });
    setMessage(`${quantity} ${quantity === 1 ? "item" : "items"} added to your cart.`);
  }

  return <article className="store-product-detail">
    <div className="site-container store-product-breadcrumbs">
      <Link href="/shop"><ArrowLeft size={17} aria-hidden="true" />All team gear</Link>
      <Link href="/shop/cart"><ShoppingBag size={17} aria-hidden="true" />View cart</Link>
    </div>
    <div className="site-container store-product-layout">
      <section className="store-product-visual" aria-label="Product images and live preview">
        <div className="store-visual-tabs" role="tablist" aria-label="Product view">
          <button role="tab" aria-selected={previewMode === "photo"} type="button" onClick={() => setPreviewMode("photo")}><ImageIcon size={17} aria-hidden="true" />Product photos</button>
          <button role="tab" aria-selected={previewMode === "live"} type="button" onClick={() => setPreviewMode("live")}><Sparkles size={17} aria-hidden="true" />Live preview</button>
        </div>
        <div className="store-visual-stage">
          {previewMode === "photo"
            ? selectedImage
              ? <img className="store-stage-photo" src={selectedImage.publicUrl} alt={selectedImage.altText} />
              : <div className="store-stage-placeholder"><ShoppingBag aria-hidden="true" /><span>Product image</span></div>
            : <div className="store-live-preview" aria-label={`Live preview of ${product.name}`}>
              {mockup
                ? <img className="store-preview-base" src={mockup.publicUrl} alt="" />
                : <div className="store-stage-placeholder"><ShoppingBag aria-hidden="true" /></div>}
              {activeLayers.map((layer, index) => <img
                key={`${layer.mediaAssetId}-${index}`}
                className="store-preview-layer"
                src={layer.publicUrl}
                alt=""
                aria-hidden="true"
                style={{
                  left: `${layer.xPercent}%`,
                  top: `${layer.yPercent}%`,
                  width: `${layer.widthPercent}%`,
                  height: `${layer.heightPercent}%`,
                  zIndex: layer.zIndex,
                  mixBlendMode: layer.blendMode as CSSProperties["mixBlendMode"]
                }}
              />)}
              {product.modifierGroups
                .filter(group => (group.type === "ShortText" || group.type === "Number") && customInputs[group.id]?.trim())
                .slice(0, 2)
                .map((group, index) => <span
                  className="store-preview-personalization"
                  key={group.id}
                  aria-hidden="true"
                  style={{ top: `${68 + index * 9}%` }}
                >{customInputs[group.id].trim()}</span>)}
            </div>}
        </div>
        {gallery.length > 1 && <div className="store-thumbnail-row" aria-label="Product photos">
          {gallery.map(image => <button key={image.mediaAssetId} type="button" aria-label={`View ${image.altText}`} aria-pressed={previewMode === "photo" && selectedImageId === image.mediaAssetId} onClick={() => { setSelectedImageId(image.mediaAssetId); setPreviewMode("photo"); }}>
            <img src={image.publicUrl} alt="" />
          </button>)}
        </div>}
        <p className="store-preview-notice">Preview placement and screen color may vary slightly from the finished pressed garment.</p>
      </section>

      <section className="store-configurator" aria-label={`Configure ${product.name}`}>
        <p className="eyebrow">{product.categoryName ?? "Official team gear"}</p>
        <h1>{product.name}</h1>
        {product.shortDescription && <p className="store-product-lead">{product.shortDescription}</p>}
        <div className="store-price-line">
          <strong>{formatStoreMoney(unitPrice, product.currency)}</strong>
          <StockBadge status={variant?.availability ?? product.availability} />
        </div>

        {product.options.map(option => <fieldset className="store-choice-group" key={option.id}>
          <legend>{option.name}<span>{option.values.find(value => value.id === optionSelections[option.id])?.name ?? "Choose one"}</span></legend>
          <div className="store-choice-grid">{option.values.map(value => {
            const available = optionValueCanBePurchased(product, option.id, value.id, optionSelections);
            const selected = optionSelections[option.id] === value.id;
            return <button
              key={value.id}
              type="button"
              className={selected ? "is-selected" : ""}
              disabled={!available}
              aria-pressed={selected}
              onClick={() => chooseOption(option.id, value.id)}
            >
              {(value.colorHex || value.swatchImageUrl) && <span className="store-choice-swatch" style={value.swatchImageUrl ? { backgroundImage: `url(${value.swatchImageUrl})` } : { backgroundColor: value.colorHex ?? undefined }} />}
              <span>{value.name}</span>
              {selected && <Check size={15} aria-hidden="true" />}
              {!available && <small>Sold out</small>}
            </button>;
          })}</div>
        </fieldset>)}

        {product.modifierGroups.map(group => <fieldset className="store-choice-group" key={group.id}>
          <legend>{group.name}{group.isRequired && <em>Required</em>}</legend>
          {group.type === "Choice" || group.type === "Color"
            ? <div className="store-modifier-list">
              {!group.isRequired && group.maximumSelections <= 1 && <label className={(modifierSelections[group.id] ?? []).length === 0 ? "is-selected" : ""}>
                <input
                  type="radio"
                  name={`modifier-${group.id}`}
                  checked={(modifierSelections[group.id] ?? []).length === 0}
                  onChange={() => {
                    setModifierSelections(current => ({ ...current, [group.id]: [] }));
                    setMessage(null);
                  }}
                />
                <span>No customization</span>
              </label>}
              {group.values.map(value => {
              const selected = (modifierSelections[group.id] ?? []).includes(value.id);
              return <label key={value.id} className={selected ? "is-selected" : ""}>
                <input
                  type={group.maximumSelections <= 1 ? "radio" : "checkbox"}
                  name={`modifier-${group.id}`}
                  checked={selected}
                  onChange={event => chooseModifier(group, value.id, event.target.checked)}
                />
                {(value.colorHex || value.overlayImageUrl) && <span className="store-choice-swatch" style={value.overlayImageUrl ? { backgroundImage: `url(${value.overlayImageUrl})` } : { backgroundColor: value.colorHex ?? undefined }} />}
                <span>{value.name}</span>
                {value.priceAdjustmentMinor > 0 && <small>+{formatStoreMoney(value.priceAdjustmentMinor, product.currency)}</small>}
              </label>;
            })}</div>
            : <label className="store-custom-input">
              <span>{group.type === "Number" ? "Enter number" : "Enter text"}{!group.isRequired && " (optional)"}</span>
              <input
                type={group.type === "Number" ? "number" : "text"}
                min={group.type === "Number" ? 0 : undefined}
                max={group.type === "Number" ? 99 : undefined}
                maxLength={group.type === "ShortText" ? 40 : undefined}
                value={customInputs[group.id] ?? ""}
                onChange={event => setCustomInputs(current => ({ ...current, [group.id]: event.target.value }))}
              />
              <small>Free-form requests are reviewed by staff before production.</small>
            </label>}
        </fieldset>)}

        <div className="store-purchase-row">
          <div className="store-quantity" aria-label="Quantity">
            <button type="button" aria-label="Decrease quantity" onClick={() => setQuantity(value => Math.max(1, value - 1))}><Minus size={17} /></button>
            <output aria-live="polite">{quantity}</output>
            <button type="button" aria-label="Increase quantity" onClick={() => setQuantity(value => Math.min(10, value + 1))}><Plus size={17} /></button>
          </div>
          <button className="button button-primary store-add-button" type="button" disabled={!canAdd} onClick={addToCart}><ShoppingBag size={18} aria-hidden="true" />{variant?.availability === "SoldOut" ? "Sold out" : "Add to cart"}</button>
        </div>
        {message && <p className={message.includes("added") ? "store-config-message success" : "store-config-message"} role="status">{message}{message.includes("added") && <> <Link href="/shop/cart">View cart</Link></>}</p>}
        <p className="store-selection-summary" aria-live="polite"><strong>Your configuration:</strong> {summary}</p>
        {product.description && <div className="store-product-description"><h2>Product details</h2><p>{product.description}</p></div>}
      </section>
    </div>
  </article>;
}

export function initialOptionSelections(product: StoreProduct, preferredValueIds: string[]): OptionSelections {
  const preferred = new Set(preferredValueIds);
  return Object.fromEntries(product.options.map(option => [
    option.id,
    option.values.find(value => preferred.has(value.id))?.id ?? option.values[0]?.id ?? ""
  ]));
}

function configurationSummary(
  product: StoreProduct,
  options: OptionSelections,
  modifiers: string[],
  inputs: CustomInputs,
  availability: PublicStockStatus | undefined
) {
  const parts = product.options.map(option =>
    `${option.name} ${option.values.find(value => value.id === options[option.id])?.name ?? "not selected"}`);
  parts.push(...modifiers);
  parts.push(...product.modifierGroups.filter(group => inputs[group.id]?.trim()).map(group => `${group.name} ${inputs[group.id].trim()}`));
  parts.push(availability === "SoldOut" ? "sold out" : availability === "LowStock" ? "low stock" : "available");
  return parts.join(", ");
}

function StockBadge({ status }: { status: PublicStockStatus }) {
  return <span className={`store-detail-stock ${status === "SoldOut" ? "is-sold" : status === "LowStock" ? "is-low" : "is-in"}`}>
    {status === "SoldOut" ? "Sold out" : status === "LowStock" ? "Low stock" : "In stock"}
  </span>;
}
