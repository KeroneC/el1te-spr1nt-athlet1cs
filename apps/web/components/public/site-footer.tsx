import { ExternalLink, Mail, MapPin, Phone, ShoppingBag } from "lucide-react";
import Link from "next/link";
import { FacebookIcon, InstagramIcon } from "@/components/public/brand-social-icons";
import { BRAND, FOOTER_LINK_GROUPS } from "@/lib/public/site";
import type { StoreNavigationMode } from "@/lib/public/deployment";
import type { SiteSettings } from "@/lib/public/types";

export function SiteFooter({ settings, storeNavigationMode = "external" }: { settings: SiteSettings; storeNavigationMode?: StoreNavigationMode }) {
  const address = [settings.addressLine1, settings.addressLine2, [settings.city, settings.state, settings.zipCode].filter(Boolean).join(", ")].filter(Boolean);
  const email = settings.contactEmail || BRAND.contactEmail;
  const phone = settings.phoneNumber || BRAND.contactPhone;
  const socials = [
    [settings.facebookUrl ?? BRAND.facebookUrl, "Facebook", FacebookIcon],
    [settings.instagramUrl ?? BRAND.instagramUrl, "Instagram", InstagramIcon]
  ] as const;

  return (
    <footer className="site-footer">
      <div className="site-container footer-grid">
        <div className="footer-brand-panel">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={BRAND.logoWhite} alt="" className="footer-logo" width="1000" height="1000" loading="lazy" decoding="async" />
          <p className="footer-brand">{settings.clubName}</p>
          <p className="footer-slogan">{BRAND.slogan}</p>
          {storeNavigationMode === "internal"
            ? <Link className="footer-shop-link" href="/shop"><ShoppingBag size={17} aria-hidden="true" />Shop team gear</Link>
            : <a className="footer-shop-link" href={BRAND.shopUrl} target="_blank" rel="noreferrer noopener"><ShoppingBag size={17} aria-hidden="true" />Shop team gear<ExternalLink size={14} aria-hidden="true" /></a>}
        </div>
        {FOOTER_LINK_GROUPS.map((group) => (
          <div key={group.title}>
            <h2><span aria-hidden="true" />{group.title}</h2>
            <div className="footer-links">
              {group.links.map((link) => <Link key={link.href} href={link.href}>{link.label}</Link>)}
              {group.title === "Explore" && <Link href="/news">News</Link>}
            </div>
          </div>
        ))}
        <div>
          <h2><span aria-hidden="true" />Get in touch</h2>
          <div className="contact-list">
            {email && <a href={`mailto:${email}`}><Mail size={17} aria-hidden="true" />{email}</a>}
            {phone && <a href={`tel:${phone.replace(/[^\d+]/g, "")}`}><Phone size={17} aria-hidden="true" />{phone}</a>}
            {address.length > 0 && <p><MapPin size={17} aria-hidden="true" /><span>{address.map((line) => <span key={line}>{line}<br /></span>)}</span></p>}
          </div>
          <div className="social-links">
            {socials.map(([url, label, Icon]) => url && (
              <a key={label} href={url} target="_blank" rel="noreferrer noopener" aria-label={`El1te Spr1nt Athlet1cs on ${label}`}><Icon aria-hidden="true" /></a>
            ))}
          </div>
        </div>
      </div>
      <div className="site-container footer-bottom">
        <p>&copy; {new Date().getFullYear()} {settings.clubName}. A community youth track and field club.</p>
        <nav aria-label="Policies"><Link href="/privacy">Privacy</Link><Link href="/accessibility">Accessibility</Link><Link href="/terms">Terms</Link><Link href="/store-policy">Store policy</Link><Link href="/admin/login">Admin</Link></nav>
      </div>
    </footer>
  );
}
