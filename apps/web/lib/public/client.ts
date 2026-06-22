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
  Sponsor
} from "./types";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";
export const PUBLIC_REVALIDATE_SECONDS = 60;

export class PublicApiError extends Error {
  constructor(public readonly status: number) {
    super(status === 404 ? "Public content was not found." : "Public content is temporarily unavailable.");
  }
}

export async function publicApiFetch<T>(path: string): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}/api/public${path}`, {
      next: { revalidate: PUBLIC_REVALIDATE_SECONDS }
    });
  } catch {
    throw new PublicApiError(503);
  }

  if (!response.ok) {
    throw new PublicApiError(response.status);
  }

  try {
    return (await response.json()) as T;
  } catch {
    throw new PublicApiError(502);
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

export const fallbackSettings: SiteSettings = {
  clubName: "El1te Spr1nt Athlet1cs",
  slogan: "Youth track and field",
  contactEmail: "",
  phoneNumber: null,
  addressLine1: null,
  addressLine2: null,
  city: null,
  state: null,
  zipCode: null,
  facebookUrl: null,
  instagramUrl: null,
  youTubeUrl: null,
  primaryCtaText: "Registration",
  primaryCtaUrl: "/registration",
  secondaryCtaText: "Contact Us",
  secondaryCtaUrl: "/contact",
  logoUrl: null
};
