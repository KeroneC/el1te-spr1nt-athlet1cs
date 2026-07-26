"use client";

import { Archive, Copy, LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { redirectForAdminResponse } from "@/lib/admin/client-response";

export function StoreProductActions({ id, name, archived }: { id: string; name: string; archived: boolean }) {
  const router = useRouter();
  const [working, setWorking] = useState<"copy" | "archive" | null>(null);
  async function run(action: "copy" | "archive") {
    if (action === "archive" && !window.confirm(`Archive ${name}? It will no longer be publishable or featured.`)) return;
    setWorking(action);
    try {
      const response = await fetch(
        action === "copy" ? `/api/admin/store/products/${id}/duplicate` : `/api/admin/store/products/${id}`,
        { method: action === "copy" ? "POST" : "DELETE" }
      );
      if (redirectForAdminResponse(response)) return;
      if (!response.ok) window.alert("The product action could not be completed.");
      else if (action === "copy") {
        const product = await response.json() as { id: string };
        router.push(`/admin/store/products/${product.id}/edit`);
      } else router.refresh();
    } finally { setWorking(null); }
  }
  return <div className="flex justify-end gap-2">
    <button type="button" onClick={() => void run("copy")} disabled={working !== null} aria-label={`Duplicate ${name}`} title="Duplicate" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 hover:border-track-red">{working === "copy" ? <LoaderCircle className="animate-spin" size={17}/> : <Copy size={17}/>}</button>
    {!archived && <button type="button" onClick={() => void run("archive")} disabled={working !== null} aria-label={`Archive ${name}`} title="Archive" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 text-red-700 hover:border-red-600">{working === "archive" ? <LoaderCircle className="animate-spin" size={17}/> : <Archive size={17}/>}</button>}
  </div>;
}
