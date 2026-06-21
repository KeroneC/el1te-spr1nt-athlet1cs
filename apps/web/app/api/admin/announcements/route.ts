import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminAnnouncement, AnnouncementWriteRequest } from "@/lib/admin/types";

export async function POST(request: Request) {
  try {
    const body = await request.json() as AnnouncementWriteRequest;
    return NextResponse.json(await adminApiFetch<AdminAnnouncement>("/api/admin/announcements", {
      method: "POST", body: JSON.stringify(body)
    }), { status: 201 });
  } catch (error) { return apiError(error); }
}

function apiError(error: unknown) {
  if (error instanceof AdminApiError) return NextResponse.json({ message: error.message, errors: error.fieldErrors }, { status: error.status });
  return NextResponse.json({ message: "The request could not be completed." }, { status: 500 });
}
