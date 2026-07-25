import { NextRequest, NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";

export async function POST(request: NextRequest) {
  try {
    const result = await adminApiFetch("/api/admin/media", { method: "POST", body: await request.formData() });
    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    return adminErrorResponse(error, "Upload failed.", "admin-media-upload");
  }
}
