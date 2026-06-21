import { NextRequest, NextResponse } from "next/server";
import { ADMIN_SESSION_COOKIE } from "@/lib/admin/server-api";

export async function POST(request: NextRequest) { return clearSession(request); }
export async function GET(request: NextRequest) { return clearSession(request); }

function clearSession(request: NextRequest) {
  const reason = request.nextUrl.searchParams.get("reason");
  const destination = new URL(reason === "expired" ? "/admin/login?reason=expired" : "/admin/login", request.url);
  const response = NextResponse.redirect(destination, { status: 303 });
  response.cookies.set(ADMIN_SESSION_COOKIE, "", {
    httpOnly: true, secure: process.env.NODE_ENV === "production", sameSite: "lax", path: "/", maxAge: 0
  });
  return response;
}
