"use client";

import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { Check, Copy, LoaderCircle, Mail, Printer, RefreshCw } from "lucide-react";
import { SupportReference } from "@/components/shared/support-reference";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import type { AdminStoreOrder, AdminStoreOrderStatus } from "@/lib/admin/types";
import { validSupportReference } from "@/lib/observability/support-reference";

const transitions: Partial<Record<AdminStoreOrderStatus, AdminStoreOrderStatus[]>> = {
  Paid: ["ReadyForProduction","NeedsReview"], NeedsReview: ["ReadyForProduction","NeedsCustomerInfo"],
  NeedsCustomerInfo: ["NeedsReview","ReadyForProduction"], ReadyForProduction: ["InProduction","NeedsCustomerInfo"],
  InProduction: ["ReadyForHandoff","NeedsCustomerInfo"], ReadyForHandoff: ["Completed"]
};

export function StoreOrderActions({ order, superAdmin }: { order: AdminStoreOrder; superAdmin: boolean }) {
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [trackingUrl, setTrackingUrl] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const trackingInput = useRef<HTMLInputElement>(null);

  async function mutate<T = unknown>(path: string, body?: unknown): Promise<T | null> {
    setBusy(path); setMessage(null); setReferenceId(null);
    try {
      const response = await fetch(path, { method: "POST", headers: body ? { "Content-Type": "application/json" } : undefined, body: body ? JSON.stringify(body) : undefined });
      if (redirectForAdminResponse(response)) return null;
      const result = await response.json().catch(() => ({})) as T & { message?: string; detail?: string; referenceId?: string };
      if (!response.ok) { setMessage(result.message ?? result.detail ?? "The order could not be updated."); setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null); return null; }
      router.refresh(); return result;
    } catch { setMessage("The order service could not be reached."); return null; }
    finally { setBusy(null); }
  }

  return <div className="space-y-5 print:hidden">
    {message && <div role="alert" className="border-l-4 border-track-red bg-red-50 p-3 text-sm font-bold text-red-900">{message}<SupportReference referenceId={referenceId}/></div>}
    <section className="border-t-4 border-track-red bg-white p-5"><div className="flex items-center justify-between gap-3"><div><h2 className="font-black">Fulfillment actions</h2><p className="text-sm text-slate-600">Only valid next steps are shown.</p></div><button onClick={() => window.print()} className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><Printer size={17}/>Print sheet</button></div><div className="mt-4 flex flex-wrap gap-2">{(transitions[order.status] ?? []).map(status => <button key={status} disabled={!!busy} onClick={() => void mutate(`/api/admin/store/orders/${order.id}/transitions`, { status, note: null })} className={`min-h-10 px-4 text-sm font-black ${status === "Canceled" ? "border border-red-700 text-red-700" : "bg-track-ink text-white"}`}>{busy ? <LoaderCircle className="inline animate-spin" size={16}/> : null}{label(status)}</button>)}{!(transitions[order.status]?.length) && <p className="text-sm text-slate-500">No fulfillment transition is available from this state.</p>}</div></section>
    <section className="border-t-4 border-track-ink bg-white p-5"><h2 className="font-black">Internal note</h2><form className="mt-3 flex flex-col gap-3" onSubmit={async event => { event.preventDefault(); const form=event.currentTarget; const note=String(new FormData(form).get("note")??"").trim(); if(note && await mutate(`/api/admin/store/orders/${order.id}/notes`,{note})) form.reset(); }}><textarea name="note" required maxLength={2000} rows={3} className="border border-slate-300 p-3" placeholder="Production or handoff note (staff only)"/><button disabled={!!busy} className="min-h-10 self-end bg-track-ink px-4 text-sm font-bold text-white">Add note</button></form></section>
    {order.emails.some(email => email.status === "Failed") && <section className="border-t-4 border-amber-500 bg-white p-5"><h2 className="font-black">Failed email</h2><div className="mt-3 space-y-2">{order.emails.filter(email=>email.status==="Failed").map(email=><button key={email.id} disabled={!!busy} onClick={()=>void mutate(`/api/admin/store/orders/${order.id}/emails/${email.id}/retry`)} className="inline-flex min-h-10 items-center gap-2 border border-amber-600 px-3 text-sm font-bold text-amber-800"><Mail size={16}/>Retry {label(email.templateName)}</button>)}</div></section>}
    {superAdmin && <><section className="border-t-4 border-sky-600 bg-white p-5"><h2 className="font-black">Secure tracking link</h2><p className="mt-1 text-sm text-slate-600">Rotating immediately revokes the customer’s previous link. The replacement is shown only here so you can deliver it through a trusted channel.</p><button disabled={!!busy} onClick={async()=>{setTrackingUrl(null);setCopied(false);const result=await mutate<{trackingUrl:string}>(`/api/admin/store/orders/${order.id}/tracking-link`);if(result?.trackingUrl){setTrackingUrl(result.trackingUrl);setMessage("A new tracking link was created and the previous link was revoked.");}}} className="mt-3 inline-flex min-h-10 items-center gap-2 border border-sky-700 px-3 text-sm font-bold text-sky-800"><RefreshCw size={16}/>Rotate link</button>{trackingUrl&&<div className="mt-4 border border-sky-200 bg-sky-50 p-3"><label className="text-xs font-black uppercase tracking-wide text-sky-950">One-time replacement link<input ref={trackingInput} readOnly value={trackingUrl} onFocus={event=>event.currentTarget.select()} className="mt-2 block min-h-11 w-full border border-sky-300 bg-white px-3 text-sm normal-case tracking-normal"/></label><button type="button" onClick={async()=>{try{await navigator.clipboard.writeText(trackingUrl);setCopied(true);}catch{trackingInput.current?.focus();trackingInput.current?.select();setCopied(false);}}} className="mt-2 inline-flex min-h-10 items-center gap-2 bg-sky-800 px-3 text-sm font-black text-white">{copied?<Check size={16}/>:<Copy size={16}/>} {copied?"Copied":"Copy link"}</button><span className="sr-only" aria-live="polite">{copied?"Tracking link copied":""}</span></div>}</section>
      {order.refunds.some(refund=>refund.status==="Failed") && <section className="border-t-4 border-amber-600 bg-white p-5"><h2 className="font-black">Failed Square refund</h2><p className="mt-1 text-sm text-slate-600">The order remains locked while the refund needs attention.</p><div className="mt-3 flex flex-wrap gap-2">{order.refunds.filter(refund=>refund.status==="Failed").map(refund=><button key={refund.id} disabled={!!busy} onClick={()=>void mutate(`/api/admin/store/orders/${order.id}/refunds/${refund.id}/retry`)} className="min-h-10 border border-amber-700 px-3 text-sm font-black text-amber-800">Retry refund</button>)}</div></section>}
      {["Paid", "PartiallyRefunded"].includes(order.paymentStatus) && <section className="border-t-4 border-red-700 bg-white p-5"><h2 className="font-black">Square refund</h2><p className="mt-1 text-sm text-slate-600">Record the refunded and restocked quantity for every line. Refund submission cannot be undone here.</p><form className="mt-4 space-y-3" onSubmit={async event=>{event.preventDefault();if(!confirm("Submit this refund to Square with the selected restocking decisions?"))return;const data=new FormData(event.currentTarget);const amount=Math.round(Number(data.get("amount"))*100);await mutate(`/api/admin/store/orders/${order.id}/refunds`,{amountMinor:amount,reason:String(data.get("reason")??"").trim(),lines:order.items.map(item=>({orderItemId:item.id,quantity:Number(data.get(`refund-${item.id}`)??0),restockQuantity:Number(data.get(`restock-${item.id}`)??0)})).filter(line=>line.quantity>0)});}}><label className="grid gap-1 text-sm font-bold">Refund amount (USD)<input name="amount" type="number" min="0.01" step="0.01" max={(remainingRefundMinor(order)/100).toFixed(2)} defaultValue={(remainingRefundMinor(order)/100).toFixed(2)} required className="min-h-11 border border-slate-300 px-3"/></label><label className="grid gap-1 text-sm font-bold">Reason<textarea name="reason" required maxLength={1000} rows={2} className="border border-slate-300 p-3"/></label><fieldset><legend className="text-sm font-black">Line decisions</legend>{order.items.map(item=><div key={item.id} className="mt-3 grid grid-cols-[1fr_76px_76px] items-end gap-2 text-sm"><span className="pb-2">{item.productName} ({item.quantity} purchased)</span><label className="grid gap-1 text-xs font-bold">Refund<input aria-label={`Refund quantity for ${item.productName}`} name={`refund-${item.id}`} type="number" min="0" max={item.quantity} defaultValue={order.paymentStatus === "Paid" ? item.quantity : 0} className="h-10 border border-slate-300 px-2"/></label><label className="grid gap-1 text-xs font-bold">Restock<input aria-label={`Restock quantity for ${item.productName}`} name={`restock-${item.id}`} type="number" min="0" max={item.quantity} defaultValue="0" className="h-10 border border-slate-300 px-2"/></label></div>)}</fieldset><button disabled={!!busy} className="min-h-11 bg-red-700 px-4 text-sm font-black text-white">Submit Square refund</button></form></section>}</>}
  </div>;
}
function label(value:string){return value.replace(/([a-z])([A-Z])/g,"$1 $2");}
function remainingRefundMinor(order: AdminStoreOrder) {
  const alreadyRefunded = order.refunds
    .filter(refund => refund.status === "Completed")
    .reduce((total, refund) => total + refund.amountMinor, 0);
  return Math.max(1, order.totalMinor - alreadyRefunded);
}
