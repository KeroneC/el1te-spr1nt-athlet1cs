import { promises as fs } from "node:fs";
import path from "node:path";
import { expect, type Page } from "@playwright/test";

const mailRoot = path.resolve(process.cwd(), "../../artifacts/e2e/mail");
const email = "e2e.admin@example.test";
const password = "E2eAdmin!2026Pass";

async function newestVerificationCode(startedAt: number): Promise<string | null> {
  const names = await fs.readdir(mailRoot).catch(() => [] as string[]);
  const candidates = await Promise.all(names.map(async name => {
    const file = path.join(mailRoot, name);
    const stat = await fs.stat(file);
    return { file, modifiedAt: stat.mtimeMs };
  }));

  for (const candidate of candidates
    .filter(value => value.modifiedAt >= startedAt - 1_000)
    .sort((left, right) => right.modifiedAt - left.modifiedAt)) {
    const content = await fs.readFile(candidate.file, "utf8");
    if (!content.includes(`To: ${email}`)) continue;
    return content.match(/verification code is (\d{6})/i)?.[1] ?? null;
  }
  return null;
}

export async function signInAsE2eSuperAdmin(page: Page) {
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill(email);
  await page.locator("#password").fill(password);
  const startedAt = Date.now();
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByLabel("Verification code")).toBeVisible();
  await expect.poll(() => newestVerificationCode(startedAt)).not.toBeNull();
  const code = await newestVerificationCode(startedAt);
  expect(code).toMatch(/^\d{6}$/);
  await page.getByLabel("Verification code").fill(code!);
  await page.getByRole("button", { name: "Verify and sign in" }).click();
  await expect(page).toHaveURL(/\/admin$/);
}
