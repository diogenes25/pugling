import { fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { TextbookForm } from "./ChildMaterialSection";
import { AuthProvider } from "../lib/auth";
import type { SeriesUnitResponse, SubjectResponse, TextbookResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * B-148. `textbookPatch.test.ts` prüft die REGEL (was in den PATCH-Rumpf gehört) – aber die Lehre aus
 * B-143 ist, dass der Defekt dort gar nicht sitzen muss: Kaputt war, dass das Formular einen Zustand des
 * Modells nicht anzeigen konnte. Für das Lehrbuch am Kind gab es diese Prüfebene bis zu dieser Story
 * überhaupt nicht.
 *
 * Ohne diese Datei liefe ein späteres Entfernen der deaktivierten `<option>` grün durch — und mit ihr
 * fiele der Schutz vorm versehentlichen Löschen weg, denn er hängt daran, dass `form` gleich `loaded`
 * bleiben KANN.
 */

const SUBJECTS: SubjectResponse[] = [
  { id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isOwn: false },
  { id: 2, name: "Mathe", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isOwn: false },
];

function buch(overrides: Partial<TextbookResponse> = {}): TextbookResponse {
  return {
    id: 4, title: "Access 8", subjectId: 1, subjectName: "Englisch", grade: 8, publisher: "Cornelsen",
    isbn: null, currentChapter: "Unit 4",
    // Ohne Reihe lädt die Unit-Liste nicht – die Komponente kommt so ohne gefälschtes `fetch` aus.
    seriesId: null, currentUnitId: null, ...overrides,
  } as TextbookResponse;
}

// `AuthProvider` ist Pflicht, obwohl nichts geladen wird: `useAsync` hängt für den 401-Fall an `useAuth`,
// und die Unit-Liste benutzt es auch dann, wenn sie ohne Reihe gar nicht abfragt.
const zeige = (o: Partial<TextbookResponse> = {}) =>
  render(
    <AuthProvider>
      <TextbookForm onMeldung={() => {}} onSeriesCreated={() => {}} childId={1} book={buch(o)} series={[]} subjects={SUBJECTS} onDone={() => {}} />
    </AuthProvider>,
  );

describe("TextbookForm – Zustände, die das Modell erlaubt", () => {
  it("zeigt ein Freitext-Fach als vorausgewählte, gesperrte Option", () => {
    zeige({ subjectId: null, subjectName: "Erdkunde" });

    const feld = screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("__freetext__");
    const option = screen.getByRole("option", { name: "Erdkunde (Freitext)" }) as HTMLOptionElement;
    // Gesperrt ist die halbe Aussage: so kann der Nutzer den Zustand sehen, aber nicht herstellen.
    expect(option.disabled).toBe(true);
  });

  it("zeigt keine Freitext-Option, wenn das Fach im Katalog steht", () => {
    zeige();

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("zeigt keine Freitext-Option, wenn gar kein Fach hinterlegt ist", () => {
    // Die Gegenprobe zum Fall darüber: Ohne Namen gibt es nichts zu zeigen, und eine leere Option
    // „ (Freitext)" wäre schlimmer als keine.
    zeige({ subjectId: null, subjectName: null });

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("beschriftet das Fach-Feld mit einer Erklärung", () => {
    // Das Etikett „(Freitext)" sagt WAS, nicht WARUM – das trägt die Feldhilfe (B-148, Entscheidung 4).
    zeige({ subjectId: null, subjectName: "Erdkunde" });

    expect(screen.getByLabelText("Erklärung zu „Fach\"", { exact: true })).toBeTruthy();
  });
});

/*
 * B-172, nachgetragen nach dem Review: die drei Referenzfelder dieses Formulars waren vom neuen
 * Verhalten völlig unabgedeckt. Gemessen hat der `frontend-reviewer` das, indem er die Unit-Bedingung
 * entfernte – 299/299 blieben grün.
 *
 * Ein gefälschtes `fetch` braucht es dafür nicht: mit gesetzter `seriesId` scheitert der echte
 * `seriesUnits`-Abruf in happy-dom von selbst. Das IST der zu prüfende Fehlerfall.
 *
 * **Der `ECONNREFUSED`-Block in der Ausgabe gehört dazu** – er ist dieser scheiternde Abruf, nicht ein
 * Mangel am Test. Wer ihn wegräumt, räumt den Fehlerfall weg; die Fälle oben halten deshalb weiter an
 * `seriesId: null` fest, wo sie ihn nicht brauchen.
 *
 * Rot macht diese Fälle das Entfernen der Rückfall-Option bzw. der Bedingung `eigeneUnit`.
 */
describe("TextbookForm – wenn eine Optionsliste den geladenen Wert nicht kennt", () => {
  const feld = (name: string) => screen.getByLabelText(name, { exact: true }) as HTMLSelectElement;

  it("zeigt das geladene Fach, wenn die Fächerliste es nicht kennt", () => {
    render(
      <AuthProvider>
        <TextbookForm onMeldung={() => {}} onSeriesCreated={() => {}} childId={1} book={buch()} series={[]} subjects={[]} onDone={() => {}} />
      </AuthProvider>,
    );

    expect(feld("Fach").value).toBe("1");
    expect((screen.getByRole("option", { name: "Englisch (nicht geladen)" }) as HTMLOptionElement)
      .disabled).toBe(false);
  });

  it("zeigt die geladene Reihe und die geladene Unit, wenn ihre Listen fehlen", async () => {
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    expect(feld("Reihe im Katalog").value).toBe("9");
    expect(screen.getByRole("option", { name: "Access (nicht geladen)" })).toBeTruthy();

    expect(feld("Aktuelle Unit").value).toBe("7");
    // `find`, nicht `get`: Seit B-235 heißt die Unit „(lädt…)", solange der Abruf läuft – „(nicht geladen)"
    // ist erst der Zustand NACH dem gescheiterten Abruf. Vorher las dieser Fall den Ausfall synchron ab.
    // Der Fehlschlag kommt weiter vom ECHTEN Abruf (siehe Block oben), nicht von einem Spion wie in den
    // B-235-Fällen unten – bewusst, weil dieser Block genau den echten Fehlerfall halten will. Preis: `find`
    // wartet auf einen `ECONNREFUSED` gegen localhost (sofort) statt auf ein abgelehntes Promise.
    expect(await screen.findByRole("option", { name: "Unit 7 (nicht geladen)" })).toBeTruthy();
  });

  it("nimmt die Unit-Option zurück, sobald die Reihe gewechselt ist", () => {
    // Sonst könnte der Nutzer eine Unit der ALTEN Reihe abschicken. `chooseSeries` räumt den Wert,
    // die Option muss mitgehen – Id UND Name hängen darum an derselben Bedingung.
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    fireEvent.change(feld("Reihe im Katalog"), { target: { value: "" } });

    expect(feld("Aktuelle Unit").value).toBe("");
    expect(screen.queryByRole("option", { name: /Unit 7/ })).toBeNull();
  });
});

/*
 * B-235. Die Unit-Liste lädt IM Formular (nicht auf Seitenebene), und solange sie unterwegs ist, beschriftete
 * das Feld den geladenen Wert mit der Beschriftung des AUSFALLS – „(nicht geladen)" bei jedem Öffnen, auch
 * wenn alles klappt. B-229 hatte dasselbe am Kind-Feld des Plan-Formulars repariert und diese Stelle für
 * „lädt auf Seitenebene" gehalten.
 *
 * Hier wird der Abruf gefälscht, nicht das `fetch`: `vi.spyOn(api, "seriesUnits")` hält das Fenster offen
 * (ein nie auflösendes Promise) bzw. schließt es als Fehlschlag – beides deterministisch, ohne auf den
 * `ECONNREFUSED` des echten Abrufs zu warten.
 *
 * Rot macht den ersten Fall das Fehlen von `laedt` am Unit-Feld (gemessen vor dem Fix: „Unit 7 (nicht
 * geladen)" statt „(lädt…)"). Der zweite ist die Gegenprobe und war schon vor dem Fix grün – er fällt nur,
 * wenn das Fenster nach dem Fehlschlag offen bliebe (`loading` statt eines Zustands, der den Abschluss kennt).
 */
describe("TextbookForm – solange die Units laden (B-235)", () => {
  afterEach(() => vi.restoreAllMocks());
  const unit = () => screen.getByLabelText("Aktuelle Unit", { exact: true }) as HTMLSelectElement;

  it("beschriftet die gespeicherte Unit mit „(lädt…)“ und sperrt das Feld, solange die Liste unterwegs ist", () => {
    vi.spyOn(api, "seriesUnits").mockReturnValue(new Promise(() => {}));
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    expect(unit().value).toBe("7");
    expect(screen.getByRole("option", { name: "Unit 7 (lädt…)" })).toBeTruthy();
    expect(unit().disabled).toBe(true);
    // Im Unit-Feld, nicht auf der Seite: das Reihen-Feld sagt hier zu Recht „(nicht geladen)" – seine Liste
    // kommt als Prop und ist leer (gemessen: die seitenweite Abfrage traf „Access (nicht geladen)").
    expect(within(unit()).queryByRole("option", { name: /nicht geladen/ })).toBeNull();
  });

  it("sagt nach einem gescheiterten Abruf weiter „(nicht geladen)“ und gibt das Feld frei", async () => {
    vi.spyOn(api, "seriesUnits").mockRejectedValue(new Error("Verbindung weg"));
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    expect(await screen.findByRole("option", { name: "Unit 7 (nicht geladen)" })).toBeTruthy();
    expect(unit().disabled).toBe(false);
    expect(within(unit()).queryByRole("option", { name: /lädt…/ })).toBeNull();
  });

  /*
   * Entscheidung 1 der Story – `loading`, nicht `showPlaceholder` – hielt kein Test (Nachschau 2026-10-23):
   * beim ersten Laden sind beide wahr, nach dem Fehlschlag beide falsch. Auseinander laufen sie erst beim
   * ZWEITEN Laden derselben Reihe: Reihe weg (ihre Units kommen an), Reihe zurück (die eigenen Units sind
   * wieder unterwegs) – `showPlaceholder` ist dann längst dauerhaft falsch und beschriftete die gespeicherte
   * Unit wieder mit dem Ausfall. Drei kontrollierte Promises reichen, ein Zwischen-Render muss nicht
   * nachgespielt werden. Rot unter `laedt={units.showPlaceholder}` (gemessen), grün unter `loading`.
   */
  it("sagt „(lädt…)“ auch beim zweiten Laden der eigenen Reihe – nach Wechsel weg und zurück", async () => {
    const reihe = (id: number, name: string): TextbookSeriesResponse => ({
      id, name, slug: name.toLowerCase(), schoolTypes: "None", isOwn: false, unitCount: 1,
      createdAt: "2026-01-01T00:00:00Z", subjectId: 1,
    });
    const einheit = (id: number, seriesId: number, label: string): SeriesUnitResponse =>
      ({ id, seriesId, label, orderIndex: 0, grade: null, bookType: null } as unknown as SeriesUnitResponse);
    // 1. Abruf (Reihe 9) kommt an, 2. Abruf (Reihe 10) kommt an, 3. Abruf (wieder Reihe 9) bleibt offen.
    vi.spyOn(api, "seriesUnits")
      .mockResolvedValueOnce([einheit(7, 9, "Unit 7")])
      .mockResolvedValueOnce([einheit(11, 10, "Unit 11")])
      .mockReturnValueOnce(new Promise(() => {}));
    render(
      <AuthProvider>
        <TextbookForm onMeldung={() => {}} onSeriesCreated={() => {}} childId={1} onDone={() => {}} subjects={SUBJECTS}
          series={[reihe(9, "Access"), reihe(10, "Camden")]}
          book={buch({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" })} />
      </AuthProvider>,
    );
    const reiheFeld = () => screen.getByLabelText("Reihe im Katalog", { exact: true });
    expect(await within(unit()).findByRole("option", { name: "Unit 7" })).toBeTruthy();

    fireEvent.change(reiheFeld(), { target: { value: "10" } });
    expect(await within(unit()).findByRole("option", { name: "Unit 11" })).toBeTruthy();

    fireEvent.change(reiheFeld(), { target: { value: "9" } });
    expect(await within(unit()).findByRole("option", { name: "Unit 7 (lädt…)" })).toBeTruthy();
    expect(unit().disabled).toBe(true);
    expect(within(unit()).queryByRole("option", { name: /nicht geladen/ })).toBeNull();
  });
});

/*
 * B-64. Der Katalog ist der Vorschlagsweg (Entscheidung 1), eine Reihe lässt sich aus dem Formular heraus
 * anlegen (Entscheidung 2), und Verlag/Klasse kommen aus dem Katalog, sobald er sie kennt (Entscheidung 3).
 *
 * Rot macht diese Fälle: das alte gleichrangige Raster (Titel vor der Reihe, keine Gruppe „Aus dem Katalog"),
 * ein immer sichtbares Verlags-/Klassenfeld (die doppelte Wahrheit) bzw. ein fehlender Rückfall, wenn der
 * Katalog den Wert NICHT kennt, und ein Inline-Anlegen, das die neue Reihe nicht wählt.
 */
describe("TextbookForm – Katalog zuerst, Verlag und Klasse aus dem Katalog (B-64)", () => {
  afterEach(() => vi.restoreAllMocks());
  const reihe = (id: number, name: string, publisherName: string | null): TextbookSeriesResponse => ({
    id, name, slug: name.toLowerCase(), schoolTypes: "None", isOwn: true, unitCount: 1,
    createdAt: "2026-01-01T00:00:00Z", subjectId: 1, publisherName,
  } as TextbookSeriesResponse);
  const einheit = (id: number, seriesId: number, grade: number | null): SeriesUnitResponse =>
    ({ id, seriesId, label: `Unit ${id}`, orderIndex: 0, grade, bookType: null } as unknown as SeriesUnitResponse);
  const zeigeMit = (series: TextbookSeriesResponse[], o: Partial<TextbookResponse> = {}, onSeriesCreated = () => {}) =>
    render(
      <AuthProvider>
        <TextbookForm onMeldung={() => {}} onSeriesCreated={onSeriesCreated} childId={1} onDone={() => {}}
          subjects={SUBJECTS} series={series} book={buch(o)} />
      </AuthProvider>,
    );

  it("stellt die Katalog-Gruppe vor Titel und Freitext", () => {
    zeigeMit([]);
    const katalog = screen.getByRole("group", { name: /Aus dem Katalog/ });
    const freitext = screen.getByRole("group", { name: /Ohne Katalog/ });
    const titel = screen.getByLabelText("Titel", { exact: true });
    expect(within(katalog).getByLabelText("Reihe im Katalog", { exact: true })).toBeTruthy();
    expect(within(freitext).getByLabelText("Verlag", { exact: true })).toBeTruthy();
    // DOCUMENT_POSITION_FOLLOWING: das zweite Element steht im Dokument hinter dem ersten.
    expect(katalog.compareDocumentPosition(titel) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(titel.compareDocumentPosition(freitext) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  /*
   * Buch und Katalog tragen bewusst VERSCHIEDENE Werte („Klett" gegen „Cornelsen"): bei gleichen Werten bliebe
   * der Fall grün, auch wenn die Anzeige den Freitext statt des Katalogs läse (Review 2026-10-26).
   */
  it("zeigt den Verlag der gewählten Reihe als Text statt als Eingabefeld – und folgt einem Reihenwechsel", () => {
    vi.spyOn(api, "seriesUnits").mockResolvedValue([]);
    zeigeMit([reihe(9, "Access", "Cornelsen"), reihe(10, "Camden", "Westermann")],
      { seriesId: 9, seriesName: "Access", publisher: "Klett" });

    expect(screen.queryByLabelText("Verlag", { exact: true })).toBeNull();
    expect(screen.getByText("Cornelsen")).toBeTruthy();
    expect(screen.queryByText("Klett")).toBeNull();
    expect(screen.getByText(/aus dem Katalog/)).toBeTruthy();

    fireEvent.change(screen.getByLabelText("Reihe im Katalog", { exact: true }), { target: { value: "10" } });
    expect(screen.getByText("Westermann")).toBeTruthy();
    expect(screen.queryByText("Cornelsen")).toBeNull();
  });

  it("lässt das Verlagsfeld stehen, wenn die Reihe keinen Verlag kennt", () => {
    vi.spyOn(api, "seriesUnits").mockResolvedValue([]);
    zeigeMit([reihe(9, "Access", null)], { seriesId: 9, seriesName: "Access", publisher: "Freitext-Verlag" });

    expect((screen.getByLabelText("Verlag", { exact: true }) as HTMLInputElement).value).toBe("Freitext-Verlag");
  });

  it("zeigt die Klasse der Unit als Text – und das Feld wieder, sobald die Unit keinen Band hat", async () => {
    vi.spyOn(api, "seriesUnits").mockResolvedValue([einheit(7, 9, 7), einheit(8, 9, null)]);
    // Gespeichert ist 9, die Unit sagt 7 – verschieden, damit die Anzeige den Katalog belegt, nicht den Freitext.
    zeigeMit([reihe(9, "Access", "Cornelsen")],
      { seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7", grade: 9 });

    const hinweis = await screen.findByText(/aus der Unit/);
    expect(hinweis.parentElement!.textContent).toBe("7 · aus der Unit");
    expect(screen.queryByLabelText("Für Klasse", { exact: true })).toBeNull();

    fireEvent.change(screen.getByLabelText("Aktuelle Unit", { exact: true }), { target: { value: "8" } });
    expect(screen.getByLabelText("Für Klasse", { exact: true })).toBeTruthy();
  });

  it("legt eine neue Reihe mit dem Fach des Buchs an und wählt sie", async () => {
    vi.spyOn(api, "seriesUnits").mockResolvedValue([]);
    const anlegen = vi.spyOn(api, "createTextbookSeries").mockResolvedValue(reihe(42, "Green Line", null));
    const neuGeladen = vi.fn();
    zeigeMit([], {}, neuGeladen);

    fireEvent.click(screen.getByRole("button", { name: "+ Neue Reihe" }));
    fireEvent.change(screen.getByLabelText("Name der neuen Reihe", { exact: true }), { target: { value: " Green Line " } });
    fireEvent.click(screen.getByRole("button", { name: /Reihe anlegen/ }));

    await within(screen.getByLabelText("Reihe im Katalog", { exact: true }))
      .findByRole("option", { name: "Green Line" });
    expect(anlegen).toHaveBeenCalledWith(expect.objectContaining({ name: "Green Line", subjectId: 1 }));
    expect((screen.getByLabelText("Reihe im Katalog", { exact: true }) as HTMLSelectElement).value).toBe("42");
    expect(neuGeladen).toHaveBeenCalledTimes(1);
  });
});
