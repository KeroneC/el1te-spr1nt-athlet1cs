export type UserRole = "Parent" | "Athlete" | "Coach" | "Admin" | "SuperAdmin";

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: UserRole;
  };
}

export interface AdminAnnouncement {
  id: string;
  title: string;
  slug: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface AnnouncementWriteRequest {
  title: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export interface AnnouncementFilters {
  search?: string;
  isPublished?: string;
  isFeatured?: string;
  includeExpired?: string;
  page?: string;
}

export type AnnouncementState = "Draft" | "Scheduled" | "Published" | "Expired";

export const EVENT_TYPES = ["Other", "Practice", "Meet", "Fundraiser", "TeamEvent", "RegistrationDeadline"] as const;
export type EventType = typeof EVENT_TYPES[number];
export const SPONSOR_TIERS = ["Platinum", "Gold", "Silver", "Bronze", "Community", "Other"] as const;
export type SponsorTier = typeof SPONSOR_TIERS[number];
export const INQUIRY_TYPES = ["General", "Parent", "Sponsor", "Volunteer", "Registration", "Other"] as const;
export type InquiryType = typeof INQUIRY_TYPES[number];
export const CONTACT_STATUSES = ["New", "Read", "Resolved", "Archived"] as const;
export type ContactSubmissionStatus = typeof CONTACT_STATUSES[number];

export interface AdminEvent {
  id: string; title: string; slug: string; eventType: EventType; startDateTimeUtc: string;
  endDateTimeUtc: string | null; locationName: string; address: string | null; description: string;
  registrationUrl: string | null; imageUrl: string | null; isFeatured: boolean; isPublished: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type EventWriteRequest = Omit<AdminEvent, "id" | "slug" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminCoach {
  id: string; firstName: string; lastName: string; role: string; bio: string; imageUrl: string | null;
  email: string | null; isEmailPublic: boolean; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type CoachWriteRequest = Omit<AdminCoach, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminSponsor {
  id: string; name: string; slug: string; tier: SponsorTier; logoUrl: string | null;
  websiteUrl: string | null; description: string | null; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type SponsorWriteRequest = Omit<AdminSponsor, "id" | "slug" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminFaq {
  id: string; question: string; answer: string; category: string; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type FaqWriteRequest = Omit<AdminFaq, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminContentBlock {
  id: string; key: string; title: string; summary: string | null; body: string; imageUrl: string | null;
  ctaText: string | null; ctaUrl: string | null; displayOrder: number; isPublished: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type ContentBlockWriteRequest = Omit<AdminContentBlock, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminSiteSettings {
  id: string; clubName: string; slogan: string; contactEmail: string; phoneNumber: string | null;
  addressLine1: string | null; addressLine2: string | null; city: string | null; state: string | null;
  zipCode: string | null; facebookUrl: string | null; instagramUrl: string | null; youtubeUrl: string | null;
  primaryCtaText: string; primaryCtaUrl: string; secondaryCtaText: string; secondaryCtaUrl: string;
  logoUrl: string | null; createdAtUtc: string; updatedAtUtc: string | null;
}
export type SiteSettingsWriteRequest = Omit<AdminSiteSettings, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminContactSubmission {
  id: string; name: string; email: string; phone: string | null; inquiryType: InquiryType;
  message: string; status: ContactSubmissionStatus; createdAtUtc: string; updatedAtUtc: string | null;
}

export interface ListFilters { search?: string; page?: string; }
export interface EventFilters extends ListFilters { eventType?: string; isPublished?: string; isFeatured?: string; fromDate?: string; toDate?: string; }
export interface ActiveFilters extends ListFilters { isActive?: string; }
export interface SponsorFilters extends ActiveFilters { tier?: string; }
export interface FaqFilters extends ActiveFilters { category?: string; }
export interface ContentFilters extends ListFilters { isPublished?: string; }
export interface ContactFilters extends ListFilters { status?: string; inquiryType?: string; fromDate?: string; toDate?: string; }

export interface AdminMediaAsset {
  id: string; originalFileName: string; contentType: string; fileExtension: string; fileSizeBytes: number;
  width: number; height: number; title: string; altText: string; caption: string | null;
  publicUrl: string; isActive: boolean; createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminGalleryAlbumListItem {
  id: string; title: string; slug: string; description: string; coverMediaAssetId: string | null;
  coverImageUrl: string | null; isPublished: boolean; eventDateUtc: string | null;
  displayOrder: number; imageCount: number; createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminGalleryAlbumMedia {
  id: string; mediaAssetId: string; publicUrl: string; title: string; altText: string; caption: string | null;
  altTextOverride: string | null; captionOverride: string | null; displayOrder: number;
  isActive: boolean; width: number; height: number;
}
export interface AdminGalleryAlbum extends Omit<AdminGalleryAlbumListItem, "coverImageUrl" | "imageCount"> {
  media: AdminGalleryAlbumMedia[];
}
