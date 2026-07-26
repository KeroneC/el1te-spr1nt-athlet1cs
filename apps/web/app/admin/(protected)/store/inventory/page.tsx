import { PageHeader } from "@/components/admin/page-header";
import { StoreInventoryWorkspace } from "@/components/admin/store-inventory-workspace";
import { getAdminList } from "@/lib/admin/page-data";
import type { AdminInventoryVariant, PagedResult } from "@/lib/admin/types";

export default async function StoreInventoryPage() {
  const inventory = await getAdminList<PagedResult<AdminInventoryVariant>>("/api/admin/store/inventory?page=1&pageSize=200");
  return <><PageHeader title="Store inventory" description="Count concrete size and garment-color variants. Every change creates an immutable adjustment record."/><StoreInventoryWorkspace initial={inventory}/></>;
}
