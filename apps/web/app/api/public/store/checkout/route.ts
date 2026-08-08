import { NextResponse } from "next/server";
import { forwardStoreOrderRequest, storeOrderJson } from "@/lib/store/public-order-proxy";

const returnCookie = "el1te_checkout_return";

export async function POST(request: Request) {
  let body: unknown;
  try { body = await request.json(); }
  catch { return NextResponse.json({ message: "The checkout request could not be read." }, { status: 400 }); }
  const { response, payload } = await forwardStoreOrderRequest("/api/public/store/checkout", body);
  if (!response.ok) return storeOrderJson(response, payload);
  const returnToken = typeof payload.returnToken === "string" ? payload.returnToken : null;
  if (!returnToken) return NextResponse.json({ message: "Square checkout could not be prepared." }, { status: 502 });
  const safePayload = { ...payload };
  delete safePayload.returnToken;
  const result = NextResponse.json(safePayload);
  result.cookies.set(returnCookie, returnToken, {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 60 * 60
  });
  return result;
}
