import { describe, expect, it } from "vitest";
import { canRate, ratingSummary, starString } from "./ExerciseRatingPanel";

/*
 * Die Zeilenanzeige der Bewertung (B-12). Durchschnitt und Anzahl sind in jedem Fall verschieden gewählt,
 * damit eine Vertauschung der beiden auffällt.
 */
describe("ratingSummary", () => {
  it("nennt eine unbewertete Übung so, statt „★ 0,0“ zu zeigen", () => {
    expect(ratingSummary(null, 0)).toBe("noch unbewertet");
  });

  it("zeigt den Durchschnitt deutsch mit einer Nachkommastelle und die Anzahl im Plural", () => {
    expect(ratingSummary(3.5, 2)).toBe("★ 3,5 (2 Bewertungen)");
  });

  it("schreibt die eine Bewertung im Singular und auch einen ganzzahligen Schnitt mit Komma", () => {
    expect(ratingSummary(4, 1)).toBe("★ 4,0 (1 Bewertung)");
  });

  it("rundet auf eine Stelle (2 von 3 Bewertern mit 4, einer mit 5 → 4,3)", () => {
    expect(ratingSummary(13 / 3, 3)).toBe("★ 4,3 (3 Bewertungen)");
  });
});

describe("starString", () => {
  it("füllt bis zur Sternzahl und lässt den Rest leer", () => {
    expect(starString(2)).toBe("★★☆☆☆");
  });
});

describe("canRate", () => {
  // Die Abgrenzung aus AK 7 („konsistent mit AK 2"): Der erste Fall belegt `isOwn` und `isOwner` verschieden,
  // damit eine Verwechslung der beiden Felder auffällt – ein Schreib-Grantee darf bearbeiten UND bewerten.
  // Über `row` statt als Objekt-Literal: ein Literal mit `isOwn` lehnte der Parametertyp als Überschuss ab.
  const row = (r: { isOwner: boolean; isOwn: boolean }) => r;
  it("lässt den Schreib-Grantee bewerten (isOwn, aber nicht isOwner)", () => {
    expect(canRate(row({ isOwner: false, isOwn: true }))).toBe(true);
  });

  it("sperrt den Verwaltenden", () => {
    expect(canRate(row({ isOwner: true, isOwn: true }))).toBe(false);
  });

  it("lässt den fremden Leser bewerten", () => {
    expect(canRate(row({ isOwner: false, isOwn: false }))).toBe(true);
  });
});
