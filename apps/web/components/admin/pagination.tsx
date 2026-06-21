import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";

export function Pagination({ page, totalPages, params }: { page: number; totalPages: number; params: URLSearchParams }) {
  if (totalPages <= 1) return null;
  const href = (target: number) => { const next = new URLSearchParams(params); next.set("page", String(target)); return `?${next}`; };
  return <nav aria-label="Announcement pages" className="mt-5 flex items-center justify-between border-t border-slate-200 pt-4"><Link aria-disabled={page <= 1} tabIndex={page <= 1 ? -1 : undefined} href={page <= 1 ? "#" : href(page - 1)} className={`inline-flex min-h-10 items-center gap-1 border px-3 text-sm font-bold ${page <= 1 ? "pointer-events-none border-slate-200 text-slate-400" : "border-slate-300 text-track-ink hover:border-track-red"}`}><ChevronLeft size={17} />Previous</Link><span className="text-sm font-semibold text-slate-600">Page {page} of {totalPages}</span><Link aria-disabled={page >= totalPages} tabIndex={page >= totalPages ? -1 : undefined} href={page >= totalPages ? "#" : href(page + 1)} className={`inline-flex min-h-10 items-center gap-1 border px-3 text-sm font-bold ${page >= totalPages ? "pointer-events-none border-slate-200 text-slate-400" : "border-slate-300 text-track-ink hover:border-track-red"}`}>Next<ChevronRight size={17} /></Link></nav>;
}
