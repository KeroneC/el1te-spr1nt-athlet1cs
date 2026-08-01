"use client";

import { ArrowLeft, Minus, Plus, ShieldCheck, ShoppingBag, Trash2 } from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import type { StoreProduct } from "@/lib/public/types";
import {
  cartConfigurationIsCurrent,
  cartTotalMinor,
  readStoreCart,
  writeStoreCart,
  type StoreCartLine
} from "@/lib/store/cart";
import { formatStoreMoney } from "@/lib/store/configurator";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";

type LineHealth = { status: "checking" | "available" | "low" | "sold" | "missing" | "error"; note: string };

export function StoreCart() {
  const [lines, setLines] = useState<StoreCartLine[]>([]);
  const [ready, setReady] = useState(false);
  const [health, setHealth] = useState<Record<string, LineHealth>>({});

  useEffect(() => {
    const stored = readStoreCart();
    setLines(stored);
    setReady(true);
    setHealth(Object.fromEntries(stored.map(line => [line.id, { status: "checking", note: "Checking availability…" }])));
    void refreshAvailability(stored).then(result => {
      setHealth(result.health);
      if (result.lines.some((line, index) => line.unitPriceMinor !== stored[index]?.unitPriceMinor)) {
        setLines(result.lines);
        writeStoreCart(result.lines);
      }
    });
  }, []);

  const total = useMemo(() => cartTotalMinor(lines), [lines]);
  const currency = lines[0]?.currency ?? "USD";
  const hasBlockedLine = Object.values(health).some(value => value.status === "sold" || value.status === "missing" || value.status === "error");

  function update(next: StoreCartLine[]) {
    setLines(next);
    writeStoreCart(next);
  }
  function changeQuantity(id: string, delta: number) {
    update(lines.map(line => line.id === id
      ? { ...line, quantity: Math.min(10, Math.max(1, line.quantity + delta)) }
      : line));
  }
  function remove(id: string) {
    update(lines.filter(line => line.id !== id));
    setHealth(current => Object.fromEntries(Object.entries(current).filter(([key]) => key !== id)));
  }

  if (!ready) return <div className="site-container loading-state" aria-label="Loading cart"><span className="loading-bar" /><span className="loading-bar short" /></div>;

  return <article className="store-cart-page">
    <header className="store-cart-hero">
      <div className="site-container">
        <p className="eyebrow light">Configuration review</p>
        <h1>Your gear bag</h1>
        <p>Review sizes, colors, personalization, and current availability before secure checkout.</p>
      </div>
    </header>
    <div className="site-container store-cart-layout">
      <section aria-labelledby="cart-items-heading">
        <div className="store-cart-title"><h2 id="cart-items-heading">{lines.length ? `${lines.length} configured ${lines.length === 1 ? "item" : "items"}` : "Your bag is empty"}</h2><Link href="/shop"><ArrowLeft size={17} />Keep shopping</Link></div>
        {lines.length ? <div className="store-cart-lines">{lines.map(line => {
          const state = health[line.id] ?? { status: "checking", note: "Checking availability…" };
          return <article className="store-cart-line" key={line.id}>
            <div className="store-cart-image">{line.imageUrl ? <ResponsiveMediaImage src={line.imageUrl} alt="" sizes="8rem" /> : <ShoppingBag aria-hidden="true" />}</div>
            <div className="store-cart-copy">
              <div><p>Configured team gear</p><h3><Link href={`/shop/${line.productSlug}`}>{line.productName}</Link></h3><strong>{formatStoreMoney(line.unitPriceMinor, line.currency)}</strong></div>
              <dl>
                <div><dt>Variant</dt><dd>{line.variantName}</dd></div>
                {[...line.optionLabels, ...line.modifierLabels].map((label, index) => <div key={`${label}-${index}`}><dt>{label.split(":")[0]}</dt><dd>{label.includes(":") ? label.slice(label.indexOf(":") + 1).trim() : label}</dd></div>)}
                {line.customInputGroupIds.length > 0 && <div><dt>Personalization</dt><dd>To be confirmed securely at checkout</dd></div>}
              </dl>
              <p className={`store-cart-health is-${state.status}`} role="status">{state.note}</p>
              <div className="store-cart-line-actions">
                <div className="store-quantity" aria-label={`Quantity for ${line.productName}`}>
                  <button type="button" aria-label={`Decrease ${line.productName} quantity`} onClick={() => changeQuantity(line.id, -1)}><Minus size={16} /></button>
                  <output>{line.quantity}</output>
                  <button type="button" aria-label={`Increase ${line.productName} quantity`} onClick={() => changeQuantity(line.id, 1)}><Plus size={16} /></button>
                </div>
                <button type="button" className="store-remove-line" onClick={() => remove(line.id)}><Trash2 size={16} />Remove</button>
              </div>
            </div>
            <strong className="store-line-total">{formatStoreMoney(line.unitPriceMinor * line.quantity, line.currency)}</strong>
          </article>;
        })}</div> : <div className="store-empty-state">
          <ShoppingBag aria-hidden="true" />
          <h2>Start with your favorite team gear</h2>
          <p>Choose a product, configure its available options, and it will appear here.</p>
          <Link className="button button-primary" href="/shop">Browse the collection</Link>
        </div>}
      </section>

      {lines.length > 0 && <aside className="store-cart-summary" aria-labelledby="cart-summary-heading">
        <p className="eyebrow">Order summary</p>
        <h2 id="cart-summary-heading">Ready for the next step?</h2>
        <dl><div><dt>Merchandise</dt><dd>{formatStoreMoney(total, currency)}</dd></div><div><dt>Tax</dt><dd>Calculated by Square</dd></div><div><dt>Practice handoff</dt><dd>Arranged after payment</dd></div><div className="store-cart-total"><dt>Subtotal</dt><dd>{formatStoreMoney(total, currency)}</dd></div></dl>
        {hasBlockedLine && <p className="store-cart-warning">Resolve unavailable or unverified items before continuing.</p>}
        <button className="button button-primary" type="button" disabled>Secure Square checkout</button>
        <p className="store-checkout-phase-note">Secure payment is being connected in the next guarded release. No order or personal information is collected in this preview.</p>
        <p className="store-secure-note"><ShieldCheck size={18} aria-hidden="true" />Card details will stay with Square.</p>
      </aside>}
    </div>
  </article>;
}

async function refreshAvailability(lines: StoreCartLine[]) {
  const health: Record<string, LineHealth> = {};
  const refreshed = await Promise.all(lines.map(async line => {
    try {
      const response = await fetch(`/api/public/store/products/${encodeURIComponent(line.productSlug)}`, { cache: "no-store" });
      if (response.status === 404) {
        health[line.id] = { status: "missing", note: "This product is no longer available." };
        return line;
      }
      if (!response.ok) {
        health[line.id] = { status: "error", note: "Availability could not be verified. Try again." };
        return line;
      }
      const product = await response.json() as StoreProduct;
      const variant = product.variants.find(value => value.id === line.variantId);
      if (!variant) {
        health[line.id] = { status: "missing", note: "This configuration is no longer available." };
        return line;
      }
      if (variant.availability === "SoldOut") {
        health[line.id] = { status: "sold", note: "Sold out — choose another configuration." };
        return line;
      }
      if (!cartConfigurationIsCurrent(line, product)) {
        health[line.id] = { status: "missing", note: "This configuration changed — open the product and update it." };
        return line;
      }
      health[line.id] = variant.availability === "LowStock"
        ? { status: "low", note: "Low stock — checkout soon." }
        : { status: "available", note: "Available" };
      const modifierAdjustment = product.modifierGroups
        .flatMap(group => group.values)
        .filter(value => line.modifierValueIds.includes(value.id))
        .reduce((total, value) => total + value.priceAdjustmentMinor, 0);
      return { ...line, unitPriceMinor: variant.priceMinor + modifierAdjustment };
    } catch {
      health[line.id] = { status: "error", note: "Availability could not be verified. Try again." };
      return line;
    }
  }));
  return { lines: refreshed, health };
}
