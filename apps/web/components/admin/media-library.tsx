"use client";
/* eslint-disable @next/next/no-img-element */

import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { CheckCircle2, ImagePlus, LoaderCircle, RotateCcw, Trash2, Upload, X } from "lucide-react";
import type { AdminGalleryAlbumListItem, AdminMediaAsset } from "@/lib/admin/types";
import { MAX_MEDIA_FILES, MEDIA_UPLOAD_CONCURRENCY, runWithConcurrency, titleFromFileName, validateMediaFile } from "@/lib/admin/media-upload";
import type { FieldErrors } from "@/lib/admin/validation";
import { FormNotice } from "./form-controls";

type UploadStatus = "pending" | "uploading" | "success" | "error";
type UploadItem = {
  id: string;
  file: File;
  previewUrl: string;
  title: string;
  altText: string;
  caption: string;
  status: UploadStatus;
  message: string;
  assetId?: string;
};

export function MediaUploadForm({ albums = [] }: { albums?: AdminGalleryAlbumListItem[] }) {
  const router = useRouter();
  const previews = useRef(new Set<string>());
  const [items, setItems] = useState<UploadItem[]>([]);
  const [albumId, setAlbumId] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [success, setSuccess] = useState(false);

  useEffect(() => () => { previews.current.forEach(url => URL.revokeObjectURL(url)); }, []);

  function addFiles(event: React.ChangeEvent<HTMLInputElement>) {
    const selected = Array.from(event.target.files ?? []);
    event.target.value = "";
    setMessage("");
    setSuccess(false);
    if (!selected.length) return;

    const availableSlots = MAX_MEDIA_FILES - items.length;
    const accepted = selected.slice(0, availableSlots);
    const invalid = accepted.filter(file => validateMediaFile(file));
    const valid = accepted.filter(file => !validateMediaFile(file));
    const next = valid.map((file, index): UploadItem => {
      const previewUrl = URL.createObjectURL(file);
      previews.current.add(previewUrl);
      return {
        id: `${file.name}-${file.size}-${file.lastModified}-${Date.now()}-${index}`,
        file,
        previewUrl,
        title: titleFromFileName(file.name),
        altText: "",
        caption: "",
        status: "pending",
        message: ""
      };
    });
    setItems(current => [...current, ...next]);

    const notices = [];
    if (selected.length > availableSlots) notices.push(`Only ${MAX_MEDIA_FILES} images can be queued at once.`);
    if (invalid.length) notices.push(`${invalid.length} unsupported or oversized image${invalid.length === 1 ? " was" : "s were"} skipped.`);
    if (notices.length) setMessage(notices.join(" "));
  }

  function updateItem(id: string, changes: Partial<UploadItem>) {
    setItems(current => current.map(item => item.id === id ? { ...item, ...changes } : item));
  }

  function removeItem(item: UploadItem) {
    if (item.status === "uploading") return;
    URL.revokeObjectURL(item.previewUrl);
    previews.current.delete(item.previewUrl);
    setItems(current => current.filter(candidate => candidate.id !== item.id));
  }

  function clearCompleted() {
    setItems(current => {
      current.filter(item => item.status === "success").forEach(item => {
        URL.revokeObjectURL(item.previewUrl);
        previews.current.delete(item.previewUrl);
      });
      return current.filter(item => item.status !== "success");
    });
  }

  async function upload(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const candidates = items.filter(item => item.status !== "success");
    setMessage("");
    setSuccess(false);
    if (!candidates.length) {
      setMessage("Choose at least one image to upload.");
      return;
    }

    const invalid = candidates.filter(item => !item.title.trim() || !item.altText.trim());
    invalid.forEach(item => {
      const missing = [!item.title.trim() && "Title", !item.altText.trim() && "Alt text"].filter(Boolean);
      updateItem(item.id, { status: "error", message: `${missing.join(" and ")} ${missing.length === 1 ? "is" : "are"} required.` });
    });
    if (invalid.length) {
      setMessage(`Complete the highlighted metadata for ${invalid.length} image${invalid.length === 1 ? "" : "s"}.`);
      const first = invalid[0];
      const fieldId = !first.title.trim() ? `${first.id}-title` : `${first.id}-alt`;
      window.requestAnimationFrame(() => document.getElementById(fieldId)?.focus());
      return;
    }

    const selectedAlbum = albums.find(album => album.id === albumId);
    let completed = 0;
    let failed = 0;
    setBusy(true);
    await runWithConcurrency(candidates, MEDIA_UPLOAD_CONCURRENCY, async (item, index) => {
      updateItem(item.id, { status: "uploading", message: item.assetId ? "Adding to album..." : "Uploading..." });
      try {
        let assetId = item.assetId;
        if (!assetId) {
          const formData = new FormData();
          formData.set("file", item.file);
          formData.set("title", item.title.trim());
          formData.set("altText", item.altText.trim());
          formData.set("caption", item.caption.trim());
          const response = await fetch("/api/admin/media", { method: "POST", body: formData });
          const result = await response.json() as AdminMediaAsset & { message?: string; errors?: FieldErrors };
          if (!response.ok) {
            const fieldMessage = Object.values(result.errors ?? {}).flat()[0];
            throw new Error(fieldMessage ?? result.message ?? "Upload failed.");
          }
          assetId = result.id;
          updateItem(item.id, { assetId });
        }

        if (selectedAlbum) {
          const response = await fetch(`/api/admin/gallery-albums/${selectedAlbum.id}/media`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ mediaAssetId: assetId, displayOrder: selectedAlbum.imageCount + index })
          });
          if (!response.ok) {
            const result = await response.json() as { message?: string };
            throw new Error(result.message ?? "Uploaded, but could not be added to the album.");
          }
        }

        completed++;
        updateItem(item.id, { status: "success", message: selectedAlbum ? `Uploaded and added to ${selectedAlbum.title}.` : "Uploaded successfully." });
      } catch (error) {
        failed++;
        updateItem(item.id, { status: "error", message: error instanceof Error ? error.message : "Upload failed." });
      }
    });
    setBusy(false);
    setSuccess(failed === 0);
    setMessage(failed ? `${completed} completed and ${failed} failed. Correct or retry the failed items.` : `${completed} image${completed === 1 ? "" : "s"} uploaded successfully.`);
    router.refresh();
  }

  const completedCount = items.filter(item => item.status === "success").length;
  return <form onSubmit={upload} noValidate className="mb-6 border border-slate-200 bg-white p-5">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div><h2 className="text-lg font-black">Upload images</h2><p className="mt-1 text-sm text-slate-500">Queue up to {MAX_MEDIA_FILES} JPEG, PNG, or WebP images. Each image can be up to 10 MB.</p></div>
      {completedCount > 0 && <button type="button" onClick={clearCompleted} disabled={busy} className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><CheckCircle2 size={17} />Clear completed</button>}
    </div>
    {message && <div className="mt-4"><FormNotice message={message} success={success} /></div>}
    <div className="mt-5 grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(15rem,0.5fr)] md:items-end">
      <div><label htmlFor="media-files" className="mb-2 block text-sm font-bold text-track-ink">Images</label><input id="media-files" type="file" multiple accept="image/jpeg,image/png,image/webp" onChange={addFiles} disabled={busy || items.length >= MAX_MEDIA_FILES} className="min-h-11 w-full border border-slate-300 bg-white p-2 file:mr-3 file:border-0 file:bg-slate-100 file:px-3 file:py-1 file:font-bold" /><p className="mt-1 text-xs text-slate-500">{items.length} of {MAX_MEDIA_FILES} queued</p></div>
      <div><label htmlFor="upload-album" className="mb-2 block text-sm font-bold text-track-ink">Add uploads to album (optional)</label><select id="upload-album" value={albumId} onChange={event => setAlbumId(event.target.value)} disabled={busy} className="min-h-11 w-full border border-slate-300 bg-white px-3"><option value="">Media library only</option>{albums.map(album => <option key={album.id} value={album.id}>{album.title}</option>)}</select></div>
    </div>

    {items.length > 0 && <div className="mt-5 grid gap-4 lg:grid-cols-2">
      {items.map((item, index) => <article key={item.id} className={`grid min-w-0 gap-4 border p-4 sm:grid-cols-[8rem_minmax(0,1fr)] ${item.status === "error" ? "border-red-400 bg-red-50/40" : item.status === "success" ? "border-emerald-300 bg-emerald-50/40" : "border-slate-200"}`}>
        <div><img src={item.previewUrl} alt="" className="aspect-square w-full bg-slate-100 object-cover" /><p className="mt-2 truncate text-xs text-slate-500" title={item.file.name}>{item.file.name}</p><p className="text-xs text-slate-500">{(item.file.size / 1024 / 1024).toFixed(2)} MB</p></div>
        <div className="min-w-0">
          <div className="flex items-center justify-between gap-3"><p className="text-xs font-black uppercase tracking-wide text-slate-500">Image {index + 1}</p><button type="button" onClick={() => removeItem(item)} disabled={busy || item.status === "uploading"} aria-label={`Remove ${item.file.name}`} title="Remove from queue" className="grid h-9 w-9 place-items-center border border-slate-300 bg-white text-slate-600 disabled:opacity-40"><X size={16} /></button></div>
          <label className="mt-2 block text-sm font-bold">Title<input id={`${item.id}-title`} value={item.title} onChange={event => updateItem(item.id, { title: event.target.value, status: item.status === "error" ? "pending" : item.status, message: "" })} required maxLength={200} disabled={item.status === "uploading" || item.status === "success"} aria-invalid={item.status === "error" && !item.title.trim()} aria-describedby={item.status === "error" && !item.title.trim() ? `${item.id}-error` : undefined} className={`mt-1 min-h-10 w-full border bg-white px-3 font-normal outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20 ${item.status === "error" && !item.title.trim() ? "border-red-600 ring-2 ring-red-200" : "border-slate-300"}`} /></label>
          <label className="mt-3 block text-sm font-bold">Alt text<input id={`${item.id}-alt`} value={item.altText} onChange={event => updateItem(item.id, { altText: event.target.value, status: item.status === "error" ? "pending" : item.status, message: "" })} required maxLength={500} disabled={item.status === "uploading" || item.status === "success"} aria-invalid={item.status === "error" && !item.altText.trim()} aria-describedby={`${item.id}-alt-hint${item.status === "error" && !item.altText.trim() ? ` ${item.id}-error` : ""}`} className={`mt-1 min-h-10 w-full border bg-white px-3 font-normal outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20 ${item.status === "error" && !item.altText.trim() ? "border-red-600 ring-2 ring-red-200" : "border-slate-300"}`} /><span id={`${item.id}-alt-hint`} className="mt-1 block text-xs font-normal text-slate-500">Describe what matters in the image for screen-reader users.</span></label>
          <label className="mt-3 block text-sm font-bold">Caption (optional)<input value={item.caption} onChange={event => updateItem(item.id, { caption: event.target.value })} maxLength={1000} disabled={item.status === "uploading" || item.status === "success"} className="mt-1 min-h-10 w-full border border-slate-300 bg-white px-3 font-normal outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" /></label>
          {item.message && <p id={`${item.id}-error`} role={item.status === "error" ? "alert" : "status"} className={`mt-3 flex items-center gap-2 text-sm font-semibold ${item.status === "error" ? "text-red-800" : item.status === "success" ? "text-emerald-800" : "text-slate-600"}`}>{item.status === "uploading" && <LoaderCircle className="animate-spin" size={16} />}{item.status === "success" && <CheckCircle2 size={16} />}{item.status === "error" && item.assetId && <RotateCcw size={16} />}{item.message}</p>}
        </div>
      </article>)}
    </div>}

    <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-5"><p className="text-sm text-slate-500"><ImagePlus className="mr-1 inline" size={17} />Uploads run three at a time and keep successful images when another fails.</p><button disabled={busy || !items.some(item => item.status !== "success")} className="inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-5 font-bold text-white disabled:opacity-50">{busy ? <LoaderCircle className="animate-spin" size={18} /> : <Upload size={18} />}{busy ? "Uploading queue..." : "Upload queue"}</button></div>
  </form>;
}

export function MediaActions({asset}:{asset:AdminMediaAsset}) {
  const router=useRouter(); const [busy,setBusy]=useState(false);
  async function remove(){if(!confirm(`Delete ${asset.title}? This cannot be undone.`))return;setBusy(true);const r=await fetch(`/api/admin/media/${asset.id}`,{method:"DELETE"});setBusy(false);if(r.ok)router.refresh();else alert((await r.json()).message??"Could not delete media.");}
  return <button type="button" disabled={busy} onClick={remove} title="Delete media" aria-label={`Delete ${asset.title}`} className="h-9 w-9 border border-slate-300 text-red-700"><Trash2 className="mx-auto" size={16}/></button>;
}
