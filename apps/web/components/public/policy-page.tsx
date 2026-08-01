import { getContentBlocks } from "@/lib/public/client";
import { contentByKey } from "@/lib/public/content";
import { PageHero, PublicErrorState } from "@/components/public/ui";

export async function PolicyPage({ contentKey, fallbackTitle }: { contentKey: string; fallbackTitle: string }) {
  try {
    const block = contentByKey(await getContentBlocks()).get(contentKey);
    if (!block) return <><PageHero title={fallbackTitle} /><section className="content-section"><div className="site-container narrow"><p>This policy draft is being prepared for organizational review.</p></div></section></>;
    return <><PageHero eyebrow="Launch policy draft" title={block.title} summary="Plain-language information about how this website operates." /><section className="content-section"><div className="site-container narrow article-body"><p>{block.body}</p><p><strong>Review status:</strong> Draft for club approval before production launch.</p></div></section></>;
  } catch (error) {
    return <><PageHero title={fallbackTitle} /><section className="content-section"><div className="site-container"><PublicErrorState error={error} /></div></section></>;
  }
}
