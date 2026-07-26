"use client";

import { CheckCircle2, DownloadCloud, LoaderCircle, ShieldAlert } from "lucide-react";
import { useState } from "react";
import { FormNotice } from "./form-controls";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import type { SquareCatalogImportPreview, SquareCatalogImportResult } from "@/lib/admin/types";
import { validSupportReference } from "@/lib/observability/support-reference";

export function SquareCatalogImport({ preview }: { preview: SquareCatalogImportPreview }) {
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  async function start() {
    if (!window.confirm(`Import ${preview.newProductCount} new Square product${preview.newProductCount === 1 ? "" : "s"} as unpublished drafts?`)) return;
    setWorking(true); setMessage(null); setReferenceId(null);
    try {
      const response = await fetch("/api/admin/store/square-import", { method: "POST" });
      if (redirectForAdminResponse(response)) return;
      const result = await response.json() as SquareCatalogImportResult & { message?: string; referenceId?: string };
      if (!response.ok) {
        setMessage(result.message ?? "The Square catalog import failed.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
      } else {
        setMessage(`Import complete: ${result.productsCreated} drafts created, ${result.productsSkipped} existing products skipped, and ${result.imagesImported} images copied into El1te media.`);
      }
    } catch { setMessage("The Square catalog import failed. Try again."); }
    finally { setWorking(false); }
  }
  if (!preview.isConfigured) return <div className="border-l-4 border-amber-500 bg-amber-50 p-5"><div className="flex gap-3"><ShieldAlert className="shrink-0 text-amber-700"/><div><h2 className="font-black text-amber-950">Square is not configured in this environment</h2><p className="mt-1 text-sm leading-6 text-amber-900">Add the Square access token and location through the documented Key Vault workflow. No credential is entered or displayed in the Admin portal.</p></div></div></div>;
  return <div className="space-y-5">
    <FormNotice message={message} success={Boolean(message?.startsWith("Import complete"))} referenceId={referenceId}/>
    <div className="grid gap-4 sm:grid-cols-3"><Metric label="Square products" value={preview.productCount}/><Metric label="New drafts" value={preview.newProductCount}/><Metric label="Already imported" value={preview.productCount - preview.newProductCount}/></div>
    <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[640px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Product</th><th className="p-4">Variants</th><th className="p-4">Images</th><th className="p-4">Import status</th></tr></thead><tbody className="divide-y">{preview.products.map(product => <tr key={product.squareCatalogObjectId}><td className="p-4 font-black">{product.name}</td><td className="p-4">{product.variantCount}</td><td className="p-4">{product.imageCount}</td><td className="p-4">{product.alreadyImported ? <span className="inline-flex items-center gap-2 text-sm font-bold text-emerald-800"><CheckCircle2 size={17}/>Already imported</span> : <span className="text-sm font-bold text-slate-600">Ready as draft</span>}</td></tr>)}</tbody></table></div>
    <div className="flex flex-col justify-between gap-4 border border-slate-200 bg-white p-5 sm:flex-row sm:items-center"><div><h2 className="font-black">One-time catalog copy</h2><p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600">Products, options, variations, current quantities, and trusted Square-hosted images are copied into El1te. Existing imports are skipped and nothing is published automatically.</p></div><button type="button" disabled={working || preview.newProductCount === 0} onClick={() => void start()} className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 bg-track-red px-5 text-sm font-black text-white disabled:opacity-50">{working ? <LoaderCircle className="animate-spin" size={18}/> : <DownloadCloud size={18}/>}Import new drafts</button></div>
  </div>;
}
function Metric({ label, value }: { label: string; value: number }) { return <div className="border-l-4 border-track-field bg-white p-5"><p className="text-3xl font-black">{value}</p><p className="mt-1 text-sm font-bold text-slate-600">{label}</p></div>; }
