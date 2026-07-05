/* eslint-disable @next/next/no-img-element */
import type { Metadata } from "next";
import { ArrowRight, Camera, ShieldCheck, Sparkles, Users } from "lucide-react";
import Link from "next/link";
import { EmptyState, PageHero } from "@/components/public/ui";
import { getGalleryAlbums } from "@/lib/public/client";
import { TEAM_VALUES } from "@/lib/public/site";

export const metadata: Metadata = {
  title: "Team",
  description: "Learn about the El1te Spr1nt Athlet1cs team identity without exposing private athlete information."
};

export default async function TeamPage() {
  const albums = await getGalleryAlbums("page=1&pageSize=2").then((result) => result.items).catch(() => []);

  return <>
    <PageHero
      eyebrow="Team identity"
      title="One Club"
      accent="One Standard"
      summary="A public look at the team culture behind El1te Spr1nt Athlet1cs without publishing private athlete roster details."
    />
    <section className="content-section">
      <div className="site-container story-layout">
        <div>
          <p className="eyebrow">Who we are</p>
          <h2>Focused athletes, supportive families, committed coaches</h2>
          <p className="section-intro">El1te Spr1nt Athlet1cs is a youth track and field club built around development, discipline, competition, and community support. The public team page keeps the focus on shared identity instead of private athlete information.</p>
          <div className="button-row">
            <Link className="button button-primary" href="/registration">Registration Info<ArrowRight size={17} aria-hidden="true" /></Link>
            <Link className="button button-secondary" href="/coaches">Meet the coaches</Link>
          </div>
        </div>
        <aside className="feature-panel">
          <Users aria-hidden="true" />
          <h2>Public roster note</h2>
          <p>Private athlete details are intentionally not published here. Families can use the gallery, coaches page, and registration hub for public team information.</p>
        </aside>
      </div>
    </section>
    <section className="content-section tone-muted">
      <div className="site-container">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Team values</p>
            <h2>What athletes practice together</h2>
          </div>
        </div>
        <div className="value-grid">
          {TEAM_VALUES.map((value, index) => {
            const Icon = [Sparkles, ShieldCheck, Users, Camera][index] ?? Sparkles;
            return <article className="value-card" key={value}><Icon aria-hidden="true" /><h3>{value}</h3></article>;
          })}
        </div>
      </div>
    </section>
    <section className="content-section">
      <div className="site-container">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Team moments</p>
            <h2>Recent gallery albums</h2>
          </div>
          <Link className="text-link" href="/gallery">Open gallery<ArrowRight size={17} aria-hidden="true" /></Link>
        </div>
        {albums.length ? <div className="gallery-album-grid compact">{albums.map((album) => (
          <Link href={`/gallery/${album.slug}`} key={album.slug} className="gallery-album-card">
            {album.coverImageUrl ? <img src={album.coverImageUrl} alt={album.coverAltText ?? ""} /> : <div className="gallery-placeholder">Gallery album</div>}
            <div><p className="eyebrow">{album.imageCount} {album.imageCount === 1 ? "photo" : "photos"}</p><h2>{album.title}</h2><p>{album.description}</p></div>
          </Link>
        ))}</div> : <EmptyState title="Team photos are coming soon" message="Published gallery albums will appear here." />}
      </div>
    </section>
  </>;
}
