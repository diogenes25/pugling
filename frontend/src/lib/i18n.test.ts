import { afterEach, beforeEach, describe, expect, it } from "vitest";
import i18n, { LANG_KEY, resources, setLanguage, storedLanguage } from "./i18n";

/*
 * B-294. Zwei Regeln, die der Bildschirm nicht zeigt: jede Sprache trägt jeden Schlüssel (sonst fiele ein englischer
 * Sohn in einer sonst englischen Arcade auf ein deutsches Wort zurück, ohne dass es jemand sieht), und die Wahl
 * überlebt den Neustart über `localStorage` (sonst wäre der Umschalter eine Einstellung für eine Sitzung).
 */

function keysOf(node: unknown, prefix = ""): string[] {
  if (typeof node !== "object" || node === null) return [prefix];
  return Object.entries(node).flatMap(([k, v]) => keysOf(v, prefix ? `${prefix}.${k}` : k));
}

describe("i18n-Ressourcen", () => {
  it("tragen in Englisch genau die Schlüssel von Deutsch – kein Schlüssel fehlt, keiner ist zu viel", () => {
    // Mutante: einen Schlüssel aus `en.ts` streichen (fehlt) oder dort einen erfinden (zu viel) – beides fällt.
    const de = keysOf(resources.de).sort();
    const en = keysOf(resources.en).sort();
    expect(en).toEqual(de);
    expect(de.length).toBeGreaterThan(20);
  });

  it("lassen keinen Text leer und übersetzen wirklich – die Navigation heißt in beiden Sprachen anders", () => {
    for (const lang of ["de", "en"] as const) {
      for (const key of keysOf(resources[lang])) {
        const value = key.split(".").reduce<unknown>((n, k) => (n as Record<string, unknown>)[k], resources[lang]);
        expect(typeof value === "string" && value.trim().length > 0, `${lang}: ${key}`).toBe(true);
      }
    }
    expect(resources.en.sohn.nav.home).not.toBe(resources.de.sohn.nav.home);
  });
});

describe("Sprachwahl", () => {
  beforeEach(() => { localStorage.removeItem(LANG_KEY); });
  // Ein gefallener Fall darf die Sprache nicht auf Englisch stehen lassen – die nächsten Fälle erwarten Deutsch.
  afterEach(async () => { await setLanguage("de"); });

  it("ist Deutsch, solange nichts (oder Unbekanntes) gespeichert ist", () => {
    expect(storedLanguage()).toBe("de");
    localStorage.setItem(LANG_KEY, "fr");
    expect(storedLanguage()).toBe("de");
  });

  it("wechselt die Sprache und merkt sie sich", async () => {
    await setLanguage("en");
    expect(localStorage.getItem(LANG_KEY)).toBe("en");
    expect(i18n.t("sohn:nav.home")).toBe("Home");
    await setLanguage("de");
    expect(i18n.t("sohn:nav.home")).toBe("Basis");
  });

  it("setzt Platzhalter aus Nutzerdaten ein, statt sie zu übersetzen", async () => {
    await setLanguage("en");
    expect(i18n.t("sohn:home.doneText", { skin: "Drachi" })).toBe("Strong. Every extra round only makes your Drachi stronger.");
    await setLanguage("de");
  });
});
