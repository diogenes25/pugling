import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { KlassenarbeitTerminFeld, klassenarbeitZeile, SchwellenFeld, WizardTrefferbereich, zeitraumZeile } from "./VaterWizard";
import { NICHT_ZUWEISBAR_GRUND, nurZuweisbare } from "./wizardSearch";
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
    schoolTypes: "Gymnasium", isOwn: true, isOwner: true, executePublic: false, canAssign: true, ...over,
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

/** B-408: die Zeile zeigt die Bewertung wie ihr Zwilling im Planbau (B-270). */
describe("WizardTrefferbereich – Bewertung je Zeile", () => {
  // Rot, wenn die Zeile ohne `ratingSummary` gerendert wird (dann fehlt der Stern-Text).
  it("zeigt die Bewertung einer bewerteten Übung", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung({ averageRating: 4.5, ratingCount: 2 })]} fehler={null} geladen={1} gesamt={1} />);
    expect(screen.getByText(/★ 4,5 \(2 Bewertungen\)/)).toBeTruthy();
  });
});

/*
 * B-140 AK 4: im Assistenten ist das Kästchen einer nicht zuweisbaren Übung gesperrt, und „Alle wählen" übernimmt nur, was
 * der Vater zuweisen darf – sonst scheiterte der Abschluss an `exercise_not_executable`, im schlimmsten Fall für Hunderte.
 */
describe("Assistent – nicht zuweisbare Übungen (B-140)", () => {
  // Rot, wenn `disabled` am Kästchen nur an `zeilenVeraltet` hängt oder der Grund nicht per `aria-describedby` verbunden ist.
  it("sperrt das Kästchen und nennt den Grund", () => {
    render(<WizardTrefferbereich {...BASIS} treffer={[uebung(), uebung({ id: 2, title: "Zurückgezogen", canAssign: false })]}
      fehler={null} geladen={2} gesamt={2} />);
    const gesperrt = screen.getByRole("checkbox", { name: /Zurückgezogen/ }) as HTMLInputElement;
    expect(gesperrt.disabled).toBe(true);
    expect(document.getElementById(gesperrt.getAttribute("aria-describedby")!)?.textContent).toBe(NICHT_ZUWEISBAR_GRUND);
    expect((screen.getByRole("checkbox", { name: /Unit 1 Wörter/ }) as HTMLInputElement).disabled).toBe(false);
  });

  // Rot, wenn „Alle wählen" alle Ids übernimmt (`items.map(e => e.id)`, der Ausgangszustand) oder die übersprungenen
  // verschweigt.
  it("„Alle wählen“ übernimmt nur zuweisbare und nennt die übersprungenen", () => {
    const folge = nurZuweisbare([{ id: 1, canAssign: true }, { id: 2, canAssign: false }, { id: 3, canAssign: true },
      { id: 4, canAssign: false }]);
    expect(folge.ids).toEqual([1, 3]);
    expect(folge.hinweis).toBe("2 Übungen gewählt, 2 nicht zuweisbare übersprungen.");
  });

  // Rot, wenn der Hinweis auch ohne Übersprungene erscheint – dann meldete jeder „Alle wählen"-Klick etwas, das nicht geschah.
  it("schweigt, wenn alles zuweisbar ist", () => {
    expect(nurZuweisbare([{ id: 1, canAssign: true }])).toEqual({ ids: [1], hinweis: null });
  });
});

/*
 * B-423: das Ziel „Klassenarbeit" fragt den Termin. Die Sperre von „Weiter" hängt an `terminProblem` (wizardFinish.test.ts)
 * und fährt die E2E `assistent-klassenarbeit.spec.ts`; hier steht, was das Feld selbst sagt.
 */
describe("Assistent – Termin der Klassenarbeit (B-423)", () => {
  // AK 1. Rot, wenn das Label „(Pflicht)" nicht trägt (der Grund für den gesperrten Knopf stünde dann nirgends).
  it("trägt „(Pflicht)“ im Namen des Feldes", () => {
    render(<KlassenarbeitTerminFeld termin="" heute="2026-11-14" onChange={() => {}} />);
    expect(screen.getByLabelText("Termin der Klassenarbeit (Pflicht)")).toBeTruthy();
  });

  // AK 1. Rot, wenn ein zu naher Termin keinen Grund am Feld nennt (`aria-describedby` fehlt oder der Text).
  it("nennt bei einem zu nahen Termin den Grund am Feld", () => {
    render(<KlassenarbeitTerminFeld termin="2026-11-15" heute="2026-11-14" onChange={() => {}} />);
    const feld = screen.getByLabelText("Termin der Klassenarbeit (Pflicht)");
    expect(document.getElementById(feld.getAttribute("aria-describedby")!)?.textContent).toMatch(/^Frühestens am 16\.11\.2026/);
  });

  // Gegenprobe: ein tauglicher Termin trägt keinen Grund. Rot, wenn der Grund unbedingt dasteht.
  it("schweigt bei einem tauglichen Termin", () => {
    render(<KlassenarbeitTerminFeld termin="2026-11-22" heute="2026-11-14" onChange={() => {}} />);
    expect(screen.getByLabelText("Termin der Klassenarbeit (Pflicht)").getAttribute("aria-describedby")).toBeNull();
    expect(screen.queryByText(/Frühestens/)).toBeNull();
  });

  // AK 2. Rot, wenn der Überblick das Ende auf den Termin legt (Dauer statt Dauer − 1) oder die Arbeit verschweigt.
  it("zeigt im Überblick Zeitraum bis zum Vortag und die Klassenarbeit", () => {
    expect(zeitraumZeile("2026-11-14", 8, true)).toBe("14.11.2026 – 21.11.2026 · 8 Tage");
    expect(klassenarbeitZeile("2026-11-22")).toBe("legt die Klassenarbeit am 22.11.2026 an");
  });

  // AK 6. Rot, wenn die anderen Ziele plötzlich ein Ende zeigen – `assistent.spec.ts` prüft genau diese Form.
  it("lässt den Zeitraum der anderen Ziele, wie er war", () => {
    expect(zeitraumZeile("2026-11-14", 30, false)).toBe("14.11.2026 · 30 Tage");
  });
});

/*
 * B-423, Review-Fund 3: das Schwellenfeld hält den Rohtext. Vorher stand der geklemmte Wert im Feld – „1" sprang sofort auf
 * die Untergrenze 2, und „12" ließ sich nicht tippen.
 */
function SchwellenRahmen({ max }: { max: number }) {
  const [wert, setWert] = useState("");
  const zahl = /^\d+$/.test(wert) ? Number(wert) : 4;
  return <SchwellenFeld wert={wert} onWert={setWert} angewandt={Math.min(Math.max(zahl, 2), max)} max={max} />;
}

describe("Assistent – Schwellentag der Leiter (B-423)", () => {
  // Rot, wenn das Feld den angewandten statt des getippten Werts zeigt: dann stünde nach der „1" eine „2".
  it("nimmt eine zweistellige Eingabe Ziffer für Ziffer an", () => {
    render(<SchwellenRahmen max={14} />);
    const feld = screen.getByLabelText("Zweite Stufe ab Tag") as HTMLInputElement;
    fireEvent.change(feld, { target: { value: "1" } });
    expect(feld.value).toBe("1");
    fireEvent.change(feld, { target: { value: "12" } });
    expect(feld.value).toBe("12");
  });

  // Rot, wenn `onBlur` fehlt: dann bliebe ein ungültiger Rohtext („1") stehen, obwohl Tag 2 gilt.
  it("zeigt beim Verlassen den angewandten Tag", () => {
    render(<SchwellenRahmen max={14} />);
    const feld = screen.getByLabelText("Zweite Stufe ab Tag") as HTMLInputElement;
    fireEvent.change(feld, { target: { value: "1" } });
    fireEvent.blur(feld);
    expect(feld.value).toBe("2");
  });
});
