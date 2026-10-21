import type { Metadata } from "next";
import { Download, ExternalLink, Mail } from "lucide-react";
import Link from "next/link";
import { PageHero } from "@/components/public/ui";
import { FORM_DOWNLOADS } from "@/lib/public/site";

export const metadata: Metadata = {
  title: "Forms",
  description: "Download registration, waiver, and photo consent forms, and view BVN Memorial Scholarship information for El1te Spr1nt Athlet1cs."
};

export default function FormsPage() {
  return <>
    <PageHero
      eyebrow="Family paperwork"
      title="Forms &"
      accent="Downloads"
      summary="Download the current registration packet and related forms before contacting the club with questions or submitting paperwork for review."
    />
    <section className="content-section">
      <div className="site-container">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Downloads</p>
            <h2>Required and helpful forms</h2>
          </div>
          <Link className="text-link" href="/registration">Registration Hub</Link>
        </div>
        <div className="download-list">
          {FORM_DOWNLOADS.map(({ name, description, audience, href, fileType, actionLabel, isDownload, Icon }) => (
            <article className="download-row" key={name}>
              <div className="download-icon"><Icon aria-hidden="true" /></div>
              <div>
                <h3>{name}</h3>
                <p>{description}</p>
                <p className="form-audience"><strong>For:</strong> {audience}</p>
                <span>{fileType}</span>
              </div>
              <a
                className="button button-secondary"
                href={href}
                download={isDownload || undefined}
                target={isDownload ? undefined : "_blank"}
                rel={isDownload ? undefined : "noreferrer"}
              >
                {actionLabel}
                {isDownload ? <Download size={17} aria-hidden="true" /> : <ExternalLink size={17} aria-hidden="true" />}
              </a>
            </article>
          ))}
        </div>
      </div>
    </section>
    <section className="cta-band">
      <div className="site-container">
        <div>
          <p className="eyebrow light">Need help?</p>
          <h2>Ask before submitting sensitive paperwork.</h2>
          <p>The website provides downloads only. A club administrator can help with questions about timing, eligibility, and form requirements.</p>
        </div>
        <Link className="button button-light" href="/contact">Contact the club<Mail size={17} aria-hidden="true" /></Link>
      </div>
    </section>
  </>;
}
