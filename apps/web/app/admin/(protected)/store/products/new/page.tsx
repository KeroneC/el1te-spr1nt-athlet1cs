import { PageHeader } from "@/components/admin/page-header";
import { StoreProductWizard } from "@/components/admin/store-product-wizard";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminProductCategory } from "@/lib/admin/types";

export default async function NewStoreProductPage() {
  const categories = await getAdminList<AdminProductCategory[]>("/api/admin/store/categories");
  return <><PageHeader title="Add merchandise" description="Build a draft through five focused steps. Nothing is public until you publish it."/><StoreProductWizard categories={categories}/></>;
}
