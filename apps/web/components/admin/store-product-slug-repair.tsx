"use client";

import { Link2, LoaderCircle, X } from "lucide-react";
import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import { validSupportReference } from "@/lib/observability/support-reference";

export function isRepairableCopiedDraft(status: string, slug: string): boolean {
  return status === "Draft" && /-copy(?:-(?:[2-9]|[1-9]\d+))?$/i.test(slug);
}

export function StoreProductSlugRepair({ id, name, slug, status }: { id: string; name: string; slug: string; status: string }) {
  const router = useRouter();
  const dialog = useRef<HTMLDialogElement>(null);
  const confirmButton = useRef<HTMLButtonElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);

  if (!isRepairableCopiedDraft(status, slug)) return null;

  function openDialog() {
    setError(null);
    setReferenceId(null);
    dialog.current?.showModal();
    queueMicrotask(() => confirmButton.current?.focus());
  }

  async function repair() {
    setBusy(true);
    setError(null);
    setReferenceId(null);
    try {
      const response = await fetch(`/api/admin/store/products/${id}/regenerate-slug`, { method: "POST" });
      if (redirectForAdminResponse(response)) return;
      const result = await response.json().catch(() => ({})) as { message?: string; title?: string; referenceId?: string; errors?: Record<string, string[]> };
      if (!response.ok) {
        setError(result.errors?.Slug?.[0] ?? result.errors?.slug?.[0] ?? result.message ?? result.title ?? "The draft URL could not be repaired.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
        return;
      }
      dialog.current?.close();
      router.refresh();
    } catch {
      setError("The draft URL could not be repaired. Check the connection and try again.");
    } finally {
      setBusy(false);
    }
  }

  return <section className="mb-6 border-l-4 border-amber-500 bg-amber-50 px-4 py-4 text-sm text-amber-950">
    <div className="flex flex-wrap items-center justify-between gap-4">
      <div><p className="font-black">Copied draft URL</p><p className="mt-1">Save the final product name, then replace <code>/{slug}</code> with a URL generated from that name.</p></div>
      <button type="button" onClick={openDialog} className="inline-flex min-h-10 items-center gap-2 border border-amber-700 bg-white px-4 font-bold hover:border-track-red focus:outline-none focus:ring-2 focus:ring-track-red"><Link2 size={17}/>Repair draft URL</button>
    </div>
    <dialog ref={dialog} onClose={() => { setError(null); setReferenceId(null); }} className="w-[min(92vw,520px)] border-0 p-0 shadow-2xl backdrop:bg-black/55">
      <div className="border-t-4 border-track-red bg-white p-6 text-left text-track-ink">
        <div className="flex items-start justify-between gap-4"><div><h2 className="text-xl font-black">Change this draft URL?</h2><p className="mt-2 leading-6 text-slate-600">The URL will be regenerated from the currently saved name <strong>{name}</strong>. The previous draft URL <code>/{slug}</code> will stop working immediately.</p></div><button type="button" disabled={busy} onClick={() => dialog.current?.close()} className="p-2 text-slate-500 hover:text-track-red" aria-label="Close URL repair confirmation"><X size={20}/></button></div>
        <p className="mt-4 border-l-4 border-amber-500 bg-amber-50 px-3 py-2 text-sm font-semibold">Confirm the product name has been saved before continuing. Published product URLs cannot be changed.</p>
        {error && <div role="alert" className="mt-4 border-l-4 border-track-red bg-red-50 px-3 py-2 text-sm font-semibold text-red-900">{error}<SupportReference referenceId={referenceId}/></div>}
        <div className="mt-6 flex flex-wrap justify-end gap-3"><button type="button" disabled={busy} onClick={() => dialog.current?.close()} className="min-h-10 border border-slate-300 px-4 text-sm font-bold">Keep current URL</button><button ref={confirmButton} type="button" disabled={busy} onClick={() => void repair()} className="inline-flex min-h-10 items-center gap-2 bg-track-red px-4 text-sm font-bold text-white disabled:opacity-60">{busy ? <LoaderCircle size={17} className="animate-spin"/> : <Link2 size={17}/>} {busy ? "Repairing…" : "Regenerate draft URL"}</button></div>
      </div>
    </dialog>
  </section>;
}
