/**
 * Zentrale, geschlossene Sprachliste des Vokabel-Stores. Bewusst kein Freitext: die Vater-UI bietet
 * nur diese Codes an (Backend speichert weiterhin den Code als String). Erste Ausbaustufe: Deutsch,
 * Englisch, Französisch, Latein. Latein hat kein Land → sinnbildliche „Flagge" (antike Säule).
 */
export interface Language {
  code: string;
  label: string;
  flag: string;
}

export const LANGUAGES: Language[] = [
  { code: "de", label: "Deutsch", flag: "🇩🇪" },
  { code: "en", label: "Englisch", flag: "🇬🇧" },
  { code: "fr", label: "Französisch", flag: "🇫🇷" },
  { code: "la", label: "Latein", flag: "🏛️" },
];

export function languageByCode(code: string): Language | undefined {
  return LANGUAGES.find((l) => l.code === code);
}

/** „🇬🇧 Englisch" bzw. der rohe Code, falls unbekannt. */
export function languageLabel(code: string): string {
  const l = languageByCode(code);
  return l ? `${l.flag} ${l.label}` : code;
}
