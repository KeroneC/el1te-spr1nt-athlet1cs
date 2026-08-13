import type { Metadata } from "next";

export type StoreNavigationMode = "internal" | "external";

export function publicIndexingEnabled(environment: string | undefined, configured: string | undefined): boolean {
  return environment === "production" && configured?.trim().toLowerCase() === "true";
}

export function robotsForEnvironment(environment: string | undefined, configured?: string): Metadata["robots"] {
  return publicIndexingEnabled(environment, configured)
    ? undefined
    : { index: false, follow: false, nocache: true };
}

export function storeNavigationMode(value: string | undefined): StoreNavigationMode {
  return value?.trim().toLowerCase() === "internal" ? "internal" : "external";
}

export function canonicalHostRedirect(requestUrl: string, host: string | null): URL | null {
  if (host?.split(":", 1)[0].toLowerCase() !== "el1tespr1ntathlet1cs.org") return null;
  const target = new URL(requestUrl);
  target.protocol = "https:";
  target.host = "www.el1tespr1ntathlet1cs.org";
  return target;
}
