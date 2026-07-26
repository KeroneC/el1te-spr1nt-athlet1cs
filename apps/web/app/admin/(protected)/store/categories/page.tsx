import { PageHeader } from "@/components/admin/page-header";
import { StoreCategoryManager } from "@/components/admin/store-category-manager";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminProductCategory } from "@/lib/admin/types";

export default async function StoreCategoriesPage() {
  const categories = await getAdminList<AdminProductCategory[]>("/api/admin/store/categories");
  return <><PageHeader title="Product categories" description="Keep customer-facing merchandise groups concise, ordered, and reusable."/><StoreCategoryManager initial={categories}/></>;
}
