/* eslint-disable @next/next/no-img-element */
import type { Metadata } from "next";
import Link from "next/link";
import { EmptyState, PageHero, PublicErrorState } from "@/components/public/ui";
import { getGalleryAlbums } from "@/lib/public/client";
import { formatDate } from "@/lib/public/format";
import { sizedPublicMediaUrl } from "@/lib/public/media";

export const metadata: Metadata = { title: "Gallery", description: "Photos from El1te Spr1nt Athlet1cs events and programs." };

export default async function GalleryPage() {
  try {
    const result = await getGalleryAlbums("pageSize=24");
    return <>
      <PageHero eyebrow="Team moments" title="Our" accent="Gallery" summary="Training, competition, and community in motion." />
      <section className="content-section"><div className="site-container">
        {result.items.length ? <div className="gallery-album-grid masonry-inspired">
          {result.items.map(album => <Link href={`/gallery/${album.slug}`} key={album.slug} className="gallery-album-card">
            {album.coverImageUrl ? <img src={sizedPublicMediaUrl(album.coverImageUrl, 800)} alt={album.coverAltText ?? ""} loading="lazy" decoding="async" /> : <div className="gallery-placeholder">No cover image</div>}
            <div><p className="eyebrow">{album.imageCount} {album.imageCount === 1 ? "photo" : "photos"}</p><h2>{album.title}</h2>{album.eventDateUtc && <time dateTime={album.eventDateUtc}>{formatDate(album.eventDateUtc)}</time>}<p>{album.description}</p></div>
          </Link>)}
        </div> : <EmptyState title="Gallery albums are coming soon" message="Published photo collections will appear here." />}
      </div></section>
    </>;
  } catch {
    return <><PageHero title="Our" accent="Gallery" /><div className="site-container content-section"><PublicErrorState /></div></>;
  }
}
