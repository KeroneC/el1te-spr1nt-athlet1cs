"use client";

import type { ApplicationInsights } from "@microsoft/applicationinsights-web";

let client: ApplicationInsights | null = null;
let activeReleaseSha = "unknown";

export function setBrowserAnalyticsClient(value: ApplicationInsights, releaseSha: string) {
  client = value;
  activeReleaseSha = releaseSha.slice(0, 40);
}

export function sanitizePublicRoute(pathname: string): string | null {
  const path = pathname.split(/[?#]/, 1)[0] || "/";
  if (path.startsWith("/admin") || path.startsWith("/api")) return null;
  if (/^\/news\/[^/]+$/.test(path)) return "/news/[slug]";
  if (/^\/events\/[^/]+$/.test(path)) return "/events/[slug]";
  if (/^\/gallery\/[^/]+$/.test(path)) return "/gallery/[slug]";
  if (/^\/shop\/[^/]+$/.test(path) && path !== "/shop/cart" && path !== "/shop/order-confirmation" && path !== "/shop/order-status") return "/shop/[slug]";
  return publicRoutes.has(path) ? path : null;
}

const publicRoutes = new Set([
  "/", "/about", "/accessibility", "/coaches", "/contact", "/events", "/faqs",
  "/forms", "/gallery", "/hall-of-fame", "/news", "/privacy", "/programs",
  "/registration", "/rgnhof", "/scholarship", "/shop", "/shop/cart",
  "/shop/order-confirmation", "/shop/order-status", "/sponsors", "/store-policy",
  "/team", "/terms"
]);

export function trackSanitizedPublicError(reference: string | null) {
  if (!client) return;
  client.trackEvent({ name: "PublicErrorBoundary" }, {
    reference: safeReference(reference), releaseSha: activeReleaseSha
  });
}

function safeReference(value: string | null): string {
  return value && /^(ESA-[A-F0-9]{16}|NEXT-[A-Za-z0-9_-]{6,64})$/.test(value) ? value : "not-available";
}
