import { describe, expect, it } from "vitest";
import { fehlerartText, verlaufFenster, verlaufTage } from "./VaterAuswertung";
import type { DailyEvaluation } from "../lib/types";

const row = (day: string, subjectId: number | null, extra: Partial<DailyEvaluation> = {}): DailyEvaluation => ({
  day, subjectId, subjectName: subjectId ? "Französisch" : "", newWords: 0, consolidatedWords: 0, fellBackWords: 0,
  correctAnswers: 1, wrongAnswers: 0, practicedSeconds: 0, errorKinds: {}, dutiesMet: 0, dutiesMissed: 0, live: false, ...extra,
});

describe("verlaufTage (B-733, AK 7)", () => {
  // Rot, wenn ein Tag ohne Zeile aus dem Verlauf fällt (dann sähe der Vater keine Lücke) oder Zeilen eines Tages verloren gehen.
  it("führt jeden Tag des Zeitraums, Tage ohne Zeile als Lücke mit leerer Liste", () => {
    const tage = verlaufTage([row("2026-11-30", 1), row("2026-11-27", 1), row("2026-11-27", null)], "2026-11-27", "2026-11-30");
    expect(tage.map((t) => [t.day, t.rows.length])).toEqual([
      ["2026-11-30", 1], ["2026-11-29", 0], ["2026-11-28", 0], ["2026-11-27", 2],
    ]);
  });
});

describe("verlaufFenster (B-733, Review: Mitternacht)", () => {
  // Rot mit `toISOString()` als „heute": um 00:30 deutscher Zeit ist der UTC-Tag noch gestern.
  it("endet am Lerntag in deutscher Zeit, auch kurz nach Mitternacht", () => {
    expect(verlaufFenster(new Date("2026-11-26T23:30:00Z"))).toEqual({ from: "2026-10-31", to: "2026-11-27" });
    expect(verlaufFenster(new Date("2026-11-26T12:00:00Z"))).toEqual({ from: "2026-10-30", to: "2026-11-26" });
  });
});

describe("verlaufTage – keine Zeile geht verloren", () => {
  // Rot, wenn eine Zeile hinter dem Fensterende still wegfällt.
  it("wächst bis zur jüngsten Zeile des Servers", () => {
    const tage = verlaufTage([row("2026-11-28", 1)], "2026-11-26", "2026-11-27");
    expect(tage.map((t) => [t.day, t.rows.length])).toEqual([["2026-11-28", 1], ["2026-11-27", 0], ["2026-11-26", 0]]);
  });
});

describe("fehlerartText (B-733)", () => {
  // Rot, wenn eine Art unübersetzt bleibt, ein unbekannter Schlüssel verschwindet oder die Reihenfolge nicht nach Häufigkeit geht.
  it("nennt die Arten deutsch, häufigste zuerst, Unbekanntes roh", () => {
    expect(fehlerartText({ Typo: 1, Accent: 3, Neu: 2 })).toBe("Akzent 3×, Neu 2×, Tippfehler 1×");
    expect(fehlerartText({ WrongWord: 1, Synonym: 1 })).toBe("Synonym 1×, falsches Wort 1×");
    expect(fehlerartText({})).toBe("");
  });
});
