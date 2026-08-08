import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { forwardStoreOrderRequest, storeOrderJson } from "@/lib/store/public-order-proxy";

type Context = { params: Promise<{ action: string }> };
const allowed = new Set(["status", "cancel"]);

export async function POST(request: Request, context: Context) {
  const { action } = await context.params;
  if (action === "return-status") {
    const token = (await cookies()).get("el1te_checkout_return")?.value;
    if (!token) return NextResponse.json({ message: "This checkout confirmation has expired." }, { status: 404 });
    const { response, payload } = await forwardStoreOrderRequest("/api/public/store/orders/return-status", { token });
    return storeOrderJson(response, payload);
  }
  if (!allowed.has(action)) return NextResponse.json({ message: "Not found." }, { status: 404 });
  let body: unknown;
  try { body = await request.json(); }
  catch { return NextResponse.json({ message: "The order request could not be read." }, { status: 400 }); }
  const { response, payload } = await forwardStoreOrderRequest(`/api/public/store/orders/${action}`, body);
  return storeOrderJson(response, payload);
}
