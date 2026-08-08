import "server-only";
import { NextResponse } from "next/server";
import { SUPPORT_REFERENCE_HEADER, validSupportReference } from "@/lib/observability/support-reference";
import { createSupportReference, logUnexpectedWebFailure } from "@/lib/observability/support-reference.server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";

export async function forwardStoreOrderRequest(path: string, body: unknown) {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: "POST",
      cache: "no-store",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    const payload = await response.json().catch(() => ({})) as Record<string, unknown>;
    if (response.ok) return { response, payload };
    if (response.status < 500) {
      return {
        response,
        payload: {
          ...payload,
          message: typeof payload.detail === "string" ? payload.detail :
            typeof payload.title === "string" ? payload.title : "The request could not be completed."
        }
      };
    }
    const referenceId = validSupportReference(response.headers.get(SUPPORT_REFERENCE_HEADER))
      ?? validSupportReference(typeof payload.referenceId === "string" ? payload.referenceId : null)
      ?? createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "store-order-upstream-failure", status: response.status });
    return { response, payload: { message: "The store is temporarily unavailable. Please try again.", referenceId } };
  } catch {
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "store-order-service-unavailable", status: 503 });
    return {
      response: new Response(null, { status: 503, headers: { [SUPPORT_REFERENCE_HEADER]: referenceId } }),
      payload: { message: "The store is temporarily unavailable. Please try again.", referenceId }
    };
  }
}

export function storeOrderJson(response: Response, payload: Record<string, unknown>) {
  const referenceId = validSupportReference(response.headers.get(SUPPORT_REFERENCE_HEADER))
    ?? validSupportReference(typeof payload.referenceId === "string" ? payload.referenceId : null);
  return NextResponse.json(payload, {
    status: response.status,
    headers: referenceId ? { [SUPPORT_REFERENCE_HEADER]: referenceId } : undefined
  });
}
