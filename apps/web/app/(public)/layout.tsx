import { SiteFooter } from "@/components/public/site-footer";
import { SiteHeader } from "@/components/public/site-header";
import { fallbackSettings, getSiteSettings } from "@/lib/public/client";
import { BrowserAnalytics } from "@/components/public/browser-analytics";
import { storeNavigationMode } from "@/lib/public/deployment";
import { isEnabledSetting } from "@/lib/runtime-config";

export const dynamic = "force-dynamic";

export default async function PublicLayout({ children }: { children: React.ReactNode }) {
  const settings = await getSiteSettings().catch(() => fallbackSettings);
  const navigationMode = storeNavigationMode(process.env.STORE_NAVIGATION_MODE);
  const allAmericansEnabled = isEnabledSetting(process.env.ALL_AMERICANS_ARCHIVE_ENABLED);
  return <><BrowserAnalytics /><a className="skip-link" href="#main-content">Skip to main content</a><SiteHeader settings={settings} storeNavigationMode={navigationMode} allAmericansEnabled={allAmericansEnabled} /><main id="main-content">{children}</main><SiteFooter settings={settings} storeNavigationMode={navigationMode} allAmericansEnabled={allAmericansEnabled} /></>;
}
