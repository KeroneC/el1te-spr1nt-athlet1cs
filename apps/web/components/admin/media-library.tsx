"use client";
/* eslint-disable @next/next/no-img-element */

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LoaderCircle, Trash2, Upload } from "lucide-react";
import type { AdminMediaAsset } from "@/lib/admin/types";

export function MediaUploadForm() {
  const router = useRouter(); const [busy,setBusy]=useState(false); const [message,setMessage]=useState(""); const [success,setSuccess]=useState(false); const [file,setFile]=useState<File|null>(null);
  const preview=file?URL.createObjectURL(file):null;
  useEffect(()=>()=>{if(preview)URL.revokeObjectURL(preview);},[preview]);
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    const form = event.currentTarget;
    event.preventDefault(); setBusy(true); setMessage(""); setSuccess(false);
    const response=await fetch("/api/admin/media",{method:"POST",body:new FormData(form)});
    const result=await response.json() as AdminMediaAsset & { message?: string }; setBusy(false);
    if(!response.ok){setMessage(result.message??"Upload failed.");return;} form.reset(); setFile(null); setSuccess(true); setMessage(`${result.title} uploaded successfully.`); router.refresh();
  }
  return <form onSubmit={submit} className="mb-6 grid gap-4 border border-slate-200 bg-white p-5 md:grid-cols-2"><div className="md:col-span-2"><h2 className="text-lg font-black">Upload image</h2><p className="text-sm text-slate-500">JPEG, PNG, or WebP, up to 10 MB.</p></div>{message&&<p role={success?"status":"alert"} className={`border-l-4 px-3 py-2 text-sm font-bold md:col-span-2 ${success?"border-emerald-600 bg-emerald-50 text-emerald-900":"border-track-red bg-red-50 text-red-800"}`}>{message}</p>}<label className="text-sm font-bold">Image<input name="file" type="file" required accept="image/jpeg,image/png,image/webp" onChange={e=>setFile(e.target.files?.[0]??null)} className="mt-2 block w-full border border-slate-300 p-2"/>{file&&<span className="mt-1 block text-xs font-normal text-slate-500">{file.name} - {(file.size/1024/1024).toFixed(2)} MB</span>}</label>{preview&&<img src={preview} alt="Selected upload preview" className="h-32 w-full object-contain bg-slate-100"/>}<label className="text-sm font-bold">Title<input name="title" required maxLength={200} className="mt-2 min-h-11 w-full border border-slate-300 px-3"/></label><label className="text-sm font-bold">Alt text<input name="altText" maxLength={500} className="mt-2 min-h-11 w-full border border-slate-300 px-3"/></label><label className="text-sm font-bold">Caption<input name="caption" maxLength={1000} className="mt-2 min-h-11 w-full border border-slate-300 px-3"/></label><button disabled={busy} className="inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-4 font-bold text-white md:justify-self-end">{busy?<LoaderCircle className="animate-spin" size={18}/>:<Upload size={18}/>}Upload</button></form>;
}

export function MediaActions({asset}:{asset:AdminMediaAsset}) {
  const router=useRouter(); const [busy,setBusy]=useState(false);
  async function remove(){if(!confirm(`Delete ${asset.title}? This cannot be undone.`))return;setBusy(true);const r=await fetch(`/api/admin/media/${asset.id}`,{method:"DELETE"});setBusy(false);if(r.ok)router.refresh();else alert((await r.json()).message??"Could not delete media.");}
  return <button type="button" disabled={busy} onClick={remove} title="Delete media" aria-label={`Delete ${asset.title}`} className="h-9 w-9 border border-slate-300 text-red-700"><Trash2 className="mx-auto" size={16}/></button>;
}
