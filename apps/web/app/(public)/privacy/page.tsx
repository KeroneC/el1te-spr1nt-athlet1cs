import type { Metadata } from "next";
import Link from "next/link";
import { PolicyPage } from "@/components/public/policy-page";
import { CONTENT_KEYS } from "@/lib/public/content";
export const metadata: Metadata = { title: "Privacy" };
export default function Page() {
  return <>
    <PolicyPage contentKey={CONTENT_KEYS.privacyPolicy} fallbackTitle="Privacy" />
    <section id="registration-privacy" className="content-section tone-muted scroll-mt-28" aria-labelledby="registration-privacy-title">
      <div className="site-container narrow space-y-4">
        <p className="eyebrow">For parents and guardians</p>
        <h2 id="registration-privacy-title">Athlete applications and check-ins</h2>
        <p>A parent or guardian should submit the application. For a new athlete, we ask for their legal name, date of birth, gender, school grade and district, and track background. We also ask for household and guardian contact information and an emergency contact’s name, phone number and relationship. Gender is required; second guardian contact is optional. Returning check-ins ask only for a guardian email and the athlete’s first name.</p>
        <p>Authorized club staff use these details to check eligibility, review the application and follow up. Only share contact information that you have permission to provide. We don’t use applicant details for advertising or send them to analytics services. Don’t include proof-of-age documents, medical details, insurance information, or signatures in the online form. Staff will tell you how to send required paperwork after an offer.</p>
        <p>Club staff can close out identifiable online applications and their review history starting 90 days after the season ends. Closeout isn’t available until the season end date is recorded. Non-identifying totals are kept. Separately received paperwork is handled under the club’s own records process. To ask about these details or request a correction, <Link className="text-link" href="/contact">contact the club</Link>.</p>
      </div>
    </section>
  </>;
}
