import Image from "next/image";
import { SiteNav } from "@/components/site-nav";

const focusAreas = [
  "Secure registration",
  "Club operations",
  "Family communication"
];

export default function Home() {
  return (
    <main>
      <section className="relative min-h-[84svh] overflow-hidden bg-track-ink">
        <Image
          src="/images/track-hero.png"
          alt="Youth sprinters practicing on an outdoor track"
          fill
          priority
          sizes="100vw"
          className="object-cover"
        />
        <div className="absolute inset-0 bg-[linear-gradient(90deg,rgba(23,32,42,0.88),rgba(23,32,42,0.58),rgba(23,32,42,0.16))]" />

        <div className="relative mx-auto flex min-h-[84svh] w-full max-w-7xl flex-col px-5 sm:px-8 lg:px-10">
          <SiteNav />

          <div className="flex flex-1 items-center py-12">
            <div className="max-w-3xl text-white">
              <p className="mb-5 text-sm font-bold uppercase tracking-[0.18em] text-track-gold">
                Nonprofit youth track club
              </p>
              <h1 className="text-5xl font-black tracking-normal sm:text-6xl lg:text-7xl">
                El1te Spr1nt Athlet1cs
              </h1>
              <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-100 sm:text-xl">
                A secure digital home for athletes, parents, coaches, programs,
                events, donations, orders, and community stories.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <a
                  className="inline-flex min-h-11 items-center justify-center bg-track-gold px-5 text-sm font-bold text-track-ink transition hover:bg-white"
                  href="/donate"
                >
                  Donate
                </a>
                <a
                  className="inline-flex min-h-11 items-center justify-center border border-white/70 px-5 text-sm font-bold text-white transition hover:border-track-gold hover:text-track-gold"
                  href="/programs"
                >
                  Explore Programs
                </a>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-white px-5 py-10 sm:px-8 lg:px-10">
        <div className="mx-auto grid max-w-7xl gap-6 md:grid-cols-[1.1fr_1fr] md:items-center">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.16em] text-track-red">
              Platform foundation
            </p>
            <h2 className="mt-3 text-3xl font-black tracking-normal text-track-ink">
              Built for the workflows a growing club needs next.
            </h2>
          </div>
          <ul className="grid gap-3 text-base font-semibold text-slate-700 sm:grid-cols-3 md:grid-cols-1 lg:grid-cols-3">
            {focusAreas.map((area) => (
              <li key={area} className="border-l-4 border-track-field pl-4">
                {area}
              </li>
            ))}
          </ul>
        </div>
      </section>
    </main>
  );
}
