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

  /*
   * Der zweite Weg in dieselbe Lüge, gefunden im Code-Review zum Sprint: nicht ein FEHLER, sondern ein
   * `fn`, das rechtmäßig `null` liefert (ein optionales Profil, ein Kind ohne Lehrbuch). Dann ist `data`
   * `null` und `error` `null` – die alte Bedingung `data === null && error === null` blieb also für immer
   * wahr, und diesmal stand nicht einmal ein Fehlerbanner daneben, das den „Lade…" verraten hätte.
   */
  it("nimmt ihn zurück, wenn die Ladung rechtmäßig null liefert", async () => {
    const d = deferred<number[] | null>();
    const { result } = renderHook(() => useAsync(() => d.promise, []), { wrapper });

    expect(result.current.showPlaceholder).toBe(true);

    await act(async () => { d.resolve(null); });

    // Weder Daten noch Fehler – und trotzdem ist der Versuch durch, es kommt nichts mehr.
    expect(result.current.data).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.showPlaceholder).toBe(false);
  });

  /*
   * Und er darf danach auch nicht wiederkommen: Genau darauf steht das Anlege-Formular der Units
   * (`VaterLehrwerke.tsx`, „Unit hinzufügen"). Käme der Platzhalter beim `reload` zurück, nähme er das
   * Formular mitsamt seinem Zustand aus dem DOM – die Regel, die dieses Feld überhaupt begründet.
   */
  it("bringt ihn nach einer rechtmäßigen null-Ladung auch beim Neuladen nicht zurück", async () => {
    // ZWEI `deferred`, damit die zweite Ladung OFFEN bleibt. Mit einem `Promise.resolve(null)` entleert
    // `act` die Microtask-Kette, `loading` ist danach schon wieder `false` – das Ladefenster, um das es
    // hier geht, wäre nie beobachtet worden. Genau daran ist der erste Entwurf dieses Falls gescheitert:
    // er fiel am `waitFor` der ersten Ladung (Zeile darüber) und belegte damit denselben Satz wie der
    // Fall vor ihm, nicht seinen eigenen.
    const first = deferred<number[] | null>();
    const second = deferred<number[] | null>();
    let calls = 0;
    const { result } = renderHook(
      () => useAsync(() => (++calls === 1 ? first.promise : second.promise), []), { wrapper });

    await act(async () => { first.resolve(null); });
    expect(result.current.showPlaceholder).toBe(false);

    await act(async () => { result.current.reload(); });

    // Tragend, nicht Zierrat: ohne diese Zeile könnte die nächste auch dann grün sein, wenn gar keine
    // zweite Ladung läuft – und dann sagt sie nichts über das Neuladen.
    expect(result.current.loading).toBe(true);
    expect(result.current.showPlaceholder).toBe(false);

    await act(async () => { second.resolve(null); });
  });

  it("bringt ihn beim Neuladen vorhandener Daten nicht zurück", async () => {
    // Auch hier zwei `deferred` statt eines aufgelösten Promise, und aus demselben Grund wie im Fall
    // darunter: der Vorgänger prüfte nach einem `act` über einem `Promise.resolve`, in dem das
    // Ladefenster längst zu war. Diese Fassung stammt aus B-184 und war die Vorlage, von der der neue
    // Fall den Fehler geerbt hat – darum wandert sie mit, statt als leere Zusicherung daneben zu
    // bleiben.
    const first = deferred<number[]>();
    const second = deferred<number[]>();
    let calls = 0;
    const { result } = renderHook(
      () => useAsync(() => (++calls === 1 ? first.promise : second.promise), []), { wrapper });

    await act(async () => { first.resolve([1]); });
    expect(result.current.showPlaceholder).toBe(false);

    // Die Begründung der vier Aufrufstellen (VaterKind.tsx:89-93) hängt daran: Ein `reload` nach dem
    // Speichern darf das Formular NICHT aus dem DOM nehmen, sonst verliert es seinen Zustand samt der
    // Bestätigung. Der Fix ist zu dieser Regel additiv – dieser Fall belegt es.
    await act(async () => { result.current.reload(); });

    expect(result.current.loading).toBe(true);
    expect(result.current.data).not.toBeNull();
    expect(result.current.showPlaceholder).toBe(false);

    await act(async () => { second.resolve([2]); });
  });
});

/*
 * B-164: `stale` sagt, ob die vorliegenden Daten zu **anderen** Abhängigkeiten gehören als den aktuellen – die Frage, die
 * `loading` nicht beantwortet (es zieht „andere Abfrage" und „dieselbe neu geladen" zusammen). Die Aufrufer mit eigenem
 * Schlüsselvergleich (`PlanPositions`, Assistent) lesen jetzt dieses Feld. Jeder Fall unterscheidet sich vom Ausgangszustand
 * in genau einer Sache.
 */
describe("useAsync – ob die Daten zu den aktuellen Abhängigkeiten gehören", () => {
  // Rot, wenn `stale` fehlt, immer falsch ist oder schon beim Start der Abfrage nachgezogen wird: nach dem Wechsel läuft
  // eine andere Abfrage, die Daten sind von vorhin. (Die Mutante „= `loading` mit Daten" fängt dieser Fall nicht – sie
  // trifft die zwei Fälle darunter.)
  it("meldet die Daten nach einem Wechsel als veraltet, bis die neue Antwort da ist", async () => {
    const a = deferred<string>();
    const b = deferred<string>();
    const { result, rerender } = renderHook(({ key }) => useAsync(() => (key === "a" ? a.promise : b.promise), [key]),
      { wrapper, initialProps: { key: "a" } });
    await act(async () => { a.resolve("A"); });
    expect(result.current.stale).toBe(false);

    rerender({ key: "b" });
    expect([result.current.data, result.current.stale]).toEqual(["A", true]);

    await act(async () => { b.resolve("B"); });
    expect([result.current.data, result.current.stale]).toEqual(["B", false]);
  });

  // Rot, wenn `stale` an `loading` hängt: ein `reload` lädt dieselbe Sache neu, die Zeilen bleiben gültig und bedienbar.
  it("meldet ein Neuladen derselben Abfrage nicht als veraltet", async () => {
    const first = deferred<string>();
    const second = deferred<string>();
    let calls = 0;
    const { result } = renderHook(() => useAsync(() => (++calls === 1 ? first.promise : second.promise), ["x"]), { wrapper });
    await act(async () => { first.resolve("A"); });

    await act(async () => { result.current.reload(); });
    expect([result.current.loading, result.current.stale]).toEqual([true, false]);
    await act(async () => { second.resolve("A2"); });
  });

  // Rot, wenn `stale` mit dem Ende der Abfrage zurückfällt: scheitert die neue, stehen weiter die alten Daten da (B-169).
  // Und rot, wenn „Erneut suchen" (`reload`) die Abhängigkeiten schon beim Start nachzieht: während der Wiederholung
  // stehen noch die Zeilen von A da, und sie würden wählbar (Review B-164).
  it("bleibt veraltet, wenn die neue Abfrage scheitert, und auch während ihrer Wiederholung", async () => {
    const a = deferred<string>();
    const b = deferred<string>();
    const retry = deferred<string>();
    let callsB = 0;
    const { result, rerender } = renderHook(
      ({ key }) => useAsync(() => (key === "a" ? a.promise : ++callsB === 1 ? b.promise : retry.promise), [key]),
      { wrapper, initialProps: { key: "a" } });
    await act(async () => { a.resolve("A"); });
    rerender({ key: "b" });
    await act(async () => { b.reject(new Error("weg")); });
    expect([result.current.loading, result.current.data, result.current.stale]).toEqual([false, "A", true]);

    await act(async () => { result.current.reload(); });
    expect([result.current.loading, result.current.data, result.current.stale]).toEqual([true, "A", true]);
    await act(async () => { retry.resolve("B"); });
    expect([result.current.data, result.current.stale]).toEqual(["B", false]);
  });

  // Rot, wenn schon die erste Ladung als veraltet gilt: das Gezeigte (noch nichts) gehört zu den Abhängigkeiten des
  // ersten Renders.
  it("ist während der ersten Ladung nicht veraltet", () => {
    const pending = deferred<string>();
    const { result } = renderHook(() => useAsync(() => pending.promise, ["x"]), { wrapper });
    expect([result.current.loading, result.current.stale]).toEqual([true, false]);
  });

  // Rot, wenn „ohne Daten nie veraltet" gilt: scheitert die erste Abfrage und wechseln die Kriterien, läuft eine ANDERE
  // Suche – der Planbau sagt dann „Suche läuft…", nicht „0 Treffer" (gemessen: `filterwechsel-raeumt-den-fehler.spec.ts`).
  it("ist nach gescheiterter erster Ladung und einem Wechsel veraltet", async () => {
    const a = deferred<string>();
    const b = deferred<string>();
    const { result, rerender } = renderHook(({ key }) => useAsync(() => (key === "a" ? a.promise : b.promise), [key]),
      { wrapper, initialProps: { key: "a" } });
    await act(async () => { a.reject(new Error("weg")); });
    expect(result.current.stale).toBe(false);
    rerender({ key: "b" });
    expect([result.current.data, result.current.stale]).toEqual([null, true]);
    await act(async () => { b.resolve("B"); });
    expect(result.current.stale).toBe(false);
  });

  // Rot, wenn nach Identität des Arrays statt je Element verglichen wird: jeder Render baut eine neue Dep-Liste.
  it("vergleicht die Abhängigkeiten je Element, nicht als Liste", async () => {
    const a = deferred<string>();
    const { result, rerender } = renderHook(({ n }) => useAsync(() => a.promise, [n, "fest"]),
      { wrapper, initialProps: { n: 1 } });
    await act(async () => { a.resolve("A"); });
    rerender({ n: 1 });
    expect(result.current.stale).toBe(false);
  });
});
