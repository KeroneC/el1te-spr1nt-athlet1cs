export type EventType =
  | "Other"
  | "Practice"
  | "Meet"
  | "Fundraiser"
  | "TeamEvent"
  | "RegistrationDeadline";

export type SponsorTier =
  | "Platinum"
  | "Gold"
  | "Silver"
  | "Bronze"
  | "Community"
  | "Other";

export type InquiryType =
  | "General"
  | "Parent"
  | "Sponsor"
  | "Volunteer"
  | "Registration"
  | "Other";

export interface SiteSettings {
  clubName: string;
  slogan: string;
  contactEmail: string;
  phoneNumber: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  zipCode: string | null;
  facebookUrl: string | null;
  instagramUrl: string | null;
  youTubeUrl: string | null;
  primaryCtaText: string;
  primaryCtaUrl: string;
  secondaryCtaText: string;
  secondaryCtaUrl: string;
  logoUrl: string | null;
}

export interface ContentBlock {
  key: string;
  title: string;
  summary: string | null;
  body: string;
  imageUrl: string | null;
  ctaText: string | null;
  ctaUrl: string | null;
  displayOrder: number;
}

export interface AnnouncementListItem {
  title: string;
  slug: string;
  summary: string;
  imageUrl: string | null;
  isFeatured: boolean;
  publishDateUtc: string | null;
}

export interface AnnouncementDetail extends AnnouncementListItem {
  body: string;
}

export interface EventListItem {
  title: string;
  slug: string;
  eventType: EventType;
  startDateTimeUtc: string;
  endDateTimeUtc: string | null;
  locationName: string;
  imageUrl: string | null;
  isFeatured: boolean;
}

export interface EventDetail extends EventListItem {
  address: string | null;
  description: string;
  registrationUrl: string | null;
}

export interface Coach {
  firstName: string;
  lastName: string;
  role: string;
  bio: string;
  imageUrl: string | null;
  email: string | null;
  displayOrder: number;
}

export interface Sponsor {
  name: string;
  slug: string;
  tier: SponsorTier;
  logoUrl: string | null;
  websiteUrl: string | null;
  description: string | null;
  displayOrder: number;
}

export interface Faq {
  question: string;
  answer: string;
  category: string;
  displayOrder: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ContactRequest {
  name: string;
  email: string;
  phone: string | null;
  inquiryType: InquiryType;
  message: string;
}

export interface ContactCreatedResponse {
  id: string;
  message: string;
}

export interface ValidationProblem {
  title?: string;
  errors?: Record<string, string[]>;
}
export interface GalleryAlbumListItem {
  title: string; slug: string; description: string; coverImageUrl: string | null;
  coverAltText: string | null; eventDateUtc: string | null; imageCount: number;
}
export interface GalleryImage {
  publicUrl: string; altText: string; caption: string | null; width: number; height: number; displayOrder: number;
}
export interface GalleryAlbum {
  title: string; slug: string; description: string; eventDateUtc: string | null; images: GalleryImage[];
}
