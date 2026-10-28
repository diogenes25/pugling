import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";
import { PIN_MIN_LENGTH, pinProblem } from "./pin";

/** B-281: eine zu kurze PIN meldet die Oberfläche selbst, auf Deutsch, bevor der Server englisch antwortet. */
describe("pinProblem", () => {
  // Rot, wenn die Grenze verrutscht (`<=` statt `<`): dann wiese die Oberfläche eine PIN ab, die der Server annimmt.
  it("lässt genau ab vier Zeichen durch", () => {
    expect(PIN_MIN_LENGTH).toBe(4);
    expect(pinProblem("123")).toBe("Die PIN braucht mindestens 4 Zeichen.");
    expect(pinProblem("1234")).toBeNull();
  });

  // Rot, wenn leer als zu kurz gilt: leer heißt „unverändert" bzw. „Login deaktiviert", der Server nimmt es an.
  it("lässt eine leere PIN durch", () => {
    expect(pinProblem("")).toBeNull();
  });
});

/**
 * Die fünf Formulare, die heute eine PIN senden. Ein struktureller Fall – er liest den Quelltext, nicht das Verhalten:
 * rot, wenn eines davon den Aufruf verliert. Ein künftiges sechstes Formular sieht er nicht, und ob das Ergebnis an der
 * Aufrufstelle vor dem Senden wirkt, prüft er auch nicht; dafür bräuchte jedes Formular einen eigenen Render-Fall.
 */
describe("PIN-Formulare", () => {
  const FORMS = ["VaterDashboard", "VaterKind", "VaterLogin", "VaterProfil", "VaterWizard"];

  it.each(FORMS)("%s prüft die PIN vor dem Absenden", (name) => {
    const source = readFileSync(`src/vater/${name}.tsx`, "utf-8");
    expect(source).toMatch(/pinProblem\(/);
  });
});
