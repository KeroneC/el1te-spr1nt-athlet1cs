import { NextResponse } from "next/server";
import { adminErrorResponse } from "@/lib/admin/error-response";
import { backendFetch } from "@/lib/admin/server-api";

export async function POST(request: Request) {
  try {
    const token = String((await request.json() as { token?: unknown }).token ?? "");
    const result = await backendFetch<{ isValid: boolean }>("/api/auth/admin/password-reset/inspect", { method: "POST", body: JSON.stringify({ token }) });
    return NextResponse.json(result);
  } catch (error) {
    return adminErrorResponse(error, "This password reset link could not be checked.", "admin-password-reset-inspect");
  }
}
