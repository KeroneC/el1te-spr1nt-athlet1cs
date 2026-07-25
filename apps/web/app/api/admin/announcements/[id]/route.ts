import { NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";
import type { AdminAnnouncement, AnnouncementWriteRequest } from "@/lib/admin/types";

type Context = { params: Promise<{ id: string }> };

export async function PUT(request: Request, context: Context) {
  const { id } = await context.params;
  try {
    const body = await request.json() as AnnouncementWriteRequest;
    return NextResponse.json(await adminApiFetch<AdminAnnouncement>(`/api/admin/announcements/${encodeURIComponent(id)}`, {
      method: "PUT", body: JSON.stringify(body)
    }));
  } catch (error) { return apiError(error); }
}

export async function DELETE(_request: Request, context: Context) {
  const { id } = await context.params;
  try {
    await adminApiFetch<null>(`/api/admin/announcements/${encodeURIComponent(id)}`, { method: "DELETE" });
    return new NextResponse(null, { status: 204 });
  } catch (error) { return apiError(error); }
}

function apiError(error: unknown) {
  return adminErrorResponse(error, "The request could not be completed.", "admin-announcement-mutation");
}
