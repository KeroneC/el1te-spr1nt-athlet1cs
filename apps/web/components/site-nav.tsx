import Link from "next/link";

const links = [
  { href: "/programs", label: "Programs" },
  { href: "/events", label: "Events" },
  { href: "/store", label: "Store" },
  { href: "/donate", label: "Donate" },
  { href: "/testimonials", label: "Testimonials" },
  { href: "/contact", label: "Contact" }
];

export function SiteNav() {
  return (
    <header className="flex items-center justify-between gap-6 py-5 text-white">
      <Link className="text-base font-bold tracking-normal sm:text-lg" href="/">
        El1te Spr1nt
      </Link>
      <nav aria-label="Primary navigation">
        <ul className="flex flex-wrap items-center justify-end gap-x-4 gap-y-2 text-sm font-semibold">
          {links.map((link) => (
            <li key={link.href}>
              <Link className="transition hover:text-track-gold" href={link.href}>
                {link.label}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
    </header>
  );
}
