"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Award, Bell, CalendarDays, ChevronRight, CircleHelp, FileImage, FileText, GalleryHorizontal, Handshake, Inbox, LayoutDashboard, LogOut, Menu, Settings, ShieldCheck, ShoppingBag, Trophy, Users, X } from "lucide-react";
import type { CurrentUser } from "@/lib/admin/types";

const active = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard, exact: true },
  { href: "/admin/announcements", label: "Announcements", icon: Bell, exact: false },
  { href: "/admin/events", label: "Events", icon: CalendarDays, exact: false },
  { href: "/admin/registration", label: "Registration", icon: FileText, exact: false },
  { href: "/admin/media", label: "Media", icon: FileImage, exact: false },
  { href: "/admin/gallery", label: "Gallery", icon: GalleryHorizontal, exact: false },
  { href: "/admin/coaches", label: "Coaches", icon: Users, exact: false },
  { href: "/admin/hall-of-fame", label: "Hall of Fame", icon: Trophy, exact: false },
  { href: "/admin/all-americans", label: "All-Americans", icon: Award, exact: false },
  { href: "/admin/sponsors", label: "Sponsors", icon: Handshake, exact: false },
  { href: "/admin/store", label: "Merchandise", icon: ShoppingBag, exact: false },
  { href: "/admin/faqs", label: "FAQs", icon: CircleHelp, exact: false },
  { href: "/admin/content", label: "Content", icon: FileText, exact: false },
  { href: "/admin/site-settings", label: "Site Settings", icon: Settings, exact: false },
  { href: "/admin/contact-submissions", label: "Contact Submissions", icon: Inbox, exact: false }
];

export function AdminShell({ user, children }: { user: CurrentUser; children: React.ReactNode }) {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const openButton = useRef<HTMLButtonElement>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    if (open) closeButton.current?.focus();
  }, [open]);
  useEffect(() => {
    const desktop = window.matchMedia("(min-width: 1024px)");
    const handleBreakpoint = () => { if (desktop.matches) setOpen(false); };
    desktop.addEventListener("change", handleBreakpoint);
    return () => desktop.removeEventListener("change", handleBreakpoint);
  }, []);
  function closeNavigation() {
    setOpen(false);
    window.requestAnimationFrame(() => openButton.current?.focus());
  }
  const navigation = user.role === "SuperAdmin" ? [...active, { href: "/admin/users", label: "Access control", icon: ShieldCheck, exact: false }] : active;
  const section = pathname.split("/").filter(Boolean).at(-1)?.replaceAll("-", " ") ?? "Dashboard";
  return <div className="min-h-screen bg-slate-100 lg:grid lg:grid-cols-[248px_1fr]" onKeyDown={event => {
    if (open && event.key === "Escape") { event.preventDefault(); closeNavigation(); }
  }}>
    {open && <div aria-hidden="true" className="fixed inset-0 z-30 bg-black/50 lg:hidden" onClick={closeNavigation} />}
    <aside id="admin-navigation-panel" className={`fixed inset-y-0 left-0 z-40 flex w-[min(84vw,280px)] flex-col bg-track-ink text-white transition-transform motion-reduce:transition-none lg:sticky lg:top-0 lg:h-screen lg:w-auto ${open ? "visible translate-x-0" : "invisible -translate-x-full lg:visible lg:translate-x-0"}`}>
      <div className="flex h-16 items-center justify-between border-b border-white/10 px-5"><Link href="/admin" className="font-black" onClick={() => { if (open) closeNavigation(); }}>EL1TE <span className="text-track-red">ADMIN</span></Link><button ref={closeButton} type="button" onClick={closeNavigation} className="flex min-h-11 min-w-11 items-center justify-center lg:hidden" aria-label="Close navigation"><X size={20} aria-hidden="true" /></button></div>
      <nav className="flex-1 overflow-y-auto px-3 py-5" aria-label="Admin navigation">
        <p className="px-3 text-xs font-bold uppercase text-slate-400">Workspace</p>
        <ul className="mt-2 space-y-1">{navigation.map((item) => { const selected = item.exact ? pathname === item.href : pathname.startsWith(item.href); const Icon = item.icon; return <li key={item.href}><Link href={item.href} onClick={() => { if (open) closeNavigation(); }} aria-current={selected ? "page" : undefined} className={`flex min-h-11 items-center gap-3 border-l-4 px-3 text-sm font-bold ${selected ? "border-track-red bg-white/10 text-white" : "border-transparent text-slate-300 hover:bg-white/5 hover:text-white"}`}><Icon size={19} />{item.label}{selected && <ChevronRight size={16} className="ml-auto" />}</Link></li>; })}</ul>
      </nav>
    </aside>
    <div className="min-w-0" inert={open}>
      <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-slate-200 bg-white px-4 sm:px-6"><button ref={openButton} type="button" onClick={() => setOpen(true)} className="flex min-h-11 min-w-11 items-center justify-center text-track-ink lg:hidden" aria-label="Open navigation" aria-expanded={open} aria-controls="admin-navigation-panel"><Menu size={22} aria-hidden="true" /></button><div className="hidden text-sm font-semibold capitalize text-slate-500 sm:block">Admin / {section}</div><div className="ml-auto flex items-center gap-4"><div className="text-right"><p className="text-sm font-bold text-track-ink">{user.displayName}</p><p className="text-xs text-slate-500">{user.role}</p></div><form action="/api/admin-session/logout" method="post"><button type="submit" className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 text-slate-600 hover:border-track-red hover:text-track-red" aria-label="Log out" title="Log out"><LogOut size={18} /></button></form></div></header>
      <main className="mx-auto w-full max-w-[1440px] p-4 sm:p-6 lg:p-8">{children}</main>
    </div>
  </div>;
}
