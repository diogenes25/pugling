import { renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useGemerkterFehler } from "./useGemerkterFehler";

/*
 * B-216. Die Regel, die vorher nur in `ListenAusfall` stand und an drei weiteren Stellen fehlte: Der
 * letzte Fehler überlebt den Start der Wiederholung. `useAsync.reload()` (und der Hand-Lader des
 * Shop-Verlaufs) setzen `error` auf `null` und `loading` auf `true` im selben Schritt – wer sein Banner an
 * `fehler` allein hängt, verliert es genau dann, wenn der Nutzer den Knopf darin drückt.
 *
 * Geprüft wird die Regel, nicht eine Darstellung: Jede der vier Aufrufstellen prüft dann nur noch, dass
 * sie den Hook benutzt (siehe `VaterWizard.test.tsx`, `HistoryTab.test.tsx`, `ListenAusfall.test.tsx`).
 */
describe("useGemerkterFehler", () => {
  type Props = { fehler: string | null; laedt: boolean; schluessel?: string };
  function starte(fehler: string | null, laedt: boolean, schluessel?: string) {
    return renderHook<string | null, Props>(
      (p) => useGemerkterFehler(p.fehler, p.laedt, p.schluessel),
      { initialProps: { fehler, laedt, schluessel } },
    );
  }

  it("gibt nichts zurück, solange nichts schiefgegangen ist", () => {
    const { result, rerender } = starte(null, true);
    expect(result.current).toBeNull();
    rerender({ fehler: null, laedt: false });
    expect(result.current).toBeNull();
  });

  it("gibt den Fehler zurück, solange er steht", () => {
    const { result } = starte("Netz weg", false);
    expect(result.current).toBe("Netz weg");
  });

  it("hält den letzten Fehler fest, während die Wiederholung läuft", () => {
    const { result, rerender } = starte("Netz weg", false);
    rerender({ fehler: null, laedt: true });
    expect(result.current).toBe("Netz weg");
  });

  it("räumt ihn ab, sobald die Wiederholung ohne Fehler durch ist", () => {
    const { result, rerender } = starte("Netz weg", false);
    rerender({ fehler: null, laedt: true });
    rerender({ fehler: null, laedt: false });
    expect(result.current).toBeNull();
  });

  it("ersetzt ihn, wenn die Wiederholung erneut scheitert", () => {
    const { result, rerender } = starte("Netz weg", false);
    rerender({ fehler: null, laedt: true });
    rerender({ fehler: "Immer noch weg", laedt: false });
    expect(result.current).toBe("Immer noch weg");
  });

  /*
   * B-227. `useAsync` setzt `error = null, loading = true` nicht nur bei `reload()`, sondern auch bei einem
   * Wechsel der Abhängigkeiten – und der Hook konnte die beiden nicht auseinanderhalten: Nach einer
   * gescheiterten Suche stand beim Filterwechsel der alte Fehler neben „Lädt…", für eine Abfrage, die es
   * nicht mehr gab. Rot macht das ein Hook ohne Schlüssel: dann liefert er hier „Netz weg" statt `null`.
   */
  it("lässt ihn fallen, wenn unter einem anderen Schlüssel eine neue Abfrage startet", () => {
    const { result, rerender } = starte("Netz weg", false, "fach=1");
    rerender({ fehler: null, laedt: true, schluessel: "fach=2" });
    expect(result.current).toBeNull();
  });

  it("hält ihn dagegen fest, wenn dieselbe Abfrage (gleicher Schlüssel) wiederholt wird", () => {
    const { result, rerender } = starte("Netz weg", false, "fach=1");
    rerender({ fehler: null, laedt: true, schluessel: "fach=1" });
    expect(result.current).toBe("Netz weg");
  });

  /*
   * Die Sequenz, die der Browser wirklich erzeugt (E2E-Fund im Nachtlauf 2026-10-17, zweiter Lauf): Der
   * Filterwechsel rendert ZUERST mit dem alten Fehler und dem neuen Schlüssel – `useAsync` räumt `error`
   * erst in seinem Effekt. Ein Gedächtnis, das in diesem Render den alten Fehler unter dem neuen Schlüssel
   * neu merkt, zeigt ihn danach als Fehler der neuen Suche. Rot macht das ein Effekt, der am Schlüssel hängt.
   */
  it("merkt den alten Fehler nicht unter dem neuen Schlüssel, wenn der Wechsel ihn ein Render lang noch trägt", () => {
    const { result, rerender } = starte("Netz weg", false, "fach=1");
    rerender({ fehler: "Netz weg", laedt: false, schluessel: "fach=2" }); // der Zwischen-Render
    rerender({ fehler: null, laedt: true, schluessel: "fach=2" });
    expect(result.current).toBeNull();
  });

  it("merkt einen gleichlautenden Fehler der neuen Suche unter deren Schlüssel", () => {
    const { result, rerender } = starte("Netz weg", false, "fach=1");
    rerender({ fehler: "Netz weg", laedt: false, schluessel: "fach=2" });
    rerender({ fehler: null, laedt: true, schluessel: "fach=2" });
    rerender({ fehler: "Netz weg", laedt: false, schluessel: "fach=2" }); // auch die neue Suche scheitert so
    rerender({ fehler: null, laedt: true, schluessel: "fach=2" }); // … und wird wiederholt
    expect(result.current).toBe("Netz weg");
  });
});
