"use client";
/* eslint-disable @next/next/no-img-element */

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LoaderCircle, Trash2, Upload } from "lucide-react";
import type { AdminMediaAsset } from "@/lib/admin/types";
import type { FieldErrors } from "@/lib/admin/validation";
import { Field, FormNotice, TextArea, fieldError, normalizeErrors } from "./form-controls";

export function MediaUploadForm() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [success, setSuccess] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [file, setFile] = useState<File | null>(null);
  const preview = file ? URL.createObjectURL(file) : null;

  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    const form = event.currentTarget;
    event.preventDefault();
    setBusy(true);
    setMessage("");
    setSuccess(false);
    setErrors({});

    try {
      const response = await fetch("/api/admin/media", { method: "POST", body: new FormData(form) });
      const result = await response.json() as AdminMediaAsset & { message?: string; errors?: FieldErrors };

      if (!response.ok) {
        const nextErrors = normalizeErrors(result.errors ?? {});
        setErrors(nextErrors);
        setMessage(result.message ?? "Upload failed.");
        const firstField = form.elements.namedItem(Object.keys(nextErrors)[0]);
        if (firstField instanceof HTMLElement) firstField.focus();
        return;
      }

      form.reset();
      setFile(null);
      setSuccess(true);
      setMessage(`${result.title} uploaded successfully.`);
      router.refresh();
    } catch {
      setMessage("Upload failed. Check your connection and try again.");
    } finally {
      setBusy(false);
    }
  }

  return <form onSubmit={submit} className="mb-6 grid gap-4 border border-slate-200 bg-white p-5 md:grid-cols-2">
    <div className="md:col-span-2"><h2 className="text-lg font-black">Upload image</h2><p className="text-sm text-slate-500">JPEG, PNG, or WebP, up to 10 MB.</p></div>
    {message && <div className="md:col-span-2"><FormNotice message={message} success={success} /></div>}
    <Field label="Image" name="file" type="file" required accept="image/jpeg,image/png,image/webp" onChange={event => setFile(event.target.files?.[0] ?? null)} error={fieldError(errors, "file")} hint={file ? `${file.name} - ${(file.size / 1024 / 1024).toFixed(2)} MB` : undefined} />
    {preview && <img src={preview} alt="Selected upload preview" className="h-32 w-full bg-slate-100 object-contain" />}
    <Field label="Title" name="title" required maxLength={200} error={fieldError(errors, "title")} />
    <Field label="Alt text" name="altText" required maxLength={500} error={fieldError(errors, "altText")} hint="Briefly describe the image for people using screen readers." />
    <TextArea label="Caption" name="caption" maxLength={1000} error={fieldError(errors, "caption")} />
    <button disabled={busy} className="inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-4 font-bold text-white md:justify-self-end">{busy ? <LoaderCircle className="animate-spin" size={18} /> : <Upload size={18} />}{busy ? "Uploading..." : "Upload"}</button>
  </form>;
}

export function MediaActions({asset}:{asset:AdminMediaAsset}) {
  const router=useRouter(); const [busy,setBusy]=useState(false);
  async function remove(){if(!confirm(`Delete ${asset.title}? This cannot be undone.`))return;setBusy(true);const r=await fetch(`/api/admin/media/${asset.id}`,{method:"DELETE"});setBusy(false);if(r.ok)router.refresh();else alert((await r.json()).message??"Could not delete media.");}
  return <button type="button" disabled={busy} onClick={remove} title="Delete media" aria-label={`Delete ${asset.title}`} className="h-9 w-9 border border-slate-300 text-red-700"><Trash2 className="mx-auto" size={16}/></button>;
}
