import { PageHeader } from "@/components/admin/page-header";
import { SquareCatalogImport } from "@/components/admin/square-catalog-import";
import { requireSuperAdminUser } from "@/lib/admin/auth";
import { getAdminItem } from "@/lib/admin/page-data";
import type { SquareCatalogImportPreview } from "@/lib/admin/types";

export default async function StoreImportPage() {
  await requireSuperAdminUser();
  const preview = await getAdminItem<SquareCatalogImportPreview>("/api/admin/store/square-import/preview");
  return <><PageHeader title="Import from Square" description="One-time migration into El1te-owned unpublished drafts. Existing imports are always skipped."/><SquareCatalogImport preview={preview}/></>;
}
