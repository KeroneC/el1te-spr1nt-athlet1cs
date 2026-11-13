import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { signInAsE2eSuperAdmin } from "./helpers/admin-auth";

const base = "/api/admin/registration";
const endpoint = "/api/public/registration/applications";

test("registration saves both family paths, deduplicates retries, and keeps unresolved transfers out of competition", async ({ page, browser }, testInfo) => {
  test.setTimeout(100_000);
  await signInAsE2eSuperAdmin(page);
  const today = new Date().toISOString().slice(0, 10);
  const currentYear = Number(today.slice(0, 4));
  const year = currentYear + 1 + testInfo.retry;
  const settings = {
    year, title: "E2E registration", openingDate: `${currentYear - 1}-01-01`, endDate: `${year}-12-31`,
    ageGroups: ["12U"], practiceLocation: "Test track", practiceSchedule: "Saturday mornings",
    fees: "No fees for this test", replyWindow: "Within one week.", paperworkEmail: "forms@example.test",
    paperworkHandoff: "Hand forms to staff at the test track.", reviewOwner: "Test reviewer", retentionOwner: "Test owner"
  };
  let season: { revision: string; isOpen: boolean } | null = null;
  const family = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const publicPage = await family.newPage();
  try {
    const saved = await page.request.put(`${base}/season`, { data: { ...settings, revision: null } });
    expect(saved.ok(), await saved.text()).toBe(true);
    season = await saved.json();
    await publicPage.goto("/registration");
    await expect(publicPage.getByRole("heading", { name: "Applications are currently closed" })).toBeVisible();
    expect((await publicPage.request.post(`${base}/queue`, { data: { year, kind: "Application" } })).status()).toBe(401);
    const start = await page.request.put(`${base}/opening`, { data: { year, revision: season!.revision, open: true, launchChecksApproved: true } });
    expect(start.ok(), await start.text()).toBe(true);
    season = await start.json();

    await publicPage.goto("/registration?src=instagram");
    const form = publicPage.locator("#apply form");
    const link = publicPage.getByRole("link", { name: "Apply for a spot", exact: true });
    await link.focus();
    await publicPage.keyboard.press("Enter");
    await expect(publicPage).toHaveURL(/#apply$/);
    const guardianEmail = "synthetic-registration@example.test";
    await form.getByLabel("Parent or guardian name").fill("Synthetic Guardian");
    await form.getByLabel("Parent or guardian email").fill(guardianEmail);
    await form.getByLabel("Athlete’s first name").fill("Jordan");
    await form.getByLabel("Age group").selectOption("12U");
    await form.getByLabel("Can your athlete attend the listed practices?").selectOption("Yes");
    await form.getByLabel("Has your athlete trained or competed with another track and field club?").selectOption("Yes");
    await form.getByLabel("Current or most recent club").fill("Synthetic Track");
    await form.getByRole("checkbox", { name: "AAU" }).check();
    await form.getByRole("checkbox", { name: "USATF" }).check();
    const a11y = await new AxeBuilder({ page: publicPage }).include("#apply").include("#returning").analyze();
    expect(a11y.violations).toEqual([]);
    const posted = publicPage.waitForRequest(request => request.url().endsWith(endpoint) && request.method() === "POST");
    await form.getByRole("button", { name: "Submit application" }).click();
    const payload = (await posted).postDataJSON();
    await expect(publicPage.getByRole("heading", { name: "Application received" })).toBeVisible();
    const reference = await publicPage.locator("#apply strong").innerText();
    expect(reference).toMatch(/^EL-[A-F0-9]{16}$/);
    const retries = await Promise.all([1, 2].map(() => publicPage.request.post(endpoint, { data: payload })));
    for (const retry of retries) {
      expect(retry.status()).toBe(200);
      expect(await retry.json()).toEqual({ reference });
    }
    const siblingPayload = { ...payload, requestKey: crypto.randomUUID(), athleteFirstName: "Casey", priorClub: "No" };
    const siblings = await Promise.all([1, 2].map(() => publicPage.request.post(endpoint, { data: siblingPayload })));
    expect(siblings.map(result => result.status()).sort()).toEqual([200, 201]);
    expect(await siblings[0].json()).toEqual(await siblings[1].json());

    const returning = publicPage.locator("#returning form");
    await returning.getByLabel("Parent or guardian email").fill(guardianEmail);
    await returning.getByLabel("Athlete’s first name").fill("Morgan");
    await returning.getByRole("button", { name: "Submit check-in" }).click();
    await expect(publicPage.getByRole("heading", { name: "Check-in received" })).toBeVisible();
    await expect(publicPage.locator("#returning").getByRole("status")).toContainText("Returning athletes do not need to submit paperwork.");
    await expect(publicPage.locator("#apply").getByRole("status")).toContainText("wait for an offer before sending completed paperwork");
    for (const width of [390, 340]) {
      await publicPage.setViewportSize({ width, height: 844 });
      expect(await publicPage.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
    }
    await page.goto("/admin/registration");
    await expect(page.getByRole("heading", { name: "New Applications" })).toBeVisible();
    await page.getByRole("button", { name: "Review Jordan" }).click();
    const queue = await (await page.request.post(`${base}/queue`, { data: { year, kind: "Application", search: reference } })).json();
    expect(queue.count).toBe(1);
    const id = queue.items[0].id;
    const detail = async () => (await page.request.post(`${base}/detail`, { data: { id } })).json();
    const review = async (status: string, competitionReady: boolean, transfer = "NeedsReview") => {
      const current = await detail();
      return page.request.put(`${base}/review`, { data: { id, revision: current.revision, status, transfer, photoConsent: "Declined", competitionReady } });
    };
    expect((await review("Offered", false)).ok()).toBe(true);
    expect((await review("PaperworkReceived", false)).ok()).toBe(true);
    expect((await review("Enrolled", false)).ok()).toBe(true);
    expect((await review("Enrolled", true)).status()).toBe(400);
    expect((await review("Enrolled", true, "Cleared")).ok()).toBe(true);
    expect(await detail()).toMatchObject({ competitionReady: true, photoConsent: "Declined" });
    await page.getByRole("button", { name: "Returning Check-Ins" }).click();
    await page.getByRole("button", { name: "Review Morgan" }).click();
    const checkInStatus = page.getByLabel("Check-in status");
    await checkInStatus.selectOption("Offered");
    await page.getByRole("button", { name: "Save review" }).click();
    await expect(checkInStatus).toHaveValue("Offered");
    await expect(checkInStatus.getByRole("option", { name: "Paperwork received" })).toHaveCount(0);
    await expect(page.getByRole("heading", { name: "Paperwork instructions for staff to send" })).toHaveCount(0);
    await checkInStatus.selectOption("Enrolled");
    await page.getByRole("button", { name: "Save review" }).click();
    await expect(page.getByRole("listitem").filter({ hasText: / · Enrolled(?: · Competition-ready)?$/ })).toBeVisible();
    const closed = await page.request.put(`${base}/opening`, { data: { year, revision: season!.revision, open: false, launchChecksApproved: false } });
    expect(closed.ok(), await closed.text()).toBe(true);
    season = await closed.json();
    expect((await publicPage.request.post(endpoint, { data: { ...payload, requestKey: crypto.randomUUID() } })).status()).toBe(409);
    expect(await (await publicPage.request.post(endpoint, { data: payload })).json()).toEqual({ reference });
  } finally {
    if (season?.isOpen) await page.request.put(`${base}/opening`, { data: { year, revision: season.revision, open: false, launchChecksApproved: false } });
    await family.close();
  }
});
