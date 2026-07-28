import { StoreCart } from "@/components/store/store-cart";
import { isStoreEnabled } from "@/lib/public/client";
import { notFound } from "next/navigation";

export const dynamic = "force-dynamic";

export default async function StoreCartPage() {
  if (!await isStoreEnabled()) notFound();
  return <StoreCart />;
}
