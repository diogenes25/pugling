import { defineConfig, devices } from "@playwright/test";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ESM: __dirname existiert nicht → aus import.meta.url ableiten.
const here = path.dirname(fileURLToPath(import.meta.url));

// Frische Wegwerf-DB je Testlauf → Backend seedet Papa(#1/0000) + Sohn(#1/1111) + Vokabeln neu.
// Die echte pugling.db bleibt unangetastet.
const dbFile = path.join(os.tmpdir(), `pugling-e2e-${Date.now()}.db`);
// Auch die hochgeladenen Bilder in einen Wegwerf-Ordner – sonst sammelt der Entwicklungsbaum
// (backend/Pugling.Api/media-uploads) mit jedem Testlauf Dateien an.
const mediaDir = path.join(os.tmpdir(), `pugling-e2e-media-${Date.now()}`);
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
    // Das Anmerkungs-Widget (nur Dev-Modus) abschalten: Playwright startet `npm run dev`, also wäre es
    // sichtbar und könnte Klicks abfangen oder Alt+A abgreifen. Der Schalter liegt bewusst im
    // localStorage statt in einer Env-Variablen – bei `reuseExistingServer` läuft der Dev-Server
    // fremdgestartet, dessen Env wir gar nicht setzen.
    storageState: {
      cookies: [],
      origins: [{
        origin: "http://localhost:5173",
        localStorage: [{ name: "pugling.remarks.off", value: "1" }],
      }],
    },
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
        Media__RootPath: mediaDir,
        // Login-Bremse aus: Sie zählt pro IP (PermitLimit 10/Minute), und alle Specs kommen über
        // dieselbe localhost-Partition. Die Suite lag knapp darunter – jeder weitere Test mit Login
        // hätte sonst einen späteren Spec mit „Login fehlgeschlagen" umgeworfen, scheinbar grundlos.
        // Dasselbe Zugeständnis macht der In-Process-TestServer im Backend.
        RateLimiting__LoginEnabled: "false",
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
