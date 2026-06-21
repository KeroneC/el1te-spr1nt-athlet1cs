import type {
  AnnouncementFilters,
  AnnouncementState,
  AnnouncementWriteRequest,
  LoginRequest
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

function isWebOrRootRelativeUrl(value: string): boolean {
  if (value.startsWith("/")) return true;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}
