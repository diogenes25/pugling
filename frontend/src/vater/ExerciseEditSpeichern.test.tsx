import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseDetail, ExerciseSummary, ExerciseTypeManifest } from "../lib/types";
import { ExerciseEditModal } from "./ExerciseEditModal";

/*
 * B-534 (Review): der Dialog hat **einen** Speichern-Knopf, und Enter in einem Feld der Beschreibung ist derselbe
 * Weg. Gefälscht wird der Abruf, nicht das `fetch`. Birkenbihl, weil sein Inhalt hier nicht bearbeitet wird –
 * so bleibt nur das Formular der Metadaten, um das es geht.
 */
const DETAIL = {
  id: 7, seriesId: 2, seriesUnitId: 3, seriesUnitLabel: "Unit 1", subjectId: 1, subjectName: "Englisch",
  type: "Birkenbihl", title: "Tiere", orderIndex: 4, rewardPoints: 12,
  gradeMin: null, gradeMax: null, schoolTypes: "None", source: null, categoryId: null,
  categoryName: null, suggestedBonus: null, defaultStage: null, defaultItemCount: null,
  authorAdultId: 1, authorName: "Papa", isOwn: false, isOwner: false, executePublic: true, grantCount: 0,
  config: {}, description: null, defaultUseLeitner: false, defaultRequireTypedTest: false,
  interestTagIds: [], averageRating: null, ratingCount: 0, grammarTopicIds: [],
} as unknown as ExerciseDetail;

const SUMMARY = { id: 7, type: "Birkenbihl", title: "Tiere" } as unknown as ExerciseSummary;

let update: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.spyOn(api, "exerciseTypes").mockResolvedValue(
    [{ type: "Birkenbihl", label: "Birkenbihl", authoringRoute: "birkenbihl" } as unknown as ExerciseTypeManifest]);
  vi.spyOn(api, "interestTags").mockResolvedValue([]);
  vi.spyOn(api, "grammarTopics").mockResolvedValue([]);
  // Der Abschnitt „Aufbau-Weg" (B-700) liest die Übungen der Unit.
  vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [], total: 0 });
  // B-407: der Dialog lädt die Arten des Fachs. Ungemockt ginge der Abruf ins Netz und brächte einen zweiten
  // „Erneut laden“-Knopf – der Fall unten fände dann nicht mehr den des Nachladens.
  vi.spyOn(api, "categories").mockResolvedValue([]);
  vi.spyOn(api, "exerciseGrants").mockResolvedValue([]);
  vi.spyOn(api, "exerciseMedia").mockResolvedValue([]);
  update = vi.fn().mockResolvedValue(DETAIL);
  vi.spyOn(api, "updateExercise").mockImplementation(update as never);
});
afterEach(() => vi.restoreAllMocks());

async function oeffne() {
  render(<AuthProvider><ExerciseEditModal exercise={SUMMARY} onClose={() => {}} onSaved={() => {}} /></AuthProvider>);
  const titel = await screen.findByLabelText("Titel", { exact: true });
  // Das Manifest kommt nach dem Detail; ohne Route sperrt `save` still.
  await act(async () => {});
  return titel as HTMLInputElement;
}

describe("ExerciseEditModal – Enter speichert", () => {
  // Rot, solange der Knopf `type="button"` ohne `form=` ist: dann hat das Formular keinen Absende-Knopf, und Enter
  // in einem Feld tut im Browser nichts (implizites Absenden braucht ihn, sobald es mehr als ein Feld gibt).
  it("der eine Speichern-Knopf ist der Absende-Knopf des Beschreibungs-Formulars", async () => {
    vi.spyOn(api, "getExercise").mockResolvedValue(DETAIL);
    const titel = await oeffne();
    const knopf = screen.getByRole("button", { name: "Übung speichern" }) as HTMLButtonElement;
    expect(knopf.type).toBe("submit");
    expect(knopf.getAttribute("form")).toBe(titel.form?.id);
    expect(titel.form?.id).toBeTruthy();
  });

  // Absichtserklärung zum Zählen: Enter (Absenden des Formulars) schickt genau einen Vollersatz. Rot würde er erst,
  // wenn der Knopf neben `type="submit"` zusätzlich ein `onClick={save}` trüge UND die Sperre fiele.
  it("Enter im Titel schickt genau einen PUT", async () => {
    vi.spyOn(api, "getExercise").mockResolvedValue(DETAIL);
    const titel = await oeffne();
    fireEvent.change(titel, { target: { value: "Tiere neu" } });
    await act(async () => { fireEvent.submit(titel.form!); });
    expect(update).toHaveBeenCalledTimes(1);
    expect((update.mock.calls[0][4] as { title: string }).title).toBe("Tiere neu");
  });

  // Rot ohne `if (veraltet) return;` in `save`: nach gescheitertem Nachladen ist nur der Knopf gesperrt, Enter
  // schickte den Vollersatz aus dem veralteten `detail.config` (B-210).
  it("Enter nach gescheitertem Nachladen schickt nichts", async () => {
    vi.spyOn(api, "getExercise")
      .mockResolvedValueOnce(DETAIL)
      .mockRejectedValue(new Error("Netz weg"));
    const titel = await oeffne();
    await act(async () => { fireEvent.submit(titel.form!); });
    expect(update).toHaveBeenCalledTimes(1);
    expect(await screen.findByRole("button", { name: "Erneut laden" })).toBeTruthy();

    await act(async () => { fireEvent.submit(titel.form!); });
    expect(update).toHaveBeenCalledTimes(1);
    expect((screen.getByRole("button", { name: "Übung speichern" }) as HTMLButtonElement).disabled).toBe(true);
  });
});
