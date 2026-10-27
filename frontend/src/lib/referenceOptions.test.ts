import { describe, expect, it } from "vitest";
import { unitOptions } from "./referenceOptions";
import type { SeriesUnitResponse } from "./types";

/*
 * B-195. `useAsync` behält seine `data` über einen Wechsel der Abhängigkeit — im Ladefenster nach einem
 * Reihenwechsel standen darum noch die Units der ALTEN Reihe zur Auswahl. Abschicken endete am Server mit
 * `400` (`TextbooksController.cs:146-147`): ein vermeidbarer Fehlschlag, den der Vater sich nicht erklären
 * konnte.
 *
 * Die Regel steht hier und nicht am Bildschirm, weil sie eine reine Funktion ist — und weil sie als
 * Pflicht-Parameter der Signatur die zweite Aufrufstelle gar nicht erst vergessen lassen kann. Genau auf
 * diesem Weg verlor B-172 seinen achten Fall (B-194).
 */

/*
 * Ohne `as`-Cast, und das ist der Punkt: Der Typcheck soll hier genau das prüfen, worauf die Regel steht —
 * dass `seriesId` wirklich am Vertrags-DTO hängt. Ein Cast nähme ihm die Aufgabe, und ein Umbau des
 * Vertrags bräche diesen Test dann still nicht mehr.
 */
function unit(over: Partial<SeriesUnitResponse> = {}): SeriesUnitResponse {
  return {
    id: 1, seriesId: 10, grade: null, orderIndex: 1, label: "Unit 1",
    bookType: "Textbook", topics: [], grammar: null, vocabularyNotes: null,
    createdAt: "2026-09-03T00:00:00Z", grammarTopicIds: [], ...over,
  };
}

describe("unitOptions – eine Unit gehört genau einer Reihe", () => {
  it("bietet die Units der gewählten Reihe vollständig an", () => {
    const optionen = unitOptions([unit({ id: 1 }), unit({ id: 2, label: "Unit 2" })], 10);

    // Der Ausgangszustand, ohne den die Zusicherung darunter auch für ein „gibt nie etwas zurück" gälte.
    expect(optionen.map((o) => o.value)).toEqual(["1", "2"]);
  });

  it("lässt die Units einer anderen Reihe weg – der Fall aus dem Ladefenster", () => {
    const optionen = unitOptions(
      [unit({ id: 1, seriesId: 10 }), unit({ id: 9, seriesId: 99, label: "Fremde Unit" })], 10);

    expect(optionen.map((o) => o.value)).toEqual(["1"]);
  });

  it("bietet ohne gewählte Reihe gar nichts an", () => {
    // Die vorliegenden Daten stammen dann zwingend von vorhin: ohne Reihe fragt der Bildschirm nichts ab.
    expect(unitOptions([unit({ id: 1 })], null)).toEqual([]);
  });

  it("stellt die Klassenstufe voran, wo es eine gibt", () => {
    // Die Beschriftung ist der Grund, warum es diese Funktion überhaupt gibt: eine Reihe trägt dieselbe
    // Unit-Nummer über mehrere Bände. Der Filter darf sie nicht mitgenommen haben.
    expect(unitOptions([unit({ grade: 8, label: "Unit 3" })], 10)[0].label).toBe("Kl. 8: Unit 3");
    expect(unitOptions([unit({ grade: null, label: "Unit 3" })], 10)[0].label).toBe("Unit 3");
  });
});
