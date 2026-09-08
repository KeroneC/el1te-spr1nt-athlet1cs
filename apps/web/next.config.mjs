import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

if (process.env.NODE_ENV === "production" && (!process.env.API_BASE_URL || !process.env.SITE_URL)) {
  throw new Error("API_BASE_URL and SITE_URL are required for a production build.");
}

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  outputFileTracingRoot: path.join(__dirname, "../.."),
  async headers() {
    const securityHeaders = [
      { key: "X-Content-Type-Options", value: "nosniff" },
      { key: "X-Frame-Options", value: "DENY" },
      { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
      { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(), payment=()" },
      ...(process.env.NODE_ENV === "production" ? [{ key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" }] : [])
    ];
    const publicAssetCache = [
      { key: "Cache-Control", value: "public, max-age=86400, stale-while-revalidate=604800" }
    ];
    return [
      { source: "/images/:path*", headers: publicAssetCache },
      { source: "/brand/:path*", headers: publicAssetCache },
      { source: "/favicon.png", headers: publicAssetCache },
      { source: "/:path*", headers: securityHeaders }
    ];
  }
};

export default nextConfig;
