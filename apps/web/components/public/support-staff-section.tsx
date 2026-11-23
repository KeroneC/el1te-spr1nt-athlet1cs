import Image from "next/image";
import styles from "./support-staff-section.module.css";

const staff = [
  { name: "Nickay Creary", portrait: "nickay-creary" },
  { name: "Michelle Newton", portrait: "michelle-newton" },
  { name: "Denise Gibbs", portrait: "denise-gibbs" },
] as const;

export function SupportStaffSection() {
  return (
    <section className={styles.section} aria-labelledby="support-staff-title">
      <div className="site-container">
        <header className={styles.heading}>
          <p className="eyebrow">Meet the team</p>
          <h2 id="support-staff-title">Support Staff</h2>
        </header>
        <ul className={styles.roster} role="list">
          {staff.map(({ name, portrait }) => (
            <li key={portrait}>
              <figure className={styles.profile}>
                <Image
                  className={styles.portrait}
                  src={`/images/support-staff/${portrait}.webp`}
                  alt=""
                  width={384}
                  height={384}
                  loading="lazy"
                  unoptimized
                />
                <figcaption>{name}</figcaption>
              </figure>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
