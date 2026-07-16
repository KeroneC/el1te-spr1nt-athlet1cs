const PUBLIC_MEDIA_PATH = /^\/media\/[0-9a-f-]{36}$/i;

export function sizedPublicMediaUrl(url: string, width: 480 | 800 | 1200 | 1600) {
  try {
    const parsed = new URL(url);
    if (!PUBLIC_MEDIA_PATH.test(parsed.pathname)) return url;
    parsed.searchParams.set("width", String(width));
    return parsed.toString();
  } catch {
    return url;
  }
}
