import { NextResponse } from "next/server";
import { SUPPORT_REFERENCE_HEADER, validSupportReference } from "@/lib/observability/support-reference";
import { createSupportReference, logUnexpectedWebFailure } from "@/lib/observability/support-reference.server";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";
const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function GET(_: Request, context: { params: Promise<{ slug: string }> }) {
  const { slug } = await context.params;
  if (!slugPattern.test(slug)) return NextResponse.json({ message: "Product not found." }, { status: 404 });
  try {
    const response = await fetch(`${apiBaseUrl}/api/public/store/products/${encodeURIComponent(slug)}`, {
      cache: "no-store"
    });
    const payload = await response.json().catch(() => null) as unknown;
    if (response.ok) return NextResponse.json(payload, { status: 200 });
    if (response.status === 404) return NextResponse.json({ message: "Product not found." }, { status: 404 });
    const referenceId = validSupportReference(response.headers.get(SUPPORT_REFERENCE_HEADER))
      ?? createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "store-product-upstream-failure", status: response.status });
    return NextResponse.json(
      { message: "Product availability could not be refreshed.", referenceId },
      { status: 502, headers: { [SUPPORT_REFERENCE_HEADER]: referenceId } }
    );
  } catch {
    const referenceId = createSupportReference();
    logUnexpectedWebFailure({ referenceId, category: "store-product-service-unavailable", status: 503 });
    return NextResponse.json(
      { message: "Product availability could not be refreshed.", referenceId },
      { status: 503, headers: { [SUPPORT_REFERENCE_HEADER]: referenceId } }
    );
  }
}
