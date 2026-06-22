import Link from "next/link";
import { ArrowLeft, Mail, Phone } from "lucide-react";
import { ContactStatusForm } from "@/components/admin/contact-status-form";
import { formatDate } from "@/components/admin/list-controls";
import { PageHeader } from "@/components/admin/page-header";
import { ResourceActionButton } from "@/components/admin/resource-action-button";
import { getAdminItem } from "@/lib/admin/page-data";
import type { AdminContactSubmission } from "@/lib/admin/types";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const item = await getAdminItem<AdminContactSubmission>(`/api/admin/contact-submissions/${encodeURIComponent(id)}`);
  return <><PageHeader title="Contact submission" description={`${item.inquiryType} inquiry from ${item.name}`} />
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]"><section className="border border-slate-200 bg-white p-5 sm:p-6"><dl className="grid gap-5 sm:grid-cols-2"><Detail label="Name" value={item.name} /><Detail label="Inquiry type" value={item.inquiryType} /><Detail label="Submitted" value={formatDate(item.createdAtUtc)} /><Detail label="Last updated" value={formatDate(item.updatedAtUtc)} /></dl><div className="mt-6 flex flex-wrap gap-3"><a href={`mailto:${item.email}`} className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><Mail size={17} />{item.email}</a>{item.phone && <a href={`tel:${item.phone.replace(/[^+\d]/g, "")}`} className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><Phone size={17} />{item.phone}</a>}</div><div className="mt-7"><h2 className="text-lg font-black">Message</h2><p className="mt-3 whitespace-pre-wrap break-words text-sm leading-7 text-slate-700">{item.message}</p></div></section>
      <aside className="space-y-5"><ContactStatusForm item={item} /><div className="border border-slate-200 bg-white p-5"><h2 className="font-black">Permanent deletion</h2><p className="mt-2 text-sm leading-6 text-slate-600">Archiving preserves the inquiry. Delete only when the record should be permanently removed.</p><div className="mt-4"><ResourceActionButton endpoint={`/api/admin/contact-submissions/${item.id}`} name={`submission from ${item.name}`} mode="delete" redirectTo="/admin/contact-submissions" /></div></div></aside></div>
    <Link href="/admin/contact-submissions" className="mt-6 inline-flex min-h-10 items-center gap-2 border border-slate-300 px-4 text-sm font-bold"><ArrowLeft size={17} />Back to submissions</Link></>;
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-xs font-bold uppercase text-slate-500">{label}</dt><dd className="mt-1 font-semibold text-track-ink">{value}</dd></div>;
}
