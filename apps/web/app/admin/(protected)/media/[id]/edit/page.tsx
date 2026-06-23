import { notFound } from "next/navigation";
import { MediaEditForm } from "@/components/admin/media-edit-form";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem } from "@/lib/admin/page-data";
import type { AdminMediaAsset } from "@/lib/admin/types";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;const asset=await getAdminItem<AdminMediaAsset>(`/api/admin/media/${encodeURIComponent(id)}`);if(!asset)notFound();return <><PageHeader title="Edit media" description="Update accessible metadata and visibility."/><MediaEditForm asset={asset}/></>}
