import { expect, test } from "@playwright/test";

test("SuperAdmin can invite an Admin whose accepted account cannot manage access", async ({ page }) => {
  test.setTimeout(60_000);
  const suffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `invited.admin.${suffix}@example.test`;
  const password = "InvitedAdmin!2026";

  await page.goto("/admin/login");
  await page.getByLabel("Email").fill("e2e.admin@example.test");
  await page.locator("#password").fill("E2eAdmin!2026Pass");
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/admin$/);

  await page.getByRole("link", { name: "Access control" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Access control" })).toBeVisible();
  const inviteForm = page.getByRole("form", { name: "Invite administrator" });
  await inviteForm.getByLabel("First name").fill("Invited");
  await inviteForm.getByLabel("Last name").fill("Admin");
  await inviteForm.getByLabel("Email").fill(email);
  await inviteForm.getByLabel("Role").selectOption("Admin");
  const invitationResponse = page.waitForResponse(response => response.url().endsWith("/api/admin/invitations") && response.request().method() === "POST");
  await inviteForm.getByRole("button", { name: "Create invitation" }).click();
  expect((await invitationResponse).status()).toBe(201);
  const invitationUrl = await page.getByLabel("Invitation link").inputValue();

  await page.goto(invitationUrl);
  await expect(page.getByText(email)).toBeVisible();
  await page.getByLabel("Create password").fill(password);
  await page.getByLabel("Confirm password").fill(password);
  await page.getByRole("button", { name: "Accept invitation" }).click();
  await expect(page.getByRole("heading", { name: "Your Admin account is ready" })).toBeVisible();
  await page.context().clearCookies();
  await page.getByRole("link", { name: "Continue to sign in" }).click();

  await page.getByLabel("Email").fill(email);
  await page.locator("#password").fill(password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/\/admin$/);
  await expect(page.getByRole("link", { name: "Access control" })).toHaveCount(0);

  await page.goto("/admin/users");
  await expect(page).toHaveURL(/\/admin\/access-denied$/);
});
