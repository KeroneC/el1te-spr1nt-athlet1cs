import type { Metadata } from "next";
import Link from "next/link";
import { SponsorTierSection } from "@/components/public/sponsor-tier-section";
import { EmptyState, PageHero, PublicErrorState } from "@/components/public/ui";
import { getContentBlocks, getSponsors } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";

export const metadata: Metadata = {
  title: "Sponsors",
  description: "Meet the community partners who support youth athletics."
};

export default async function SponsorsPage() {
  try {
    const [sponsors, blocks] = await Promise.all([getSponsors(), getContentBlocks()]);
    const intro = contentByKey(blocks).get(CONTENT_KEYS.sponsorsIntro);
    const tiers = ["Platinum", "Gold", "Silver", "Bronze", "Community", "Other"];
    const grouped = tiers
      .map((tier) => ({ tier, items: sponsors.filter((sponsor) => sponsor.tier === tier) }))
      .filter((group) => group.items.length);

    return <>
      <PageHero eyebrow="Community partners" title="Our" accent="Sponsors" summary={intro?.body ?? intro?.title} />
      {sponsors.length ? <div className="sponsor-podium">
        {grouped.map((group) => <SponsorTierSection
          key={group.tier}
          sponsors={group.items}
          tier={group.tier as (typeof sponsors)[number]["tier"]}
        />)}
      </div> : <section className="content-section"><div className="site-container"><EmptyState title="Sponsor profiles are coming soon" message="Active community partners will be recognized here." /></div></section>}
      <section className="cta-band"><div className="site-container"><div><h2>Interested in supporting the team?</h2><p>Start a sponsorship conversation with the club. No sponsor payments are collected through this site.</p></div><Link className="button button-light" href="/contact">Contact us</Link></div></section>
    </>;
  } catch {
    return <><PageHero title="Our" accent="Sponsors" /><div className="site-container content-section"><PublicErrorState /></div></>;
  }
}
