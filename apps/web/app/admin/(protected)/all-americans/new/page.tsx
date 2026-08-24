import { AllAmericanYearForm } from "@/components/admin/all-american-year-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminMediaAsset, PagedResult } from "@/lib/admin/types";
export default async function Page() { const media = await getAdminList<PagedResult<AdminMediaAsset>>("/api/admin/media?isActive=true&pageSize=100"); return <><PageHeader title="Create All-American year" description="Start as a draft, add annual media, then publish when the verified summary is ready." /><AllAmericanYearForm assets={media.items} /></>; }
