import "server-only";
import { redirect } from "next/navigation";
import { AdminApiError } from "./api-error";
import { adminApiFetch } from "./server-api";
import type { CurrentUser } from "./types";
import { isAdminRole } from "./validation";

export async function requireAdminUser(): Promise<CurrentUser> {
  try {
    const user = await adminApiFetch<CurrentUser>("/api/auth/me");
    if (!user.isActive || !isAdminRole(user.role)) redirect("/admin/access-denied");
    return user;
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 403) redirect("/admin/access-denied");
    if (error instanceof AdminApiError && error.status === 401) redirect("/api/admin-session/logout?reason=expired");
    throw error;
  }
}

export function handleAdminPageError(error: unknown): never {
  if (error instanceof AdminApiError && error.status === 401) redirect("/api/admin-session/logout?reason=expired");
  if (error instanceof AdminApiError && error.status === 403) redirect("/admin/access-denied");
  throw error;
}
