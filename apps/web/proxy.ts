import { NextResponse, type NextRequest } from "next/server";
import { canonicalHostRedirect } from "@/lib/public/deployment";
import { isRetiredAdminRoute } from "@/lib/admin/retired-routes";

export function proxy(request: NextRequest) {
  const target = canonicalHostRedirect(request.url, request.headers.get("host"));
  if (target) return NextResponse.redirect(target, 308);

  if (isRetiredAdminRoute(request.nextUrl.pathname)) {
    return new NextResponse("Not Found", {
      status: 404,
      headers: { "Content-Type": "text/plain; charset=utf-8" },
    });
  }

  return NextResponse.next();
}

export const config = { matcher: "/:path*" };
