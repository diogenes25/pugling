import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ExerciseTypeManifest } from "./types";
import { umfangText } from "./exerciseTypes";

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

    // „hält bis dahin den Ladezustand" – ohne diese Zeile bliebe ein Hook grün, der bei `reload` `loading`
    // nie setzt (Nachschau 2026-10-17). `error` ist im selben Schritt schon `null`.
    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.error).toBeNull());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.label("Vocabulary")).toBe("Vokabelkarten");
    expect(exerciseTypes).toHaveBeenCalledTimes(2);
  });
});

/** B-424: Zahl und Wort kommen vom Server; hier wird nur zusammengesetzt. */
describe("umfangText", () => {
  const karten = { itemNoun: "Karten", itemNounOne: "Karte" } as ExerciseTypeManifest;
  // Rot, wenn die Mehrzahl fehlt oder die Einzahl auch für mehrere gilt.
  it("nennt Zahl und Wort in der Mehrzahl", () => { expect(umfangText(12, karten)).toBe("12 Karten"); });
  // Rot, wenn „1 Karten" entsteht.
  it("nimmt für genau eins die Einzahl", () => { expect(umfangText(1, karten)).toBe("1 Karte"); });
  // AK 4. Rot, wenn eine leere Übung „0 Karten" sagt statt „leer".
  it("sagt bei null Atomen „leer\"", () => { expect(umfangText(0, karten)).toBe("leer"); });
  // Rot, wenn ein Typ ohne zählbare Atome eine Zahl bekommt.
  it("schweigt ohne Zahl", () => { expect(umfangText(null, karten)).toBeNull(); expect(umfangText(undefined, karten)).toBeNull(); });
  // Rot, wenn ein noch ladendes Manifest „12 undefined" erzeugt.
  it("zeigt ohne Manifest nur die Zahl", () => { expect(umfangText(12, undefined)).toBe("12"); });
});
