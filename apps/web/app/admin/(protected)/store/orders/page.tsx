import Link from "next/link";
import { Eye } from "lucide-react";
import { PageHeader } from "@/components/admin/page-header";
import { Badge, EmptyState, FilterActions, FilterSelect, FilterText, formatDate } from "@/components/admin/list-controls";
import { Pagination } from "@/components/admin/pagination";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminStoreOrderStatus, AdminStoreOrderSummary, PagedResult } from "@/lib/admin/types";

const statuses: AdminStoreOrderStatus[] = ["AwaitingPayment","Paid","NeedsReview","ReadyForProduction","InProduction","NeedsCustomerInfo","ReadyForHandoff","Completed","Canceled","Refunded"];

export default async function StoreOrdersPage({ searchParams }: { searchParams: Promise<Record<string,string|undefined>> }) {
  const filters = await searchParams;
  const query = new URLSearchParams({ page: filters.page ?? "1", pageSize: "20" });
  if (filters.search) query.set("search", filters.search);
  if (filters.status) query.set("status", filters.status);
  if (filters.paymentStatus) query.set("paymentStatus", filters.paymentStatus);
  const result = await getAdminList<PagedResult<AdminStoreOrderSummary>>(`/api/admin/store/orders?${query}`);
  const paging = new URLSearchParams(query); paging.delete("pageSize");
  return <><PageHeader title="Order workboard" description="Track verified payments, personalization review, production, practice/event handoff, refunds, and customer messages."/>
    <form className="mb-5 grid gap-3 border border-slate-200 bg-white p-4 md:grid-cols-[1fr_210px_190px_auto] md:items-end"><FilterText value={filters.search} placeholder="Reference, buyer, or email"/><FilterSelect label="Fulfillment" name="status" value={filters.status} options={[["","All"],...statuses.map(value => [value, label(value)] as const)]}/><FilterSelect label="Payment" name="paymentStatus" value={filters.paymentStatus} options={[["","All"],["Pending","Pending"],["Paid","Paid"],["Refunding","Refunding"],["Refunded","Refunded"],["Failed","Failed"],["Canceled","Canceled"]]}/><FilterActions clearHref="/admin/store/orders" filtered={Boolean(filters.search || filters.status || filters.paymentStatus)}/></form>
    {result.items.length ? <div className="overflow-x-auto border border-slate-200 bg-white"><table className="w-full min-w-[980px] text-left"><thead className="bg-slate-100 text-xs uppercase text-slate-600"><tr><th className="p-4">Order</th><th className="p-4">Buyer</th><th className="p-4">Fulfillment</th><th className="p-4">Payment</th><th className="p-4">Total</th><th className="p-4">Placed</th><th className="p-4 text-right">View</th></tr></thead><tbody className="divide-y">{result.items.map(order => <tr key={order.id}><td className="p-4"><p className="font-mono text-sm font-black">{order.orderReference}</p>{order.hasPersonalization && <p className="mt-1 text-xs font-bold text-amber-700">Personalized</p>}</td><td className="p-4"><p className="font-bold">{order.customerName}</p><p className="text-xs text-slate-500">{order.customerEmail}</p></td><td className="p-4"><Badge tone={tone(order.status)}>{label(order.status)}</Badge></td><td className="p-4"><Badge tone={order.paymentStatus === "Paid" ? "green" : order.paymentStatus === "Refunding" ? "amber" : order.paymentStatus === "Refunded" ? "neutral" : order.paymentStatus === "Failed" ? "red" : "blue"}>{order.paymentStatus}</Badge></td><td className="p-4 font-black">{money(order.totalMinor,order.currency)}</td><td className="p-4 text-sm">{formatDate(order.createdAtUtc)}</td><td className="p-4 text-right"><Link href={`/admin/store/orders/${order.id}`} aria-label={`Open ${order.orderReference}`} className="inline-flex h-10 w-10 items-center justify-center border border-slate-300 hover:border-track-red"><Eye size={17}/></Link></td></tr>)}</tbody></table></div> : <EmptyState title="No orders found" description="Paid and in-progress Square orders will appear here."/>}
    <Pagination page={result.page} totalPages={result.totalPages} params={paging}/>
  </>;
}
function label(value: string) { return value.replace(/([a-z])([A-Z])/g,"$1 $2"); }
function tone(status: AdminStoreOrderStatus): "green"|"amber"|"red"|"blue"|"neutral" { if (["Completed","ReadyForHandoff"].includes(status)) return "green"; if (["NeedsReview","NeedsCustomerInfo"].includes(status)) return "amber"; if (["Canceled","Refunded"].includes(status)) return "neutral"; if (["Paid","ReadyForProduction","InProduction"].includes(status)) return "blue"; return "red"; }
function money(value:number,currency:string){return new Intl.NumberFormat("en-US",{style:"currency",currency}).format(value/100);}
