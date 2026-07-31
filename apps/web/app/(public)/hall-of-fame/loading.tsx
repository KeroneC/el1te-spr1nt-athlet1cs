export default function Loading() {
  return <main aria-busy="true" aria-label="Loading Hall of Fame">
    <section className="hall-hero"><div className="site-container hall-hero-content"><div className="hall-crest skeleton-block" /></div></section>
    <section className="content-section hall-inductees"><div className="site-container"><div className="hall-inductee-grid"><div className="hall-inductee skeleton-block" /><div className="hall-inductee skeleton-block" /></div></div></section>
  </main>;
}
