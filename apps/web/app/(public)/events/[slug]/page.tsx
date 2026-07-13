/* eslint-disable @next/next/no-img-element -- CMS media can use administrator-configured external hosts. */
import type { Metadata } from "next";
import { CalendarDays, MapPin } from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PublicApiError, getEvent } from "@/lib/public/client";
import { formatEventDate, isExternalUrl, labelEnum } from "@/lib/public/format";
type Props={params:Promise<{slug:string}>};
export async function generateMetadata({params}:Props):Promise<Metadata>{try{const item=await getEvent((await params).slug);return{title:item.title,description:item.description,alternates:{canonical:`/events/${item.slug}`},openGraph:{title:item.title,description:item.description}};}catch{return{title:"Event"};}}
export default async function EventDetailPage({params}:Props){let item;try{item=await getEvent((await params).slug);}catch(error){if(error instanceof PublicApiError&&error.status===404)notFound();throw error;}return <article><header className="article-hero"><div className="site-container narrow"><Link className="back-link" href="/events">Back to events</Link><span className="tag">{labelEnum(item.eventType)}</span><h1>{item.title}</h1><div className="event-facts"><p><CalendarDays aria-hidden="true"/>{formatEventDate(item.startDateTimeUtc,item.endDateTimeUtc)}</p><p><MapPin aria-hidden="true"/>{item.locationName}{item.address?`, ${item.address}`:""}</p></div></div></header>{item.imageUrl&&<div className="article-feature-media site-container narrow"><img src={item.imageUrl} alt={`Event: ${item.title}`} /></div>}<div className="site-container narrow article-body"><p>{item.description}</p>{item.registrationUrl&&<Link className="button button-primary" href={item.registrationUrl} target={isExternalUrl(item.registrationUrl)?"_blank":undefined} rel={isExternalUrl(item.registrationUrl)?"noreferrer noopener":undefined}>Registration details</Link>}</div></article>;}
