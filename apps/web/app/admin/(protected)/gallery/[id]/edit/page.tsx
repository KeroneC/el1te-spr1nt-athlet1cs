import { GalleryForm } from "@/components/admin/gallery-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem, getAdminList } from "@/lib/admin/page-data";
import type { AdminGalleryAlbum, AdminMediaAsset, PagedResult } from "@/lib/admin/types";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;const [album,media]=await Promise.all([getAdminItem<AdminGalleryAlbum>(`/api/admin/gallery-albums/${encodeURIComponent(id)}`),getAdminList<PagedResult<AdminMediaAsset>>("/api/admin/media?isActive=true&pageSize=100")]);return <><PageHeader title="Edit gallery album" description={`Manage /${album.slug} and its image order.`}/><GalleryForm album={album} assets={media.items}/></>}
