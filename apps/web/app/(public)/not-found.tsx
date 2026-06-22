import Link from "next/link";
export default function NotFound() { return <div className="site-container route-state"><p className="eyebrow">404</p><h1>That page is not available</h1><p>The content may have moved or may no longer be published.</p><Link className="button button-primary" href="/">Return home</Link></div>; }
