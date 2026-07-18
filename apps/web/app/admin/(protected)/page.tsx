import Link from "next/link";
import { ArrowRight, CalendarDays, Handshake, Inbox, Plus, Users } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { handleAdminPageError, requireAdminUser } from "@/lib/admin/auth";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminContactSubmission, AdminEvent, PagedResult } from "@/lib/admin/types";

export default async function AdminDashboardPage() {
  const user = await requireAdminUser();
  const [events, coaches, sponsors, contacts] = await Promise.all([
    count<AdminEvent>(`/api/admin/events?fromDate=${encodeURIComponent(new Date().toISOString())}&page=1&pageSize=1`),
    count("/api/admin/coaches?isActive=true&page=1&pageSize=1"),
    count("/api/admin/sponsors?isActive=true&page=1&pageSize=1"),
    count<AdminContactSubmission>("/api/admin/contact-submissions?status=New&page=1&pageSize=5")
  ]);
  return <><PageHeader title={`Welcome, ${user.firstName}`} description="Manage the club's public CMS content and private contact inquiries." />
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4" aria-label="CMS summary"><Metric label="Upcoming events" value={events.total} href="/admin/events" icon={CalendarDays}/><Metric label="Active coaches" value={coaches.total} href="/admin/coaches" icon={Users}/><Metric label="Active sponsors" value={sponsors.total} href="/admin/sponsors" icon={Handshake}/><Metric label="New inquiries" value={contacts.total} href="/admin/contact-submissions?status=New" icon={Inbox}/></section>
    <section className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3"><Quick href="/admin/announcements/new" title="Create announcement" description="Draft or publish a club update."/><Quick href="/admin/events/new" title="Create event" description="Schedule a meet, practice, or deadline."/><Quick href="/admin/content" title="Manage page content" description="Edit keyed public website sections."/></section>
    <section className="mt-8"><div className="mb-3 flex items-center justify-between"><h2 className="text-xl font-black text-track-ink">New contact submissions</h2><Link href="/admin/contact-submissions" className="text-sm font-bold text-track-red hover:underline">View all</Link></div>{contacts.items.length?<div className="divide-y divide-slate-200 border border-slate-200 bg-white">{contacts.items.map(item=><Link key={item.id} href={`/admin/contact-submissions/${item.id}`} className="flex items-center justify-between gap-4 p-4 hover:bg-slate-50"><div className="min-w-0"><p className="font-bold">{item.name}</p><p className="mt-1 truncate text-xs text-slate-500">{item.message}</p></div><ArrowRight size={18} className="shrink-0"/></Link>)}</div>:<div className="border border-dashed border-slate-300 bg-white p-8 text-center text-sm text-slate-600">No new contact submissions.</div>}</section>
  </>;
}
async function count<T>(path:string):Promise<{total:number;items:T[]}>{try{const result=await adminApiFetch<PagedResult<T>>(path);return{total:result.totalCount,items:result.items}}catch(error){if(error instanceof AdminApiError&&(error.status===401||error.status===403))handleAdminPageError(error);return{total:0,items:[]}}}
function Metric({label,value,href,icon:Icon}:{label:string;value:number;href:string;icon:typeof CalendarDays}){return <Link href={href} className="border-l-4 border-track-field bg-white p-5 shadow-sm"><Icon size={21} className="text-track-field"/><p className="mt-3 text-3xl font-black">{value}</p><p className="mt-1 text-sm font-semibold text-slate-600">{label}</p></Link>}
function Quick({href,title,description}:{href:string;title:string;description:string}){return <Link href={href} className="border-l-4 border-track-red bg-white p-5 shadow-sm"><Plus size={20} className="text-track-red"/><h2 className="mt-3 font-black">{title}</h2><p className="mt-1 text-sm text-slate-600">{description}</p></Link>}
