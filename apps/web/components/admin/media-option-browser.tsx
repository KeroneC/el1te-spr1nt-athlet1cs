"use client";
/* eslint-disable @next/next/no-img-element */

import { ChevronLeft, ChevronRight, Image as ImageIcon, Search } from "lucide-react";
import { useEffect, useState } from "react";
import type { AdminMediaAsset, PagedResult } from "@/lib/admin/types";
import { redirectForAdminResponse } from "@/lib/admin/client-response";

const PAGE_SIZE = 24;

export function MediaOptionBrowser({
  excludedIds = [],
  onSelect,
  selectLabel = "Choose",
  disabled = false
}: {
  excludedIds?: string[];
  onSelect: (asset: AdminMediaAsset) => void | Promise<void>;
  selectLabel?: string;
  disabled?: boolean;
}) {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<PagedResult<AdminMediaAsset> | null>(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");
  const excluded = new Set(excludedIds);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setQuery(search.trim());
      setPage(1);
    }, 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const controller = new AbortController();
    const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
    if (query) params.set("search", query);
    setLoading(true);
    setMessage("");
    void fetch(`/api/admin/media/options?${params}`, { signal: controller.signal })
      .then(async response => {
        if (redirectForAdminResponse(response)) throw new Error("Session expired.");
        if (!response.ok) throw new Error("Media could not be loaded.");
        return response.json() as Promise<PagedResult<AdminMediaAsset>>;
      })
      .then(setResult)
      .catch(error => {
        if (error instanceof DOMException && error.name === "AbortError") return;
        setMessage("Media could not be loaded. Try again.");
      })
      .finally(() => setLoading(false));
    return () => controller.abort();
  }, [page, query]);

  const assets = (result?.items ?? []).filter(asset => !excluded.has(asset.id));
  return <div className="border border-slate-200 bg-slate-50 p-3 sm:p-4">
    <label className="block text-sm font-bold text-track-ink" htmlFor="media-option-search">Search media</label>
    <div className="relative mt-2">
      <Search aria-hidden="true" className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" size={17} />
      <input id="media-option-search" type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Title, filename, or alt text" className="min-h-11 w-full border border-slate-300 bg-white pl-10 pr-3 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />
    </div>
    <div role="status" aria-live="polite" className="mt-2 min-h-5 text-xs text-slate-500">
      {loading ? "Loading media..." : message || `${result?.totalCount ?? 0} active image${result?.totalCount === 1 ? "" : "s"}`}
    </div>
    {!loading && !message && assets.length > 0 && <div className="mt-3 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      {assets.map(asset => <button key={asset.id} type="button" disabled={disabled} onClick={() => void onSelect(asset)} aria-label={`${selectLabel} ${asset.title}`} className="group min-w-0 border border-slate-200 bg-white p-2 text-left hover:border-track-red focus-visible:border-track-red disabled:cursor-wait disabled:opacity-50">
        <img src={asset.publicUrl} alt="" className="aspect-square w-full bg-slate-100 object-cover" />
        <span className="mt-2 block truncate text-sm font-bold text-track-ink">{asset.title}</span>
        <span className="mt-1 inline-flex items-center gap-1 text-xs font-semibold text-track-red"><ImageIcon size={14} />{selectLabel}</span>
      </button>)}
    </div>}
    {!loading && !message && assets.length === 0 && <p className="mt-3 border border-dashed border-slate-300 bg-white p-5 text-center text-sm text-slate-500">{query ? "No active media matches this search." : "No active media is available on this page."}</p>}
    {(result?.totalPages ?? 0) > 1 && <div className="mt-4 flex items-center justify-between gap-3 border-t border-slate-200 pt-3">
      <button type="button" disabled={page <= 1 || loading} onClick={() => setPage(current => current - 1)} className="inline-flex min-h-10 items-center gap-1 border border-slate-300 bg-white px-3 text-sm font-bold disabled:opacity-40"><ChevronLeft size={16} />Previous</button>
      <span className="text-sm font-semibold text-slate-600">Page {result?.page ?? page} of {result?.totalPages}</span>
      <button type="button" disabled={page >= (result?.totalPages ?? 1) || loading} onClick={() => setPage(current => current + 1)} className="inline-flex min-h-10 items-center gap-1 border border-slate-300 bg-white px-3 text-sm font-bold disabled:opacity-40">Next<ChevronRight size={16} /></button>
    </div>}
  </div>;
}
