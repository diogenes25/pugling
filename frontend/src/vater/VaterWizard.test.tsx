import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { WizardTrefferbereich } from "./VaterWizard";
import type { ExerciseSummary } from "../lib/types";

/*
 * B-162. Der Assistent gab eine gescheiterte Übungssuche als leeren Katalog aus — mit einer
 * ausdrücklichen, falschen Handlungsanweisung („Lege welche unter ‚Übungen' an"). Wer ihr folgte, legte
 * eine Übung an, die längst existierte.
 *
 * Zwei Zusicherungen sind hier tragend und leicht zu übersehen:
 *
 * 1. Der Fehlerfall prüft die ABWESENHEIT des Leer-Satzes, nicht nur die Anwesenheit des Banners. Nur
 *    ohne sie könnte die Fläche beides gleichzeitig sagen, und genau das war der Schaden.
 * 2. Der „Erneut suchen"-Knopf ist kein Komfort. Über den Stepper zurück und wieder vor ändert keine
 *    Abhängigkeit von `useAsync` — es fliegt KEINE neue Abfrage. Ohne den Knopf stünde die neue,
 *    wahre Meldung über einem Bildschirm, aus dem nur F5 herausführt, und der kostet den ganzen
 *    Assistenten.
 */

function uebung(over: Partial<ExerciseSummary> = {}): ExerciseSummary {
  return {
    id: 1, seriesId: 2, seriesUnitId: 3, type: "Vocabulary", title: "Unit 1 Wörter",
    schoolTypes: "Gymnasium", isOwn: true, isOwner: true, executePublic: false, ...over,
  } as ExerciseSummary;
}

const BASIS = {
  geladen: 0, gesamt: 0, platzhalter: false, laedt: false, zeilenVeraltet: false,
  fachName: "Englisch", selected: [] as number[], selectAllBusy: false,
  onToggle: () => {}, onSelectAll: () => {}, onRetry: () => {},
  typLabel: (t: string) => t,
};

const LEER_SATZ = /Keine passenden Übungen im Katalog/;

describe("WizardTrefferbereich – gescheiterte Suche ist nicht leerer Katalog", () => {
  it("zeigt bei einem Fehler die Ursache und NICHT den Leer-Satz", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[]} fehler="Netz weg" />);

    expect(screen.getByText(/Die Übungssuche ist gescheitert/)).toBeTruthy();
    expect(screen.queryByText(LEER_SATZ)).toBeNull();
  });

  it("bietet einen Ausweg an, der wirklich neu sucht", () => {
    const onRetry = vi.fn();
    render(<WizardTrefferbereich {...BASIS} treffer={[]} fehler="Netz weg" onRetry={onRetry} />);

    fireEvent.click(screen.getByRole("button", { name: "Erneut suchen" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("zeigt während der ersten Ladung den Platzhalter und keine der beiden Meldungen", () => {
    // Der vierte der vier Zustände. Ohne diesen Fall behauptete die Beschreibung des Bausteins mehr,
    // als der Test belegt – und „Lade…" könnte klaglos neben dem Leer-Satz stehen.
    render(<WizardTrefferbereich {...BASIS} platzhalter laedt treffer={[]} fehler={null} />);

    expect(screen.getByText("Lade Übungen…")).toBeTruthy();
    expect(screen.queryByText(LEER_SATZ)).toBeNull();
    expect(screen.queryByText(/gescheitert/)).toBeNull();
  });

  it("lässt den Leer-Satz stehen, wenn die Suche wirklich nichts fand", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[]} fehler={null} />);

    expect(screen.getByText(LEER_SATZ)).toBeTruthy();
    expect(screen.queryByText(/gescheitert/)).toBeNull();
  });

  it("behauptet die Trefferzahl nicht, solange die Zeilen zum vorigen Filter gehören", () => {
    // Der Fall aus dem Ist-Stand: `useAsync` behält die ALTE Seite über den Filterwechsel. Ohne diese
    // Regel stünde „2 passende Übungen" neben Kästchen, die zu einer anderen Abfrage gehören.
    render(<WizardTrefferbereich {...BASIS} zeilenVeraltet
      treffer={[uebung(), uebung({ id: 2, title: "Unit 2 Wörter" })]} fehler={null} />);

    expect(screen.queryByText("2 passende Übungen")).toBeNull();
    expect(screen.getByText("Suche läuft…")).toBeTruthy();
  });

  it("nennt sie, sobald die Zeilen zum eingestellten Filter gehören", () => {
    // Die Gegenprobe: ohne sie wäre die Zusicherung oben auch dann erfüllt, wenn die Zahl NIE erschiene.
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung(), uebung({ id: 2 })]} fehler={null} />);

    expect(screen.getByText("2 passende Übungen")).toBeTruthy();
  });
});
