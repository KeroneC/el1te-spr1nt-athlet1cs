"use client";

import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import type { MouseEvent, ReactNode } from "react";
import { ArrowLeft, ArrowUpRight } from "lucide-react";
import type { PublicRegistrationSeason, RegistrationChannel } from "@/lib/registration";
import { RegistrationIntake } from "./registration-intake";
import styles from "./registration.module.css";

type Path = "apply" | "returning";

function subscribe(onChange: () => void) {
  window.addEventListener("hashchange", onChange);
  return () => window.removeEventListener("hashchange", onChange);
}

function pathFromHash(): Path | null {
  if (window.location.hash === "#apply") return "apply";
  if (window.location.hash === "#returning") return "returning";
  return null;
}

function focusHeading(heading: HTMLHeadingElement | null) {
  heading?.focus({ preventScroll: true });
  heading?.scrollIntoView({ block: "start" });
}

export function RegistrationPaths({ season, channel, intro }: {
  season: PublicRegistrationSeason; channel: RegistrationChannel; intro: ReactNode;
}) {
  const selected = useSyncExternalStore(subscribe, pathFromHash, () => null);
  const previous = useRef<Path | null>(null);
  const applicationHeading = useRef<HTMLHeadingElement>(null);
  const returnHeading = useRef<HTMLHeadingElement>(null);
  const applicationChoice = useRef<HTMLAnchorElement>(null);
  const returnChoice = useRef<HTMLAnchorElement>(null);
  const statusHeading = useRef<HTMLHeadingElement>(null);
  const [intakeClosed, setIntakeClosed] = useState(false);

  useEffect(() => {
    if (selected) {
      focusHeading(selected === "apply" ? applicationHeading.current : returnHeading.current);
    } else if (previous.current && ["", "#registration-paths"].includes(window.location.hash)) {
      ((previous.current === "apply" ? applicationChoice.current : returnChoice.current) ?? statusHeading.current)?.focus();
    }
    previous.current = selected;
  }, [selected]);

  function refocusSelected(event: MouseEvent<HTMLAnchorElement>, path: Path) {
    if (selected !== path || event.metaKey || event.ctrlKey || event.altKey || event.shiftKey) return;
    event.preventDefault();
    focusHeading(path === "apply" ? applicationHeading.current : returnHeading.current);
  }

  return <>
    <section className={styles.hero} aria-label="Registration">
      <div className={`site-container ${styles.heroContent}`}>
        {intro}
        <h2 id="registration-paths" ref={statusHeading} tabIndex={-1} className={styles.statusPill}
          data-status={intakeClosed ? "closed" : "open"}>{intakeClosed ? "Applications are currently closed" : "Applications are open"}</h2>
        {!intakeClosed && <nav className={styles.paths} aria-label="Registration paths">
        <a ref={applicationChoice} className={styles.pathCard} href="#apply" aria-label="Apply for a spot"
          aria-current={selected === "apply" ? "location" : undefined}
          onClick={event => refocusSelected(event, "apply")}>
          <span className={styles.pathArt} aria-hidden="true"><span className={styles.trackArt}><span>01</span></span></span>
          <span className={styles.pathLabel}><span className={styles.pathOverline}>New to El1te</span><strong>Apply for a spot</strong><span className={styles.pathDescription}>For athletes joining the club for the first time.</span></span>
          <ArrowUpRight className={styles.pathArrow} size={21} aria-hidden="true" />
        </a>
        <a ref={returnChoice} className={styles.pathCard} href="#returning" aria-label="Returning athlete? Check in"
          aria-current={selected === "returning" ? "location" : undefined}
          onClick={event => refocusSelected(event, "returning")}>
          <span className={styles.pathArt} aria-hidden="true">
            {/* The existing public, pre-sized WebPs avoid three image requests or runtime transforms. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/team/meet-community-480.webp"
              srcSet="/images/team/meet-community-480.webp 480w, /images/team/meet-community-960.webp 960w"
              sizes="(max-width: 600px) 104px, (max-width: 1240px) 48vw, 570px"
              alt="" width="480" height="320" decoding="async" />
          </span>
          <span className={styles.pathLabel}><span className={styles.pathOverline}>Returning athlete</span><strong>Check in</strong><span className={styles.pathDescription}>Two fields. No paperwork.</span></span>
          <ArrowUpRight className={styles.pathArrow} size={21} aria-hidden="true" />
        </a>
        </nav>}
      </div>
    </section>
    <section id="apply" className={`site-container ${styles.intakeSection}`} hidden={selected !== "apply"}>
      <header className={styles.intakeHeading}>
        {!intakeClosed && <a className={styles.back} href="#registration-paths"><ArrowLeft size={16} aria-hidden="true" />Registration choices</a>}
        <p className={styles.pathOverline}>New to El1te</p>
        <h2 ref={applicationHeading} tabIndex={-1}>Apply for a spot</h2>
        <p>Use one application per athlete. Fields are required unless marked optional.</p>
      </header>
      <RegistrationIntake season={season} channel={channel} kind="Application"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
    <section id="returning" className={`site-container ${styles.intakeSection}`} hidden={selected !== "returning"}>
      <header className={styles.intakeHeading}>
        {!intakeClosed && <a className={styles.back} href="#registration-paths"><ArrowLeft size={16} aria-hidden="true" />Registration choices</a>}
        <p className={styles.pathOverline}>Back for another season</p>
        <h2 ref={returnHeading} tabIndex={-1}>Returning athlete? Check in</h2>
        <p>Tell us your athlete plans to return. No registration paperwork is required.</p>
      </header>
      <RegistrationIntake season={season} channel={channel} kind="Returning"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
  </>;
}
