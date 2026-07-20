"use client";

import { createElement, useEffect, useRef, useState } from "react";
import { sponsorTierClass } from "../../lib/public/site";
import type { Sponsor } from "../../lib/public/types";
import { SponsorLogoTile } from "./sponsor-logo-tile";

type SponsorTierSectionProps = {
  sponsors: Sponsor[];
  tier: Sponsor["tier"];
};

export function SponsorTierSection({ sponsors, tier }: SponsorTierSectionProps) {
  const sectionRef = useRef<HTMLElement>(null);
  const [revealReady, setRevealReady] = useState(false);
  const [visible, setVisible] = useState(false);
  const headingId = `sponsor-${tier.toLowerCase()}`;

  useEffect(() => {
    const section = sectionRef.current;
    if (!section) return;

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reducedMotion || !("IntersectionObserver" in window)) {
      setRevealReady(true);
      setVisible(true);
      return;
    }

    setRevealReady(true);
    const observer = new IntersectionObserver(([entry]) => {
      if (!entry.isIntersecting) return;
      setVisible(true);
      observer.unobserve(entry.target);
    }, { rootMargin: "0px 0px -8%", threshold: 0.12 });

    observer.observe(section);
    return () => observer.disconnect();
  }, []);

  const className = [
    "sponsor-tier-band",
    sponsorTierClass(tier),
    revealReady ? "is-reveal-ready" : "",
    visible ? "is-visible" : ""
  ].filter(Boolean).join(" ");

  const heading = createElement("div", { className: "sponsor-tier-heading" },
    createElement("h2", { id: headingId }, `${tier} Sponsors`)
  );

  const grid = createElement("div", { className: "sponsor-logo-grid", role: "list" },
    sponsors.map((sponsor, index) => createElement(SponsorLogoTile, {
      sponsor,
      revealIndex: index,
      key: sponsor.slug
    }))
  );

  return createElement("section", { className, ref: sectionRef, "aria-labelledby": headingId },
    createElement("div", { className: "sponsor-tier-inner" }, heading, grid)
  );
}
