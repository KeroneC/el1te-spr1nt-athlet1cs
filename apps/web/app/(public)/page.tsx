/* eslint-disable @next/next/no-img-element */
import { ArrowRight, Medal, Timer, Users } from "lucide-react";
import Link from "next/link";
import { AnnouncementCard, EmptyState, EventCard } from "@/components/public/ui";
import { getAnnouncements, getCoaches, getContentBlocks, getEvents, getGalleryAlbums, getSponsors } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";
import { BRAND, prioritizeSponsorPreviews, sponsorTierClass } from "@/lib/public/site";

export default async function HomePage() {
  const [blocksResult, newsResult, eventsResult, coachesResult, sponsorsResult, galleryResult] = await Promise.allSettled([
    getContentBlocks(), getAnnouncements("featured=true&page=1&pageSize=3"),
    getEvents("upcomingOnly=true&page=1&pageSize=3"), getCoaches(), getSponsors(), getGalleryAlbums("page=1&pageSize=3")
  ]);
  const blocks = contentByKey(blocksResult.status === "fulfilled" ? blocksResult.value : []);
  const mission = blocks.get(CONTENT_KEYS.homeMission);
  const programs = blocks.get(CONTENT_KEYS.homePrograms);
  const announcements = newsResult.status === "fulfilled" ? newsResult.value.items : [];
  const events = eventsResult.status === "fulfilled" ? eventsResult.value.items : [];
  const coaches = coachesResult.status === "fulfilled" ? coachesResult.value.slice(0, 3) : [];
  const sponsors = sponsorsResult.status === "fulfilled" ? prioritizeSponsorPreviews(sponsorsResult.value) : [];
  const albums = galleryResult.status === "fulfilled" ? galleryResult.value.items : [];

  return <>
    <section className="home-hero">
      <div className="site-container hero-content">
        <div className="hero-logo-panel">
          <img src="/images/brand/el1te-full-black.png" alt={`${BRAND.name}. ${BRAND.slogan}`} />
        </div>
        <div className="button-row">
          <Link className="button button-primary" href="/registration">Registration Info<ArrowRight size={18} aria-hidden="true" /></Link>
          <Link className="button button-ghost" href="/gallery">View Gallery</Link>
        </div>
      </div>
    </section>

    <section className="achievement-band"><div className="site-container achievement-layout"><div className="achievement-image"><img src="/images/team/medalists.jpg" alt="Four El1te Spr1nt Athlet1cs competitors displaying their medals after a track meet" /></div><div className="achievement-copy"><p className="eyebrow">Earned together</p><h2>Confidence built at the track</h2><p>Every practice, race, and finish line is a chance for young athletes to grow in discipline, teamwork, and belief.</p><Link className="text-link" href="/about">Our mission<ArrowRight size={17} aria-hidden="true" /></Link></div></div></section>

    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Train. Grow. Compete.</p><h2>{programs?.title ?? "Programs for young athletes"}</h2></div><Link className="text-link" href="/programs">Explore programs<ArrowRight size={17} aria-hidden="true" /></Link></div>{programs?.body && <p className="section-intro">{programs.body}</p>}<div className="program-grid"><div><Timer aria-hidden="true" /><h3>Speed development</h3><p>Age-appropriate mechanics, acceleration, and confident movement.</p></div><div><Medal aria-hidden="true" /><h3>Competition preparation</h3><p>Training that helps athletes arrive ready for meets and team events.</p></div><div><Users aria-hidden="true" /><h3>Whole-athlete growth</h3><p>Discipline, teamwork, respect, and joy alongside athletic development.</p></div></div></div></section>

    {mission && <section className="mission-band"><div className="site-container mission-grid"><div><p className="eyebrow">Our purpose</p><h2>{mission.title}</h2><div className="section-rule" aria-hidden="true" /></div><p>{mission.body}</p></div></section>}

    <section className="content-section tone-muted"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Club updates</p><h2>Featured news</h2></div><Link className="text-link" href="/news">All news<ArrowRight size={17} aria-hidden="true" /></Link></div>{announcements.length ? <div className="card-grid">{announcements.map((item) => <AnnouncementCard key={item.slug} item={item} />)}</div> : <EmptyState title="No featured updates yet" message="Check back soon for the latest club news." />}</div></section>

    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">On the calendar</p><h2>Upcoming events</h2></div><Link className="text-link" href="/events">Full schedule<ArrowRight size={17} aria-hidden="true" /></Link></div>{events.length ? <div className="card-grid event-preview-grid">{events.map((item) => <EventCard key={item.slug} item={item} variant="compact" />)}</div> : <EmptyState title="No upcoming events" message="New practices, meets, and team events will appear here." />}</div></section>

    <section className="content-section tone-dark"><div className="site-container"><div className="section-heading"><div><p className="eyebrow light">People behind the progress</p><h2>Meet the coaching team</h2></div><Link className="text-link light-link" href="/coaches">All coaches<ArrowRight size={17} aria-hidden="true" /></Link></div>{coaches.length ? <div className="coach-preview-grid">{coaches.map((coach) => <article key={`${coach.firstName}-${coach.lastName}`}><div className="avatar" aria-hidden="true">{coach.firstName[0]}{coach.lastName[0]}</div><h3>{coach.firstName} {coach.lastName}</h3><p>{coach.role}</p></article>)}</div> : <p>Coach profiles are coming soon.</p>}</div></section>

    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Season snapshots</p><h2>Moments from the team</h2></div><Link className="text-link" href="/gallery">Open gallery<ArrowRight size={17} aria-hidden="true" /></Link></div>{albums.length ? <div className="gallery-album-grid compact">{albums.map((album) => <Link href={`/gallery/${album.slug}`} key={album.slug} className="gallery-album-card">{album.coverImageUrl ? <img src={album.coverImageUrl} alt={album.coverAltText ?? ""} /> : <div className="gallery-placeholder">Gallery album</div>}<div><p className="eyebrow">{album.imageCount} {album.imageCount === 1 ? "photo" : "photos"}</p><h2>{album.title}</h2><p>{album.description}</p></div></Link>)}</div> : <EmptyState title="Gallery albums are coming soon" message="Published team photo collections will appear here." />}</div></section>

    {sponsors.length > 0 && <section className="sponsor-strip"><div className="site-container"><div className="sponsor-strip-heading"><div><p className="eyebrow">Community partners</p><h2>Backing the next generation</h2></div><Link className="text-link" href="/sponsors">Meet our sponsors<ArrowRight size={17} aria-hidden="true" /></Link></div><div className="sponsor-name-list">{sponsors.map((sponsor) => <article className={sponsorTierClass(sponsor.tier)} key={sponsor.slug}><small>{sponsor.tier}</small><div className="sponsor-preview-logo">{sponsor.logoUrl ? <img src={sponsor.logoUrl} alt={`${sponsor.name} logo`} /> : <span>{sponsor.name}</span>}</div></article>)}</div></div></section>}

    <section className="cta-band"><div className="site-container"><div><p className="eyebrow light">Ready for the next step?</p><h2>{BRAND.slogan}</h2><p>Review the registration packet and contact the club before submitting paperwork for staff review.</p></div><div className="button-row"><Link className="button button-light" href="/registration">Registration Info<ArrowRight size={18} aria-hidden="true" /></Link><Link className="button button-ghost" href="/contact">Ask a question</Link></div></div></section>
  </>;
}
