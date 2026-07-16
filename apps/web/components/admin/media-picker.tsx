"use client";
/* eslint-disable @next/next/no-img-element */

import { Image as ImageIcon, X } from "lucide-react";
import { useState } from "react";
import { MediaOptionBrowser } from "./media-option-browser";

export function MediaPicker({ name, label, defaultValue = "", error }: { name: string; label: string; defaultValue?: string; error?: string }) {
  const [value, setValue] = useState(defaultValue);
  const [open, setOpen] = useState(false);
  return <div>
    <label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label>
    <div className="flex gap-2"><input id={name} name={name} type="url" value={value} onChange={e => setValue(e.target.value)} className="min-h-11 min-w-0 flex-1 border border-slate-300 px-3" /><button type="button" onClick={() => setOpen(v => !v)} className="inline-flex min-h-11 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><ImageIcon size={17} />Choose</button>{value && <button type="button" onClick={() => setValue("")} className="h-11 w-11 border border-slate-300" aria-label="Clear image"><X className="mx-auto" size={17}/></button>}</div>
    {error && <p className="mt-1 text-sm font-semibold text-red-700">{error}</p>}
    {value && <img src={value} alt="" className="mt-3 h-24 w-36 border border-slate-200 object-cover" />}
    {open && <div className="mt-3"><MediaOptionBrowser onSelect={asset => { setValue(asset.publicUrl); setOpen(false); }} /></div>}
  </div>;
}
