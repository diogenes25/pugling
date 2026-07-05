// Geteilte Vokabel-Vokabularien (Wortart/Genus) für Store-Verwaltung und Übungs-Vokabelauswahl.
import type { Genus, PartOfSpeech } from "./types";

/** Alle Wortarten in Anzeige-Reihenfolge (spiegelt das PartOfSpeech-Enum des Backends). */
export const POS: PartOfSpeech[] = [
  "Noun", "Verb", "Adjective", "Adverb", "Pronoun", "Preposition",
  "Conjunction", "Article", "Numeral", "Interjection", "Phrase", "Other",
];

/** Deutsche Labels der Wortarten. */
export const POS_LABEL: Record<PartOfSpeech, string> = {
  Noun: "Substantiv", Verb: "Verb", Adjective: "Adjektiv", Adverb: "Adverb",
  Pronoun: "Pronomen", Preposition: "Präposition", Conjunction: "Konjunktion",
  Article: "Artikel", Numeral: "Numerale", Interjection: "Interjektion",
  Phrase: "Phrase", Other: "Sonstige",
};

/** Grammatikalische Geschlechter in Anzeige-Reihenfolge. */
export const GENUS: Genus[] = ["Masculine", "Feminine", "Neuter"];

/** Deutsche Labels der Geschlechter. */
export const GENUS_LABEL: Record<Genus, string> = {
  Masculine: "maskulin", Feminine: "feminin", Neuter: "neutral",
};
