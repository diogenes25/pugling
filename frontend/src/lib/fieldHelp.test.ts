import { describe, expect, it } from "vitest";
import { FIELD_HELP } from "./fieldHelp";

/*
 * B-201: Derselbe Hilfetext `pointsGoalMet` steht an zwei Orten – auf der Plan-Seite und im Assistenten
 * (`VaterWizard.tsx`, `topic="pointsGoalMet"`). `fieldHelp.ts` hält bewusst EINE Formulierung je Begriff,
 * also muss dieser eine Text an beiden Orten stimmen: auf der Plan-Seite wird die Empfehlung der Übung
 * vorbelegt und eine Änderung gilt für diese Position (seit B-191 nicht darüber hinaus), im Assistenten
 * gewinnt die gewählte Intensität (Entscheidung zu B-186).
 */
describe("FIELD_HELP.pointsGoalMet – stimmt an beiden Orten", () => {
  const text = FIELD_HELP.pointsGoalMet.text;

  // Rot, solange der alte Satz steht: „bleibt die Änderung" galt über die Positionsgrenze hinweg nie.
  it("verspricht nicht, dass eine Änderung über die Position hinaus bleibt", () => {
    expect(text).not.toMatch(/bleibt die Änderung/);
    expect(text).toMatch(/gilt für diese eine Position/);
  });

  // Rot, wenn der Text den Assistenten verschweigt – dort erbt die Position keine Empfehlung.
  it("sagt, dass der Assistent den Wert aus der Intensität vorbelegt", () => {
    expect(text).toMatch(/Assistent belegt den Wert .*Intensität vor/);
  });

  // Rot, wenn beim Umschreiben die Währung oder der Shop-Bezug verloren geht (Regel über FIELD_HELP).
  it("nennt weiter die Währung und wofür sie ausgegeben wird", () => {
    expect(text).toMatch(/🪙 Münzen/);
    expect(text).toMatch(/Familien-Shop/);
  });
});

// B-551: der Stufenplan gilt je Plantag (`StageStep(DayNumber, Stage)`), nicht je Leitner-Fach. Rot, wenn der Text wieder
// eine Übersteuerung je Fach verspricht.
describe("Feldhilfe Stufe – Stufenplan", () => {
  it("nennt den Stufenplan je Plantag", () => {
    expect(FIELD_HELP.stage.text).toContain("Stufenplan je Plantag");
    expect(FIELD_HELP.stage.text).not.toMatch(/Stufenplan je Leitner-Fach/);
  });
});
