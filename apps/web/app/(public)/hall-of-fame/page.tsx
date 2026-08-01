import type { Metadata } from "next";
import { Mail } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { EmptyState, Pagination, PublicErrorState } from "@/components/public/ui";
import { getHallOfFameInductees } from "@/lib/public/client";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";

export const metadata: Metadata = {
  title: "RGN El1te Hall of Fame",
  description: "Honor Roland George Newton and celebrate El1te Spr1nt Athlet1cs athletes continuing their journey to greatness."
};

export default async function HallOfFamePage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  const params = await searchParams;
  const page = Math.max(1, Number(params.page) || 1);
  let inducteeContent: React.ReactNode;
  try {
    const result = await getHallOfFameInductees(`page=${page}&pageSize=8`);
    inducteeContent = result.items.length ? <>
      <div className="hall-inductee-grid">
        {result.items.map((inductee) => <article className="hall-inductee" key={inductee.slug}>
          <div className="hall-inductee-photo"><ResponsiveMediaImage src={inductee.photoUrl} alt={inductee.photoAlt} sizes="(max-width: 768px) 100vw, 50vw" /></div>
          <div className="hall-inductee-body">
            <p className="eyebrow">{inductee.inductionYear ? <>Class of {inductee.inductionYear} <span aria-hidden="true">·</span> {inductee.affiliation}</> : inductee.affiliation}</p>
            <h3>{inductee.name}</h3>
            <p>{inductee.summary}</p>
          </div>
        </article>)}
      </div>
      <Pagination page={result.page} totalPages={result.totalPages} pathname="/hall-of-fame" />
    </> : <EmptyState title="The next chapter is being written" message="Hall of Fame inductees will appear here after club review." />;
  } catch (error) {
    inducteeContent = <PublicErrorState error={error} />;
  }
  return <>
    <section className="hall-hero">
      <div className="site-container hall-hero-content">
        <p className="eyebrow light">A family legacy</p>
        <h1 className="sr-only">RGN El1te Hall of Fame</h1>
        <Image src="/images/hall-of-fame/rgn-hall-of-fame-crest.png" alt="" width={1024} height={1024} priority sizes="(max-width: 48rem) 90vw, 36rem" className="hall-crest" />
      </div>
    </section>

    <section className="hall-dedication">
      <div className="site-container hall-dedication-layout">
        <div>
          <p className="eyebrow light">In honor of our dad</p>
          <h2>Roland George Newton</h2>
        </div>
        <p>Roland George Newton, our dad and quiet cheerleader, passed away on March 13, 2024. He loved having young people and friends around, and his legacy lives on through the RGN El1te Hall of Fame, honoring athletes who continue their journey to greatness on or off the track.</p>
      </div>
    </section>

    <section className="content-section hall-inductees">
      <div className="site-container">
        <div className="section-heading"><div><p className="eyebrow">Meet our inductees</p><h2>Carrying greatness forward</h2></div></div>
        {inducteeContent}
      </div>
    </section>

    <section className="cta-band"><div className="site-container"><div><p className="eyebrow light">Help preserve the story</p><h2>Have a Hall of Fame update?</h2><p>Families and alumni can share photos, achievements, or corrected information for club review.</p></div><Link className="button button-light" href="/contact">Send an update<Mail size={17} aria-hidden="true" /></Link></div></section>
  </>;
}
