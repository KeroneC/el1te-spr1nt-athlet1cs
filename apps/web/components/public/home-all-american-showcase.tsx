"use client";

import { ChevronLeft, ChevronRight, Pause, Play } from "lucide-react";
import Link from "next/link";
import { useEffect, useReducer, useRef, useState } from "react";
import styles from "./home-all-american-showcase.module.css";

export const SHOWCASE_INTERVAL_MS = 5_000;

export type ShowcaseState = {
  activeIndex: number;
  userPaused: boolean;
};

export type ShowcaseAction =
  | { type: "tick"; slideCount: number }
  | { type: "next"; slideCount: number }
  | { type: "previous"; slideCount: number }
  | { type: "pause" }
  | { type: "play" };

export function showcaseReducer(state: ShowcaseState, action: ShowcaseAction): ShowcaseState {
  switch (action.type) {
    case "tick":
      return state.userPaused ? state : { ...state, activeIndex: (state.activeIndex + 1) % action.slideCount };
    case "next":
      return { activeIndex: (state.activeIndex + 1) % action.slideCount, userPaused: true };
    case "previous":
      return { activeIndex: (state.activeIndex - 1 + action.slideCount) % action.slideCount, userPaused: true };
    case "pause":
      return { ...state, userPaused: true };
    case "play":
      return { ...state, userPaused: false };
  }
}

export function visibleSlideIndexes(activeIndex: number, slideCount: number) {
  return [
    (activeIndex - 1 + slideCount) % slideCount,
    activeIndex,
    (activeIndex + 1) % slideCount
  ];
}

export function shouldAutoplay(input: {
  userPaused: boolean;
  hovered: boolean;
  focusWithin: boolean;
  pageVisible: boolean;
  reducedMotion: boolean;
}) {
  return !input.userPaused && !input.hovered && !input.focusWithin && input.pageVisible && !input.reducedMotion;
}

type ShowcaseSlide = {
  id: string;
  src: string;
  alt: string;
  width: number;
  height: number;
  focalPoint?: string;
  landscape?: boolean;
};

export const ALL_AMERICAN_SLIDES: ShowcaseSlide[] = [
  {
    id: "jonathan-newton-01",
    src: "/images/home/all-americans/jonathan-newton-01",
    alt: "Jonathan Newton wearing two AAU Junior Olympic medals and holding an All-American patch",
    width: 960,
    height: 1349,
    focalPoint: "center 38%"
  },
  {
    id: "alexis-bansah",
    src: "/images/home/all-americans/alexis-bansah",
    alt: "Alexis Bansah holding an AAU Junior Olympic medal and All-American patch",
    width: 960,
    height: 1280,
    focalPoint: "center 35%"
  },
  {
    id: "camryn-bruno",
    src: "/images/home/all-americans/camryn-bruno",
    alt: "Camryn Bruno wearing an AAU Junior Olympic medal",
    width: 960,
    height: 1280,
    focalPoint: "center 36%"
  },
  {
    id: "javon-johnston",
    src: "/images/home/all-americans/javon-johnston",
    alt: "Javon Johnston wearing AAU Junior Olympic medals",
    width: 960,
    height: 1280,
    focalPoint: "center 34%"
  },
  {
    id: "claire-jubeck",
    src: "/images/home/all-americans/claire-jubeck",
    alt: "Claire Jubeck holding an AAU Junior Olympic medal and All-American patch",
    width: 960,
    height: 1280,
    focalPoint: "center 37%"
  },
  {
    id: "javon-alexis",
    src: "/images/home/all-americans/javon-alexis",
    alt: "Javon Johnston and Alexis Bansah celebrating with AAU Junior Olympic medals",
    width: 960,
    height: 1705,
    focalPoint: "center 35%"
  },
  {
    id: "jonathan-newton-02",
    src: "/images/home/all-americans/jonathan-newton-02",
    alt: "Jonathan Newton holding an AAU Junior Olympic medal and All-American patch",
    width: 960,
    height: 1280,
    focalPoint: "center 36%"
  },
  {
    id: "relay-team",
    src: "/images/home/all-americans/relay-team",
    alt: "Matthew, Rocco, Kingston, and Chase together after the AAU Junior Olympic 4 by 100 meter relay",
    width: 960,
    height: 720,
    focalPoint: "center center",
    landscape: true
  }
];

export function HomeAllAmericanShowcase({ archiveEnabled = false }: { archiveEnabled?: boolean }) {
  const [state, dispatch] = useReducer(showcaseReducer, { activeIndex: 0, userPaused: false });
  const [hovered, setHovered] = useState(false);
  const [focusWithin, setFocusWithin] = useState(false);
  const [pageVisible, setPageVisible] = useState(true);
  const [reducedMotion, setReducedMotion] = useState(false);
  const focusLeaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const slideCount = ALL_AMERICAN_SLIDES.length;
  const autoplayEnabled = shouldAutoplay({ userPaused: state.userPaused, hovered, focusWithin, pageVisible, reducedMotion });

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const updatePreference = () => setReducedMotion(media.matches);
    updatePreference();
    media.addEventListener("change", updatePreference);
    return () => media.removeEventListener("change", updatePreference);
  }, []);

  useEffect(() => {
    const updateVisibility = () => setPageVisible(document.visibilityState === "visible");
    updateVisibility();
    document.addEventListener("visibilitychange", updateVisibility);
    return () => document.removeEventListener("visibilitychange", updateVisibility);
  }, []);

  useEffect(() => {
    if (!autoplayEnabled) return;
    const timer = window.setInterval(() => dispatch({ type: "tick", slideCount }), SHOWCASE_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [autoplayEnabled, slideCount]);

  useEffect(() => () => {
    if (focusLeaveTimer.current) clearTimeout(focusLeaveTimer.current);
  }, []);

  const renderedIndexes = visibleSlideIndexes(state.activeIndex, slideCount);
  const activeSlide = ALL_AMERICAN_SLIDES[state.activeIndex];

  function handleBlur() {
    focusLeaveTimer.current = setTimeout(() => setFocusWithin(false), 0);
  }

  function handleFocus() {
    if (focusLeaveTimer.current) clearTimeout(focusLeaveTimer.current);
    setFocusWithin(true);
  }

  return (
    <section
      className={`${styles.showcase} ${activeSlide.landscape ? styles.landscapeFinale : ""}`}
      aria-labelledby="all-american-title"
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onFocus={handleFocus}
      onBlur={handleBlur}
    >
      <div className={styles.heading}>
        <p>2026 AAU Junior Olympic Games</p>
        <h1 id="all-american-title"><strong>9</strong> All-Americans</h1>
        <span>11 All-American medals</span>
        {archiveEnabled && <Link className={styles.archiveLink} href="/all-americans">Explore our All-American legacy</Link>}
      </div>

      <div className={styles.stage} aria-live="off">
        <span className={styles.slash} aria-hidden="true" />
        {renderedIndexes.map((slideIndex, positionIndex) => {
          const slide = ALL_AMERICAN_SLIDES[slideIndex];
          const position = positionIndex === 0 ? "previous" : positionIndex === 1 ? "active" : "next";
          const isActive = position === "active";
          return (
            <figure
              className={`${styles.card} ${styles[position]} ${slide.landscape ? styles.landscape : ""}`}
              key={slide.id}
              aria-hidden={!isActive}
            >
              <picture>
                <source
                  srcSet={`${slide.src}-480.webp 480w, ${slide.src}-960.webp 960w`}
                  sizes="(max-width: 768px) 84vw, (max-width: 1100px) 42vw, 34rem"
                  type="image/webp"
                />
                <img
                  src={`${slide.src}-960.webp`}
                  alt={isActive ? slide.alt : ""}
                  width={slide.width}
                  height={slide.height}
                  loading="lazy"
                  decoding="async"
                  style={{ objectPosition: slide.focalPoint }}
                />
              </picture>
            </figure>
          );
        })}
      </div>

      <div className={styles.controlRail}>
        <span className={styles.progress} aria-hidden="true" data-testid="all-american-progress">
          {String(state.activeIndex + 1).padStart(2, "0")} <i>/</i> {String(slideCount).padStart(2, "0")}
        </span>
        <div className={styles.controls} role="group" aria-label="All-American showcase controls">
          <button type="button" onClick={() => dispatch({ type: "previous", slideCount })} aria-label="Show previous photograph">
            <ChevronLeft aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={() => dispatch({ type: state.userPaused ? "play" : "pause" })}
            aria-label={state.userPaused ? "Play photograph showcase" : "Pause photograph showcase"}
            aria-pressed={state.userPaused}
          >
            {state.userPaused ? <Play aria-hidden="true" /> : <Pause aria-hidden="true" />}
          </button>
          <button type="button" onClick={() => dispatch({ type: "next", slideCount })} aria-label="Show next photograph">
            <ChevronRight aria-hidden="true" />
          </button>
        </div>
      </div>
    </section>
  );
}
