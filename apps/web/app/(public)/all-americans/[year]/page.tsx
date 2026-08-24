import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";
import { getAllAmericanYear, PublicApiError } from "@/lib/public/client";
import { isEnabledSetting } from "@/lib/runtime-config";
import styles from "../all-americans.module.css";

export const dynamic = "force-dynamic";
type Props = { params: Promise<{ year: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  if (!isEnabledSetting(process.env.ALL_AMERICANS_ARCHIVE_ENABLED)) return { title: "All-American archive" };
  try { const record = await getAllAmericanYear((await params).year); return { title: record.title, description: record.summary, alternates: { canonical: `/all-americans/${record.slug}` } }; }
  catch { return { title: "All-American archive" }; }
}

function placementLabel(value: number | null) {
  if (!value) return null;
  const mod100 = value % 100;
  const suffix = mod100 >= 11 && mod100 <= 13 ? "th" : value % 10 === 1 ? "st" : value % 10 === 2 ? "nd" : value % 10 === 3 ? "rd" : "th";
  return `${value}${suffix}`;
}

export default async function AllAmericanYearPage({ params }: Props) {
  if (!isEnabledSetting(process.env.ALL_AMERICANS_ARCHIVE_ENABLED)) notFound();
  let record;
  try { record = await getAllAmericanYear((await params).year); }
  catch (error) { if (error instanceof PublicApiError && error.status === 404) notFound(); throw error; }
  return <article>
    <header className={styles.detailHero}>{record.heroImageUrl && <ResponsiveMediaImage src={record.heroImageUrl} alt={record.heroAltText ?? ""} priority sizes="100vw" />}<div className={`site-container ${styles.detailCopy}`}><Link className={styles.back} href="/all-americans">Back to the legacy archive</Link><h1>{record.title}</h1><p>{record.summary}</p></div><div className={`site-container ${styles.detailStats}`}><div><strong>{record.athleteCount}</strong><span>All-Americans</span></div><div><strong>{record.medalCount}</strong><span>All-American medals</span></div></div></header>
    <section className="content-section"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">The moments</p><h2>{record.year} Junior Olympics</h2></div></div><div className={styles.photoGrid}>{record.images.map((image, index) => <figure key={`${image.publicUrl}-${index}`}><ResponsiveMediaImage src={image.publicUrl} alt={image.altText} width={image.width} height={image.height} sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw" />{image.caption && <figcaption>{image.caption}</figcaption>}</figure>)}</div></div></section>
    {record.detailsComplete && <section className="content-section tone-muted"><div className="site-container"><div className="section-heading"><div><p className="eyebrow">Verified results</p><h2>Meet the All-Americans</h2></div></div><div className={styles.roster}>{record.recipients.map((athlete) => <article className={styles.athlete} key={`${athlete.firstName}-${athlete.lastName}`}><div className={styles.athletePhoto}>{athlete.photoUrl ? <ResponsiveMediaImage src={athlete.photoUrl} alt={athlete.photoAltText ?? `${athlete.firstName} ${athlete.lastName}`} sizes="(max-width: 640px) 100vw, 25vw" /> : <span className={styles.fallback} aria-hidden="true">{athlete.firstName[0]}{athlete.lastName[0]}</span>}</div><h3>{athlete.firstName} {athlete.lastName}</h3><ul>{athlete.results.map((result, index) => <li key={`${result.eventName}-${index}`}><strong>{result.eventName}</strong>{result.division ? ` · ${result.division}` : ""}{result.isRelay ? " · Relay" : ""}{placementLabel(result.placement) ? ` · ${placementLabel(result.placement)}` : ""}</li>)}</ul></article>)}</div></div></section>}
  </article>;
}
