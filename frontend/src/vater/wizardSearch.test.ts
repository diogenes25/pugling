import { describe, expect, it } from "vitest";
import { wizardSearchParams, type WizardFilter } from "./wizardSearch";

/*
 * B-18. Die Filter des Assistenten-Schritts „Übungen".
 *
 * Der Bildschirm zeigt nicht, welche Query beim Server ankommt — und genau dort saß die Lücke der Story:
 * Der Server konnte nach Art, Typ und Quelle filtern, der Assistent schickte es nur nie mit.
 */

const leer: WizardFilter = {
  subjectId: 4, grade: undefined, schoolType: undefined,
  search: "", categoryId: "", type: "", source: "",
};

describe("wizardSearchParams – leer heißt „alle“, nicht „leerer Wert“", () => {
  it("schickt einen ungesetzten Filter gar nicht mit", () => {
    // `categoryId: ""` als Query wäre für den Server ein Filter auf nichts – die Trefferliste bliebe
    // ohne sichtbaren Grund leer.
    expect(wizardSearchParams(leer)).toEqual({
      subjectId: 4, grade: undefined, schoolType: undefined,
      search: undefined, categoryId: undefined, type: undefined, source: undefined, take: undefined,
    });
  });

  it("nimmt die drei neuen Filter mit, sobald sie gesetzt sind", () => {
    const p = wizardSearchParams({ ...leer, categoryId: 7, type: "Cloze", source: "Green Line 1" });

    expect(p.categoryId).toBe(7);
    expect(p.type).toBe("Cloze");
    expect(p.source).toBe("Green Line 1");
  });

  it("trimmt die Freitexte und wirft reine Leerzeichen weg", () => {
    const p = wizardSearchParams({ ...leer, search: "  Drill ", source: "   " });

    expect(p.search).toBe("Drill");
    expect(p.source).toBeUndefined();
  });

  it("hält Quelle und Suchtext getrennt", () => {
    // Die Quelle benennt eine Lehrwerk-Stelle. In die Titelsuche gefaltet träfe „Unit 1" jede Übung,
    // deren Titel zufällig eine Unit erwähnt – deshalb zwei Parameter, nicht einer.
    const p = wizardSearchParams({ ...leer, search: "Drill", source: "Unit 1" });

    expect(p.search).toBe("Drill");
    expect(p.source).toBe("Unit 1");
  });

  it("reicht Klasse und Schulart unverändert durch", () => {
    const p = wizardSearchParams({ ...leer, grade: 8, schoolType: "Gymnasium" });

    expect(p.grade).toBe(8);
    expect(p.schoolType).toBe("Gymnasium");
  });

  /*
   * Der Kern des „Alle wählen"-Nachschlags: Er muss DIESELBEN Filter fahren wie die Liste, nur mit
   * höherem `take`. Liefe er auf einer anderen Abbildung, übernähme der Knopf andere Übungen, als der
   * Vater vor sich sieht — und das fiele erst am fertigen Plan auf.
   */
  it("ändert mit `take` nur die Seitengröße, keinen Filter", () => {
    const filter: WizardFilter = { ...leer, categoryId: 7, type: "Cloze", source: "Green Line 1", search: "Drill" };
    const { take: _ohne, ...liste } = wizardSearchParams(filter);
    const { take, ...nachschlag } = wizardSearchParams(filter, 500);

    expect(take).toBe(500);
    expect(nachschlag).toEqual(liste);
  });
});
