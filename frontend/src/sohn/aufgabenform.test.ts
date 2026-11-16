import { describe, expect, it } from "vitest";
import { aufgabenform, type FormKarte } from "./aufgabenform";

/*
 * B-526 – die Form einer Aufgabe aus den Feldern der Karte. Je Fall steht dabei, welche Änderung ihn rot macht.
 */

const karte = (over: Partial<FormKarte>): FormKarte => ({
  reveal: null, choices: null, letterTiles: null, answerLength: null, audioUrl: null, prompt: "la gare",
  passage: null, decoding: null, cueBox: null, ...over,
});

describe("Antwortart und Arbeitsanweisung", () => {
  it("folgen je Antwortform der Tabelle aus Entscheidung 5", () => {
    // Rot, wenn eine Zeile der Tabelle fehlt oder zwei Formen dieselbe Anweisung bekämen.
    const faelle: [Partial<FormKarte>, string, string][] = [
      [{ choices: ["a", "b"] }, "auswahl", "lektion.choose"],
      [{ letterTiles: ["g", "a", "r", "e"] }, "ordnen", "lektion.order"],
      [{ answerLength: 4 }, "kaestchen", "lektion.boxes"],
      [{}, "freitext", "lektion.write"],
      [{ reveal: "der Bahnhof" }, "selbst", "lektion.self"],
      [{ reveal: "der Bahnhof", displayOnly: true }, "kennenlernen", "lektion.intro"],
    ];
    for (const [felder, antwort, anweisung] of faelle) {
      const form = aufgabenform(karte(felder), false);
      expect([form.antwort, form.anweisung], JSON.stringify(felder)).toEqual([antwort, anweisung]);
    }
  });

  it("nennt den Wortkasten und die reine Höraufgabe beim Namen, auch wenn Kästchen kämen", () => {
    // Rot, wenn die Form des Eingabefelds gewinnt: „Fülle die Kästchen“ sagte nichts über den Wortkasten.
    expect(aufgabenform(karte({ cueBox: ["Klinik", "Patient"], answerLength: 6 }), false).anweisung).toBe("lektion.cueBox");
    expect(aufgabenform(karte({ audioUrl: "/a.mp3", prompt: null }), false).anweisung).toBe("lektion.listen");
    // Mit einer Frage daneben ist die Aufnahme Material, keine Diktat-Aufgabe – und sie bringt Bedienelemente mit,
    // die nicht in die schmale Blase passen. Rot, wenn sie dort landete.
    const hoertext = aufgabenform(karte({ audioUrl: "/a.mp3", prompt: "Wo ist der Bahnhof?" }), false);
    expect([hoertext.anweisung, hoertext.darstellung]).toEqual(["lektion.write", "karte"]);
  });

  it("nennt eine offene Karte nur dann „neu“, wenn der Server sie als Erstkontakt meldet", () => {
    // Rot, wenn jede Anzeigestufe „Neu – präg es dir ein“ hieße: eine Anzeigestufe des Tages zeigt auch Wörter, die
    // das Kind längst kennt (Review B-526). Ohne Angabe (Testkarte) bleibt es bei „neu“.
    const offen = { reveal: "der Bahnhof", displayOnly: true };
    expect(aufgabenform(karte({ ...offen, introduction: true }), false).anweisung).toBe("lektion.intro");
    expect(aufgabenform(karte({ ...offen, introduction: false }), false).anweisung).toBe("lektion.look");
    expect(aufgabenform(karte(offen), false).anweisung).toBe("lektion.intro");
  });

  it("liest eine mitgelieferte Lösung als „nicht getippt“, auch wenn Auswahlfelder dabei wären", () => {
    // Rot, wenn die Antwortfläche einer Selbsteinschätzung als Auswahl erschiene – die Lösung stünde daneben.
    expect(aufgabenform(karte({ reveal: "der Bahnhof", choices: ["a", "b"] }), false).antwort).toBe("selbst");
  });
});

describe("Darstellung", () => {
  it("ist die Sprechblase für Wort oder Satz", () => {
    // Rot, wenn die häufigste Karte – eine Vokabel – als volle Karte ohne Maskottchen erschiene.
    expect(aufgabenform(karte({}), false).darstellung).toBe("blase");
    // Die reine Höraufgabe (kein Text daneben) ist ein Knopf und bleibt in der Blase.
    expect(aufgabenform(karte({ choices: ["a", "b"], audioUrl: "/a.mp3", prompt: null }), false).darstellung).toBe("blase");
  });

  it("ist die volle Karte, sobald Lesetext, Bild, Wortkasten oder Dekodierung dabei sind", () => {
    // Rot, wenn eines dieser Felder in die schmale Blase gezwängt würde. Jedes einzeln, damit keines fehlt.
    const lang: Partial<FormKarte>[] = [
      { passage: "Ein langer Text." }, { imageUrl: "/bild.png" }, { cueBox: ["Klinik"] },
      { decoding: [{ wordId: 1, learningWord: "la" }] },
    ];
    for (const felder of lang) expect(aufgabenform(karte(felder), false).darstellung, JSON.stringify(felder)).toBe("karte");
  });

  it("hängt an den Feldern, nicht an der Textlänge", () => {
    // Absichtserklärung: heute gibt es keine Längenregel, die diesen Fall rot machen könnte. Er steht hier, damit
    // niemand eine einführt – dieselbe Aufgabenart sähe dann mal so, mal so aus (Entscheidung 6).
    expect(aufgabenform(karte({ prompt: "x".repeat(400) }), false).darstellung).toBe("blase");
    // Leere Listen sind kein Material.
    expect(aufgabenform(karte({ cueBox: [], decoding: [] }), false).darstellung).toBe("blase");
  });
});

describe("Diktat (B-597)", () => {
  it("diktiert nur mit Stimme des Geräts und nur, wo der Server ein Diktat schickt", () => {
    // Rot, wenn das Diktat ohne Stimme gälte (die Karte zeigte dann gar nichts), wenn eine Stimme allein schon ein Diktat
    // machte, oder wenn die Anweisung beim Diktat „Schreibe die Antwort“ hieße.
    const diktat = karte({ prompt: "still", dictationSpokenAnswer: "silent" });
    expect([aufgabenform(diktat, true).diktat, aufgabenform(diktat, true).anweisung]).toEqual([true, "lektion.listen"]);
    expect([aufgabenform(diktat, false).diktat, aufgabenform(diktat, false).anweisung]).toEqual([false, "lektion.write"]);
    expect(aufgabenform(karte({ prompt: "still" }), true).diktat).toBe(false);
  });
});
