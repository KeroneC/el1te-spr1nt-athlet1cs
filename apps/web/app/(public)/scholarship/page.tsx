import type { Metadata } from "next";
import { Download, Mail } from "lucide-react";
import Link from "next/link";
import { PageHero } from "@/components/public/ui";
import { AwardIcon, FORM_DOWNLOADS } from "@/lib/public/site";

const scholarshipForm = FORM_DOWNLOADS.find((form) => form.name.includes("Scholarship"));

export const metadata: Metadata = {
  title: "BVN Memorial Scholarship",
  description: "Learn about the Beulah Veronica Newton Memorial Scholarship for El1te Spr1nt Athlet1cs athletes."
};

export default function ScholarshipPage() {
  return <>
    <PageHero
      eyebrow="BVN Memorial Scholarship"
      title="Honoring Beulah"
      accent="Veronica Newton"
      summary="This scholarship honors Beulah Veronica Newton and supports an El1te Spr1nt Athlet1cs athlete continuing to grow on and off the track."
    />
    <section className="content-section">
      <div className="site-container story-layout">
        <div>
          <p className="eyebrow">Community legacy</p>
          <h2>A respectful next step for eligible athletes</h2>
          <div className="prose">
            <p>This scholarship is in honor of Beulah Veronica Newton, remembered by many as Miss Beulah. The award is intended to recognize an El1te Spr1nt Athlet1cs athlete in their junior or senior year who meets the selected criteria.</p>
            <p>Specific criteria and application details should be reviewed from the official scholarship form. Families with questions can contact the club before submitting materials.</p>
          </div>
        </div>
        <aside className="feature-panel">
          <AwardIcon aria-hidden="true" />
          <h2>Application materials</h2>
          <p>Use the downloadable scholarship form for the current application details. Do not submit private information through this public website.</p>
          {scholarshipForm && <a className="button button-primary" href={scholarshipForm.href} download>Download form<Download size={17} aria-hidden="true" /></a>}
        </aside>
      </div>
    </section>
    <section className="cta-band">
      <div className="site-container">
        <div>
          <p className="eyebrow light">Questions</p>
          <h2>Confirm details with the club.</h2>
          <p>Scholarship timing and requirements can be reviewed with club staff before families submit application materials.</p>
        </div>
        <Link className="button button-light" href="/contact">Contact us<Mail size={17} aria-hidden="true" /></Link>
      </div>
    </section>
  </>;
}
