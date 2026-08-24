/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import { Edit3, Plus } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { Badge, EmptyState, FilterActions, FilterSelect, FilterText } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { ResourceActionButton } from "@/components/admin/resource-action-button";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminAllAmericanYearListItem, ContentFilters, PagedResult } from "@/lib/admin/types";
import { buildListQuery } from "@/lib/admin/validation";

export default async function Page({ searchParams }: { searchParams: Promise<ContentFilters> }) {
  const filters = await searchParams;
  const query = buildListQuery(filters, ["search", "isPublished"]);
  const result = await getAdminList<PagedResult<AdminAllAmericanYearListItem>>(`/api/admin/all-americans?${query}`);
  const pagination = new URLSearchParams(query); pagination.delete("pageSize");
  const filtered = Boolean(filters.search || filters.isPublished);
  return <><PageHeader title="All-American archive" description="Publish annual Junior Olympic stories, media, athletes, and verified performances." action={{ href: "/admin/all-americans/new", label: "Create year", icon: Plus }} />
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_200px_auto] md:items-end"><FilterText value={filters.search} placeholder="Year, title, or summary" /><FilterSelect label="Status" name="isPublished" value={filters.isPublished} options={[["", "All"], ["true", "Published"], ["false", "Draft"]]} /><FilterActions clearHref="/admin/all-americans" filtered={filtered} /></form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[850px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Year</th><th>Athletes</th><th>Medals</th><th>Images</th><th>Details</th><th>Status</th><th className="p-4 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-200">{result.items.map((item) => <tr key={item.id}><td className="p-4"><div className="flex items-center gap-3">{item.heroImageUrl ? <img src={item.heroImageUrl} alt="" className="h-14 w-20 object-cover" /> : <div className="grid h-14 w-20 place-items-center bg-slate-100 text-xs">No hero</div>}<div><p className="font-black">{item.year}</p><p className="max-w-xs truncate text-xs text-slate-500">{item.title}</p></div></div></td><td>{item.athleteCount}</td><td>{item.medalCount}</td><td>{item.imageCount}</td><td><Badge tone={item.detailsComplete ? "green" : "neutral"}>{item.detailsComplete ? "Complete" : "Summary only"}</Badge></td><td><Badge tone={item.isPublished ? "green" : "neutral"}>{item.isPublished ? "Published" : "Draft"}</Badge></td><td className="p-4"><div className="flex justify-end gap-2"><Link href={`/admin/all-americans/${item.id}/edit`} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300" aria-label={`Edit ${item.year}`}><Edit3 size={16} /></Link><ResourceActionButton endpoint={`/api/admin/all-americans/${item.id}`} name={String(item.year)} mode="delete" /></div></td></tr>)}</tbody></table></div> : <EmptyState title={filtered ? "No years match these filters" : "No All-American years yet"} description={filtered ? "Clear or adjust the filters." : "Create the first annual story."} />}
    <Pagination page={result.page} totalPages={result.totalPages} params={pagination} /></>;
}
