import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

/*
 * B-215. Der Hook schluckte den Fehlschlag des Manifest-Abrufs (`.catch(() => setAll([]))`) – danach war
 * „Server kennt null Typen" von „Abruf gescheitert" nicht zu unterscheiden, und kein Konsument konnte den
 * Ausfall anzeigen oder wiederholen.
 *
 * Gemockt wird hier das API-Modul, nicht `fetch`: Es ist ein Hook-Test (`renderHook`), kein nachgebauter
 * Bildschirm (`frontend/CLAUDE.md`). Der Modul-Cache des Manifests ist je Testfall frisch
 * (`vi.resetModules`), sonst trüge der zweite Fall das Ergebnis des ersten.
 */
const exerciseTypes = vi.fn();
// Nur den einen Aufruf ersetzen; `errorMessage` & Co. bleiben echt – der Hook formt den Fehlertext damit.
vi.mock("./api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("./api")>()),
  api: { exerciseTypes: () => exerciseTypes() },
}));

const MANIFEST = [{ type: "Vocabulary", label: "Vokabelkarten", authoringRoute: "vocabulary" }];

async function frischerHook() {
  vi.resetModules();
  const mod = await import("./exerciseTypes");
  return mod.useExerciseTypes;
}

describe("useExerciseTypes", () => {
  beforeEach(() => { exerciseTypes.mockReset(); });

  it("liefert nach einem gelungenen Abruf Namen und Routen", async () => {
    exerciseTypes.mockResolvedValueOnce(MANIFEST);
    const useExerciseTypes = await frischerHook();
    const { result } = renderHook(() => useExerciseTypes());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBeNull();
    expect(result.current.label("Vocabulary")).toBe("Vokabelkarten");
    expect(result.current.route("Vocabulary")).toBe("vocabulary");
  });

  it("nennt den Fehlschlag, statt ihn als leeres Manifest auszugeben – und bleibt lesbar", async () => {
    exerciseTypes.mockRejectedValueOnce(new Error("Netz weg"));
    const useExerciseTypes = await frischerHook();
    const { result } = renderHook(() => useExerciseTypes());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBe("Netz weg");
    // „Lesbar statt kaputt" bleibt: Schlüssel als Name, keine Route.
    expect(result.current.all).toEqual([]);
    expect(result.current.label("Essay")).toBe("Essay");
    expect(result.current.route("Essay")).toBeNull();
  });

  it("versucht es auf `reload()` wirklich erneut – und hält bis dahin den Ladezustand", async () => {
    exerciseTypes.mockRejectedValueOnce(new Error("Netz weg")).mockResolvedValueOnce(MANIFEST);
    const useExerciseTypes = await frischerHook();
    const { result } = renderHook(() => useExerciseTypes());
    await waitFor(() => expect(result.current.error).toBe("Netz weg"));

    act(() => result.current.reload());

    await waitFor(() => expect(result.current.error).toBeNull());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.label("Vocabulary")).toBe("Vokabelkarten");
    expect(exerciseTypes).toHaveBeenCalledTimes(2);
  });
});
