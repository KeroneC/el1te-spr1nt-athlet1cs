import { getContentBlocks } from "@/lib/public/client";
import { contentByKey } from "@/lib/public/content";
import { PageHero, PublicErrorState } from "@/components/public/ui";
import { PolicyContent } from "@/components/public/policy-content";

export async function PolicyPage({ contentKey, fallbackTitle }: { contentKey: string; fallbackTitle: string }) {
  try {
    const block = contentByKey(await getContentBlocks()).get(contentKey);
    if (!block) return <><PageHero title={fallbackTitle} /><section className="content-section"><div className="site-container narrow"><p>This policy is temporarily unavailable. Contact the club if you need assistance.</p></div></section></>;
    return <><PageHero eyebrow="Site policy" title={block.title} summary="Plain-language information about how this website operates." /><section className="content-section"><PolicyContent body={block.body} /></section></>;
  } catch (error) {
    return <><PageHero title={fallbackTitle} /><section className="content-section"><div className="site-container"><PublicErrorState error={error} /></div></section></>;
  }
}
