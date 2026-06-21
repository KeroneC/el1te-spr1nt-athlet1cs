import Link from "next/link";
import { Edit3, Plus, Search, X } from "lucide-react";
import { DeleteAnnouncementButton } from "@/components/admin/delete-announcement-button";
import { PageHeader } from "@/components/admin/page-header";
import { Pagination } from "@/components/admin/pagination";
import { StatusBadge } from "@/components/admin/status-badge";
import { handleAdminPageError } from "@/lib/admin/auth";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminAnnouncement, AnnouncementFilters, PagedResult } from "@/lib/admin/types";
import { buildAnnouncementQuery } from "@/lib/admin/validation";

export default async function AnnouncementsPage({ searchParams }: { searchParams: Promise<AnnouncementFilters> }) {
  const filters = await searchParams;
  const query = buildAnnouncementQuery(filters);
  let result: PagedResult<AdminAnnouncement>;
  try { result = await adminApiFetch(`/api/admin/announcements?${query}`); } catch (error) { handleAdminPageError(error); }
  const urlParams = new URLSearchParams(query); urlParams.delete("pageSize");
  const filtered = Boolean(filters.search || filters.isPublished || filters.isFeatured || filters.includeExpired);
  return <><PageHeader title="Announcements" description="Create, schedule, publish, feature, and retire club updates." action={{ href: "/admin/announcements/new", label: "Create announcement", icon: Plus }} />
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[minmax(220px,1fr)_180px_180px_auto_auto] md:items-end">
      <label className="text-sm font-bold text-track-ink">Search<input name="search" defaultValue={filters.search} placeholder="Title or summary" className="mt-2 min-h-10 w-full border border-slate-300 px-3 font-normal outline-none focus:border-track-red" /></label>
      <Select label="Published" name="isPublished" value={filters.isPublished} options={[['', 'All'], ['true', 'Published'], ['false', 'Draft']]} />
      <Select label="Featured" name="isFeatured" value={filters.isFeatured} options={[['', 'All'], ['true', 'Featured'], ['false', 'Not featured']]} />
      <label className="flex min-h-10 items-center gap-2 text-sm font-bold text-track-ink"><input type="checkbox" name="includeExpired" value="true" defaultChecked={filters.includeExpired === "true"} className="h-5 w-5 accent-track-red" />Include expired</label>
      <div className="flex gap-2"><button className="inline-flex min-h-10 items-center gap-2 bg-track-ink px-4 text-sm font-bold text-white"><Search size={17} />Apply</button>{filtered && <Link href="/admin/announcements" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 text-slate-600" aria-label="Clear filters" title="Clear filters"><X size={18} /></Link>}</div>
    </form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[900px] border-collapse text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="px-4 py-3">Announcement</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Featured</th><th className="px-4 py-3">Publish / expire</th><th className="px-4 py-3">Updated</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-200">{result.items.map((item) => <tr key={item.id} className="align-top hover:bg-slate-50"><td className="px-4 py-4"><p className="font-bold text-track-ink">{item.title}</p><p className="mt-1 text-xs text-slate-500">/{item.slug}</p></td><td className="px-4 py-4"><StatusBadge announcement={item} /></td><td className="px-4 py-4 text-sm font-semibold text-slate-700">{item.isFeatured ? "Featured" : "Standard"}</td><td className="px-4 py-4 text-sm text-slate-600"><p>{formatDate(item.publishDateUtc) ?? "Not set"}</p><p className="mt-1 text-xs">Expires: {formatDate(item.expirationDateUtc) ?? "Never"}</p></td><td className="px-4 py-4 text-sm text-slate-600">{formatDate(item.updatedAtUtc ?? item.createdAtUtc)}</td><td className="px-4 py-4"><div className="flex justify-end gap-2"><Link href={`/admin/announcements/${item.id}/edit`} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300 text-slate-600 hover:border-track-red hover:text-track-red" aria-label={`Edit ${item.title}`} title="Edit"><Edit3 size={17} /></Link><DeleteAnnouncementButton id={item.id} title={item.title} /></div></td></tr>)}</tbody></table></div> : <div className="border border-dashed border-slate-300 bg-white px-6 py-14 text-center"><h2 className="text-lg font-black text-track-ink">{filtered ? "No announcements match these filters" : "No announcements yet"}</h2><p className="mt-2 text-sm text-slate-600">{filtered ? "Clear or adjust the filters to see more results." : "Create the first announcement for the club."}</p>{!filtered && <Link href="/admin/announcements/new" className="mt-5 inline-flex min-h-10 items-center gap-2 bg-track-red px-4 text-sm font-bold text-white"><Plus size={17} />Create announcement</Link>}</div>}
    <Pagination page={result.page} totalPages={result.totalPages} params={urlParams} />
  </>;
}

function Select({ label, name, value, options }: { label: string; name: string; value?: string; options: [string, string][] }) { return <label className="text-sm font-bold text-track-ink">{label}<select name={name} defaultValue={value ?? ""} className="mt-2 min-h-10 w-full border border-slate-300 bg-white px-3 font-normal outline-none focus:border-track-red">{options.map(([key, text]) => <option key={key} value={key}>{text}</option>)}</select></label>; }
function formatDate(value: string | null) { return value ? new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : null; }
