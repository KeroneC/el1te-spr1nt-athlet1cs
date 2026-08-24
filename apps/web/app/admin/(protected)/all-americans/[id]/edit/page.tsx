import { AllAmericanYearForm } from "@/components/admin/all-american-year-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem, getAdminList } from "@/lib/admin/page-data";
import type { AdminAllAmericanYear, AdminMediaAsset, PagedResult } from "@/lib/admin/types";
export default async function Page({ params }: { params: Promise<{ id: string }> }) { const { id } = await params; const [year, media] = await Promise.all([getAdminItem<AdminAllAmericanYear>(`/api/admin/all-americans/${encodeURIComponent(id)}`), getAdminList<PagedResult<AdminMediaAsset>>("/api/admin/media?isActive=true&pageSize=100")]); return <><PageHeader title={`Edit ${year.year} archive`} description="Manage the annual story, image order, roster, and verified performances." /><AllAmericanYearForm year={year} assets={media.items} /></>; }
