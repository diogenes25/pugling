import { defineConfig, devices } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { dbFile, mediaDir } from "./e2e/temp-paths";

// ESM: __dirname existiert nicht → aus import.meta.url ableiten.
const here = path.dirname(fileURLToPath(import.meta.url));

// dbFile/mediaDir kommen aus e2e/temp-paths.ts (B-55) - geteilt mit e2e/global-teardown.ts, damit der
// Teardown dieselben Dateinamen berechnet statt eigene mit einem neuen `Date.now()` zu erfinden.
// Frische Wegwerf-DB je Testlauf → Backend seedet Papa(#1/0000) + Sohn(#1/1111) + Vokabeln neu.
// Die echte pugling.db bleibt unangetastet. Auch die hochgeladenen Bilder liegen in einem
// Wegwerf-Ordner – sonst sammelt der Entwicklungsbaum (backend/Pugling.Api/media-uploads) mit jedem
// Testlauf Dateien an.
const backendDir = path.resolve(here, "../backend/Pugling.Api");

export default defineConfig({
  testDir: "./e2e",
  globalSetup: "./e2e/global-setup.ts",
  globalTeardown: "./e2e/global-teardown.ts",
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
      // Server-Ausgabe durchreichen (B-139). Playwright verwirft sie sonst, sobald der Start geglückt
      // ist – und genau dann fängt der interessante Teil an: der Nachtlauf war drei Nächte rot, jeder
      // Aufruf antwortete mit 500, und die Ausnahme dahinter stand in keinem Protokoll. Ohne diese
      // zwei Zeilen kostet jeder Diagnoseschritt eine ganze Nacht.
      stdout: "pipe",
      stderr: "pipe",
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
