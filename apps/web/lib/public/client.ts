import type {
  AnnouncementDetail,
  AnnouncementListItem,
  Coach,
  ContentBlock,
  EventDetail,
  EventListItem,
  Faq,
  PagedResult,
  SiteSettings,
  Sponsor,
  StoreCatalog,
  StoreProduct
} from "./types";
import type { GalleryAlbum, GalleryAlbumListItem } from "./types";
import { BRAND } from "./site";
import { SUPPORT_REFERENCE_HEADER, validSupportReference } from "@/lib/observability/support-reference";
import { createSupportReference, logUnexpectedWebFailure } from "@/lib/observability/support-reference.server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";
const configuredRevalidateSeconds = Number(process.env.PUBLIC_REVALIDATE_SECONDS ?? 60);
export const PUBLIC_REVALIDATE_SECONDS = Number.isFinite(configuredRevalidateSeconds) && configuredRevalidateSeconds >= 0
  ? configuredRevalidateSeconds
  : 60;

export class PublicApiError extends Error {
  constructor(public readonly status: number, public readonly referenceId: string | null = null) {
    super(status === 404 ? "Public content was not found." : "Public content is temporarily unavailable.");
  }
}

export async function publicApiFetch<T>(path: string, revalidateSeconds = PUBLIC_REVALIDATE_SECONDS): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}/api/public${path}`, {
      next: { revalidate: revalidateSeconds }
    });
  } catch {
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "public-api-unavailable", status: 503 });
    throw new PublicApiError(503, referenceId);
  }

  if (!response.ok) {
    let referenceId = response.status >= 500
      ? validSupportReference(response.headers.get(SUPPORT_REFERENCE_HEADER))
      : null;
    if (!referenceId && response.status >= 500) {
      const problem = await response.clone().json().catch(() => null) as { referenceId?: unknown } | null;
      referenceId = validSupportReference(problem?.referenceId);
    }
    if (!referenceId && response.status >= 500) {
      referenceId = createSupportReference();
      logUnexpectedWebFailure({ referenceId, category: "public-api-upstream-failure", status: response.status });
    }
    throw new PublicApiError(response.status, referenceId);
  }

  try {
    return (await response.json()) as T;
  } catch {
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "public-api-invalid-response", status: 502 });
    throw new PublicApiError(502, referenceId);
  }
}

export const getSiteSettings = () => publicApiFetch<SiteSettings>("/site-settings");
export const getContentBlocks = () => publicApiFetch<ContentBlock[]>("/content-blocks");
export const getAnnouncements = (query = "") =>
  publicApiFetch<PagedResult<AnnouncementListItem>>(`/announcements${query ? `?${query}` : ""}`);
export const getAnnouncement = (slug: string) =>
  publicApiFetch<AnnouncementDetail>(`/announcements/${encodeURIComponent(slug)}`);
export const getEvents = (query = "") =>
  publicApiFetch<PagedResult<EventListItem>>(`/events${query ? `?${query}` : ""}`);
export const getEvent = (slug: string) =>
  publicApiFetch<EventDetail>(`/events/${encodeURIComponent(slug)}`);
export const getCoaches = () => publicApiFetch<Coach[]>("/coaches");
export const getSponsors = () => publicApiFetch<Sponsor[]>("/sponsors");
export const getFaqs = () => publicApiFetch<Faq[]>("/faqs");
export const getGalleryAlbums = (query = "") =>
  publicApiFetch<PagedResult<GalleryAlbumListItem>>(`/gallery-albums${query ? `?${query}` : ""}`);
export const getGalleryAlbum = (slug: string) =>
  publicApiFetch<GalleryAlbum>(`/gallery-albums/${encodeURIComponent(slug)}`);
export const getStoreProducts = (query = "") =>
  publicApiFetch<StoreCatalog>(`/store/products${query ? `?${query}` : ""}`, 15);
export const getStoreProduct = (slug: string) =>
  publicApiFetch<StoreProduct>(`/store/products/${encodeURIComponent(slug)}`, 15);
export const isStoreEnabled = async () => {
  try {
    const response = await fetch(`${apiBaseUrl}/api/public/store/products?page=1&pageSize=1`, {
      next: { revalidate: 15 }
    });
    return response.ok;
  } catch {
    return false;
  }
};

export const fallbackSettings: SiteSettings = {
  clubName: BRAND.name,
  slogan: BRAND.slogan,
  contactEmail: BRAND.contactEmail,
  phoneNumber: BRAND.contactPhone,
  addressLine1: null,
  addressLine2: null,
  city: null,
  state: null,
  zipCode: null,
  facebookUrl: BRAND.facebookUrl,
  instagramUrl: BRAND.instagramUrl,
  youTubeUrl: null,
  primaryCtaText: "Registration Info",
  primaryCtaUrl: "/registration",
  secondaryCtaText: "Contact Us",
  secondaryCtaUrl: "/contact",
  logoUrl: null
};
