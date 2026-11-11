import { cookies } from "next/headers";
import { historicalPhotosEnabled } from "@/lib/admin/historical-photos";
import { ADMIN_SESSION_COOKIE } from "@/lib/admin/server-api";

type Context = { params: Promise<{ id: string; action: string }> };
const guid = /^[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i;
const headers = { "Cache-Control": "private, no-store", "X-Content-Type-Options": "nosniff" };
const empty = (status: number) => new Response(null, { status, headers });

export function GET(request: Request, context: Context) {
  return forward(request, context, "GET");
}

export function PUT(request: Request, context: Context) {
  return forward(request, context, "PUT");
}

async function forward(request: Request, { params }: Context, method: "GET" | "PUT") {
  const { id, action } = await params;
  if (!historicalPhotosEnabled() || !guid.test(id) ||
      (method === "GET" ? action !== "preview" && action !== "download" : action !== "review")) return empty(404);
  const token = (await cookies()).get(ADMIN_SESSION_COOKIE)?.value;
  if (!token) return empty(401);
  let body: string | undefined;
  if (method === "PUT") {
    if (!request.headers.get("content-type")?.toLowerCase().startsWith("application/json") ||
        request.headers.get("sec-fetch-site") === "cross-site") return empty(400);
    const reader = request.body?.getReader();
    if (!reader) return empty(400);
    const decoder = new TextDecoder();
    body = "";
    let length = 0;
    try {
      for (;;) {
        const { value, done } = await reader.read();
        if (done) break;
        length += value.byteLength;
        if (length > 1024) { await reader.cancel(); return empty(413); }
        body += decoder.decode(value, { stream: true });
      }
      body += decoder.decode();
    } catch { return empty(400); }
  }

  const api = (process.env.API_BASE_URL ?? "https://localhost:7171").replace(/\/$/, "");
  try {
    const upstream = await fetch(`${api}/api/admin/historical-photos/${id}/${action}`, {
      method, body, cache: "no-store", redirect: "error", signal: request.signal,
      headers: { Authorization: `Bearer ${token}`, ...(body === undefined ? {} : { "Content-Type": "application/json" }) }
    });
    if (!upstream.ok) {
      await upstream.body?.cancel();
      return empty(upstream.status >= 500 ? 502 : upstream.status);
    }
    if (method === "PUT") return empty(204);
    const type = action === "preview" ? "image/webp" : "image/jpeg";
    if (!upstream.body || upstream.headers.get("content-type") !== type) {
      await upstream.body?.cancel();
      return empty(502);
    }
    return new Response(upstream.body, {
      headers: {
        ...headers, "Content-Type": type,
        ...(action === "download" ? { "Content-Disposition": `attachment; filename="historical-photo-${id}.jpg"` } : {})
      }
    });
  } catch { return empty(502); }
}
