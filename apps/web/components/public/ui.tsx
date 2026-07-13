/* eslint-disable @next/next/no-img-element -- CMS media can come from administrator-configured external hosts. */
import { ArrowRight, CalendarDays, MapPin } from "lucide-react";
import Link from "next/link";
import { formatDate, formatEventDate, labelEnum } from "@/lib/public/format";
import type { AnnouncementListItem, ContentBlock, EventListItem } from "@/lib/public/types";

export function PageHero({ eyebrow, title, accent, summary }: { eyebrow?: string; title: string; accent?: string; summary?: string | null }) {
  return <section className="page-hero"><div className="site-container narrow">{eyebrow && <p className="eyebrow light">{eyebrow}</p>}<h1>{title}{accent && <> <span>{accent}</span></>}</h1><div className="hero-rule" aria-hidden="true" />{summary && <p>{summary}</p>}</div></section>;
}

export function ContentSection({ block, tone = "light" }: { block: ContentBlock; tone?: "light" | "muted" | "dark" }) {
  return <section className={`content-section tone-${tone}`}><div className={block.imageUrl ? "site-container content-media-layout" : "site-container narrow"}><div className="content-media-copy"><p className="eyebrow">{block.summary}</p><h2>{block.title}</h2><div className="prose"><p>{block.body}</p></div>{block.ctaText && block.ctaUrl && <Link className="text-link" href={block.ctaUrl}>{block.ctaText}<ArrowRight size={17} aria-hidden="true" /></Link>}</div>{block.imageUrl && <div className="content-media-image">{/* eslint-disable-next-line @next/next/no-img-element */}<img src={block.imageUrl} alt={`Supporting image for ${block.title}`} /></div>}</div></section>;
}

export function EmptyState({ title, message }: { title: string; message: string }) {
  return <div className="empty-state"><h2>{title}</h2><p>{message}</p></div>;
}

export function PublicErrorState() {
  return <div className="empty-state error-state"><h2>We could not load this content</h2><p>Please try again shortly. The rest of the website is still available.</p><Link className="button button-secondary" href="/">Return home</Link></div>;
}

export function AnnouncementCard({ item }: { item: AnnouncementListItem }) {
  return <article className={item.isFeatured ? "content-card announcement-card featured" : "content-card announcement-card"}>{item.imageUrl && <div className="card-media">{/* eslint-disable-next-line @next/next/no-img-element */}<img src={item.imageUrl} alt={`Club update: ${item.title}`} /></div>}<div className="card-content"><div className="card-accent" />{item.isFeatured && <span className="tag">Featured</span>}<p className="card-meta">{formatDate(item.publishDateUtc)}</p><h3><Link href={`/news/${item.slug}`}>{item.title}</Link></h3><p>{item.summary}</p><Link className="text-link" href={`/news/${item.slug}`}>Read update<ArrowRight size={16} aria-hidden="true" /></Link></div></article>;
}

export function EventCard({ item, variant = "list" }: { item: EventListItem; variant?: "list" | "compact" }) {
  const startDate = new Date(item.startDateTimeUtc);
  const month = Number.isNaN(startDate.valueOf()) ? "TBD" : startDate.toLocaleString("en-US", { month: "short", timeZone: "UTC" });
  const day = Number.isNaN(startDate.valueOf()) ? "" : startDate.toLocaleString("en-US", { day: "2-digit", timeZone: "UTC" });
  return <article className={`event-list-card event-list-card-${variant}${item.imageUrl ? " has-media" : ""}`}>{item.imageUrl && <div className="event-card-media">{/* eslint-disable-next-line @next/next/no-img-element */}<img src={item.imageUrl} alt={`Event: ${item.title}`} /></div>}<div className="event-date-block"><span>{month}</span><strong>{day}</strong></div><div className="event-list-body"><span className="tag">{labelEnum(item.eventType)}</span><h3><Link href={`/events/${item.slug}`}>{item.title}</Link></h3><p className="card-meta"><CalendarDays size={16} aria-hidden="true" />{formatEventDate(item.startDateTimeUtc, item.endDateTimeUtc)}</p><p className="location"><MapPin size={16} aria-hidden="true" />{item.locationName}</p></div><div className="event-list-action"><Link className="text-link" href={`/events/${item.slug}`}>Details<ArrowRight size={16} aria-hidden="true" /></Link></div></article>;
}

export function Pagination({ page, totalPages, pathname, params = {} }: { page: number; totalPages: number; pathname: string; params?: Record<string, string | undefined> }) {
  if (totalPages <= 1) return null;
  const href = (target: number) => {
    const query = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => value && query.set(key, value));
    query.set("page", String(target));
    return `${pathname}?${query}`;
  };
  return <nav className="pagination" aria-label="Pagination"><Link aria-disabled={page <= 1} className={page <= 1 ? "is-disabled" : ""} href={href(Math.max(1, page - 1))}>Previous</Link><span>Page {page} of {totalPages}</span><Link aria-disabled={page >= totalPages} className={page >= totalPages ? "is-disabled" : ""} href={href(Math.min(totalPages, page + 1))}>Next</Link></nav>;
}
