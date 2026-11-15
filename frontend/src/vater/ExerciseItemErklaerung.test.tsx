import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseDetail, ExerciseSummary, ExerciseTypeManifest, VocabItemResponse } from "../lib/types";
import { ExerciseEditModal } from "./ExerciseEditModal";

/*
 * B-583 AK 6: die Erklärung je Wort im Bearbeiten-Dialog – gespeicherter Wert beim Öffnen sichtbar, Änderung per PATCH.
 */
const DETAIL = {
  id: 7, seriesId: 2, seriesUnitId: 3, seriesUnitLabel: "Unit 1", subjectId: 1, subjectName: "Französisch",
  type: "Vocabulary", title: "Am Bahnhof", orderIndex: 1, rewardPoints: 10,
  gradeMin: null, gradeMax: null, schoolTypes: "None", source: null, categoryId: null,
  categoryName: null, suggestedBonus: null, defaultStage: null, defaultItemCount: null,
  authorAdultId: 1, authorName: "Papa", isOwn: true, isOwner: true, executePublic: true, grantCount: 0,
  config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de" }, description: null,
  defaultUseLeitner: false, defaultRequireTypedTest: false, interestTagIds: [], averageRating: null, ratingCount: 0,
  grammarTopicIds: [],
} as unknown as ExerciseDetail;

const ITEM = {
  id: 41, orderIndex: 0, vocabularyId: 9, front: "la gare", back: "der Bahnhof", translationAlternatives: null,
  hint: null, explanation: "la gare ist weiblich.", _self: "", vocabulary: "",
} as unknown as VocabItemResponse;

let patch: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.spyOn(api, "exerciseTypes").mockResolvedValue(
    [{ type: "Vocabulary", label: "Vokabeln", authoringRoute: "vocabulary" } as unknown as ExerciseTypeManifest]);
  vi.spyOn(api, "interestTags").mockResolvedValue([]);
  vi.spyOn(api, "grammarTopics").mockResolvedValue([]);
  vi.spyOn(api, "categories").mockResolvedValue([]);
  vi.spyOn(api, "exerciseGrants").mockResolvedValue([]);
  vi.spyOn(api, "exerciseMedia").mockResolvedValue([]);
  vi.spyOn(api, "getExercise").mockResolvedValue(DETAIL);
  vi.spyOn(api, "exerciseItems").mockResolvedValue([ITEM]);
  patch = vi.fn().mockResolvedValue(ITEM);
  vi.spyOn(api, "patchExerciseItem").mockImplementation(patch as never);
});
afterEach(() => vi.restoreAllMocks());

describe("Bearbeiten-Dialog – Erklärung je Wort (B-583)", () => {
  // Rot, wenn das Feld beim Öffnen leer ist (der gespeicherte Wert käme nicht zurück) oder die Änderung nicht als
  // `explanation` gepatcht wird.
  it("zeigt die gespeicherte Erklärung und speichert eine Änderung", async () => {
    render(<AuthProvider><ExerciseEditModal exercise={{ id: 7, type: "Vocabulary", title: "Am Bahnhof" } as unknown as ExerciseSummary}
      onClose={() => {}} onSaved={() => {}} /></AuthProvider>);
    const feld = await screen.findByLabelText("Erklärung für la gare") as HTMLInputElement;
    expect(feld.value).toBe("la gare ist weiblich.");
    expect(screen.queryByRole("button", { name: "Erklärung für la gare speichern" })).toBeNull();

    fireEvent.change(feld, { target: { value: "Bahnhof: der, gare: la." } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Erklärung für la gare speichern" })); });
    expect(patch).toHaveBeenCalledWith(2, 3, 7, 41, { explanation: "Bahnhof: der, gare: la." });
  });
});
