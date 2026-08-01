import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminErrorResponse } from "@/lib/admin/error-response";
import { backendFetch } from "@/lib/admin/server-api";

export async function POST(request: Request) {
  try {
    const input = await request.json() as Record<string, unknown>;
    await backendFetch("/api/auth/admin/password-reset/complete", {
      method: "POST", body: JSON.stringify({ token: input.token, password: input.password, confirmPassword: input.confirmPassword })
    });
    return new NextResponse(null, { status: 204 });
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 400)
      return NextResponse.json({ message: error.message, errors: error.fieldErrors }, { status: 400 });
    return adminErrorResponse(error, "Password reset is temporarily unavailable.", "admin-password-reset-complete");
  }
}
