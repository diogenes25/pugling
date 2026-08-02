import { describe, expect, it } from "vitest";
import { buildTypeConfig, configToEditorState, emptyRow } from "./exerciseConfig";
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

  it("Liste: Alternativen bleiben Listen", () => {
    const { rows } = rundlauf("List", [{ value: "Paris", alternatives: ["Paris (FR)", MIT_KOMMA] }]);
    expect(rows[0].alternatives).toEqual(["Paris (FR)", MIT_KOMMA]);
  });

  it("Übersetzung: Alternativen bleiben Listen und die Store-Bindung bleibt erhalten", () => {
    const { rows } = rundlauf("Translation",
      [{ source: "Where do you live?", target: "Wo wohnst du?", alternatives: ["Wo lebst du?", MIT_KOMMA],
        vocabularyId: 7, origSource: "Where do you live?", origTarget: "Wo wohnst du?" }],
      { sourceLang: "en", targetLang: "de" });

    expect(rows[0].alternatives).toEqual(["Wo lebst du?", MIT_KOMMA]);
    expect(rows[0].vocabularyId).toBe(7);
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
