"use client";

import { Archive, Copy, LoaderCircle, X } from "lucide-react";
import { useRouter } from "next/navigation";
import { FormEvent, useRef, useState } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import { validSupportReference } from "@/lib/observability/support-reference";

type ApiProblem = {
  message?: string;
  title?: string;
  referenceId?: string;
  errors?: Record<string, string[]>;
};

export function validateDuplicateProductName(value: string, sourceName: string): string | null {
  const name = value.trim();
  if (!name) return "Enter a name for the new product.";
  if (name.length > 200) return "The product name cannot exceed 200 characters.";
  if (name.localeCompare(sourceName.trim(), undefined, { sensitivity: "accent" }) === 0)
    return "Enter a name that is different from the product being copied.";
  return null;
}

export function StoreProductActions({ id, name, archived }: { id: string; name: string; archived: boolean }) {
  const router = useRouter();
  const dialog = useRef<HTMLDialogElement>(null);
  const nameInput = useRef<HTMLInputElement>(null);
  const [working, setWorking] = useState<"copy" | "archive" | null>(null);
  const [copyName, setCopyName] = useState("");
  const [copyError, setCopyError] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);

  function openCopyDialog() {
    setCopyName("");
    setCopyError(null);
    setReferenceId(null);
    dialog.current?.showModal();
    queueMicrotask(() => nameInput.current?.focus());
  }

  async function duplicate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validation = validateDuplicateProductName(copyName, name);
    if (validation) {
      setCopyError(validation);
      nameInput.current?.focus();
      return;
    }

    setWorking("copy");
    setCopyError(null);
    setReferenceId(null);
    try {
      const response = await fetch(`/api/admin/store/products/${id}/duplicate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: copyName.trim() })
      });
      if (redirectForAdminResponse(response)) return;
      const result = await response.json().catch(() => ({})) as ApiProblem & { id?: string };
      if (!response.ok) {
        const fieldError = result.errors?.Name?.[0] ?? result.errors?.name?.[0];
        setCopyError(fieldError ?? result.message ?? result.title ?? "The product could not be duplicated.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
        nameInput.current?.focus();
        return;
      }
      if (!result.id) {
        setCopyError("The product was copied, but its draft could not be opened. Refresh the catalog and try again.");
        return;
      }
      dialog.current?.close();
      router.push(`/admin/store/products/${result.id}/edit`);
    } catch {
      setCopyError("The product could not be duplicated. Check the connection and try again.");
    } finally {
      setWorking(null);
    }
  }

  async function archive() {
    if (!window.confirm(`Archive ${name}? It will no longer be publishable or featured.`)) return;
    setWorking("archive");
    try {
      const response = await fetch(`/api/admin/store/products/${id}`, { method: "DELETE" });
      if (redirectForAdminResponse(response)) return;
      if (!response.ok) window.alert("The product action could not be completed.");
      else router.refresh();
    } finally {
      setWorking(null);
    }
  }

  return <>
    <div className="flex justify-end gap-2">
      <button type="button" onClick={openCopyDialog} disabled={working !== null} aria-label={`Duplicate ${name}`} title="Duplicate" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 hover:border-track-red focus:outline-none focus:ring-2 focus:ring-track-red"><Copy size={17}/></button>
      {!archived && <button type="button" onClick={() => void archive()} disabled={working !== null} aria-label={`Archive ${name}`} title="Archive" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 text-red-700 hover:border-red-600 focus:outline-none focus:ring-2 focus:ring-track-red">{working === "archive" ? <LoaderCircle className="animate-spin" size={17}/> : <Archive size={17}/>}</button>}
    </div>
    <dialog ref={dialog} onClose={() => { setCopyError(null); setReferenceId(null); }} className="w-[min(92vw,520px)] border-0 p-0 shadow-2xl backdrop:bg-black/55">
      <form onSubmit={duplicate} className="border-t-4 border-track-red bg-white p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-black text-track-ink">Name the copied product</h2>
            <p className="mt-2 text-sm leading-6 text-slate-600">Catalog details from <strong>{name}</strong> will be copied into a new draft. Its URL will be generated from the new name.</p>
          </div>
          <button type="button" onClick={() => dialog.current?.close()} disabled={working === "copy"} className="p-2 text-slate-500 hover:text-track-red" aria-label="Close duplicate product dialog"><X size={20}/></button>
        </div>
        <label htmlFor={`duplicate-name-${id}`} className="mt-5 block text-sm font-bold text-track-ink">New product name</label>
        <input ref={nameInput} id={`duplicate-name-${id}`} value={copyName} onChange={event => { setCopyName(event.target.value); if (copyError) setCopyError(null); }} maxLength={200} autoComplete="off" aria-invalid={copyError ? true : undefined} aria-describedby={copyError ? `duplicate-name-error-${id}` : `duplicate-name-help-${id}`} className="mt-2 min-h-11 w-full border border-slate-300 px-3 py-2 text-track-ink focus:border-track-red focus:outline-none focus:ring-2 focus:ring-track-red/30"/>
        <p id={`duplicate-name-help-${id}`} className="mt-2 text-xs text-slate-500">Use the final customer-facing name when possible. You can still edit the draft before publishing.</p>
        {copyError && <div id={`duplicate-name-error-${id}`} role="alert" className="mt-4 border-l-4 border-track-red bg-red-50 px-3 py-2 text-sm font-semibold text-red-900">{copyError}<SupportReference referenceId={referenceId}/></div>}
        <div className="mt-6 flex flex-wrap justify-end gap-3">
          <button type="button" disabled={working === "copy"} onClick={() => dialog.current?.close()} className="min-h-10 border border-slate-300 px-4 text-sm font-bold text-track-ink">Cancel</button>
          <button type="submit" disabled={working === "copy"} className="inline-flex min-h-10 items-center gap-2 bg-track-red px-4 text-sm font-bold text-white disabled:opacity-60">{working === "copy" ? <LoaderCircle size={17} className="animate-spin"/> : <Copy size={17}/>} {working === "copy" ? "Creating draft…" : "Create duplicate draft"}</button>
        </div>
      </form>
    </dialog>
  </>;
}
