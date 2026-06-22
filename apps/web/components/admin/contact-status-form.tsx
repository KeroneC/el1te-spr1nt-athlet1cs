"use client";
import { useRouter } from "next/navigation";
import { FormNotice, SelectField } from "./form-controls";
import { useAdminMutation } from "@/lib/admin/use-admin-mutation";
import { CONTACT_STATUSES, type AdminContactSubmission, type ContactSubmissionStatus } from "@/lib/admin/types";
import type { FieldErrors } from "@/lib/admin/validation";
export function ContactStatusForm({ item }: { item: AdminContactSubmission }) { const router = useRouter(); const mutation = useAdminMutation<AdminContactSubmission>(); async function submit(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); const status = String(new FormData(event.currentTarget).get("status")) as ContactSubmissionStatus; const validate = (): FieldErrors => CONTACT_STATUSES.includes(status) ? {} : { status: ["Choose a valid status."] }; const result = await mutation.save(`/api/admin/contact-submissions/${item.id}/status`, "PUT", { status }, validate); if (result) router.refresh(); }
return <form onSubmit={submit} className="space-y-4 border border-slate-200 bg-white p-5"><h2 className="text-lg font-black text-track-ink">Submission status</h2><FormNotice message={mutation.message} success={mutation.success} /><SelectField label="Status" name="status" defaultValue={item.status} options={CONTACT_STATUSES.map(v => [v,v])} /><button type="submit" disabled={mutation.submitting} className="min-h-10 bg-track-red px-4 text-sm font-bold text-white disabled:opacity-60">{mutation.submitting ? "Saving..." : "Update status"}</button></form>; }
