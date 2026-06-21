import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";
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
  if (error instanceof AdminApiError) return NextResponse.json({ message: error.message, errors: error.fieldErrors }, { status: error.status });
  return NextResponse.json({ message: "The request could not be completed." }, { status: 500 });
}
