import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { SubjectResponse, TextbookSeriesResponse } from "../lib/types";
import { ExerciseFilterBar, type ExerciseFilter } from "./ExerciseFilterBar";

/*
 * B-513 (Review): die Reihe im Planbau filtert selbst. Vorher lebte sie nur lokal als Zwischenstufe zur Unit, und eine
 * Reihe ohne Unit zeigte die Übungen aller Reihen. Gefälscht wird der Abruf, nicht das `fetch`.
 */
const ENGLISH = { id: 1, name: "Englisch" } as SubjectResponse;

describe("ExerciseFilterBar – Reihe", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn die gewählte Reihe nicht in den Filter geht (die Suche bliebe auf dem ganzen Fach).
  it("gibt die gewählte Reihe als Filter weiter und leert die Unit", async () => {
    vi.spyOn(api, "textbookSeries").mockResolvedValue([{ id: 7, name: "Access" } as TextbookSeriesResponse]);
    vi.spyOn(api, "seriesUnits").mockResolvedValue([]);
    vi.spyOn(api, "categories").mockResolvedValue([]);
    vi.spyOn(api, "grammarTopics").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    const changes: ExerciseFilter[] = [];
    render(<AuthProvider><ExerciseFilterBar value={{ subjectId: 1, seriesUnitId: 3 }} onChange={(f) => changes.push(f)}
      subjects={[ENGLISH]} /></AuthProvider>);

    fireEvent.change(await screen.findByRole("combobox", { name: "Reihe-Filter" }), { target: { value: "7" } });

    expect(changes[changes.length - 1]).toMatchObject({ subjectId: 1, seriesId: 7, seriesUnitId: undefined });
  });

  // Rot, wenn ein Fachwechsel die Reihe stehen lässt – sie gehört zum alten Fach und lieferte eine leere Liste.
  it("setzt die Reihe beim Fachwechsel zurück", async () => {
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    vi.spyOn(api, "categories").mockResolvedValue([]);
    vi.spyOn(api, "grammarTopics").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    vi.spyOn(api, "seriesUnits").mockResolvedValue([]);
    const changes: ExerciseFilter[] = [];
    render(<AuthProvider><ExerciseFilterBar value={{ subjectId: 1, seriesId: 7 }} onChange={(f) => changes.push(f)}
      subjects={[ENGLISH, { id: 2, name: "Latein" } as SubjectResponse]} /></AuthProvider>);

    fireEvent.change(screen.getByRole("combobox", { name: "Fach-Filter" }), { target: { value: "2" } });

    expect(changes[changes.length - 1]).toMatchObject({ subjectId: 2, seriesId: undefined });
  });
});
