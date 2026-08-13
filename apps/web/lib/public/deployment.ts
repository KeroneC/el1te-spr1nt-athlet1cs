import type { Metadata } from "next";

export type StoreNavigationMode = "internal" | "external";

export function robotsForEnvironment(environment: string | undefined): Metadata["robots"] {
  return environment === "demo" ? { index: false, follow: false, nocache: true } : undefined;
}

export function storeNavigationMode(value: string | undefined): StoreNavigationMode {
  return value?.trim().toLowerCase() === "internal" ? "internal" : "external";
}
