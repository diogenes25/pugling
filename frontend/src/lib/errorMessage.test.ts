import { describe, expect, it } from "vitest";
import { ApiError, errorMessage } from "./api";

/*
 * Die deutschen Texte zu fachlichen Fehler-Codes hatten bisher keinen Test — auffallen würde ein
 * fehlender Eintrag erst dem Nutzer, als englische Rohzeile mitten in der Oberfläche.
 *
 * Angelegt im Nachtlauf-Sprint 3 (B-144/B-127), weil genau das dort passiert wäre: Die beiden neuen
 * Löschsperren erfüllen die Regel über der Tabelle (der Nutzer sieht sie an einem Knopf, den er selbst
 * gedrückt hat, und kann etwas dagegen tun), waren aber nicht eingetragen. Gefunden hat es der
 * Rollengang — nicht der Build, nicht die Tests, nicht die beiden Reviewer.
 */

const problem = (code: string, detail = "Some English sentence.") =>
  errorMessage(new ApiError(409, detail, "trace-123", code));

describe("errorMessage – deutsche Fassung fachlicher Codes", () => {
  it("übersetzt die Löschsperre am Fach und nennt, was NICHT sperrt", () => {
    const text = problem("subject_in_use");

    expect(text).toContain("Zielen oder im Stundenplan");
    // Der zweite Halbsatz ist die eigentliche Arbeit des Textes: Ohne ihn sucht der Vater den Fehler bei
    // seinen Lehrwerk-Reihen, die gar nichts damit zu tun haben.
    expect(text).toContain("Lehrwerk-Reihen");
    expect(text).not.toContain("Some English sentence.");
  });

  it("übersetzt die Löschsperre am Verlag und benennt das fremde Konto", () => {
    const text = problem("publisher_in_use");

    expect(text).toContain("anderen Kontos");
    expect(text).not.toContain("Some English sentence.");
  });

  it("hängt an einer übersetzten Meldung KEINE Trace-Id an", () => {
    // Die Referenz hilft beim Melden eines Defekts, nicht beim Beheben einer Löschsperre – sie macht
    // einen sonst verständlichen Satz nur wieder technisch.
    expect(problem("subject_in_use")).not.toContain("trace-123");
  });

  it("lässt einen technischen Code als Rohmeldung durch, mit Referenz", () => {
    // Die Gegenprobe: Die Tabelle soll nicht wachsen, bis sie jeden Code doppelt. Was den Nutzer nicht
    // handeln lässt, behält seine Rohform – und dann ist die Trace-Id das Nützlichste daran.
    const text = errorMessage(new ApiError(500, "Unexpected failure.", "trace-999", "internal_error"));

    expect(text).toContain("Unexpected failure.");
    expect(text).toContain("trace-999");
  });
});
