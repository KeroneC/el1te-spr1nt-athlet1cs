export const SUPPORT_REFERENCE_HEADER = "X-Reference-Id";
const SUPPORT_REFERENCE_PATTERN = /^ESA-[0-9A-F]{16}$/;

export function validSupportReference(value: unknown): string | null {
  if (typeof value !== "string") return null;
  const normalized = value.trim().toUpperCase();
  return SUPPORT_REFERENCE_PATTERN.test(normalized) ? normalized : null;
}

export function supportReferenceFromDigest(digest: string | undefined): string | null {
  if (!digest) return null;
  const normalized = digest.trim().replace(/[^a-zA-Z0-9_-]/g, "").slice(0, 32);
  return normalized ? `WEB-${normalized}` : null;
}
