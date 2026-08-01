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
    const cspMode = (process.env.CSP_MODE ?? "report-only").toLowerCase();
    const csp = [
      "default-src 'self'",
      "base-uri 'self'",
      "frame-ancestors 'none'",
      "form-action 'self'",
      "object-src 'none'",
      "img-src 'self' data: blob: https:",
      "font-src 'self' data:",
      "style-src 'self' 'unsafe-inline'",
      "script-src 'self' 'unsafe-inline'",
      "connect-src 'self' https://*.applicationinsights.azure.com https://*.in.applicationinsights.azure.com"
    ].join("; ");
    const securityHeaders = [
      { key: "X-Content-Type-Options", value: "nosniff" },
      { key: "X-Frame-Options", value: "DENY" },
      { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
      { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=(), payment=()" },
      ...(process.env.NODE_ENV === "production" ? [{ key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" }] : []),
      ...(cspMode === "off" ? [] : [{ key: cspMode === "enforce" ? "Content-Security-Policy" : "Content-Security-Policy-Report-Only", value: csp }])
    ];
    return [{ source: "/:path*", headers: securityHeaders }];
  }
};

export default nextConfig;
