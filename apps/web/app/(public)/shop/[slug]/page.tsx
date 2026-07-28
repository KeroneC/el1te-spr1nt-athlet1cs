import { notFound } from "next/navigation";
import { StoreProductConfigurator } from "@/components/store/store-product-configurator";
import { getStoreProduct, PublicApiError } from "@/lib/public/client";

type Props = { params: Promise<{ slug: string }> };

export default async function ShopProductPage({ params }: Props) {
  const { slug } = await params;
  let product;
  try {
    product = await getStoreProduct(slug);
  } catch (error) {
    if (error instanceof PublicApiError && error.status === 404) notFound();
    throw error;
  }
  return <StoreProductConfigurator product={product} />;
}
