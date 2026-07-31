import { HallOfFameInducteeForm } from "@/components/admin/hall-of-fame-inductee-form";
import { PageHeader } from "@/components/admin/page-header";

export default function Page() {
  return <><PageHeader title="Create Hall of Fame inductee" description="Add an inductee as a draft or publish a complete profile." /><HallOfFameInducteeForm /></>;
}
