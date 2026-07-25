import { NextResponse } from "next/server";
import { backendFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";

export async function POST(request: Request) {
  let body: { token: string; password: string; confirmPassword: string };
  try {
    const value = await request.json() as Partial<typeof body>;
    body = {
      token: typeof value.token === "string" ? value.token : "",
      password: typeof value.password === "string" ? value.password : "",
      confirmPassword: typeof value.confirmPassword === "string" ? value.confirmPassword : ""
    };
  } catch { return NextResponse.json({ message: "The invitation request is invalid." }, { status: 400 }); }
  try {
    await backendFetch<null>("/api/admin-invitations/accept", { method: "POST", body: JSON.stringify(body) });
    return new NextResponse(null, { status: 204 });
  } catch (error) {
    return adminErrorResponse(error, "The invitation service is temporarily unavailable.", "invitation-accept");
  }
}
