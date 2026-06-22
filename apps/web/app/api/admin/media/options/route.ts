import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";

export async function GET() {
  try {
    return NextResponse.json(await adminApiFetch("/api/admin/media?isActive=true&pageSize=100"));
  } catch (error) {
    if (error instanceof AdminApiError) return NextResponse.json({ message: error.message }, { status: error.status });
    return NextResponse.json({ message: "Media could not be loaded." }, { status: 500 });
  }
}
