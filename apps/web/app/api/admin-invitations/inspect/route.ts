import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { backendFetch } from "@/lib/admin/server-api";
import type { AdminInvitationDetails } from "@/lib/admin/types";

export async function POST(request: Request) {
  const token = await readToken(request);
  if (!token) return NextResponse.json({ message: "This invitation link is incomplete." }, { status: 400 });
  try {
    return NextResponse.json(await backendFetch<AdminInvitationDetails>("/api/admin-invitations/inspect", {
      method: "POST", body: JSON.stringify({ token })
    }));
  } catch (error) {
    if (error instanceof AdminApiError) return NextResponse.json({ message: error.message }, { status: error.status });
    return NextResponse.json({ message: "The invitation service is temporarily unavailable." }, { status: 503 });
  }
}

async function readToken(request: Request): Promise<string> {
  try {
    const value = await request.json() as { token?: unknown };
    return typeof value.token === "string" ? value.token : "";
  } catch { return ""; }
}
