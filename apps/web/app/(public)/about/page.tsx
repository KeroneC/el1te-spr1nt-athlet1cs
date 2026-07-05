import type { Metadata } from "next";
import Link from "next/link";
import { ContentSection, PageHero, PublicErrorState } from "@/components/public/ui";
import { getContentBlocks } from "@/lib/public/client";
import { CONTENT_KEYS, contentByKey } from "@/lib/public/content";
export const metadata: Metadata = { title: "About", description: "Learn about our youth track club, mission, and values." };
export default async function AboutPage() { try { const blocks=contentByKey(await getContentBlocks()); const story=blocks.get(CONTENT_KEYS.aboutStory); const values=blocks.get(CONTENT_KEYS.aboutValues); return <><PageHero eyebrow="About the club" title={story?.title ?? "Mission &"} accent={story?.title ? undefined : "Purpose"} summary={story?.summary} />{story && <ContentSection block={story} />}{values && <ContentSection block={values} tone="muted" />}<section className="cta-band"><div className="site-container"><h2>Come build the next chapter with us.</h2><Link className="button button-light" href="/registration">Registration information</Link></div></section></>; } catch { return <><PageHero title="About" accent="Our Club" /><div className="site-container content-section"><PublicErrorState /></div></>; } }
