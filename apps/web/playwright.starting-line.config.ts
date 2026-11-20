import { defineConfig, devices } from "@playwright/test";

// Run against a local Next server using *only* the synthetic season fixture.
// There is no SQL, live origin, or registration-opening step in this suite.
const url = new URL(process.env.STARTING_LINE_PREVIEW_URL || "http://127.0.0.1:3013");
if (url.protocol !== "http:" || url.hostname !== "127.0.0.1" || url.pathname !== "/" ||
    url.search || url.hash || url.username || url.password) {
  throw new Error("The Starting Line rehearsal accepts only a loopback HTTP origin.");
}

export default defineConfig({
  testDir: "./qa/starting-line", workers: 1, timeout: 45_000,
  reporter: "list", use: { baseURL: url.origin, trace: "off", screenshot: "off", video: "off" },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } },
    { name: "mobile-webkit", use: { ...devices["iPhone 13"], viewport: { width: 390, height: 844 } } }
  ]
});
