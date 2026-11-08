import { describe, expect, it } from "vitest";
import { initialForm, vocabSettingsConfig } from "./ExerciseEditModal";
import type { ExerciseDetail } from "../lib/types";

/**
 * B-421, AK 7, Bearbeiten: der Beschreibungs-Block des Dialogs liest den Haken aus der geladenen Config und schreibt ihn
 * beim Speichern zurück – über den Server-Stand, ohne dessen übrige Einstellungen zu verlieren. Den Weg durch den echten
 * Dialog (setzen, speichern, neu laden) fährt `e2e/artikel-pflicht.spec.ts`.
 */
const detail = (config: Record<string, unknown>) => ({
  title: "En ville", description: null, rewardPoints: 10, gradeMin: null, gradeMax: null, source: null, schoolTypes: null,
  defaultUseLeitner: true, defaultRequireTypedTest: false, defaultStage: null, defaultItemCount: null,
  interestTagIds: [], grammarTopicIds: [], config,
}) as unknown as ExerciseDetail;

describe("Artikel-Pflicht im Bearbeiten-Dialog", () => {
  // Rot, wenn die Vorbelegung das Feld nicht liest – oder ein fehlendes Feld als gesetzt.
  it("belegt den Haken aus der Config vor", () => {
    expect(initialForm(detail({ direction: "front-to-back", articleRequired: true })).articleRequired).toBe(true);
    expect(initialForm(detail({ direction: "front-to-back" })).articleRequired).toBe(false);
  });

  // Rot, wenn das Speichern den Haken nicht schreibt oder die übrige Config (Richtung) verliert.
  it("schreibt den Haken über den Server-Stand", () => {
    const config = vocabSettingsConfig({ direction: "back-to-front", articleRequired: true },
      { sourceLang: "fr", targetLang: "de", articleRequired: false });
    expect(config).toMatchObject({ direction: "back-to-front", sourceLang: "fr", targetLang: "de", articleRequired: false });
  });
});
