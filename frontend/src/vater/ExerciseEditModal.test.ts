import { describe, expect, it } from "vitest";

import { initialForm, payloadFrom } from "./ExerciseEditModal";
import type { ExerciseDetail } from "../lib/types";

/*
 * B-46. Der Editor speichert per **Vollersatz**-PUT: was die Nutzlast nicht nennt, löscht der Server. Für
 * die Felder, die das Formular anzeigt, ist das harmlos — für die, die es NICHT anzeigt, ist es ein
 * stiller Datenverlust, und genau dort hing die Zusage bis hierher an einem Kommentar.
 *
 * Dreimal derselbe Fall inzwischen: `suggestedBonus` (Bonus-Vorschlag des Autors), `executePublic`
 * (Sichtbarkeit, deren Server-Default `true` ist) und seit B-46 `interestTagIds` (die Einkleidung, die der
 * KI-Creator einträgt). Beim dritten Mal bekommt die Regel eine Zusicherung statt eines weiteren
 * Kommentars — sonst kostet das vierte Feld denselben Fehler noch einmal.
 *
 * Geprüft wird `payloadFrom` und nicht die Oberfläche: die Abbildung „geladener Stand + Formular →
 * Nutzlast" ist die ganze Regel, und sie ist eine reine Funktion. Ein Klick-Test durch das Modal würde
 * dieselbe Zeile prüfen und dazu ein Netz mitbringen.
 */

/**
 * Ein vollständiger geladener Stand — **annotiert statt gecastet**, und das ist hier tragend: Ein
 * `as ExerciseDetail` würde die Übersetzung genau dann noch durchlassen, wenn `ExerciseDetail` ein
 * weiteres Feld bekommt. Das ist der Moment, den diese Datei laut ihrem Kopf abfangen soll — ohne Cast
 * fällt sie dann beim Übersetzen rot und erzwingt die Frage „muss das mit durch?".
 */
const BASE: ExerciseDetail = {
  id: 7, seriesId: 2, seriesUnitId: 3, seriesUnitLabel: "Unit 1", subjectId: 1, subjectName: "Englisch",
  type: "Vocabulary", title: "Tiere", orderIndex: 4, rewardPoints: 12,
  gradeMin: 5, gradeMax: 6, schoolTypes: "Gymnasium", source: "Green Line 1", categoryId: 9,
  categoryName: "Vokabeln", suggestedBonus: null, defaultStage: null, defaultItemCount: null,
  authorAdultId: 1, authorName: "Papa", isOwn: true, isOwner: true, executePublic: true, grantCount: 1,
  config: {}, description: null, defaultUseLeitner: false, defaultRequireTypedTest: false,
  interestTagIds: [],
};

function detailWith(over: Partial<ExerciseDetail> = {}): ExerciseDetail {
  return { ...BASE, ...over };
}

describe("payloadFrom – der Vollersatz-PUT darf nichts verlieren, was das Formular nicht zeigt", () => {
  /*
   * Eine Zusicherung über die **ganze** Nutzlast statt drei über einzelne Felder. Der Unterschied ist
   * nicht Kürze: `toEqual` bewacht die Kongruenz in beide Richtungen und fällt damit auch bei einem
   * versehentlich **zusätzlich** mitgeschickten Feld — und ein unbekanntes Feld lehnt der Server mit
   * `unknown_field` ab (die Fläche, die `http(…, body?: unknown)` strukturell nicht sieht).
   *
   * `executePublic: false` ist bewusst der NICHT-Vorgabewert: bei `true` wäre die Zusicherung auch für
   * eine Fassung erfüllt, die das Feld wegläßt und den Server-Default greifen lässt.
   */
  it("baut genau die Nutzlast, die der Server erwartet – Feld für Feld", () => {
    const bonus = {
      comboThreshold: 3, comboBonusPoints: 5, speedThresholdSeconds: 4,
      speedBonusPoints: 2, newContentPoints: 1,
    };
    const detail = detailWith({ interestTagIds: [3, 7], executePublic: false, suggestedBonus: bonus });

    expect(payloadFrom(detail, initialForm(detail), detail.config)).toEqual({
      title: "Tiere",
      description: null,
      orderIndex: 4,
      rewardPoints: 12,
      config: {},
      gradeMin: 5,
      gradeMax: 6,
      schoolTypes: "Gymnasium",
      source: "Green Line 1",
      categoryId: 9,
      defaultUseLeitner: false,
      defaultRequireTypedTest: false,
      defaultStage: null,
      defaultItemCount: null,
      suggestedBonus: bonus,
      executePublic: false,
      interestTagIds: [3, 7],
    });
  });

  it("schickt eine leere Einkleidung als leere Liste, nicht als fehlendes Feld", () => {
    /*
     * Kein Duplikat des Falls darüber, auch wenn beide beim Entfernen der Zeile fallen: Diese Zusicherung
     * hat einen **eigenen** Defekt, der nur sie rot macht — `detail.interestTagIds.length ? … : undefined`.
     * Am Server ist das heute folgenlos (`SetInterestLinks` behandelt `?? []` gleich), sie bewacht also die
     * **Form**, nicht das Ergebnis. Das steht hier ausdrücklich, damit niemand sie später für eine
     * Wirkungs-Zusicherung hält.
     */
    const payload = payloadFrom(detailWith(), initialForm(detailWith()), {});

    expect(payload.interestTagIds).toEqual([]);
  });
});
