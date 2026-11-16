import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseDetail, ExerciseSummary, ExerciseTypeManifest } from "../lib/types";
import { ExerciseEditModal } from "./ExerciseEditModal";

/*
 * B-599, Entscheidung 5 – der Bearbeiten-Dialog eines Lückentexts bietet die Standard-Abfrageform aus dem Manifest an und
 * schickt die Wahl im Vollersatz mit. Eigene Datei, weil das Manifest je Modul zwischengespeichert wird (exerciseTypes.ts):
 * im Nachbartest stünde dort Birkenbihl. Gefälscht wird der Abruf, nicht das `fetch`.
 */
const DETAIL = {
  id: 9, seriesId: 2, seriesUnitId: 3, seriesUnitLabel: "Unit 1", subjectId: 1, subjectName: "Englisch",
  type: "Cloze", title: "Question tags", orderIndex: 1, rewardPoints: 10,
  gradeMin: null, gradeMax: null, schoolTypes: "None", source: null, categoryId: null,
  categoryName: null, suggestedBonus: null, defaultStage: null, defaultItemCount: null,
  authorAdultId: 1, authorName: "Papa", isOwn: true, isOwner: true, executePublic: true, grantCount: 0,
  config: { text: "She works, {{1}}?", gaps: [{ index: 1, answer: "doesn't she" }] }, description: null,
  defaultUseLeitner: false, defaultRequireTypedTest: false,
  interestTagIds: [], averageRating: null, ratingCount: 0, grammarTopicIds: [],
} as unknown as ExerciseDetail;

const CLOZE = {
  type: "Cloze", label: "Lückentext", authoringRoute: "cloze",
  stages: [{ value: 2, label: "Wortbank" }, { value: 8, label: "Übersetzung + Auswahl je Lücke" }],
} as unknown as ExerciseTypeManifest;

let update: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.spyOn(api, "exerciseTypes").mockResolvedValue([CLOZE]);
  vi.spyOn(api, "getExercise").mockResolvedValue(DETAIL);
  vi.spyOn(api, "interestTags").mockResolvedValue([]);
  vi.spyOn(api, "grammarTopics").mockResolvedValue([]);
  vi.spyOn(api, "categories").mockResolvedValue([]);
  vi.spyOn(api, "exerciseGrants").mockResolvedValue([]);
  vi.spyOn(api, "exerciseMedia").mockResolvedValue([]);
  update = vi.fn().mockResolvedValue(DETAIL);
  vi.spyOn(api, "updateExercise").mockImplementation(update as never);
});
afterEach(() => vi.restoreAllMocks());

describe("ExerciseEditModal – Standard-Abfrageform beim Lückentext", () => {
  // Rot, wenn der Dialog das Feld nur Vokabeln zeigte (der Stand vor B-599), die Stufen nicht aus dem Manifest nähme
  // oder die Wahl nicht im Vollersatz mitschickte.
  it("bietet die Stufen des Manifests an und schickt die gewählte mit", async () => {
    render(<AuthProvider><ExerciseEditModal exercise={{ id: 9, type: "Cloze", title: "Question tags" } as unknown as ExerciseSummary}
      onClose={() => {}} onSaved={() => {}} /></AuthProvider>);
    const feld = await screen.findByLabelText("Standard-Abfrageform", { exact: true }) as HTMLSelectElement;
    await act(async () => {});
    expect([...feld.options].map((o) => o.text)).toEqual(
      ["Standard (Vorgabe des Typs)", "Wortbank", "Übersetzung + Auswahl je Lücke"]);

    fireEvent.change(feld, { target: { value: "8" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Übung speichern" })); });

    expect(update).toHaveBeenCalledTimes(1);
    expect((update.mock.calls[0][4] as { defaultStage: number | null }).defaultStage).toBe(8);
  });
});
