import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import { de } from "../i18n/de";
import { en } from "../i18n/en";

/**
 * Laufzeit-Infrastruktur der Mehrsprachigkeit (B-294, erste Teilstufe von B-85): `react-i18next` mit zwei Sprachen,
 * die Wahl liegt rein client-seitig im `localStorage` (Entscheidung 2 – kein Server-Feld, bevor eine Zeile übersetzt
 * ist). Deutsch ist der Standard und bleibt es für jede Spec, die die Sprache nicht ausdrücklich umschaltet
 * (`playwright.config.ts` pinnt den Schlüssel). Übersetzt wird nur UI-Chrome; Nutzerdaten reisen als Werte in
 * `{{…}}`-Platzhaltern und bleiben, wie der Vater sie eingegeben hat.
 */
export const LANG_KEY = "pugling.lang";
export const SUPPORTED_LANGUAGES = ["de", "en"] as const;
export type Language = (typeof SUPPORTED_LANGUAGES)[number];

export const isLanguage = (value: unknown): value is Language =>
  typeof value === "string" && (SUPPORTED_LANGUAGES as readonly string[]).includes(value);

/** Die gespeicherte Wahl – oder Deutsch, wenn keine (oder eine unbekannte) gespeichert ist. */
export function storedLanguage(): Language {
  try {
    const raw = localStorage.getItem(LANG_KEY);
    return isLanguage(raw) ? raw : "de";
  } catch {
    return "de";
  }
}

/** Wechselt die Sprache und merkt sie sich; `<html lang>` zieht die Arcade-Wurzel nach. */
export async function setLanguage(language: Language): Promise<void> {
  try { localStorage.setItem(LANG_KEY, language); } catch { /* privater Modus: die Wahl gilt für diese Sitzung */ }
  await i18next.changeLanguage(language);
}

export const resources = { de, en } as const;

void i18next.use(initReactI18next).init({
  resources,
  lng: storedLanguage(),
  fallbackLng: "de",
  defaultNS: "sohn",
  ns: ["sohn"],
  interpolation: { escapeValue: false }, // React escaped schon
  returnNull: false,
});

export default i18next;
