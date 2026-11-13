import "server-only";
import type { PublicRegistrationSeason } from "@/lib/registration";

export async function getRegistrationSeason(): Promise<PublicRegistrationSeason | null> {
  try {
    const response = await fetch(`${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/public/registration`, {
      cache: "no-store", signal: AbortSignal.timeout(4000), redirect: "error"
    });
    return response.status === 200 ? await response.json() : null;
  } catch { return null; }
}
