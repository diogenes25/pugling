import { describe, expect, it } from "vitest";
import type { PositionStatus } from "../lib/types";
import { abschnitte, frist, hauptweg, istTagespflicht, kennenlernAnteil, naechstePositionId, nebenweg, pflichtErfuellt, zielAnteil } from "./pfad";

/*
 * B-525 – die Regeln des Lernpfads. Je Fall steht dabei, welche Änderung ihn rot macht.
 */

const pos = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  goalProgress: 0, dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, imprintedCount: 4, penaltyCoins: 5,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", ...over,
} as PositionStatus);

describe("Abschnitte", () => {
  it("fassen nur aufeinanderfolgende Positionen derselben Unit zusammen", () => {
    // Rot, wenn über die ganze Liste gruppiert wird: dann stünden hier zwei Abschnitte statt drei, und der Pfad
    // verließe die Reihenfolge des Plans.
    const liste = [
      pos({ positionId: 1, seriesUnitId: 2, seriesUnitLabel: "Unit 2" }),
      pos({ positionId: 2, seriesUnitId: 2, seriesUnitLabel: "Unit 2" }),
      pos({ positionId: 3, seriesUnitId: 3, seriesUnitLabel: "Unit 3" }),
      pos({ positionId: 4, seriesUnitId: 2, seriesUnitLabel: "Unit 2" }),
    ];
    expect(abschnitte(liste).map((a) => [a.title, a.positions.map((p) => p.positionId)])).toEqual([
      ["Unit 2", [1, 2]], ["Unit 3", [3]], ["Unit 2", [4]],
    ]);
  });
});

describe("Häkchen", () => {
  it("bekommt eine freie Position nie, auch wenn der Server sie als erfüllt meldet", () => {
    // Rot, wenn `goalMet` allein entscheidet (B-467).
    expect(pflichtErfuellt(pos({ cadence: "None", goalMet: true }))).toBe(false);
    expect(pflichtErfuellt(pos({ cadence: "Weekly", goalMet: true }))).toBe(true);
  });
});

describe("Der nächste Knoten", () => {
  it("ist das erste offene Tagesziel, auch wenn ein offenes Wochenziel davor steht", () => {
    // Rot, wenn schlicht die erste offene Pflicht genommen wird.
    const liste = [
      pos({ positionId: 1, cadence: "Weekly" }),
      pos({ positionId: 2, cadence: "Daily", goalMet: true }),
      pos({ positionId: 3, cadence: "Daily" }),
    ];
    expect(naechstePositionId(liste)).toBe(3);
  });

  it("ist das erste offene Wochenziel, sobald alle Tagesziele erfüllt sind", () => {
    // Rot, wenn Wochenziele gar nicht hervorgehoben werden.
    const liste = [pos({ positionId: 1, cadence: "Daily", goalMet: true }), pos({ positionId: 2, cadence: "Weekly" })];
    expect(naechstePositionId(liste)).toBe(2);
  });

  it("ist keiner, wenn alle Pflichten erfüllt sind oder es nur freie Positionen gibt", () => {
    // Rot, wenn nach erfüllter Pflicht ein freiwilliger Knoten hervorgehoben wird. Die freie Position ist hier
    // absichtlich „nicht erfüllt“ gemeldet – auch dann darf sie nicht der nächste Knoten werden.
    expect(naechstePositionId([pos({ goalMet: true }), pos({ positionId: 2, cadence: "None", goalMet: false })])).toBeNull();
  });
});

describe("Der Ring", () => {
  it("zeigt den Anteil eingeprägter Inhalte und fehlt ohne Pool", () => {
    // Rot, wenn eine Position ohne Inhalte einen (leeren oder vollen) Ring bekäme oder durch 0 geteilt würde.
    expect(kennenlernAnteil(pos({ imprintedCount: 4, poolSize: 10 }))).toBe(0.4);
    expect(kennenlernAnteil(pos({ imprintedCount: 0, poolSize: 0 }))).toBeNull();
    // Ein geschrumpfter Pool darf den Ring nicht über voll hinaus treiben.
    expect(kennenlernAnteil(pos({ imprintedCount: 12, poolSize: 10 }))).toBe(1);
  });

  it("zählt eingeprägte, nicht nur gesehene Wörter (B-643)", () => {
    // Rot, wenn der Ring wieder „eingeführt“ liest: zehn gesehen, drei aus dem Gedächtnis – der Ring ist nicht voll.
    expect(kennenlernAnteil(pos({ introducedCount: 10, imprintedCount: 3, poolSize: 10 }))).toBe(0.3);
  });
});

describe("Der Zielring", () => {
  it("füllt sich mit dem Stand der Pflicht und ist voll genau mit dem Haken", () => {
    // Rot, wenn der innere Ring nur goalMet spiegelt (0.5 → 0) oder ein Teilstand ihn ohne Haken schließt.
    expect(zielAnteil(pos({ goalMet: false, goalProgress: 0.5 }))).toBe(0.5);
    expect(zielAnteil(pos({ goalMet: false, goalProgress: 1 }))).toBeLessThan(1);
    expect(zielAnteil(pos({ goalMet: true, goalProgress: 0 }))).toBe(1);
  });

  it("fehlt an einer freien Position, auch wenn der Server sie als erfüllt meldet", () => {
    // Rot, wenn eine Kür-Position einen vollen Zielring bekäme (B-467: frei heißt goalMet = true).
    expect(zielAnteil(pos({ cadence: "None", goalMet: true, goalProgress: 1 }))).toBeNull();
  });
});

describe("Die Frist", () => {
  // Ein Mittwoch, 22:30 UTC: bis zum Server-Tagesende sind es anderthalb Stunden.
  const mittwoch = new Date(Date.UTC(2026, 10, 11, 22, 30));
  const sonntag = new Date(Date.UTC(2026, 10, 15, 20, 0));

  it("rechnet ein Tagesziel bis Mitternacht UTC und rundet auf", () => {
    // Rot, wenn nach Ortszeit gerechnet oder abgerundet wird (dann stünde „noch 1 Std.“ bzw. je nach Zeitzone
    // etwas anderes) – die Anzeige widerspräche der Buchung.
    expect(frist(pos({ cadence: "Daily" }), mittwoch)).toEqual({ art: "stunden", stunden: 2 });
  });

  it("nennt ein Wochenziel „bis Sonntag“ und erst am Sonntag die Stunden", () => {
    // Rot, wenn ein Wochenziel wie ein Tagesziel täglich abliefe.
    expect(frist(pos({ cadence: "Weekly" }), mittwoch)).toEqual({ art: "sonntag" });
    expect(frist(pos({ cadence: "Weekly" }), sonntag)).toEqual({ art: "stunden", stunden: 4 });
  });

  it("gibt es nur für offene Pflicht", () => {
    // Rot, wenn ein erfülltes Ziel oder eine freie Position weiter eine Frist zeigte.
    expect(frist(pos({ goalMet: true }), mittwoch)).toBeNull();
    expect(frist(pos({ cadence: "None", goalMet: false }), mittwoch)).toBeNull();
  });
});

describe("„Los“", () => {
  it("führt ins Üben, solange etwas fällig oder neu ist, und lässt den Test als Nebenweg", () => {
    // Rot, wenn „Los“ das Kind in den Test schickt, bevor es geübt hat.
    const p = pos({ dueCount: 3, introducedCount: 4, poolSize: 10 });
    expect([hauptweg(p), nebenweg(p)]).toEqual(["practice", "test"]);
  });

  it("startet den Test direkt, wenn nichts zu üben ist, und lässt das Üben als Nebenweg", () => {
    // Rot, wenn „Los“ in eine leere Übungsrunde führte – dort stünde nur „Nichts fällig“.
    const p = pos({ dueCount: 0, introducedCount: 10, poolSize: 10 });
    expect([hauptweg(p), nebenweg(p)]).toEqual(["test", "practice"]);
  });

  it("startet den Test auch, wenn die Tagesdosis verbraucht, aber noch nicht alles kennengelernt ist", () => {
    // Rot, wenn „noch nicht alles kennengelernt“ als „es gibt etwas zu üben“ gelesen wird: der Server zählt Neues
    // schon in `dueCount` mit und kappt es an der Dosis – 0 heißt, heute kommt nichts mehr (Review B-525).
    expect(hauptweg(pos({ dueCount: 0, introducedCount: 4, poolSize: 40 }))).toBe("test");
  });

  it("bietet an einem nicht prüfbaren Tag keinen Test an und bleibt spielbar", () => {
    // Rot, wenn `testable = false` den Test trotzdem anböte (er antwortete mit 400, B-96) oder die Position
    // ohne jeden Knopf dastünde.
    const p = pos({ testable: false, dueCount: 0, introducedCount: 10, poolSize: 10 });
    expect([hauptweg(p), nebenweg(p)]).toEqual(["practice", null]);
  });

  it("lässt eine prüfbare Position ohne Karteikasten erst üben", () => {
    // Rot, wenn ohne Leitner nur der Test bliebe – das Kind würde Wörter gefragt, die es nie gesehen hat (B-454).
    const p = pos({ useLeitner: false, dueCount: 0, introducedCount: 0, poolSize: 8 });
    expect([hauptweg(p), nebenweg(p)]).toEqual(["practice", "test"]);
  });
});

describe("Nacharbeit bis erledigt (B-636)", () => {
  // Rot, wenn die Nacharbeit wie ein Wochenziel behandelt wird: dann wäre das offene Wochenziel davor der nächste Knoten.
  it("ist eine Tagespflicht und wird vor dem Wochenziel hervorgehoben", () => {
    expect(istTagespflicht("UntilDone")).toBe(true);
    expect(istTagespflicht("Weekly")).toBe(false);
    const liste = [pos({ positionId: 1, cadence: "Weekly" }), pos({ positionId: 2, cadence: "UntilDone" })];
    expect(naechstePositionId(liste)).toBe(2);
  });
});
