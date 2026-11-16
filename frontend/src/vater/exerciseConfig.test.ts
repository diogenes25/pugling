import { describe, expect, it } from "vitest";
import { buildTypeConfig, choicesMissAnswer, configToEditorState, contentProblem, emptyRow, normalizeAnswer } from "./exerciseConfig";
import type { ExerciseTypeKey } from "../lib/types";

/*
 * Der Rundlauf ist die Gegenprobe, die dieser Datei bisher fehlte: `buildTypeConfig` schreibt die Config,
 * `configToEditorState` liest sie zurück – laufen die zwei auseinander, verliert Bearbeiten still Inhalte.
 *
 * Nötig wurde er mit B-69: Die Listenfelder wechselten von `string` auf `string[]`, und die Zeile ist ein
 * `Record<string, any>` – der Compiler sieht einen vergessenen Aufrufer also NICHT. Darum führt jeder Fall
 * einen Wert mit Komma mit; genau der zerbrach vorher am kommagetrennten Sammelfeld.
 */

/** Ein Wert, den das alte Sammelfeld in zwei zerrissen hätte. */
const MIT_KOMMA = "groß, wirklich groß";

/** Baut die Config und liest sie zurück – wie beim Anlegen und anschließenden Bearbeiten. */
function rundlauf(type: ExerciseTypeKey, rows: Record<string, unknown>[], extra: Record<string, unknown> = {}) {
  const config = buildTypeConfig(type, rows, extra);
  return { config, ...configToEditorState(type, config) };
}

describe("Rundlauf über die umgestellten Listenfelder", () => {
  it("Lückentext: Alternativen und Wortpool bleiben Listen", () => {
    const { rows, extra } = rundlauf("Cloze",
      [{ index: 1, answer: "riesig", alternatives: ["sehr groß", MIT_KOMMA], vocabKey: null }],
      { text: "Das Haus ist {{1}}.", wordBank: ["riesig", MIT_KOMMA] });

    expect(rows[0].alternatives).toEqual(["sehr groß", MIT_KOMMA]);
    expect(extra.wordBank).toEqual(["riesig", MIT_KOMMA]);
  });

  // B-524: Die Satzform reist mit dem Speicherverweis hin und zurück; leer heißt „keine“ (null), nicht "". Rot,
  // wenn einer der beiden Wege `form` vergisst – dann verlöre Bearbeiten die Satzform still.
  it("Lückentext: Speicherverweis und Satzform überleben den Rundlauf", () => {
    const { config, rows } = rundlauf("Cloze",
      [{ index: 1, answer: "", alternatives: [], vocabKey: "en_patient_patient_in", form: " patients " },
       { index: 2, answer: "debate", alternatives: [], vocabKey: null, form: "" }],
      { text: "{{1}} and {{2}}" });

    const gaps = (config as { gaps: { vocabKey: string | null; form: string | null }[] }).gaps;
    expect(gaps.map((g) => g.form)).toEqual(["patients", null]);
    expect(rows[0].vocabKey).toBe("en_patient_patient_in");
    expect(rows[0].form).toBe("patients");
    expect(rows[1].form).toBe("");
  });

  // B-599: Die falschen Vorschläge je Lücke reisen hin und zurück; leer heißt „keine“ (fehlt in der Config). Rot, wenn
  // einer der beiden Wege `distractors` vergisst – dann verlöre Bearbeiten die Vorschläge des Autors still.
  it("Lückentext: falsche Vorschläge je Lücke überleben den Rundlauf", () => {
    const { config, rows } = rundlauf("Cloze",
      [{ index: 1, answer: "doesn't she", alternatives: [], vocabKey: null, form: "", distractors: ["don't she", " isn't she "] },
       { index: 2, answer: "didn't they", alternatives: [], vocabKey: null, form: "", distractors: [] }],
      { text: "{{1}} {{2}}" });

    const gaps = (config as { gaps: { distractors: string[] | null }[] }).gaps;
    expect(gaps[0].distractors).toEqual(["don't she", "isn't she"]);
    expect(rows[0].distractors).toEqual(["don't she", "isn't she"]);
    expect(rows[1].distractors).toEqual([]);
  });

  // B-524: Die Ablenker des Wortkastens reisen hin und zurück; leer heißt „keine“ (fehlt in der Config). Rot, wenn
  // einer der beiden Wege `cueDistractors` vergisst.
  it("Lückentext: Ablenker im Wortkasten überleben den Rundlauf", () => {
    const { config, extra } = rundlauf("Cloze",
      [{ index: 1, answer: "racism", alternatives: [], vocabKey: null, form: "" }],
      { text: "{{1}}", wordBank: [], cueDistractors: ["rassistisch", " ", MIT_KOMMA] });

    expect((config as { cueDistractors?: string[] }).cueDistractors).toEqual(["rassistisch", MIT_KOMMA]);
    expect(extra.cueDistractors).toEqual(["rassistisch", MIT_KOMMA]);
  });

  // B-524 (Review): Eine Speicherlücke hat keine Inline-Lösung und ist trotzdem vollständig. Rot, wenn die Prüfung
  // wieder `answer` verlangt – dann ließe sich keine Lücke aus dem Vokabel-Store speichern.
  it("Lückentext: eine Speicherlücke ohne Inline-Lösung ist gültig, eine leere Inline-Lücke nicht", () => {
    expect(contentProblem("Cloze", [{ index: 1, answer: "", vocabKey: "en_x", form: "" }], { text: "{{1}}" }, 0)).toBeNull();
    expect(contentProblem("Cloze", [{ index: 1, answer: "", vocabKey: null, form: "" }], { text: "{{1}}" }, 0)).not.toBeNull();
  });

  it("Liste: Alternativen bleiben Listen", () => {
    const { rows } = rundlauf("List", [{ value: "Paris", alternatives: ["Paris (FR)", MIT_KOMMA] }]);
    expect(rows[0].alternatives).toEqual(["Paris (FR)", MIT_KOMMA]);
  });

  it("Übersetzung: Alternativen bleiben Listen und die Store-Bindung bleibt erhalten", () => {
    const { rows } = rundlauf("Translation",
      [{ source: "Where do you live?", target: "Wo wohnst du?", alternatives: ["Wo lebst du?", MIT_KOMMA],
        vocabularyId: 7, origSource: "Where do you live?", origTarget: "Wo wohnst du?",
        origSourceLang: "en", origTargetLang: "de" }],
      { sourceLang: "en", targetLang: "de" });

    expect(rows[0].alternatives).toEqual(["Wo lebst du?", MIT_KOMMA]);
    expect(rows[0].vocabularyId).toBe(7);
  });

  /*
   * B-396: die Bindung eines Paars zeigt auf einen Speicher-Eintrag EINES Sprachpaars. Ändert der Vater nur
   * Quell- oder Zielsprache, meint dasselbe Paar ein anderes Wort – die Bindung muss fallen, sonst zeigt die
   * Übung weiter auf die Vokabel des alten Sprachpaars. Je Fall rot, wenn der Vergleich die jeweilige Sprache
   * nicht prüft; der Fall oben ("Bindung bleibt erhalten") ist die Gegenprobe mit unverändertem Paar.
   */
  it.each([
    ["Quellsprache", { sourceLang: "fr", targetLang: "de" }],
    ["Zielsprache", { sourceLang: "en", targetLang: "fr" }],
  ])("Übersetzung: ein Wechsel der %s löst die Store-Bindung", (_, neu) => {
    const geladen = configToEditorState("Translation", {
      sourceLang: "en", targetLang: "de",
      items: [{ source: "Where do you live?", target: "Wo wohnst du?", vocabularyId: 7 }],
    });
    expect(geladen.rows[0].vocabularyId).toBe(7);

    const gespeichert = buildTypeConfig("Translation", geladen.rows, { ...geladen.extra, ...neu }) as { items: { vocabularyId: number | null }[] };

    expect(gespeichert.items[0].vocabularyId).toBeNull();
  });

  // B-400: eine Übung mit einem anderen Sprachpaar als dem Rückfall, geladen und unverändert gespeichert, behält ihre
  // Bindung. Rot, wenn das gemerkte Sprachpaar nicht aus der Config kommt (etwa fest "en"/"de").
  it("Übersetzung: ein geladenes Sprachpaar fr→es bleibt beim Speichern gebunden", () => {
    const geladen = configToEditorState("Translation", {
      sourceLang: "fr", targetLang: "es",
      items: [{ source: "Où habites-tu ?", target: "¿Dónde vives?", vocabularyId: 9 }],
    });

    const gespeichert = buildTypeConfig("Translation", geladen.rows, geladen.extra) as { items: { vocabularyId: number | null }[] };

    expect(gespeichert.items[0].vocabularyId).toBe(9);
  });

  // Eine Altübung ohne Sprachen zeigt im Editor den Rückfall en→de; bloßes Öffnen und Speichern darf die
  // Bindung nicht lösen. Rot, wenn das gemerkte Sprachpaar den Rückfall nicht mitnimmt.
  it("Übersetzung: Öffnen und Speichern einer Übung ohne Sprachen behält die Store-Bindung", () => {
    const geladen = configToEditorState("Translation", {
      items: [{ source: "Where do you live?", target: "Wo wohnst du?", vocabularyId: 7 }],
    });

    const gespeichert = buildTypeConfig("Translation", geladen.rows, geladen.extra) as { items: { vocabularyId: number | null }[] };

    expect(gespeichert.items[0].vocabularyId).toBe(7);
  });

  it("Leseverstehen: die Auswahl bleibt eine Liste", () => {
    const { rows } = rundlauf("Reading",
      [{ prompt: "Wohin fährt Tom?", answer: "Brighton", choices: ["Brighton", MIT_KOMMA] }],
      { text: "Tom goes to Brighton." });

    expect(rows[0].choices).toEqual(["Brighton", MIT_KOMMA]);
  });

  it("Hörverstehen: die Auswahl bleibt eine Liste", () => {
    const { rows } = rundlauf("Listening",
      [{ prompt: "Woher kommt B?", answer: "Leeds", choices: ["Leeds", MIT_KOMMA] }],
      { audioUrl: "https://example.org/a.mp3", transcript: "" });

    expect(rows[0].choices).toEqual(["Leeds", MIT_KOMMA]);
  });

  /*
   * B-72: die Dekodierung führte bisher EIN kommagetrenntes Sammelfeld ("Wort:wörtlich, Wort:wörtlich"),
   * doppelt zerlegt (erst an ",", dann an ":"). Ein Komma ODER ein Doppelpunkt in Wort oder Glosse zerriss
   * den Eintrag lautlos in zusätzliche, falsche Paare - eine wörtliche Glosse mit einer Umschreibung
   * ("is:isst, gerade") ist genau dieser Fall, kein Rand. Seit dieser Story ist `decoding` ein Array aus
   * Paaren (kein Split mehr), darum muss der Rundlauf beide Zeichen unversehrt tragen.
   */
  it("Birkenbihl: ein Wortpaar mit Komma UND Doppelpunkt in der Glosse bleibt EIN Paar", () => {
    const { config, rows } = rundlauf("Birkenbihl",
      [{ text: "She is about to leave.", naturalTranslation: "Sie ist im Begriff zu gehen.",
        decoding: [{ word: "is:about", gloss: "ist:im Begriff zu, gerade" }] }],
      { learningLang: "en", nativeLang: "de" });

    const sentences = config as { sentences: { decoding: { learningWord: string; gloss: string | null }[] }[] };
    expect(sentences.sentences[0].decoding).toEqual([{ learningWord: "is:about", gloss: "ist:im Begriff zu, gerade" }]);
    expect(rows[0].decoding).toEqual([{ word: "is:about", gloss: "ist:im Begriff zu, gerade" }]);
  });
});

describe("Leere Listen", () => {
  /*
   * „Keine angegeben" hat genau EINE Schreibweise. Eine leere Liste wäre die zweite – und nur eine von
   * beiden beantwortet die Frage „hat dieser Eintrag Alternativen?" so, wie ein Leser es erwartet.
   */
  it.each([
    ["Cloze", { index: 1, answer: "a" }, { text: "x {{1}}" }, "gaps", "alternatives"],
    ["List", { value: "a" }, {}, "items", "alternatives"],
    ["Reading", { prompt: "p", answer: "a" }, { text: "t" }, "questions", "choices"],
  ] as const)("%s: eine leere Liste wird nicht gesendet, eine gefüllte schon", (type, row, extra, key, field) => {
    const build = (values: string[]) =>
      (buildTypeConfig(type, [{ ...row, [field]: values }], { ...extra }) as Record<string, Record<string, unknown>[]>)
        [key][0][field] ?? null;

    // Die Gegenprobe steht dabei: ohne sie wäre der Fall auch dann grün, wenn das Feld umbenannt
    // worden oder ganz verschwunden wäre – „richtig weggelassen" sieht dann aus wie „verloren".
    expect(build(["x"])).toEqual(["x"]);
    expect(build([])).toBeNull();
  });

  it("räumt auch beim Speichern auf: leere und nur aus Leerzeichen bestehende Felder fallen weg", () => {
    const { rows } = rundlauf("List", [{ value: "Paris", alternatives: ["", "  ", " Lutetia "] }]);
    expect(rows[0].alternatives).toEqual(["Lutetia"]);
  });

  it("die leere Anfangszeile führt die Listen bereits als Liste", () => {
    for (const type of ["Cloze", "List", "Translation"] as const) {
      expect(emptyRow(type).alternatives).toEqual([]);
    }
    expect(emptyRow("Reading").choices).toEqual([]);
  });
});

/** B-421, AK 7, Anlegen: der Haken des Wortwählers fährt mit der Config zum Server. */
describe("Vokabeln: articleRequired beim Anlegen", () => {
  // Rot, wenn der Anlege-Weg das Feld nicht schreibt oder ein fehlendes Feld als gesetzt liest.
  it("schreibt den Haken – und ohne Haken false", () => {
    const an = buildTypeConfig("Vocabulary", [], { direction: "front-to-back", sourceLang: "fr", targetLang: "de", articleRequired: true },
      { vocabRefs: [{ vocabularyId: 7 }] }) as { articleRequired: boolean };
    const aus = buildTypeConfig("Vocabulary", [], { direction: "front-to-back", sourceLang: "fr", targetLang: "de" },
      { vocabRefs: [{ vocabularyId: 7 }] }) as { articleRequired: boolean };
    expect(an.articleRequired).toBe(true);
    expect(aus.articleRequired).toBe(false);
  });
});

/*
 * B-457: Die Warnung „Lösung fehlt in der Auswahl" vergleicht wie der Server (`StageMechanics.Normalize`,
 * `SchlusszeichenTests.cs`). Jeder Fall wird rot, wenn die zugehörige Regel im Client fehlt: Schlusszeichen,
 * Leerzeichen zwischen ihnen, der Schutz für reine Satzzeichen, der Apostroph – und ein Komma im Satz bleibt.
 */
describe("normalizeAnswer folgt dem Server", () => {
  it.each([
    ["The station ?!", "the station"],
    ["the station ? !", "the station"],
    [" ? ! ", "? !"],
    ["l’école.", "l'école"],
    ["Yes, I am.", "yes, i am"],
  ])("%s → %s", (given, expected) => expect(normalizeAnswer(given)).toBe(expected));

  it("warnt nicht, wenn sich Lösung und Auswahl nur im Schlusszeichen unterscheiden", () => {
    expect(choicesMissAnswer({ answer: "Hello!", choices: ["Hello", "Goodbye"] })).toBe(false);
    expect(choicesMissAnswer({ answer: "Hello!", choices: ["Goodbye"] })).toBe(true);
  });
});

// B-441: „Artikel verlangt" kommt aus der gespeicherten Config zurück. Rot, wenn das Rücklesen den Haken fest auf „aus" setzt.
describe("Rundlauf Vokabel – Artikel verlangt", () => {
  it("liest den gespeicherten Haken zurück", () => {
    expect(configToEditorState("Vocabulary", { articleRequired: true }).extra.articleRequired).toBe(true);
    expect(configToEditorState("Vocabulary", { articleRequired: false }).extra.articleRequired).toBe(false);
    expect(configToEditorState("Vocabulary", {}).extra.articleRequired).toBe(false);
  });
});
