import type { ImgHTMLAttributes } from "react";

/* eslint-disable @next/next/no-img-element -- Managed API media supplies its own responsive WebP srcset. */

const managedMediaPattern = /\/media\/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?:$|[?#])/i;

export function isManagedMediaUrl(value: string): boolean {
  try {
    const parsed = new URL(value, "http://local.invalid");
    return managedMediaPattern.test(parsed.pathname);
  } catch {
    return false;
  }
}

export function mediaUrlAtWidth(value: string, width: 480 | 960 | 1600): string {
  if (!isManagedMediaUrl(value)) return value;
  const separator = value.includes("?") ? "&" : "?";
  return `${value}${separator}width=${width}`;
}

type Props = Omit<ImgHTMLAttributes<HTMLImageElement>, "src" | "srcSet" | "loading" | "decoding"> & {
  src: string;
  priority?: boolean;
};

export function ResponsiveMediaImage({
  src,
  alt,
  priority = false,
  sizes = "(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw",
  width = 1600,
  height = 900,
  ...props
}: Props) {
  const managed = isManagedMediaUrl(src);
  return <img
    {...props}
    src={managed ? mediaUrlAtWidth(src, 960) : src}
    srcSet={managed ? [480, 960, 1600].map((candidate) => `${mediaUrlAtWidth(src, candidate as 480 | 960 | 1600)} ${candidate}w`).join(", ") : undefined}
    sizes={managed ? sizes : undefined}
    width={width}
    height={height}
    alt={alt}
    loading={priority ? "eager" : "lazy"}
    decoding="async"
    fetchPriority={priority ? "high" : "auto"}
  />;
}
