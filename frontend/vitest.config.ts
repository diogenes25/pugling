import { defineConfig } from "vitest/config";

/**
 * Unit-Tests (`npm test`): Regeln unter `src/lib/`, Bildschirm-Logik in Ordnern wie `src/vater/`, und
 * seit der Werkzeugkette auch einzelne Bauteile und Hooks über React Testing Library. Die Oberfläche
 * als **Ganzes** prüft weiter Playwright (`npm run test:e2e`) – hier geht es um Einheiten, nicht um Wege
 * durch die App.
 *
 * Bewusst eine **eigene** Konfiguration statt eines `test`-Blocks in `vite.config.ts`: Die PWA-Erzeugung
 * dort hat im Testlauf nichts zu suchen (und `vite-plugin-pwa` verträgt sich ohnehin nicht mit vite 8).
 */
export default defineConfig({
  test: {
    // `src/lib/api.ts` liest den Token aus `localStorage`; ohne DOM ließe sich der API-Client nicht laden.
    environment: "happy-dom",
    include: ["src/**/*.test.ts", "src/**/*.test.tsx"],
    // Räumt das DOM zwischen den Fällen ab. **Nicht optional**, solange `globals: true` fehlt – die
    // Begründung steht in der Datei selbst, denn dort fällt sie beim Löschen ins Auge.
    setupFiles: ["src/test-setup.ts"],
  },
});
