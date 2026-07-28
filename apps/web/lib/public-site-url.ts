const webProtocols = new Set(["http:", "https:"]);

export function publicSiteUrl(
  path: string,
  requestUrl: URL,
  configuredSiteUrl: string | undefined
): URL {
  let origin = requestUrl.origin;

  if (configuredSiteUrl) {
    try {
      const configuredUrl = new URL(configuredSiteUrl);
      if (webProtocols.has(configuredUrl.protocol)) origin = configuredUrl.origin;
    } catch {
      // Production configuration validation reports invalid SITE_URL values.
    }
  }

  return new URL(path, `${origin}/`);
}
