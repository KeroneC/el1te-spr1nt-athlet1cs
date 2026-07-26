"use client";
/* eslint-disable @next/next/no-img-element */

import { ArrowLeft, ArrowRight, Check, ImagePlus, LoaderCircle, Plus, Save, Trash2, WandSparkles } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { MediaOptionBrowser } from "./media-option-browser";
import { FormNotice } from "./form-controls";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import type {
  AdminMediaAsset, AdminProductCategory, AdminProductMedia, AdminProductModifierGroup,
  AdminProductOption, AdminProductVariant, AdminProductVisualizerLayer, AdminStoreProduct,
  ProductMediaRole, ProductModifierType, StoreProductStatus
} from "@/lib/admin/types";
import { validSupportReference } from "@/lib/observability/support-reference";

const steps = ["Basics", "Media", "Variants", "Customizations", "Preview"] as const;
const uid = () => crypto.randomUUID();

export type StoreProductDraft = {
  categoryId: string | null; name: string; shortDescription: string; description: string;
  basePriceMinor: number; status: StoreProductStatus; isFeatured: boolean; displayOrder: number;
  allowsSpecialRequests: boolean; media: AdminProductMedia[]; options: AdminProductOption[];
  variants: AdminProductVariant[]; modifierGroups: AdminProductModifierGroup[];
  visualizerLayers: AdminProductVisualizerLayer[];
};

export function StoreProductWizard({
  item,
  categories
}: {
  item?: AdminStoreProduct;
  categories: AdminProductCategory[];
}) {
  const router = useRouter();
  const [step, setStep] = useState(0);
  const [draft, setDraft] = useState<StoreProductDraft>(() => fromItem(item));
  const [showMedia, setShowMedia] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const errors = validateStoreProductDraft(draft);
  const canPublish = errors.length === 0;
  const price = useMemo(() => new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(draft.basePriceMinor / 100), [draft.basePriceMinor]);

  function patch(value: Partial<StoreProductDraft>) { setDraft(current => ({ ...current, ...value })); }
  function addMedia(asset: AdminMediaAsset) {
    patch({ media: [...draft.media, {
      id: uid(), mediaAssetId: asset.id, publicUrl: asset.publicUrl, title: asset.title, altText: asset.altText,
      role: draft.media.length ? "Gallery" : "MockupBase", altTextOverride: null, displayOrder: draft.media.length
    }] });
    setShowMedia(false);
  }
  function addOption() {
    patch({ options: [...draft.options, {
      id: uid(), name: `Option ${draft.options.length + 1}`, isTracked: true, displayOrder: draft.options.length,
      isActive: true, squareCatalogObjectId: null, values: []
    }] });
  }
  function updateOption(id: string, value: Partial<AdminProductOption>) {
    patch({ options: draft.options.map(option => option.id === id ? { ...option, ...value } : option) });
  }
  function addOptionValue(optionId: string) {
    const option = draft.options.find(value => value.id === optionId);
    if (!option) return;
    updateOption(optionId, { values: [...option.values, {
      id: uid(), name: `Value ${option.values.length + 1}`, slug: "", colorHex: null,
      swatchMediaAssetId: null, displayOrder: option.values.length, isActive: true, squareCatalogObjectId: null
    }] });
  }
  function generateVariants() {
    const tracked = draft.options.filter(value => value.isTracked && value.isActive && value.values.some(item => item.isActive));
    const combinations = tracked.reduce<Array<Array<{ id: string; name: string }>>>(
      (rows, option) => rows.flatMap(row => option.values.filter(value => value.isActive).map(value => [...row, { id: value.id, name: value.name }])),
      [[]]
    );
    const rows = combinations.length ? combinations : [[]];
    const byKey = new Map(draft.variants.map(value => [[...value.optionValueIds].sort().join("|"), value]));
    const prefix = draft.name.replace(/[^a-z0-9]+/gi, "-").replace(/^-|-$/g, "").slice(0, 18).toUpperCase() || "ITEM";
    patch({ variants: rows.map((combo, index) => {
      const key = combo.map(value => value.id).sort().join("|");
      const existing = byKey.get(key);
      return existing ?? {
        id: uid(), name: combo.map(value => value.name).join(" / ") || "Standard",
        sku: `${prefix}-${String(index + 1).padStart(2, "0")}`, priceOverrideMinor: null,
        onHandQuantity: 0, reservedQuantity: 0, availableQuantity: 0, lowStockThreshold: 3,
        isActive: true, squareCatalogObjectId: null, squareCatalogVersion: null, rowVersion: "",
        optionValueIds: combo.map(value => value.id)
      };
    }) });
  }
  function addModifier() {
    patch({ modifierGroups: [...draft.modifierGroups, {
      id: uid(), name: "Logo treatment", type: "Choice", isRequired: false,
      minimumSelections: 0, maximumSelections: 1, displayOrder: draft.modifierGroups.length,
      isActive: true, values: []
    }] });
  }
  function updateModifier(id: string, value: Partial<AdminProductModifierGroup>) {
    patch({ modifierGroups: draft.modifierGroups.map(group => group.id === id ? { ...group, ...value } : group) });
  }
  function addModifierValue(groupId: string) {
    const group = draft.modifierGroups.find(value => value.id === groupId);
    if (!group) return;
    updateModifier(groupId, { values: [...group.values, {
      id: uid(), name: `Choice ${group.values.length + 1}`, priceAdjustmentMinor: 0,
      colorHex: null, overlayMediaAssetId: null, displayOrder: group.values.length, isActive: true
    }] });
  }
  function addVisualizerLayer(media: AdminProductMedia) {
    patch({ visualizerLayers: [...draft.visualizerLayers, {
      id: uid(), mediaAssetId: media.mediaAssetId, productOptionValueId: null,
      productModifierValueId: null, xPercent: 25, yPercent: 25,
      widthPercent: 50, heightPercent: 50, zIndex: draft.visualizerLayers.length + 1,
      blendMode: "normal"
    }] });
  }
  function updateLayer(id: string, value: Partial<AdminProductVisualizerLayer>) {
    patch({ visualizerLayers: draft.visualizerLayers.map(layer => layer.id === id ? { ...layer, ...value } : layer) });
  }

  async function save() {
    setMessage(null); setReferenceId(null);
    const currentErrors = validateStoreProductDraft(draft);
    if (currentErrors.length) { setMessage(currentErrors[0]); return; }
    setSaving(true);
    try {
      const response = await fetch(item ? `/api/admin/store/products/${item.id}` : "/api/admin/store/products", {
        method: item ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(toRequest(draft))
      });
      if (redirectForAdminResponse(response)) return;
      const body = await response.json() as AdminStoreProduct & { message?: string; referenceId?: string; errors?: Record<string, string[]> };
      if (!response.ok) {
        setMessage(body.message ?? Object.values(body.errors ?? {}).flat()[0] ?? "The product could not be saved.");
        setReferenceId(response.status >= 500 ? validSupportReference(body.referenceId) : null);
        return;
      }
      setMessage("Product saved successfully.");
      if (!item) router.replace(`/admin/store/products/${body.id}/edit?saved=created`);
      else router.refresh();
    } catch {
      setMessage("The product could not be saved. Try again.");
    } finally { setSaving(false); }
  }

  return <div className="space-y-5">
    <FormNotice message={message} success={message?.includes("successfully")} referenceId={referenceId}/>
    <nav aria-label="Product setup progress" className="overflow-x-auto border border-slate-200 bg-white p-2">
      <ol className="flex min-w-[650px] gap-1">{steps.map((label, index) => <li key={label} className="flex-1">
        <button type="button" onClick={() => setStep(index)} aria-current={step === index ? "step" : undefined}
          className={`flex min-h-12 w-full items-center justify-center gap-2 px-3 text-sm font-black ${step === index ? "bg-track-ink text-white" : index < step ? "bg-emerald-50 text-emerald-900" : "text-slate-500 hover:bg-slate-50"}`}>
          {index < step ? <Check size={17}/> : <span>{index + 1}</span>}{label}
        </button>
      </li>)}</ol>
    </nav>

    {step === 0 && <section className="grid gap-5 border border-slate-200 bg-white p-5 sm:grid-cols-2 sm:p-6">
      <Heading title="Product basics" text="Name, price, publishing, and where the item appears."/>
      <Field label="Product name" value={draft.name} onChange={name => patch({ name })} required/>
      <label className="text-sm font-bold">Category<select value={draft.categoryId ?? ""} onChange={event => patch({ categoryId: event.target.value || null })} className={input}><option value="">Uncategorized</option>{categories.filter(value => value.isActive).map(value => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label>
      <label className="text-sm font-bold">Base price (USD)<input type="number" min="0" step="0.01" value={(draft.basePriceMinor / 100).toFixed(2)} onChange={event => patch({ basePriceMinor: Math.max(0, Math.round(Number(event.target.value) * 100)) })} className={input}/></label>
      <label className="text-sm font-bold">Status<select value={draft.status} onChange={event => patch({ status: event.target.value as StoreProductStatus })} className={input}><option>Draft</option><option>Published</option><option>Archived</option></select></label>
      <Field label="Short description" value={draft.shortDescription} onChange={shortDescription => patch({ shortDescription })} className="sm:col-span-2"/>
      <label className="text-sm font-bold sm:col-span-2">Full description<textarea rows={6} value={draft.description} onChange={event => patch({ description: event.target.value })} className={`${input} py-3`}/></label>
      <CheckField label="Feature this product" checked={draft.isFeatured} onChange={isFeatured => patch({ isFeatured })}/>
      <CheckField label="Allow special requests" checked={draft.allowsSpecialRequests} onChange={allowsSpecialRequests => patch({ allowsSpecialRequests })}/>
    </section>}

    {step === 1 && <section className="border border-slate-200 bg-white p-5 sm:p-6">
      <Heading title="Product media" text="Choose real product photos and standardized visualizer layers from the media library."/>
      <div className="mt-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{draft.media.map((media, index) => <article key={media.id} className="border border-slate-200 p-3">
        <img src={media.publicUrl} alt="" className="aspect-[4/3] w-full bg-slate-100 object-contain"/>
        <p className="mt-2 truncate text-sm font-black">{media.title}</p>
        <select aria-label={`Role for ${media.title}`} value={media.role} onChange={event => patch({ media: draft.media.map(value => value.id === media.id ? { ...value, role: event.target.value as ProductMediaRole } : value) })} className={`${input} mt-2`}>
          <option value="Gallery">Gallery</option><option value="MockupBase">Mockup base</option><option value="LogoOverlay">Logo overlay</option>
        </select>
        <button type="button" onClick={() => patch({ media: draft.media.filter(value => value.id !== media.id).map((value, order) => ({ ...value, displayOrder: order })) })} className="mt-3 inline-flex items-center gap-2 text-sm font-bold text-red-700"><Trash2 size={16}/>Remove</button>
        {index === 0 && <span className="ml-3 text-xs font-bold text-slate-500">Primary image</span>}
      </article>)}
        <button type="button" onClick={() => setShowMedia(value => !value)} className="flex min-h-48 flex-col items-center justify-center gap-3 border-2 border-dashed border-slate-300 text-sm font-black hover:border-track-red hover:text-track-red"><ImagePlus size={28}/>Add from media</button>
      </div>
      {showMedia && <div className="mt-5"><MediaOptionBrowser excludedIds={draft.media.map(value => value.mediaAssetId)} onSelect={addMedia}/></div>}
    </section>}

    {step === 2 && <section className="space-y-5">
      <div className="border border-slate-200 bg-white p-5 sm:p-6"><Heading title="Tracked options" text="Sizes and garment colors create concrete SKUs whose stock is counted."/>
        <div className="mt-5 space-y-4">{draft.options.map(option => <article key={option.id} className="border-l-4 border-track-field bg-slate-50 p-4">
          <div className="flex flex-wrap items-end gap-3"><Field label="Option name" value={option.name} onChange={name => updateOption(option.id, { name })} className="min-w-48 flex-1"/>
            <CheckField label="Tracked inventory option" checked={option.isTracked} onChange={isTracked => updateOption(option.id, { isTracked })}/>
            <button type="button" onClick={() => patch({ options: draft.options.filter(value => value.id !== option.id) })} className="h-11 px-3 text-red-700" aria-label={`Remove ${option.name}`}><Trash2 size={18}/></button>
          </div>
          <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">{option.values.map(value => <div key={value.id} className="flex gap-2"><input aria-label={`${option.name} value`} value={value.name} onChange={event => updateOption(option.id, { values: option.values.map(item => item.id === value.id ? { ...item, name: event.target.value } : item) })} className={`${input} mt-0`}/><button type="button" onClick={() => updateOption(option.id, { values: option.values.filter(item => item.id !== value.id) })} aria-label={`Remove ${value.name}`} className="w-11 border border-slate-300"><Trash2 className="mx-auto" size={16}/></button></div>)}</div>
          <button type="button" onClick={() => addOptionValue(option.id)} className="mt-3 inline-flex items-center gap-2 text-sm font-bold text-track-red"><Plus size={16}/>Add value</button>
        </article>)}</div>
        <div className="mt-4 flex flex-wrap gap-3"><button type="button" onClick={addOption} className={secondary}><Plus size={17}/>Add option</button><button type="button" onClick={generateVariants} className={primary}><WandSparkles size={17}/>Generate variant matrix</button></div>
      </div>
      <div className="overflow-x-auto border border-slate-200 bg-white p-5 sm:p-6"><Heading title="Variant matrix" text="SKU and threshold changes apply here. Stock quantity changes belong in Inventory."/>
        <table className="mt-5 w-full min-w-[760px] text-left"><thead className="border-b text-xs uppercase text-slate-500"><tr><th className="p-3">Variant</th><th className="p-3">SKU</th><th className="p-3">Price override</th><th className="p-3">Low stock at</th><th className="p-3">Available</th><th className="p-3">Active</th></tr></thead>
          <tbody className="divide-y">{draft.variants.map(variant => <tr key={variant.id}><td className="p-3"><input aria-label="Variant name" value={variant.name} onChange={event => patch({ variants: draft.variants.map(value => value.id === variant.id ? { ...value, name: event.target.value } : value) })} className={`${input} mt-0`}/></td><td className="p-3"><input aria-label={`SKU for ${variant.name}`} value={variant.sku} onChange={event => patch({ variants: draft.variants.map(value => value.id === variant.id ? { ...value, sku: event.target.value } : value) })} className={`${input} mt-0`}/></td><td className="p-3"><input aria-label={`Price override for ${variant.name}`} type="number" min="0" step=".01" value={variant.priceOverrideMinor == null ? "" : (variant.priceOverrideMinor / 100).toFixed(2)} onChange={event => patch({ variants: draft.variants.map(value => value.id === variant.id ? { ...value, priceOverrideMinor: event.target.value === "" ? null : Math.round(Number(event.target.value) * 100) } : value) })} className={`${input} mt-0 w-32`}/></td><td className="p-3"><input aria-label={`Low stock threshold for ${variant.name}`} type="number" min="0" value={variant.lowStockThreshold} onChange={event => patch({ variants: draft.variants.map(value => value.id === variant.id ? { ...value, lowStockThreshold: Math.max(0, Number(event.target.value)) } : value) })} className={`${input} mt-0 w-24`}/></td><td className="p-3 font-black">{variant.availableQuantity}</td><td className="p-3"><input aria-label={`${variant.name} active`} type="checkbox" checked={variant.isActive} onChange={event => patch({ variants: draft.variants.map(value => value.id === variant.id ? { ...value, isActive: event.target.checked } : value) })} className="h-5 w-5 accent-track-red"/></td></tr>)}</tbody>
        </table>{!draft.variants.length && <p className="mt-5 border border-dashed p-6 text-center text-sm text-slate-500">Add option values, then generate the variant matrix.</p>}
      </div>
    </section>}

    {step === 3 && <div className="space-y-5"><section className="border border-slate-200 bg-white p-5 sm:p-6">
      <Heading title="Untracked customizations" text="Logo treatments and optional name/number choices change configuration and price without splitting inventory."/>
      <div className="mt-5 space-y-4">{draft.modifierGroups.map(group => <article key={group.id} className="border-l-4 border-track-red bg-slate-50 p-4">
        <div className="grid gap-3 sm:grid-cols-3"><Field label="Group name" value={group.name} onChange={name => updateModifier(group.id, { name })}/><label className="text-sm font-bold">Type<select value={group.type} onChange={event => updateModifier(group.id, { type: event.target.value as ProductModifierType })} className={input}><option>Choice</option><option>Color</option><option>ShortText</option><option>Number</option></select></label><CheckField label="Required" checked={group.isRequired} onChange={isRequired => updateModifier(group.id, { isRequired, minimumSelections: isRequired ? 1 : 0 })}/></div>
        {group.type === "Choice" || group.type === "Color" ? <div className="mt-4 space-y-2">{group.values.map(value => <div key={value.id} className="grid gap-2 sm:grid-cols-[1fr_160px_44px]"><input aria-label="Customization choice" value={value.name} onChange={event => updateModifier(group.id, { values: group.values.map(item => item.id === value.id ? { ...item, name: event.target.value } : item) })} className={`${input} mt-0`}/><input aria-label={`Surcharge for ${value.name}`} type="number" min="0" step=".01" value={(value.priceAdjustmentMinor / 100).toFixed(2)} onChange={event => updateModifier(group.id, { values: group.values.map(item => item.id === value.id ? { ...item, priceAdjustmentMinor: Math.max(0, Math.round(Number(event.target.value) * 100)) } : item) })} className={`${input} mt-0`}/><button type="button" onClick={() => updateModifier(group.id, { values: group.values.filter(item => item.id !== value.id) })} className="border border-slate-300 text-red-700"><Trash2 className="mx-auto" size={16}/></button></div>)}<button type="button" onClick={() => addModifierValue(group.id)} className="inline-flex items-center gap-2 text-sm font-bold text-track-red"><Plus size={16}/>Add choice</button></div> : <p className="mt-4 text-sm text-slate-600">Customer input will be reviewed before production. Surcharge configuration is finalized in checkout phase.</p>}
        <button type="button" onClick={() => patch({ modifierGroups: draft.modifierGroups.filter(value => value.id !== group.id) })} className="mt-4 inline-flex items-center gap-2 text-sm font-bold text-red-700"><Trash2 size={16}/>Remove group</button>
      </article>)}</div>
      <button type="button" onClick={addModifier} className={`${secondary} mt-4`}><Plus size={17}/>Add customization</button>
    </section>
      <section className="border border-slate-200 bg-white p-5 sm:p-6">
        <Heading title="Visualizer placement" text="Position approved transparent layers as percentages so previews remain deterministic on every screen size."/>
        <div className="mt-5 grid gap-4 lg:grid-cols-[minmax(240px,.65fr)_1.35fr]">
          <div><h3 className="text-sm font-black uppercase tracking-wide text-slate-500">Available overlay media</h3><div className="mt-3 grid grid-cols-2 gap-3">{draft.media.filter(media => media.role === "LogoOverlay").map(media => <button key={media.id} type="button" disabled={draft.visualizerLayers.some(layer => layer.mediaAssetId === media.mediaAssetId)} onClick={() => addVisualizerLayer(media)} className="border border-slate-200 p-2 text-left disabled:opacity-40"><img src={media.publicUrl} alt="" className="aspect-square w-full bg-slate-100 object-contain"/><span className="mt-2 block truncate text-xs font-black">{media.title}</span><span className="text-xs text-track-red">Add layer</span></button>)}</div>{!draft.media.some(media => media.role === "LogoOverlay") && <p className="mt-3 border border-dashed p-4 text-sm text-slate-500">Assign an image the “Logo overlay” role in Media to position it here.</p>}</div>
          <div><h3 className="text-sm font-black uppercase tracking-wide text-slate-500">Placed layers</h3><div className="mt-3 space-y-3">{draft.visualizerLayers.map(layer => { const media = draft.media.find(value => value.mediaAssetId === layer.mediaAssetId); return <article key={layer.id} className="grid gap-3 border-l-4 border-track-field bg-slate-50 p-4 sm:grid-cols-5"><p className="font-black sm:col-span-5">{media?.title ?? "Visualizer layer"}</p><NumberField label="X %" value={layer.xPercent} onChange={xPercent => updateLayer(layer.id, { xPercent })}/><NumberField label="Y %" value={layer.yPercent} onChange={yPercent => updateLayer(layer.id, { yPercent })}/><NumberField label="Width %" value={layer.widthPercent} min={1} onChange={widthPercent => updateLayer(layer.id, { widthPercent })}/><NumberField label="Height %" value={layer.heightPercent} min={1} onChange={heightPercent => updateLayer(layer.id, { heightPercent })}/><NumberField label="Layer order" value={layer.zIndex} min={0} max={1000} onChange={zIndex => updateLayer(layer.id, { zIndex })}/><button type="button" onClick={() => patch({ visualizerLayers: draft.visualizerLayers.filter(value => value.id !== layer.id) })} className="inline-flex items-center gap-2 text-sm font-bold text-red-700 sm:col-span-5"><Trash2 size={16}/>Remove layer</button></article>; })}</div>{!draft.visualizerLayers.length && <p className="mt-3 border border-dashed p-5 text-sm text-slate-500">No interactive overlay placement is configured yet.</p>}</div>
        </div>
      </section>
    </div>}

    {step === 4 && <section className="grid gap-5 lg:grid-cols-[minmax(0,1.2fr)_minmax(320px,.8fr)]">
      <div className="border border-slate-200 bg-white p-5 sm:p-6"><Heading title="Review product" text="A concise production and storefront preview before saving."/>
        <dl className="mt-5 grid gap-4 sm:grid-cols-2"><Summary label="Name" value={draft.name || "Not set"}/><Summary label="Price" value={price}/><Summary label="Status" value={draft.status}/><Summary label="Images" value={String(draft.media.length)}/><Summary label="Tracked variants" value={String(draft.variants.length)}/><Summary label="Customizations" value={String(draft.modifierGroups.length)}/></dl>
        {errors.length > 0 && <div role="alert" className="mt-5 border-l-4 border-amber-500 bg-amber-50 p-4"><p className="font-black text-amber-950">Before this product can be saved</p><ul className="mt-2 list-disc pl-5 text-sm text-amber-900">{errors.map(error => <li key={error}>{error}</li>)}</ul></div>}
      </div>
      <div className="border border-slate-200 bg-white p-5 sm:p-6"><p className="text-xs font-black uppercase tracking-[.18em] text-track-red">Customer preview</p>{draft.media[0] ? <img src={draft.media[0].publicUrl} alt="" className="mt-4 aspect-square w-full bg-slate-100 object-contain"/> : <div className="mt-4 grid aspect-square place-items-center bg-slate-100 text-sm text-slate-500">Product image</div>}<h2 className="mt-4 text-2xl font-black">{draft.name || "Product name"}</h2><p className="mt-1 text-xl font-black text-track-red">{price}</p><p className="mt-3 text-sm leading-6 text-slate-600">{draft.shortDescription || "Short product description"}</p></div>
    </section>}

    <footer className="sticky bottom-0 z-10 flex flex-wrap items-center justify-between gap-3 border border-slate-300 bg-white p-3 shadow-lg">
      <div className="flex gap-2"><Link href="/admin/store/products" className={secondary}><ArrowLeft size={17}/>Products</Link>{step > 0 && <button type="button" onClick={() => setStep(value => value - 1)} className={secondary}>Back</button>}</div>
      <div className="flex gap-2">{step < steps.length - 1 && <button type="button" onClick={() => setStep(value => value + 1)} className={secondary}>Next<ArrowRight size={17}/></button>}<button type="button" disabled={saving || !canPublish} onClick={() => void save()} className={primary}>{saving ? <LoaderCircle className="animate-spin" size={18}/> : <Save size={18}/>}Save product</button></div>
    </footer>
  </div>;
}

function fromItem(item?: AdminStoreProduct): StoreProductDraft {
  return item ? {
    categoryId: item.categoryId, name: item.name, shortDescription: item.shortDescription ?? "",
    description: item.description ?? "", basePriceMinor: item.basePriceMinor, status: item.status,
    isFeatured: item.isFeatured, displayOrder: item.displayOrder, allowsSpecialRequests: item.allowsSpecialRequests,
    media: item.media, options: item.options, variants: item.variants,
    modifierGroups: item.modifierGroups, visualizerLayers: item.visualizerLayers
  } : {
    categoryId: null, name: "", shortDescription: "", description: "", basePriceMinor: 0,
    status: "Draft", isFeatured: false, displayOrder: 0, allowsSpecialRequests: false,
    media: [], options: [], variants: [], modifierGroups: [], visualizerLayers: []
  };
}
function toRequest(draft: StoreProductDraft) {
  return {
    ...draft,
    shortDescription: draft.shortDescription.trim() || null,
    description: draft.description.trim() || null,
    media: draft.media.map(({ id, mediaAssetId, role, altTextOverride, displayOrder }) => ({ id, mediaAssetId, role, altTextOverride, displayOrder })),
    options: draft.options.map(option => ({ id: option.id, name: option.name, isTracked: option.isTracked, displayOrder: option.displayOrder, isActive: option.isActive, values: option.values.map(value => ({ id: value.id, name: value.name, colorHex: value.colorHex, swatchMediaAssetId: value.swatchMediaAssetId, displayOrder: value.displayOrder, isActive: value.isActive })) })),
    variants: draft.variants.map(({ id, name, sku, priceOverrideMinor, lowStockThreshold, isActive, rowVersion, optionValueIds }) => ({ id, name, sku, priceOverrideMinor, lowStockThreshold, isActive, rowVersion: rowVersion || null, optionValueIds })),
    modifierGroups: draft.modifierGroups.map(group => ({ id: group.id, name: group.name, type: group.type, isRequired: group.isRequired, minimumSelections: group.minimumSelections, maximumSelections: group.maximumSelections, displayOrder: group.displayOrder, isActive: group.isActive, values: group.values.map(value => ({ id: value.id, name: value.name, priceAdjustmentMinor: value.priceAdjustmentMinor, colorHex: value.colorHex, overlayMediaAssetId: value.overlayMediaAssetId, displayOrder: value.displayOrder, isActive: value.isActive })) }))
  };
}
export function validateStoreProductDraft(draft: StoreProductDraft) {
  const errors: string[] = [];
  if (!draft.name.trim()) errors.push("Enter a product name.");
  if (draft.status === "Published" && !draft.variants.length) errors.push("Published products need at least one tracked variant.");
  if (draft.variants.some(value => !value.name.trim() || !value.sku.trim())) errors.push("Every variant needs a name and SKU.");
  if (new Set(draft.variants.map(value => value.sku.trim().toLowerCase())).size !== draft.variants.length) errors.push("Variant SKUs must be unique.");
  if (draft.status === "Published" && !draft.media.length) errors.push("Published products need at least one image.");
  if (draft.status === "Published" && !draft.variants.some(value => value.isActive)) errors.push("Published products need an active variant.");
  return errors;
}
function Heading({ title, text }: { title: string; text: string }) { return <header className="sm:col-span-2"><h2 className="text-xl font-black">{title}</h2><p className="mt-1 text-sm leading-6 text-slate-600">{text}</p></header>; }
function Field({ label, value, onChange, required, className = "" }: { label: string; value: string; onChange: (value: string) => void; required?: boolean; className?: string }) { return <label className={`text-sm font-bold ${className}`}>{label}{required && <span className="text-track-red"> *</span>}<input value={value} onChange={event => onChange(event.target.value)} className={input}/></label>; }
function CheckField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) { return <label className="flex min-h-11 items-center gap-3 text-sm font-bold"><input type="checkbox" checked={checked} onChange={event => onChange(event.target.checked)} className="h-5 w-5 accent-track-red"/>{label}</label>; }
function Summary({ label, value }: { label: string; value: string }) { return <div><dt className="text-xs font-black uppercase tracking-wider text-slate-500">{label}</dt><dd className="mt-1 text-lg font-black">{value}</dd></div>; }
function NumberField({ label, value, onChange, min = 0, max = 100 }: { label: string; value: number; onChange: (value:number)=>void; min?:number; max?:number }) { return <label className="text-xs font-black uppercase text-slate-500">{label}<input type="number" min={min} max={max} value={value} onChange={event => onChange(Math.min(max, Math.max(min, Number(event.target.value))))} className={`${input} mt-1`}/></label>; }
const input = "mt-2 min-h-11 w-full border border-slate-300 bg-white px-3 font-normal outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20";
const primary = "inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-4 text-sm font-black text-white hover:bg-red-800 disabled:cursor-not-allowed disabled:opacity-50";
const secondary = "inline-flex min-h-11 items-center justify-center gap-2 border border-slate-300 bg-white px-4 text-sm font-black hover:border-track-red hover:text-track-red";
