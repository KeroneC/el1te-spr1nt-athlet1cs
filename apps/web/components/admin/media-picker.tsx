"use client";
/* eslint-disable @next/next/no-img-element */

import { Image as ImageIcon, X } from "lucide-react";
import { useEffect, useState } from "react";
import type { AdminMediaAsset, PagedResult } from "@/lib/admin/types";

export function MediaPicker({ name, label, defaultValue = "", error }: { name: string; label: string; defaultValue?: string; error?: string }) {
  const [value, setValue] = useState(defaultValue);
  const [assets, setAssets] = useState<AdminMediaAsset[]>([]);
  const [open, setOpen] = useState(false);
  useEffect(() => { if (open && !assets.length) void fetch("/api/admin/media/options").then(r => r.ok ? r.json() : null).then((r: PagedResult<AdminMediaAsset> | null) => setAssets(r?.items ?? [])); }, [open, assets.length]);
  return <div>
    <label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label>
    <div className="flex gap-2"><input id={name} name={name} type="url" value={value} onChange={e => setValue(e.target.value)} className="min-h-11 min-w-0 flex-1 border border-slate-300 px-3" /><button type="button" onClick={() => setOpen(v => !v)} className="inline-flex min-h-11 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><ImageIcon size={17} />Choose</button>{value && <button type="button" onClick={() => setValue("")} className="h-11 w-11 border border-slate-300" aria-label="Clear image"><X className="mx-auto" size={17}/></button>}</div>
    {error && <p className="mt-1 text-sm font-semibold text-red-700">{error}</p>}
    {value && <img src={value} alt="" className="mt-3 h-24 w-36 border border-slate-200 object-cover" />}
    {open && <div className="mt-3 grid max-h-72 grid-cols-2 gap-2 overflow-y-auto border border-slate-200 bg-slate-50 p-3 sm:grid-cols-4">{assets.map(asset => <button key={asset.id} type="button" onClick={() => { setValue(asset.publicUrl); setOpen(false); }} className="border border-slate-200 bg-white p-2 text-left hover:border-track-red"><img src={asset.publicUrl} alt={asset.altText} className="aspect-square w-full object-cover" /><span className="mt-1 block truncate text-xs font-bold">{asset.title}</span></button>)}{!assets.length && <p className="col-span-full p-4 text-sm text-slate-500">No active media yet.</p>}</div>}
  </div>;
}
