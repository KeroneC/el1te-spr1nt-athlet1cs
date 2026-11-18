// Local visual rehearsal only. Not imported by the application or its deployment.
import { createServer } from "node:http";
import { createHmac, randomBytes } from "node:crypto";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const webDir = fileURLToPath(new URL("../", import.meta.url));
const webPort = 3013, fixturePort = 5130;
const state = process.argv[2] ?? "open";
if (!["open", "closed", "unavailable"].includes(state)) throw new Error("Choose open, closed, or unavailable.");
const signingKey = randomBytes(32);
const season = {
  year: 2027, title: "Local design preview · sample season", openingDate: "2026-11-27",
  firstPracticeDate: "2027-01-15", practiceLocation: "Sample practice location",
  practiceSchedule: "Sample practice schedule", fees: "Sample fee information",
  replyWindow: "Local preview only. No registration is saved or email sent.", isOpen: state === "open"
};
const fixture = createServer(async (req, res) => {
  const send = (status, data) => {
    res.writeHead(status, { "Content-Type": "application/json", "Cache-Control": "private, no-store" });
    res.end(data === undefined ? undefined : JSON.stringify(data));
  };
  if (req.method === "GET" && req.url === "/api/public/registration")
    return state === "unavailable" ? send(503, {}) : send(200, season);
  if (req.method === "POST" && req.url === "/api/public/registration/visit") return send(204);
  if (req.method === "POST" && req.url === "/api/public/registration/applications") {
    if (!season.isOpen) return send(409, { code: "intake_closed" });
    let body = "";
    try {
      for await (const chunk of req) {
        if (Buffer.byteLength(body) + chunk.length > 8192) return send(413, {});
        body += chunk;
      }
      const { guardianEmail, requestKey } = JSON.parse(body);
      if (typeof guardianEmail !== "string" || !/@example\.test$/i.test(guardianEmail))
        return send(400, { errors: { guardianEmail: ["Use a fictional address ending in @example.test for this local preview."] } });
      if (typeof requestKey !== "string" || !/^[a-f\d-]{36}$/i.test(requestKey)) return send(400, {});
      // No family data, receipts, or payload hashes retained. Retries get the same simulated reference.
      const code = createHmac("sha256", signingKey).update(requestKey).digest("hex").slice(0, 16).toUpperCase();
      return send(200, { reference: `EL-${code}` });
    } catch { return send(400, {}); }
  }
  return send(404, {});
});
fixture.once("error", () => { console.error("Preview API could not start on loopback port 5130."); process.exitCode = 1; });
fixture.listen(fixturePort, "127.0.0.1", () => {
  const origin = `http://127.0.0.1:${webPort}`;
  const web = spawn(process.execPath, ["node_modules/next/dist/bin/next", "dev", "--webpack", "--hostname", "127.0.0.1", "--port", String(webPort)], {
    cwd: webDir, stdio: "inherit",
    env: { ...process.env, NODE_ENV: "development", SITE_URL: origin, API_BASE_URL: `http://127.0.0.1:${fixturePort}`,
      DEPLOYMENT_ENVIRONMENT: "demo", PUBLIC_INDEXING_ENABLED: "false", EL1TE_PLAYWRIGHT_TEST: "true",
      NEXT_TELEMETRY_DISABLED: "1", PUBLIC_REVALIDATE_SECONDS: "0", BROWSER_ANALYTICS_ENABLED: "false",
      APPLICATIONINSIGHTS_CONNECTION_STRING: "",
      STORE_NAVIGATION_MODE: "external" }
  });
  console.log(`Local registration preview: ${origin}/registration (fictional @example.test email only; nothing saved).`);
  for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, () => web.kill(signal));
  web.once("exit", code => { fixture.close(); process.exitCode = code ?? 0; });
});
