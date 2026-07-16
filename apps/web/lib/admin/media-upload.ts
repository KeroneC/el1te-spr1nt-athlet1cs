export const MAX_MEDIA_FILES = 20;
export const MAX_MEDIA_FILE_SIZE = 10 * 1024 * 1024;
export const MEDIA_UPLOAD_CONCURRENCY = 3;

const ALLOWED_MEDIA_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);

export function titleFromFileName(fileName: string) {
  const withoutExtension = fileName.replace(/\.[^.]+$/, "");
  const title = withoutExtension.replace(/[-_]+/g, " ").replace(/\s+/g, " ").trim();
  return title || "Untitled image";
}

export function validateMediaFile(file: Pick<File, "size" | "type">) {
  if (!ALLOWED_MEDIA_TYPES.has(file.type)) return "Use a JPEG, PNG, or WebP image.";
  if (file.size > MAX_MEDIA_FILE_SIZE) return "Image must be 10 MB or smaller.";
  return null;
}

export async function runWithConcurrency<T>(items: T[], limit: number, worker: (item: T, index: number) => Promise<void>) {
  let nextIndex = 0;
  async function run() {
    while (nextIndex < items.length) {
      const index = nextIndex++;
      await worker(items[index], index);
    }
  }
  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, run));
}
