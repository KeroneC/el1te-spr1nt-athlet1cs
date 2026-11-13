import { NextResponse } from "next/server";
import { registrationChannel } from "@/lib/registration";
import { SUPPORT_REFERENCE_HEADER, validSupportReference } from "@/lib/observability/support-reference";

const headers = { "Cache-Control": "no-store" };
const problems = new Set(["form", "guardianEmail", "guardianName", "guardianPhone",
  "athleteFirstName", "ageGroup", "availability", "experience", "priorClub", "clubName", "affiliations", "lastMeetMonth"]);

export async function POST(request: Request, { params }: { params: Promise<{ action: string }> }) {
  const { action } = await params;
  if (action !== "applications" && action !== "visit") return new Response(null, { status: 404, headers });
  const answer = (status: number, data: object) => NextResponse.json(data, { status, headers });
  if (!request.headers.get("content-type")?.toLowerCase().startsWith("application/json") ||
      request.headers.get("sec-fetch-site") === "cross-site")
    return answer(400, { message: "This form could not be submitted." });
  const reader = request.body?.getReader();
  if (!reader) return answer(400, { message: "This form could not be read." });
  let body: Record<string, unknown>;
  try {
    const decoder = new TextDecoder();
    let data = "", size = 0;
    for (;;) {
      const chunk = await reader.read();
      if (chunk.done) break;
      size += chunk.value.byteLength;
      if (size > (action === "visit" ? 512 : 4096)) {
        await reader.cancel();
        return answer(413, { message: "This form is too large." });
      }
      data += decoder.decode(chunk.value, { stream: true });
    }
    body = JSON.parse(data + decoder.decode());
    if (!body || typeof body !== "object" || Array.isArray(body)) throw new Error();
  } catch { return answer(400, { message: "This form could not be read." }); }

  try {
    const upstream = await fetch(`${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/public/registration/${action}`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ...body, channel: registrationChannel(body.channel) }),
      cache: "no-store", redirect: "error", signal: AbortSignal.timeout(action === "visit" ? 3000 : 12000)
    });
    if (action === "visit") return new Response(null, { status: 204, headers });
    const data = await upstream.json().catch(() => null);
    if (upstream.ok && /^EL-[A-F0-9]{16}$/.test(data?.reference))
      return answer(upstream.status, { reference: data.reference });
    if (upstream.status === 400) {
      const errors = Object.fromEntries(Object.entries(data?.errors ?? {}).filter(([name, value]) =>
        problems.has(name) && Array.isArray(value) && typeof value[0] === "string"));
      return answer(400, { message: "Check your answers and try again.", errors });
    }
    if (upstream.status === 409)
      return answer(409, { message: data?.title === "Applications are not open."
        ? "Applications are not open. Check the Registration page for updates."
        : "This retry did not match the original submission. If you already applied, contact the club before submitting again." });
    if (upstream.status === 429) return answer(429, { message: "Too many submissions. Try again later or contact the club." });
    const referenceId = validSupportReference(upstream.headers.get(SUPPORT_REFERENCE_HEADER)) ?? validSupportReference(data?.referenceId);
    return answer(503, { message: "We couldn’t confirm your submission. Retry this same form or contact the club.", referenceId });
  } catch {
    return answer(503, { message: "We couldn’t confirm your submission. Retry this same form or contact the club." });
  }
}
