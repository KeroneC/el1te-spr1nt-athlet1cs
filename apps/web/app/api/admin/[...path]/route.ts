import { NextResponse } from "next/server";
import { AdminApiError } from "@/lib/admin/api-error";
import { adminApiFetch } from "@/lib/admin/server-api";
import { isAllowedAdminMutation } from "@/lib/admin/mutation-policy";
import { readAdminMutationBody } from "@/lib/admin/mutation-request";

type Context = { params: Promise<{ path: string[] }> };

export function POST(request: Request, context: Context) { return proxy(request, context, "POST"); }
export function PUT(request: Request, context: Context) { return proxy(request, context, "PUT"); }
export function DELETE(request: Request, context: Context) { return proxy(request, context, "DELETE"); }

async function proxy(request: Request, context: Context, method: "POST" | "PUT" | "DELETE") {
  const { path } = await context.params;
  if (!isAllowedAdminMutation(path, method)) return NextResponse.json({ message: "The requested admin operation is not available." }, { status: 404 });
  try {
    const body = await readAdminMutationBody(request, method);
    const result = await adminApiFetch<unknown>(`/api/admin/${path.map(encodeURIComponent).join("/")}`, { method, body });
    return method === "DELETE" ? new NextResponse(null, { status: 204 }) : NextResponse.json(result, { status: method === "POST" ? 201 : 200 });
  } catch (error) {
    if (error instanceof AdminApiError) return NextResponse.json({ message: error.message, errors: error.fieldErrors }, { status: error.status });
    return NextResponse.json({ message: "The request could not be completed." }, { status: 500 });
  }
}
