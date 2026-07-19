import type { Metadata } from "next";
import { FileText } from "lucide-react";
import { ScholarshipBloom } from "@/components/public/scholarship-bloom";
import styles from "@/components/public/scholarship-bloom.module.css";
import { FORM_DOWNLOADS } from "@/lib/public/site";
import { SCHOLARSHIP_COPY } from "@/lib/public/scholarship";

const scholarshipForm = FORM_DOWNLOADS.find((form) => form.name.includes("Scholarship"));

export const metadata: Metadata = {
  title: "BVN Memorial Scholarship",
  description: "Learn about the Beulah Veronica Newton Memorial Scholarship for El1te Spr1nt Athlet1cs athletes."
};

export default function ScholarshipPage() {
  return <>
    <ScholarshipBloom criteriaHref={scholarshipForm?.href ?? "/forms/scholarship-form.pdf"} />

    <section className={styles.storySection} aria-labelledby="miss-beulah-heading">
      <div className={`site-container ${styles.storyGrid}`}>
        <div className={styles.storyHeading}>
          <p className={styles.sectionLabel}>Her legacy</p>
          <h2 id="miss-beulah-heading">Known and loved as Miss Beulah</h2>
        </div>
        <div className={styles.storyCopy}>
          <span className={styles.storyMark} aria-hidden="true">“</span>
          <p>{SCHOLARSHIP_COPY.legacy}</p>
        </div>
      </div>
    </section>

    <section className={styles.criteriaSection} aria-labelledby="criteria-heading">
      <div className={`site-container ${styles.criteriaCard}`}>
        <div className={styles.criteriaCopy}>
          <p className={styles.sectionLabel}>Carrying her belief forward</p>
          <h2 id="criteria-heading">The BVN Memorial Scholarship</h2>
          <p>{SCHOLARSHIP_COPY.award}</p>
        </div>
        <div className={styles.criteriaAction}>
          <p className={styles.criteriaMeta}>PDF · Application and criteria</p>
          <a className={`button button-primary ${styles.criteriaButton}`} href={scholarshipForm?.href ?? "/forms/scholarship-form.pdf"} target="_blank" rel="noreferrer"><span>View scholarship criteria</span><FileText size={17} aria-hidden="true" /></a>
        </div>
      </div>
    </section>
  </>;
}
