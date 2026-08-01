import { NextResponse } from "next/server";
import { adminErrorResponse } from "@/lib/admin/error-response";
import { backendFetch } from "@/lib/admin/server-api";

export async function POST(request: Request) {
  let email = "";
  try { email = String((await request.json() as { email?: unknown }).email ?? "").trim(); }
  catch { /* generic response below */ }
  if (!/^\S+@\S+\.\S+$/.test(email))
    return NextResponse.json({ message: "Enter a valid email address." }, { status: 400 });
  try {
    await backendFetch("/api/auth/admin/password-reset/request", { method: "POST", body: JSON.stringify({ email }) });
    return NextResponse.json({ message: "If an eligible account exists, a password reset message has been sent." }, { status: 202 });
  } catch (error) {
    return adminErrorResponse(error, "Password recovery is temporarily unavailable.", "admin-password-reset-request");
  }
}
