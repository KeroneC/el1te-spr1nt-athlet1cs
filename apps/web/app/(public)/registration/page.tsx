import type { Metadata } from "next";
import { ArrowRight, CalendarDays, CircleDollarSign, ClipboardList, Clock3, Mail, MapPin, Users } from "lucide-react";
import Link from "next/link";
import { FormDownloadList } from "@/components/public/form-download-list";
import { REGISTRATION_DOWNLOADS } from "@/lib/public/site";
import { getRegistrationSeason } from "@/lib/public/registration";
import { registrationChannel, registrationDate, registrationOpeningDate, upcomingRegistrationDate } from "@/lib/registration";
import { RegistrationVisit } from "@/components/public/registration-intake";
import { RegistrationPaths } from "@/components/public/registration-paths";
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
  const planned = state !== "unavailable" && !season?.isOpen
    ? upcomingRegistrationDate(season?.openingDate ?? registrationOpeningDate)
    : null;
  const seasonDetails = [
    { label: "Eligibility", value: "Ages 6 and up", Icon: Users },
    { label: "First practice", value: season?.firstPracticeDate ? registrationDate(season.firstPracticeDate) : null, Icon: CalendarDays },
    { label: "Practice location", value: season?.practiceLocation, Icon: MapPin },
    { label: "Practice schedule", value: season?.practiceSchedule, Icon: CalendarDays },
    { label: "Fees", value: season?.fees, Icon: CircleDollarSign },
    { label: "When you’ll hear back", value: season?.replyWindow, Icon: Clock3 }
  ];
  const intro = <div className={styles.intro}>
    <p className={styles.heroOverline}><span aria-hidden="true" />El1te track &amp; field <span aria-hidden="true" /> Registration</p>
    <h1>Find your <em>starting lane</em></h1>
    <p>New to El1te or back for another season? Start here.</p>
  </div>;

  return <div data-registration-hub="true" className={styles.page}>
    {season && <RegistrationVisit year={season.year} channel={channel} />}
    {season?.isOpen ? <RegistrationPaths season={season} channel={channel} intro={intro} /> :
      <section className={styles.hero} aria-label="Registration">
        <div className={`site-container ${styles.heroContent}`}>
          {intro}
          <div className={styles.statusPanel}>
            <h2 className={styles.statusPill}>{state === "unavailable" ? "Registration is temporarily unavailable"
              : planned ? "Applications open soon" : "Applications are currently closed"}</h2>
            {state === "unavailable" ? <>
              <p>We can’t check registration status right now. Try again or ask the club a question.</p>
              <a className={styles.primaryAction} href={retryHref}>Check registration status</a>
            </> : planned ? <p>Planned opening: <strong>{registrationDate(planned)}</strong>. Check back here for availability.</p>
              : <p>Check back here for the next opening, or <Link href="/contact">contact the club</Link> with questions.</p>}
          </div>
        </div>
      </section>}
    <section className={styles.seasonSection} aria-labelledby="registration-season-heading">
      <div className={`site-container ${styles.seasonLayout}`}>
        <div>
          <p className={styles.pathOverline}>{season?.title || "Club registration"}</p>
          <h2 id="registration-season-heading">Season at a glance</h2>
          <dl className={styles.seasonDetails}>
            {seasonDetails.filter(({ value }) => value).map(({ label, value, Icon }) => <div key={label}>
              <Icon size={19} aria-hidden="true" /><dt>{label}</dt><dd>{value}</dd>
            </div>)}
          </dl>
        </div>
        <aside className={styles.help} aria-label="Registration help">
          <h3>What happens next?</h3>
          <p>Save your reference after submitting. Staff review applications individually and follow up by email; submitting doesn’t reserve a spot.</p>
          <div className={styles.helpActions}>
            <Link href="/contact">Ask a registration question <Mail size={16} aria-hidden="true" /></Link>
            <Link href="/faqs">Read frequently asked questions <ArrowRight size={16} aria-hidden="true" /></Link>
          </div>
        </aside>
      </div>
    </section>
    <section className={styles.paperworkSection} aria-labelledby="registration-paperwork-heading">
      <div className="site-container">
        <div className={styles.paperworkHeader}>
          <ClipboardList size={23} aria-hidden="true" />
          <div><p className={styles.pathOverline}>For new athletes offered a spot</p><h2 id="registration-paperwork-heading">Preview the paperwork</h2></div>
          <Link className={styles.secondaryAction} href="/forms">All forms <ArrowRight size={17} aria-hidden="true" /></Link>
        </div>
        <p className={styles.paperworkIntro}>Wait for an offer and club instructions before sending completed forms or proof of age. Photo consent is optional. Returning athletes do not need paperwork.</p>
        <FormDownloadList forms={REGISTRATION_DOWNLOADS} />
      </div>
    </section>
  </div>;
}
