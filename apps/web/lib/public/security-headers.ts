export const contentSecurityPolicy = [
  "default-src 'self'",
  "base-uri 'self'",
  "frame-ancestors 'none'",
  "form-action 'self'",
  "object-src 'none'",
  "img-src 'self' data: blob: https:",
  "font-src 'self' data:",
  "style-src 'self' 'unsafe-inline'",
  "script-src 'self' 'unsafe-inline'",
  "connect-src 'self' https://*.applicationinsights.azure.com https://*.in.applicationinsights.azure.com",
].join("; ");

export function runtimeCspHeader(value = process.env.CSP_MODE) {
  const mode = (value ?? "report-only").trim().toLowerCase();
  if (mode === "off") return null;
  return {
    name: mode === "enforce" ? "Content-Security-Policy" : "Content-Security-Policy-Report-Only",
    value: contentSecurityPolicy,
  } as const;
}
