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
  const registration = path[0] === "registration";
  const privateHeaders = registration ? { "Cache-Control": "private, no-store" } : undefined;
  if (registration && (request.headers.get("sec-fetch-site") === "cross-site" ||
      !request.headers.get("content-type")?.toLowerCase().startsWith("application/json")))
    return NextResponse.json({ message: "The request could not be completed." }, { status: 400, headers: privateHeaders });
  try {
    const body = await readAdminMutationBody(request, method);
    if (registration && (body?.length ?? 0) > 8192)
      return NextResponse.json({ message: "The request is too large." }, { status: 413, headers: privateHeaders });
    const result = await adminApiFetch<unknown>(`/api/admin/${path.map(encodeURIComponent).join("/")}`, { method, body });
    return method === "DELETE" ? new NextResponse(null, { status: 204 }) : NextResponse.json(result, { status: method === "POST" && !registration ? 201 : 200, headers: privateHeaders });
  } catch (error) {
    const response = adminErrorResponse(error, "The request could not be completed.", "admin-mutation-proxy");
    if (registration) response.headers.set("Cache-Control", "private, no-store");
    return response;
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
