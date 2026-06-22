import "server-only";
import { notFound } from "next/navigation";
import { AdminApiError } from "./api-error";
import { handleAdminPageError } from "./auth";
import { adminApiFetch } from "./server-api";

export async function getAdminItem<T>(path: string): Promise<T> {
  try { return await adminApiFetch<T>(path); }
  catch (error) { if (error instanceof AdminApiError && error.status === 404) notFound(); handleAdminPageError(error); }
}

export async function getAdminList<T>(path: string): Promise<T> {
  try { return await adminApiFetch<T>(path); } catch (error) { handleAdminPageError(error); }
}
