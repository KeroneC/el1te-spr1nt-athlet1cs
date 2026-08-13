import type { MetadataRoute } from "next";
import { publicIndexingEnabled } from "@/lib/public/deployment";

export const dynamic = "force-dynamic";

export default function robots(): MetadataRoute.Robots {
  const indexing = publicIndexingEnabled(
    process.env.DEPLOYMENT_ENVIRONMENT,
    process.env.PUBLIC_INDEXING_ENABLED
  );
  if (!indexing) {
    return { rules: { userAgent: "*", disallow: "/" } };
  }

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/admin/", "/api/", "/shop/order-status", "/shop/order-confirmation"]
    },
    sitemap: new URL("/sitemap.xml", process.env.SITE_URL).toString()
  };
}
