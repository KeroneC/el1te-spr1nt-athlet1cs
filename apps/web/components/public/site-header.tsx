"use client";

import { ChevronDown, ClipboardCheck, ExternalLink, Menu, X } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { BRAND, HEADER_NAV_ITEMS } from "@/lib/public/site";
import type { SiteSettings } from "@/lib/public/types";

export function SiteHeader({ settings }: { settings: SiteSettings }) {
  const [open, setOpen] = useState(false);
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const pathname = usePathname();
  const clubName = settings.clubName || BRAND.name;
  const headerRef = useRef<HTMLElement>(null);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const groupButtonRefs = useRef<Record<string, HTMLButtonElement | null>>({});

  const isCurrentRoute = (href: string) => pathname === href || pathname.startsWith(`${href}/`);
  const closeNavigation = () => {
    setOpen(false);
    setOpenGroup(null);
  };

  useEffect(() => {
    closeNavigation();
  }, [pathname]);

  useEffect(() => {
    if (!open && !openGroup) return;

    const handlePointerDown = (event: PointerEvent) => {
      if (headerRef.current && !headerRef.current.contains(event.target as Node)) closeNavigation();
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      event.preventDefault();
      if (openGroup) {
        const group = openGroup;
        setOpenGroup(null);
        groupButtonRefs.current[group]?.focus();
      } else if (open) {
        setOpen(false);
        menuButtonRef.current?.focus();
      }
    };

    document.addEventListener("pointerdown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, openGroup]);

  return (
    <header className="site-header" ref={headerRef}>
      <div className="site-container header-inner">
        <Link className="brand" href="/" aria-label={`${clubName} home`} onClick={() => setOpen(false)}>
          <span className="brand-wordmark" aria-hidden="true">
            <span>El<span className="brand-one">1</span>te</span>
            <span>Spr<span className="brand-one">1</span>nt</span>
            <span>Athlet<span className="brand-one">1</span>cs</span>
          </span>
        </Link>
        <button ref={menuButtonRef}
          className="icon-button mobile-menu-button"
          type="button"
          aria-label={open ? "Close navigation" : "Open navigation"}
          aria-expanded={open}
          aria-controls="primary-navigation"
          onClick={() => {
            setOpen((value) => !value);
            setOpenGroup(null);
          }}
        >
          {open ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}
        </button>
        <nav id="primary-navigation" className={open ? "primary-nav is-open" : "primary-nav"} aria-label="Primary navigation">
          <ul className="primary-nav-list">
            {HEADER_NAV_ITEMS.map((item) => "links" in item ? (
              <li className={openGroup === item.label ? "nav-group is-open" : "nav-group"} key={item.label}>
                <button
                  ref={(element) => { groupButtonRefs.current[item.label] = element; }}
                  type="button"
                  className={item.links.some((link) => isCurrentRoute(link.href)) ? "nav-group-trigger is-active" : "nav-group-trigger"}
                  aria-expanded={openGroup === item.label}
                  aria-controls={`nav-group-${item.label.toLowerCase()}`}
                  onClick={() => setOpenGroup((value) => value === item.label ? null : item.label)}
                >
                  <span>{item.label}</span>
                  <span className="nav-chevron"><ChevronDown size={14} aria-hidden="true" /></span>
                </button>
                <ul className="nav-submenu" id={`nav-group-${item.label.toLowerCase()}`}>
                  {item.links.map(({ href, label }) => (
                    <li key={href}><Link className={isCurrentRoute(href) ? "is-active" : undefined} href={href} onClick={closeNavigation}>{label}</Link></li>
                  ))}
                </ul>
              </li>
            ) : (
              <li key={item.href}><Link className={isCurrentRoute(item.href) ? "is-active" : undefined} href={item.href} onClick={closeNavigation}><span>{item.label}</span></Link></li>
            ))}
            <li><a className="nav-shop-link" href={BRAND.shopUrl} target="_blank" rel="noreferrer noopener" onClick={() => setOpen(false)}><span>Shop</span><ExternalLink size={14} aria-hidden="true" /></a></li>
          </ul>
          <Link className="button button-primary nav-cta" href="/registration" onClick={closeNavigation}>
            <ClipboardCheck size={16} aria-hidden="true" />Registration
          </Link>
        </nav>
      </div>
    </header>
  );
}
