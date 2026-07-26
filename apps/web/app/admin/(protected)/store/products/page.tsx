/* eslint-disable @next/next/no-img-element */
import Link from "next/link";
import { Edit3, Plus } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { Badge, EmptyState, FilterActions, FilterSelect, FilterText } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { StoreProductActions } from "@/components/admin/store-product-actions";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminStoreProductSummary, PagedResult } from "@/lib/admin/types";

export default async function ProductsPage({ searchParams }: { searchParams: Promise<Record<string, string | undefined>> }) {
  const filters = await searchParams;
  const params = new URLSearchParams({ page: filters.page ?? "1", pageSize: "20" });
  if (filters.search) params.set("search", filters.search);
  if (filters.status) params.set("status", filters.status);
  if (filters.lowStock) params.set("lowStock", filters.lowStock);
  const result = await getAdminList<PagedResult<AdminStoreProductSummary>>(`/api/admin/store/products?${params}`);
  const paging = new URLSearchParams(params); paging.delete("pageSize");
  return <><PageHeader title="Store catalog" description="Products remain private drafts until you deliberately publish them and the public store flag is enabled." action={{ href: "/admin/store/products/new", label: "Add product", icon: Plus }}/>
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_190px_190px_auto] md:items-end"><FilterText value={filters.search} placeholder="Product name or SKU"/><FilterSelect label="Status" name="status" value={filters.status} options={[["","All"],["Draft","Draft"],["Published","Published"],["Archived","Archived"]]}/><FilterSelect label="Stock" name="lowStock" value={filters.lowStock} options={[["","All"],["true","Low / sold out"],["false","Healthy"]]}/><FilterActions clearHref="/admin/store/products" filtered={Boolean(filters.search || filters.status || filters.lowStock)}/></form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[920px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Product</th><th className="p-4">Status</th><th className="p-4">Price</th><th className="p-4">Variants</th><th className="p-4">Available</th><th className="p-4">Source</th><th className="p-4 text-right">Actions</th></tr></thead><tbody className="divide-y">{result.items.map(product => <tr key={product.id}><td className="p-4"><div className="flex items-center gap-3">{product.primaryImageUrl ? <img src={product.primaryImageUrl} alt="" className="h-14 w-14 bg-slate-100 object-contain"/> : <div className="grid h-14 w-14 place-items-center bg-slate-100 text-xs text-slate-400">No image</div>}<div><p className="font-black">{product.name}</p><p className="text-xs text-slate-500">{product.categoryName ?? "Uncategorized"} · /{product.slug}</p></div></div></td><td className="p-4"><Badge tone={product.status === "Published" ? "green" : product.status === "Archived" ? "neutral" : "amber"}>{product.status}</Badge></td><td className="p-4 font-black">{currency(product.basePriceMinor)}</td><td className="p-4">{product.variantCount}</td><td className="p-4"><p className="font-black">{product.totalAvailable}</p>{product.lowStockVariantCount > 0 && <p className="text-xs font-bold text-amber-700">{product.lowStockVariantCount} low</p>}</td><td className="p-4 text-sm">{product.squareCatalogObjectId ? "Square import" : "El1te"}</td><td className="p-4"><div className="flex justify-end gap-2"><Link href={`/admin/store/products/${product.id}/edit`} aria-label={`Edit ${product.name}`} className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 hover:border-track-red"><Edit3 size={17}/></Link><StoreProductActions id={product.id} name={product.name} archived={product.status === "Archived"}/></div></td></tr>)}</tbody></table></div> : <EmptyState title="No products found" description="Create a product or import the existing Square catalog as drafts."/>}
    <Pagination page={result.page} totalPages={result.totalPages} params={paging}/>
  </>;
}
function currency(minor: number) { return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(minor / 100); }
