import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";
import { EmptyState, Pagination, PublicErrorState } from "@/components/public/ui";
import { getAllAmericanYears } from "@/lib/public/client";
import { isEnabledSetting } from "@/lib/runtime-config";
import styles from "./all-americans.module.css";

export const dynamic = "force-dynamic";
export const metadata: Metadata = { title: "All-American Legacy", description: "Celebrate El1te athletes who earned All-American honors at the AAU Junior Olympic Games.", alternates: { canonical: "/all-americans" } };

export default async function AllAmericansPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  if (!isEnabledSetting(process.env.ALL_AMERICANS_ARCHIVE_ENABLED)) notFound();
  const page = Math.max(1, Number((await searchParams).page) || 1);
  try {
    const result = await getAllAmericanYears(`page=${page}&pageSize=12`);
    return <>
      <header className={styles.archiveHero}><div className={`site-container ${styles.heroInner}`}><p className={styles.eyebrow}>Junior Olympic legacy</p><h1>Built for the <span>big stage</span></h1><p>Honoring the El1te athletes who earned All-American recognition at the AAU Junior Olympic Games.</p></div></header>
      <section className="content-section"><div className="site-container">
        {result.items.length ? <div className={styles.yearGrid}>{result.items.map((year) => <Link href={`/all-americans/${year.slug}`} className={styles.yearCard} key={year.slug}>
          <div className={styles.yearImage}>{year.heroImageUrl ? <ResponsiveMediaImage src={year.heroImageUrl} alt={year.heroAltText ?? ""} sizes="(max-width: 832px) 100vw, 44vw" /> : null}</div>
          <div className={styles.yearCopy}><p className={styles.eyebrow}>{year.year} archive</p><h2>{year.title}</h2><p>{year.summary}</p><div className={styles.stats}><div><strong>{year.athleteCount}</strong><span>All-Americans</span></div><div><strong>{year.medalCount}</strong><span>Medals</span></div></div><span className={styles.cardLink}>Explore the year</span></div>
        </Link>)}</div> : <EmptyState title="The legacy archive is being prepared" message="Published Junior Olympic years will appear here." />}
        <Pagination page={result.page} totalPages={result.totalPages} pathname="/all-americans" />
      </div></section>
    </>;
  } catch (error) {
    return <><header className={styles.archiveHero}><div className={`site-container ${styles.heroInner}`}><h1>All-American legacy</h1></div></header><div className="site-container content-section"><PublicErrorState error={error} /></div></>;
  }
}
