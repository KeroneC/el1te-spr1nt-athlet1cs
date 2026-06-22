import { SiteFooter } from "@/components/public/site-footer";
import { SiteHeader } from "@/components/public/site-header";
import { fallbackSettings, getSiteSettings } from "@/lib/public/client";

export default async function PublicLayout({ children }: { children: React.ReactNode }) {
  const settings = await getSiteSettings().catch(() => fallbackSettings);
  return <><a className="skip-link" href="#main-content">Skip to main content</a><SiteHeader settings={settings} /><main id="main-content">{children}</main><SiteFooter settings={settings} /></>;
}
