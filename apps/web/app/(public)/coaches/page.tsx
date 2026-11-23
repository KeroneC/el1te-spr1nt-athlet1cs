import type { Metadata } from "next";
import Link from "next/link";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";
import { SupportStaffSection } from "@/components/public/support-staff-section";
import { EmptyState, PageHero, PublicErrorState } from "@/components/public/ui";
import { getCoaches } from "@/lib/public/client";

export const metadata: Metadata = {
  title: "Coaches",
  description: "Meet the coaches and support staff behind our young athletes.",
};

async function CoachingRoster() {
  try {
    const coaches = await getCoaches();
    if (!coaches.length) {
      return <EmptyState title="Coach profiles are coming soon" message="Our active coaching team will be listed here." />;
    }

    return (
      <div className="coach-grid">
        {coaches.map((coach) => (
          <article className="coach-card" key={`${coach.firstName}-${coach.lastName}`}>
            <div className="coach-photo">
              {coach.imageUrl ? (
                <ResponsiveMediaImage
                  src={coach.imageUrl}
                  alt={`${coach.firstName} ${coach.lastName}`}
                  sizes="(max-width: 768px) 100vw, 33vw"
                />
              ) : (
                <span aria-hidden="true">{coach.firstName[0]}{coach.lastName[0]}</span>
              )}
            </div>
            <div>
              <h2>{coach.firstName} {coach.lastName}</h2>
              <p className="eyebrow">{coach.role}</p>
            </div>
          </article>
        ))}
      </div>
    );
  } catch (error) {
    return <PublicErrorState error={error} />;
  }
}

export default function CoachesPage() {
  return (
    <>
      <PageHero eyebrow="Our team" title="Meet The" accent="Coaches" summary="Instruction, encouragement, and positive competition at every stage." />
      <section className="content-section">
        <div className="site-container">
          <CoachingRoster />
        </div>
      </section>
      <SupportStaffSection />
      <section className="cta-band">
        <div className="site-container">
          <div>
            <p className="eyebrow light">Team support</p>
            <h2>Meet the team culture behind the coaching.</h2>
            <p>Learn how the club presents team identity publicly without exposing private athlete roster information.</p>
          </div>
          <div className="button-row">
            <Link className="button button-light" href="/team">Team page</Link>
            <Link className="button button-ghost" href="/registration">Registration Info</Link>
          </div>
        </div>
      </section>
    </>
  );
}
