import AxeBuilder from "@axe-core/playwright";
import { expect, test, type APIRequestContext, type Locator, type Page } from "@playwright/test";
import { signInAsE2eSuperAdmin } from "./helpers/admin-auth";

const adminPath = "/api/admin/registration";
const submitPath = "/api/public/registration/applications";
const responsiveWidths = [340, 390, 768, 1280];
const referencePattern = /^EL-[A-F0-9]{16}$/;
const guardianEmail = "synthetic-registration@example.test";

async function checkLayout(page: Page, active: Locator, widths = responsiveWidths) {
  for (const width of widths) {
    await page.setViewportSize({ width, height: 844 });
    await expect(active).toBeVisible();
    await expect.poll(() => page.evaluate(() =>
      document.documentElement.scrollWidth - document.documentElement.clientWidth
    ), { message: "No horizontal document overflow at " + width + "px" }).toBeLessThanOrEqual(0);
  }
  await page.setViewportSize({ width: 390, height: 844 });
}

async function checkA11y(page: Page, target: string) {
  const scan = await new AxeBuilder({ page }).include(target).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
  expect(scan.violations.filter(result => result.impact === "serious" || result.impact === "critical")).toEqual([]);
}

async function loadSeason(request: APIRequestContext, year: number) {
  const response = await request.get(adminPath + "/seasons");
  expect(response.ok()).toBe(true);
  const overview = await response.json() as { seasons: { year: number; revision: string; isOpen: boolean }[] };
  return overview.seasons.find(season => season.year === year);
}

async function saveReview(page: Page) {
  const saved = page.waitForResponse(response => response.url().endsWith(adminPath + "/review") && response.request().method() === "PUT");
  await page.getByRole("button", { name: "Save review" }).click();
  const response = await saved;
  expect(response.ok(), await response.text()).toBe(true);
  await expect(page.getByText("Review saved. No email was sent.", { exact: true })).toBeVisible();
}

async function choosePath(page: Page, path: "apply" | "returning") {
  const title = path === "apply" ? "Apply for a spot" : "Returning athlete? Check in";
  const link = page.getByRole("link", { name: title, exact: true });
  await link.focus();
  await link.press("Enter");
  await expect(page).toHaveURL(new RegExp("#" + path + "$"));
  const active = page.locator("#" + path);
  await expect(active.getByRole("heading", { name: title, exact: true })).toBeFocused();
  await expect(page.locator(path === "apply" ? "#returning" : "#apply")).toBeHidden();
  return active;
}

async function fillReturning(form: Locator, firstName: string, email = guardianEmail) {
  await form.getByLabel("Parent or guardian email").fill(email);
  await form.getByLabel("Athlete’s first name").fill(firstName);
}

test("families and staff can complete both registration paths through the browser", async ({ page, browser, baseURL }, testInfo) => {
  test.setTimeout(150_000);
  const currentYear = new Date().getUTCFullYear();
  const year = currentYear + 1 + testInfo.retry;
  const openingDate = String(currentYear - 1) + "-01-01";
  const futureOpening = String(year) + "-01-01";
  const endDate = String(year) + "-12-31";
  const title = "E2E Registration Season " + year;
  const family = await browser.newContext({ baseURL, viewport: { width: 390, height: 844 } });
  const publicPage = await family.newPage();
  const stalePage = await family.newPage();
  const submissionRequests: unknown[] = [];
  let staffSignedIn = false;
  let sessionExpired = false;

  await family.addInitScript(() => {
    let copied = false;
    Object.defineProperty(navigator, "clipboard", { configurable: true, value: {
      writeText: async () => {
        if (copied) throw new Error("Synthetic clipboard denial");
        copied = true;
      }
    } });
  });

  try {
    await test.step("staff create, validate and open a season without API shortcuts", async () => {
      await signInAsE2eSuperAdmin(page);
      staffSignedIn = true;
      await page.goto("/admin/registration");
      await page.getByRole("button", { name: "Season Settings", exact: true }).click();
      await page.getByLabel("Season", { exact: true }).selectOption("0");
      for (const [name, value] of [
        ["Season year", String(year)],
        ["Season title", title],
        ["Opening date (Eastern)", futureOpening],
        ["Season end date (Eastern)", futureOpening],
        ["Eligible age groups (one per line)", "12U\n14U"],
        ["Practice location", "Test track"],
        ["Practice schedule", "Saturday mornings"],
        ["Fees", "No fees for this test"],
        ["Expected reply window (shown to families)", "Within one week."],
        ["Designated club paperwork inbox (staff only)", "forms@example.test"],
        ["In-person handoff instructions (staff only)", "Hand forms to staff at the test track."],
        ["Review owner", "Test reviewer"],
        ["Retention owner", "Test owner"]
      ]) await page.getByLabel(name, { exact: true }).fill(value);

      const save = page.getByRole("button", { name: "Save season details" });
      await checkLayout(page, save);
      await checkA11y(page, "main");
      await save.click();
      const badEndDate = page.getByLabel("Season end date (Eastern)", { exact: true });
      await expect(badEndDate).toHaveAttribute("aria-invalid", "true");
      await expect(badEndDate).toHaveAccessibleDescription(/season end must follow opening/i);
      await badEndDate.fill(endDate);
      await save.click();
      await expect(page.getByLabel("Season", { exact: true })).toHaveValue(String(year));

      await publicPage.goto("/registration");
      await expect(publicPage.locator('[data-registration-hub="true"]')).toBeVisible();
      await expect(publicPage.getByRole("heading", { name: "Applications open soon" })).toBeVisible();
      await expect(publicPage.getByRole("link", { name: "Apply for a spot", exact: true })).toHaveCount(0);
      await expect(page.getByRole("button", { name: "Open intake", exact: true })).toBeDisabled();
      await page.getByLabel("Opening date (Eastern)", { exact: true }).fill(openingDate);
      const updated = page.waitForResponse(response => response.url().endsWith(adminPath + "/season") && response.request().method() === "PUT");
      await save.click();
      expect((await updated).ok()).toBe(true);
      await expect(save).toBeEnabled();
      await publicPage.reload();
      await expect(publicPage.getByRole("heading", { name: "Applications are currently closed" })).toBeVisible();
      await expect(publicPage.getByText(/planned opening:/i)).toHaveCount(0);
      await expect(publicPage.getByRole("link", { name: "Apply for a spot", exact: true })).toHaveCount(0);
      expect((await publicPage.request.post(adminPath + "/queue", { data: { year, kind: "Application" } })).status()).toBe(401);

      const open = page.getByRole("button", { name: "Open intake", exact: true });
      await expect(open).toBeDisabled();
      await page.getByRole("checkbox", { name: /club approved season details/i }).check();
      await open.click();
      await expect(page.getByRole("button", { name: "Close intake", exact: true })).toBeVisible();
      await expect(save).toBeDisabled();
      expect((await loadSeason(page.request, year))?.isOpen).toBe(true);
    });

    let payload: Record<string, unknown> | undefined;
    let appReference = "";
    await test.step("families can switch paths, recover from validation and retry the exact save", async () => {
      await publicPage.goto("/registration?src=instagram");
      await expect(publicPage.locator('[data-registration-hub="true"]')).toBeVisible();
      await expect(publicPage.getByRole("heading", { name: "Applications are open" })).toBeVisible();
      await expect(publicPage.locator("#apply")).toBeHidden();
      await expect(publicPage.locator("#returning")).toBeHidden();
      const application = await choosePath(publicPage, "apply");
      const form = application.locator("form");
      await form.getByLabel("Parent or guardian name").fill("Synthetic Guardian");

      const returning = await choosePath(publicPage, "returning");
      await returning.getByLabel("Parent or guardian email").fill(guardianEmail);
      await publicPage.goBack();
      await expect(publicPage).toHaveURL(/#apply$/);
      await expect(form.getByLabel("Parent or guardian name")).toHaveValue("Synthetic Guardian");
      await expect(returning).toBeHidden();

      await form.getByLabel("Parent or guardian email").fill(guardianEmail);
      const phone = form.getByLabel("Parent or guardian phone (optional)");
      await phone.fill("123");
      await form.getByLabel("Athlete’s first name").fill("Jordan");
      await form.getByLabel("Age group").selectOption("12U");
      await form.getByLabel("Can your athlete attend the listed practices?").selectOption("Yes");
      await form.getByLabel("Has your athlete trained or competed with another track and field club?").selectOption("Yes");
      await form.getByLabel("Current or most recent club").fill("Synthetic Track");
      const aau = form.getByRole("checkbox", { name: "AAU", exact: true });
      const usatf = form.getByRole("checkbox", { name: "USATF", exact: true });
      const unsure = form.getByRole("checkbox", { name: "Not sure", exact: true });
      await aau.check();
      await usatf.check();
      await unsure.check();
      await expect(aau).not.toBeChecked();
      await expect(usatf).not.toBeChecked();
      await unsure.uncheck();
      await aau.check();
      await usatf.check();
      await checkLayout(publicPage, form);
      await checkA11y(publicPage, "#apply");

      const submit = form.getByRole("button", { name: "Submit application", exact: true });
      await submit.click();
      await expect(phone).toBeFocused();
      await expect(phone).toHaveAttribute("aria-invalid", "true");
      await expect(phone).toHaveAccessibleDescription(/enter a valid phone number/i);
      await checkLayout(publicPage, form.getByRole("alert"));
      await phone.fill("");

      // Commit the first attempt normally, but drop its response to simulate a
      // network failure after the SQL save. Only the identical retry may succeed.
      let forwardedStatus = 0;
      let loseFirstReceipt = true;
      await publicPage.route("**" + submitPath, async route => {
        submissionRequests.push(route.request().postDataJSON());
        if (!loseFirstReceipt) return route.continue();
        loseFirstReceipt = false;
        const persisted = await route.fetch();
        forwardedStatus = persisted.status();
        await route.abort("failed");
      });
      await submit.evaluate(button => {
        // Fire both clicks before React has time to dismiss the first one.
        const submitButton = button as HTMLButtonElement;
        submitButton.click();
        submitButton.click();
      });
      const retry = form.getByRole("button", { name: "Retry submission" });
      await expect(retry).toBeVisible();
      expect(forwardedStatus).toBe(201);
      expect(submissionRequests).toHaveLength(1);
      await expect(form.getByLabel("Parent or guardian email")).toBeDisabled();
      await retry.click();
      await expect(application.getByRole("heading", { name: "Application received" })).toBeVisible();
      expect(submissionRequests).toHaveLength(2);
      expect(submissionRequests[0]).toEqual(submissionRequests[1]);
      payload = submissionRequests[0] as Record<string, unknown>;
      appReference = await application.locator("strong").innerText();
      expect(appReference).toMatch(referencePattern);
      expect(publicPage.url()).not.toContain(appReference);
      expect(publicPage.url()).not.toContain(guardianEmail);
      await expect(application.getByText(/no automatic confirmation email is sent/i)).toBeVisible();
      await expect(application.getByText(/wait for an offer before sending completed paperwork/i)).toBeVisible();
      await application.getByRole("button", { name: "Copy reference" }).click();
      await expect(application.getByText("Reference copied.", { exact: true })).toBeVisible();
      await checkLayout(publicPage, application.getByRole("heading", { name: "Application received" }));
      await application.getByRole("button", { name: "Apply for another athlete" }).click();
      await expect(form.getByLabel("Parent or guardian name")).toBeFocused();

      await choosePath(publicPage, "returning");
      await expect(returning.getByLabel("Parent or guardian email")).toHaveValue(guardianEmail);
      await expect(returning.getByLabel("Parent or guardian name")).toHaveCount(0);
      await fillReturning(returning, "Morgan");
      await checkLayout(publicPage, returning.locator("form"));
      await checkA11y(publicPage, "#returning");
      await returning.getByRole("button", { name: "Submit check-in" }).click();
      await expect(returning.getByRole("heading", { name: "Check-in received" })).toBeVisible();
      await expect(returning.getByText("Returning athletes do not need to submit paperwork.", { exact: true })).toBeVisible();
      await expect(returning.getByText(/no automatic confirmation email is sent/i)).toBeVisible();
      expect(await returning.locator("strong").innerText()).toMatch(referencePattern);
      await returning.getByRole("button", { name: "Copy reference" }).click();
      await expect(returning.getByText("Copy the reference above manually.", { exact: true })).toBeVisible();
      await publicPage.unroute("**" + submitPath);

      // Direct links should choose the right visible form after hydration.
      await stalePage.goto("/registration#returning");
      await expect(stalePage.locator("#returning form")).toBeVisible();
      await expect(stalePage.locator("#apply")).toBeHidden();
      await fillReturning(stalePage.locator("#returning"), "AfterClosure", "closed-form@example.test");

      // Seed just enough synthetic rows for the staff pagination scenario.
      const records = Array.from({ length: 21 }, (_, i) => ({
        ...payload,
        requestKey: crypto.randomUUID(),
        guardianEmail: "page-test-" + i + "@example.test",
        athleteFirstName: "PageTest" + String(i).padStart(2, "0"),
        priorClub: "No",
        clubName: null,
        affiliations: []
      }));
      for (let start = 0; start < records.length; start += 5) {
        const responses = await Promise.all(records.slice(start, start + 5).map(data => publicPage.request.post(submitPath, { data })));
        expect(responses.map(response => response.status())).toEqual(responses.map(() => 201));
      }
    });

    await test.step("staff can navigate errors, filters, pages and both review flows", async () => {
      await page.setViewportSize({ width: 390, height: 844 });
      await expect(page.getByRole("navigation", { name: "Admin navigation" })).toHaveCount(0);
      const menu = page.getByRole("button", { name: "Open navigation" });
      await menu.press("Enter");
      await expect(page.getByRole("navigation", { name: "Admin navigation" })).toBeVisible();
      await page.keyboard.press("Escape");
      await expect(page.getByRole("navigation", { name: "Admin navigation" })).toHaveCount(0);
      await expect(menu).toBeFocused();

      await page.route("**" + adminPath + "/queue", route => route.fulfill({ status: 503, json: {
        message: "Synthetic queue outage", referenceId: "ESA-1234567890ABCDEF"
      } }));
      await page.getByRole("button", { name: "New Applications", exact: true }).click();
      await expect(page.getByText(/synthetic queue outage/i)).toBeVisible();
      await expect(page.getByText("ESA-1234567890ABCDEF", { exact: true })).toBeVisible();
      await page.unroute("**" + adminPath + "/queue");
      await page.getByRole("button", { name: "Retry", exact: true }).click();

      const search = page.getByLabel("Search name, email, or reference");
      const applyFilters = page.getByRole("button", { name: "Apply filters", exact: true });
      const queueHeading = page.getByRole("heading", { name: "New Applications", exact: true });
      await checkLayout(page, queueHeading);
      await checkA11y(page, "main");
      await search.fill("NoSuchAthlete");
      await applyFilters.click();
      await expect(page.getByText(/no matching records/i)).toBeVisible();
      const clearFilters = page.getByRole("button", { name: "Clear filters", exact: true });
      await clearFilters.click();
      await expect(search).toHaveValue("");
      await expect(clearFilters).toBeDisabled();
      await search.fill("PageTest");
      await page.getByLabel("Age group", { exact: true }).selectOption("12U");
      await page.getByLabel("Status", { exact: true }).selectOption("New");
      await page.getByLabel("Transfer review", { exact: true }).selectOption("NotNeeded");
      await applyFilters.click();
      await expect(page.getByText("Page 1 of 2", { exact: true })).toBeVisible();
      const pagination = page.getByRole("navigation", { name: "Registration queue pages" });
      await pagination.getByRole("button", { name: "Next" }).click();
      await expect(pagination.getByText("Page 2 of 2", { exact: true })).toBeVisible();
      const secondPageRow = page.getByRole("button", { name: /^Review PageTest\d{2}$/ });
      const rowName = await secondPageRow.innerText();
      await secondPageRow.click();
      await page.getByRole("button", { name: "Back to queue" }).click();
      await expect(search).toHaveValue("PageTest");
      await expect(page.getByLabel("Age group", { exact: true })).toHaveValue("12U");
      await expect(page.getByLabel("Status", { exact: true })).toHaveValue("New");
      await expect(page.getByLabel("Transfer review", { exact: true })).toHaveValue("NotNeeded");
      await expect(pagination.getByText("Page 2 of 2", { exact: true })).toBeVisible();
      await expect(page.getByRole("button", { name: rowName, exact: true })).toBeFocused();
      await pagination.getByRole("button", { name: "Previous" }).click();
      await expect(pagination.getByText("Page 1 of 2", { exact: true })).toBeVisible();
      await clearFilters.click();
      await search.fill(appReference);
      await applyFilters.click();
      expect(page.url()).not.toContain(appReference);
      await page.getByRole("button", { name: "Review Jordan", exact: true }).click();

      const applicationStatus = page.getByLabel("Application status", { exact: true });
      const transfer = page.getByLabel("Transfer review", { exact: true });
      const photoConsent = page.getByLabel("Optional photo consent", { exact: true });
      const ready = page.getByRole("checkbox", { name: "Competition-ready" });
      await expect(page.getByRole("heading", { name: new RegExp("Jordan.*" + appReference) })).toBeFocused();
      await expect(ready).toBeDisabled();
      await checkLayout(page, page.getByRole("button", { name: "Save review" }));
      await checkA11y(page, "main");

      await applicationStatus.selectOption("Offered");
      await saveReview(page);
      await expect(page.getByRole("heading", { name: "Paperwork instructions for staff to send" })).toBeVisible();
      const queueResponse = await page.request.post(adminPath + "/queue", { data: { year, kind: "Application", search: appReference } });
      expect(queueResponse.ok()).toBe(true);
      const queue = await queueResponse.json();
      expect(queue.count).toBe(1);
      const id = queue.items[0].id as string;
      const detail = async () => (await page.request.post(adminPath + "/detail", { data: { id } })).json();
      const otherReviewer = await detail();
      expect((await page.request.put(adminPath + "/review", { data: {
        id, revision: otherReviewer.revision, status: "Offered", transfer: "NeedsReview", photoConsent: "Granted", competitionReady: false
      } })).ok()).toBe(true);

      await applicationStatus.selectOption("PaperworkReceived");
      await page.getByRole("button", { name: "Save review" }).click();
      await expect(page.getByText(/another reviewer made a change/i)).toBeVisible();
      await page.getByRole("button", { name: "Reload record" }).click();
      await expect(photoConsent).toHaveValue("Granted");
      await applicationStatus.selectOption("PaperworkReceived");
      await photoConsent.selectOption("Declined");
      await saveReview(page);
      await applicationStatus.selectOption("Enrolled");
      await expect(ready).toBeDisabled();
      await expect(ready).not.toBeChecked();
      await saveReview(page); // Enrollment itself does not depend on photo consent or transfer.
      const unresolved = await detail();
      expect(unresolved).toMatchObject({ status: "Enrolled", transfer: "NeedsReview", photoConsent: "Declined", competitionReady: false });
      expect((await page.request.put(adminPath + "/review", { data: {
        id, revision: unresolved.revision, status: "Enrolled", transfer: "NeedsReview", photoConsent: "Declined", competitionReady: true
      } })).status()).toBe(400);
      await transfer.selectOption("Cleared");
      await ready.check();
      await saveReview(page);
      expect(await detail()).toMatchObject({ competitionReady: true, photoConsent: "Declined" });
      await applicationStatus.selectOption("Closed");
      await expect(ready).toBeDisabled();
      await expect(ready).not.toBeChecked();

      await page.getByRole("button", { name: "Returning Check-Ins", exact: true }).click();
      await page.getByRole("button", { name: "Review Morgan" }).click();
      const returningStatus = page.getByLabel("Check-in status");
      await returningStatus.selectOption("Offered");
      await saveReview(page);
      await expect(returningStatus.getByRole("option", { name: "Paperwork received" })).toHaveCount(0);
      await expect(page.getByRole("heading", { name: "Paperwork instructions for staff to send" })).toHaveCount(0);
      await returningStatus.selectOption("Enrolled");
      await page.route("**" + adminPath + "/detail", route => route.fulfill({ status: 503, json: { message: "Synthetic follow-up read error" } }));
      await page.getByRole("button", { name: "Save review" }).click();
      await expect(page.getByText(/review saved/i)).toBeVisible();
      const reload = page.getByRole("button", { name: "Reload record" });
      await expect(reload).toBeVisible();
      await page.unroute("**" + adminPath + "/detail");
      await reload.click();
      await expect(returningStatus).toHaveValue("Enrolled");
      await expect(page.getByRole("heading", { name: "Paperwork instructions for staff to send" })).toHaveCount(0);
    });

    await test.step("closing intake blocks stale forms while saved receipts stay recoverable", async () => {
      await page.getByRole("button", { name: "Season Settings", exact: true }).click();
      await page.getByRole("button", { name: "Close intake", exact: true }).click();
      await expect(page.getByRole("button", { name: "Open intake", exact: true })).toBeVisible();
      expect((await loadSeason(page.request, year))?.isOpen).toBe(false);
      const staleReturning = stalePage.locator("#returning");
      await staleReturning.getByRole("button", { name: "Submit check-in" }).click();
      await expect(staleReturning.getByRole("alert")).toContainText(/applications are not open/i);
      await expect(staleReturning.getByRole("button", { name: "Submit check-in" })).toHaveCount(0);
      await staleReturning.getByRole("link", { name: "Check registration status" }).click();
      await expect(stalePage.locator('[data-registration-hub="true"]')).toBeVisible();
      await expect(stalePage.getByRole("heading", { name: "Applications are currently closed" })).toBeVisible();
      expect(payload).toBeDefined();
      const replay = await publicPage.request.post(submitPath, { data: payload });
      expect(replay.status()).toBe(200);
      expect(await replay.json()).toEqual({ reference: appReference });

      await page.route("**" + adminPath + "/queue", route => route.fulfill({ status: 401, json: { message: "Synthetic expired session" } }));
      await page.getByRole("button", { name: "Returning Check-Ins", exact: true }).click();
      await expect(page).toHaveURL(/\/admin\/login\?reason=expired$/);
      sessionExpired = true;
    });
  } finally {
    if (staffSignedIn && !sessionExpired) {
      const season = await loadSeason(page.request, year);
      if (season?.isOpen) {
        const closed = await page.request.put(adminPath + "/opening", { data: { year, revision: season.revision, open: false, launchChecksApproved: false } });
        expect(closed.ok(), await closed.text()).toBe(true);
      }
    }
    await family.close();
  }
});

test("blank registration downloads stay available and individually named", async ({ page }) => {
  await page.goto("/forms");
  const directLinks = page.getByRole("link", { name: /download .*(athlete registration|waiver|photo consent)/i });
  await expect(directLinks).toHaveCount(3);
  const hrefs = await directLinks.evaluateAll(items => items.map(item => (item as HTMLAnchorElement).href));
  expect(new Set(hrefs).size).toBe(3);
  for (const href of hrefs) {
    const response = await page.request.get(href);
    expect(response.ok()).toBe(true);
    expect(response.headers()["content-type"]).toContain("application/pdf");
    expect((await response.body()).subarray(0, 4).toString()).toBe("%PDF");
  }
  const downloadEvent = page.waitForEvent("download");
  await directLinks.first().click();
  const download = await downloadEvent;
  expect(download.suggestedFilename()).toMatch(/\.pdf$/i);
  expect(await download.failure()).toBeNull();
  await page.goto("/registration");
  await expect(page.getByRole("link", { name: /download .*athlete registration/i })).toBeVisible();
  await page.getByRole("link", { name: "All forms", exact: true }).click();
  await expect(page).toHaveURL(/\/forms$/);
  await page.goto("/faqs");
  await page.getByText("How do I register my child?", { exact: true }).click();
  await expect(page.getByText(/returning athletes.*no paperwork is required/i)).toBeVisible();
});
