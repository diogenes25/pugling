import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SelfAssessAnswer } from "./SohnTest";

/**
 * Die Selbsteinschätzung in der Klausur. Geprüft wird die **Reihenfolge**: Die Lösung – und mit B-70 auch die
 * gleichwertigen Antworten – darf erst nach dem Aufdecken stehen, sonst liest das Kind sie neben „Gewusst" und
 * trägt sich durch. Der Fehler lief unbemerkt, weil man ihn nur mit laufendem Server sehen konnte: die
 * Bedingung fragte zusätzlich `reveal !== null` ab, und das ist auf einer nicht-getippten Stufe immer wahr.
 */
describe("SelfAssessAnswer", () => {
  const view = (props: Partial<Parameters<typeof SelfAssessAnswer>[0]> = {}) => {
    const onJudge = vi.fn();
    const onReveal = vi.fn();
    const r = render(
      <SelfAssessAnswer reveal="riesig" alternatives={["sehr groß"]} revealed={false} busy={false}
        onReveal={onReveal} onJudge={onJudge} {...props} />
    );
    return { ...r, onJudge, onReveal };
  };

  it("verbirgt Lösung, Alternativen UND die Bewertung, solange nicht aufgedeckt ist", () => {
    const { container } = view();

    expect(screen.getByRole("button", { name: "Aufdecken 🔄" })).toBeTruthy();
    expect(container.textContent).not.toContain("riesig");
    // Die Alternativen sind Teil der Lösung: „auch richtig: sehr groß" verrät sie genauso.
    expect(container.textContent).not.toContain("sehr groß");
    // Ohne Lösung gibt es nichts zu bewerten – stünde der Knopf hier, wäre das Aufdecken überspringbar.
    expect(screen.queryByRole("button", { name: "Gewusst" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Nicht gewusst" })).toBeNull();
  });

  it("zeigt nach dem Aufdecken Lösung, gleichwertige Antworten und die Bewertung", () => {
    const { container, onJudge } = view({ revealed: true });

    expect(container.textContent).toContain("riesig");
    expect(container.textContent).toContain("auch richtig: sehr groß");
    expect(screen.queryByRole("button", { name: "Aufdecken 🔄" })).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Gewusst" }));
    expect(onJudge).toHaveBeenCalledWith(true);
    fireEvent.click(screen.getByRole("button", { name: "Nicht gewusst" }));
    expect(onJudge).toHaveBeenCalledWith(false);
  });

  it("meldet das Aufdecken nach oben, statt es selbst zu behalten", () => {
    const { onReveal } = view();
    fireEvent.click(screen.getByRole("button", { name: "Aufdecken 🔄" }));
    // Der Zustand liegt beim Test-Schirm: er setzt ihn beim Weiterrücken auf die nächste Frage zurück.
    expect(onReveal).toHaveBeenCalledTimes(1);
  });

  it("sperrt die Bewertung, während eine Antwort läuft", () => {
    view({ revealed: true, busy: true });
    expect(screen.getByRole("button", { name: "Gewusst" })).toHaveProperty("disabled", true);
    expect(screen.getByRole("button", { name: "Nicht gewusst" })).toHaveProperty("disabled", true);
  });
});
