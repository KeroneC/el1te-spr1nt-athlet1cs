"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, LoaderCircle, Save } from "lucide-react";
import Link from "next/link";
import type { AdminAnnouncement, AnnouncementWriteRequest } from "@/lib/admin/types";
import { validateAnnouncement, type FieldErrors } from "@/lib/admin/validation";

export function AnnouncementForm({ announcement }: { announcement?: AdminAnnouncement }) {
  const router = useRouter();
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (submitting) return;
    const data = new FormData(event.currentTarget);
    const request: AnnouncementWriteRequest = {
      title: String(data.get("title") ?? "").trim(), summary: String(data.get("summary") ?? "").trim(),
      body: String(data.get("body") ?? "").trim(), imageUrl: nullable(String(data.get("imageUrl") ?? "")),
      isFeatured: data.get("isFeatured") === "on", isPublished: data.get("isPublished") === "on",
      publishDateUtc: iso(String(data.get("publishDateUtc") ?? "")), expirationDateUtc: iso(String(data.get("expirationDateUtc") ?? ""))
    };
    const clientErrors = validateAnnouncement(request); setErrors(clientErrors); setMessage(null);
    if (Object.keys(clientErrors).length) return;
    setSubmitting(true);
    try {
      const url = announcement ? `/api/admin/announcements/${encodeURIComponent(announcement.id)}` : "/api/admin/announcements";
      const response = await fetch(url, { method: announcement ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
      if (response.status === 401) { window.location.assign("/api/admin-session/logout?reason=expired"); return; }
      if (response.status === 403) { window.location.assign("/admin/access-denied"); return; }
      const result = await response.json() as AdminAnnouncement & { message?: string; errors?: FieldErrors };
      if (!response.ok) { setErrors(normalizeErrors(result.errors ?? {})); setMessage(result.message ?? "The announcement could not be saved."); return; }
      if (!announcement) { router.replace(`/admin/announcements/${result.id}/edit?saved=created`); router.refresh(); return; }
      setMessage("Announcement saved successfully."); router.refresh();
    } catch { setMessage("The announcement could not be saved. Try again."); }
    finally { setSubmitting(false); }
  }

  return <form onSubmit={submit} noValidate className="space-y-6">
    {message && <div role="status" className={`border-l-4 px-4 py-3 text-sm font-semibold ${message.includes("success") ? "border-track-field bg-emerald-50 text-emerald-900" : "border-track-red bg-red-50 text-red-900"}`}>{message}</div>}
    <section className="border border-slate-200 bg-white p-5 sm:p-6" aria-labelledby="content-heading"><h2 id="content-heading" className="text-lg font-black text-track-ink">Content</h2><div className="mt-5 grid gap-5">
      <Field label="Title" name="title" required maxLength={200} defaultValue={announcement?.title} error={field(errors, "title")} />
      {announcement && <div><label className="mb-2 block text-sm font-bold text-track-ink">Public slug</label><div className="min-h-11 border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">/{announcement.slug}</div><p className="mt-1 text-xs text-slate-500">The backend preserves this slug when the title changes.</p></div>}
      <Field label="Summary" name="summary" required maxLength={200} defaultValue={announcement?.summary} error={field(errors, "summary")} />
      <div><label htmlFor="body" className="mb-2 block text-sm font-bold text-track-ink">Body</label><textarea id="body" name="body" required rows={10} defaultValue={announcement?.body} aria-invalid={Boolean(field(errors, "body"))} aria-describedby={field(errors, "body") ? "body-error" : undefined} className="w-full border border-slate-300 px-3 py-2 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{field(errors, "body") && <p id="body-error" className="mt-1 text-sm font-semibold text-red-700">{field(errors, "body")}</p>}</div>
      <Field label="Image URL (optional)" name="imageUrl" type="url" maxLength={500} defaultValue={announcement?.imageUrl ?? ""} error={field(errors, "imageUrl")} />
    </div></section>
    <section className="border border-slate-200 bg-white p-5 sm:p-6" aria-labelledby="publishing-heading"><h2 id="publishing-heading" className="text-lg font-black text-track-ink">Publishing</h2><p className="mt-1 text-sm text-slate-600">Drafts stay private. Future publish dates are scheduled; expired announcements are hidden publicly. Dates use your local timezone and are sent as UTC.</p><div className="mt-5 grid gap-5 sm:grid-cols-2"><DateField label="Publish date and time" name="publishDateUtc" value={toLocal(announcement?.publishDateUtc)} error={field(errors, "publishDateUtc")} /><DateField label="Expiration date and time" name="expirationDateUtc" value={toLocal(announcement?.expirationDateUtc)} error={field(errors, "expirationDateUtc")} /></div><div className="mt-5 flex flex-wrap gap-5"><Checkbox name="isPublished" label="Published" defaultChecked={announcement?.isPublished} /><Checkbox name="isFeatured" label="Featured" defaultChecked={announcement?.isFeatured} /></div></section>
    <div className="flex flex-wrap items-center justify-between gap-3"><Link href="/admin/announcements" className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-4 text-sm font-bold text-track-ink hover:border-track-red"><ArrowLeft size={17} />Back to announcements</Link><button type="submit" disabled={submitting} className="inline-flex min-h-11 items-center gap-2 bg-track-red px-5 text-sm font-bold text-white hover:bg-red-800 disabled:opacity-60">{submitting ? <LoaderCircle size={18} className="animate-spin" /> : <Save size={18} />}{submitting ? "Saving..." : announcement ? "Save changes" : "Create announcement"}</button></div>
  </form>;
}

function Field({ label, name, error, ...props }: { label: string; name: string; error?: string } & React.InputHTMLAttributes<HTMLInputElement>) { const id = `${name}-error`; return <div><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><input id={name} name={name} {...props} aria-invalid={Boolean(error)} aria-describedby={error ? id : undefined} className="min-h-11 w-full border border-slate-300 px-3 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{error && <p id={id} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>; }
function DateField({ label, name, value, error }: { label: string; name: string; value: string; error?: string }) { return <Field label={label} name={name} type="datetime-local" defaultValue={value} error={error} />; }
function Checkbox({ name, label, defaultChecked }: { name: string; label: string; defaultChecked?: boolean }) { return <label className="inline-flex min-h-10 items-center gap-3 text-sm font-bold text-track-ink"><input type="checkbox" name={name} defaultChecked={defaultChecked} className="h-5 w-5 accent-track-red" />{label}</label>; }
function nullable(value: string) { return value.trim() || null; }
function iso(value: string) { return value ? new Date(value).toISOString() : null; }
function toLocal(value?: string | null) { if (!value) return ""; const date = new Date(value); const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000); return local.toISOString().slice(0, 16); }
function field(errors: FieldErrors, name: string) { return errors[name]?.[0] ?? errors[name[0].toUpperCase() + name.slice(1)]?.[0]; }
function normalizeErrors(errors: FieldErrors) { return Object.fromEntries(Object.entries(errors).map(([key, value]) => [key[0].toLowerCase() + key.slice(1), value])); }
