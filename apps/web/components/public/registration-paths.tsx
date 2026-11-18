"use client";

import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import { ArrowLeft, ArrowUpRight } from "lucide-react";
import type { PublicRegistrationSeason, RegistrationChannel } from "@/lib/registration";
import { RegistrationIntake } from "./registration-intake";
import { EntranceArt, EntranceHeader } from "./registration-entrance";
import styles from "./starting-line.module.css";

type Path = "apply" | "returning";

function subscribe(onChange: () => void) {
  window.addEventListener("hashchange", onChange);
  window.addEventListener("popstate", onChange);
  return () => {
    window.removeEventListener("hashchange", onChange);
    window.removeEventListener("popstate", onChange);
  };
}

function pathFromHash(): Path | null {
  if (/^#apply(?:\/|$)/.test(window.location.hash)) return "apply";
  return window.location.hash === "#returning" ? "returning" : null;
}

export function RegistrationPaths({ season, channel, archivePreview = false }: {
  season: PublicRegistrationSeason; channel: RegistrationChannel; archivePreview?: boolean;
}) {
  const selected = useSyncExternalStore(subscribe, pathFromHash, () => null);
  const previous = useRef<Path | null>(null);
  const application = useRef<HTMLElement>(null);
  const returnHeading = useRef<HTMLHeadingElement>(null);
  const applicationChoice = useRef<HTMLAnchorElement>(null);
  const returnChoice = useRef<HTMLAnchorElement>(null);
  const closedHeading = useRef<HTMLHeadingElement>(null);
  const [intakeClosed, setIntakeClosed] = useState(false);
  const [sweep, setSweep] = useState<Path | null>(null);

  useEffect(() => {
    let frame: number | undefined;
    if (selected) {
      // Native fragment navigation finishes after React's effects and can otherwise
      // take focus back from the newly revealed heading, especially when pressing Enter.
      frame = window.requestAnimationFrame(() => {
        const heading = selected === "apply" ? application.current?.querySelector<HTMLElement>("[data-step-heading], [role='status'][tabindex]") : returnHeading.current;
        heading?.focus({ preventScroll: true });
        const panel = (selected === "apply" ? application.current : returnHeading.current?.closest("section"));
        if (panel && panel.getBoundingClientRect().top < 0) panel.scrollIntoView({ block: "start", behavior: "instant" });
      });
    } else if (previous.current && ["", "#registration-paths"].includes(window.location.hash)) {
      const target = (previous.current === "apply" ? applicationChoice.current : returnChoice.current) ?? closedHeading.current;
      frame = window.requestAnimationFrame(() => target?.focus());
    }
    previous.current = selected;
    return () => { if (frame !== undefined) window.cancelAnimationFrame(frame); };
  }, [selected]);

  function formTop(path: Path) {
    return <>
      {sweep === path && <span className={styles.wipe} data-path={path} aria-hidden="true"
        onAnimationEnd={() => setSweep(null)} />}
      <div className={styles.pathTools}>
        <a href="#registration-paths"><ArrowLeft size={16} aria-hidden="true" />Registration choices</a>
        <span>{path === "apply" ? "New athlete" : "Returning athlete"}</span>
      </div>
    </>;
  }

  return <>
    <section id="registration-paths" className={styles.entrance} aria-label="Registration" hidden={selected !== null}>
      <EntranceHeader open={!intakeClosed} />
      {intakeClosed ? <div className={styles.closedScene}>
        <EntranceArt closed />
        <div className={styles.closedCopy}><h2 ref={closedHeading} tabIndex={-1}>Applications are currently closed</h2>
          <p>Intake closed during your visit. Check back for the next opening or <a href="/contact">ask the club a question.</a></p></div>
      </div> : <nav className={styles.scene} aria-label="Registration paths">
        <EntranceArt archivePreview={archivePreview} />
        <a ref={applicationChoice} href="#apply" aria-label="Apply for a spot" className={styles.newLane}
          onClick={event => { if (!event.metaKey && !event.ctrlKey && !event.shiftKey && !event.altKey) setSweep("apply"); }}>
          <div className={styles.label}>
            <span className={styles.folio}><b aria-hidden="true" />Lane 01 <span>/</span> New athlete</span>
            <strong>New to <em>El1te.</em></strong>
            <span className={styles.copy}>Your first season with the club starts here.</span>
            <span className={styles.action}>Apply for a spot <ArrowUpRight aria-hidden="true" /></span>
          </div>
        </a>
        <a ref={returnChoice} href="#returning" aria-label="Returning athlete? Check in" className={styles.teamLane}
          onClick={event => { if (!event.metaKey && !event.ctrlKey && !event.shiftKey && !event.altKey) setSweep("returning"); }}>
          <div className={styles.label}>
            <span className={styles.folio}><b aria-hidden="true" />Lane 02 <span>/</span> Returning athlete</span>
            <strong>Welcome <em>back.</em></strong>
            <span className={styles.copy}>Two fields. No paperwork.</span>
            <span className={styles.action}>Check in <ArrowUpRight aria-hidden="true" /></span>
          </div>
        </a>
      </nav>}
    </section>
    <section id="apply" ref={application} className={styles.formPage} hidden={selected !== "apply"}>
      {formTop("apply")}
      <RegistrationIntake season={season} channel={channel} kind="Application"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
    <section id="returning" className={styles.formPage} hidden={selected !== "returning"}>
      {formTop("returning")}
      <div className={styles.returnIntro}>
        <span className={styles.returnMark} aria-hidden="true">02 / Check-in</span>
        <h1 ref={returnHeading} tabIndex={-1}>Returning athlete? Check in</h1>
        <p>Tell us your athlete plans to return. No registration paperwork is required.</p>
      </div>
      <RegistrationIntake season={season} channel={channel} kind="Returning"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
  </>;
}
