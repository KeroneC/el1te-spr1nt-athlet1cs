/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import { Edit3 } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { MediaActions, MediaUploadForm } from "@/components/admin/media-library";
import { EmptyState, FilterActions, FilterSelect, FilterText, formatDate } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { getAdminList } from "@/lib/admin/page-data";
import type { ActiveFilters, AdminGalleryAlbumListItem, AdminMediaAsset, PagedResult } from "@/lib/admin/types";
import { buildListQuery } from "@/lib/admin/validation";

export default async function Page({searchParams}:{searchParams:Promise<ActiveFilters>}) {
  const filters=await searchParams; const query=buildListQuery(filters,["search","isActive"]);
  const [result,albums]=await Promise.all([
    getAdminList<PagedResult<AdminMediaAsset>>(`/api/admin/media?${query}`),
    getAdminList<PagedResult<AdminGalleryAlbumListItem>>("/api/admin/gallery-albums?page=1&pageSize=100")
  ]);
  const pagination=new URLSearchParams(query); pagination.delete("pageSize");
  const filtered=Boolean(filters.search||filters.isActive);
  return <><PageHeader title="Media library" description="Upload and manage reusable images for website content and galleries."/><MediaUploadForm albums={albums.items}/><form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_200px_auto] md:items-end"><FilterText value={filters.search} placeholder="Title, filename, or alt text"/><FilterSelect label="Status" name="isActive" value={filters.isActive} options={[["","All"],["true","Active"],["false","Inactive"]]}/><FilterActions clearHref="/admin/media" filtered={filtered}/></form>{result.items.length?<div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{result.items.map(asset=><article key={asset.id} className="border border-slate-200 bg-white"><img src={asset.publicUrl} alt={asset.altText} className="aspect-[4/3] w-full object-cover"/><div className="p-4"><div className="flex items-start justify-between gap-3"><div className="min-w-0"><h2 className="truncate font-black">{asset.title}</h2><p className="truncate text-xs text-slate-500">{asset.originalFileName}</p><p className="text-xs text-slate-500">{asset.width} x {asset.height} · {(asset.fileSizeBytes/1024/1024).toFixed(2)} MB</p><p className="text-xs text-slate-500">{formatDate(asset.createdAtUtc,false)}</p></div><span className={`text-xs font-bold ${asset.isActive?"text-emerald-700":"text-slate-500"}`}>{asset.isActive?"Active":"Inactive"}</span></div><div className="mt-3 flex justify-end gap-2"><Link href={`/admin/media/${asset.id}/edit`} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300" aria-label={`Edit ${asset.title}`}><Edit3 size={16}/></Link><MediaActions asset={asset}/></div></div></article>)}</div>:<EmptyState title={filtered?"No media matches these filters":"No media uploaded yet"} description={filtered?"Clear or adjust the filters.":"Upload the first image to begin."}/>}<Pagination page={result.page} totalPages={result.totalPages} params={pagination}/></>;
}
