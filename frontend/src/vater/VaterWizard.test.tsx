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
  geladen: 0, gesamt: 0, platzhalter: false, laedt: false, zeilenVeraltet: false, schluessel: "fach=1",
  fachName: "Englisch", selected: [] as number[], selectAllBusy: false,
  onToggle: () => {}, onSelectAll: () => {}, onRetry: () => {},
  typLabel: (t: string) => t,
  umfang: (_t: string, n: number | null | undefined) => (n == null ? null : `${n} Karten`),
  onTry: () => {},
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

  /*
   * B-216. `useAsync.reload()` setzt `error` auf `null` und `loading` auf `true` im selben Schritt. Hing das
   * Banner an `fehler` allein, verschwände es samt Knopf im Moment des Klicks — der Fokus fiele auf
   * `<body>`, und während der Wiederholung stünde nichts da. Rot macht das ein Banner, das nur `fehler`
   * liest: dann ist nach dem zweiten Render kein Knopf mehr im DOM.
   */
  it("hält Banner und Knopf während der Wiederholung fest, statt sie unter dem Finger wegzunehmen", () => {
    const { rerender } = render(<WizardTrefferbereich {...BASIS} treffer={[]} fehler="Netz weg" />);
    expect(screen.getByRole("button", { name: "Erneut suchen" })).toBeTruthy();

    rerender(<WizardTrefferbereich {...BASIS} treffer={[]} fehler={null} laedt />);

    expect(screen.getByText(/Die Übungssuche ist gescheitert: Netz weg/)).toBeTruthy();
    expect(screen.getByRole("button", { name: "Lädt…" })).toHaveProperty("disabled", true);
    // Im selben Fenster darf weder der Leer-Satz noch eine Trefferzahl stehen – sonst behauptete der
    // Bildschirm zwei Ursachen zugleich (Reviewer-Fund: beide hingen noch an `fehler`, nicht am Gedächtnis).
    expect(screen.queryByText(LEER_SATZ)).toBeNull();
    expect(screen.getByText("Suche gescheitert")).toBeTruthy();

    // Und es räumt sich ab, sobald die Wiederholung durch ist – sonst stünde der alte Fehler neben Treffern.
    rerender(<WizardTrefferbereich {...BASIS} treffer={[uebung()]} fehler={null} laedt={false} />);
    expect(screen.queryByText(/gescheitert/)).toBeNull();
  });

  /*
   * B-227, die andere Hälfte: Startet unter einem ANDEREN Schlüssel eine neue Suche, gehört der alte Fehler
   * zu einer Abfrage, die es nicht mehr gibt. Dann sagt die Fläche „Suche läuft…" – nicht „gescheitert" mit
   * einem „Lädt…"-Knopf darunter. Rot macht das ein Trefferbereich, der den Schlüssel nicht an das
   * Gedächtnis reicht: dann stehen Banner und „Suche gescheitert" hier noch.
   */
  it("räumt Banner und „gescheitert“ ab, wenn ein Filterwechsel eine neue Suche startet", () => {
    const { rerender } = render(<WizardTrefferbereich {...BASIS} treffer={[]} fehler="Netz weg" />);
    expect(screen.getByText(/Die Übungssuche ist gescheitert: Netz weg/)).toBeTruthy();

    rerender(<WizardTrefferbereich {...BASIS} treffer={[]} fehler={null} laedt zeilenVeraltet schluessel="fach=2" />);

    expect(screen.getByText("Suche läuft…")).toBeTruthy();
    expect(screen.queryByText(/gescheitert/)).toBeNull();
    expect(screen.queryByRole("button", { name: "Lädt…" })).toBeNull();
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

/** B-424: der Vater sieht je Zeile den Umfang und kann die Übung ausprobieren, ohne die Auswahl zu verändern. */
describe("WizardTrefferbereich – Umfang und Ausprobieren", () => {
  // AK 3. Rot, wenn die Zeile den Umfang nicht zeigt (dann fehlt „12 Karten").
  it("zeigt den Umfang der Übung in der Zeile", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung({ itemCount: 12 })]} fehler={null} geladen={1} gesamt={1} />);
    expect(screen.getByText(/· 12 Karten/)).toBeTruthy();
  });

  // Rot, wenn eine Übung ohne zählbare Atome einen Umfang behauptet (ein „· null"/„· 0" stünde dort).
  it("sagt nichts zum Umfang, wo es nichts zu zählen gibt", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung({ type: "Essay", itemCount: null })]} fehler={null} geladen={1} gesamt={1} />);
    expect(screen.getByText(/Unit 1 Wörter/).textContent).not.toMatch(/Karten|null/);
  });

  // AK 3. Rot, wenn der Knopf fehlt, die falsche Übung öffnet – oder im Label sitzt und die Auswahl mit umschaltet.
  it("öffnet den Testmodus für genau diese Übung und lässt die Auswahl stehen", () => {
    const onTry = vi.fn();
    const onToggle = vi.fn();
    const zwei = uebung({ id: 2, title: "Zweite" });
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung(), zwei]} fehler={null} geladen={2} gesamt={2}
      onTry={onTry} onToggle={onToggle} />);
    fireEvent.click(screen.getByRole("button", { name: "„Zweite\" ausprobieren" }));
    expect(onTry).toHaveBeenCalledWith(zwei);
    expect(onToggle).not.toHaveBeenCalled();
  });

  // Rot, wenn „Ausprobieren" auf Zeilen des vorigen Filters bedienbar bleibt (Review-Fund, wie das Kästchen B-169).
  it("sperrt Ausprobieren auf veralteten Zeilen", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung()]} fehler={null} geladen={1} gesamt={1} zeilenVeraltet />);
    expect((screen.getByRole("button", { name: "„Unit 1 Wörter\" ausprobieren" }) as HTMLButtonElement).disabled).toBe(true);
  });
});
