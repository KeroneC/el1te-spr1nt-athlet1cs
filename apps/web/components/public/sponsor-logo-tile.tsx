"use client";

/* eslint-disable @next/next/no-img-element */
import { createElement, useState, type CSSProperties } from "react";
import { sponsorTierClass } from "../../lib/public/site";
import type { Sponsor } from "../../lib/public/types";

type SponsorLogoTileProps = {
  sponsor: Sponsor;
  variant?: "full" | "preview";
  revealIndex?: number;
};

export function hasUsableSponsorLogo(logoUrl: string | null, failed: boolean) {
  return Boolean(logoUrl) && !failed;
}

export function SponsorLogoTile({ sponsor, variant = "full", revealIndex }: SponsorLogoTileProps) {
  const [logoFailed, setLogoFailed] = useState(false);
  const showLogo = hasUsableSponsorLogo(sponsor.logoUrl, logoFailed);
  const linked = Boolean(sponsor.websiteUrl);

  const logo = showLogo
    ? createElement("img", {
        src: sponsor.logoUrl ?? undefined,
        alt: linked ? "" : `${sponsor.name} logo`,
        loading: "lazy",
        decoding: "async",
        onError: () => setLogoFailed(true)
      })
    : createElement("span", { className: "sponsor-logo-fallback", "aria-hidden": linked || undefined }, sponsor.name);
  const content = createElement("span", { className: "sponsor-logo-tile-canvas" }, logo);
  const tile = sponsor.websiteUrl
    ? createElement("a", {
        className: "sponsor-logo-tile sponsor-logo-tile-linked",
        href: sponsor.websiteUrl,
        target: "_blank",
        rel: "noreferrer noopener",
        "aria-label": `Visit ${sponsor.name} website (opens in a new tab)`
      }, content)
    : createElement("div", { className: "sponsor-logo-tile" }, content);

  return createElement("div", {
    className: `sponsor-logo-item sponsor-tier ${sponsorTierClass(sponsor.tier)} sponsor-logo-item-${variant}`,
    role: "listitem",
    style: revealIndex === undefined ? undefined : ({ "--sponsor-reveal-index": revealIndex } as CSSProperties)
  }, tile);
}
