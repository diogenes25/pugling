import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ApiError } from "./api";
import { useAction } from "./useAction";

/**
 * Die Zusicherungen des Schreib-Primitivs, durch das **jede** Mutation des Vater-Webs läuft (23 Masken).
 * Sie sind am Bildschirm sämtlich **unsichtbar**: ob zwei Klicks zwei POSTs abschicken, ob `run` nach einem
 * Fehler wieder frei ist, ob der Meldungstext aus `errorMessage` kommt – ein Bruch verändert das Bild nicht.
 * Genau darum steht die Prüfung hier und nicht in einem Playwright-Weg.
 *
 * Getestet wird der Hook **einzeln** (`renderHook`), nicht über einen stellvertretend gerenderten Knopf: der
 * Defekt sitzt im Primitiv, und zwei synchrone `run()`-Aufrufe auf derselben Instanz sind genau der
 * Doppelklick – ohne `api.ts`, Router und Bildschirm hereinzuziehen.
 */

/** Eine Mutation, die erst hält und dann auf Kommando fertig wird – der Zustand „läuft" ist sonst nicht greifbar. */
function tor() {
  let oeffne!: () => void;
  let brich!: (err: unknown) => void;
  const promise = new Promise<void>((res, rej) => { oeffne = res; brich = rej; });
  return { promise, oeffne, brich };
}

describe("useAction", () => {
  it("schickt bei zwei Klicks im selben Tick genau eine Mutation ab", async () => {
    const { result } = renderHook(() => useAction());
    const t = tor();
    let aufrufe = 0;
    const mutation = () => { aufrufe++; return t.promise; };

    let erster!: Promise<boolean>;
    let zweiter!: Promise<boolean>;
    await act(async () => {
      // Beide Aufrufe treffen **dieselbe** `result.current` – ohne Re-Render dazwischen, genau wie der
      // zweite Klick vor dem ersten Render mit `disabled`.
      erster = result.current.run(mutation);
      zweiter = result.current.run(mutation);
      t.oeffne();
      await Promise.all([erster, zweiter]);
    });

    expect(aufrufe).toBe(1);
    expect(await erster).toBe(true);
    // Der verworfene zweite Aufruf meldet **nicht** Erfolg: der Aufrufer hängt sein Aufräumen daran.
    expect(await zweiter).toBe(false);
  });

  it("sperrt auch quer über `run` und `runFor` – es ist eine Sperre, nicht zwei", async () => {
    const { result } = renderHook(() => useAction());
    const t = tor();
    let aufrufe = 0;

    let lauf!: Promise<boolean>;
    let holen!: Promise<number | null>;
    await act(async () => {
      lauf = result.current.run(() => { aufrufe++; return t.promise; });
      holen = result.current.runFor(async () => { aufrufe++; return 7; });
      t.oeffne();
      await Promise.all([lauf, holen]);
    });

    expect(aufrufe).toBe(1);
    expect(await holen).toBeNull();
  });

  it("ist nach einem Fehler wieder frei – ein zweiter Anlauf läuft", async () => {
    const { result } = renderHook(() => useAction());
    let aufrufe = 0;

    await act(async () => {
      await result.current.run(() => { aufrufe++; return Promise.reject(new Error("Erster Anlauf")); });
    });
    await act(async () => {
      await result.current.run(() => { aufrufe++; return Promise.resolve(); });
    });

    // Ohne das `finally` um die Sperre bliebe sie nach dem Fehler stehen und die Maske wäre tot.
    expect(aufrufe).toBe(2);
    expect(result.current.message).toBeNull();
  });

  it("hält `busy`, solange die Mutation läuft", async () => {
    const { result } = renderHook(() => useAction());
    const t = tor();

    let lauf!: Promise<boolean>;
    await act(async () => { lauf = result.current.run(() => t.promise); });
    expect(result.current.busy).toBe(true);

    await act(async () => { t.oeffne(); await lauf; });
    expect(result.current.busy).toBe(false);
  });

  it("meldet den Fehler über `errorMessage` – mit Trace-Referenz, nicht als rohes Objekt", async () => {
    const { result } = renderHook(() => useAction());

    await act(async () => {
      await result.current.run(() => Promise.reject(new ApiError(409, "Schon vergeben", "t-42")));
    });

    // Ein `String(err)` ergäbe „ApiError: Schon vergeben" – die Trace-Referenz zeigt, dass der Weg über
    // `errorMessage` geht (dort hängt auch die deutsche Fassung fachlicher Codes).
    expect(result.current.message).toEqual({ ok: false, text: "Schon vergeben (Ref: t-42)" });
  });

  it("lässt das Banner ohne `okText` leer und füllt es mit", async () => {
    const { result } = renderHook(() => useAction());

    await act(async () => { await result.current.run(() => Promise.resolve()); });
    // Für Panels, die sich nach dem Speichern schlicht neu laden: der Erfolg ist dort die neue Liste.
    expect(result.current.message).toBeNull();

    await act(async () => { await result.current.run(() => Promise.resolve(), "Gespeichert."); });
    expect(result.current.message).toEqual({ ok: true, text: "Gespeichert." });
  });

  it("gibt mit `runFor` das Ergebnis heraus und bei Fehler `null`", async () => {
    const { result } = renderHook(() => useAction());

    let gutfall: { id: number } | null = null;
    await act(async () => { gutfall = await result.current.runFor(async () => ({ id: 12 })); });
    expect(gutfall).toEqual({ id: 12 });

    let schlechtfall: unknown = "nicht gesetzt";
    await act(async () => { schlechtfall = await result.current.runFor(() => Promise.reject(new Error("Nein"))); });
    expect(schlechtfall).toBeNull();
    expect(result.current.message).toEqual({ ok: false, text: "Nein" });
  });

  it("setzt und räumt Meldungen ohne Server-Aufruf (`fail`/`succeed`/`clear`)", async () => {
    const { result } = renderHook(() => useAction());

    // Der Weg der Eingabeprüfung: melden, ohne zu senden.
    act(() => result.current.fail("Titel nötig."));
    expect(result.current.message).toEqual({ ok: false, text: "Titel nötig." });

    act(() => result.current.succeed("Nichts zu speichern."));
    expect(result.current.message).toEqual({ ok: true, text: "Nichts zu speichern." });

    act(() => result.current.clear());
    expect(result.current.message).toBeNull();
  });
});
