import { PageHeader } from "@/components/admin/page-header";
import { StoreProductWizard } from "@/components/admin/store-product-wizard";
import { StoreProductSlugRepair } from "@/components/admin/store-product-slug-repair";
import { getAdminItem, getAdminList } from "@/lib/admin/page-data";
import type { AdminProductCategory, AdminStoreProduct } from "@/lib/admin/types";

export default async function EditStoreProductPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const [item, categories] = await Promise.all([
    getAdminItem<AdminStoreProduct>(`/api/admin/store/products/${id}`),
    getAdminList<AdminProductCategory[]>("/api/admin/store/categories")
  ]);
  return <><PageHeader title={`Edit ${item.name}`} description={`Draft and catalog setup for /${item.slug}. Inventory quantities are changed in the Inventory workspace.`}/><StoreProductSlugRepair id={item.id} name={item.name} slug={item.slug} status={item.status}/><StoreProductWizard item={item} categories={categories}/></>;
}
