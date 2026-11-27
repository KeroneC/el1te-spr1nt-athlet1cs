export type RegistrationChannel = "facebook" | "instagram" | "direct";
export const registrationChannel = (value: unknown): RegistrationChannel =>
  value === "facebook" || value === "instagram" ? value : "direct";

export type PublicRegistrationSeason = {
  year: number; title: string | null; openingDate: string; practiceLocation: string | null; practiceSchedule: string | null;
  fees: string | null; replyWindow: string | null; isOpen: boolean;
};

export const registrationOpeningDate = "2026-11-27";
export const registrationApplicationVersion = 3;
export const registrationMinimumAge = 7;
export const registrationMaximumAge = 18;
export const registrationAgeLabel = `Ages ${registrationMinimumAge}–${registrationMaximumAge}`;

export function openingPhase(value: string, now = new Date()): "upcoming" | "today" | "closed" {
  const dateParts = new Intl.DateTimeFormat("en-US", {
    year: "numeric", month: "2-digit", day: "2-digit", timeZone: "America/New_York"
  }).formatToParts(now);
  const part = (kind: "year" | "month" | "day") => dateParts.find(item => item.type === kind)?.value;
  const today = `${part("year")}-${part("month")}-${part("day")}`;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return "closed";
  return value > today ? "upcoming" : value === today ? "today" : "closed";
}

export function registrationDate(value: string, includeYear = false) {
  return new Intl.DateTimeFormat("en-US", { weekday: "long", month: "long", day: "numeric",
    year: includeYear ? "numeric" : undefined, timeZone: "UTC" })
    .format(new Date(`${value}T00:00:00Z`));
}
