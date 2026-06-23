import { NextRequest, NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { AdminApiError } from "@/lib/admin/api-error";

export async function POST(request: NextRequest) {
  try {
    const result = await adminApiFetch("/api/admin/media", { method: "POST", body: await request.formData() });
    return NextResponse.json(result, { status: 201 });
  } catch (error) {
    const status = error instanceof AdminApiError ? error.status : 500;
    return NextResponse.json(error instanceof AdminApiError ? { message: error.message, errors: error.fieldErrors } : { message: "Upload failed." }, { status });
  }
}
