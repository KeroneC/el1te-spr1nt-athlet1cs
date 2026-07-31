"use client";

import { useRouter } from "next/navigation";
import { Checkbox, Field, fieldError, FormActions, FormNotice, FormSection, TextArea } from "./form-controls";
import { MediaPicker } from "./media-picker";
import { useAdminMutation } from "@/lib/admin/use-admin-mutation";
import type { AdminHallOfFameInductee, HallOfFameInducteeWriteRequest } from "@/lib/admin/types";
import { nullable, number, text, validateHallOfFameInductee } from "@/lib/admin/validation";

export function HallOfFameInducteeForm({ item }: { item?: AdminHallOfFameInductee }) {
  const router = useRouter();
  const mutation = useAdminMutation<AdminHallOfFameInductee>();

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const yearText = text(data.get("inductionYear"));
    const request: HallOfFameInducteeWriteRequest = {
      name: text(data.get("name")),
      affiliation: text(data.get("affiliation")),
      summary: text(data.get("summary")),
      photoUrl: nullable(data.get("photoUrl")),
      photoAlt: nullable(data.get("photoAlt")),
      inductionYear: yearText ? number(data.get("inductionYear")) : null,
      displayOrder: number(data.get("displayOrder")),
      isActive: data.get("isActive") === "on"
    };
    const result = await mutation.save(
      item ? `/api/admin/hall-of-fame-inductees/${item.id}` : "/api/admin/hall-of-fame-inductees",
      item ? "PUT" : "POST",
      request,
      () => validateHallOfFameInductee(request)
    );
    if (result && !item) router.replace(`/admin/hall-of-fame/${result.id}/edit?saved=created`);
    else if (result) router.refresh();
  }

  return <form onSubmit={submit} noValidate className="space-y-6">
    <FormNotice message={mutation.message} success={mutation.success} referenceId={mutation.referenceId} />
    <FormSection title="Inductee profile" description="The name, affiliation, and story are required for both drafts and active records.">
      <Field label="Name" name="name" required maxLength={200} defaultValue={item?.name} error={fieldError(mutation.errors, "name")} />
      <Field label="Affiliation" name="affiliation" required maxLength={200} defaultValue={item?.affiliation} error={fieldError(mutation.errors, "affiliation")} />
      <Field label="Induction year (optional)" name="inductionYear" type="number" min={1900} max={2100} defaultValue={item?.inductionYear ?? ""} error={fieldError(mutation.errors, "inductionYear")} />
      <Field label="Display order" name="displayOrder" type="number" min={0} defaultValue={item?.displayOrder ?? 0} error={fieldError(mutation.errors, "displayOrder")} />
      <TextArea label="Summary" name="summary" rows={7} required maxLength={2000} defaultValue={item?.summary} error={fieldError(mutation.errors, "summary")} className="sm:col-span-2" />
    </FormSection>
    <FormSection title="Photo and visibility" description="Inactive drafts may omit a photo. Active records require both a photo and meaningful alt text.">
      <div><MediaPicker name="photoUrl" label="Inductee photo" defaultValue={item?.photoUrl ?? ""} error={fieldError(mutation.errors, "photoUrl")} /></div>
      <Field label="Photo alt text" name="photoAlt" minLength={10} maxLength={500} defaultValue={item?.photoAlt ?? ""} error={fieldError(mutation.errors, "photoAlt")} hint="Describe the person and relevant context shown in the photo." />
      <Checkbox name="isActive" label="Active" defaultChecked={item?.isActive ?? true} hint="Active inductees appear on the public Hall of Fame page." />
    </FormSection>
    <FormActions backHref="/admin/hall-of-fame" backLabel="Back to Hall of Fame" submitting={mutation.submitting} editing={Boolean(item)} />
  </form>;
}
