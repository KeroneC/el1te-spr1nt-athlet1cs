import { NextRequest, NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";

export async function GET(request: NextRequest) {
  try {
    const params = new URLSearchParams({ isActive: "true" });
    for (const name of ["search", "page", "pageSize"]) {
      const value = request.nextUrl.searchParams.get(name);
      if (value) params.set(name, value);
    }
    return NextResponse.json(await adminApiFetch(`/api/admin/media?${params}`));
  } catch (error) {
    return adminErrorResponse(error, "Media could not be loaded.", "admin-media-options");
  }
}
