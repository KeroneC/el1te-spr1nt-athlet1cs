import type { Metadata } from "next";
import { ArrowRight, Download, Mail } from "lucide-react";
import Link from "next/link";
import { PageHero, PublicErrorState } from "@/components/public/ui";
import { FORM_DOWNLOADS } from "@/lib/public/site";
import { getRegistrationSeason } from "@/lib/public/registration";
import { registrationChannel, registrationDate, registrationOpeningDate } from "@/lib/registration";
import { RegistrationIntake, RegistrationVisit } from "@/components/public/registration-intake";

export const metadata: Metadata = {
  title: "Registration Hub",
  description: "Review registration information, required forms, and next steps for El1te Spr1nt Athlet1cs.",
  referrer: "no-referrer"
};

export default async function RegistrationPage({ searchParams }: { searchParams: Promise<{ src?: string }> }) {
  try {
    const [season, query] = await Promise.all([getRegistrationSeason(), searchParams]);
    const channel = registrationChannel(query.src);
    return <>
      <PageHero
        eyebrow="Registration Hub"
        title={season?.title ?? "Registration"}
        summary="Apply for a spot with El1te Spr1nt Athlet1cs or let us know your athlete plans to return."
      />
      {season && <RegistrationVisit year={season.year} channel={channel} />}
      <section className="content-section">
        <div className="site-container registration-layout">
          <div>
            <p className="eyebrow">Season details</p>
            <h2>{season?.isOpen ? "Applications are open" : "Applications are currently closed"}</h2>
            {season?.isOpen
              ? <div className="button-row mt-4"><a className="button button-primary" href="#apply">Apply for a spot</a>
                <a className="button button-secondary" href="#returning">Returning athlete? Check in</a></div>
              : <p className="section-intro">Planned opening: {registrationDate(season?.openingDate ?? registrationOpeningDate)}. Check back here for availability.</p>}
            {season?.title && <dl className="mt-5 grid gap-4 sm:grid-cols-2">
              {[
                ["Eligible age groups", season.ageGroups.join(", ")], ["Practice location", season.practiceLocation],
                ["Practice schedule", season.practiceSchedule], ["Fees", season.fees], ["When you’ll hear back", season.replyWindow]
              ].map(([label, value]) => <div key={label}><dt className="font-bold">{label}</dt><dd className="whitespace-pre-line break-words">{value}</dd></div>)}
            </dl>}
            <p className="mt-5">Staff review applications individually. An application or returning check-in does not reserve a spot.</p>
          </div>
          <aside className="registration-aside">
            <h2>What happens next?</h2>
            <ul>
              <li>Save the reference shown after you submit.</li>
              <li>Wait for the club to follow up by email.</li>
              <li>New athletes: send paperwork only after an offer. Returning athletes: check in online; no paperwork is required.</li>
            </ul>
            <Link className="button button-primary" href="/contact">Ask a registration question<Mail size={17} aria-hidden="true" /></Link>
            <Link className="text-link registration-faq-link" href="/faqs">Read frequently asked questions<ArrowRight size={17} aria-hidden="true" /></Link>
          </aside>
        </div>
      </section>
      {season?.isOpen && <section className="content-section tone-muted">
        <div className="site-container grid gap-8 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
          <div id="apply" className="min-w-0 scroll-mt-28">
            <h2>Apply for a spot</h2><p className="mb-4">Use one form per athlete. Staff will follow up if a club affiliation needs review.</p>
            <RegistrationIntake season={season} channel={channel} kind="Application" />
          </div>
          <div id="returning" className="min-w-0 scroll-mt-28">
            <h2>Returning athlete? Check in</h2><p className="mb-4">Tell us your athlete plans to return. No registration paperwork is required.</p>
            <RegistrationIntake season={season} channel={channel} kind="Returning" />
          </div>
        </div>
      </section>}
      <section className="content-section tone-muted">
        <div className="site-container">
          <div className="section-heading"><div><p className="eyebrow">For new athletes offered a spot</p><h2>Preview the paperwork</h2></div><Link className="text-link" href="/forms">All forms<ArrowRight size={17} aria-hidden="true" /></Link></div>
          <div className="download-list">
            {FORM_DOWNLOADS.slice(0, 3).map(({ name, description, href, fileType, Icon }) => <article className="download-row" key={name}><div className="download-icon"><Icon aria-hidden="true" /></div><div><h3>{name}</h3><p>{description}</p><span>{fileType}</span></div><a className="button button-secondary" href={href} download>Download<Download size={17} aria-hidden="true" /></a></article>)}
          </div>
        </div>
      </section>
      <section className="cta-band"><div className="site-container"><div><p className="eyebrow light">After an offer for a new athlete</p><h2>Club staff will explain how to send your packet.</h2><p>Have your application reference ready. Photo consent is optional. This site does not accept documents, signatures, or payments for registration.</p></div><Link className="button button-light" href="/contact">Contact the club</Link></div></section>
    </>;
  } catch (error) {
    return <><PageHero title="Registration" /><div className="site-container content-section"><PublicErrorState error={error} /></div></>;
  }
}
