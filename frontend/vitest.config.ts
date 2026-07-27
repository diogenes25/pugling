import { defineConfig } from "vitest/config";

/**
 * Unit-Tests der Logik unter `src/lib/` (`npm test`). Die Oberfläche selbst prüft Playwright
 * (`npm run test:e2e`) – hier geht es um Regeln, die kein Browser braucht.
 *
 * Bewusst eine **eigene** Konfiguration statt eines `test`-Blocks in `vite.config.ts`: Die PWA-Erzeugung
 * dort hat im Testlauf nichts zu suchen (und `vite-plugin-pwa` verträgt sich ohnehin nicht mit vite 8).
 */
export default defineConfig({
  test: {
    // `src/lib/api.ts` liest den Token aus `localStorage`; ohne DOM ließe sich der API-Client nicht laden.
    environment: "happy-dom",
    include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
  },
});
