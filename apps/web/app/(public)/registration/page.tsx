import type { Metadata } from "next";
import Link from "next/link";
import { PageHero, PublicErrorState } from "@/components/public/ui";
import { getContentBlocks } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";
export const metadata:Metadata={title:"Registration",description:"Find current youth track club registration information."};
export default async function RegistrationPage(){try{const intro=contentByKey(await getContentBlocks()).get(CONTENT_KEYS.registrationIntro);return <><PageHero eyebrow="Registration" title={intro?.title??"Get ready for the season"} summary={intro?.body}/><section className="content-section"><div className="site-container narrow registration-info"><h2>Registration information</h2><p>This page shares current club instructions and season details. Online athlete registration is not available through this website yet.</p>{intro?.ctaText&&intro.ctaUrl&&<Link className="button button-primary" href={intro.ctaUrl}>{intro.ctaText}</Link>}<div className="notice"><h3>Need help?</h3><p>Contact the club for eligibility, season timing, requirements, and next steps.</p><Link className="text-link" href="/contact">Ask a registration question</Link></div></div></section></>;}catch{return <><PageHero title="Registration"/><div className="site-container content-section"><PublicErrorState/></div></>;}}
