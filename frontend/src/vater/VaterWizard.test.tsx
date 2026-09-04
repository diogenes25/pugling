import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ListenAusfall, WizardTrefferbereich } from "./VaterWizard";
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

/*
 * B-188. Die drei Optionslisten des Assistenten (Kinder, Fächer, Arten) zeigten ihren Fehler nirgends.
 * Der Baustein trägt jetzt die Regel; DASS der Assistent ihn an allen drei Stellen benutzt, kann ein
 * Bausteintest nicht sagen — das prüft `e2e/assistent-listen-fallen-aus.spec.ts` am schwersten Fall.
 */
describe("ListenAusfall", () => {
  it("schweigt, solange nichts schiefgegangen ist", () => {
    const { container } = render(
      <ListenAusfall was="Deine Kinder" fehler={null} laedt={false} onRetry={() => {}} />);

    // Nicht „kein Banner", sondern **gar nichts**: Eine dauerhaft leere Fehlerfläche über jedem Schritt
    // wäre ein Kasten, der immer da ist und nie etwas sagt.
    expect(container.innerHTML).toBe("");
  });

  it("nennt die Liste beim Namen und bietet den Weg zurück an", () => {
    const onRetry = vi.fn();
    render(<ListenAusfall was="Deine Kinder" fehler="Netz weg" laedt={false} onRetry={onRetry} />);

    // Der Name der Liste trägt die Aussage: „konnte nicht geladen werden" allein sagt nicht, WAS fehlt,
    // und der Assistent lädt drei Listen.
    expect(screen.getByRole("alert").textContent).toMatch(/Deine Kinder konnten nicht geladen werden: Netz weg/);

    fireEvent.click(screen.getByRole("button", { name: "Erneut laden" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("bleibt während der Wiederholung stehen, statt unter dem Finger zu verschwinden", () => {
    const { rerender } = render(
      <ListenAusfall was="Die Fächer" fehler="Netz weg" laedt={false} onRetry={() => {}} />);

    /*
     * Der Zustand, den `useAsync.reload()` erzeugt: `error` wird `null` und `loading` `true` – im selben
     * Schritt. An `fehler` allein hinge das Banner samt Knopf genau im Moment des Klicks aus: Der gedrückte
     * Knopf verschwände unter dem Finger, der Fokus fiele auf `<body>`, und während der Wiederholung stünde
     * gar nichts da. Der Fall bildet diesen Übergang darum als Rerender nach, nicht als Anfangszustand.
     */
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt onRetry={() => {}} />);

    expect(screen.getByRole("alert").textContent).toMatch(/Die Fächer konnten nicht geladen werden: Netz weg/);
    const knopf = screen.getByRole("button", { name: "Lädt…" }) as HTMLButtonElement;
    // `disabled={busy}` an jedem Knopf, dessen Klick etwas auslöst (frontend/CLAUDE.md). Erst durch das
    // Festhalten oben ist dieser Zustand überhaupt erreichbar – vorher war die Sperre toter Code.
    expect(knopf.disabled).toBe(true);
  });

  it("räumt sich ab, wenn die Wiederholung geklappt hat", () => {
    const { rerender, container } = render(
      <ListenAusfall was="Die Fächer" fehler="Netz weg" laedt={false} onRetry={() => {}} />);
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt onRetry={() => {}} />);

    // Die Gegenprobe zum Festhalten: Ein Gedächtnis, das nie vergisst, ließe das Banner für immer stehen.
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt={false} onRetry={() => {}} />);

    expect(container.innerHTML).toBe("");
  });
});
