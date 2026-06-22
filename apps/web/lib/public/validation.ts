import type { ContactRequest, InquiryType } from "./types";

export type ContactErrors = Partial<Record<keyof ContactRequest, string>>;

export const INQUIRY_TYPES: InquiryType[] = [
  "General",
  "Parent",
  "Sponsor",
  "Volunteer",
  "Registration",
  "Other"
];

export function validateContact(input: ContactRequest): ContactErrors {
  const errors: ContactErrors = {};
  if (!input.name.trim()) errors.name = "Enter your name.";
  else if (input.name.trim().length > 200) errors.name = "Name must be 200 characters or fewer.";
  if (!input.email.trim()) errors.email = "Enter your email address.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.email)) errors.email = "Enter a valid email address.";
  if (input.phone && input.phone.length > 50) errors.phone = "Phone must be 50 characters or fewer.";
  if (!INQUIRY_TYPES.includes(input.inquiryType)) errors.inquiryType = "Choose an inquiry type.";
  if (!input.message.trim()) errors.message = "Enter a message.";
  else if (input.message.trim().length > 4000) errors.message = "Message must be 4,000 characters or fewer.";
  return errors;
}

export function buildAnnouncementQuery(page: number, featured?: boolean): string {
  const query = new URLSearchParams({ page: String(Math.max(1, page)), pageSize: "9" });
  if (featured !== undefined) query.set("featured", String(featured));
  return query.toString();
}

export function buildEventQuery(page: number, eventType?: string, upcomingOnly = true): string {
  const query = new URLSearchParams({
    page: String(Math.max(1, page)),
    pageSize: "9",
    upcomingOnly: String(upcomingOnly)
  });
  if (eventType) query.set("eventType", eventType);
  return query.toString();
}
