import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, api } from "../lib/api";
import { ExercisePreviewModal } from "./ExercisePreviewModal";

/*
 * B-444: der Testmodus öffnet mit dem Fokus auf „×", der Fehler wird erst danach geladen. Gefälscht wird der Abruf, nicht
 * das `fetch` (Vorbild `TextbookForm.test.tsx`).
 */
describe("Testmodus – ein Fehler beim Laden", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn der Fehler in einem Kasten ohne Live-Region steht: eine nachgeladene Meldung sagt dann kein Screenreader an.
  it("steht in einer Live-Region, die schon vor ihm da war", async () => {
    let fail: (e: unknown) => void = () => {};
    vi.spyOn(api, "previewExercise").mockReturnValue(new Promise((_, reject) => { fail = reject; }));
    render(<ExercisePreviewModal exerciseId={1} title="Unit 1" onClose={() => {}} />);
    const region = screen.getByRole("status");

    fail(new ApiError(409, "Exercise has no content.", "t-1", "exercise_empty"));

    expect(await screen.findByText(/keine Inhalte/)).toBeTruthy();
    expect(region.textContent).toContain("keine Inhalte");
  });

  // Rot, wenn der Satz wieder einen Ausweg nennt („Füge erst … hinzu") oder von Wörtern spricht, die ein Lückentext oder
  // eine Liste nicht hat (Meldungsregel 2026-11-07).
  it("sagt bei einer leeren Übung nur, was sperrt", async () => {
    vi.spyOn(api, "previewExercise").mockRejectedValue(new ApiError(409, "x", "t-2", "exercise_empty"));
    render(<ExercisePreviewModal exerciseId={1} title="Unit 1" onClose={() => {}} />);

    const text = (await screen.findByText(/keine Inhalte/)).textContent ?? "";

    expect(text).not.toMatch(/Füge|Wörter/);
  });
});
