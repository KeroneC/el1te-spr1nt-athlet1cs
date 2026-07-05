"use client";

import { ExternalLink, Menu, ShoppingBag, X } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { BRAND, PRIMARY_NAV_LINKS } from "@/lib/public/site";
import type { SiteSettings } from "@/lib/public/types";

export function SiteHeader({ settings }: { settings: SiteSettings }) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const logoUrl = settings.logoUrl ?? BRAND.logoMark;

  return (
    <header className="site-header">
      <div className="site-container header-inner">
        <Link className="brand" href="/" onClick={() => setOpen(false)}>
          {/* CMS logo hosts are intentionally unrestricted during this pre-upload phase. */}
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <span className="brand-mark-tilt" aria-hidden="true"><img src={logoUrl} alt="" className="brand-logo" /></span>
          <span><strong>El1te Spr1nt</strong><small>Athlet1cs</small></span>
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
            {PRIMARY_NAV_LINKS.map(({ href, label }) => (
              <li key={href}><Link className={pathname === href ? "is-active" : undefined} href={href} onClick={() => setOpen(false)}><span>{label}</span></Link></li>
            ))}
            <li><a className="nav-shop-link" href={BRAND.shopUrl} target="_blank" rel="noreferrer noopener" onClick={() => setOpen(false)}><span>Shop</span><ExternalLink size={14} aria-hidden="true" /></a></li>
          </ul>
          <Link className="button button-primary nav-cta" href="/registration" onClick={() => setOpen(false)}>
            <ShoppingBag size={16} aria-hidden="true" />Registration Info
          </Link>
        </nav>
      </div>
    </header>
  );
}
