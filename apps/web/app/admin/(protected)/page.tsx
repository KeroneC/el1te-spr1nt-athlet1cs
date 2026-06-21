import Link from "next/link";
import { ArrowRight, Plus } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { requireAdminUser } from "@/lib/admin/auth";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminAnnouncement, PagedResult } from "@/lib/admin/types";

export default async function AdminDashboardPage() {
  const user = await requireAdminUser();
  let recent: AdminAnnouncement[] = [];
  try { recent = (await adminApiFetch<PagedResult<AdminAnnouncement>>("/api/admin/announcements?page=1&pageSize=5&includeExpired=true")).items; } catch { /* Dashboard remains useful during a content-service error. */ }
  return <><PageHeader title={`Welcome, ${user.firstName}`} description="Manage the club's public content from this workspace. Announcements are the first available module." />
    <section className="grid gap-4 sm:grid-cols-2"><Link href="/admin/announcements/new" className="border-l-4 border-track-red bg-white p-5 shadow-sm transition hover:shadow-md"><Plus size={22} className="text-track-red" /><h2 className="mt-3 text-lg font-black text-track-ink">Create announcement</h2><p className="mt-1 text-sm text-slate-600">Draft and publish a new club update.</p></Link><Link href="/admin/announcements" className="border-l-4 border-track-field bg-white p-5 shadow-sm transition hover:shadow-md"><ArrowRight size={22} className="text-track-field" /><h2 className="mt-3 text-lg font-black text-track-ink">Manage announcements</h2><p className="mt-1 text-sm text-slate-600">Review, edit, filter, and remove announcements.</p></Link></section>
    <section className="mt-8"><div className="mb-3 flex items-center justify-between"><h2 className="text-xl font-black text-track-ink">Recent announcements</h2><Link href="/admin/announcements" className="text-sm font-bold text-track-red hover:underline">View all</Link></div>{recent.length ? <div className="divide-y divide-slate-200 border border-slate-200 bg-white">{recent.map((item) => <Link key={item.id} href={`/admin/announcements/${item.id}/edit`} className="flex items-center justify-between gap-4 p-4 hover:bg-slate-50"><div><p className="font-bold text-track-ink">{item.title}</p><p className="mt-1 text-xs text-slate-500">/{item.slug}</p></div><span className="text-sm font-semibold text-slate-600">{item.isPublished ? "Published" : "Draft"}</span></Link>)}</div> : <div className="border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-600">No announcements yet. Create the first club update.</div>}</section>
  </>;
}
