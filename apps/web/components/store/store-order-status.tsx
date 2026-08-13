"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Clock3, LoaderCircle, PackageCheck, RefreshCw, ShoppingBag, X } from "lucide-react";
import { SupportReference } from "@/components/shared/support-reference";
import type { StoreOrderStatusResult, ValidationProblem } from "@/lib/public/types";
import { validSupportReference } from "@/lib/observability/support-reference";
import { formatStoreMoney } from "@/lib/store/configurator";

export function StoreOrderStatus() {
  const [token, setToken] = useState("");
  const [order, setOrder] = useState<StoreOrderStatusResult | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [now, setNow] = useState(0);
  const cancellationDialog = useRef<HTMLDialogElement>(null);
  const loadToken = useCallback(async (value: string, silent = false) => {
    if (!value.trim()) { setMessage("Enter the secure token from your order email."); return; }
    if (!silent) { setBusy(true); setMessage(null); setReferenceId(null); }
    try {
      const response = await fetch("/api/public/store/orders/status", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token: value.trim() }) });
      const result = await response.json() as StoreOrderStatusResult & ValidationProblem & { message?: string };
      if (!response.ok) { setMessage(result.message ?? result.title ?? "This order link is invalid or expired."); setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null); return; }
      setOrder(result);
    } catch { if (!silent) setMessage("Order status could not be reached. Try again."); }
    finally { if (!silent) setBusy(false); }
  }, []);
  useEffect(() => {
    setNow(Date.now());
    const fragment = new URLSearchParams(window.location.hash.replace(/^#/, ""));
    const value = fragment.get("token") ?? window.location.hash.replace(/^#/, "");
    if (value) { setToken(value); void loadToken(value); }
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [loadToken]);
  useEffect(() => {
    if (!order || order.paymentStatus !== "Refunding") return;
    const timer = setInterval(() => void loadToken(token, true), 5000);
    return () => clearInterval(timer);
  }, [loadToken, order, token]);
  const seconds = useMemo(() => order?.customerCancellationExpiresAtUtc
    ? Math.max(0, Math.ceil((new Date(order.customerCancellationExpiresAtUtc).getTime() - now) / 1000)) : 0, [order, now]);

  async function cancel() {
    if (!order || busy) return;
    setBusy(true); setMessage(null); setReferenceId(null);
    try {
      const response = await fetch("/api/public/store/orders/cancel", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token }) });
      const result = await response.json() as StoreOrderStatusResult & ValidationProblem & { message?: string };
      if (!response.ok) { setMessage(result.message ?? result.title ?? "The order could not be canceled."); setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null); return; }
      setOrder(result);
      cancellationDialog.current?.close();
      setMessage(result.paymentStatus === "Refunded"
        ? "The order was canceled and the full Square refund completed."
        : "The order is locked from production. Your full Square refund is being processed; this page will update automatically.");
    } catch { setMessage("The cancellation service could not be reached. Your order was not changed. Try again."); }
    finally { setBusy(false); }
  }

  return <main className="store-order-status-page"><header className="store-cart-hero"><div className="site-container"><p className="eyebrow light">Private order access</p><h1>Track your gear</h1><p>Your secure link shows payment, production, and practice/event handoff progress.</p></div></header><div className="site-container store-order-status-wrap">
    {!order && <section className="store-token-entry"><ShoppingBag aria-hidden="true"/><h2>Open your order</h2><p>Use the secure link in your confirmation email, or paste its token below.</p><label>Secure order token<input value={token} onChange={event => setToken(event.target.value)} autoComplete="off"/></label><button className="button button-primary" onClick={() => void loadToken(token)} disabled={busy}>{busy ? "Loading…" : "View order"}</button>{message && <div role="alert" className="store-checkout-error">{message}<SupportReference referenceId={referenceId}/></div>}</section>}
    {order && <><section className="store-status-heading"><div><p className="eyebrow">{order.orderReference}</p><h2>{statusLabel(order.status)}</h2><p>Payment: <strong>{order.paymentStatus}</strong></p></div><button className="store-refresh" onClick={() => void loadToken(token)} disabled={busy}><RefreshCw size={17} aria-hidden="true"/>Refresh</button></section>
      {message && <div role="status" className="store-checkout-error">{message}<SupportReference referenceId={referenceId}/></div>}
      {order.canCustomerCancel && <section className="store-cancel-window"><Clock3 aria-hidden="true"/><div><h3>Cancellation window</h3><p>You have <strong>{Math.floor(seconds / 60)}:{String(seconds % 60).padStart(2, "0")}</strong> to cancel the complete order automatically for a full Square refund.</p></div><button type="button" onClick={() => cancellationDialog.current?.showModal()} disabled={busy}>Cancel order</button></section>}
      {order.paymentStatus === "Refunding" && <section className="store-refund-progress" role="status"><LoaderCircle className="animate-spin" aria-hidden="true"/><div><h3>Refund processing</h3><p>This order cannot enter production. Square is processing the full refund, and this page will refresh automatically.</p></div></section>}
      <div className="store-status-grid"><section className="store-status-panel"><h3>Order items</h3>{order.items.map((item, index) => <article key={`${item.productName}-${index}`} className="store-status-item"><div><strong>{item.quantity} × {item.productName}</strong><span>{item.variantName}</span>{item.configuration.map(value => <small key={`${value.label}-${value.value}`}>{value.label}: {value.value}</small>)}</div><strong>{formatStoreMoney(item.lineTotalMinor, order.currency)}</strong></article>)}<dl className="store-status-totals"><div><dt>Subtotal</dt><dd>{formatStoreMoney(order.subtotalMinor, order.currency)}</dd></div><div><dt>Tax</dt><dd>{formatStoreMoney(order.taxMinor, order.currency)}</dd></div><div><dt>Total</dt><dd>{formatStoreMoney(order.totalMinor, order.currency)}</dd></div></dl></section><section className="store-status-panel"><h3>Progress</h3><ol className="store-timeline">{order.timeline.map((item, index) => <li key={`${item.createdAtUtc}-${index}`}><PackageCheck aria-hidden="true"/><div><strong>{item.label}</strong><time dateTime={item.createdAtUtc}>{new Date(item.createdAtUtc).toLocaleString()}</time></div></li>)}</ol><p className="store-handoff-note">All launch orders are handed off at an arranged practice or club event. Staff will contact you when your order is ready.</p></section></div>
      <p className="store-status-help">Need help? Include <strong>{order.orderReference}</strong> when you <Link href="/contact">contact the club</Link>.</p>
      <dialog ref={cancellationDialog} className="store-cancel-dialog" onClose={() => { if (!busy) setReferenceId(null); }}><div><button type="button" className="store-dialog-close" onClick={() => cancellationDialog.current?.close()} disabled={busy} aria-label="Close cancellation confirmation"><X aria-hidden="true"/></button><p className="eyebrow">Final confirmation</p><h2>Cancel the complete order?</h2><p>The order will be locked from production, its inventory will be restored, and a full refund will be requested from Square. This action cannot be undone here.</p>{message && <div role="alert" className="store-checkout-error">{message}<SupportReference referenceId={referenceId}/></div>}<div className="store-dialog-actions"><button type="button" onClick={() => cancellationDialog.current?.close()} disabled={busy}>Keep order</button><button type="button" className="button button-primary" onClick={() => void cancel()} disabled={busy}>{busy?<><LoaderCircle className="animate-spin" aria-hidden="true"/>Canceling and refunding…</>:"Cancel and refund order"}</button></div></div></dialog></>}
  </div></main>;
}

function statusLabel(status: StoreOrderStatusResult["status"]) {
  return ({ AwaitingPayment: "Awaiting payment", Paid: "Payment confirmed", NeedsReview: "Personalization review", ReadyForProduction: "Ready for production", InProduction: "In production", NeedsCustomerInfo: "More information needed", ReadyForHandoff: "Ready for handoff", Completed: "Order completed", Canceled: "Order canceled", Refunded: "Order refunded" })[status];
}
