import { Mail, MapPin, Phone } from "lucide-react";
import Link from "next/link";
import type { SiteSettings } from "@/lib/public/types";

export function SiteFooter({ settings }: { settings: SiteSettings }) {
  const address = [settings.addressLine1, settings.addressLine2, [settings.city, settings.state, settings.zipCode].filter(Boolean).join(", ")].filter(Boolean);
  const socials = [
    [settings.facebookUrl, "Facebook", "Fb"],
    [settings.instagramUrl, "Instagram", "Ig"],
    [settings.youTubeUrl, "YouTube", "Yt"]
  ] as const;

  return (
    <footer className="site-footer">
      <div className="site-container footer-grid">
        <div>
          <p className="footer-brand">{settings.clubName}</p>
          <p className="footer-slogan">{settings.slogan}</p>
        </div>
        <div>
          <h2>Explore</h2>
          <div className="footer-links">
            <Link href="/programs">Programs</Link><Link href="/events">Schedule</Link>
            <Link href="/news">News</Link><Link href="/registration">Registration</Link>
          </div>
        </div>
        <div>
          <h2>Get in touch</h2>
          <div className="contact-list">
            {settings.contactEmail && <a href={`mailto:${settings.contactEmail}`}><Mail size={17} aria-hidden="true" />{settings.contactEmail}</a>}
            {settings.phoneNumber && <a href={`tel:${settings.phoneNumber}`}><Phone size={17} aria-hidden="true" />{settings.phoneNumber}</a>}
            {address.length > 0 && <p><MapPin size={17} aria-hidden="true" /><span>{address.map((line) => <span key={line}>{line}<br /></span>)}</span></p>}
          </div>
          <div className="social-links">
            {socials.map(([url, label, abbreviation]) => url && (
              <a key={label} href={url} target="_blank" rel="noreferrer noopener" aria-label={label}><span aria-hidden="true">{abbreviation}</span></a>
            ))}
          </div>
        </div>
      </div>
      <div className="site-container footer-bottom">
        <p>&copy; {new Date().getFullYear()} {settings.clubName}. All rights reserved.</p>
        <Link href="/admin/login">Admin</Link>
      </div>
    </footer>
  );
}
