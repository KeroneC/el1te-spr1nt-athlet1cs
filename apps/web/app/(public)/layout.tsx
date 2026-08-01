import { SiteFooter } from "@/components/public/site-footer";
import { SiteHeader } from "@/components/public/site-header";
import { fallbackSettings, getSiteSettings, isStoreEnabled } from "@/lib/public/client";
import { BrowserAnalytics } from "@/components/public/browser-analytics";

export default async function PublicLayout({ children }: { children: React.ReactNode }) {
  const [settings, storeEnabled] = await Promise.all([
    getSiteSettings().catch(() => fallbackSettings),
    isStoreEnabled()
  ]);
  return <><BrowserAnalytics /><a className="skip-link" href="#main-content">Skip to main content</a><SiteHeader settings={settings} storeEnabled={storeEnabled} /><main id="main-content">{children}</main><SiteFooter settings={settings} storeEnabled={storeEnabled} /></>;
}
