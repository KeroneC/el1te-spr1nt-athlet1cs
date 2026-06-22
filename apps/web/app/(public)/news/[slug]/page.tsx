import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PublicApiError, getAnnouncement } from "@/lib/public/client";
import { formatDate } from "@/lib/public/format";
type Props={params:Promise<{slug:string}>};
export async function generateMetadata({params}:Props):Promise<Metadata>{try{const item=await getAnnouncement((await params).slug);return{title:item.title,description:item.summary,alternates:{canonical:`/news/${item.slug}`},openGraph:{title:item.title,description:item.summary,type:"article"}};}catch{return{title:"News update"};}}
export default async function NewsDetail({params}:Props){let item;try{item=await getAnnouncement((await params).slug);}catch(error){if(error instanceof PublicApiError&&error.status===404)notFound();throw error;}return <article><header className="article-hero"><div className="site-container narrow"><Link className="back-link" href="/news">Back to news</Link>{item.isFeatured&&<span className="tag">Featured</span>}<h1>{item.title}</h1><p>{item.summary}</p><time dateTime={item.publishDateUtc??undefined}>{formatDate(item.publishDateUtc)}</time></div></header><div className="site-container narrow article-body"><p>{item.body}</p></div></article>;}
