import { NextResponse, type NextRequest } from "next/server";
import { canonicalHostRedirect } from "@/lib/public/deployment";
import { isRetiredAdminRoute } from "@/lib/admin/retired-routes";
import { runtimeCspHeader } from "@/lib/public/security-headers";

function withRuntimeCsp(response: NextResponse) {
  const header = runtimeCspHeader();
  response.headers.delete("Content-Security-Policy");
  response.headers.delete("Content-Security-Policy-Report-Only");
  if (header) response.headers.set(header.name, header.value);
  return response;
}

export function proxy(request: NextRequest) {
  const target = canonicalHostRedirect(request.url, request.headers.get("host"));
  if (target) return withRuntimeCsp(NextResponse.redirect(target, 308));

  if (isRetiredAdminRoute(request.nextUrl.pathname)) {
    return withRuntimeCsp(new NextResponse("Not Found", {
      status: 404,
      headers: { "Content-Type": "text/plain; charset=utf-8" },
    }));
  }

  return withRuntimeCsp(NextResponse.next());
}

export const config = { matcher: "/:path*" };
