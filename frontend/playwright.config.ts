import { defineConfig, devices } from "@playwright/test";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ESM: __dirname existiert nicht → aus import.meta.url ableiten.
const here = path.dirname(fileURLToPath(import.meta.url));

// Frische Wegwerf-DB je Testlauf → Backend seedet Papa(#1/0000) + Sohn(#1/1111) + Vokabeln neu.
// Die echte pugling.db bleibt unangetastet.
const dbFile = path.join(os.tmpdir(), `pugling-e2e-${Date.now()}.db`);
const backendDir = path.resolve(here, "../backend/Pugling.Api");

export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"]],
  use: {
    baseURL: "http://localhost:5173",
    headless: true,
    viewport: { width: 393, height: 830 }, // Handy-Größe (Redmi-7-nah)
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: `dotnet run --project "${backendDir}" --urls http://localhost:5200`,
      url: "http://localhost:5200/openapi/v1.json",
      timeout: 120_000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        ConnectionStrings__Default: `Data Source=${dbFile}`,
      },
    },
    {
      command: "npm run dev -- --port 5173",
      url: "http://localhost:5173",
      timeout: 60_000,
      reuseExistingServer: !process.env.CI,
    },
  ],
});
