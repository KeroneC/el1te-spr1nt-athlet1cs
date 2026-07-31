import Link from "next/link";
import { Edit3, Plus } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { Pagination } from "@/components/admin/pagination";
import { Badge, EmptyState, FilterActions, FilterSelect, FilterText } from "@/components/admin/list-controls";
import { ResourceActionButton } from "@/components/admin/resource-action-button";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminHallOfFameInductee, HallOfFameFilters, PagedResult } from "@/lib/admin/types";
import { buildListQuery } from "@/lib/admin/validation";

export default async function Page({ searchParams }: { searchParams: Promise<HallOfFameFilters> }) {
  const filters = await searchParams;
  const query = buildListQuery(filters, ["search", "isActive", "inductionYear"]);
  const result = await getAdminList<PagedResult<AdminHallOfFameInductee>>(`/api/admin/hall-of-fame-inductees?${query}`);
  const paginationParams = new URLSearchParams(query);
  paginationParams.delete("pageSize");
  const filtered = Boolean(filters.search || filters.isActive || filters.inductionYear);
  return <>
    <PageHeader title="Hall of Fame" description="Manage inductee profiles, public visibility, and display order." action={{ href: "/admin/hall-of-fame/new", label: "Create inductee", icon: Plus }} />
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_160px_180px_auto] md:items-end">
      <FilterText value={filters.search} placeholder="Name or affiliation" />
      <FilterText label="Induction year" name="inductionYear" value={filters.inductionYear} />
      <FilterSelect label="Status" name="isActive" value={filters.isActive} options={[["", "All"], ["true", "Active"], ["false", "Inactive"]]} />
      <FilterActions clearHref="/admin/hall-of-fame" filtered={filtered} />
    </form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[850px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="px-4 py-3">Inductee</th><th className="px-4 py-3">Affiliation</th><th className="px-4 py-3">Class</th><th className="px-4 py-3">Order</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-200">{result.items.map(item => <tr key={item.id}><td className="px-4 py-4"><p className="font-bold">{item.name}</p><p className="text-xs text-slate-500">/{item.slug}</p></td><td className="px-4 py-4 text-sm">{item.affiliation}</td><td className="px-4 py-4 text-sm">{item.inductionYear ?? "Not set"}</td><td className="px-4 py-4 text-sm">{item.displayOrder}</td><td className="px-4 py-4"><Badge tone={item.isActive ? "green" : "neutral"}>{item.isActive ? "Active" : "Inactive"}</Badge></td><td className="px-4 py-4"><div className="flex justify-end gap-2"><Link href={`/admin/hall-of-fame/${item.id}/edit`} aria-label={`Edit ${item.name}`} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300"><Edit3 size={17} /></Link>{item.isActive && <ResourceActionButton endpoint={`/api/admin/hall-of-fame-inductees/${item.id}`} name={item.name} mode="deactivate" />}</div></td></tr>)}</tbody></table></div> : <EmptyState title={filtered ? "No inductees match these filters" : "No Hall of Fame inductees yet"} description={filtered ? "Clear or adjust the filters." : "Create the first inductee profile."} />}
    <Pagination page={result.page} totalPages={result.totalPages} params={paginationParams} />
  </>;
}
