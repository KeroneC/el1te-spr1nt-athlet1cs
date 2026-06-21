"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { LoaderCircle, Trash2, X } from "lucide-react";

export function DeleteAnnouncementButton({ id, title }: { id: string; title: string }) {
  const router = useRouter();
  const dialog = useRef<HTMLDialogElement>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function remove() {
    setSubmitting(true); setError(null);
    try {
      const response = await fetch(`/api/admin/announcements/${encodeURIComponent(id)}`, { method: "DELETE" });
      if (response.status === 401) { window.location.assign("/api/admin-session/logout?reason=expired"); return; }
      if (response.status === 403) { window.location.assign("/admin/access-denied"); return; }
      if (!response.ok) { const body = await response.json() as { message?: string }; setError(body.message ?? "Announcement could not be deleted."); return; }
      dialog.current?.close(); router.refresh();
    } catch { setError("Announcement could not be deleted. Try again."); }
    finally { setSubmitting(false); }
  }

  return <><button type="button" onClick={() => dialog.current?.showModal()} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300 text-slate-600 hover:border-track-red hover:text-track-red focus:outline-none focus:ring-2 focus:ring-track-red" aria-label={`Delete ${title}`} title="Delete"><Trash2 size={17} /></button>
    <dialog ref={dialog} onClose={() => setError(null)} className="w-[min(92vw,480px)] border-0 p-0 shadow-2xl backdrop:bg-black/55"><div className="border-t-4 border-track-red bg-white p-6"><div className="flex items-start justify-between gap-4"><div><h2 className="text-xl font-black text-track-ink">Delete announcement?</h2><p className="mt-2 text-sm leading-6 text-slate-600"><strong>{title}</strong> will be permanently deleted. This cannot be undone.</p></div><button type="button" onClick={() => dialog.current?.close()} className="p-2 text-slate-500 hover:text-track-red" aria-label="Close confirmation"><X size={20} /></button></div>{error && <p role="alert" className="mt-4 bg-red-50 px-3 py-2 text-sm font-semibold text-red-800">{error}</p>}<div className="mt-6 flex justify-end gap-3"><button type="button" disabled={submitting} onClick={() => dialog.current?.close()} className="min-h-10 border border-slate-300 px-4 text-sm font-bold text-track-ink">Cancel</button><button type="button" disabled={submitting} onClick={remove} className="inline-flex min-h-10 items-center gap-2 bg-track-red px-4 text-sm font-bold text-white disabled:opacity-60">{submitting ? <LoaderCircle size={17} className="animate-spin" /> : <Trash2 size={17} />}{submitting ? "Deleting..." : "Delete permanently"}</button></div></div></dialog></>;
}
