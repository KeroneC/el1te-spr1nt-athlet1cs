// Local visual rehearsal only. Not imported by the application or its deployment.
import { createServer } from "node:http";
import { createHmac, randomBytes } from "node:crypto";
import { spawn } from "node:child_process";
import { access, readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { localArchiveArtwork } from "../lib/local-registration-preview.mjs";
import { registration2027Copy } from "../lib/registration-2027-copy.mjs";

const webDir = fileURLToPath(new URL("../", import.meta.url));
const args = process.argv.slice(2);
const archive = args.includes("--archive-art");
const built = args.includes("--built");
const alternatePorts = args.includes("--alternate-ports");
const stagedServer = fileURLToPath(new URL("../../../artifacts/web/apps/web/server.js", import.meta.url));
if (built) await access(stagedServer).catch(() => {
  throw new Error("Stage the optimized web artifact with scripts/artifacts/build-web-artifact.sh first.");
});
const state = args.find(arg => !["--archive-art", "--built", "--alternate-ports"].includes(arg)) ?? "open";
if (!["open", "upcoming", "closed", "unavailable"].includes(state)) throw new Error("Choose open, upcoming, closed, or unavailable.");
if (archive && (built || state !== "open")) throw new Error("Archive review is only available in the open development preview.");
const portOffset = (state === "upcoming" ? 1 : 0) + (alternatePorts ? 2 : 0);
const [webPort, fixturePort] = [3013 + portOffset, 5130 + portOffset];
// Never look into the archive or expose originals from a server. Only the six explicitly
// prepared, metadata-stripped copies under the ignored artifacts directory may be loaded.
const artwork = new Map(archive ? await Promise.all(localArchiveArtwork.map(async file => [
  `/__registration-preview/${file}`, await readFile(new URL(`../../../artifacts/registration-archive-preview/${file}`, import.meta.url))
])) : []);
const signingKey = randomBytes(32);
const season = {
  year: 2027, title: "Local design preview · sample season", openingDate: "2026-11-27",
  ...registration2027Copy,
  replyWindow: "Local preview only. No registration is saved or email sent.", isOpen: state === "open"
};
const fixture = createServer(async (req, res) => {
  const send = (status, data) => {
    res.writeHead(status, { "Content-Type": "application/json", "Cache-Control": "private, no-store" });
    res.end(data === undefined ? undefined : JSON.stringify(data));
  };
  if (req.method === "GET" && artwork.has(req.url)) {
    const image = artwork.get(req.url);
    res.writeHead(200, { "Content-Type": "image/webp", "Content-Length": image.length,
      "Cache-Control": "private, no-store", "X-Content-Type-Options": "nosniff" });
    return res.end(image);
  }
  if (req.method === "GET" && req.url === "/api/public/registration")
    return state === "unavailable" ? send(503, {}) : state === "upcoming" ? send(200, {
      ...season, title: null, fees: null, practiceLocation: null,
      practiceSchedule: null, replyWindow: null,
      openingDate: [season.openingDate, new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10)].sort().at(-1)
    }) : send(200, season);
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
fixture.once("error", () => { console.error(`Preview API could not start on loopback port ${fixturePort}.`); process.exitCode = 1; });
fixture.listen(fixturePort, "127.0.0.1", () => {
  const origin = `http://127.0.0.1:${webPort}`;
  const web = spawn(process.execPath, built ? [stagedServer] :
    ["node_modules/next/dist/bin/next", "dev", "--webpack", "--hostname", "127.0.0.1", "--port", String(webPort)], {
    cwd: webDir, stdio: "inherit",
    env: { ...process.env, NODE_ENV: built ? "production" : "development", HOSTNAME: "127.0.0.1", PORT: String(webPort),
      SITE_URL: origin, API_BASE_URL: `http://127.0.0.1:${fixturePort}`,
      DEPLOYMENT_ENVIRONMENT: "demo", PUBLIC_INDEXING_ENABLED: "false", EL1TE_PLAYWRIGHT_TEST: "true",
      NEXT_TELEMETRY_DISABLED: "1", PUBLIC_REVALIDATE_SECONDS: "0", BROWSER_ANALYTICS_ENABLED: "false",
      APPLICATIONINSIGHTS_CONNECTION_STRING: "",
      STORE_NAVIGATION_MODE: "external", STARTING_LINE_ARCHIVE_PREVIEW: String(archive) }
  });
  console.log(`Local registration preview: ${origin}/registration (fictional @example.test email only; nothing saved).`);
  for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, () => web.kill(signal));
  web.once("exit", code => { fixture.close(); process.exitCode = code ?? 0; });
});
