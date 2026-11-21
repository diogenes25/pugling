import { defineConfig, devices } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { dbFile, mediaDir } from "./e2e/temp-paths";

// Ein Worker, dessen Ereignisschleife steht, gilt nach 45 s statt nach 5 Minuten als tot (B-605). Der Läufer liest die
// Frist erst beim Beenden (`playwright/lib/runner/index.js:2107`, 1.65.1) und zählt sie ab dem letzten Herzschlag, den
// ein lebender Worker beim Beenden jede Sekunde schickt – getroffen wird also nur ein stehender Prozess. `??=`: von außen gesetzt gewinnt.
process.env.PWTEST_CHILD_PROCESS_TIMEOUT ??= "45000";

// ESM: __dirname existiert nicht → aus import.meta.url ableiten.
const here = path.dirname(fileURLToPath(import.meta.url));

// dbFile/mediaDir kommen aus e2e/temp-paths.ts (B-55) - geteilt mit e2e/global-teardown.ts, damit der
// Teardown dieselben Dateinamen berechnet statt eigene mit einem neuen `Date.now()` zu erfinden.
// Frische Wegwerf-DB je Testlauf → Backend seedet Papa(#1/0000) + Sohn(#1/1111) + Vokabeln neu.
// Die echte pugling.db bleibt unangetastet. Auch die hochgeladenen Bilder liegen in einem
// Wegwerf-Ordner – sonst sammelt der Entwicklungsbaum (backend/Pugling.Api/media-uploads) mit jedem
// Testlauf Dateien an.
const backendDir = path.resolve(here, "../backend/Pugling.Api");

// Die Ports sind überschreibbar (B-485): auf :5200 läuft im Heimnetz der Server mit der echten DB der Söhne, und den
// hält ein E2E-Lauf nicht an. `PUGLING_E2E_API_PORT=5201 PUGLING_E2E_WEB_PORT=5174 npx playwright test` fährt daneben.
const apiPort = process.env.PUGLING_E2E_API_PORT ?? "5200";
const webPort = process.env.PUGLING_E2E_WEB_PORT ?? "5173";
const webOrigin = `http://localhost:${webPort}`;
// Einen schon laufenden Vite-Server nur auf dem Standardpaar übernehmen: ein fremd gestarteter Server auf dem
// Ausweichport hätte PUGLING_API_URL nie bekommen und leitete auf :5200 – in die echte DB.
const sidePorts = apiPort !== "5200" || webPort !== "5173";
// Auch die Build-Konfiguration ist überschreibbar (B-525): ein laufender `dotnet run` hält die Debug-Ausgabe gesperrt,
// und der Start hier scheiterte am Kopieren der DLLs. `PUGLING_E2E_CONFIGURATION=Release` baut daneben.
const configuration = process.env.PUGLING_E2E_CONFIGURATION ? ` -c ${process.env.PUGLING_E2E_CONFIGURATION}` : "";

// Läuft eine Mutanten-Probe (.claude/scripts/mutant-probe.py), liegen mutierte Quellen im Baum: `dotnet run` unten
// baute einen Mutanten in den Server, und ein rotes Ergebnis hielte man für einen Fehler im echten Code (Sprint 82).
// Hier und nicht im globalSetup – Playwright startet den webServer vor dem globalSetup. Die Probe selbst darf (ihr
// Testbefehl trägt PUGLING_MUTANT_PROBE); eine Sperre älter als vier Stunden ist verwaist (wie im Skript).
const probeLock = path.resolve(here, "../.claude/.mutant-probe.lock");
if (!process.env.PUGLING_MUTANT_PROBE && fs.existsSync(probeLock)
    && Date.now() - fs.statSync(probeLock).mtimeMs < 4 * 3600_000) {
  throw new Error(`Eine Mutanten-Probe läuft (${probeLock}) – der Server würde einen Mutanten bauen. Nach ihrem Ende starten.`);
}

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
    baseURL: webOrigin,
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
        origin: webOrigin,
        localStorage: [
          { name: "pugling.remarks.off", value: "1" },
          // Deutsch bleibt der E2E-Standard (B-85, Entscheidung 6): ein späterer Sprachwechsel bricht keine Spec
          // heimlich; nur `sprache.spec.ts` schaltet um und zurück.
          { name: "pugling.lang", value: "de" },
        ],
      }],
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: `dotnet run${configuration} --project "${backendDir}" --urls http://localhost:${apiPort}`,
      url: `http://localhost:${apiPort}/openapi/v1.json`,
      timeout: 120_000,
      reuseExistingServer: false,
      // Server-Ausgabe durchreichen (B-139). Playwright verwirft sie sonst, sobald der Start geglückt
      // ist – und genau dann fängt der interessante Teil an: der Nachtlauf war Nächte lang rot (wie
      // viele, steht in B-139 – die Zahl stand an zwei Orten verschieden, B-175), jeder Aufruf antwortete
      // mit 500, und die Ausnahme dahinter stand in keinem Protokoll. Ohne diese zwei Zeilen kostet
      // jeder Diagnoseschritt eine ganze Nacht.
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
      command: `npm run dev -- --port ${webPort} --strictPort`,
      url: webOrigin,
      env: { PUGLING_API_URL: `http://localhost:${apiPort}` },
      timeout: 60_000,
      reuseExistingServer: !process.env.CI && !sidePorts,
    },
  ],
});
