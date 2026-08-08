import { describe, expect, it } from "vitest";
import {
  applySeriesChange,
  derivableValues,
  isDerived,
  type DerivableField,
  type DerivableValues,
} from "./seriesDerivation";

const english = { subjectId: 5, sourceLanguage: "en", targetLanguage: "de" };
const french = { subjectId: 5, sourceLanguage: "fr", targetLanguage: "de" };
const maths = { subjectId: 9, sourceLanguage: null, targetLanguage: null };

const form = (over: Partial<DerivableValues> = {}): DerivableValues =>
  ({ subjectId: "5", sourceLang: "en", targetLang: "de", ...over });

const touched = (...fields: DerivableField[]) => new Set<DerivableField>(fields);

describe("derivableValues", () => {
  it("macht aus fehlenden Angaben einen leeren String statt null", () => {
    expect(derivableValues(maths)).toEqual({ subjectId: "9", sourceLang: "", targetLang: "" });
    expect(derivableValues(undefined)).toEqual({ subjectId: "", sourceLang: "", targetLang: "" });
  });
});

describe("isDerived", () => {
  it("meldet einen Wert, der wirklich aus der Reihe kommt", () => {
    expect(isDerived("sourceLang", form(), english, touched())).toBe(true);
  });

  // Der Kern von B-126: beim Bearbeiten eines gespeicherten Profils ist NICHTS berührt.
  it("meldet einen selbst gesetzten Wert NICHT als übernommen", () => {
    expect(isDerived("sourceLang", form({ sourceLang: "fr" }), english, touched())).toBe(false);
  });

  it("meldet nichts, wenn die Reihe zu dem Feld schweigt", () => {
    expect(isDerived("sourceLang", form({ sourceLang: "" }), maths, touched())).toBe(false);
  });

  it("meldet nichts ohne gewählte Reihe", () => {
    expect(isDerived("sourceLang", form(), undefined, touched())).toBe(false);
  });

  it("meldet nichts für ein berührtes Feld, auch bei gleichem Wert", () => {
    expect(isDerived("sourceLang", form(), english, touched("sourceLang"))).toBe(false);
  });

  // Das benannte Hauptrisiko der Story: das Fach reist als `number` in der Reihe und als `string` im
  // Formular. Rutscht die Umrechnung, meldete das Fach nie mehr „abgeleitet" – und zwar lautlos.
  it("meldet das Fach trotz number-zu-string-Umrechnung", () => {
    expect(isDerived("subjectId", form(), english, touched())).toBe(true);
  });
});

describe("applySeriesChange", () => {
  it("füllt leere bzw. vorbelegte Felder aus der neuen Reihe (B-67 bleibt)", () => {
    const next = applySeriesChange(form({ sourceLang: "en", subjectId: "" }), touched(), undefined, french);
    expect(next.sourceLang).toBe("fr");
    expect(next.subjectId).toBe("5");
  });

  it("lässt ein abgeleitetes Feld der neuen Reihe folgen", () => {
    const next = applySeriesChange(form(), touched(), english, maths);
    expect(next.subjectId).toBe("9");
  });

  it("lässt einen selbst gesetzten Wert stehen, wenn die neue Reihe schweigt", () => {
    const next = applySeriesChange(form({ sourceLang: "fr" }), touched(), english, maths);
    expect(next.sourceLang).toBe("fr");
  });

  // Ein geleertes Sprachfeld käme nie in der DB an (kein `clearSourceLang` im Vertrag, Server überliest
  // `null`) – die Oberfläche meldete „Gespeichert." und zeigte danach wieder den alten Wert.
  it("fällt beim Leeren auf die Vorgabe zurück statt auf den leeren String", () => {
    const next = applySeriesChange(form(), touched(), english, maths);
    expect(next.sourceLang).toBe("en");
    expect(next.targetLang).toBe("de");
    // Beim Fach ist "" ein echter Zustand – `clearSubject` kann ihn übertragen.
    expect(applySeriesChange(form(), touched(), english, undefined).subjectId).toBe("");
  });

  // Ein in einer früheren Sitzung gesetzter Wert ist so wenig zu überschreiben wie ein berührter –
  // `touched` weiß von ihm nur nichts.
  it("verwirft einen geladenen Profilwert nicht, wenn die neue Reihe etwas anderes sagt", () => {
    const saved = form({ sourceLang: "fr" });
    const next = applySeriesChange(saved, touched(), english, french, saved);
    expect(next.sourceLang).toBe("fr");
  });

  it("füllt bei einem neuen Profil weiter aus der Reihe, obwohl es nichts Geladenes gibt", () => {
    const next = applySeriesChange(form({ sourceLang: "en" }), touched(), undefined, french, undefined);
    expect(next.sourceLang).toBe("fr");
  });

  it("räumt bei werkunabhängig (keine Reihe gewählt) die abgeleiteten Felder ab", () => {
    const next = applySeriesChange(form(), touched(), english, undefined);
    expect(next.subjectId).toBe("");
    expect(next.sourceLang).toBe("en");
  });

  it("fasst ein berührtes Feld nie an", () => {
    const next = applySeriesChange(form(), touched("sourceLang"), english, french);
    expect(next.sourceLang).toBe("en");
  });

  // Beim Bauen selbst hereingelaufen: die Funktion kopierte den ganzen Formularzustand, und der
  // Aufrufer spreizt das Ergebnis – die alte `seriesId` überschrieb dabei die frisch gewählte.
  it("gibt genau die drei ableitbaren Felder zurück, kein fremdes mit", () => {
    const next = applySeriesChange(form(), touched(), english, french);
    expect(Object.keys(next).sort()).toEqual(["sourceLang", "subjectId", "targetLang"]);
  });

  // Ohne diesen Fall wäre die Vorgabe eines neuen Profils weg, sobald jemand ein Mathe-Werk wählt.
  it("räumt die Sprachvorgabe eines neuen Profils nicht weg", () => {
    const next = applySeriesChange(form(), touched(), undefined, maths);
    expect(next.sourceLang).toBe("en");
    expect(next.targetLang).toBe("de");
    expect(next.subjectId).toBe("9");
  });
});
