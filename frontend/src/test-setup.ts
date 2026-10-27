import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";
// Die Übersetzungen einmal laden: Komponenten der Arcade rufen `useTranslation`, und ohne Instanz lieferte
// react-i18next nur Schlüssel (B-294).
import "./lib/i18n";

/**
 * Räumt das DOM nach jedem Testfall ab (eingehängt über `setupFiles` in `vitest.config.ts`).
 *
 * React Testing Library registriert dieses `afterEach` **selbst**, aber nur, wenn ein globales
 * `afterEach` existiert – also unter `globals: true`. Diese Konfiguration setzt das bewusst nicht
 * (die 21 bestehenden Tests importieren `describe`/`it`/`expect` ausdrücklich aus `vitest`, und zwei
 * Schreibweisen für denselben Test sind eine zu viel). Ohne die Zeile hier bleibt jedes gerenderte
 * Bauteil zwischen den Fällen stehen: `getByText` findet dann zwei Treffer, und die **Reihenfolge**
 * der Fälle entscheidet über Grün – die teuerste Sorte Flake, weil sie einzeln ausgeführt verschwindet.
 *
 * Die Datei liegt unter `src/`, damit `tsc -b` sie mitprüft (`tsconfig.json` prüft nur `src`).
 * Bewacht von `test-setup.test.tsx` – wer sie aushängt, bekommt Rot statt eines stillen Flakes.
 */
afterEach(cleanup);
