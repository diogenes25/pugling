import { StrictMode } from "react";
import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useOncePerKey } from "./useOncePerKey";

/** A promise plus its resolver, so a test can control exactly when the async work "arrives". */
function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => { resolve = res; });
  return { promise, resolve };
}

/**
 * Der Effekt-Doppellauf (B-62): React kann denselben Start-Effekt zweimal für dasselbe Mount aufrufen
 * (StrictMode im Dev, generell bei einem Remount) - ohne Sperre legte `SohnPractice.tsx` darum zwei
 * Sitzungen an. Eine reine Boolean-Sperre reicht dabei NICHT: kombiniert mit einem `alive`-Flag, das die
 * Effekt-Aufräumfunktion auf `false` setzt, blieb der Bildschirm bei „wird geladen…" hängen - genau dieser
 * Fehler zeigte sich beim eigenen E2E-Beleg dieser Reparatur.
 * <p>
 * Der erste Testfall braucht dafür einen echten `React.StrictMode`-Wrapper, nicht bloß ein `rerender()`
 * mit demselben Schlüssel: React vergleicht das Dependency-Array per `Object.is`, zwei gleiche
 * String-Primitive lösen ganz ohne StrictMode KEINEN zweiten Effekt-Lauf aus - ein `rerender()` allein
 * hätte die naive, fehlerhafte Erstversuch-Implementierung (reine Bool-Ref + `alive`-Flag) nicht als rot
 * entlarvt, obwohl sie unter echtem StrictMode nachweislich hängen bleibt (frontend-reviewer-Fund).
 * </p>
 */
describe("useOncePerKey", () => {
  it("startet die Arbeit nur einmal je Schlüssel, aber liefert das Ergebnis auch beim zweiten (überlebenden) Lauf", () => {
    const work = deferred<string>();
    const start = vi.fn(() => work.promise);
    const onDone = vi.fn();
    const onError = vi.fn();

    // StrictMode simuliert Mount→Cleanup→Mount für denselben Schlüssel bereits beim ERSTEN Render - kein
    // `rerender()` nötig, um den Doppellauf auszulösen.
    const { unmount } = renderHook(
      ({ key }) => useOncePerKey(key, start, onDone, onError),
      { initialProps: { key: "1:2" }, wrapper: StrictMode },
    );

    expect(start).toHaveBeenCalledTimes(1);

    work.resolve("fertig");
    return work.promise.then(() => {
      // Der ZWEITE (überlebende) Lauf hat sein eigenes onDone bekommen - nicht blockiert vom ersten Cleanup.
      expect(onDone).toHaveBeenCalledWith("fertig");
      expect(onError).not.toHaveBeenCalled();
      unmount();
    });
  });

  it("startet erneut für einen ANDEREN Schlüssel - ein Wechsel der Position ist kein Doppellauf", () => {
    const starts: string[] = [];
    const start = vi.fn(() => { starts.push("called"); return Promise.resolve("x"); });

    const { rerender } = renderHook(
      ({ key }) => useOncePerKey(key, start, () => {}, () => {}),
      { initialProps: { key: "1:2" as string | null } },
    );
    rerender({ key: "1:3" });

    expect(starts).toHaveLength(2);
  });

  it("liefert nichts mehr nach einem echten Unmount", async () => {
    const work = deferred<string>();
    const onDone = vi.fn();

    const { unmount } = renderHook(() => useOncePerKey("1:2", () => work.promise, onDone, () => {}));
    unmount();
    work.resolve("zu spät");
    await work.promise;

    expect(onDone).not.toHaveBeenCalled();
  });
});
