import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";
import type { ContentBlock } from "@/lib/public/types";

export function SupportStaffSection({ block }: { block: ContentBlock | null }) {
  const photo = block?.imageUrl?.trim();
  if (!block || !photo || !block.title.trim() || !block.body.trim()) return null;

  return <section className="content-section tone-muted" aria-labelledby="support-staff-heading">
    <div className="site-container grid items-center gap-8 md:grid-cols-2 lg:gap-14">
      <div className="min-w-0 break-words">
        <p className="eyebrow">Behind the team</p>
        <h2 id="support-staff-heading">{block.title}</h2>
        <p className="mt-4 whitespace-pre-line leading-relaxed">{block.body}</p>
      </div>
      <div className="min-w-0 border-b-4 border-track-red bg-white">
        <ResponsiveMediaImage src={photo} alt={block.summary?.trim() || "El1te Spr1nt Athlet1cs support staff group photo"}
          className="aspect-[3/2] w-full object-contain"
          sizes="(max-width: 767px) calc(100vw - 2rem), (max-width: 1280px) 46vw, 570px"
          width={1200} height={800} />
      </div>
    </div>
  </section>;
}
