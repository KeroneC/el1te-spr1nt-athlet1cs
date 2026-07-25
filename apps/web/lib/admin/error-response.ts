import { NextResponse } from "next/server";
import { AdminApiError } from "./api-error";
import { SUPPORT_REFERENCE_HEADER } from "@/lib/observability/support-reference";
import { createSupportReference, logUnexpectedWebFailure } from "@/lib/observability/support-reference.server";

export function adminErrorResponse(
  error: unknown,
  fallbackMessage = "The request could not be completed.",
  category = "admin-route-failure"
) {
  if (error instanceof AdminApiError) {
    const referenceId = error.status >= 500
      ? error.referenceId ?? createSupportReference()
      : null;
    if (referenceId && !error.referenceId) {
      logUnexpectedWebFailure({ referenceId, category, status: error.status });
    }
    return response(error.status, {
      message: error.message,
      errors: error.fieldErrors,
      referenceId
    }, referenceId);
  }

  const referenceId = createSupportReference();
  logUnexpectedWebFailure({ referenceId, category });
  return response(500, { message: fallbackMessage, referenceId }, referenceId);
}

function response(
  status: number,
  body: { message: string; errors?: Record<string, string[]>; referenceId?: string | null },
  referenceId: string | null
) {
  const headers = referenceId ? { [SUPPORT_REFERENCE_HEADER]: referenceId } : undefined;
  return NextResponse.json(body, { status, headers });
}
