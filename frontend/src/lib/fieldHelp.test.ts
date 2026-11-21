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

// B-607: der Hinweis einer Vokabelzeile erscheint nur auf der Auswahlstufe einer Erkennen-Karte. Rot, wenn der Text eine der
// drei Grenzen verschweigt (Auswahlstufe, Schreiben, Richtung „Deutsch → Fremdwort“) oder das Thema aus FIELD_HELP fällt.
describe("Feldhilfe Hinweis einer Vokabelzeile (B-607)", () => {
  const text = FIELD_HELP.vocabularyHint.text;

  it("trägt den Titel, aus dem der Knopfname entsteht", () => {
    expect(FIELD_HELP.vocabularyHint.title).toBe("Hinweis");
  });
  it("nennt die Auswahlstufe beim Üben als einzigen Ort", () => {
    expect(text).toMatch(/nur beim Üben auf der Auswahlstufe/);
  });
  // Review B-607: der Abschlusstest zeigt keinen Tipp, auch nicht als Auswahl (B-544, PositionTestsController.ToItem).
  it("sagt, dass der Abschlusstest ihn nie zeigt", () => {
    expect(text).toMatch(/im Abschlusstest nie/);
  });
  it("sagt, dass er beim Schreiben nie erscheint", () => {
    expect(text).toMatch(/Beim Schreiben .* erscheint er nie/);
  });
  it("nennt die Richtung „Deutsch → Fremdwort“ mit dem Namen aus dem Formular", () => {
    expect(text).toMatch(/„Deutsch → Fremdwort“ erscheint er auf keiner Stufe/);
  });
});
