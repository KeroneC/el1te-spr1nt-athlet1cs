import { SiteFooter } from "@/components/public/site-footer";
import { SiteHeader } from "@/components/public/site-header";
import { fallbackSettings, getSiteSettings } from "@/lib/public/client";
import { BrowserAnalytics } from "@/components/public/browser-analytics";
import { storeNavigationMode } from "@/lib/public/deployment";

export default async function PublicLayout({ children }: { children: React.ReactNode }) {
  const settings = await getSiteSettings().catch(() => fallbackSettings);
  const navigationMode = storeNavigationMode(process.env.STORE_NAVIGATION_MODE);
  return <><BrowserAnalytics /><a className="skip-link" href="#main-content">Skip to main content</a><SiteHeader settings={settings} storeNavigationMode={navigationMode} /><main id="main-content">{children}</main><SiteFooter settings={settings} storeNavigationMode={navigationMode} /></>;
}
