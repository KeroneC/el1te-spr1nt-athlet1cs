"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { SupportReference } from "@/components/shared/support-reference";
import { validSupportReference } from "@/lib/observability/support-reference";

export function PasswordRecoveryForm() {
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  async function submit(event: React.FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(null); setReferenceId(null);
    try {
      const response = await fetch("/api/admin-password-reset/request", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email }) });
      const result = await response.json() as { message?: string; referenceId?: string };
      setMessage(result.message ?? "If an eligible account exists, a password reset message has been sent.");
      setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
    } catch { setMessage("Password recovery is temporarily unavailable."); }
    finally { setBusy(false); }
  }
  return <form onSubmit={submit} className="space-y-5">
    {message && <div role="status" className="border-l-4 border-sky-500 bg-sky-50 px-4 py-3 text-sm font-semibold text-sky-900">{message}<SupportReference referenceId={referenceId} /></div>}
    <div><label className="mb-2 block text-sm font-bold" htmlFor="recovery-email">Admin email</label><input className="min-h-11 w-full border border-slate-300 px-3 focus:ring-2 focus:ring-track-red" id="recovery-email" type="email" autoComplete="email" required value={email} onChange={event => setEmail(event.target.value)} /></div>
    <button className="button button-primary w-full" disabled={busy}>{busy ? "Sending…" : "Send recovery link"}</button>
    <p className="text-center text-sm"><Link className="font-bold text-track-red underline" href="/admin/login">Back to sign in</Link></p>
  </form>;
}

export function PasswordResetForm() {
  const [token, setToken] = useState("");
  const [valid, setValid] = useState<boolean | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [referenceId, setReferenceId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    const value = new URLSearchParams(window.location.hash.slice(1)).get("token") ?? "";
    history.replaceState(null, "", window.location.pathname);
    setToken(value);
    void fetch("/api/admin-password-reset/inspect", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token: value }) })
      .then(response => response.json()).then((result: { isValid?: boolean }) => setValid(result.isValid === true)).catch(() => setValid(false));
  }, []);
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); setBusy(true); setMessage(null); setReferenceId(null);
    try {
      const response = await fetch("/api/admin-password-reset/complete", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token, password: data.get("password"), confirmPassword: data.get("confirmPassword") }) });
      if (response.ok) { setValid(false); setMessage("Password updated. Existing Admin sessions were signed out. You may now sign in."); }
      else {
        const result = await response.json() as { message?: string; referenceId?: string };
        setMessage(result.message ?? "The password could not be reset.");
        setReferenceId(response.status >= 500 ? validSupportReference(result.referenceId) : null);
      }
    } catch { setMessage("Password reset is temporarily unavailable."); }
    finally { setBusy(false); }
  }
  if (valid === null) return <p role="status">Checking your secure link…</p>;
  if (!valid) return <div>{message && <div role="status" className="mb-4 border-l-4 border-emerald-500 bg-emerald-50 px-4 py-3 text-sm font-semibold">{message}<SupportReference referenceId={referenceId} /></div>}<p>This reset link is invalid, expired, or has already been used.</p><Link className="button button-primary mt-5" href="/admin/login">Return to sign in</Link></div>;
  return <form onSubmit={submit} className="space-y-5">
    {message && <div role="alert">{message}<SupportReference referenceId={referenceId} /></div>}
    <div><label className="mb-2 block text-sm font-bold" htmlFor="new-password">New password</label><input className="min-h-11 w-full border border-slate-300 px-3" id="new-password" name="password" type="password" autoComplete="new-password" minLength={12} required /></div>
    <div><label className="mb-2 block text-sm font-bold" htmlFor="confirm-password">Confirm password</label><input className="min-h-11 w-full border border-slate-300 px-3" id="confirm-password" name="confirmPassword" type="password" autoComplete="new-password" minLength={12} required /></div>
    <p className="text-sm text-slate-600">Use 12 or more characters with uppercase, lowercase, number, and symbol characters.</p>
    <button className="button button-primary w-full" disabled={busy}>{busy ? "Updating…" : "Update password"}</button>
  </form>;
}
