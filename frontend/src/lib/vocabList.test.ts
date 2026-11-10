import { describe, expect, it } from "vitest";
import { parseVocabList } from "./vocabList";

describe("parseVocabList", () => {
  it.each([
    ["Tab", "la ville\tdie Stadt"],
    ["Semikolon", "la ville ; die Stadt"],
    ["Halbgeviertstrich", "la ville – die Stadt"],
    ["Bindestrich mit Leerzeichen", "la ville - die Stadt"],
    ["Gleichheitszeichen", "la ville = die Stadt"],
  ])("trennt am %s", (_name, line) => {
    expect(parseVocabList(line).pairs).toEqual([{ word: "la ville", translation: "die Stadt", alternatives: [] }]);
  });

  it("lässt einen Bindestrich im Wort stehen", () => {
    expect(parseVocabList("le week-end - das Wochenende").pairs[0].word).toBe("le week-end");
    // Ohne anderen Trenner ist die Zeile kein Paar - sie wird gemeldet, nicht am Bindestrich zerschnitten.
    expect(parseVocabList("le week-end").rejected).toEqual(["le week-end"]);
  });

  it("nimmt bei gemischten Trennern den verlässlichsten – Tab vor Semikolon vor Strich", () => {
    // Eine bewusste Rangfolge: ein Strich kommt in Wörtern und Wendungen vor, ein Tab nie.
    expect(parseVocabList("le va-et-vient – das Kommen\tdas Hin und Her").pairs[0])
      .toEqual({ word: "le va-et-vient – das Kommen", translation: "das Hin und Her", alternatives: [] });
    expect(parseVocabList("la ville — die Stadt").pairs[0].translation).toBe("die Stadt");
  });

  it("behält ein Komma in der Übersetzung", () => {
    expect(parseVocabList("les déchets;Abfall, Müll").pairs[0].translation).toBe("Abfall, Müll");
  });

  it("liest weitere Spalten als gleichwertige Übersetzungen", () => {
    expect(parseVocabList("grand;groß;riesig").pairs[0].alternatives).toEqual(["riesig"]);
  });

  it("überspringt Leerzeilen und meldet Zeilen ohne Trenner", () => {
    const r = parseVocabList("la ville;die Stadt\n\nUnité 1\n le chien ; der Hund \r\n");
    expect(r.pairs.map((p) => p.word)).toEqual(["la ville", "le chien"]);
    expect(r.rejected).toEqual(["Unité 1"]);
  });

  it("hält ein wiederholtes Paar einmal, ein Homonym zweimal", () => {
    const r = parseVocabList("la ville;die Stadt\nLa Ville;Die Stadt\nla tour;der Turm\nla tour;die Runde");
    expect(r.pairs.map((p) => p.translation)).toEqual(["die Stadt", "der Turm", "die Runde"]);
  });
});
