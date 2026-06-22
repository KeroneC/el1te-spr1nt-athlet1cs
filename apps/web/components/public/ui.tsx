import { ArrowRight, CalendarDays, MapPin } from "lucide-react";
import Link from "next/link";
import { formatDate, formatEventDate, labelEnum } from "@/lib/public/format";
import type { AnnouncementListItem, ContentBlock, EventListItem } from "@/lib/public/types";

export function PageHero({ eyebrow, title, summary }: { eyebrow?: string; title: string; summary?: string | null }) {
  return <section className="page-hero"><div className="site-container narrow">{eyebrow && <p className="eyebrow">{eyebrow}</p>}<h1>{title}</h1>{summary && <p>{summary}</p>}</div></section>;
}

export function ContentSection({ block, tone = "light" }: { block: ContentBlock; tone?: "light" | "muted" | "dark" }) {
  return <section className={`content-section tone-${tone}`}><div className="site-container narrow"><p className="eyebrow">{block.summary}</p><h2>{block.title}</h2><div className="prose"><p>{block.body}</p></div>{block.ctaText && block.ctaUrl && <Link className="text-link" href={block.ctaUrl}>{block.ctaText}<ArrowRight size={17} aria-hidden="true" /></Link>}</div></section>;
}

export function EmptyState({ title, message }: { title: string; message: string }) {
  return <div className="empty-state"><h2>{title}</h2><p>{message}</p></div>;
}

export function PublicErrorState() {
  return <div className="empty-state error-state"><h2>We could not load this content</h2><p>Please try again shortly. The rest of the website is still available.</p><Link className="button button-secondary" href="/">Return home</Link></div>;
}

export function AnnouncementCard({ item }: { item: AnnouncementListItem }) {
  return <article className="content-card"><div className="card-accent" />{item.isFeatured && <span className="tag">Featured</span>}<p className="card-meta">{formatDate(item.publishDateUtc)}</p><h3><Link href={`/news/${item.slug}`}>{item.title}</Link></h3><p>{item.summary}</p><Link className="text-link" href={`/news/${item.slug}`}>Read update<ArrowRight size={16} aria-hidden="true" /></Link></article>;
}

export function EventCard({ item }: { item: EventListItem }) {
  return <article className="content-card event-card"><div className="event-icon"><CalendarDays aria-hidden="true" /></div><span className="tag">{labelEnum(item.eventType)}</span><h3><Link href={`/events/${item.slug}`}>{item.title}</Link></h3><p className="card-meta">{formatEventDate(item.startDateTimeUtc, item.endDateTimeUtc)}</p><p className="location"><MapPin size={16} aria-hidden="true" />{item.locationName}</p><Link className="text-link" href={`/events/${item.slug}`}>Event details<ArrowRight size={16} aria-hidden="true" /></Link></article>;
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
