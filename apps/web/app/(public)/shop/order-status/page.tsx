import type { Metadata } from "next";
import { StoreOrderStatus } from "@/components/store/store-order-status";

export const metadata: Metadata = { title: "Order status | El1te Spr1nt Athlet1cs", robots: { index: false, follow: false } };
export default function OrderStatusPage() { return <StoreOrderStatus/>; }
