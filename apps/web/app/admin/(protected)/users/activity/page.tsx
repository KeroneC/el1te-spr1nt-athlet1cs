import { FilterActions, FilterText, EmptyState, formatDate } from "@/components/admin/list-controls";
import { PageHeader } from "@/components/admin/page-header";
import { Pagination } from "@/components/admin/pagination";
import { requireSuperAdminUser } from "@/lib/admin/auth";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminActivityLog, PagedResult } from "@/lib/admin/types";
import { buildListQuery } from "@/lib/admin/validation";

type Filters = { search?: string; fromDate?: string; toDate?: string; page?: string };

export default async function AdminActivityPage({ searchParams }: { searchParams: Promise<Filters> }) {
  await requireSuperAdminUser();
  const filters = await searchParams;
  const query = buildListQuery(filters, ["search", "fromDate", "toDate"]);
  const result = await getAdminList<PagedResult<AdminActivityLog>>(`/api/admin/activity?${query}`);
  const params = new URLSearchParams(query); params.delete("pageSize");
  const filtered = Boolean(filters.search || filters.fromDate || filters.toDate);
  return <><PageHeader title="Administrative activity" description="Append-only history for invitations, privileged account acceptance, and access changes." />
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_180px_180px_auto] md:items-end"><FilterText value={filters.search} placeholder="Administrator or action" /><FilterText label="From" name="fromDate" value={filters.fromDate} /><FilterText label="To" name="toDate" value={filters.toDate} /><FilterActions clearHref="/admin/users/activity" filtered={filtered} /></form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[900px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="px-4 py-3">Time</th><th className="px-4 py-3">Administrator</th><th className="px-4 py-3">Action</th><th className="px-4 py-3">Summary</th><th className="px-4 py-3">Reference</th></tr></thead><tbody className="divide-y divide-slate-200">{result.items.map(item => <tr key={item.id}><td className="whitespace-nowrap px-4 py-4 text-sm text-slate-600">{formatDate(item.createdAtUtc)}</td><td className="px-4 py-4 text-sm font-bold text-track-ink">{item.actorDisplayName}</td><td className="px-4 py-4 text-sm">{labelAction(item.action)}</td><td className="max-w-xl px-4 py-4 text-sm leading-6 text-slate-700">{item.summary}</td><td className="px-4 py-4 font-mono text-xs text-slate-500">{item.correlationId ?? "Not recorded"}</td></tr>)}</tbody></table></div> : <EmptyState title={filtered ? "No activity matches these filters" : "No administrative activity yet"} description={filtered ? "Clear or adjust the filters." : "Privileged account changes will appear here."} />}
    <Pagination page={result.page} totalPages={result.totalPages} params={params} />
  </>;
}

function labelAction(action: string): string { return action.replace(/([a-z])([A-Z])/g, "$1 $2"); }
