import type { Metadata } from "next";
import { Mail, Trophy } from "lucide-react";
import Link from "next/link";
import { PageHero } from "@/components/public/ui";
import { HALL_OF_FAME_INDUCTEES } from "@/lib/public/site";

export const metadata: Metadata = {
  title: "RGN El1te Hall of Fame",
  description: "Celebrate El1te Spr1nt Athlet1cs athletes continuing their journey to greatness."
};

export default function HallOfFamePage() {
  return <>
    <PageHero
      eyebrow="RGN El1te Hall of Fame"
      title="Greatness Beyond"
      accent="The Track"
      summary="Created in honor of Roland George Newton, the Hall of Fame recognizes athletes who continue their journey with purpose, discipline, and community pride."
    />
    <section className="content-section">
      <div className="site-container story-layout">
        <div>
          <p className="eyebrow">Recognition</p>
          <h2>Celebrating the path beyond the finish line</h2>
          <p className="section-intro">The RGN El1te Hall of Fame highlights athletes who carry club standards into school, competition, leadership, and life after their youth track journey.</p>
        </div>
        <aside className="feature-panel">
          <Trophy aria-hidden="true" />
          <h2>Have an update?</h2>
          <p>Families and alumni can contact the club with Hall of Fame updates, photos, or corrected information for review.</p>
          <Link className="button button-primary" href="/contact">Send an update<Mail size={17} aria-hidden="true" /></Link>
        </aside>
      </div>
    </section>
    <section className="content-section tone-muted">
      <div className="site-container">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Inductees</p>
            <h2>RGN El1te Hall of Fame</h2>
          </div>
        </div>
        <div className="honor-grid">
          {HALL_OF_FAME_INDUCTEES.map((inductee) => (
            <article className="honor-card" key={inductee.name}>
              <div className="honor-medal" aria-hidden="true"><Trophy size={30} /></div>
              <h3>{inductee.name}</h3>
              <p>{inductee.summary}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  </>;
}
