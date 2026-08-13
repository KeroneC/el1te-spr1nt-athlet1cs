import { NextResponse, type NextRequest } from "next/server";
import { canonicalHostRedirect } from "@/lib/public/deployment";

export function proxy(request: NextRequest) {
  const target = canonicalHostRedirect(request.url, request.headers.get("host"));
  return target ? NextResponse.redirect(target, 308) : NextResponse.next();
}

export const config = { matcher: "/:path*" };
