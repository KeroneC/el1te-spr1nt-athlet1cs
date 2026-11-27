import { mkdir } from "node:fs/promises";
import path from "node:path";
import { expect, test, type Locator, type Page } from "@playwright/test";

// Opt in deliberately. Unlike the ordinary browser suite, this walkthrough
// permits one fake submission per journey to the local, non-persisting server.
test.skip(process.env.STARTING_LINE_WALKTHROUGH !== "1", "Screenshot walkthrough requires the local preview fixture.");

const previewOrigin = "http://127.0.0.1:3013";
const previewFolder = path.resolve("../../artifacts/registration-walkthrough");
const fakeEmail = "parent@example.test";
const endpoint = "/api/public/registration/applications";

test.beforeEach(async ({ page, baseURL, request }) => {
  expect(baseURL).toBe(previewOrigin);
  const fixture = await request.get("http://127.0.0.1:5130/api/public/registration");
  expect(await fixture.json()).toMatchObject({ title: "Local design preview · sample season", isOpen: true,
    replyWindow: "Local preview only. No registration is saved or email sent." });
  await page.route(url => url.origin !== previewOrigin, route => route.abort());
  await page.goto("/registration");
  await expect(page.getByRole("link", { name: "Apply for a spot", exact: true })).toBeVisible();
});

async function capture(page: Page, device: string, name: string, panel?: Locator) {
  const dir = path.join(previewFolder, device);
  await mkdir(dir, { recursive: true });
  await page.evaluate(() => document.fonts.ready);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(
    await page.evaluate(() => document.documentElement.clientWidth));
  const options = { path: path.join(dir, name + ".png"), animations: "disabled" as const, scale: "css" as const };
  if (panel) await panel.screenshot({ ...options, style: ".site-header{position:static!important;}" });
  else {
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ ...options, fullPage: false });
  }
}

async function submitAndCapture(page: Page, panel: Locator, device: string, kind: "Application" | "Returning") {
  const application = kind === "Application";
  const start = application ? "07-application" : "10-check-in";
  let unblock!: () => void;
  const pending = new Promise<void>(resolve => { unblock = resolve; });
  const requests: string[] = [];
  await page.route(previewOrigin + endpoint, async route => {
    const req = route.request();
    const data = req.postDataJSON();
    if (req.method() !== "POST" || data.guardianEmail !== fakeEmail || data.kind !== kind || requests.length) {
      await route.abort();
      return;
    }
    requests.push(data.kind);
    await pending;
    await route.continue();
  });
  try {
    await panel.getByRole("button", { name: application ? "Submit application" : "Submit check-in", exact: true }).click();
    await expect(panel.getByRole("button", { name: /Submitting/ })).toBeDisabled();
    await capture(page, device, start + "-submitting", panel);
  } finally { unblock(); }
  await expect(panel.getByRole("heading", { name: application ? "Application received" : "Check-in received" })).toBeVisible();
  await expect(panel.getByRole("heading", { level: 1 })).toHaveCount(1);
  await expect(panel.getByRole("heading", { name: "What happens next", exact: true })).toBeVisible();
  await expect(panel.locator("form")).toHaveCount(0);
  await expect(panel.getByText(/^EL-[0-9A-F]{16}$/)).toBeVisible();
  if (application) {
    const nextSteps = panel.getByRole("heading", { name: "What happens next", exact: true }).locator("..");
    await expect(nextSteps.locator("p")).toHaveText(["The club will follow up with a text, phone call or email."]);
    await expect(panel.getByText("This does not reserve a spot.", { exact: true })).toBeVisible();
  } else {
    await expect(panel.getByText(/No automatic confirmation email is sent/)).toBeVisible();
  }
  const status = panel.getByRole("status");
  await expect(status.filter({ hasText: /received/ })).toBeFocused();
  expect(requests).toEqual([kind]);
  await capture(page, device, application ? "08-application-receipt" : "11-check-in-receipt", panel);
  await panel.getByRole("button", { name: "Copy reference", exact: true }).click();
  await expect(panel.getByText(/Reference copied\.|Copy the reference above manually\./)).toBeVisible();
  await panel.getByRole("button", { name: application ? "Apply for another athlete" : "Check in another athlete", exact: true }).click();
  await expect(panel.locator(application ? "[name=athleteFirstName]" : "[name=guardianEmail]")).toHaveValue("");
}

test("capture the complete new-athlete journey", async ({ page, isMobile }) => {
  test.setTimeout(90_000);
  const device = isMobile ? "phone" : "desktop";
  await capture(page, device, "00-starting-line");
  await page.getByRole("link", { name: "Apply for a spot", exact: true }).click();
  const panel = page.locator("#apply");
  const input = (name: string) => panel.locator(`[name="${name}"]`);
  const next = () => panel.getByRole("button", { name: "Continue", exact: true }).click();

  await expect(panel.getByRole("heading", { name: "Who’s your athlete?" })).toBeVisible();
  await expect(panel.getByText("Choose a date", { exact: true })).toBeVisible();
  await input("athleteFirstName").fill("Avery");
  await input("athleteLastName").fill("Example");
  await input("dateOfBirth").fill("2015-07-03");
  await input("schoolGrade").selectOption("6");
  await input("schoolDistrict").fill("West Allegheny");
  await input("gender").selectOption("Female");
  // Exact age eligibility is checked by the real API when a family submits.
  for (const invalid of ["", "2099-01-01"]) {
    await input("dateOfBirth").fill(invalid);
    await next();
    await expect(input("dateOfBirth")).toHaveAttribute("aria-invalid", "true");
    await expect(input("dateOfBirth")).toBeFocused();
  }
  await input("dateOfBirth").fill("2015-07-03");
  await capture(page, device, "01-athlete", panel);
  await next();

  await input("guardianName").fill("Jordan Example");
  await input("guardianEmail").fill(fakeEmail);
  await input("guardianPhone").fill("412-555-0100");
  await capture(page, device, "02-guardian", panel);
  await next();

  await input("streetAddress").fill("100 Example Lane");
  await input("city").fill("Sample City");
  await expect(input("state")).toHaveValue("PA");
  await input("postalCode").fill("15201");
  await capture(page, device, "03-household", panel);
  await next();

  await input("emergencyContactName").fill("Morgan Example");
  await input("emergencyContactPhone").fill("412-555-0101");
  await input("emergencyContactRelationship").fill("Family friend");
  await capture(page, device, "04-emergency-contact", panel);
  await next();

  await expect(input("availability")).toHaveCount(0);
  await input("experience").fill("One season of recreational track.");
  await input("priorClub").selectOption("Yes");
  await input("clubName").fill("Example Track Club");
  await panel.getByRole("checkbox", { name: "AAU", exact: true }).check();
  await panel.getByRole("checkbox", { name: "USATF", exact: true }).check();
  await input("lastMeetMonth").fill("2022-09");
  await panel.getByRole("heading", { name: "Track background", exact: true }).click();
  await capture(page, device, "05-track-background", panel);
  await next();

  await expect(panel.getByRole("heading", { name: "Ready to send?" })).toBeVisible();
  await panel.getByRole("button", { name: "Edit guardian", exact: true }).click();
  await input("guardianPhone").fill("412-555-0102");
  await panel.getByRole("button", { name: "Return to review", exact: true }).click();
  await expect(panel.getByText("412-555-0102", { exact: true })).toBeVisible();
  await capture(page, device, "06-review-and-submit", panel);
  await submitAndCapture(page, panel, device, "Application");
});

test("capture the returning check-in journey", async ({ page, isMobile }) => {
  const device = isMobile ? "phone" : "desktop";
  await page.getByRole("link", { name: "Returning athlete? Check in", exact: true }).click();
  const panel = page.locator("#returning");
  await expect(panel.locator("[name=dateOfBirth]")).toHaveCount(0);
  await panel.getByLabel("Parent or guardian email").fill(fakeEmail);
  await panel.getByLabel("Athlete’s first name").fill("Avery");
  await capture(page, device, "09-returning-check-in", panel);
  await submitAndCapture(page, panel, device, "Returning");
});
