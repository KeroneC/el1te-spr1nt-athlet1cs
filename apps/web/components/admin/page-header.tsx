import Link from "next/link";
import type { LucideIcon } from "lucide-react";

export function PageHeader({ title, description, action }: { title: string; description?: string; action?: { href: string; label: string; icon: LucideIcon } }) {
  return <header className="mb-6 flex flex-col justify-between gap-4 border-b border-slate-300 pb-5 sm:flex-row sm:items-end"><div><h1 className="text-3xl font-black text-track-ink">{title}</h1>{description && <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">{description}</p>}</div>{action && <Link href={action.href} className="inline-flex min-h-11 items-center justify-center gap-2 bg-track-red px-4 text-sm font-bold text-white hover:bg-red-800 focus:outline-none focus:ring-2 focus:ring-track-red focus:ring-offset-2"><action.icon size={18} />{action.label}</Link>}</header>;
}
