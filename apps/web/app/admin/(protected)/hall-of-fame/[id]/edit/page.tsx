import { HallOfFameInducteeForm } from "@/components/admin/hall-of-fame-inductee-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem } from "@/lib/admin/page-data";
import type { AdminHallOfFameInductee } from "@/lib/admin/types";

export default async function Page({ params, searchParams }: { params: Promise<{id:string}>; searchParams: Promise<{saved?:string}> }) {
  const { id } = await params;
  const item = await getAdminItem<AdminHallOfFameInductee>(`/api/admin/hall-of-fame-inductees/${encodeURIComponent(id)}`);
  const { saved } = await searchParams;
  return <><PageHeader title="Edit Hall of Fame inductee" description={item.name} />{saved && <p role="status" className="mb-5 border-l-4 border-track-field bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-900">Inductee created successfully.</p>}<HallOfFameInducteeForm item={item} /></>;
}
