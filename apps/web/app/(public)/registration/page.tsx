import type { Metadata } from "next";
import { ArrowRight, Download, Mail } from "lucide-react";
import Link from "next/link";
import { PageHero, PublicErrorState } from "@/components/public/ui";
import { getContentBlocks } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";
import { FORM_DOWNLOADS, QUICK_STEPS } from "@/lib/public/site";

export const metadata: Metadata = {
  title: "Registration Hub",
  description: "Review registration information, required forms, and next steps for El1te Spr1nt Athlet1cs."
};

export default async function RegistrationPage() {
  try {
    const intro = contentByKey(await getContentBlocks()).get(CONTENT_KEYS.registrationIntro);
    return <>
      <PageHero
        eyebrow="Registration Hub"
        title="Registration &"
        accent="Forms"
        summary={intro?.body ?? "Registration is currently handled by club review. Families interested in joining El1te Spr1nt Athlet1cs should review the required forms and contact the club with questions before submitting completed paperwork."}
      />
      <section className="content-section">
        <div className="site-container registration-layout">
          <div>
            <p className="eyebrow">How it works</p>
            <h2>Parent-friendly steps without collecting sensitive data online</h2>
            <p className="section-intro">The registration packet includes athlete, guardian, emergency contact, proof-of-age, insurance, and medical information. For now, those details stay out of this website and are reviewed directly by club staff.</p>
            <div className="step-grid">
              {QUICK_STEPS.map((step, index) => <article key={step.title} className="step-card"><span>{index + 1}</span><h3>{step.title}</h3><p>{step.body}</p></article>)}
            </div>
          </div>
          <aside className="registration-aside">
            <h2>Before submitting paperwork</h2>
            <ul>
              <li>Review the required forms.</li>
              <li>Ask questions about eligibility or timing.</li>
              <li>Do not email sensitive medical details unless club staff has asked for them through the appropriate process.</li>
            </ul>
            <Link className="button button-primary" href="/contact">Ask a registration question<Mail size={17} aria-hidden="true" /></Link>
            <Link className="text-link registration-faq-link" href="/faqs">Read frequently asked questions<ArrowRight size={17} aria-hidden="true" /></Link>
          </aside>
        </div>
      </section>
      <section className="content-section tone-muted">
        <div className="site-container">
          <div className="section-heading"><div><p className="eyebrow">Required paperwork</p><h2>Download the forms</h2></div><Link className="text-link" href="/forms">All forms<ArrowRight size={17} aria-hidden="true" /></Link></div>
          <div className="download-list">
            {FORM_DOWNLOADS.slice(0, 3).map(({ name, description, href, fileType, Icon }) => <article className="download-row" key={name}><div className="download-icon"><Icon aria-hidden="true" /></div><div><h3>{name}</h3><p>{description}</p><span>{fileType}</span></div><a className="button button-secondary" href={href} download>Download<Download size={17} aria-hidden="true" /></a></article>)}
          </div>
        </div>
      </section>
      <section className="cta-band"><div className="site-container"><div><p className="eyebrow light">No online registration yet</p><h2>Completed forms are reviewed by club staff.</h2><p>This site shares information and downloads only. It does not store athlete registrations, waivers, payments, or private documents.</p></div><Link className="button button-light" href="/contact">Contact the club</Link></div></section>
    </>;
  } catch {
    return <><PageHero title="Registration" /><div className="site-container content-section"><PublicErrorState /></div></>;
  }
}
