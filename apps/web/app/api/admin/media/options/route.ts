import { NextRequest, NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";

export async function GET(request: NextRequest) {
  try {
    const params = new URLSearchParams({ isActive: "true" });
    for (const name of ["search", "page", "pageSize"]) {
      const value = request.nextUrl.searchParams.get(name);
      if (value) params.set(name, value);
    }
    return NextResponse.json(await adminApiFetch(`/api/admin/media?${params}`));
  } catch (error) {
    if (error instanceof AdminApiError) return NextResponse.json({ message: error.message }, { status: error.status });
    return NextResponse.json({ message: "Media could not be loaded." }, { status: 500 });
  }
}
