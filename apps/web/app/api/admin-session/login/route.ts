import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { ADMIN_SESSION_COOKIE, backendFetch } from "@/lib/admin/server-api";
import type { AdminLoginResult, CurrentUser, LoginRequest, LoginResponse } from "@/lib/admin/types";
import { isAdminRole, validateLoginInput } from "@/lib/admin/validation";
import { adminErrorResponse } from "@/lib/admin/error-response";

export async function POST(request: Request) {
  let body: LoginRequest;
  try {
    const input = await request.json() as Partial<LoginRequest>;
    body = {
      email: typeof input.email === "string" ? input.email : "",
      password: typeof input.password === "string" ? input.password : ""
    };
  }
  catch { return NextResponse.json({ message: "Enter a valid email and password." }, { status: 400 }); }

  const errors = validateLoginInput(body);
  if (Object.keys(errors).length) return NextResponse.json({ message: "Check the form fields.", errors }, { status: 400 });

  try {
    const result = await backendFetch<AdminLoginResult>("/api/auth/admin/login", {
      method: "POST",
      body: JSON.stringify({ email: body.email.trim(), password: body.password })
    });
    if (result.requiresMfa && result.challengeToken && result.challengeExpiresAtUtc) {
      const response = NextResponse.json({ requiresMfa: true });
      response.cookies.set("el1te_admin_mfa", result.challengeToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "strict",
        path: "/api/admin-session/mfa",
        expires: new Date(result.challengeExpiresAtUtc)
      });
      return response;
    }
    const login = result.authentication as LoginResponse | null;
    if (!login) throw new AdminApiError(502, "The admin service returned an invalid response.");
    const user = await backendFetch<CurrentUser>("/api/auth/me", {
      headers: { Authorization: `Bearer ${login.accessToken}` }
    });
    if (!user.isActive || !isAdminRole(user.role)) {
      return NextResponse.json({ message: "This account does not have administrative access." }, { status: 403 });
    }

    const response = NextResponse.json({ user, requiresMfa: false });
    response.cookies.set(ADMIN_SESSION_COOKIE, login.accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires: new Date(login.expiresAt)
    });
    return response;
  } catch (error) {
    if (error instanceof AdminApiError && error.status === 401) {
      return NextResponse.json({ message: "Email or password is incorrect." }, { status: 401 });
    }
    return adminErrorResponse(error, "The admin service is unavailable. Please try again.", "admin-login");
  }
}
