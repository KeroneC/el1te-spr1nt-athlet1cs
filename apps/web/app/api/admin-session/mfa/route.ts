import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminErrorResponse } from "@/lib/admin/error-response";
import { ADMIN_SESSION_COOKIE, backendFetch } from "@/lib/admin/server-api";
import type { CurrentUser, LoginResponse } from "@/lib/admin/types";

const MFA_COOKIE = "el1te_admin_mfa";

export async function POST(request: Request) {
  const challengeToken = (await cookies()).get(MFA_COOKIE)?.value;
  let code = "";
  try { code = String((await request.json() as { code?: unknown }).code ?? "").trim(); }
  catch { /* handled below */ }
  if (!challengeToken || !/^\d{6}$/.test(code))
    return NextResponse.json({ message: "Enter the six-digit verification code." }, { status: 400 });

  try {
    const login = await backendFetch<LoginResponse>("/api/auth/admin/mfa/verify", {
      method: "POST", body: JSON.stringify({ challengeToken, code })
    });
    const user = await backendFetch<CurrentUser>("/api/auth/me", {
      headers: { Authorization: `Bearer ${login.accessToken}` }
    });
    const response = NextResponse.json({ user });
    response.cookies.delete(MFA_COOKIE);
    response.cookies.set(ADMIN_SESSION_COOKIE, login.accessToken, {
      httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax",
      path: "/", expires: new Date(login.expiresAt)
    });
    return response;
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 401)
      return NextResponse.json({ message: "That code is invalid or expired." }, { status: 401 });
    return adminErrorResponse(error, "Verification is temporarily unavailable.", "admin-mfa");
  }
}
