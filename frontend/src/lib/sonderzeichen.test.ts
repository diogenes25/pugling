import { describe, expect, it, vi } from "vitest";
import { einsetzen, leistenZeichen, ohneDiakritika } from "./sonderzeichen";

/*
 * B-661 – die Zeichen der Sonderzeichen-Leiste. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("leistenZeichen", () => {
  // Entscheidung 1. Rot, wenn Französisch das „î“ verliert, das Friedrich blockiert hat, oder wenn eine Sprache ohne
  // Eintrag doch eine Leiste bekäme.
  it("bietet je Sprache ihre feste Menge, ohne Eintrag keine", () => {
    expect(leistenZeichen("fr")).toContain("î");
    expect(leistenZeichen("FR")).toContain("ç");
    expect(leistenZeichen("de")).toEqual(["ä", "ö", "ü", "ß"]);
    expect(leistenZeichen("en")).toEqual([]);
    expect(leistenZeichen(null)).toEqual([]);
  });

  // Entscheidung 3. Rot, wenn die Wort-Ergänzung entfällt (ein „ñ“ in einem französischen Wort wäre unabschreibbar).
  // Dass sie im Test nie kommt, hält die Struktur (die Antwortfläche bekommt kein Wort) und Lernkarte.test.tsx, Fall 3.
  it("ergänzt die Sonderzeichen des Wortes", () => {
    expect(leistenZeichen("fr", "le cañon")).toContain("ñ");
    expect(leistenZeichen("en", "Señor")).toEqual(["ñ"]);
    // Großbuchstaben kommen klein, ASCII und Satzzeichen nie.
    expect(leistenZeichen(null, "Ëtre, à l'école")).toEqual(["ë", "à", "é"]);
  });

  // Rot ohne die NFC-Normalisierung: ein zerlegt gespeichertes Wort brächte keine Taste mit.
  it("bietet auch für ein zerlegt gespeichertes Wort das ganze Zeichen an", () => {
    expect(leistenZeichen(null, "di\u0302ner")).toEqual(["î"]);
  });
});

describe("ohneDiakritika", () => {
  // Entscheidung 2 (nur der Hinweis). Rot, wenn die Faltung das „î“ stehen lässt oder ß/œ zerlegt.
  it("nimmt Zeichen auf und unter dem Buchstaben weg, nichts sonst", () => {
    expect(ohneDiakritika("dîner")).toBe("diner");
    expect(ohneDiakritika("ça, élève")).toBe("ca, eleve");
    expect(ohneDiakritika("Straße, œuf")).toBe("Straße, œuf");
  });
});

describe("einsetzen", () => {
  // Rot, wenn das Zeichen am Ende statt an der Schreibmarke landet oder eine Auswahl nicht ersetzt.
  it("setzt an der Schreibmarke ein und ersetzt eine Auswahl", () => {
    const feld = document.createElement("input");
    feld.value = "dner";
    feld.setSelectionRange(1, 1);
    const onWert = vi.fn();
    einsetzen(feld, "dner", "î", onWert);
    expect(onWert).toHaveBeenCalledWith("dîner");

    feld.value = "dXner";
    feld.setSelectionRange(1, 2);
    einsetzen(feld, "dXner", "î", onWert);
    expect(onWert).toHaveBeenLastCalledWith("dîner");
  });

  it("hängt ohne Feld ans Ende an", () => {
    const onWert = vi.fn();
    einsetzen(null, "ma", "ï", onWert);
    expect(onWert).toHaveBeenCalledWith("maï");
  });
});
