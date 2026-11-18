import { ArrowDownRight } from "lucide-react";
import type { ReactNode } from "react";
import styles from "./starting-line.module.css";

/** Original lane illustration. Vector marks stay crisp without another hero image request. */
function TrackArtwork() {
  return <svg className={styles.trackDrawing} viewBox="0 0 880 540" preserveAspectRatio="xMidYMid slice" fill="none" focusable="false">
    <defs>
      <linearGradient id="lane-surface" x1="0" y1="540" x2="650" y2="0" gradientUnits="userSpaceOnUse">
        <stop stopColor="#8e1920" /><stop offset=".57" stopColor="#d72e2d" /><stop offset="1" stopColor="#f15237" />
      </linearGradient>
      <pattern id="lane-grit" width="9" height="9" patternUnits="userSpaceOnUse">
        <path d="m1 2 1-.4m3 4 .9-.3m1.9-3 .4-.6" stroke="#631512" strokeOpacity=".34" strokeWidth="1.15" />
        <circle cx="2" cy="7" r=".65" fill="#ffe7d6" opacity=".43" />
      </pattern>
    </defs>
    <rect width="880" height="540" fill="url(#lane-surface)" />
    <g stroke="#ffe7c9" strokeWidth="3.5" opacity=".8">
      {[-176, -98, -20, 58, 136, 214, 292, 370, 448].map(y =>
        <path key={y} transform={`translate(0 ${y})`} d="M-160 238H36C279 238 356 12 577 12H1010" />)}
    </g>
    <g stroke="#661823" strokeWidth="3.5" opacity=".62" transform="rotate(-27 385 158)">
      <path d="M336 89v27m18 40v27m19 46v27m20 45v27m20 47v27" />
    </g>
    <rect width="880" height="540" fill="url(#lane-grit)" />
  </svg>;
}

/** Mounted once per entrance. Photo and line work are decorative; all labels live in real text. */
export function EntranceArt({ closed = false }: { closed?: boolean }) {
  return <div aria-hidden="true" className={styles.art}>
    {!closed && <div className={styles.photo}>
      {/* The existing public derivatives are served directly; no runtime image transformer. */}
      <picture>
        <source media="(max-width: 680px)"
          srcSet="/images/team/meet-community-480.webp 480w, /images/team/meet-community-960.webp 960w" sizes="100vw" />
        <img src="/images/team/meet-community-960.webp"
        srcSet="/images/team/meet-community-480.webp 480w, /images/team/meet-community-960.webp 960w, /images/team/meet-community-1600.webp 1600w"
        sizes="63vw" alt="" width="960" height="640" decoding="async" />
      </picture>
    </div>}
    <div className={styles.track}><div className={styles.trackSurface}><TrackArtwork /></div></div>
    {!closed && <>
      <svg className={styles.seam} viewBox="0 0 100 100" preserveAspectRatio="none" focusable="false">
        <path className={styles.desktopCut} d="M57 -1 43 101" pathLength="1" />
        <path className={styles.mobileCut} d="M-1 55 101 45" pathLength="1" />
      </svg>
      <span className={styles.paintNumber}>01</span>
    </>}
  </div>;
}

export function EntranceHeader({ open, children }: { open?: boolean; children?: ReactNode }) {
  return <header className={styles.mast}>
    <div className={styles.mastInner}>
      <div>
        <p className={styles.eyebrow}><i aria-hidden="true" />Family registration</p>
        <h1>Find your <em>starting lane</em></h1>
      </div>
      <div className={styles.meta}>
        {open && <h2 className={styles.open}><i aria-hidden="true" />Applications are open</h2>}
        <div className={styles.detailsRow}><span>Ages 6 and up</span>
          <a href="#registration-season-heading">View season details <ArrowDownRight size={14} aria-hidden="true" /></a>
        </div>
        {children}
      </div>
    </div>
  </header>;
}
