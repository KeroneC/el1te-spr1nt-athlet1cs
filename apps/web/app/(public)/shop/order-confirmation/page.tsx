import type { Metadata } from "next";
import { StoreOrderConfirmation } from "@/components/store/store-order-confirmation";

export const metadata: Metadata = { title: "Order confirmation | El1te Spr1nt Athlet1cs" };
export default function OrderConfirmationPage() { return <StoreOrderConfirmation/>; }
