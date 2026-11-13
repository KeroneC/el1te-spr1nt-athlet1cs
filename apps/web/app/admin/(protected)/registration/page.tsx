import { PageHeader } from "@/components/admin/page-header";
import { RegistrationWorkspace, type RegistrationOverview } from "@/components/admin/registration-workspace";
import { requireAdminUser } from "@/lib/admin/auth";
import { getAdminList } from "@/lib/admin/page-data";

export default async function RegistrationAdminPage() {
  const [overview, user] = await Promise.all([
    getAdminList<RegistrationOverview>("/api/admin/registration/seasons"), requireAdminUser()
  ]);
  return <>
    <PageHeader title="Registration" description="Review applications and returning check-ins. Send family decisions separately from the club inbox." />
    <RegistrationWorkspace initial={overview} canCloseout={user.role === "SuperAdmin"} />
  </>;
}
