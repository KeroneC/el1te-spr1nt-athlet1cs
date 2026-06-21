import { getAnnouncementState } from "@/lib/admin/validation";
import type { AdminAnnouncement } from "@/lib/admin/types";

const styles = { Draft: "bg-slate-200 text-slate-800", Scheduled: "bg-blue-100 text-blue-800", Published: "bg-emerald-100 text-emerald-800", Expired: "bg-amber-100 text-amber-900" };

export function StatusBadge({ announcement }: { announcement: AdminAnnouncement }) {
  const state = getAnnouncementState(announcement);
  return <span className={`inline-flex min-h-7 items-center px-2 text-xs font-bold ${styles[state]}`}>{state}</span>;
}
