import { defineConfig, devices } from "@playwright/test";

const repositoryRoot = "../..";
const apiUrl = "http://127.0.0.1:5127";
const webUrl = "http://127.0.0.1:3100";
const connectionString = String.raw`Server=(localdb)\mssqllocaldb;Database=El1teSpr1ntTrack_E2E;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`;

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["html", { open: "never" }], ["github"]] : "list",
  use: {
    baseURL: webUrl,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: `dotnet ef database update --configuration E2E --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --connection "${connectionString}" && dotnet run --configuration E2E --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --no-launch-profile`,
      cwd: repositoryRoot,
      url: `${apiUrl}/health/ready`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        ASPNETCORE_URLS: apiUrl,
        ConnectionStrings__DefaultConnection: connectionString,
        Jwt__Key: "E2E-only-signing-key-at-least-32-characters-long",
        Jwt__Issuer: "El1teSpr1ntTrack.E2E",
        Jwt__Audience: "El1teSpr1ntTrack.Web.E2E",
        Jwt__ExpiresMinutes: "30",
        SeedAdmin__Email: "e2e.admin@example.test",
        SeedAdmin__Password: "E2eAdmin!2026Pass",
        SeedAdmin__FirstName: "E2E",
        SeedAdmin__LastName: "Admin",
        MediaStorage__LocalRoot: "../../../../artifacts/e2e/uploads",
        MediaStorage__PublicBaseUrl: apiUrl,
      },
    },
    {
      command: "npm.cmd run dev -- --hostname 127.0.0.1 --port 3100",
      cwd: ".",
      url: webUrl,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { API_BASE_URL: apiUrl, PUBLIC_REVALIDATE_SECONDS: "0" },
    },
  ],
});
