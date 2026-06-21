import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { ADMIN_SESSION_COOKIE, backendFetch } from "@/lib/admin/server-api";
import type { CurrentUser, LoginRequest, LoginResponse } from "@/lib/admin/types";
import { isAdminRole, validateLoginInput } from "@/lib/admin/validation";

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
    const login = await backendFetch<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: body.email.trim(), password: body.password })
    });
    const user = await backendFetch<CurrentUser>("/api/auth/me", {
      headers: { Authorization: `Bearer ${login.accessToken}` }
    });
    if (!user.isActive || !isAdminRole(user.role)) {
      return NextResponse.json({ message: "This account does not have administrative access." }, { status: 403 });
    }

    const response = NextResponse.json({ user });
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
    return NextResponse.json({ message: "The admin service is unavailable. Please try again." }, { status: 503 });
  }
}
