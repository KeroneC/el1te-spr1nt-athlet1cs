import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";
import { chromium } from "@playwright/test";

const routes = ["/", "/registration", "/forms", "/gallery", "/hall-of-fame", "/shop"];
const baseUrl = process.env.PERFORMANCE_BASE_URL;
const reportPath = process.env.PERFORMANCE_REPORT_PATH;
const enforceBudgets = process.env.PERFORMANCE_ENFORCE_BUDGETS !== "false";

if (!baseUrl) throw new Error("PERFORMANCE_BASE_URL is required.");
const parsedBaseUrl = new URL(baseUrl);
if (!["http:", "https:"].includes(parsedBaseUrl.protocol)) {
  throw new Error("PERFORMANCE_BASE_URL must use HTTP or HTTPS.");
}

const browser = await chromium.launch({ headless: true });
const results = [];

try {
  for (const route of routes) {
    const context = await browser.newContext({
      viewport: { width: 390, height: 844 },
      deviceScaleFactor: 2,
      isMobile: true,
      reducedMotion: "reduce"
    });
    await context.addInitScript(() => {
      window.__el1tePerformance = { lcp: 0, cls: 0 };
      new PerformanceObserver((entries) => {
        const latest = entries.getEntries().at(-1);
        if (latest) window.__el1tePerformance.lcp = latest.startTime;
      }).observe({ type: "largest-contentful-paint", buffered: true });
      new PerformanceObserver((entries) => {
        for (const entry of entries.getEntries()) {
          if (!entry.hadRecentInput) window.__el1tePerformance.cls += entry.value;
        }
      }).observe({ type: "layout-shift", buffered: true });
    });

    const page = await context.newPage();
    let imageResponses = [];
    page.on("response", (response) => {
      if (response.request().resourceType() !== "image") return;
      const headers = response.headers();
      imageResponses.push({
        url: response.url(),
        bytes: Number(headers["content-length"] ?? 0),
        cacheControl: headers["cache-control"] ?? ""
      });
    });

    async function measure(cacheState) {
      imageResponses = [];
      const target = new URL(route, parsedBaseUrl).toString();
      if (cacheState === "cold") {
        await page.goto(target, { waitUntil: "networkidle", timeout: 60_000 });
      } else {
        await page.reload({ waitUntil: "networkidle", timeout: 60_000 });
      }
      await page.waitForTimeout(250);

      const timing = await page.evaluate(() => {
        const navigation = performance.getEntriesByType("navigation")[0];
        const images = performance.getEntriesByType("resource")
          .filter((entry) => entry.initiatorType === "img")
          .map((entry) => ({
            url: entry.name,
            transferBytes: entry.transferSize,
            encodedBytes: entry.encodedBodySize
          }));
        return {
          ttfbMs: navigation.responseStart,
          domContentLoadedMs: navigation.domContentLoadedEventEnd,
          loadMs: navigation.loadEventEnd,
          lcpMs: window.__el1tePerformance.lcp,
          cls: window.__el1tePerformance.cls,
          images
        };
      });

      const responseByUrl = new Map(imageResponses.map((item) => [item.url, item]));
      const imageBytes = [...responseByUrl.values()].reduce((sum, item) => sum + item.bytes, 0);
      return {
        cacheState,
        ...timing,
        imageBytes,
        imageResponses: [...responseByUrl.values()]
      };
    }

    const cold = await measure("cold");
    const warm = await measure("warm");
    results.push({ route, cold, warm });
    await context.close();
  }
} finally {
  await browser.close();
}

const failures = [];
for (const result of results) {
  if (result.warm.lcpMs <= 0 || result.warm.lcpMs > 2_500) {
    failures.push(result.route + ": warm LCP exceeded 2500ms or was unavailable");
  }
  if (result.cold.cls > 0.1 || result.warm.cls > 0.1) {
    failures.push(result.route + ": CLS exceeded 0.1");
  }

  const budget = result.route === "/" ? 1_500_000 : result.route === "/gallery" ? 2_000_000 : null;
  if (budget && result.cold.imageBytes > budget) {
    failures.push(result.route + ": initial image bytes exceeded " + budget);
  }

  const cacheable = result.cold.imageResponses.filter((item) =>
    item.url.includes("/images/") || item.url.includes("/brand/") || item.url.includes("/media/")
  );
  if (cacheable.some((item) => !item.cacheControl.includes("max-age=86400"))) {
    failures.push(result.route + ": a static or managed image is missing the one-day cache policy");
  }

  const coldUrls = new Set(result.cold.images.map((item) => item.url));
  const retransferred = result.warm.images.filter((item) =>
    coldUrls.has(item.url) && item.transferBytes > 0
  );
  if (retransferred.length > 0) {
    failures.push(result.route + ": unchanged images were retransferred on the warm navigation");
  }
}

const report = {
  generatedAtUtc: new Date().toISOString(),
  baseUrl: parsedBaseUrl.origin,
  viewport: { width: 390, height: 844, deviceScaleFactor: 2 },
  budgets: {
    warmLcpMs: 2_500,
    maxCls: 0.1,
    homepageImageBytes: 1_500_000,
    galleryImageBytes: 2_000_000
  },
  results,
  failures
};

const output = JSON.stringify(report, null, 2) + "\n";
if (reportPath) {
  await mkdir(path.dirname(reportPath), { recursive: true });
  await writeFile(reportPath, output, "utf8");
}
process.stdout.write(output);
if (enforceBudgets && failures.length > 0) process.exitCode = 1;
