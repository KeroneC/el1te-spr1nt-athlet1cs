import "server-only";
import type { PublicRegistrationSeason } from "@/lib/registration";

type RegistrationSeasonResult =
  | { state: "loaded"; season: PublicRegistrationSeason }
  | { state: "no-season" | "unavailable"; season: null };

function isSeason(value: unknown): value is PublicRegistrationSeason {
  if (!value || typeof value !== "object") return false;
  const data = value as Record<string, unknown>;
  const nullableText = (field: string) => typeof data[field] === "string" || data[field] === null;
  return Number.isSafeInteger(data.year) &&
    typeof data.isOpen === "boolean" &&
    typeof data.openingDate === "string" &&
    /^\d{4}-\d{2}-\d{2}$/.test(data.openingDate) &&
    Number.isFinite(Date.parse(data.openingDate)) &&
    Array.isArray(data.ageGroups) && data.ageGroups.every(group => typeof group === "string") &&
    ["title", "practiceLocation", "practiceSchedule", "fees", "replyWindow"].every(nullableText);
}

export async function getRegistrationSeason(): Promise<RegistrationSeasonResult> {
  try {
    const response = await fetch(`${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/public/registration`, {
      cache: "no-store", signal: AbortSignal.timeout(4000), redirect: "error"
    });
    if (response.status === 204) return { state: "no-season", season: null };
    if (response.status === 200) {
      const data: unknown = await response.json();
      if (isSeason(data)) return { state: "loaded", season: data };
    }
  } catch { /* Fail closed without misrepresenting an unavailable service. */ }
  return { state: "unavailable", season: null };
}
