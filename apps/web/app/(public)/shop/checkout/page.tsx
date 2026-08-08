import type { Metadata } from "next";
import { StoreCheckout } from "@/components/store/store-checkout";

export const metadata: Metadata = { title: "Checkout | El1te Spr1nt Athlet1cs" };
export default function StoreCheckoutPage() { return <StoreCheckout/>; }
