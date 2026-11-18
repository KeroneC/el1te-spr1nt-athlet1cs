import type { Metadata } from "next";
import Link from "next/link";
import { PageHero, PublicErrorState } from "@/components/public/ui";
import { getFaqs } from "@/lib/public/client";
import { registrationAgeLabel } from "@/lib/registration";

export const metadata: Metadata = { title: "FAQs", description: "Answers to common questions about programs, practices, registration, and support." };
const registrationFaqs = [
  { question: "How do I register my child?", answer: "New athletes apply on the Registration page. If the club offers a spot, staff will explain how to send completed paperwork. Returning athletes only need to check in online; no paperwork is required." },
  { question: "Will I get a confirmation email?", answer: "No automatic confirmation email is sent. Save the reference shown on screen. The club will follow up separately by email. An application or check-in does not reserve a spot." },
  { question: "What ages can join?", answer: `Athletes must be ${registrationAgeLabel.toLowerCase()} on the first practice date. Registration lists the date and practice details.` }
];

export default async function FaqPage() {
  const result = await getFaqs().then(faqs => ({ faqs, error: null }), (error: unknown) => ({ faqs: [], error }));
  const questions = new Set(registrationFaqs.map(item => item.question.toLowerCase()));
  const groups = Map.groupBy(result.faqs.filter(item => !questions.has(item.question.trim().toLowerCase())), faq => faq.category);
  return <>
      <PageHero eyebrow="Frequently asked questions" title="Helpful answers for families" summary="Find quick information about joining, practicing, and supporting the club." />
      <section className="content-section"><div className="site-container faq-layout">
        <section><h2>Registration</h2><div className="faq-list">
          {registrationFaqs.map(item => <details key={item.question}><summary>{item.question}</summary><p>{item.answer} <Link className="text-link" href="/registration">Registration page</Link></p></details>)}
          {groups.get("Registration")?.map(item => <details key={item.question}><summary>{item.question}</summary><p>{item.answer}</p></details>)}
        </div></section>
        {[...groups].filter(([category]) => category !== "Registration").map(([category, items]) =>
          <section key={category}><h2>{category}</h2><div className="faq-list">{items.map(item =>
            <details key={item.question}><summary>{item.question}</summary><p>{item.answer}</p></details>)}</div></section>)}
        {result.error != null && <PublicErrorState error={result.error} />}
        <div className="faq-contact"><h2>Still have a question?</h2><p>Send the club a message and choose the inquiry type that best fits.</p><Link className="button button-primary" href="/contact">Contact us</Link></div>
      </div></section>
  </>;
}
