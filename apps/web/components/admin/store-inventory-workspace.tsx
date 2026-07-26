"use client";

import { ClipboardCheck, LoaderCircle, PackagePlus, RefreshCw, Search, SlidersHorizontal } from "lucide-react";
import { useMemo, useState } from "react";
import { FormNotice } from "./form-controls";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import type { AdminInventoryVariant, InventoryAdjustmentReason, PagedResult } from "@/lib/admin/types";
import { validSupportReference } from "@/lib/observability/support-reference";

type Mode = "receive" | "count" | "adjust";

export function StoreInventoryWorkspace({ initial }: { initial: PagedResult<AdminInventoryVariant> }) {
  const [result, setResult] = useState(initial);
  const [mode, setMode] = useState<Mode>("receive");
  const [search, setSearch] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [values, setValues] = useState<Record<string, string>>({});
  const [selected, setSelected] = useState<string | null>(null);
  const [reason, setReason] = useState<InventoryAdjustmentReason>("Correction");
  const [note, setNote] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [working, setWorking] = useState(false);

  const rows = useMemo(() => result.items.filter(value => {
    const term = search.trim().toLowerCase();
    return (!lowStockOnly || value.isLowStock || value.isSoldOut) &&
      (!term || `${value.productName} ${value.variantName} ${value.sku}`.toLowerCase().includes(term));
  }), [lowStockOnly, result.items, search]);

  async function reload() {
    setWorking(true);
    try {
      const response = await fetch("/api/admin/store/inventory?page=1&pageSize=200");
      if (redirectForAdminResponse(response)) return;
      if (response.ok) setResult(await response.json() as PagedResult<AdminInventoryVariant>);
    } finally { setWorking(false); }
  }

  async function submit() {
    setMessage(null); setReferenceId(null);
    const changed = rows.filter(row => values[row.variantId] !== undefined && values[row.variantId] !== "");
    const validationMessage = validateInventoryOperation(mode, selected, rows, values);
    if (validationMessage) { setMessage(validationMessage); return; }
    setWorking(true);
    try {
      let endpoint: string;
      let body: unknown;
      if (mode === "receive") {
        endpoint = "/api/admin/store/inventory/receipts";
        body = { note: note.trim() || null, lines: changed.map(row => ({ variantId: row.variantId, quantity: Number(values[row.variantId]), rowVersion: row.rowVersion })) };
      } else if (mode === "count") {
        endpoint = "/api/admin/store/inventory/stocktakes";
        body = { note: note.trim() || null, lines: changed.map(row => ({ variantId: row.variantId, countedOnHandQuantity: Number(values[row.variantId]), rowVersion: row.rowVersion })) };
      } else {
        const variantId = selected!;
        const row = result.items.find(value => value.variantId === variantId)!;
        endpoint = `/api/admin/store/inventory/${variantId}/adjustments`;
        body = { quantityDelta: Number(values[variantId]), reason, note: note.trim() || null, rowVersion: row.rowVersion };
      }
      const response = await fetch(endpoint, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (redirectForAdminResponse(response)) return;
      const payload = await response.json() as { message?: string; errors?: Record<string, string[]>; referenceId?: string };
      if (!response.ok) {
        setMessage(payload.message ?? Object.values(payload.errors ?? {}).flat()[0] ?? "Inventory could not be updated.");
        setReferenceId(response.status >= 500 ? validSupportReference(payload.referenceId) : null);
        return;
      }
      setMessage(mode === "receive" ? "Inventory receipt recorded." : mode === "count" ? "Physical stocktake completed." : "Inventory adjustment recorded.");
      setValues({}); setSelected(null); setNote("");
      await reload();
    } catch { setMessage("Inventory could not be updated. Try again."); }
    finally { setWorking(false); }
  }

  return <div className="space-y-5">
    <FormNotice message={message} success={Boolean(message?.includes("recorded") || message?.includes("completed"))} referenceId={referenceId}/>
    <div className="grid gap-4 border border-slate-200 bg-white p-4 lg:grid-cols-[minmax(240px,1fr)_auto_auto] lg:items-end">
      <label className="text-sm font-bold">Search inventory<div className="relative mt-2"><Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={17}/><input type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Product, option, or SKU" className="min-h-11 w-full border border-slate-300 pl-10 pr-3"/></div></label>
      <label className="flex min-h-11 items-center gap-2 text-sm font-bold"><input type="checkbox" checked={lowStockOnly} onChange={event => setLowStockOnly(event.target.checked)} className="h-5 w-5 accent-track-red"/>Low stock only</label>
      <button type="button" disabled={working} onClick={() => void reload()} className={secondary}><RefreshCw size={17}/>Refresh</button>
    </div>

    <section className="border border-slate-200 bg-white">
      <header className="border-b border-slate-200 p-4 sm:p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="text-xl font-black">Inventory matrix</h2><p className="mt-1 text-sm text-slate-600">Available stock equals on hand minus active reservations.</p></div>
        <div className="flex flex-wrap gap-2" role="group" aria-label="Inventory operation">
          <ModeButton active={mode === "receive"} onClick={() => { setMode("receive"); setValues({}); setSelected(null); }} icon={PackagePlus}>Receive</ModeButton>
          <ModeButton active={mode === "count"} onClick={() => { setMode("count"); setValues({}); setSelected(null); }} icon={ClipboardCheck}>Physical count</ModeButton>
          <ModeButton active={mode === "adjust"} onClick={() => { setMode("adjust"); setValues({}); setSelected(null); }} icon={SlidersHorizontal}>Adjust</ModeButton>
        </div></div></header>
      <div className="overflow-x-auto"><table className="w-full min-w-[840px] text-left"><thead className="bg-slate-100 text-xs uppercase tracking-wide text-slate-600"><tr><th className="p-3 sm:p-4">Product / variant</th><th className="p-3">SKU</th><th className="p-3 text-right">On hand</th><th className="p-3 text-right">Reserved</th><th className="p-3 text-right">Available</th><th className="p-3">Status</th><th className="p-3">{mode === "receive" ? "Quantity received" : mode === "count" ? "Counted on hand" : "Change (+ / −)"}</th></tr></thead>
        <tbody className="divide-y divide-slate-200">{rows.map(row => <tr key={row.variantId} className={selected === row.variantId ? "bg-red-50" : ""}><td className="p-3 sm:p-4"><p className="font-black">{row.productName}</p><p className="text-sm text-slate-600">{row.variantName}</p></td><td className="p-3 font-mono text-sm">{row.sku}</td><td className="p-3 text-right font-black">{row.onHandQuantity}</td><td className="p-3 text-right">{row.reservedQuantity}</td><td className="p-3 text-right text-lg font-black">{row.availableQuantity}</td><td className="p-3"><StockBadge row={row}/></td><td className="p-3"><div className="flex items-center gap-2">{mode === "adjust" && <input type="radio" name="selectedVariant" checked={selected === row.variantId} onChange={() => setSelected(row.variantId)} aria-label={`Adjust ${row.productName} ${row.variantName}`} className="h-5 w-5 accent-track-red"/>}<input type="number" min={mode === "receive" || mode === "count" ? 0 : undefined} aria-label={`${mode} quantity for ${row.productName} ${row.variantName}`} value={values[row.variantId] ?? ""} onFocus={() => mode === "adjust" && setSelected(row.variantId)} onChange={event => setValues(current => ({ ...current, [row.variantId]: event.target.value }))} className="min-h-10 w-28 border border-slate-300 px-3 text-right"/></div></td></tr>)}</tbody></table></div>
      {!rows.length && <p className="border-t border-dashed p-8 text-center text-sm text-slate-500">No variants match this view.</p>}
    </section>

    <section className="grid gap-4 border border-slate-200 bg-white p-4 sm:grid-cols-[1fr_auto] sm:items-end sm:p-5">
      <div className="grid gap-4 sm:grid-cols-2">
        {mode === "adjust" && <label className="text-sm font-bold">Reason<select value={reason} onChange={event => setReason(event.target.value as InventoryAdjustmentReason)} className="mt-2 min-h-11 w-full border border-slate-300 bg-white px-3"><option>Correction</option><option>Damage</option><option>ReturnRestock</option></select></label>}
        <label className={`text-sm font-bold ${mode !== "adjust" ? "sm:col-span-2" : ""}`}>Internal note (optional)<input value={note} onChange={event => setNote(event.target.value)} maxLength={1000} className="mt-2 min-h-11 w-full border border-slate-300 px-3" placeholder={mode === "count" ? "Shelf count, date, or discrepancy note" : "Shipment, correction, or return context"}/></label>
      </div>
      <button type="button" disabled={working || !rows.length} onClick={() => void submit()} className={primary}>{working ? <LoaderCircle className="animate-spin" size={18}/> : mode === "receive" ? <PackagePlus size={18}/> : mode === "count" ? <ClipboardCheck size={18}/> : <SlidersHorizontal size={18}/>} {mode === "receive" ? "Record receipt" : mode === "count" ? "Complete stocktake" : "Record adjustment"}</button>
    </section>
  </div>;
}

export function validateInventoryOperation(
  mode: Mode,
  selected: string | null,
  rows: AdminInventoryVariant[],
  values: Record<string, string>
): string | null {
  const changed = rows.filter(row => values[row.variantId] !== undefined && values[row.variantId] !== "");
  if (mode !== "adjust" && changed.length === 0) return "Enter at least one quantity.";
  if (mode === "adjust" && !selected) return "Choose a variant to adjust.";
  if (changed.some(row => !Number.isInteger(Number(values[row.variantId])))) return "Quantities must be whole numbers.";
  if (mode === "receive" && changed.some(row => Number(values[row.variantId]) <= 0)) return "Received quantities must be greater than zero.";
  if (mode === "count" && changed.some(row => Number(values[row.variantId]) < row.reservedQuantity)) return "A physical count cannot be lower than reserved inventory.";
  if (mode === "adjust" && selected && Number(values[selected]) === 0) return "An adjustment cannot be zero.";
  return null;
}

function ModeButton({ active, onClick, icon: Icon, children }: { active: boolean; onClick: () => void; icon: typeof PackagePlus; children: React.ReactNode }) { return <button type="button" aria-pressed={active} onClick={onClick} className={`inline-flex min-h-10 items-center gap-2 px-3 text-sm font-black ${active ? "bg-track-ink text-white" : "border border-slate-300 bg-white"}`}><Icon size={16}/>{children}</button>; }
function StockBadge({ row }: { row: AdminInventoryVariant }) { const style = row.isSoldOut ? "bg-red-100 text-red-800" : row.isLowStock ? "bg-amber-100 text-amber-900" : "bg-emerald-100 text-emerald-900"; return <span className={`inline-flex px-2 py-1 text-xs font-black ${style}`}>{row.isSoldOut ? "Sold out" : row.isLowStock ? "Low stock" : "In stock"}</span>; }
const primary = "inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-5 text-sm font-black text-white hover:bg-red-800 disabled:opacity-50";
const secondary = "inline-flex min-h-11 items-center justify-center gap-2 border border-slate-300 bg-white px-4 text-sm font-black hover:border-track-red";
