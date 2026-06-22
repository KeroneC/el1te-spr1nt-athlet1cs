import "server-only";
import { cookies } from "next/headers";
import { AdminApiError, safeProblem } from "./api-error";

export const ADMIN_SESSION_COOKIE = "el1te_admin_session";
const apiBaseUrl = (process.env.API_BASE_URL ?? "https://localhost:7171").replace(/\/$/, "");

export async function backendFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  return request<T>(path, init);
}

export async function adminApiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = (await cookies()).get(ADMIN_SESSION_COOKIE)?.value;
  if (!token) throw new AdminApiError(401, "Authentication is required.");
  return request<T>(path, {
    ...init,
    headers: { ...init.headers, Authorization: `Bearer ${token}` }
  });
}

async function request<T>(path: string, init: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Accept: "application/json",
        ...(init.body && !(init.body instanceof FormData) ? { "Content-Type": "application/json" } : {}),
        ...init.headers
      }
    });
  } catch {
    throw new AdminApiError(503, "The admin service is temporarily unavailable.");
  }

  const value = response.status === 204 ? null : await readJson(response);
  if (!response.ok) throw safeProblem(response.status, value);
  return value as T;
}

async function readJson(response: Response): Promise<unknown> {
  const type = response.headers.get("content-type") ?? "";
  if (!type.includes("application/json") && !type.includes("application/problem+json")) return null;
  try { return await response.json(); } catch { return null; }
}
