import Link from "next/link";
import { AlertTriangle, Boxes, CircleDollarSign, DownloadCloud, FolderTree, Package, PackageCheck, Plus, ShoppingBag } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem } from "@/lib/admin/page-data";
import type { AdminStoreDashboard } from "@/lib/admin/types";

export default async function StoreDashboardPage() {
  const summary = await getAdminItem<AdminStoreDashboard>("/api/admin/store/dashboard");
  return <><PageHeader title="Merchandise operations" description="Build the catalog, count every size and color, and prepare the store before its public launch." action={{ href: "/admin/store/products/new", label: "Add product", icon: Plus }}/>
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-label="Store summary">
      <Metric label="Published products" value={summary.publishedProducts} icon={ShoppingBag} tone="red"/>
      <Metric label="Draft products" value={summary.draftProducts} icon={Package} tone="slate"/>
      <Metric label="Active variants" value={summary.activeVariants} icon={Boxes} tone="green"/>
      <Metric label="Units on hand" value={summary.totalOnHand} icon={PackageCheck} tone="green"/>
      <Metric label="Low-stock variants" value={summary.lowStockVariants} icon={AlertTriangle} tone="amber"/>
      <Metric label="Sold-out variants" value={summary.soldOutVariants} icon={CircleDollarSign} tone="red"/>
    </section>
    <section className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <Workspace href="/admin/store/products" title="Catalog" text="Products, photos, tracked options, customizations, and publishing." icon={ShoppingBag}/>
      <Workspace href="/admin/store/categories" title="Categories" text="Reusable customer-facing groups and ordering." icon={FolderTree}/>
      <Workspace href="/admin/store/inventory" title="Inventory" text="Receive merchandise, correct counts, and complete physical stocktakes." icon={Boxes}/>
      <Workspace href="/admin/store/import" title="Square import" text="SuperAdmins can copy the existing Square catalog into safe local drafts." icon={DownloadCloud}/>
    </section>
    <div className="mt-8 border-l-4 border-track-field bg-white p-5"><h2 className="font-black">Public store remains off</h2><p className="mt-1 text-sm leading-6 text-slate-600">This workspace is safe to prepare now. The current Square storefront stays live, and no El1te product appears publicly until the final cutover phase.</p></div>
  </>;
}
function Metric({ label, value, icon: Icon, tone }: { label: string; value: number; icon: typeof Package; tone: "red" | "green" | "amber" | "slate" }) { const color = { red: "border-track-red text-track-red", green: "border-track-field text-track-field", amber: "border-amber-500 text-amber-700", slate: "border-slate-500 text-slate-600" }[tone]; return <div className={`border-l-4 bg-white p-5 shadow-sm ${color}`}><Icon size={21}/><p className="mt-3 text-3xl font-black text-track-ink">{value}</p><p className="mt-1 text-sm font-bold text-slate-600">{label}</p></div>; }
function Workspace({ href, title, text, icon: Icon }: { href: string; title: string; text: string; icon: typeof ShoppingBag }) { return <Link href={href} className="group border border-slate-200 bg-white p-6 hover:border-track-red"><Icon className="text-track-red" size={24}/><h2 className="mt-4 text-xl font-black group-hover:text-track-red">{title}</h2><p className="mt-2 text-sm leading-6 text-slate-600">{text}</p></Link>; }
