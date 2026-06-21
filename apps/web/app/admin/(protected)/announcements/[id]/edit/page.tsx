import { notFound } from "next/navigation";
import { AnnouncementForm } from "@/components/admin/announcement-form";
import { PageHeader } from "@/components/admin/page-header";
import { handleAdminPageError } from "@/lib/admin/auth";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminAnnouncement } from "@/lib/admin/types";

export default async function EditAnnouncementPage({ params, searchParams }: { params: Promise<{ id: string }>; searchParams: Promise<{ saved?: string }> }) {
  const { id } = await params; let announcement: AdminAnnouncement;
  try { announcement = await adminApiFetch(`/api/admin/announcements/${encodeURIComponent(id)}`); }
  catch (error) { if (error instanceof AdminApiError && error.status === 404) notFound(); handleAdminPageError(error); }
  const { saved } = await searchParams;
  return <><PageHeader title="Edit announcement" description={`Manage /${announcement.slug}`} />{saved === "created" && <p role="status" className="mb-5 border-l-4 border-track-field bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-900">Announcement created successfully.</p>}<AnnouncementForm announcement={announcement} /></>;
}
