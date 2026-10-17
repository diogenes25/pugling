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
  function starte(fehler: string | null, laedt: boolean) {
    return renderHook((p: { fehler: string | null; laedt: boolean }) => useGemerkterFehler(p.fehler, p.laedt),
      { initialProps: { fehler, laedt } });
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
});
