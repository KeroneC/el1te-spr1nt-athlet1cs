import type { Metadata } from "next";

export function robotsForEnvironment(environment: string | undefined): Metadata["robots"] {
  return environment === "demo" ? { index: false, follow: false, nocache: true } : undefined;
}
