"use client";
import { useEffect } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import { supportReferenceFromDigest } from "@/lib/observability/support-reference";
import { trackSanitizedPublicError } from "@/lib/observability/browser-analytics";
export default function ErrorPage({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    const reference = supportReferenceFromDigest(error.digest);
    console.error("Public page error", error.digest);
    trackSanitizedPublicError(reference);
  }, [error]);
  return <div className="site-container route-state"><h1>Something did not load</h1><p>The page is temporarily unavailable. You can retry without losing your place.</p><SupportReference referenceId={supportReferenceFromDigest(error.digest)} /><button className="button button-primary" onClick={reset}>Try again</button></div>;
}
