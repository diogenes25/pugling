import { describe, expect, it } from "vitest";
import { FREETEXT_SUBJECT, seriesFormValues, seriesPatch, type SeriesFormValues } from "./seriesPatch";
import type { TextbookSeriesResponse } from "../lib/types";

/*
 * B-123. Die Zusicherung, um die es hier geht, ist am Bildschirm nicht sichtbar: ein unverändertes Feld
 * darf gar nicht mitgeschickt werden. Genau daran hängt die Sicherheit von Entscheidung 2 der Story —
 * „leeren" entsteht aus dem Vergleich gegen den Ladezustand, nicht aus einer dritten Select-Option.
 */

function reihe(overrides: Partial<TextbookSeriesResponse> = {}): TextbookSeriesResponse {
  return {
    id: 7, name: "Access", slug: "access", publisherId: 3, publisherName: "Klett",
    subjectName: "Englisch", subjectId: 1, schoolTypes: "Gymnasium", sourceLanguage: "en",
    targetLanguage: "de", notes: "Aufbau", ownerAdultId: 1, isOwn: true, unitCount: 2,
    gradeMin: 8, gradeMax: 8, createdAt: "2026-01-01T00:00:00Z", ...overrides,
  };
}

const geladen = (o: Partial<TextbookSeriesResponse> = {}) => seriesFormValues(reihe(o));
const patch = (form: SeriesFormValues, o: Partial<TextbookSeriesResponse> = {}) =>
  seriesPatch(geladen(o), form);

describe("seriesFormValues", () => {
  it("füllt die Formularfelder als Strings, `null` wird zum leeren String", () => {
    // `subjectName` muss hier mit auf `null`: eine Reihe ohne Fach-Id, die noch einen Namen trägt, ist
    // NICHT „nichts", sondern der Freitext-Zustand – der Fall darunter.
    const werte = seriesFormValues(reihe({
      publisherId: null, subjectId: null, subjectName: null, notes: null, sourceLanguage: null,
    }));

    expect(werte).toEqual({
      name: "Access", publisherId: "", subjectId: "", schoolTypes: "Gymnasium",
      sourceLanguage: "", targetLanguage: "de", notes: "",
    });
  });

  it("bildet ein Fach ohne Katalog-Id auf den Freitext-Sentinel ab", () => {
    // B-143, Zustand A: die Reihe zeigt „Englisch", der Katalog kennt das Fach nicht (mehr). Ohne den
    // Sentinel hieße `""` hier zweierlei, und das Formular könnte den Zustand nicht anzeigen.
    expect(seriesFormValues(reihe({ subjectId: null })).subjectId).toBe(FREETEXT_SUBJECT);
  });

  it("hält den Sentinel von jeder echten Fach-Id fern", () => {
    // Eine Eigenschaft der Wahl, nicht des Typs: `FREETEXT_SUBJECT` darf nie das Ergebnis von
    // `String(id)` sein können, sonst verwechselte das Formular einen echten Wert mit dem Platzhalter.
    expect(Number.isNaN(Number(FREETEXT_SUBJECT))).toBe(true);
  });
});

describe("seriesPatch – nur das Geänderte", () => {
  it("schickt nichts, wenn nichts angefasst wurde", () => {
    expect(patch(geladen())).toBeNull();
  });

  it("schickt genau ein Feld, wenn genau eines geändert wurde", () => {
    expect(patch({ ...geladen(), name: "Green Line" })).toEqual({ name: "Green Line" });
  });

  it("trimmt Name und Notiz, und erkennt eine reine Leerzeichen-Änderung nicht als Änderung", () => {
    expect(patch({ ...geladen(), name: "  Access  ", notes: " Aufbau " })).toBeNull();
  });
});

describe("seriesPatch – leeren gegen unverändert", () => {
  it("setzt clearPublisherId, wenn ein gesetzter Verlag weggenommen wird", () => {
    expect(patch({ ...geladen(), publisherId: "" })).toEqual({ clearPublisherId: true });
  });

  it("schickt beim Wechsel die neue Id statt des Schalters", () => {
    expect(patch({ ...geladen(), publisherId: "9" })).toEqual({ publisherId: 9 });
  });

  it("schickt gar nichts, wenn schon vorher kein Verlag stand", () => {
    // Der Fall, der ohne Ladezustand-Vergleich einen bestehenden Wert gelöscht hätte: „keine Angabe"
    // ist hier der Ausgangszustand, nicht eine Handlung.
    expect(patch(geladen({ publisherId: null }), { publisherId: null })).toBeNull();
  });

  it("schickt beim Leeren des Fachs nur den Schalter – der Server räumt den Namen mit", () => {
    // `clearSubject` nimmt Id UND Namen (TextbookSeriesController, wie CreatorProfiles/Textbooks).
    // Den Namen zusätzlich zu schicken wäre doppelt – und beim ersten Umbenennen des Schalters wäre
    // hier grün, was auf der Leitung rot ist. Darum trägt der Server die Regel, nicht das Formular.
    expect(patch({ ...geladen(), subjectId: "" })).toEqual({ clearSubject: true });
  });

  it("schickt die Schulart als Sentinel, nicht als Schalter", () => {
    // Der dritte Mechanismus der Datei, und der einzige, dessen Draht-Wert sonst nirgends geprüft wird.
    expect(patch({ ...geladen(), schoolTypes: "None" })).toEqual({ schoolTypes: "None" });
  });

  it("schickt beim Fachwechsel nur die Id — den Namen leitet der Server ab", () => {
    // Bis B-142 ging der Anzeigename mit, weil der Server ihn nicht aus der Id ableitete. Diese Stelle
    // war die einzige, die das kompensierte; der Fall pinnt jetzt, dass sie es NICHT mehr tut.
    expect(patch({ ...geladen(), subjectId: "2" })).toEqual({ subjectId: 2 });
  });

  it("leert eine Sprache über den leeren String, ohne Schalter", () => {
    // Anders als die beiden Referenzen: der Server macht aus `""` ein `null`.
    expect(patch({ ...geladen(), sourceLanguage: "" })).toEqual({ sourceLanguage: "" });
  });

  it("sammelt mehrere Änderungen in einem Rumpf", () => {
    expect(patch({ ...geladen(), name: "Neu", publisherId: "", schoolTypes: "Realschule" }))
      .toEqual({ name: "Neu", clearPublisherId: true, schoolTypes: "Realschule" });
  });

  // ── B-143: zwei Zustände, die das Modell erlaubt und das Formular bisher nicht ausdrücken konnte ──

  it("lässt ein Freitext-Fach unangetastet, solange niemand es anfasst", () => {
    // Der Kern des Defekts: vorher war `loaded.subjectId === form.subjectId === ""`, also kein Diff –
    // aber auch kein Weg, den Freitext loszuwerden. Jetzt sind beide der Sentinel: immer noch kein Diff,
    // und das ist hier die RICHTIGE Antwort. Ein Speichern mit anderem Feld darf ihn nicht mitnehmen.
    const ohneKatalogFach = { subjectId: null };
    expect(patch({ ...geladen(ohneKatalogFach), name: "Access neu" }, ohneKatalogFach))
      .toEqual({ name: "Access neu" });
  });

  it("entfernt ein Freitext-Fach über die Leer-Option des Fach-Felds", () => {
    // Entscheidung 3: kein eigener Knopf. Der Wechsel vom Sentinel auf `""` IST der Unterschied, und
    // `clearSubject` räumt Id und Namen gemeinsam.
    const ohneKatalogFach = { subjectId: null };
    expect(patch({ ...geladen(ohneKatalogFach), subjectId: "" }, ohneKatalogFach))
      .toEqual({ clearSubject: true });
  });

  it("lässt eine Schulart-Kombination unangetastet, solange niemand sie anfasst", () => {
    // Zustand B. Er ist heute nur über die API herstellbar (Entscheidung 5), darum steht der
    // Ausgangszustand hier von Hand – ein E2E könnte ihn nicht erzeugen.
    const kombination = { schoolTypes: "Realschule, Gymnasium" as SeriesFormValues["schoolTypes"] };
    expect(patch({ ...geladen(kombination), notes: "Anders" }, kombination))
      .toEqual({ notes: "Anders" });
  });

  it("speichert die Schulart, wenn der Nutzer sie aktiv wählt – auch für alle", () => {
    // Entscheidung 4: der Schutz gilt dem Versehen, nicht der Absicht. `None` ist ein echter Wert.
    const kombination = { schoolTypes: "Realschule, Gymnasium" as SeriesFormValues["schoolTypes"] };
    expect(patch({ ...geladen(kombination), schoolTypes: "None" }, kombination))
      .toEqual({ schoolTypes: "None" });
  });
});
