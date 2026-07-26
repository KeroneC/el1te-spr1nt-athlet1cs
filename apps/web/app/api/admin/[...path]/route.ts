import { NextResponse } from "next/server";
import { adminApiFetch } from "@/lib/admin/server-api";
import { adminErrorResponse } from "@/lib/admin/error-response";
import { isAllowedAdminMutation, isAllowedAdminRead } from "@/lib/admin/mutation-policy";
import { readAdminMutationBody } from "@/lib/admin/mutation-request";

type Context = { params: Promise<{ path: string[] }> };

export function GET(request: Request, context: Context) { return proxyRead(request, context); }
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
    return adminErrorResponse(error, "The request could not be completed.", "admin-mutation-proxy");
  }
}

async function proxyRead(request: Request, context: Context) {
  const { path } = await context.params;
  if (!isAllowedAdminRead(path)) return NextResponse.json({ message: "The requested admin operation is not available." }, { status: 404 });
  try {
    const search = new URL(request.url).search;
    const result = await adminApiFetch<unknown>(`/api/admin/${path.map(encodeURIComponent).join("/")}${search}`);
    return NextResponse.json(result);
  } catch (error) {
    return adminErrorResponse(error, "The request could not be completed.", "admin-read-proxy");
  }
}
