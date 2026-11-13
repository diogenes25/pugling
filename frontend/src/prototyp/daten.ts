// PROTOTYP – Wegwerf-Daten für /prototyp/pfad (docs/duolingo-snipwise-vergleich-plan.md, Stufe A).
// Nichts hiervon kommt vom Server; die Form soll beurteilt werden, nicht der Inhalt.

export type KnotenArt = "lektion" | "wiederholung" | "truhe" | "test";
export type KnotenZustand = "fertig" | "dran" | "gesperrt";

export interface Knoten {
  id: string;
  art: KnotenArt;
  titel: string;
  icon: string;
  /** Stufen der Position (Kennenlernen → … → frei schreiben) – der segmentierte Ring. */
  stufen: number;
  stufenFertig: number;
  /** Heute Pflicht: Malus in Münzen, wenn der Tag ohne diese Lektion endet. */
  pflichtMalus?: number;
  faellig?: number;
  hinweis?: string;
}

export interface Abschnitt {
  id: string;
  kopf: string;
  titel: string;
  knoten: Knoten[];
}

export const STUFEN_NAMEN = ["Kennenlernen", "Auswählen", "Buchstaben", "Frei schreiben"];

export const PFAD: Abschnitt[] = [
  {
    id: "u1", kopf: "Abschnitt 1", titel: "Unité 1 · Bonjour !",
    knoten: [
      { id: "u1a", art: "lektion", titel: "Begrüßen", icon: "👋", stufen: 4, stufenFertig: 4 },
      { id: "u1b", art: "lektion", titel: "Zahlen bis 20", icon: "🔢", stufen: 4, stufenFertig: 4 },
      { id: "u1t", art: "test", titel: "Test Unité 1", icon: "🏁", stufen: 1, stufenFertig: 1 },
    ],
  },
  {
    id: "u2", kopf: "Abschnitt 2", titel: "Unité 2 · En ville",
    knoten: [
      { id: "u2a", art: "lektion", titel: "In der Stadt · Wörter 1", icon: "🏙️", stufen: 4, stufenFertig: 4 },
      { id: "u2b", art: "lektion", titel: "In der Stadt · Wörter 2", icon: "🥖", stufen: 4, stufenFertig: 1, pflichtMalus: 5 },
      { id: "u2w", art: "wiederholung", titel: "Wiederholen", icon: "🏋️", stufen: 1, stufenFertig: 0, pflichtMalus: 5, faellig: 14 },
      { id: "u2c", art: "lektion", titel: "Lückentext · Au marché", icon: "📝", stufen: 3, stufenFertig: 0 },
      { id: "u2x", art: "truhe", titel: "Belohnungsbox", icon: "🎁", stufen: 1, stufenFertig: 0 },
      { id: "u2d", art: "lektion", titel: "Hören · Wo ist …?", icon: "🎧", stufen: 3, stufenFertig: 0 },
      { id: "u2t", art: "test", titel: "Test Unité 2", icon: "🏁", stufen: 1, stufenFertig: 0, hinweis: "Klassenarbeit am 27.11." },
    ],
  },
];

export type Aufgabe =
  | { art: "auswahl"; anweisung: string; frage: string; optionen: string[]; loesung: string; erklaerung: string; neu: boolean }
  | { art: "luecke"; anweisung: string; vor: string; nach: string; optionen: string[]; loesung: string; erklaerung: string; neu: boolean }
  | { art: "ordnen"; anweisung: string; frage: string; loesung: string; erklaerung: string; neu: boolean }
  | { art: "tippen"; anweisung: string; frage: string; loesung: string; erklaerung: string; neu: boolean };

export const LEKTION: Aufgabe[] = [
  {
    art: "auswahl", anweisung: "Wähle die Übersetzung", frage: "la boulangerie",
    optionen: ["die Bäckerei", "die Bücherei", "der Bahnhof", "die Brücke"], loesung: "die Bäckerei",
    erklaerung: "le boulanger ist der Bäcker – er arbeitet in der boulangerie.", neu: true,
  },
  {
    art: "auswahl", anweisung: "Richtig oder falsch", frage: "„le marché“ heißt „der Markt“.",
    optionen: ["Richtig", "Falsch"], loesung: "Richtig",
    erklaerung: "Au marché = auf dem Markt.", neu: false,
  },
  {
    art: "luecke", anweisung: "Vervollständige den Satz", vor: "Je vais", nach: "la gare.",
    optionen: ["à", "au", "de", "chez"], loesung: "à",
    erklaerung: "la gare ist weiblich: à + la bleibt „à la“. Nur à + le wird zu „au“.", neu: true,
  },
  {
    art: "ordnen", anweisung: "Ordne die Buchstaben", frage: "die Brücke", loesung: "pont",
    erklaerung: "le pont – denk an „Sur le pont d’Avignon“.", neu: true,
  },
  {
    art: "auswahl", anweisung: "Wähle die Übersetzung", frage: "der Bahnhof",
    optionen: ["la gare", "la rue", "le pont", "la place"], loesung: "la gare",
    erklaerung: "la gare – der Bahnhof. Die Garage fürs Auto heißt „le garage“.", neu: false,
  },
  {
    art: "tippen", anweisung: "Schreibe auf Französisch", frage: "die Straße", loesung: "la rue",
    erklaerung: "la rue – mit Artikel, wie im Vokabeltest.", neu: true,
  },
];
