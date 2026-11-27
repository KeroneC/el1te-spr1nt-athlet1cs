import type { Metadata } from "next";
import { ChevronDown } from "lucide-react";
import Link from "next/link";
import { getRegistrationSeason } from "@/lib/public/registration";
import { openingPhase, registrationChannel, registrationOpeningDate } from "@/lib/registration";
import { registration2027Copy } from "@/lib/registration-2027-copy.mjs";
import { RegistrationPoster } from "@/components/public/registration-poster";
import { RegistrationVisit } from "@/components/public/registration-intake";
import { RegistrationPaths } from "@/components/public/registration-paths";
import { isLocalRegistrationArchivePreview } from "@/lib/local-registration-preview.mjs";
import { EntranceArt, EntranceHeader } from "@/components/public/registration-entrance";
import line from "@/components/public/starting-line.module.css";
import styles from "@/components/public/registration.module.css";

export const metadata: Metadata = {
  title: "Registration Hub",
  description: "Review registration information, required forms, and next steps for El1te Spr1nt Athlet1cs.",
  referrer: "no-referrer"
};

export default async function RegistrationPage({ searchParams }: { searchParams: Promise<{ src?: string }> }) {
  const [{ state, season }, query] = await Promise.all([getRegistrationSeason(), searchParams]);
  const channel = registrationChannel(query.src);
  const retryHref = channel === "direct" ? "/registration" : `/registration?src=${channel}`;
  const openingDate = season?.openingDate ?? registrationOpeningDate;
  const showPracticeWindow = state !== "unavailable" && (!season || season.year === 2027);
  // These three public logistics can be shown before opening; never use draft season settings.
  const advanceCopy = showPracticeWindow && season?.title == null && !season?.isOpen ? registration2027Copy : null;
  // The API only releases the title once a season has opened. A reclosure on
  // opening day must not promise a second opening.
  const phase = state === "unavailable" || season?.title != null ? "closed" : openingPhase(openingDate);
  const seasonDetails = [
    { label: "Practice location", value: season?.practiceLocation ?? advanceCopy?.practiceLocation },
    { label: "Practice schedule", value: season?.practiceSchedule ?? advanceCopy?.practiceSchedule },
    { label: "Tuition", value: season?.fees ?? advanceCopy?.fees },
    { label: "When you’ll hear back", value: season?.replyWindow }
  ].filter(({ value }) => value);
  return <div data-registration-hub="true" className={styles.page}>
    {season && <RegistrationVisit year={season.year} channel={channel} />}
    {season?.isOpen ? <RegistrationPaths season={season} channel={channel} archivePreview={isLocalRegistrationArchivePreview()} /> :
      phase !== "closed" ? <RegistrationPoster openingDate={openingDate} today={phase === "today"} retryHref={retryHref} /> :
      <section className={line.entrance} aria-label="Registration">
        <EntranceHeader />
        <div className={line.closedScene}>
          <EntranceArt closed />
          <div className={line.closedCopy}>
            <h2>{state === "unavailable" ? "Registration is temporarily unavailable"
              : "Applications are currently closed"}</h2>
            {state === "unavailable" ? <>
              <p>We can’t check registration status right now. Try again or ask the club a question.</p>
              <a className={line.closedAction} href={retryHref}>Check registration status</a>
            </> : <p>Check back here for the next opening, or <Link href="/contact">contact the club</Link> with questions.</p>}
          </div>
        </div>
      </section>}
    <aside aria-label="Registration resources" className={styles.resources}>
      {showPracticeWindow && <div className="site-container">
        <p className={styles.practiceNotice}><strong>First indoor practice:</strong> We expect to start in late January or early February. We’ll share the exact date closer to the start of the season.</p>
      </div>}
      <div className={`site-container ${styles.resourceLayout}`}>
        {seasonDetails.length > 0 && <details className={styles.season}>
          <summary>Season details <ChevronDown size={16} aria-hidden="true" /></summary>
          {season?.title && <p className={styles.seasonTitle}>{season.title}</p>}
          <dl className={styles.seasonDetails}>
            {seasonDetails.map(({ label, value }) => <div key={label}>
              <dt>{label}</dt><dd>{value}</dd>
            </div>)}
          </dl>
        </details>}
        <nav className={styles.resourceLinks} aria-label="Registration help">
          <Link href="/forms">Forms</Link>
          <Link href="/faqs">FAQs</Link>
          <Link href="/contact">Contact the club</Link>
        </nav>
      </div>
    </aside>
  </div>;
}
