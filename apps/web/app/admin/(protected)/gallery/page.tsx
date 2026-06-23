/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import { Edit3, Plus } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { Badge, EmptyState, FilterActions, FilterSelect, FilterText, formatDate } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { ResourceActionButton } from "@/components/admin/resource-action-button";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminGalleryAlbumListItem, ContentFilters, PagedResult } from "@/lib/admin/types";
import { buildListQuery } from "@/lib/admin/validation";

export default async function Page({searchParams}:{searchParams:Promise<ContentFilters>}) {
  const filters=await searchParams; const query=buildListQuery(filters,["search","isPublished"]);
  const result=await getAdminList<PagedResult<AdminGalleryAlbumListItem>>(`/api/admin/gallery-albums?${query}`);
  const pagination=new URLSearchParams(query); pagination.delete("pageSize"); const filtered=Boolean(filters.search||filters.isPublished);
  return <><PageHeader title="Gallery albums" description="Curate and publish photo collections." action={{href:"/admin/gallery/new",label:"Create album",icon:Plus}}/><form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_200px_auto] md:items-end"><FilterText value={filters.search} placeholder="Title or description"/><FilterSelect label="Status" name="isPublished" value={filters.isPublished} options={[["","All"],["true","Published"],["false","Draft"]]}/><FilterActions clearHref="/admin/gallery" filtered={filtered}/></form>{result.items.length?<div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[800px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Album</th><th>Event date</th><th>Images</th><th>Status</th><th>Order</th><th className="p-4 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-200">{result.items.map(album=><tr key={album.id}><td className="p-4"><div className="flex items-center gap-3">{album.coverImageUrl?<img src={album.coverImageUrl} alt="" className="h-14 w-20 object-cover"/>:<div className="grid h-14 w-20 place-items-center bg-slate-100 text-xs text-slate-500">No cover</div>}<div><p className="font-bold">{album.title}</p><p className="text-xs text-slate-500">/{album.slug}</p></div></div></td><td>{formatDate(album.eventDateUtc,false)}</td><td>{album.imageCount}</td><td><Badge tone={album.isPublished?"green":"neutral"}>{album.isPublished?"Published":"Draft"}</Badge></td><td>{album.displayOrder}</td><td className="p-4"><div className="flex justify-end gap-2"><Link href={`/admin/gallery/${album.id}/edit`} className="inline-flex h-9 w-9 items-center justify-center border border-slate-300" aria-label={`Edit ${album.title}`}><Edit3 size={16}/></Link><ResourceActionButton endpoint={`/api/admin/gallery-albums/${album.id}`} name={album.title} mode="delete"/></div></td></tr>)}</tbody></table></div>:<EmptyState title={filtered?"No albums match these filters":"No gallery albums yet"} description={filtered?"Clear or adjust the filters.":"Create the first album to begin."}/>}<Pagination page={result.page} totalPages={result.totalPages} params={pagination}/></>;
}
