import AxeBuilder from "@axe-core/playwright";
import { mkdir } from "node:fs/promises";
import path from "node:path";
import { expect, test, type Page } from "@playwright/test";

const publicPath = "/registration";
const applicationPost = "**/api/public/registration/applications";
const receipt = "EL-FA00000000000001";
const field = (page: Page, name: string) => page.locator(`#apply [name="${name}"]:visible`);
const next = (page: Page) => page.getByRole("button", { name: "Continue", exact: true }).click();
const edit = (page: Page, name: RegExp) =>
  page.getByRole("link", { name }).or(page.getByRole("button", { name }));

test.beforeEach(async ({ page, baseURL }) => {
  // Never let a submission through to any backend, even if a test forgot its mock.
  await page.route(applicationPost, route => route.fulfill({ status: 503, json: { message: "Synthetic preview: no persistence." } }));
  await page.route("**/api/public/registration/visit", route => route.fulfill({ status: 204 }));
  await page.route(url => ["http:", "https:"].includes(url.protocol) && url.origin !== baseURL, route => route.abort());
});

async function checkAxe(page: Page) {
  const result = await new AxeBuilder({ page }).include("[data-registration-hub]")
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
  expect(result.violations.map(v => ({ id: v.id, targets: v.nodes.map(n => n.target) }))).toEqual([]);
}

async function toReview(page: Page, priorClub = false) {
  await field(page, "athleteFirstName").fill("Avery");
  await field(page, "athleteLastName").fill("Fixture");
  await field(page, "dateOfBirth").fill("2015-07-03");
  await field(page, "schoolGrade").selectOption("Not applicable");
  await field(page, "schoolDistrict").selectOption("West Allegheny");
  await field(page, "gender").selectOption("Female");
  await next(page);
  await field(page, "guardianName").fill("Synthetic parent");
  await field(page, "guardianEmail").fill("parent@example.test");
  await field(page, "guardianPhone").fill("412-555-0100");
  await next(page);
  await field(page, "streetAddress").fill("123 Fixture Lane");
  await field(page, "city").fill("Sample City");
  await expect(field(page, "state")).toHaveValue("PA");
  await field(page, "postalCode").fill("15201");
  await next(page);
  await field(page, "emergencyContactName").fill("Fixture contact");
  await field(page, "emergencyContactPhone").fill("412-555-0199");
  await field(page, "emergencyContactRelationship").fill("Family friend");
  await next(page);
  await expect(field(page, "availability")).toHaveCount(0);
  await expect(field(page, "lastMeetMonth")).toHaveCount(0);
  await field(page, "priorClub").selectOption(priorClub ? "Yes" : "No");
  if (priorClub) {
    await field(page, "clubName").fill("Synthetic Track Club");
    await page.locator("#apply").getByRole("checkbox", { name: "AAU", exact: true }).check();
    const month = page.locator("#apply").getByLabel("Last meet month and year", { exact: true });
    await expect(month).toHaveAttribute("required", "");
    await next(page);
    await expect(month).toBeFocused();
    await expect(month).toHaveAccessibleDescription(/enter the last meet month and year/i);
    await month.fill("2022-09");
    await page.locator("#apply").getByRole("heading", { name: "Track background", exact: true }).click();
  } else {
    await expect(field(page, "lastMeetMonth")).toHaveCount(0);
  }
  await next(page);
  await expect(page).toHaveURL(/#apply\/review$/);
}

test("requires the last meet month after a previous club and carries it to review", async ({ page }) => {
  await page.goto(publicPath + "#apply");
  await toReview(page, true);
  await expect(page.locator("#apply").getByText("2022-09", { exact: true })).toBeVisible();
});

test("new application supports an unlisted district, requires gender, and lets families change PA", async ({ page }) => {
  await page.goto(publicPath + "#apply");
  await field(page, "athleteFirstName").fill("Avery");
  await field(page, "athleteLastName").fill("Fixture");
  await field(page, "dateOfBirth").fill("2015-07-03");
  await field(page, "schoolGrade").selectOption("6");
  const district = field(page, "schoolDistrict");
  await expect(district.locator('option[value="Cornell"]')).toHaveCount(1);
  await district.selectOption({ label: "Other / not listed" });
  const other = field(page, "schoolDistrictOther");
  await next(page);
  await expect(other).toBeFocused();
  await expect(other).toHaveAccessibleDescription(/enter a school district name/i);
  await other.fill("Example District");
  const gender = field(page, "gender");
  await expect(gender.locator('option:not([value=""])')).toHaveText(["Female", "Male"]);
  await next(page);
  await expect(gender).toBeFocused();
  await expect(gender).toHaveAccessibleDescription("Choose Female or Male.");
  await gender.selectOption("Male");
  await next(page);
  await field(page, "guardianName").fill("Synthetic parent");
  await field(page, "guardianEmail").fill("parent@example.test");
  await field(page, "guardianPhone").fill("412-555-0100");
  await next(page);
  const state = field(page, "state");
  await expect(state).toHaveValue("PA");
  await state.selectOption("OH");
  await page.goBack();
  await page.goForward();
  await expect(state).toHaveValue("OH");
});

test("all four widths: both choices, one-tap navigation, contrast, no overflow and lean imagery", async ({ page, isMobile }) => {
  test.setTimeout(90_000);
  for (const width of [340, 390, 768, 1440]) {
    await page.setViewportSize({ width, height: width <= 390 ? 740 : 900 });
    await page.goto(publicPath);
    const apply = page.getByRole("link", { name: "Apply for a spot", exact: true });
    const returning = page.getByRole("link", { name: "Returning athlete? Check in", exact: true });
    await expect(apply).toBeVisible();
    await expect(returning).toBeVisible();
    const resources = page.getByRole("complementary", { name: "Registration resources" });
    const disclosure = resources.locator("summary");
    await expect(disclosure).toHaveText("Season details");
    await expect(resources.getByText("Practice location", { exact: true })).toBeHidden();
    if (width === 390) {
      await disclosure.click();
      await expect(resources.getByText("Practice location", { exact: true })).toBeVisible();
      await disclosure.click();
      await page.evaluate(() => window.scrollTo(0, 0));
    }
    await expect(page.locator("#registration-paths")).toHaveCSS("background-color", "rgb(255, 255, 255)");
    const images = page.locator('#registration-paths picture img:visible');
    const archive = process.env.STARTING_LINE_ARCHIVE_PREVIEW === "true";
    await expect(images).toHaveCount(archive ? 2 : 1);
    await expect.poll(() => images.evaluateAll(els => els.every(el =>
      el instanceof HTMLImageElement && el.complete && el.naturalWidth > 0))).toBe(true);
    expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(0);
    if (width <= 390) {
      const resources = await page.evaluate(() => performance.getEntriesByType("resource")
        .filter((r): r is PerformanceResourceTiming => r instanceof PerformanceResourceTiming && (r.initiatorType === "img" || /\.(webp|svg|png)(\?|$)/i.test(r.name)))
        .map(r => ({ name: r.name, bytes: r.encodedBodySize })));
      for (const photo of archive ? ["starting-blocks", "team-huddle"] : ["meet-community"])
        expect(resources.some(r => r.name.includes(photo))).toBe(true);
      // Browser cache can legitimately produce zero for a reused entry.
      expect(resources.reduce((sum, image) => sum + image.bytes, 0)).toBeLessThan(300_000);
    }
    // axe waits until entrance drawing has settled to measure actual colors.
    if (width === 390 || width === 1440) await checkAxe(page);
    if (archive && process.env.STARTING_LINE_CAPTURE === "1" && !isMobile && [390, 1440].includes(width)) {
      const dir = path.resolve("../../artifacts/visual-review");
      await mkdir(dir, { recursive: true });
      await page.locator("#registration-paths").evaluate(async el => {
        await document.fonts.ready;
        await Promise.allSettled(el.getAnimations({ subtree: true }).map(animation => animation.finished));
      });
      await page.screenshot({ path: path.join(dir, `archive-entrance-${width}.jpg`), type: "jpeg", quality: 88 });
    }
    if (width === 390 && isMobile) await returning.tap(); else await returning.press("Enter");
    const form = page.locator("#returning");
    await expect(form).toHaveCSS("background-color", "rgb(255, 255, 255)");
    await expect(form.getByLabel("Parent or guardian email")).toBeVisible();
    await expect(form.getByLabel("Athlete’s first name")).toBeVisible();
    await expect(form.getByRole("button", { name: "Continue" })).toHaveCount(0);
    await expect(returning).toBeHidden();
  }
});

test("application draft, review edit, server error routing, exact retry and receipt", async ({ page, isMobile }) => {
  const requests: string[] = [];
  await page.route(applicationPost, async route => {
    requests.push(route.request().postData() || "");
    if (requests.length === 1) return route.fulfill({ status: 400, json: { message: "Check your answers.",
      errors: { emergencyContactPhone: ["Verify this synthetic phone number."] } } });
    if (requests.length === 2) return route.abort("failed");
    return route.fulfill({ status: 200, json: { reference: receipt } });
  });
  await page.goto(publicPath);
  const apply = page.getByRole("link", { name: "Apply for a spot", exact: true });
  await apply.focus();
  await apply.press("Enter");
  await expect(page.getByRole("heading", { name: "Who’s your athlete?" })).toBeFocused();
  await next(page);
  await expect(field(page, "athleteFirstName")).toBeFocused();
  await toReview(page);
  await checkAxe(page);
  if (process.env.STARTING_LINE_CAPTURE === "1") {
    // Synthetic fixtures only; this never runs in the actual application.
    const dir = path.resolve("../../artifacts/visual-review");
    await mkdir(dir, { recursive: true });
    await page.locator("#apply").screenshot({ path: path.join(dir, `review-${isMobile ? "phone" : "desktop"}.jpg`), type: "jpeg", quality: 87 });
  }
  expect(requests).toEqual([]);
  await edit(page, /^edit guardian$/i).click();
  await field(page, "guardianName").fill("Edited fixture guardian");
  await page.getByRole("button", { name: "Return to review" }).click();
  await expect(page.getByText("Edited fixture guardian", { exact: true })).toBeVisible();
  await page.goBack();
  await expect(field(page, "guardianName")).toHaveValue("Edited fixture guardian");
  await page.goForward();
  await expect(page).toHaveURL(/#apply\/review$/);
  const state = await page.evaluate(() => JSON.stringify([location.href, history.state, { ...localStorage }, { ...sessionStorage }]));
  for (const value of ["Edited fixture guardian", "Fixture contact", "parent@example.test", "2015-07-03"]) expect(state).not.toContain(value);
  const submit = page.getByRole("button", { name: "Submit application" });
  await submit.click();
  await expect(field(page, "emergencyContactPhone")).toBeFocused();
  await expect(field(page, "emergencyContactPhone")).toHaveAccessibleDescription(/verify this synthetic phone/i);
  await field(page, "emergencyContactPhone").fill("412-555-0122");
  await page.getByRole("button", { name: "Return to review" }).click();
  await submit.click();
  await page.getByRole("button", { name: "Retry submission" }).click();
  await expect(page.getByRole("heading", { name: "Application received" })).toBeVisible();
  const whatNext = page.locator("#apply section").filter({ has: page.getByRole("heading", { name: "What happens next", exact: true }) });
  await expect(whatNext.locator("p")).toHaveText(["The club will follow up with a text, phone call or email."]);
  await expect(page.locator("#apply").getByText("This does not reserve a spot.", { exact: true })).toBeVisible();
  expect(requests).toHaveLength(3);
  expect(requests[1]).toBe(requests[2]);
  expect(JSON.parse(requests[2])).toMatchObject({ guardianName: "Edited fixture guardian", emergencyContactPhone: "412-555-0122",
    kind: "Application", applicationVersion: 3 });
  expect(JSON.parse(requests[2])).not.toHaveProperty("availability");
  await expect(page.getByText(receipt, { exact: true })).toBeVisible();
  await expect(page.getByText("Edited fixture guardian", { exact: true })).toHaveCount(0);
});

test("server-side age eligibility returns the family to date of birth without showing the cutoff date", async ({ page }) => {
  const message = "Athletes must be ages 7–18 on the first practice date.";
  await page.route(applicationPost, route => route.fulfill({ status: 400, json: {
    message: "Check your answers.", errors: { dateOfBirth: [message] }
  } }));
  await page.goto(publicPath + "#apply");
  await toReview(page);
  await page.getByRole("button", { name: "Submit application", exact: true }).click();
  await expect(field(page, "dateOfBirth")).toBeFocused();
  await expect(field(page, "dateOfBirth")).toHaveAccessibleDescription(/Athletes must be ages 7–18 on the first practice date/);
  await expect(page.getByText(/late January or early February/)).toHaveCount(1);
  expect(await page.content()).not.toContain("firstPracticeDate");
});

test("fresh direct link cannot skip to Review; returning check-in has one save and closure is actionable", async ({ page }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto(publicPath + "#apply/review");
  await expect(field(page, "athleteFirstName")).toBeVisible();
  await expect(page.getByRole("button", { name: "Submit application" })).toHaveCount(0);
  await field(page, "athleteFirstName").fill("Draft athlete");
  await page.locator('a[href="#registration-paths"]:visible').click();
  const returning = page.getByRole("link", { name: "Returning athlete? Check in", exact: true });
  await returning.click();
  const form = page.locator("#returning");
  await form.getByLabel("Parent or guardian email").fill("synthetic@example.test");
  await form.getByLabel("Athlete’s first name").fill("Taylor");
  let count = 0;
  await page.route(applicationPost, route => {
    count++;
    const body = route.request().postDataJSON();
    expect(body.kind).toBe("Returning");
    expect(body).not.toHaveProperty("dateOfBirth");
    return count === 1 ? route.fulfill({ status: 201, json: { reference: receipt } })
      : route.fulfill({ status: 409, json: { code: "intake_closed", message: "Applications are not open." } });
  });
  await form.getByRole("button", { name: "Submit check-in" }).click();
  await expect(form.getByRole("heading", { name: "Check-in received" })).toBeVisible();
  await expect(form.getByText("The club will follow up by email.", { exact: true })).toBeVisible();
  await expect(form.getByText(/no automatic confirmation email/i)).toBeVisible();
  await form.getByRole("button", { name: "Check in another athlete" }).click();
  const resetEmail = form.getByLabel("Parent or guardian email");
  await expect(resetEmail).toBeFocused();
  await resetEmail.fill("synthetic@example.test");
  await form.getByLabel("Athlete’s first name").fill("Sibling");
  await form.getByRole("button", { name: "Submit check-in" }).click();
  await expect(form.getByRole("alert")).toContainText("not open");
  await expect(form.getByRole("link", { name: "Check registration status" })).toBeVisible();
  await expect(form.getByRole("button", { name: "Submit check-in" })).toHaveCount(0);
  expect(count).toBe(2);
});

test("static reduced-motion entrance, keyboard focus, hover geometry and 200% reflow", async ({ page, isMobile }) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto(publicPath);
  await page.evaluate(() => document.fonts.ready);
  const entrance = page.locator("#registration-paths");
  const apply = entrance.getByRole("link", { name: "Apply for a spot", exact: true });
  const returning = entrance.getByRole("link", { name: "Returning athlete? Check in", exact: true });
  await expect(returning).toBeVisible();
  await expect.poll(() => entrance.evaluate(el => el.getAnimations({ subtree: true })
    .filter(a => a.playState === "running").length)).toBe(0);
  await apply.focus();
  // iOS WebKit's virtual phone has no hardware keyboard attached. Exercise tap
  // in the responsive test and programmatic focus here; test Tab on desktop.
  if (isMobile) await returning.focus();
  else await page.keyboard.press("Tab");
  await expect(returning).toBeFocused();
  if (!isMobile) {
    const before = await returning.boundingBox();
    await apply.hover();
    expect(await returning.boundingBox()).toEqual(before);
    await returning.hover();
    expect(await returning.boundingBox()).toEqual(before);
  }
  // Equivalent layout viewport for a 680px desktop window zoomed to 200%.
  // Do not use pinch emulation: it scales pixels without triggering reflow.
  await page.setViewportSize({ width: 340, height: 360 });
  await expect(returning).toBeVisible();
  await returning.press("Enter");
  await expect(page.getByRole("heading", { name: "Returning athlete? Check in" })).toBeFocused();
  await expect(page.getByRole("button", { name: "Submit check-in" })).toBeEnabled();
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(0);
  await checkAxe(page);
});
