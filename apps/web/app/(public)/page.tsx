import { ArrowRight, Medal, Timer, Users } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { AnnouncementCard, EmptyState, EventCard } from "@/components/public/ui";
import { fallbackSettings, getAnnouncements, getCoaches, getContentBlocks, getEvents, getSiteSettings, getSponsors } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";

export default async function HomePage() {
  const [settingsResult, blocksResult, newsResult, eventsResult, coachesResult, sponsorsResult] = await Promise.allSettled([
    getSiteSettings(), getContentBlocks(), getAnnouncements("featured=true&page=1&pageSize=3"),
    getEvents("upcomingOnly=true&page=1&pageSize=3"), getCoaches(), getSponsors()
  ]);
  const settings = settingsResult.status === "fulfilled" ? settingsResult.value : fallbackSettings;
  const blocks = contentByKey(blocksResult.status === "fulfilled" ? blocksResult.value : []);
  const hero = blocks.get(CONTENT_KEYS.homeHero);
  const mission = blocks.get(CONTENT_KEYS.homeMission);
  const programs = blocks.get(CONTENT_KEYS.homePrograms);
  const announcements = newsResult.status === "fulfilled" ? newsResult.value.items : [];
  const events = eventsResult.status === "fulfilled" ? eventsResult.value.items : [];
  const coaches = coachesResult.status === "fulfilled" ? coachesResult.value.slice(0, 3) : [];
  const sponsors = sponsorsResult.status === "fulfilled" ? sponsorsResult.value.slice(0, 5) : [];

  return <>
    <section className="home-hero">
      <Image src="/images/track-hero.png" alt="Youth sprinters training together on an outdoor track" fill priority sizes="100vw" className="hero-image" />
      <div className="hero-overlay" />
      <div className="site-container hero-content"><p className="eyebrow light">Youth track and field</p><h1>{hero?.title ?? settings.clubName}</h1><p>{hero?.body ?? settings.slogan}</p><div className="button-row"><Link className="button button-primary" href={settings.primaryCtaUrl}>{settings.primaryCtaText}<ArrowRight size={18} aria-hidden="true" /></Link><Link className="button button-ghost" href={settings.secondaryCtaUrl}>{settings.secondaryCtaText}</Link></div></div>
    </section>

    {mission && <section className="mission-band"><div className="site-container mission-grid"><div><p className="eyebrow">Our purpose</p><h2>{mission.title}</h2></div><p>{mission.body}</p></div></section>}

    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Train. Grow. Compete.</p><h2>{programs?.title ?? "Programs for young athletes"}</h2></div><Link className="text-link" href="/programs">Explore programs<ArrowRight size={17} aria-hidden="true" /></Link></div>{programs?.body && <p className="section-intro">{programs.body}</p>}<div className="program-grid"><div><Timer aria-hidden="true" /><h3>Speed development</h3><p>Age-appropriate mechanics, acceleration, and confident movement.</p></div><div><Medal aria-hidden="true" /><h3>Competition preparation</h3><p>Training that helps athletes arrive ready for meets and team events.</p></div><div><Users aria-hidden="true" /><h3>Whole-athlete growth</h3><p>Discipline, teamwork, respect, and joy alongside athletic development.</p></div></div></div></section>

    <section className="content-section tone-muted"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Club updates</p><h2>Featured news</h2></div><Link className="text-link" href="/news">All news<ArrowRight size={17} aria-hidden="true" /></Link></div>{announcements.length ? <div className="card-grid">{announcements.map((item) => <AnnouncementCard key={item.slug} item={item} />)}</div> : <EmptyState title="No featured updates yet" message="Check back soon for the latest club news." />}</div></section>

    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">On the calendar</p><h2>Upcoming events</h2></div><Link className="text-link" href="/events">Full schedule<ArrowRight size={17} aria-hidden="true" /></Link></div>{events.length ? <div className="card-grid">{events.map((item) => <EventCard key={item.slug} item={item} />)}</div> : <EmptyState title="No upcoming events" message="New practices, meets, and team events will appear here." />}</div></section>

    <section className="content-section tone-dark"><div className="site-container"><div className="section-heading"><div><p className="eyebrow light">People behind the progress</p><h2>Meet the coaching team</h2></div><Link className="text-link light-link" href="/coaches">All coaches<ArrowRight size={17} aria-hidden="true" /></Link></div>{coaches.length ? <div className="coach-preview-grid">{coaches.map((coach) => <article key={`${coach.firstName}-${coach.lastName}`}><div className="avatar" aria-hidden="true">{coach.firstName[0]}{coach.lastName[0]}</div><h3>{coach.firstName} {coach.lastName}</h3><p>{coach.role}</p></article>)}</div> : <p>Coach profiles are coming soon.</p>}</div></section>

    {sponsors.length > 0 && <section className="sponsor-strip"><div className="site-container"><p className="eyebrow">Community partners</p><div>{sponsors.map((sponsor) => <span key={sponsor.slug}>{sponsor.name}</span>)}</div><Link className="text-link" href="/sponsors">Meet our sponsors<ArrowRight size={17} aria-hidden="true" /></Link></div></section>}

    <section className="cta-band"><div className="site-container"><div><p className="eyebrow light">Ready for the next step?</p><h2>{settings.slogan}</h2></div><Link className="button button-light" href={settings.primaryCtaUrl}>{settings.primaryCtaText}<ArrowRight size={18} aria-hidden="true" /></Link></div></section>
  </>;
}
