import { NextResponse } from "next/server";

export const dynamic = "force-dynamic";

export function GET() {
  return NextResponse.json({
    browserAnalyticsEnabled: process.env.BROWSER_ANALYTICS_ENABLED === "true",
    applicationInsightsConnectionString: process.env.APPLICATIONINSIGHTS_CONNECTION_STRING ?? "",
    releaseSha: (process.env.RELEASE_SHA ?? "local").slice(0, 40)
  }, { headers: { "Cache-Control": "no-store" } });
}
