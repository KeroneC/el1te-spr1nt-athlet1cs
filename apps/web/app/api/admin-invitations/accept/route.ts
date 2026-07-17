import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { backendFetch } from "@/lib/admin/server-api";

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
    if (error instanceof AdminApiError) return NextResponse.json({ message: error.message, errors: error.fieldErrors }, { status: error.status });
    return NextResponse.json({ message: "The invitation service is temporarily unavailable." }, { status: 503 });
  }
}
