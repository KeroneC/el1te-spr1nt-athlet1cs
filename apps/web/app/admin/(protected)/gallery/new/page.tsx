import { GalleryForm } from "@/components/admin/gallery-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminMediaAsset, PagedResult } from "@/lib/admin/types";
export default async function Page(){const media=await getAdminList<PagedResult<AdminMediaAsset>>("/api/admin/media?isActive=true&pageSize=100");return <><PageHeader title="Create gallery album" description="Create the album, then add and arrange its images."/><GalleryForm assets={media.items}/></>}
