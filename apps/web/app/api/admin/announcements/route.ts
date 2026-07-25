import { NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";
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
  return adminErrorResponse(error, "The request could not be completed.", "admin-announcement-create");
}
