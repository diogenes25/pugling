import type { ReactNode } from "react";
import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useAsync } from "./useAsync";
import { AuthProvider } from "./auth";

/*
 * B-184. Vier von 41 Platzhaltern im Vater-Web prüften allein `data === null` und zeigten darum nach einer
 * gescheiterten ersten Ladung dauerhaft „Lade…" — neben dem Fehlerbanner, das im selben Blickfeld das
 * Gegenteil sagte.
 *
 * Warum der Test HIER sitzt und nicht an den vier Aufrufstellen: die sitzen in Seitenkomponenten, die am
 * Netz hängen, und kein Test dieses Frontends mockt `../lib/api` (CatalogAdmin.test.tsx). Am Hook ist der
 * Fehlerfall dagegen in drei Zeilen erreichbar. Abgedeckt ist damit die BEDINGUNG, nicht ihr Einbau — das
 * ist die ausgesprochene Grenze dieser Story, keine übersehene.
 *
 * `AuthProvider` ist Pflicht, kein Zierrat: `useAsync` ruft `useAuth`, und das wirft außerhalb des
 * Providers (auth.tsx:97).
 */

function wrapper({ children }: { children: ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

/** A promise plus its resolver/rejecter, so a test decides exactly when the async work "arrives". */
function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej; });
  // Ohne diesen Fänger meldet Node die Ablehnung als unbehandelt, bevor `useAsync` sie aufnimmt.
  promise.catch(() => {});
  return { promise, resolve, reject };
}

describe("useAsync – wann ein Platzhalter die Wahrheit sagt", () => {
  it("zeigt ihn, solange die erste Ladung läuft", async () => {
    const d = deferred<number[]>();
    const { result } = renderHook(() => useAsync(() => d.promise, []), { wrapper });

    expect(result.current.showPlaceholder).toBe(true);
    expect(result.current.error).toBeNull();

    await act(async () => { d.resolve([1, 2]); });
    expect(result.current.showPlaceholder).toBe(false);
  });

  it("nimmt ihn zurück, sobald die erste Ladung gescheitert ist", async () => {
    const d = deferred<number[]>();
    const { result } = renderHook(() => useAsync(() => d.promise, []), { wrapper });

    await act(async () => { d.reject(new Error("Netz weg")); });

    await waitFor(() => expect(result.current.error).not.toBeNull());
    // Der Kern der Story: `data` ist hier weiterhin `null` – trotzdem darf kein Platzhalter stehen,
    // denn es kommt nichts mehr. Genau diese Zusicherung fällt mit der Bedingung `data === null`.
    expect(result.current.data).toBeNull();
    expect(result.current.showPlaceholder).toBe(false);
  });

  it("bringt ihn beim Neuladen vorhandener Daten nicht zurück", async () => {
    let n = 0;
    const { result } = renderHook(
      () => useAsync(() => Promise.resolve([++n]), []), { wrapper });

    await waitFor(() => expect(result.current.data).not.toBeNull());
    expect(result.current.showPlaceholder).toBe(false);

    // Die Begründung der vier Aufrufstellen (VaterKind.tsx:89-93) hängt daran: Ein `reload` nach dem
    // Speichern darf das Formular NICHT aus dem DOM nehmen, sonst verliert es seinen Zustand samt der
    // Bestätigung. Der Fix ist zu dieser Regel additiv – dieser Fall belegt es.
    await act(async () => { result.current.reload(); });
    expect(result.current.showPlaceholder).toBe(false);
  });
});
