"use client";

import { useState } from "react";
import { redirectForAdminResponse } from "./client-response";
import type { FieldErrors } from "./validation";
import { normalizeErrors } from "@/components/admin/form-controls";
import { validSupportReference } from "@/lib/observability/support-reference";

export function useAdminMutation<T>() {
  const [errors, setErrors] = useState<FieldErrors>({}); const [message, setMessage] = useState<string | null>(null); const [referenceId, setReferenceId] = useState<string | null>(null); const [success, setSuccess] = useState(false); const [submitting, setSubmitting] = useState(false);
  async function save(endpoint: string, method: "POST" | "PUT", request: unknown, validate: () => FieldErrors): Promise<T | null> {
    const clientErrors = validate(); setErrors(clientErrors); setMessage(null); setReferenceId(null); setSuccess(false); if (Object.keys(clientErrors).length) return null;
    setSubmitting(true); try { const response = await fetch(endpoint, { method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) }); if (redirectForAdminResponse(response)) return null; const result = await response.json() as T & { message?: string; errors?: FieldErrors; referenceId?: string }; if (!response.ok) { setErrors(normalizeErrors(result.errors ?? {})); setMessage(result.message ?? "The record could not be saved."); setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null); return null; } setSuccess(true); setMessage("Changes saved successfully."); return result; } catch { setMessage("The record could not be saved. Try again."); return null; } finally { setSubmitting(false); }
  }
  return { errors, message, referenceId, success, submitting, save };
}
