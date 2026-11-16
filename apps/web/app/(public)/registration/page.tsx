import type { Metadata } from "next";
import { ArrowRight, Mail } from "lucide-react";
import Link from "next/link";
import { PageHero } from "@/components/public/ui";
import { FormDownloadList } from "@/components/public/form-download-list";
import { REGISTRATION_DOWNLOADS } from "@/lib/public/site";
import { getRegistrationSeason } from "@/lib/public/registration";
import { registrationChannel, registrationDate, registrationOpeningDate, upcomingRegistrationDate } from "@/lib/registration";
import { RegistrationVisit } from "@/components/public/registration-intake";
import { RegistrationPaths } from "@/components/public/registration-paths";

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
  const seasonDetails = <>
    {season?.title && <dl className="mt-5 grid gap-4 sm:grid-cols-2">
      {[
        ["Eligible age groups", season.ageGroups.join(", ")], ["Practice location", season.practiceLocation],
        ["Practice schedule", season.practiceSchedule], ["Fees", season.fees], ["When you’ll hear back", season.replyWindow]
      ].map(([label, value]) => <div key={label}><dt className="font-bold">{label}</dt><dd className="whitespace-pre-line break-words">{value}</dd></div>)}
    </dl>}
    <p className="mt-5">Staff review submissions individually. An application or returning check-in does not reserve a spot.</p>
  </>;
  const aside = <aside className="registration-aside">
    <h2>What happens next?</h2>
    <ul>
      <li>Save the reference shown after you submit. An automatic confirmation email won’t be sent.</li>
      <li>Wait for the club to follow up by email.</li>
      <li>New athletes send paperwork only after an offer. Returning athletes check in online; no paperwork is required.</li>
    </ul>
    <div className="registration-help-actions">
      <Link className="button button-secondary" href="/contact">Ask a registration question<Mail size={17} aria-hidden="true" /></Link>
      <Link className="text-link registration-faq-link" href="/faqs">Read frequently asked questions<ArrowRight size={17} aria-hidden="true" /></Link>
    </div>
  </aside>;
  return <div data-registration-hub="true">
    <PageHero eyebrow="Registration Hub" title={season?.title ?? "Registration"}
      summary="Apply for a spot with El1te Spr1nt Athlet1cs or let us know your athlete plans to return." />
    {season && <RegistrationVisit year={season.year} channel={channel} />}
    <section className="content-section">
      <div className="site-container registration-layout">
        {season?.isOpen
          ? <RegistrationPaths season={season} channel={channel} aside={aside}>{seasonDetails}</RegistrationPaths>
          : <>
            <div>
              <p className="eyebrow">Season details</p>
              <h2>{state === "unavailable" ? "Registration is temporarily unavailable"
                : planned ? "Applications open soon" : "Applications are currently closed"}</h2>
              {state === "unavailable" ? <>
                <p className="section-intro">We can’t check registration status right now. Try again or ask the club a question.</p>
                <a className="button button-primary" href={retryHref}>Check registration status</a>
              </> : planned
                ? <p className="section-intro">Planned opening: {registrationDate(planned)}. Check back here for availability.</p>
                : <p className="section-intro">Check back here for the next opening, or contact the club with questions.</p>}
              {state !== "unavailable" && seasonDetails}
            </div>
            {aside}
          </>}
      </div>
    </section>
    <section className="content-section tone-muted">
      <div className="site-container">
        <div className="section-heading">
          <div><p className="eyebrow">For new athletes offered a spot</p><h2>Preview the paperwork</h2></div>
          <Link className="text-link" href="/forms">All forms<ArrowRight size={17} aria-hidden="true" /></Link>
        </div>
        <FormDownloadList forms={REGISTRATION_DOWNLOADS} />
      </div>
    </section>
    <section className="cta-band"><div className="site-container">
      <div>
        <p className="eyebrow light">After an offer for a new athlete</p>
        <h2>Club staff will explain how to send your packet.</h2>
        <p>Have your application reference ready. Photo consent is optional. This site does not accept documents, signatures, or payments for registration.</p>
      </div>
      <Link className="button button-light" href="/contact">Contact the club</Link>
    </div></section>
  </div>;
}
