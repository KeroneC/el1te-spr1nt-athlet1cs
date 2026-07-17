"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Check, Clipboard, LoaderCircle, RefreshCw, Save, Send, XCircle } from "lucide-react";
import { Field, FormNotice, SelectField, fieldError } from "./form-controls";
import { useAdminMutation } from "@/lib/admin/use-admin-mutation";
import type { AdminInvitation, AdminInvitationCreated, AdminUser } from "@/lib/admin/types";
import type { FieldErrors } from "@/lib/admin/validation";

const roles = [["Admin", "Admin"], ["SuperAdmin", "SuperAdmin"]] as const;

export function InviteAdminForm() {
  const mutation = useAdminMutation<AdminInvitationCreated>();
  const [invitationUrl, setInvitationUrl] = useState<string | null>(null);
  const router = useRouter();
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const request = {
      firstName: String(data.get("firstName") ?? "").trim(),
      lastName: String(data.get("lastName") ?? "").trim(),
      email: String(data.get("email") ?? "").trim(),
      role: String(data.get("role") ?? "Admin")
    };
    const result = await mutation.save("/api/admin/invitations", "POST", request, () => validateInvitation(request));
    if (!result) return;
    form.reset();
    setInvitationUrl(result.invitationUrl);
    router.refresh();
  }
  return <form onSubmit={submit} aria-label="Invite administrator" className="border border-slate-200 bg-white p-5 sm:p-6">
    <div><h2 className="text-lg font-black text-track-ink">Invite an administrator</h2><p className="mt-1 text-sm leading-6 text-slate-600">Create an email-bound link that expires after 72 hours. Share it only with the intended recipient.</p></div>
    <div className="mt-5 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <Field label="First name" name="firstName" required maxLength={100} error={fieldError(mutation.errors, "firstName")} />
      <Field label="Last name" name="lastName" required maxLength={100} error={fieldError(mutation.errors, "lastName")} />
      <Field label="Email" name="email" type="email" required maxLength={256} error={fieldError(mutation.errors, "email")} />
      <SelectField label="Role" name="role" options={roles} error={fieldError(mutation.errors, "role")} />
    </div>
    <div className="mt-5 flex flex-wrap items-center justify-between gap-3"><FormNotice message={mutation.message} success={mutation.success} /><button disabled={mutation.submitting} className="ml-auto inline-flex min-h-11 items-center gap-2 bg-track-red px-4 text-sm font-bold text-white disabled:opacity-60">{mutation.submitting ? <LoaderCircle size={18} className="animate-spin" /> : <Send size={18} />}{mutation.submitting ? "Creating..." : "Create invitation"}</button></div>
    {invitationUrl && <InviteLinkPanel url={invitationUrl} onClose={() => setInvitationUrl(null)} />}
  </form>;
}

export function AdminUserControls({ user, currentUserId }: { user: AdminUser; currentUserId: string }) {
  const mutation = useAdminMutation<AdminUser>();
  const router = useRouter();
  const self = user.id === currentUserId;
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const result = await mutation.save(`/api/admin/users/${user.id}`, "PUT", {
      role: String(data.get("role")), isActive: data.get("isActive") === "on"
    }, () => ({}));
    if (result) router.refresh();
  }
  return <form onSubmit={submit} className="min-w-[300px]">
    <div className="flex items-end justify-end gap-2">
      <label className="text-xs font-bold text-slate-600">Role<select name="role" defaultValue={user.role} disabled={self} className="mt-1 block min-h-9 border border-slate-300 bg-white px-2 text-sm disabled:bg-slate-100">{roles.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
      <label className="flex min-h-9 items-center gap-2 border border-slate-300 px-3 text-sm font-bold"><input type="checkbox" name="isActive" defaultChecked={user.isActive} disabled={self} className="h-4 w-4 accent-track-red" />Active</label>
      <button disabled={self || mutation.submitting} className="inline-flex h-9 w-9 items-center justify-center bg-track-ink text-white disabled:opacity-40" aria-label={`Save access for ${user.firstName} ${user.lastName}`} title="Save access">{mutation.submitting ? <LoaderCircle size={16} className="animate-spin" /> : <Save size={16} />}</button>
    </div>
    {self && <p className="mt-1 text-right text-xs text-slate-500">Your own access is protected.</p>}
    {mutation.message && <p role={mutation.success ? "status" : "alert"} className={`mt-1 text-right text-xs font-semibold ${mutation.success ? "text-emerald-700" : "text-red-700"}`}>{mutation.message}</p>}
  </form>;
}

export function InvitationActions({ invitation }: { invitation: AdminInvitation }) {
  const [busy, setBusy] = useState<"reissue" | "revoke" | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [invitationUrl, setInvitationUrl] = useState<string | null>(null);
  const router = useRouter();
  if (invitation.status === "Accepted" || invitation.status === "Revoked") return null;
  async function act(action: "reissue" | "revoke") {
    setBusy(action); setMessage(null);
    try {
      const response = await fetch(`/api/admin/invitations/${invitation.id}${action === "reissue" ? "/reissue" : ""}`, { method: action === "reissue" ? "POST" : "DELETE" });
      if (response.status === 401) { window.location.assign("/api/admin-session/logout?reason=expired"); return; }
      if (response.status === 403) { window.location.assign("/admin/access-denied"); return; }
      const body = response.status === 204 ? null : await response.json() as AdminInvitationCreated & { message?: string };
      if (!response.ok) { setMessage(body?.message ?? "The invitation could not be updated."); return; }
      if (body?.invitationUrl) setInvitationUrl(body.invitationUrl);
      setMessage(action === "reissue" ? "A new invitation link is ready." : "Invitation revoked.");
      router.refresh();
    } catch { setMessage("The invitation could not be updated."); }
    finally { setBusy(null); }
  }
  return <div className="min-w-[280px]">
    <div className="flex justify-end gap-2"><button type="button" disabled={Boolean(busy)} onClick={() => act("reissue")} className="inline-flex min-h-9 items-center gap-2 border border-slate-300 px-3 text-xs font-bold"><RefreshCw size={15} className={busy === "reissue" ? "animate-spin" : ""} />Reissue</button><button type="button" disabled={Boolean(busy)} onClick={() => act("revoke")} className="inline-flex min-h-9 items-center gap-2 border border-red-300 px-3 text-xs font-bold text-red-700"><XCircle size={15} />Revoke</button></div>
    {message && <p role="status" className="mt-1 text-right text-xs text-slate-600">{message}</p>}
    {invitationUrl && <InviteLinkPanel url={invitationUrl} onClose={() => setInvitationUrl(null)} compact />}
  </div>;
}

function InviteLinkPanel({ url, onClose, compact = false }: { url: string; onClose: () => void; compact?: boolean }) {
  const [copied, setCopied] = useState(false);
  async function copy() { await navigator.clipboard.writeText(url); setCopied(true); }
  return <div className={`${compact ? "mt-2" : "mt-5"} border-l-4 border-track-red bg-slate-100 p-3`}><div className="flex items-center justify-between gap-3"><p className="text-xs font-bold text-track-ink">Share this link once with the intended recipient.</p><button type="button" onClick={onClose} className="text-xs font-bold text-slate-500">Close</button></div><div className="mt-2 flex gap-2"><input readOnly value={url} aria-label="Invitation link" className="min-w-0 flex-1 border border-slate-300 bg-white px-2 text-xs" /><button type="button" onClick={copy} className="inline-flex h-10 items-center gap-2 bg-track-ink px-3 text-xs font-bold text-white">{copied ? <Check size={15} /> : <Clipboard size={15} />}{copied ? "Copied" : "Copy"}</button></div></div>;
}

function validateInvitation(request: { firstName: string; lastName: string; email: string; role: string }): FieldErrors {
  const errors: FieldErrors = {};
  if (!request.firstName) errors.firstName = ["First name is required."];
  if (!request.lastName) errors.lastName = ["Last name is required."];
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(request.email)) errors.email = ["Enter a valid email address."];
  if (request.role !== "Admin" && request.role !== "SuperAdmin") errors.role = ["Choose a valid role."];
  return errors;
}
