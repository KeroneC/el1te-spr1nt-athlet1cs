"use client";

import type { CSSProperties } from "react";
import { useState } from "react";
import Image from "next/image";
import { Flower2, RotateCcw, Sparkles } from "lucide-react";
import { SCHOLARSHIP_COPY } from "@/lib/public/scholarship";
import styles from "./scholarship-bloom.module.css";

type BloomVariant = "abstract" | "literal";
type PetalStyle = CSSProperties & { "--rotation": string; "--delay": string };

const abstractPetals = Array.from({ length: 12 }, (_, index) => ({
  id: `abstract-${index}`,
  rotation: `${index * 30}deg`,
  delay: `${index * 55}ms`
}));

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
  const [variant, setVariant] = useState<BloomVariant>("abstract");
  const [replay, setReplay] = useState(0);

  function selectVariant(nextVariant: BloomVariant) {
    setVariant(nextVariant);
    setReplay((value) => value + 1);
  }

  return <section className={styles.hero} aria-labelledby="scholarship-title">
    <div className={`site-container ${styles.heroInner}`}>
      <div className={styles.previewControls}>
        <div>
          <span className={styles.previewLabel}>Bloom preview</span>
          <span className={styles.previewHint}>Temporary comparison control</span>
        </div>
        <div className={styles.previewActions} role="group" aria-label="Choose a scholarship bloom animation">
          <button type="button" className={variant === "abstract" ? styles.previewButtonActive : styles.previewButton} aria-pressed={variant === "abstract"} onClick={() => selectVariant("abstract")}><Sparkles size={16} aria-hidden="true" />Abstract</button>
          <button type="button" className={variant === "literal" ? styles.previewButtonActive : styles.previewButton} aria-pressed={variant === "literal"} onClick={() => selectVariant("literal")}><Flower2 size={16} aria-hidden="true" />Literal</button>
          <button type="button" className={styles.replayButton} onClick={() => setReplay((value) => value + 1)}><RotateCcw size={16} aria-hidden="true" />Replay</button>
        </div>
      </div>

      <div key={`${variant}-${replay}`} className={styles.bloomStage}>
        <div className={styles.bloomField} aria-hidden="true">
          {variant === "abstract" ? <AbstractBloom /> : <LiteralBloom />}
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
      <p className="sr-only" aria-live="polite">Previewing the {variant} bloom animation.</p>
    </div>
  </section>;
}

function AbstractBloom() {
  return <div className={styles.abstractBloom}>
    {abstractPetals.map((petal) => <span key={petal.id} className={styles.abstractPetal} style={{ "--rotation": petal.rotation, "--delay": petal.delay } as PetalStyle} />)}
    <span className={styles.abstractRing} />
  </div>;
}

function LiteralBloom() {
  return <div className={styles.literalBloom}>
    <div className={styles.literalOuter}>
      {literalOuterPetals.map((petal) => <span key={petal.id} className={styles.literalOuterPetal} style={{ "--rotation": petal.rotation, "--delay": petal.delay } as PetalStyle} />)}
    </div>
    <div className={styles.literalInner}>
      {literalInnerPetals.map((petal) => <span key={petal.id} className={styles.literalInnerPetal} style={{ "--rotation": petal.rotation, "--delay": petal.delay } as PetalStyle} />)}
    </div>
    <span className={styles.literalCore} />
  </div>;
}
