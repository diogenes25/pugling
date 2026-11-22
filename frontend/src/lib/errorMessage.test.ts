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

  it("übersetzt die Löschsperre am Verlag und nennt BEIDE Fälle", () => {
    const text = problem("publisher_in_use");

    expect(text).toContain("anderen Kontos");
    // Der zweite Fall ist der wichtigere: Auf einer geseedeten Datenbank greift die Sperre ausschließlich
    // wegen einer herrenlosen Reihe. Ohne ihn schickt der Satz den Vater eine fremde Reihe suchen, die es
    // nicht gibt.
    expect(text).toContain("herrenlose");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-403/B-414: die Sperre beim Fachwechsel einer Reihe trifft den Creator am Speichern-Knopf des Reihen-Formulars. Der Satz
  // nennt, was sperrt (Themen an Unit/Übung, Art an der Übung; beim Entfernen jede Übung) – und keinen Ausweg über eine zweite
  // Sperre: das Löschen einer Übung (`exercise_in_use`) und der Verweis auf „Verwendung" gehören zu jener Sperre, nicht zu dieser.
  it("übersetzt die Sperre beim Fachwechsel einer Reihe und nennt nur, was sperrt", () => {
    const text = problem("series_subject_in_use");

    expect(text).toContain("Grammatik-Thema");
    // Die Oberfläche nennt Kategorien „Art" (B-403-Review).
    expect(text).toContain("Art dieses Fachs");
    // Beim Entfernen sperrt jede Übung und jedes Unit-Thema (Delta-Review B-403, Review Sprint 21): als Bedingung, damit der
    // Satz auch für eine Reihe mit schlichten Übungen wahr ist. Rot, wenn er den Entfernen-Fall nicht oder als Tatsache nennt.
    expect(text).toContain("ganz entfernen nicht, solange die Reihe Übungen trägt oder eine ihrer Units");
    expect(text).toContain("Wechseln lässt es sich nicht, solange");
    // B-414: kein Ausweg über die Löschsperre, kein Verweis auf deren Fläche.
    expect(text).not.toContain("löschen");
    expect(text).not.toContain("Verwendung");
    // B-405: ein anderes Fach zu wählen ist kein Ausweg aus dieser Sperre – es prüft dieselben Zuordnungen.
    expect(text).not.toContain("anderes Fach");
    expect(text).not.toContain("Some English sentence.");
  });

  it("übersetzt die Löschsperre am Schlagwort und nennt den Ausweg", () => {
    const text = problem("interest_tag_in_use");

    // Der Ausweg ist hier wichtiger als bei den beiden anderen Sperren: Der Vater hat nichts falsch
    // gemacht — der KI-Creator kleidet Übungen ein, ohne dass er es sieht. Ohne den Satz stünde dort die
    // englische Rohzeile an genau der Stelle, an der er eine Anleitung braucht.
    expect(text).toContain("Bearbeiten-Dialog");
    // Und die Grenze des Auswegs: fremde Übungen kann er nicht ausziehen.
    expect(text).toContain("nicht gehören");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-673 und B-659: derselbe Code sperrt Umhängen und Löschen eines gespielten Plans. Rot, wenn der Eintrag fehlt (dann
  // die englische Zeile), wenn er nur eine der beiden Sperren nennt, oder wenn der Ausweg fürs Löschen fehlt.
  it("übersetzt die Sperre beim Umhängen und Löschen eines gespielten Plans", () => {
    const text = problem("plan_has_data");

    expect(text).toContain("keinem anderen Kind");
    expect(text).toContain("nicht löschen (stattdessen „Deaktivieren“)");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-205: die Löschsperre an einer Übung (seit B-413 nur noch dort). Rot, wenn der Eintrag fehlt (dann die englische Zeile).
  // Der Halbsatz zur Betreuung ersetzt, was der Server dynamisch sagt: Verwendungen außerhalb der eigenen Betreuung sperren auch.
  // B-414: kein „Entferne sie dort zuerst" – eine gespielte Position lässt sich nicht herausnehmen (`position_has_data`).
  it("übersetzt die Löschsperre an einer Übung, nennt die unsichtbaren Verwendungen und keinen Ausweg", () => {
    const text = problem("exercise_in_use");

    expect(text).toContain("Lehrplan, einer Klassenarbeit oder als Etappe eines Ziels");
    expect(text).toContain("nicht betreust");
    // Seit B-409 zeigt „Verwendung" alle drei eigenen Sperren – der Verweis ist wahr.
    expect(text).toContain("„Verwendung“");
    expect(text).not.toContain("Entferne");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-413: Unit und Reihe haben einen eigenen Code. Eine Ziel-Etappe auf der Unit ist die Verwendung keiner Übung, darum darf der
  // Satz nicht auf „Verwendung" zeigen. Rot ohne Eintrag, ohne die Unit-Etappe oder mit einem Verweis bzw. Ausweg.
  it("übersetzt die Löschsperre an Unit und Reihe und nennt die Ziel-Etappe auf der Unit", () => {
    const text = problem("unit_content_in_use");

    expect(text).toContain("Lehrplan, einer Klassenarbeit oder als Etappe eines Ziels");
    expect(text).toContain("auf eine Unit selbst");
    expect(text).not.toContain("Verwendung");
    expect(text).not.toContain("Entferne");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-205: die Sperre beim Verschieben und Entfernen von Wörtern – eine andere Regel mit eigenem Code. Rot, wenn der
  // Eintrag fehlt oder den Löschtext wiederholt (dann fehlte der Ausweg „ans Ende anhängen geht“).
  it("übersetzt die Wörter-Sperre einer gespielten Übung und nennt, was erlaubt bleibt", () => {
    const text = problem("exercise_items_locked");

    expect(text).toContain("Lernstand");
    expect(text).toContain("ans Ende anhängen");
    // B-414: aus einem Plan nehmen lässt sich eine gespielte Übung nicht (`position_has_data`) – kein Ausweg dorthin.
    expect(text).not.toContain("aus den Plänen");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-141: das Umbenennen auf ein belegtes Schlagwort-Label ist seither in der Schlagwort-Verwaltung erreichbar. Rot,
  // wenn der Eintrag fehlt (dann die englische Zeile).
  it("übersetzt den belegten Schlagwort-Namen", () => {
    const text = problem("duplicate_interest_tag");

    expect(text).toContain("anderes Schlagwort");
    expect(text).not.toContain("Some English sentence.");
  });

  // B-411: die 18 Sperren, die ein Erwachsener an einem eigenen Knopf auslöst, sprechen Deutsch. Je Code ein Fall mit einem
  // Wort, das nur sein Satz trägt – entfällt ein Eintrag, fällt genau sein Fall. Kein Satz nennt einen Ausweg über eine zweite
  // Sperre (Regel 2026-11-07); „Wähle einen anderen" bei Namensdubletten ist die eigene Sperre.
  it.each([
    ["duplicate_key", "Artikelnummer"],
    ["duplicate_achievement", "Ziel-Metrik"],
    ["duplicate_vocabulary_in_exercise", "steht schon in dieser Übung"],
    ["duplicate_key_result", "demselben Bereich"],
    ["media_variant_exists", "Variante"],
    ["media_already_linked", "schon verknüpft"],
    ["duplicate_email", "E-Mail-Adresse"],
    ["duplicate_profile_name", "auch ein inaktives"],
    ["duplicate_category_name", "diese Art schon"],
    ["duplicate_publisher", "Verlag"],
    ["grammar_topic_in_use", "hängt noch an Übungen oder Units"],
    ["last_owner", "Verwalten"],
    ["vocabulary_in_use", "Grundform"],
    ["position_has_data", "geübt"],
    ["timetable_slot_taken", "Stundenplan"],
    ["purchase_not_open", "storniert"],
    ["activation_not_pending", "beantwortet"],
    ["concurrency_conflict", "überschnitten"],
  ])("übersetzt %s", (code, keyword) => {
    const text = problem(code);

    expect(text).toContain(keyword);
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
