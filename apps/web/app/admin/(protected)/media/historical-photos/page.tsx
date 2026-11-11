/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageHeader } from "@/components/admin/page-header";
import { EmptyState, FilterActions, FilterSelect, FilterText } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { HistoricalPhotoReview } from "@/components/admin/historical-photo-review";
import { handleAdminPageError, requireSuperAdminUser } from "@/lib/admin/auth";
import { AdminApiError } from "@/lib/admin/api-error";
import { historicalPhotosEnabled, historicalReviews, type HistoricalPhotoList } from "@/lib/admin/historical-photos";
import { adminApiFetch } from "@/lib/admin/server-api";

const href = "/admin/media/historical-photos";

export default async function Page({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  if (!historicalPhotosEnabled()) notFound();
  await requireSuperAdminUser();
  const raw = await searchParams;
  const read = (key: string, max: number) => typeof raw[key] === "string" ? raw[key].slice(0, max) : "";
  const search = read("search", 255);
  const collection = read("collection", 450);
  const rawReview = read("review", 24);
  const review = Object.hasOwn(historicalReviews, rawReview) ? rawReview : "";
  const page = read("page", 9);
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries({ search, collection, review, page })) if (value) params.set(key, value);
  const result = await adminApiFetch<HistoricalPhotoList>(`/api/admin/historical-photos?${params}`).catch(error => {
    if (error instanceof AdminApiError && error.status === 404) notFound();
    handleAdminPageError(error);
  });
  const filtered = Boolean(search || collection || review);
  return <>
    <Link href="/admin/media" className="mb-4 inline-block text-sm font-bold text-red-800 underline">Back to Media Library</Link>
    <PageHeader title="Historical Photos" description="Private archive from the former club website. Approve photos only for confirmed social use; nothing here is published on the site." />
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_1fr_1fr_auto] md:items-end">
      <FilterText value={search} placeholder="Original filename" />
      <FilterSelect label="Collection" name="collection" value={collection}
        options={[["", "All collections"], ...result.collections.map(name => [name, name] as [string, string])]} />
      <FilterSelect label="Review status" name="review" value={review}
        options={[["", "All statuses"], ...Object.entries(historicalReviews)]} />
      <FilterActions clearHref={href} filtered={filtered} />
    </form>
    <p className="mb-3 text-sm font-semibold text-slate-600">{result.totalCount} {result.totalCount === 1 ? "photo" : "photos"}</p>
    {result.items.length > 0 ? <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {result.items.map(photo => <article key={photo.id} className="min-w-0 border border-slate-200 bg-white">
        <img alt="" loading="lazy" decoding="async" src={`/api/admin/historical-photos/${photo.id}/preview`}
          className="aspect-[4/3] w-full bg-slate-100 object-contain" />
        <div className="p-4">
          <h2 className="break-all text-sm font-black">{photo.fileNames[0] ?? "Historical photo"}</h2>
          {photo.fileNames.length > 1 && <p className="mt-1 text-xs text-slate-600">{photo.fileNames.length} source filenames</p>}
          <p className="mt-1 break-words text-xs text-slate-600">{photo.collections.join(" · ")}</p>
          <HistoricalPhotoReview id={photo.id} current={photo.review} />
        </div>
      </article>)}
    </div> : <EmptyState title={filtered ? "No matching photos" : "No historical photos imported yet"}
      description={filtered ? "Try adjusting your filters." : "The archive will appear here after the verified import."} />}
    <Pagination page={result.page} totalPages={result.totalPages} params={params} label="Historical photo pages" />
  </>;
}
