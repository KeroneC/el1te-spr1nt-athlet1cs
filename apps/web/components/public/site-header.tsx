"use client";

import { Menu, X } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import type { SiteSettings } from "@/lib/public/types";

const links = [
  ["/about", "About"],
  ["/programs", "Programs"],
  ["/news", "News"],
  ["/events", "Events"],
  ["/gallery", "Gallery"],
  ["/coaches", "Coaches"],
  ["/sponsors", "Sponsors"],
  ["/faqs", "FAQs"],
  ["/contact", "Contact"]
] as const;

export function SiteHeader({ settings }: { settings: SiteSettings }) {
  const [open, setOpen] = useState(false);

  return (
    <header className="site-header">
      <div className="site-container header-inner">
        <Link className="brand" href="/" onClick={() => setOpen(false)}>
          {settings.logoUrl ? (
            // CMS logo hosts are intentionally unrestricted during this pre-upload phase.
            // eslint-disable-next-line @next/next/no-img-element
            <img src={settings.logoUrl} alt="" className="brand-logo" />
          ) : (
            <span className="brand-mark" aria-hidden="true">E1</span>
          )}
          <span>{settings.clubName}</span>
        </Link>
        <button
          className="icon-button mobile-menu-button"
          type="button"
          aria-label={open ? "Close navigation" : "Open navigation"}
          aria-expanded={open}
          aria-controls="primary-navigation"
          onClick={() => setOpen((value) => !value)}
        >
          {open ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}
        </button>
        <nav id="primary-navigation" className={open ? "primary-nav is-open" : "primary-nav"} aria-label="Primary navigation">
          <ul>
            {links.map(([href, label]) => (
              <li key={href}><Link href={href} onClick={() => setOpen(false)}>{label}</Link></li>
            ))}
          </ul>
          <Link className="button button-primary nav-cta" href={settings.primaryCtaUrl} onClick={() => setOpen(false)}>
            {settings.primaryCtaText}
          </Link>
        </nav>
      </div>
    </header>
  );
}
