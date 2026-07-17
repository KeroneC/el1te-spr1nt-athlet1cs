"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { CheckCircle2, LoaderCircle } from "lucide-react";
import type { AdminInvitationDetails } from "@/lib/admin/types";
import type { FieldErrors } from "@/lib/admin/validation";

export function InvitationAcceptance() {
  const [token, setToken] = useState("");
  const [details, setDetails] = useState<AdminInvitationDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [complete, setComplete] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});

  useEffect(() => {
    const params = new URLSearchParams(window.location.hash.slice(1));
    const invitationToken = params.get("token") ?? "";
    setToken(invitationToken);
    if (!invitationToken) { setMessage("This invitation link is incomplete."); setLoading(false); return; }
    void inspect(invitationToken);
  }, []);

  async function inspect(invitationToken: string) {
    try {
      const response = await fetch("/api/admin-invitations/inspect", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token: invitationToken }) });
      const result = await response.json() as AdminInvitationDetails & { message?: string };
      if (!response.ok) { setMessage(result.message ?? "This invitation is no longer available."); return; }
      setDetails(result);
    } catch { setMessage("The invitation service is temporarily unavailable."); }
    finally { setLoading(false); }
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const password = String(data.get("password") ?? "");
    const confirmPassword = String(data.get("confirmPassword") ?? "");
    const clientErrors = validatePassword(password, confirmPassword);
    setErrors(clientErrors); setMessage(null);
    if (Object.keys(clientErrors).length) return;
    setSubmitting(true);
    try {
      const response = await fetch("/api/admin-invitations/accept", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token, password, confirmPassword }) });
      if (!response.ok) {
        const result = await response.json() as { message?: string; errors?: FieldErrors };
        setMessage(result.message ?? "The invitation could not be accepted."); setErrors(normalize(result.errors ?? {})); return;
      }
      window.history.replaceState(null, "", window.location.pathname);
      setComplete(true);
    } catch { setMessage("The invitation service is temporarily unavailable."); }
    finally { setSubmitting(false); }
  }

  if (loading) return <div className="flex min-h-40 items-center justify-center" role="status"><LoaderCircle className="animate-spin text-track-red" /><span className="sr-only">Checking invitation</span></div>;
  if (complete) return <div className="py-4 text-center"><CheckCircle2 size={42} className="mx-auto text-emerald-600" /><h2 className="mt-4 text-xl font-black text-track-ink">Your Admin account is ready</h2><p className="mt-2 text-sm leading-6 text-slate-600">Sign in using the email address and password you just confirmed.</p><Link href="/admin/login" className="mt-6 inline-flex min-h-11 items-center bg-track-red px-5 text-sm font-bold text-white">Continue to sign in</Link></div>;
  if (!details) return <div><p role="alert" className="border-l-4 border-track-red bg-red-50 px-4 py-3 text-sm font-semibold text-red-900">{message}</p><Link href="/contact" className="mt-5 inline-flex text-sm font-bold text-track-red">Contact the club</Link></div>;

  return <form onSubmit={submit} noValidate className="space-y-5">
    <div className="border-l-4 border-track-red bg-slate-100 px-4 py-3"><p className="text-sm font-bold text-track-ink">{details.firstName} {details.lastName}</p><p className="text-sm text-slate-600">{details.email} · {details.role}</p><p className="mt-1 text-xs text-slate-500">Expires {new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(details.expiresAtUtc))}</p></div>
    {message && <p role="alert" className="border-l-4 border-track-red bg-red-50 px-4 py-3 text-sm font-semibold text-red-900">{message}</p>}
    <PasswordField name="password" label="Create password" error={errors.password?.[0]} autoComplete="new-password" />
    <PasswordField name="confirmPassword" label="Confirm password" error={errors.confirmPassword?.[0]} autoComplete="new-password" />
    <p className="text-xs leading-5 text-slate-500">Use at least 12 characters with uppercase, lowercase, number, and symbol characters.</p>
    <button disabled={submitting} className="inline-flex min-h-11 w-full items-center justify-center gap-2 bg-track-red px-5 text-sm font-bold text-white disabled:opacity-60">{submitting && <LoaderCircle size={18} className="animate-spin" />}{submitting ? "Creating account..." : "Accept invitation"}</button>
  </form>;
}

function PasswordField({ name, label, error, autoComplete }: { name: string; label: string; error?: string; autoComplete: string }) {
  const errorId = `${name}-error`;
  return <div><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><input id={name} name={name} type="password" minLength={12} maxLength={128} autoComplete={autoComplete} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} className="min-h-11 w-full border border-slate-300 px-3 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{error && <p id={errorId} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>;
}

function validatePassword(password: string, confirmation: string): FieldErrors {
  const errors: FieldErrors = {};
  if (password.length < 12 || password.length > 128 || !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/\d/.test(password) || !/[^A-Za-z0-9]/.test(password)) errors.password = ["Use 12 to 128 characters with uppercase, lowercase, number, and symbol characters."];
  if (password !== confirmation) errors.confirmPassword = ["Password and confirmation password do not match."];
  return errors;
}

function normalize(errors: FieldErrors): FieldErrors { return Object.fromEntries(Object.entries(errors).map(([key, value]) => [key[0].toLowerCase() + key.slice(1), value])); }
