import type { MetadataRoute } from "next";
import { getAnnouncements, getEvents, getGalleryAlbums, getStoreProducts } from "@/lib/public/client";
import { publicIndexingEnabled } from "@/lib/public/deployment";

export const dynamic = "force-dynamic";

const staticPaths = [
  "", "/about", "/accessibility", "/coaches", "/contact", "/events", "/faqs",
  "/forms", "/gallery", "/hall-of-fame", "/news", "/privacy", "/programs",
  "/registration", "/scholarship", "/shop", "/sponsors", "/store-policy", "/team", "/terms"
];

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  if (!publicIndexingEnabled(process.env.DEPLOYMENT_ENVIRONMENT, process.env.PUBLIC_INDEXING_ENABLED)) return [];
  const base = new URL(process.env.SITE_URL ?? "http://localhost:3000");
  const urls = new Set(staticPaths.map((path) => new URL(path || "/", base).toString()));
  const [news, events, galleries, products] = await Promise.all([
    getAnnouncements("page=1&pageSize=100").catch(() => null),
    getEvents("page=1&pageSize=100&upcomingOnly=false").catch(() => null),
    getGalleryAlbums("page=1&pageSize=100").catch(() => null),
    getStoreProducts("page=1&pageSize=100").catch(() => null)
  ]);
  news?.items.forEach((item) => urls.add(new URL(`/news/${item.slug}`, base).toString()));
  events?.items.forEach((item) => urls.add(new URL(`/events/${item.slug}`, base).toString()));
  galleries?.items.forEach((item) => urls.add(new URL(`/gallery/${item.slug}`, base).toString()));
  products?.items.forEach((item) => urls.add(new URL(`/shop/${item.slug}`, base).toString()));
  return [...urls].map((url) => ({ url }));
}
