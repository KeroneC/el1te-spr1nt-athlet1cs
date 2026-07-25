import "server-only";
import { cookies } from "next/headers";
import { AdminApiError, safeProblem } from "./api-error";
import { SUPPORT_REFERENCE_HEADER, validSupportReference } from "@/lib/observability/support-reference";
import { createSupportReference, logUnexpectedWebFailure } from "@/lib/observability/support-reference.server";

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
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "admin-api-unavailable", status: 503 });
    throw new AdminApiError(503, "The admin service is temporarily unavailable.", {}, referenceId);
  }

  let value: unknown;
  try {
    value = response.status === 204 ? null : await readJson(response);
  } catch {
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "admin-api-invalid-response", status: 502 });
    throw new AdminApiError(502, "The admin service returned an invalid response.", {}, referenceId);
  }
  if (!response.ok) {
    const error = safeProblem(response.status, value);
    const headerReference = response.status >= 500
      ? validSupportReference(response.headers.get(SUPPORT_REFERENCE_HEADER))
      : null;
    let referenceId = error.referenceId ?? headerReference;
    if (response.status >= 500 && !referenceId) {
      referenceId = createSupportReference();
      logUnexpectedWebFailure({ referenceId, category: "admin-api-upstream-failure", status: response.status });
    }
    throw new AdminApiError(error.status, error.message, error.fieldErrors, referenceId);
  }
  return value as T;
}

async function readJson(response: Response): Promise<unknown> {
  const type = response.headers.get("content-type") ?? "";
  if (!type.includes("application/json") && !type.includes("application/problem+json")) return null;
  return response.json();
}
