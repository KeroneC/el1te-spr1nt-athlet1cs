import { ScrollText } from "lucide-react";
import { AdminUserControls, InvitationActions, InviteAdminForm } from "@/components/admin/admin-access-controls";
import { Badge, EmptyState, formatDate } from "@/components/admin/list-controls";
import { PageHeader } from "@/components/admin/page-header";
import { requireSuperAdminUser } from "@/lib/admin/auth";
import { adminApiFetch } from "@/lib/admin/server-api";
import type { AdminInvitation, AdminUser, PagedResult } from "@/lib/admin/types";

export default async function AdminUsersPage() {
  const currentUser = await requireSuperAdminUser();
  const [users, invitations] = await Promise.all([
    adminApiFetch<PagedResult<AdminUser>>("/api/admin/users?page=1&pageSize=100"),
    adminApiFetch<PagedResult<AdminInvitation>>("/api/admin/invitations?page=1&pageSize=100")
  ]);
  return <><PageHeader title="Access control" description="Invite administrators, manage active access, and protect the club's SuperAdmin coverage." action={{ href: "/admin/users/activity", label: "View activity", icon: ScrollText }} />
    <InviteAdminForm />
    <section className="mt-7" aria-labelledby="admin-users-title"><div className="mb-3"><h2 id="admin-users-title" className="text-xl font-black text-track-ink">Administrators</h2><p className="mt-1 text-sm text-slate-600">Changes apply to protected API access immediately. Administrators may need to sign in again to see a newly granted role.</p></div>
      {users.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[900px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="px-4 py-3">Administrator</th><th className="px-4 py-3">Created</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Access</th></tr></thead><tbody className="divide-y divide-slate-200">{users.items.map(user => <tr key={user.id}><td className="px-4 py-4"><p className="font-bold text-track-ink">{user.firstName} {user.lastName}</p><p className="text-sm text-slate-600">{user.email}</p></td><td className="px-4 py-4 text-sm text-slate-600">{formatDate(user.createdAtUtc, false)}</td><td className="px-4 py-4"><Badge tone={user.isActive ? "green" : "neutral"}>{user.isActive ? "Active" : "Inactive"}</Badge></td><td className="px-4 py-4"><AdminUserControls user={user} currentUserId={currentUser.id} /></td></tr>)}</tbody></table></div> : <EmptyState title="No administrators found" description="Create a protected invitation to add an administrator." />}
    </section>
    <section className="mt-7" aria-labelledby="invitations-title"><div className="mb-3"><h2 id="invitations-title" className="text-xl font-black text-track-ink">Invitation history</h2><p className="mt-1 text-sm text-slate-600">Invitation secrets are never stored. Generate a new link when a recipient needs a replacement; the previous link stops working and the 72-hour expiration resets.</p></div>
      {invitations.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[1000px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="px-4 py-3">Recipient</th><th className="px-4 py-3">Role</th><th className="px-4 py-3">Invited by</th><th className="px-4 py-3">Expires</th><th className="px-4 py-3">Status</th><th className="px-4 py-3 text-right">Actions</th></tr></thead><tbody className="divide-y divide-slate-200">{invitations.items.map(invitation => <tr key={invitation.id}><td className="px-4 py-4"><p className="font-bold text-track-ink">{invitation.firstName} {invitation.lastName}</p><p className="text-sm text-slate-600">{invitation.email}</p></td><td className="px-4 py-4 text-sm">{invitation.role}</td><td className="px-4 py-4 text-sm text-slate-600">{invitation.invitedByDisplayName}</td><td className="px-4 py-4 text-sm text-slate-600">{formatDate(invitation.expiresAtUtc)}</td><td className="px-4 py-4"><Badge tone={invitationTone(invitation.status)}>{invitation.status}</Badge></td><td className="px-4 py-4"><InvitationActions invitation={invitation} /></td></tr>)}</tbody></table></div> : <EmptyState title="No invitations yet" description="New Admin invitations will appear here." />}
    </section>
  </>;
}

function invitationTone(status: AdminInvitation["status"]): "green" | "amber" | "red" | "neutral" {
  if (status === "Accepted") return "green";
  if (status === "Pending") return "amber";
  if (status === "Revoked") return "red";
  return "neutral";
}
