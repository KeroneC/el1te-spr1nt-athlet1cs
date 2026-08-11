"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import { ArrowLeft, LockKeyhole, ShieldCheck } from "lucide-react";
import { SupportReference } from "@/components/shared/support-reference";
import type { StoreCheckoutResult, StoreProduct, ValidationProblem } from "@/lib/public/types";
import { validSupportReference } from "@/lib/observability/support-reference";
import { cartTotalMinor, readStoreCart, type StoreCartLine } from "@/lib/store/cart";
import { formatStoreMoney } from "@/lib/store/configurator";

type InputDefinition = { lineId: string; groupId: string; label: string; type: "ShortText" | "Number"; required: boolean };

export function StoreCheckout() {
  const [lines, setLines] = useState<StoreCartLine[]>([]);
  const [inputs, setInputs] = useState<InputDefinition[]>([]);
  const [values, setValues] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [attemptId, setAttemptId] = useState(() => crypto.randomUUID());
  const phoneInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const cart = readStoreCart();
    setLines(cart);
    Promise.all([...new Set(cart.map(line => line.productSlug))].map(async slug => {
      const response = await fetch(`/api/public/store/products/${encodeURIComponent(slug)}`, { cache: "no-store" });
      if (!response.ok) throw new Error("availability");
      return await response.json() as StoreProduct;
    })).then(products => {
      const bySlug = new Map(products.map(product => [product.slug, product]));
      setInputs(cart.flatMap(line => {
        const product = bySlug.get(line.productSlug);
        return line.customInputGroupIds.flatMap(groupId => {
          const group = product?.modifierGroups.find(value => value.id === groupId);
          return group && (group.type === "ShortText" || group.type === "Number")
            ? [{ lineId: line.id, groupId, label: group.name, type: group.type, required: group.isRequired } satisfies InputDefinition]
            : [];
        });
      }));
    }).catch(() => setMessage("One or more products could not be verified. Return to your bag and try again."))
      .finally(() => setLoading(false));
  }, []);

  const total = useMemo(() => cartTotalMinor(lines), [lines]);
  const currency = lines[0]?.currency ?? "USD";

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitting(true); setMessage(null); setReferenceId(null); setFieldErrors({});
    const data = new FormData(event.currentTarget);
    const payload = {
      checkoutAttemptId: attemptId,
      customerName: String(data.get("customerName") ?? "").trim(),
      customerEmail: String(data.get("customerEmail") ?? "").trim(),
      customerPhone: String(data.get("customerPhone") ?? "").trim(),
      athleteTeamNote: String(data.get("athleteTeamNote") ?? "").trim() || null,
      confirmsAdultBuyer: data.get("confirmsAdultBuyer") === "on",
      acceptsStorePolicy: data.get("acceptsStorePolicy") === "on",
      lines: lines.map(line => ({
        productVariantId: line.variantId,
        quantity: line.quantity,
        modifierValueIds: line.modifierValueIds,
        customInputs: line.customInputGroupIds.map(groupId => ({
          modifierGroupId: groupId,
          value: values[`${line.id}:${groupId}`]?.trim() ?? ""
        }))
      }))
    };
    try {
      const response = await fetch("/api/public/store/checkout", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload)
      });
      const result = await response.json() as StoreCheckoutResult & ValidationProblem & { message?: string };
      if (!response.ok) {
        const phoneError = checkoutFieldErrors(result).customerPhone;
        if (phoneError) {
          setFieldErrors({ customerPhone: phoneError });
          setMessage("Review the highlighted checkout detail.");
          setAttemptId(crypto.randomUUID());
          requestAnimationFrame(() => phoneInput.current?.focus());
          return;
        }
        setMessage(result.message ?? result.title ?? "Checkout could not be prepared.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
        return;
      }
      window.location.assign(result.checkoutUrl);
    } catch {
      setMessage("Checkout could not be reached. Please try again.");
    } finally { setSubmitting(false); }
  }

  if (loading) return <div className="site-container store-order-loading" role="status">Preparing secure checkout…</div>;
  if (!lines.length) return <div className="site-container store-empty-state store-order-empty"><h1>Your gear bag is empty</h1><Link className="button button-primary" href="/shop">Browse the collection</Link></div>;

  return <article className="store-cart-page">
    <header className="store-cart-hero"><div className="site-container"><p className="eyebrow light">Secure handoff order</p><h1>Checkout details</h1><p>Tell our gear team who is placing the order. Square will securely collect payment next.</p></div></header>
    <form className="site-container store-checkout-layout" onSubmit={submit}>
      <section className="store-checkout-form" aria-labelledby="buyer-heading">
        <Link className="store-back-link" href="/shop/cart"><ArrowLeft size={17}/>Return to your bag</Link>
        {message && <div className="store-checkout-error" role="alert">{message}<SupportReference referenceId={referenceId}/></div>}
        <div className="store-form-panel"><p className="eyebrow">Adult buyer</p><h2 id="buyer-heading">Contact details</h2>
          <div className="store-form-grid"><label>Full name<input name="customerName" autoComplete="name" required maxLength={200}/></label><label>Email<input name="customerEmail" type="email" autoComplete="email" required maxLength={256}/></label><label>Phone<input ref={phoneInput} name="customerPhone" type="tel" inputMode="tel" autoComplete="tel" required maxLength={40} aria-invalid={fieldErrors.customerPhone ? "true" : undefined} aria-describedby={fieldErrors.customerPhone ? "checkout-phone-error" : undefined} onInput={() => setFieldErrors(current => { const next = { ...current }; delete next.customerPhone; return next; })}/>{fieldErrors.customerPhone && <span id="checkout-phone-error" className="store-field-error" role="alert">{fieldErrors.customerPhone}</span>}</label><label>Practice or athlete note <span>(optional)</span><input name="athleteTeamNote" maxLength={300}/></label></div>
        </div>
        {inputs.length > 0 && <div className="store-form-panel"><p className="eyebrow">Final configuration</p><h2>Personalization</h2><p>Confirm these details carefully. Correctly produced personalized gear is final sale after the cancellation window.</p><div className="store-form-grid">{inputs.map(input => { const line = lines.find(value => value.id === input.lineId)!; const key = `${input.lineId}:${input.groupId}`; return <label key={key}>{line.productName}: {input.label}<input value={values[key] ?? ""} onChange={event => setValues(current => ({ ...current, [key]: event.target.value }))} required={input.required} inputMode={input.type === "Number" ? "numeric" : undefined} maxLength={40}/></label>; })}</div></div>}
        <div className="store-form-panel store-consent-panel"><label><input type="checkbox" name="confirmsAdultBuyer" required/><span>I confirm that I am at least 18 years old and am placing this order.</span></label><label><input type="checkbox" name="acceptsStorePolicy" required/><span>I reviewed and accept the <Link href="/store-policy" target="_blank">Store Policy</Link>.</span></label></div>
      </section>
      <aside className="store-cart-summary"><p className="eyebrow">Order summary</p><h2>{lines.length} {lines.length === 1 ? "item" : "items"}</h2><ul className="store-checkout-items">{lines.map(line => <li key={line.id}><span>{line.quantity} × {line.productName}<small>{line.variantName}</small></span><strong>{formatStoreMoney(line.unitPriceMinor * line.quantity, line.currency)}</strong></li>)}</ul><dl><div><dt>Merchandise</dt><dd>{formatStoreMoney(total, currency)}</dd></div><div><dt>Tax</dt><dd>Calculated by Square</dd></div><div><dt>Handoff</dt><dd>Free at practice/event</dd></div></dl><button className="button button-primary" disabled={submitting || !!message && inputs.length === 0}>{submitting ? "Opening Square…" : "Continue to Square"}</button><p className="store-secure-note"><ShieldCheck size={18} aria-hidden="true"/>Card details stay with Square.</p><p className="store-secure-note"><LockKeyhole size={18} aria-hidden="true"/>Your bag does not store personal details.</p></aside>
    </form>
  </article>;
}

export function checkoutFieldErrors(problem: ValidationProblem): Record<string, string> {
  const phone = problem.errors?.customerPhone?.[0] ?? problem.errors?.CustomerPhone?.[0];
  return phone ? { customerPhone: phone } : {};
}
