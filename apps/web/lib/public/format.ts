import type { EventType } from "./types";

export const EVENT_TYPES: EventType[] = [
  "Practice",
  "Meet",
  "Fundraiser",
  "TeamEvent",
  "RegistrationDeadline",
  "Other"
];

export function labelEnum(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export function formatDate(value: string | null): string {
  if (!value) return "Date to be announced";
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeZone: "UTC"
  }).format(new Date(value));
}

export function formatEventDate(start: string, end: string | null): string {
  const formatter = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone: "UTC",
    timeZoneName: "short"
  });
  const startLabel = formatter.format(new Date(start));
  return end ? `${startLabel} - ${formatter.format(new Date(end))}` : startLabel;
}

export function isExternalUrl(url: string): boolean {
  return /^https?:\/\//i.test(url);
}
