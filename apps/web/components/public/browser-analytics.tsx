"use client";

import { useEffect, useRef } from "react";
import { usePathname } from "next/navigation";
import { useReportWebVitals } from "next/web-vitals";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";
import { sanitizePublicRoute, setBrowserAnalyticsClient } from "@/lib/observability/browser-analytics";

type RuntimeConfig = { browserAnalyticsEnabled: boolean; applicationInsightsConnectionString: string; releaseSha: string };

export function BrowserAnalytics() {
  const pathname = usePathname();
  const initialPathname = useRef(pathname);
  const client = useRef<ApplicationInsights | null>(null);

  useEffect(() => {
    let active = true;
    void fetch("/api/runtime-config", { cache: "no-store" }).then(response => response.json()).then((config: RuntimeConfig) => {
      if (!active || !config.browserAnalyticsEnabled || !config.applicationInsightsConnectionString || initialPathname.current.startsWith("/admin")) return;
      const instance = new ApplicationInsights({ config: {
        connectionString: config.applicationInsightsConnectionString,
        disableCookiesUsage: true,
        isStorageUseDisabled: true,
        disableExceptionTracking: true,
        disableAjaxTracking: true,
        disableFetchTracking: true,
        disableCorrelationHeaders: true,
        enableAutoRouteTracking: false,
        autoTrackPageVisitTime: false,
        enableAjaxErrorStatusText: false,
        enableRequestHeaderTracking: false,
        enableResponseHeaderTracking: false
      }});
      instance.loadAppInsights();
      instance.addTelemetryInitializer(envelope => {
        envelope.ext = envelope.ext ?? {};
        envelope.ext.user = undefined;
        const tags = { ...(envelope.tags ?? {}), "ai.cloud.role": "web-browser" } as Record<string, string>;
        delete tags["ai.user.id"];
        delete tags["ai.user.authUserId"];
        delete tags["ai.session.id"];
        envelope.tags = tags;
        return true;
      });
      instance.context.application.ver = config.releaseSha.slice(0, 40);
      client.current = instance;
      setBrowserAnalyticsClient(instance, config.releaseSha);
      const route = sanitizePublicRoute(initialPathname.current);
      if (route) instance.trackPageView({ name: route, uri: route });
      const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined;
      if (navigation) instance.trackMetric({ name: "NavigationDuration", average: navigation.duration });
    }).catch(() => { /* Analytics must never interrupt the website. */ });
    return () => { active = false; };
  }, []); // Initialize once; route changes are recorded by the next effect.

  useEffect(() => {
    const route = sanitizePublicRoute(pathname);
    if (route && client.current) client.current.trackPageView({ name: route, uri: route });
  }, [pathname]);

  useReportWebVitals(metric => {
    if (client.current && ["CLS", "FCP", "INP", "LCP", "TTFB"].includes(metric.name))
      client.current.trackMetric({ name: `WebVital.${metric.name}`, average: metric.value });
  });

  return null;
}
