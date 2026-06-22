import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

if (process.env.NODE_ENV === "production" && !process.env.API_BASE_URL) {
  throw new Error("API_BASE_URL is required for a production build.");
}

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  outputFileTracingRoot: path.join(__dirname, "../..")
};

export default nextConfig;
