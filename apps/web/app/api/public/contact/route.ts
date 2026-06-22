import { NextResponse } from "next/server";
import type { ContactRequest, ValidationProblem } from "@/lib/public/types";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://localhost:5000";

export async function POST(request: Request) {
  let body: ContactRequest;
  try {
    body = (await request.json()) as ContactRequest;
  } catch {
    return NextResponse.json({ message: "The request could not be read." }, { status: 400 });
  }

  try {
    const response = await fetch(`${apiBaseUrl}/api/public/contact-submissions`, {
      method: "POST",
      cache: "no-store",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    const payload = (await response.json().catch(() => ({}))) as ValidationProblem;
    if (response.status === 400) {
      return NextResponse.json({ message: "Please correct the highlighted fields.", errors: payload.errors ?? {} }, { status: 400 });
    }
    if (!response.ok) {
      return NextResponse.json({ message: "Your message could not be sent. Please try again." }, { status: 502 });
    }
    return NextResponse.json(payload, { status: 201 });
  } catch {
    return NextResponse.json({ message: "The contact service is temporarily unavailable. Please try again." }, { status: 503 });
  }
}
