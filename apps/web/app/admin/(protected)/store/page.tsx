import Link from "next/link";
import { AlertTriangle, Boxes, CircleDollarSign, ClipboardList, FolderTree, Package, PackageCheck, Plus, ShoppingBag } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { getAdminItem } from "@/lib/admin/page-data";
import type { AdminStoreDashboard, AdminStoreOperationsDashboard } from "@/lib/admin/types";

export default async function StoreDashboardPage() {
  const summary = await getAdminItem<AdminStoreDashboard>("/api/admin/store/dashboard");
  const orders = await getAdminItem<AdminStoreOperationsDashboard>("/api/admin/store/operations-dashboard");
  return <><PageHeader title="Merchandise operations" description="Build the catalog, count every size and color, and prepare the store before its public launch." action={{ href: "/admin/store/products/new", label: "Add product", icon: Plus }}/>
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-label="Store summary">
      <Metric label="Published products" value={summary.publishedProducts} icon={ShoppingBag} tone="red"/>
      <Metric label="Draft products" value={summary.draftProducts} icon={Package} tone="slate"/>
      <Metric label="Active variants" value={summary.activeVariants} icon={Boxes} tone="green"/>
      <Metric label="Units on hand" value={summary.totalOnHand} icon={PackageCheck} tone="green"/>
      <Metric label="Low-stock variants" value={summary.lowStockVariants} icon={AlertTriangle} tone="amber"/>
      <Metric label="Sold-out variants" value={summary.soldOutVariants} icon={CircleDollarSign} tone="red"/>
    </section>
    <section className="mt-8" aria-labelledby="order-operations"><div className="mb-4 flex items-end justify-between gap-4"><div><p className="text-xs font-black uppercase tracking-widest text-track-red">Order operations</p><h2 id="order-operations" className="mt-1 text-2xl font-black">From payment to handoff</h2></div><Link className="text-sm font-black text-track-red" href="/admin/store/orders">Open workboard →</Link></div><div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><Metric label="Cancellation hold" value={orders.cancellationHold} icon={CircleDollarSign} tone="amber"/><Metric label="Needs review" value={orders.needsReview} icon={ClipboardList} tone="red"/><Metric label="In production" value={orders.inProduction} icon={Package} tone="slate"/><Metric label="Ready for handoff" value={orders.readyForHandoff} icon={PackageCheck} tone="green"/></div>{(orders.refundFailures > 0 || orders.emailFailures > 0) && <p className="mt-3 border-l-4 border-track-red bg-red-50 p-3 text-sm font-bold text-red-900">Attention needed: {orders.refundFailures} refund and {orders.emailFailures} email failures.</p>}</section>
    <section className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <Workspace href="/admin/store/orders" title="Orders" text="Review paid orders, production, refunds, and handoff." icon={ClipboardList}/>
      <Workspace href="/admin/store/products" title="Catalog" text="Products, photos, tracked options, customizations, and publishing." icon={ShoppingBag}/>
      <Workspace href="/admin/store/categories" title="Categories" text="Reusable customer-facing groups and ordering." icon={FolderTree}/>
      <Workspace href="/admin/store/inventory" title="Inventory" text="Receive merchandise, correct counts, and complete physical stocktakes." icon={Boxes}/>
    </section>
    <div className="mt-8 border-l-4 border-track-field bg-white p-5"><h2 className="font-black">Square checkout is guarded</h2><p className="mt-1 text-sm leading-6 text-slate-600">Catalog visibility and payment are controlled independently. Checkout stays unavailable unless both store flags and Square credentials are enabled for the environment.</p></div>
  </>;
}
function Metric({ label, value, icon: Icon, tone }: { label: string; value: number; icon: typeof Package; tone: "red" | "green" | "amber" | "slate" }) { const color = { red: "border-track-red text-track-red", green: "border-track-field text-track-field", amber: "border-amber-500 text-amber-700", slate: "border-slate-500 text-slate-600" }[tone]; return <div className={`border-l-4 bg-white p-5 shadow-sm ${color}`}><Icon size={21}/><p className="mt-3 text-3xl font-black text-track-ink">{value}</p><p className="mt-1 text-sm font-bold text-slate-600">{label}</p></div>; }
function Workspace({ href, title, text, icon: Icon }: { href: string; title: string; text: string; icon: typeof ShoppingBag }) { return <Link href={href} className="group border border-slate-200 bg-white p-6 hover:border-track-red"><Icon className="text-track-red" size={24}/><h2 className="mt-4 text-xl font-black group-hover:text-track-red">{title}</h2><p className="mt-2 text-sm leading-6 text-slate-600">{text}</p></Link>; }
