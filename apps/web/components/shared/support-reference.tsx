"use client";

import { useState } from "react";

export function SupportReference({ referenceId }: { referenceId?: string | null }) {
  const [copied, setCopied] = useState(false);
  if (!referenceId) return null;

  async function copy() {
    try {
      await navigator.clipboard.writeText(referenceId!);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }

  return <div className="support-reference">
    <span>Reference: <code>{referenceId}</code></span>
    <button type="button" onClick={copy} aria-label={`Copy support reference ${referenceId}`}>Copy</button>
    <span className="sr-only" role="status" aria-live="polite">{copied ? "Reference copied." : ""}</span>
  </div>;
}
