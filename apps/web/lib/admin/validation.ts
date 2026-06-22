import type {
  AnnouncementFilters,
  AnnouncementState,
  AnnouncementWriteRequest,
  LoginRequest,
  EventWriteRequest,
  CoachWriteRequest,
  SponsorWriteRequest,
  FaqWriteRequest,
  ContentBlockWriteRequest,
  SiteSettingsWriteRequest
} from "./types";

export type FieldErrors = Record<string, string[]>;

export function validateLoginInput(input: LoginRequest): FieldErrors {
  const errors: FieldErrors = {};
  if (!input.email.trim()) errors.email = ["Email is required."];
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.email)) errors.email = ["Enter a valid email address."];
  if (!input.password) errors.password = ["Password is required."];
  return errors;
}

export function validateAnnouncement(input: AnnouncementWriteRequest): FieldErrors {
  const errors: FieldErrors = {};
  if (!input.title.trim()) errors.title = ["Title is required."];
  else if (input.title.length > 200) errors.title = ["Title must be 200 characters or fewer."];
  if (!input.summary.trim()) errors.summary = ["Summary is required."];
  else if (input.summary.length > 200) errors.summary = ["Summary must be 200 characters or fewer."];
  if (!input.body.trim()) errors.body = ["Body is required."];
  if (input.imageUrl && input.imageUrl.length > 500) errors.imageUrl = ["Image URL must be 500 characters or fewer."];
  if (input.imageUrl && !isWebOrRootRelativeUrl(input.imageUrl)) errors.imageUrl = ["Enter a valid HTTP(S) or root-relative URL."];
  if (input.publishDateUtc && input.expirationDateUtc && new Date(input.expirationDateUtc) <= new Date(input.publishDateUtc)) {
    errors.expirationDateUtc = ["Expiration must be after publication."];
  }
  return errors;
}

export function buildAnnouncementQuery(filters: AnnouncementFilters, pageSize = 20): string {
  const params = new URLSearchParams();
  if (filters.search?.trim()) params.set("search", filters.search.trim());
  if (filters.isPublished === "true" || filters.isPublished === "false") params.set("isPublished", filters.isPublished);
  if (filters.isFeatured === "true" || filters.isFeatured === "false") params.set("isFeatured", filters.isFeatured);
  if (filters.includeExpired === "true") params.set("includeExpired", "true");
  const page = Number.parseInt(filters.page ?? "1", 10);
  params.set("page", Number.isFinite(page) && page > 0 ? String(page) : "1");
  params.set("pageSize", String(pageSize));
  return params.toString();
}

export function getAnnouncementState(
  announcement: Pick<AnnouncementWriteRequest, "isPublished" | "publishDateUtc" | "expirationDateUtc">,
  now = new Date()
): AnnouncementState {
  if (!announcement.isPublished) return "Draft";
  if (announcement.expirationDateUtc && new Date(announcement.expirationDateUtc) <= now) return "Expired";
  if (announcement.publishDateUtc && new Date(announcement.publishDateUtc) > now) return "Scheduled";
  return "Published";
}

export function isAdminRole(role: string): role is "Admin" | "SuperAdmin" {
  return role === "Admin" || role === "SuperAdmin";
}

export function validateEvent(input: EventWriteRequest): FieldErrors {
  const errors = required({ title: input.title, locationName: input.locationName, description: input.description });
  if (!input.startDateTimeUtc || Number.isNaN(Date.parse(input.startDateTimeUtc))) errors.startDateTimeUtc = ["Start date and time are required."];
  if (input.endDateTimeUtc && new Date(input.endDateTimeUtc) < new Date(input.startDateTimeUtc)) errors.endDateTimeUtc = ["End must be after the start."];
  urlError(errors, "registrationUrl", input.registrationUrl); urlError(errors, "imageUrl", input.imageUrl);
  return errors;
}

export function validateCoach(input: CoachWriteRequest): FieldErrors {
  const errors = required({ firstName: input.firstName, lastName: input.lastName, role: input.role, bio: input.bio });
  if (input.email && !isEmail(input.email)) errors.email = ["Enter a valid email address."];
  if (input.isEmailPublic && !input.email) errors.email = ["An email is required before it can be public."];
  orderError(errors, input.displayOrder); urlError(errors, "imageUrl", input.imageUrl);
  return errors;
}

export function validateSponsor(input: SponsorWriteRequest): FieldErrors {
  const errors = required({ name: input.name }); orderError(errors, input.displayOrder);
  urlError(errors, "websiteUrl", input.websiteUrl); urlError(errors, "logoUrl", input.logoUrl); return errors;
}

export function validateFaq(input: FaqWriteRequest): FieldErrors {
  const errors = required({ question: input.question, answer: input.answer, category: input.category });
  orderError(errors, input.displayOrder); return errors;
}

export function validateContentBlock(input: ContentBlockWriteRequest): FieldErrors {
  const errors = required({ key: input.key, title: input.title, body: input.body }); orderError(errors, input.displayOrder);
  urlError(errors, "imageUrl", input.imageUrl); urlError(errors, "ctaUrl", input.ctaUrl); return errors;
}

export function validateSiteSettings(input: SiteSettingsWriteRequest): FieldErrors {
  const errors = required({ clubName: input.clubName, slogan: input.slogan, contactEmail: input.contactEmail,
    primaryCtaText: input.primaryCtaText, primaryCtaUrl: input.primaryCtaUrl,
    secondaryCtaText: input.secondaryCtaText, secondaryCtaUrl: input.secondaryCtaUrl });
  if (input.contactEmail && !isEmail(input.contactEmail)) errors.contactEmail = ["Enter a valid email address."];
  for (const field of ["facebookUrl", "instagramUrl", "youtubeUrl", "primaryCtaUrl", "secondaryCtaUrl", "logoUrl"] as const) urlError(errors, field, input[field]);
  return errors;
}

export function buildListQuery<T extends object>(filters: T, allowed: readonly string[], pageSize = 20): string {
  const values = filters as Record<string, string | undefined>;
  const params = new URLSearchParams();
  for (const key of allowed) { const value = values[key]?.trim(); if (value) params.set(key, value); }
  const page = Number.parseInt(values.page ?? "1", 10); params.set("page", Number.isFinite(page) && page > 0 ? String(page) : "1");
  params.set("pageSize", String(pageSize)); return params.toString();
}

export function nullable(value: FormDataEntryValue | null): string | null { const text = String(value ?? "").trim(); return text || null; }
export function text(value: FormDataEntryValue | null): string { return String(value ?? "").trim(); }
export function number(value: FormDataEntryValue | null): number { return Number.parseInt(String(value ?? "0"), 10) || 0; }
export function iso(value: FormDataEntryValue | null): string | null { const textValue = text(value); return textValue ? new Date(textValue).toISOString() : null; }
export function toLocalDateTime(value?: string | null): string { if (!value) return ""; const date = new Date(value); return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16); }

function required(values: Record<string, string>): FieldErrors { const errors: FieldErrors = {}; for (const [key, value] of Object.entries(values)) if (!value.trim()) errors[key] = ["This field is required."]; return errors; }
function orderError(errors: FieldErrors, value: number) { if (value < 0) errors.displayOrder = ["Display order cannot be negative."]; }
function urlError(errors: FieldErrors, field: string, value: string | null) { if (value && !isWebOrRootRelativeUrl(value)) errors[field] = ["Enter a valid HTTP(S) or root-relative URL."]; }
function isEmail(value: string) { return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value); }

function isWebOrRootRelativeUrl(value: string): boolean {
  if (value.startsWith("/")) return true;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}
