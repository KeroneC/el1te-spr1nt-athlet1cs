import type { CSSProperties } from "react";
import Image from "next/image";
import { SCHOLARSHIP_COPY } from "@/lib/public/scholarship";
import styles from "./scholarship-bloom.module.css";

type PetalStyle = CSSProperties & { "--rotation": string; "--delay": string };

const literalOuterPetals = Array.from({ length: 10 }, (_, index) => ({
  id: `literal-outer-${index}`,
  rotation: `${index * 36}deg`,
  delay: `${index * 65}ms`
}));

const literalInnerPetals = Array.from({ length: 7 }, (_, index) => ({
  id: `literal-inner-${index}`,
  rotation: `${index * (360 / 7)}deg`,
  delay: `${280 + index * 55}ms`
}));

export function ScholarshipBloom({ criteriaHref }: { criteriaHref: string }) {
  return <section className={styles.hero} aria-labelledby="scholarship-title">
    <div className={`site-container ${styles.heroInner}`}>
      <div className={styles.bloomStage}>
        <div className={styles.bloomField} aria-hidden="true">
          <LiteralBloom />
        </div>
        <a className={styles.logoLink} href={criteriaHref} target="_blank" rel="noreferrer" aria-label="Open the BVN Memorial Scholarship criteria PDF in a new tab">
          <Image src="/images/scholarship/bvn-memorial-scholarship-transparent.png" alt="BVN — Beulah Veronica Newton Memorial Scholarship" width={1200} height={700} priority sizes="(max-width: 48rem) 94vw, 42rem" />
        </a>
      </div>

      <div className={styles.heroCopy}>
        <p className={styles.eyebrow}>A legacy of encouragement</p>
        <h1 id="scholarship-title"><span>In honor of</span> Beulah Veronica Newton</h1>
        <p>{SCHOLARSHIP_COPY.introduction}</p>
      </div>
    </div>
  </section>;
}

function LiteralBloom() {
  return <div className={styles.literalBloom}>
    <div className={styles.literalOuter}>
      {literalOuterPetals.map((petal) => <span key={petal.id} className={styles.literalOuterPetal} style={{ "--rotation": petal.rotation, "--delay": petal.delay } as PetalStyle} />)}
    </div>
    <div className={styles.literalInner}>
      {literalInnerPetals.map((petal) => <span key={petal.id} className={styles.literalInnerPetal} style={{ "--rotation": petal.rotation, "--delay": petal.delay } as PetalStyle} />)}
    </div>
    <span className={styles.literalCenterShade} />
  </div>;
}
