import { registrationDate, registrationMaximumAge, registrationMinimumAge } from "@/lib/registration";
import styles from "./registration-poster.module.css";

/** The opening notice and native disclosure work without JavaScript. */
export function RegistrationPoster({ openingDate, today, retryHref }: {
  openingDate: string; today: boolean; retryHref: string;
}) {
  return <section className={styles.poster} aria-labelledby="on-your-marks">
    <svg className={styles.lanes} viewBox="0 0 610 410" preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
      <g fill="none" stroke="#fff1dd" strokeWidth="3">
        {[-156, -104, -52, 0, 52, 104, 156, 208].map(y =>
          <path key={y} transform={`translate(0 ${y})`} d="M-130 395 H70 C222 395 282 330 362 215 L635 -175" />)}
      </g>
      <path className={styles.chalk} pathLength="1" d="M141 -40 L468 455" fill="none" stroke="#fffaf2" strokeWidth="10" />
    </svg>
    <div className={styles.copy}>
      <p className={styles.kicker}>El1te Spr1nt Athlet1cs <span>/</span> Family registration</p>
      <h1 id="on-your-marks">On your <em>marks.</em></h1>
      <p className={styles.date}>{today ? "Registration is scheduled to open today." :
        <>Registration opens <time dateTime={openingDate}>{registrationDate(openingDate)}</time>.</>}</p>
      <p className={styles.age}>For athletes ages {registrationMinimumAge}–{registrationMaximumAge} as of the first practice.</p>
      {today && <a className={styles.retry} href={retryHref}>Check registration status</a>}
      <details className={styles.prep}>
        <summary>How to prepare</summary>
        <p><strong>New to El1te?</strong> Complete one application per athlete when registration opens. Send paperwork only after an offer.</p>
        <p><strong>Returning athlete?</strong> Check in with your guardian email and your athlete’s first name. No paperwork.</p>
      </details>
    </div>
  </section>;
}
