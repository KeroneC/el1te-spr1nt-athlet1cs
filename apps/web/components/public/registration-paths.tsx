"use client";

import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import type { MouseEvent, ReactNode } from "react";
import type { PublicRegistrationSeason, RegistrationChannel } from "@/lib/registration";
import { RegistrationIntake } from "./registration-intake";

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

export function RegistrationPaths({ season, channel, children, aside }: {
  season: PublicRegistrationSeason; channel: RegistrationChannel; children: ReactNode; aside: ReactNode;
}) {
  const selected = useSyncExternalStore(subscribe, pathFromHash, () => null);
  const previous = useRef<Path | null>(null);
  const applicationHeading = useRef<HTMLHeadingElement>(null);
  const returnHeading = useRef<HTMLHeadingElement>(null);
  const applicationChoice = useRef<HTMLAnchorElement>(null);
  const returnChoice = useRef<HTMLAnchorElement>(null);
  const [intakeClosed, setIntakeClosed] = useState(false);

  useEffect(() => {
    if (selected) {
      focusHeading(selected === "apply" ? applicationHeading.current : returnHeading.current);
    } else if (previous.current && !window.location.hash) {
      (previous.current === "apply" ? applicationChoice.current : returnChoice.current)?.focus();
    }
    previous.current = selected;
  }, [selected]);

  function refocusSelected(event: MouseEvent<HTMLAnchorElement>, path: Path) {
    if (selected !== path || event.metaKey || event.ctrlKey || event.altKey || event.shiftKey) return;
    event.preventDefault();
    focusHeading(path === "apply" ? applicationHeading.current : returnHeading.current);
  }

  return <>
    <div>
      <p className="eyebrow">Season details</p>
      <h2>{intakeClosed ? "Applications are currently closed" : "Applications are open"}</h2>
      <nav className="registration-actions" aria-label="Registration paths">
        <a ref={applicationChoice} className="button button-primary" href="#apply"
          aria-current={selected === "apply" ? "location" : undefined}
          onClick={event => refocusSelected(event, "apply")}>Apply for a spot</a>
        <a ref={returnChoice} className="button button-secondary" href="#returning"
          aria-current={selected === "returning" ? "location" : undefined}
          onClick={event => refocusSelected(event, "returning")}>Returning athlete? Check in</a>
      </nav>
      {children}
    </div>
    {aside}
    <section id="apply" className="registration-path" hidden={selected !== "apply"}>
      <h2 ref={applicationHeading} tabIndex={-1}>Apply for a spot</h2>
      <p className="mb-4">Use one application per athlete. Fields are required unless marked optional.</p>
      <RegistrationIntake season={season} channel={channel} kind="Application"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
    <section id="returning" className="registration-path" hidden={selected !== "returning"}>
      <h2 ref={returnHeading} tabIndex={-1}>Returning athlete? Check in</h2>
      <p className="mb-4">Tell us your athlete plans to return. No registration paperwork is required.</p>
      <RegistrationIntake season={season} channel={channel} kind="Returning"
        intakeClosed={intakeClosed} onIntakeClosed={() => setIntakeClosed(true)} />
    </section>
  </>;
}
