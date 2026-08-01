import { ArrowRight, Search, ShoppingBag, SlidersHorizontal } from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getStoreProducts, PublicApiError } from "@/lib/public/client";
import type { PublicStockStatus } from "@/lib/public/types";
import { formatStoreMoney } from "@/lib/store/configurator";
import { ResponsiveMediaImage } from "@/components/public/responsive-media-image";

type Props = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

const availabilityValues = new Set<PublicStockStatus>(["InStock", "LowStock", "SoldOut"]);

export default async function ShopPage({ searchParams }: Props) {
  const input = await searchParams;
  const search = single(input.search);
  const category = single(input.category);
  const availabilityInput = single(input.availability);
  const availability = availabilityValues.has(availabilityInput as PublicStockStatus)
    ? availabilityInput as PublicStockStatus
    : "";
  const page = Math.max(1, Number(single(input.page)) || 1);
  const query = new URLSearchParams({ page: String(page), pageSize: "12" });
  if (search) query.set("search", search);
  if (category) query.set("category", category);
  if (availability) query.set("availability", availability);

  let catalog;
  try {
    catalog = await getStoreProducts(query.toString());
  } catch (error) {
    if (error instanceof PublicApiError && error.status === 404) notFound();
    throw error;
  }

  return <article className="storefront">
    <header className="store-hero">
      <div className="site-container store-hero-grid">
        <div>
          <p className="eyebrow light">Official team merchandise</p>
          <h1>Wear the work.</h1>
          <p>Club gear configured for your athlete, with live size and color availability before checkout.</p>
        </div>
        <div className="store-hero-mark" aria-hidden="true">
          <ShoppingBag />
          <span>El1te</span>
        </div>
      </div>
    </header>

    <section className="site-container store-catalog-section">
      <div className="store-catalog-heading">
        <div><p className="eyebrow">Team collection</p><h2>Find your gear</h2></div>
        <p>{catalog.totalCount} {catalog.totalCount === 1 ? "product" : "products"}</p>
      </div>

      <form className="store-filter-panel" action="/shop" method="get">
        <label className="store-search-field">
          <span>Search products</span>
          <span><Search size={18} aria-hidden="true" /><input name="search" defaultValue={search} placeholder="Hoodie, shirt, bag…" /></span>
        </label>
        <label><span>Category</span><select name="category" defaultValue={category}><option value="">All categories</option>{catalog.categories.map(value => <option key={value.slug} value={value.slug}>{value.name} ({value.productCount})</option>)}</select></label>
        <label><span>Availability</span><select name="availability" defaultValue={availability}><option value="">Any availability</option><option value="InStock">In stock</option><option value="LowStock">Low stock</option><option value="SoldOut">Sold out</option></select></label>
        <button className="button button-primary" type="submit"><SlidersHorizontal size={17} aria-hidden="true" />Apply filters</button>
        {(search || category || availability) && <Link className="store-clear-filters" href="/shop">Clear all</Link>}
      </form>

      {catalog.items.length ? <div className="store-product-grid">
        {catalog.items.map(product => <Link className="store-product-card" href={`/shop/${product.slug}`} key={product.slug}>
          <div className="store-product-image">
            {product.primaryImageUrl
              ? <ResponsiveMediaImage src={product.primaryImageUrl} alt={product.primaryImageAltText ?? product.name} sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw" />
              : <span aria-hidden="true"><ShoppingBag /></span>}
            {product.isFeatured && <span className="store-featured-tag">Featured</span>}
            <span className={`store-stock-tag ${stockClass(product.availability)}`}>{stockLabel(product.availability)}</span>
          </div>
          <div className="store-product-copy">
            <p>{product.categoryName ?? "Team gear"}</p>
            <h2>{product.name}</h2>
            {product.shortDescription && <p>{product.shortDescription}</p>}
            <div><strong>{priceRange(product.minimumPriceMinor, product.maximumPriceMinor, product.currency)}</strong><span>Configure <ArrowRight size={17} aria-hidden="true" /></span></div>
          </div>
        </Link>)}
      </div> : <div className="store-empty-state">
        <ShoppingBag aria-hidden="true" />
        <h2>No gear matches those filters</h2>
        <p>Try another category or clear the filters to see the complete team collection.</p>
        <Link className="button button-secondary" href="/shop">View all gear</Link>
      </div>}

      {catalog.totalPages > 1 && <nav className="store-pagination" aria-label="Shop pages">
        <Link aria-disabled={catalog.page <= 1} className={catalog.page <= 1 ? "is-disabled" : ""} href={pageHref(query, catalog.page - 1)}>Previous</Link>
        <span>Page {catalog.page} of {catalog.totalPages}</span>
        <Link aria-disabled={catalog.page >= catalog.totalPages} className={catalog.page >= catalog.totalPages ? "is-disabled" : ""} href={pageHref(query, catalog.page + 1)}>Next</Link>
      </nav>}
    </section>
  </article>;
}

function single(value: string | string[] | undefined) {
  return (Array.isArray(value) ? value[0] : value ?? "").trim().slice(0, 100);
}
function stockLabel(value: PublicStockStatus) {
  return value === "LowStock" ? "Low stock" : value === "SoldOut" ? "Sold out" : "In stock";
}
function stockClass(value: PublicStockStatus) {
  return value === "LowStock" ? "is-low" : value === "SoldOut" ? "is-sold" : "is-in";
}
function priceRange(minimum: number, maximum: number, currency: string) {
  return minimum === maximum
    ? formatStoreMoney(minimum, currency)
    : `${formatStoreMoney(minimum, currency)}–${formatStoreMoney(maximum, currency)}`;
}
function pageHref(query: URLSearchParams, page: number) {
  const value = new URLSearchParams(query);
  value.set("page", String(Math.max(1, page)));
  return `/shop?${value.toString()}`;
}
