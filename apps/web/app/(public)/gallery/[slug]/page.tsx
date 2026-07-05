/* eslint-disable @next/next/no-img-element */
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { EmptyState } from "@/components/public/ui";
import { getGalleryAlbum, PublicApiError } from "@/lib/public/client";
import { formatDate } from "@/lib/public/format";
type Props={params:Promise<{slug:string}>};
export async function generateMetadata({params}:Props):Promise<Metadata>{try{const album=await getGalleryAlbum((await params).slug);return{title:album.title,description:album.description};}catch{return{title:"Gallery album"};}}
export default async function GalleryDetail({params}:Props){let album;try{album=await getGalleryAlbum((await params).slug);}catch(error){if(error instanceof PublicApiError&&error.status===404)notFound();throw error;}return <article><header className="article-hero"><div className="site-container"><Link className="back-link" href="/gallery">Back to gallery</Link><h1>{album.title}</h1><p>{album.description}</p>{album.eventDateUtc&&<time dateTime={album.eventDateUtc}>{formatDate(album.eventDateUtc)}</time>}</div></header><div className="site-container content-section">{album.images.length?<div className="gallery-image-grid">{album.images.map((image,index)=><figure key={`${image.publicUrl}-${index}`}><img src={image.publicUrl} alt={image.altText} width={image.width} height={image.height}/>{image.caption&&<figcaption>{image.caption}</figcaption>}</figure>)}</div>:<EmptyState title="This album is being prepared" message="Published images will appear here when the album is ready."/>}<div className="gallery-detail-actions"><Link className="button button-secondary" href="/gallery">Back to all albums</Link><Link className="button button-primary" href="/contact">Send photo update</Link></div></div></article>}
