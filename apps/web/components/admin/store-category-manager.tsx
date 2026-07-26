"use client";

import { Check, LoaderCircle, Pencil, Plus, X } from "lucide-react";
import { useState } from "react";
import { FormNotice } from "./form-controls";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import type { AdminProductCategory } from "@/lib/admin/types";
import { validSupportReference } from "@/lib/observability/support-reference";

export function StoreCategoryManager({ initial }: { initial: AdminProductCategory[] }) {
  const [categories, setCategories] = useState(initial);
  const [editing, setEditing] = useState<string | "new" | null>(null);
  const [name, setName] = useState("");
  const [displayOrder, setDisplayOrder] = useState(0);
  const [isActive, setIsActive] = useState(true);
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);

  function begin(category?: AdminProductCategory) {
    setEditing(category?.id ?? "new"); setName(category?.name ?? "");
    setDisplayOrder(category?.displayOrder ?? categories.length); setIsActive(category?.isActive ?? true);
    setMessage(null);
  }
  async function save() {
    if (!name.trim()) { setMessage("Enter a category name."); return; }
    setWorking(true); setMessage(null); setReferenceId(null);
    try {
      const creating = editing === "new";
      const response = await fetch(creating ? "/api/admin/store/categories" : `/api/admin/store/categories/${editing}`, {
        method: creating ? "POST" : "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: name.trim(), displayOrder, isActive })
      });
      if (redirectForAdminResponse(response)) return;
      const result = await response.json() as AdminProductCategory & { message?: string; referenceId?: string };
      if (!response.ok) {
        setMessage(result.message ?? "The category could not be saved.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
      } else {
        setCategories(current => creating ? [...current, result] : current.map(value => value.id === result.id ? result : value));
        setEditing(null); setMessage("Category saved successfully.");
      }
    } catch { setMessage("The category could not be saved. Try again."); }
    finally { setWorking(false); }
  }
  return <div className="space-y-5"><FormNotice message={message} success={message?.includes("successfully")} referenceId={referenceId}/>
    <div className="flex justify-end"><button type="button" onClick={() => begin()} className={primary}><Plus size={17}/>Add category</button></div>
    <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[620px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Category</th><th className="p-4">Products</th><th className="p-4">Order</th><th className="p-4">Status</th><th className="p-4 text-right">Action</th></tr></thead><tbody className="divide-y">{editing === "new" && <EditRow name={name} setName={setName} displayOrder={displayOrder} setDisplayOrder={setDisplayOrder} isActive={isActive} setIsActive={setIsActive} working={working} save={save} cancel={() => setEditing(null)}/>}
      {categories.toSorted((a,b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name)).map(category => editing === category.id ? <EditRow key={category.id} name={name} setName={setName} displayOrder={displayOrder} setDisplayOrder={setDisplayOrder} isActive={isActive} setIsActive={setIsActive} working={working} save={save} cancel={() => setEditing(null)}/> : <tr key={category.id}><td className="p-4"><p className="font-black">{category.name}</p><p className="text-xs text-slate-500">/{category.slug}</p></td><td className="p-4">{category.productCount}</td><td className="p-4">{category.displayOrder}</td><td className="p-4"><span className={`px-2 py-1 text-xs font-black ${category.isActive ? "bg-emerald-100 text-emerald-900" : "bg-slate-100 text-slate-600"}`}>{category.isActive ? "Active" : "Inactive"}</span></td><td className="p-4 text-right"><button type="button" onClick={() => begin(category)} aria-label={`Edit ${category.name}`} className="inline-flex h-10 w-10 items-center justify-center border border-slate-300"><Pencil size={17}/></button></td></tr>)}</tbody></table></div>
    <div className="border-l-4 border-track-field bg-white p-5 text-sm leading-6 text-slate-600"><strong className="text-track-ink">Safe deactivation:</strong> inactive categories remain attached to existing products but are not offered for new catalog organization.</div>
  </div>;
}
function EditRow({ name, setName, displayOrder, setDisplayOrder, isActive, setIsActive, working, save, cancel }: { name: string; setName: (value:string)=>void; displayOrder:number; setDisplayOrder:(value:number)=>void; isActive:boolean; setIsActive:(value:boolean)=>void; working:boolean; save:()=>void; cancel:()=>void }) { return <tr className="bg-red-50"><td className="p-3"><label className="sr-only" htmlFor="category-name">Category name</label><input id="category-name" autoFocus value={name} onChange={event => setName(event.target.value)} className="min-h-11 w-full border border-slate-300 px-3"/></td><td className="p-3 text-sm text-slate-500">—</td><td className="p-3"><label className="sr-only" htmlFor="category-order">Display order</label><input id="category-order" type="number" min="0" value={displayOrder} onChange={event => setDisplayOrder(Math.max(0, Number(event.target.value)))} className="min-h-11 w-24 border border-slate-300 px-3"/></td><td className="p-3"><label className="flex items-center gap-2 text-sm font-bold"><input type="checkbox" checked={isActive} onChange={event => setIsActive(event.target.checked)} className="h-5 w-5 accent-track-red"/>Active</label></td><td className="p-3"><div className="flex justify-end gap-2"><button type="button" disabled={working} onClick={save} aria-label="Save category" className="inline-flex h-10 w-10 items-center justify-center bg-track-red text-white">{working ? <LoaderCircle className="animate-spin" size={17}/> : <Check size={17}/>}</button><button type="button" disabled={working} onClick={cancel} aria-label="Cancel editing category" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 bg-white"><X size={17}/></button></div></td></tr>; }
const primary = "inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-4 text-sm font-black text-white hover:bg-red-800";
