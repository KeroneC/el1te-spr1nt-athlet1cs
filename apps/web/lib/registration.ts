export type RegistrationChannel = "facebook" | "instagram" | "direct";
export const registrationChannel = (value: unknown): RegistrationChannel =>
  value === "facebook" || value === "instagram" ? value : "direct";

export type PublicRegistrationSeason = {
  year: number; title: string | null; openingDate: string; practiceLocation: string | null; practiceSchedule: string | null;
  fees: string | null; replyWindow: string | null; ageGroups: string[]; isOpen: boolean;
};

export const registrationOpeningDate = "2026-11-27";
export function registrationDate(value: string) {
  return new Intl.DateTimeFormat("en-US", { weekday: "long", month: "long", day: "numeric", timeZone: "UTC" })
    .format(new Date(`${value}T00:00:00Z`));
}
