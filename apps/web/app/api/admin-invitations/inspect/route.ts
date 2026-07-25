import { NextResponse } from "next/server";
import { backendFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";
import type { AdminInvitationDetails } from "@/lib/admin/types";

export async function POST(request: Request) {
  const token = await readToken(request);
  if (!token) return NextResponse.json({ message: "This invitation link is incomplete." }, { status: 400 });
  try {
    return NextResponse.json(await backendFetch<AdminInvitationDetails>("/api/admin-invitations/inspect", {
      method: "POST", body: JSON.stringify({ token })
    }));
  } catch (error) {
    return adminErrorResponse(error, "The invitation service is temporarily unavailable.", "invitation-inspect");
  }
}

async function readToken(request: Request): Promise<string> {
  try {
    const value = await request.json() as { token?: unknown };
    return typeof value.token === "string" ? value.token : "";
  } catch { return ""; }
}
