import { NextResponse } from "next/server";
import { isEnabledSetting } from "@/lib/runtime-config";

export const dynamic = "force-dynamic";

export function GET() {
  return NextResponse.json({
    browserAnalyticsEnabled: isEnabledSetting(process.env.BROWSER_ANALYTICS_ENABLED),
    applicationInsightsConnectionString: process.env.APPLICATIONINSIGHTS_CONNECTION_STRING ?? "",
    releaseSha: (process.env.RELEASE_SHA ?? "local").slice(0, 40)
  }, { headers: { "Cache-Control": "no-store" } });
}
