export function isEnabledSetting(value: string | undefined) {
  return value?.trim().toLowerCase() === "true";
}
