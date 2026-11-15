import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { Paged } from "../lib/uiTypes";
import type { ChildResponse, PlanResponse, SubjectProgress, WordMastery } from "../lib/types";
import { VaterLernstand, WeakWords } from "./VaterLernstand";

/*
 * B-467: „nur schwache" ist die Vorgabe. Eine leere Liste darunter heißt zweierlei – „kein Wort ist schwach" oder „es gibt
 * noch gar kein Wort". Nur das erste ist ein Erfolg. Gefälscht wird der Abruf, nicht das `fetch` (Vorbild
 * `TextbookForm.test.tsx`).
 */
// B-584: unter einer Liste mit Wörtern fragt der Zuweisen-Knopf die Pläne ab – ohne Fälschung ginge das ans Netz.
beforeEach(() => { vi.spyOn(api, "plans").mockResolvedValue([]); });

// `AuthProvider` ist Pflicht: `useAsync` hängt für den 401-Fall an `useAuth`.
const show = () => render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
const page = (total: number): Paged<WordMastery> => ({ items: [], total });

describe("Lernstand – leere Liste der schwachen Wörter", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn die leere Liste immer als Erfolg gelesen wird: bei einem Kind ohne eine einzige Antwort stünde dann
  // „es sitzt alles. 👍".
  it("meldet bei einem Kind ohne Lernstand keinen Erfolg", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue(page(0));
    show();

    // Der Satz nennt, was gemessen ist (Antworten) – eingeführte, nie beantwortete Wörter zählen hier nicht.
    expect(await screen.findByText(/Noch kein Lernstand/)).toBeTruthy();
    expect(screen.queryByText(/sitzt alles/)).toBeNull();
  });

  // Rot, wenn der Erfolg ganz wegfällt: hat das Kind Wörter und keins ist schwach, ist „es sitzt alles" wahr.
  it("meldet den Erfolg, wenn es Wörter gibt und keins schwach ist", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => page(p.onlyWeak ? 0 : 3));
    show();

    expect(await screen.findByText(/sitzt alles/)).toBeTruthy();
  });
});

/*
 * B-499: was die zweite Abfrage („gibt es überhaupt ein Wort?") zeigen darf, solange sie nicht stimmt oder fehlt.
 */
describe("Lernstand – die zweite Abfrage", () => {
  afterEach(() => vi.restoreAllMocks());

  // Gegenprobe zum Leerfall: der Satz nennt die gemessene Größe, wie der Server sie zählt (`countsForMastery`) – eine
  // Antwort auf einer Selbsteinschätzungs-Stufe ist eine Antwort, aber keine gewertete. Rot, wenn er „beantwortet" sagt.
  it("nennt im Leerfall gewertete Antworten, nicht beantwortete Fragen", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue(page(0));
    show();

    const text = (await screen.findByText(/Noch kein Lernstand/)).textContent ?? "";
    expect(text).toMatch(/gewertet/);
    expect(text).not.toMatch(/beantwortet/);
  });

  // Rot, wenn „Erneut laden" nur die Liste neu holt: scheiterte die zweite Abfrage beim Öffnen, bliebe für ein Kind ohne
  // Lernstand dauerhaft der neutrale Satz stehen.
  it("wird mit „Erneut laden“ ebenfalls neu geholt", async () => {
    let fail = true;
    vi.spyOn(api, "childWordMastery").mockImplementation(async () => {
      if (fail) throw new Error("Verbindung weg");
      return page(0);
    });
    show();
    const retry = await screen.findByRole("button", { name: "Erneut laden" });

    fail = false;
    fireEvent.click(retry);

    expect(await screen.findByText(/Noch kein Lernstand/)).toBeTruthy();
  });

  // Rot, wenn die zweite Abfrage nach einem Kinderwechsel mit der Zahl des vorigen Kindes antwortet: dann stünde für das
  // neue Kind kurz „es sitzt alles", obwohl für es noch nichts gemessen ist.
  it("spricht nach einem Kinderwechsel nicht mit der Zahl des vorigen Kindes", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation((id, p = {}) =>
      id === 1 ? Promise.resolve(page(p.onlyWeak ? 0 : 3))
        : p.onlyWeak ? Promise.resolve(page(0)) : new Promise<Paged<WordMastery>>(() => {}));
    const { rerender } = render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
    expect(await screen.findByText(/sitzt alles/)).toBeTruthy();

    rerender(<AuthProvider><WeakWords childId={2} /></AuthProvider>);

    expect(await screen.findByText("Kein Wort unter 50 %.")).toBeTruthy();
    expect(screen.queryByText(/sitzt alles/)).toBeNull();
  });
});

// B-502: ohne „nur schwache" steht der Leerfall allein auf der Liste. Rot, wenn er nach einem Kinderwechsel aus der
// (leeren) Liste des vorigen Kindes kommt, solange die des neuen noch lädt.
describe("Lernstand – Wortliste nach einem Kinderwechsel", () => {
  afterEach(() => vi.restoreAllMocks());

  it("sagt für das neue Kind nichts aus dem Stand des vorigen", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation((id) =>
      id === 1 ? Promise.resolve(page(0)) : new Promise<Paged<WordMastery>>(() => {}));
    const { rerender } = render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
    fireEvent.click(screen.getByLabelText(/nur schwache/));
    expect(await screen.findByText(/Noch kein Lernstand/)).toBeTruthy();

    rerender(<AuthProvider><WeakWords childId={2} /></AuthProvider>);

    expect(screen.queryByText(/Noch kein Lernstand/)).toBeNull();
  });
});

/*
 * B-504: was nach einem Kinderwechsel und bei einer gescheiterten Zählung im Leerfall stehen darf.
 */
describe("Lernstand – Seite und Zählung (B-504)", () => {
  afterEach(() => vi.restoreAllMocks());
  const word = (id: number): WordMastery => ({
    vocabularyId: id, word: `w${id}`, translation: `t${id}`, avgMasteryPercent: 10, minBox: 1, itemCount: 1,
  } as WordMastery);

  // AK 1. Rot, wenn `skip` den Kinderwechsel überlebt (Kind 2 würde dann ab Wort 26 abgefragt) – und, Entscheidung 1,
  // wenn der Wechsel den Filter zurücksetzt (`key={childId}` oder ein `setOnlyWeak(true)`): hier ist er vorher abgewählt.
  it("beginnt nach einem Kinderwechsel auf Seite 1 und behält den Filter", async () => {
    const spy = vi.spyOn(api, "childWordMastery").mockImplementation(async (id) =>
      id === 1 ? { items: [word(1)], total: 30 } : { items: [word(2)], total: 3 });
    const { rerender } = render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
    fireEvent.click(screen.getByLabelText(/nur schwache/));
    fireEvent.click(await screen.findByRole("button", { name: "Weiter ›" }));
    await screen.findByText("26–30 von 30");

    rerender(<AuthProvider><WeakWords childId={2} /></AuthProvider>);

    expect(await screen.findByText("w2")).toBeTruthy();
    // Die Listenabfrage (nicht die Zählung mit `take: 1`) für Kind 2: Seite 1, ohne Filter.
    const child2List = spy.mock.calls.filter(([id, p]) => id === 2 && p?.take !== 1).map(([, p]) => [p?.onlyWeak, p?.skip]);
    expect(child2List).toEqual([[false, 0]]);
    expect((screen.getByLabelText(/nur schwache/) as HTMLInputElement).checked).toBe(false);
  });

  // AK 2. Rot, wenn der Leerfall an der Länge der Seite hängt: eine leere Seite jenseits von drei schwachen Wörtern
  // sagte dann „es sitzt alles".
  it("meldet keinen Erfolg für eine leere Seite jenseits des Bestands", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [], total: 3 });
    show();

    expect(await screen.findByText("Wörter (3)")).toBeTruthy();
    expect(screen.queryByText(/sitzt alles/)).toBeNull();
    expect(screen.queryByText(/Kein Wort unter 50/)).toBeNull();
  });

  // AK 3. Rot, wenn eine gescheiterte Zählung still bleibt: dann stünde dauerhaft der neutrale Satz ohne Weg zurück.
  it("zeigt eine gescheiterte Zählung mit „Erneut laden“, das sie neu holt", async () => {
    let fail = true;
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => {
      if (p.onlyWeak) return page(0);
      if (fail) throw new Error("Verbindung weg");
      return page(3);
    });
    show();
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toMatch(/Die Angaben zum Lernstand konnten nicht geladen werden/);

    fail = false;
    fireEvent.click(screen.getByRole("button", { name: "Erneut laden" }));

    expect(await screen.findByText(/sitzt alles/)).toBeTruthy();
  });

  // AK 3, zweiter Fall. Rot, wenn der Kasten der Zählung auch neben dem der Liste steht.
  it("zeigt bei einer gescheiterten Liste nur einen Kasten", async () => {
    vi.spyOn(api, "childWordMastery").mockRejectedValue(new Error("Verbindung weg"));
    show();
    await screen.findByText(/Die Vokabeln konnten nicht geladen werden/);

    expect(screen.getAllByRole("alert")).toHaveLength(1);
  });
});

/*
 * B-509: ein anderes Kind ist eine andere Liste – nichts aus der Liste des vorigen steht unter dem Namen des neuen.
 * Innerhalb eines Kindes wird gesperrt, nicht ausgeblendet.
 */
describe("Lernstand – nichts vom vorigen Kind, nichts vom vorigen Fenster (B-509)", () => {
  afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); });
  const word = (id: number): WordMastery => ({
    vocabularyId: id, word: `w${id}`, translation: `t${id}`, avgMasteryPercent: 10, minBox: 1, itemCount: 1,
    seenCount: 1, correctCount: 0,
  } as WordMastery);
  const thirty = { items: Array.from({ length: 25 }, (_, i) => word(i + 1)), total: 30 };

  // AK 1. Rot, wenn Zeilen, Kopfzahl oder Pager nach dem Wechsel aus `words.data` des vorigen Kindes lesen.
  it("zeigt nach dem Wechsel auf ein Kind, dessen Liste scheitert, keine Zeile, Zahl und Seite des vorigen", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (id) => {
      if (id === 1) return thirty;
      throw new Error("Verbindung weg");
    });
    const { rerender } = render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
    await screen.findByText("1–25 von 30");

    rerender(<AuthProvider><WeakWords childId={2} /></AuthProvider>);
    await screen.findByText(/Die Vokabeln konnten nicht geladen werden/);

    expect(screen.queryByText("w1")).toBeNull();
    expect(screen.queryByText("Wörter (30)")).toBeNull();
    expect(screen.queryByText("1–25 von 30")).toBeNull();
  });

  // AK 3. Rot, wenn `openWord` den Wechsel überlebt: dieselbe Vokabel stünde beim neuen Kind ungefragt aufgeklappt.
  it("klappt ein offenes Wort beim Kinderwechsel zu", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [word(7)], total: 1 });
    vi.spyOn(api, "childItemProgress").mockResolvedValue({ items: [], total: 0 });
    const { rerender } = render(<AuthProvider><WeakWords childId={1} /></AuthProvider>);
    fireEvent.click(await screen.findByRole("button", { name: "Einzelheiten" }));
    expect(screen.getByRole("button", { name: "Schließen" })).toBeTruthy();

    rerender(<AuthProvider><WeakWords childId={2} /></AuthProvider>);

    expect(await screen.findByRole("button", { name: "Einzelheiten" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Schließen" })).toBeNull();
  });

  // AK 4, Pager. Rot, wenn der Pager nach einem gescheiterten Seitenwechsel bedienbar bleibt (`busy={words.loading}`):
  // „Weiter ›" blätterte dann von einem Fenster aus, das nicht stimmt.
  it("sperrt den Pager, solange die Zeilen nicht zur eingestellten Seite gehören", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => {
      if ((p.skip ?? 0) > 0) throw new Error("Verbindung weg");
      return thirty;
    });
    show();
    fireEvent.click(await screen.findByRole("button", { name: "Weiter ›" }));
    await screen.findByText(/Die Vokabeln konnten nicht geladen werden/);

    expect((screen.getByRole("button", { name: "Weiter ›" }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole("button", { name: "‹ Zurück" }) as HTMLButtonElement).disabled).toBe(true);
  });

  // AK 4, Kopfzahl. Rot, wenn die Kopfzahl nach einem gescheiterten Filterwechsel die Zahl des vorigen Filters nennt.
  it("nennt nach einem gescheiterten Filterwechsel nicht die Zahl des vorigen Filters", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => {
      if (p.take === 1) return page(30);
      if (p.onlyWeak) return thirty;
      throw new Error("Verbindung weg");
    });
    show();
    await screen.findByText("Wörter (30)");

    fireEvent.click(screen.getByLabelText(/nur schwache/));
    await screen.findByText(/Die Vokabeln konnten nicht geladen werden/);

    expect(screen.queryByText("Wörter (30)")).toBeNull();
  });

  // AK 5. Rot, wenn der Kasten der Zählung auch ohne „nur schwache" erscheint, wo der Leerfall die Zählung nicht braucht.
  it("zeigt ohne „nur schwache“ keinen Kasten der Zählung", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => {
      if (p.take === 1) throw new Error("Verbindung weg");
      return page(0);
    });
    show();
    fireEvent.click(screen.getByLabelText(/nur schwache/));
    await screen.findByText(/Noch kein Lernstand/);

    expect(screen.queryByText(/Die Angaben zum Lernstand konnten nicht geladen werden/)).toBeNull();
  });
});

/*
 * B-509 (Review): auch die Kopfzeile und der Katalog sprechen nach einem Kinderwechsel nicht vom vorigen Kind.
 */
describe("Lernstand – Kopfzeile und Katalog nach einem Kinderwechsel (B-509)", () => {
  afterEach(() => vi.restoreAllMocks());
  // Gleiche Komponente, neues Kind: per Router-Wechsel, nicht per neuem Mount (sonst wäre `stale` nie wahr).
  const page2 = () => render(<AuthProvider><MemoryRouter initialEntries={["/vater/kind/1/lernstand"]}>
    <Routes><Route path="/vater/kind/:childId/lernstand" element={<VaterLernstand />} /></Routes>
    <Nav to="/vater/kind/2/lernstand" /></MemoryRouter></AuthProvider>);

  // Rot, wenn der Name nicht an `stale` hängt: über der Liste von Kind 2 stünde „Lernstand · Anna", solange Kind 2 lädt
  // – und dauerhaft, wenn sein Abruf scheitert. Rot auch, wenn der Fehler still bleibt.
  it("nennt nicht den Namen des vorigen Kindes und zeigt einen gescheiterten Abruf", async () => {
    vi.spyOn(api, "child").mockImplementation(async (id) => {
      if (id === 1) return { id: 1, name: "Anna" } as ChildResponse;
      throw new Error("Verbindung weg");
    });
    vi.spyOn(api, "childWordMastery").mockResolvedValue(page(0));
    page2();
    await screen.findByText("Lernstand · Anna");
    fireEvent.click(screen.getByRole("button", { name: "wechseln" }));

    expect(await screen.findByText(/Die Angaben zum Kind konnten nicht geladen werden/)).toBeTruthy();
    expect(screen.queryByText(/Anna/)).toBeNull();
  });

  // Rot ohne `key={childId}` am Katalog: das offene Fach überlebte den Wechsel, und seine Units würden für Kind 2 mit der
  // Fach-Id aus der Liste von Kind 1 abgefragt.
  it("schließt das offene Fach beim Kinderwechsel", async () => {
    vi.spyOn(api, "child").mockImplementation(async (id) => ({ id, name: `Kind ${id}` } as ChildResponse));
    vi.spyOn(api, "childLearnSubjects").mockImplementation(async () =>
      [{ subjectId: 5, name: "Englisch", active: true, seriesUnitCount: 1, exerciseCount: 1, progress: {} } as unknown as SubjectProgress]);
    const units = vi.spyOn(api, "childLearnSeriesUnits").mockResolvedValue([]);
    vi.spyOn(api, "childWordMastery").mockResolvedValue(page(0));
    page2();
    fireEvent.click(screen.getByRole("radio", { name: /Nach Katalog/ }));
    fireEvent.click(await screen.findByRole("button", { name: "Units" }));
    await screen.findByRole("button", { name: "Schließen" });

    fireEvent.click(screen.getByRole("button", { name: "wechseln" }));
    await screen.findByText("Lernstand · Kind 2");

    expect(await screen.findByRole("button", { name: "Units" })).toBeTruthy();
    expect(units.mock.calls.some(([childId]) => childId === 2)).toBe(false);
  });
});

/** Ein Knopf, der im selben Router auf ein anderes Kind wechselt – so bleibt `VaterLernstand` gemountet. */
function Nav({ to }: { to: string }) {
  const navigate = useNavigate();
  return <button type="button" onClick={() => navigate(to)}>wechseln</button>;
}

/*
 * B-584 AK 8: die schwachen Wörter der Seite mit einem Griff als tägliche Wiederholung. Gefälscht werden die Abrufe.
 */
describe("Lernstand – schwache Wörter als Wiederholung zuweisen (B-584)", () => {
  afterEach(() => vi.restoreAllMocks());

  const wort = (vocabularyId: number, word: string, sourceLanguage = "en") =>
    ({ vocabularyId, word, translation: "x", masteryPercent: 20, minBox: 1, correctCount: 0, seenCount: 2,
      sourceLanguage, targetLanguage: "de" }) as unknown as WordMastery;
  const plan = (id: number, isPlayable: boolean) => ({ id, title: `Plan ${id}`, isPlayable }) as unknown as PlanResponse;
  const zeige = () => render(<AuthProvider><MemoryRouter><WeakWords childId={1} /></MemoryRouter></AuthProvider>);

  // Rot, wenn der Knopf ohne spielbaren Plan freigegeben ist (dann führte er in einen Server-Fehler), oder wenn er den
  // Grund nicht nennt.
  it("sperrt den Knopf ohne spielbaren Plan und nennt den Grund", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [wort(7, "dog"), wort(8, "cat")], total: 2 });
    vi.spyOn(api, "plans").mockResolvedValue([plan(3, false)]);
    zeige();

    const knopf = await screen.findByRole("button", { name: "Diese 2 Wörter als tägliche Wiederholung zuweisen" });
    expect(await screen.findByText("Gesperrt: das Kind hat heute keinen laufenden Plan.")).toBeTruthy();
    expect((knopf as HTMLButtonElement).disabled).toBe(true);
  });

  // Rot, wenn der Knopf auch ohne „nur schwache“ erscheint (dann bündelte er sichere Wörter), oder wenn er die Wörter
  // der Seite nicht an den spielbaren Plan schickt.
  it("schickt die Wörter der Seite an den spielbaren Plan – und fehlt ohne „nur schwache“", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [wort(7, "dog"), wort(8, "cat")], total: 2 });
    vi.spyOn(api, "plans").mockResolvedValue([plan(3, false), plan(4, true)]);
    const zuweisen = vi.spyOn(api, "addReviewFromWords").mockResolvedValue({ id: 99 } as never);
    zeige();

    const knopf = await screen.findByRole("button", { name: "Diese 2 Wörter als tägliche Wiederholung zuweisen" });
    await vi.waitFor(() => expect((knopf as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(knopf);
    await screen.findByText("2 Wörter sind jetzt eine tägliche Wiederholung.");
    expect(zuweisen).toHaveBeenCalledWith(4, { vocabularyIds: [7, 8] });
    expect(screen.getByRole("link", { name: /Zum Plan/ }).getAttribute("href")).toBe("/vater/plan/4");
  });

  // Review-Fund (die frühere Zeile prüfte im Ladefenster und trug nichts). Rot, wenn der Knopf auch ohne „nur schwache“
  // erscheint: dann bündelte er Wörter, die sitzen.
  it("fehlt ohne „nur schwache“, auch nachdem die Liste geladen ist", async () => {
    const abruf = vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [wort(7, "dog")], total: 1 });
    vi.spyOn(api, "plans").mockResolvedValue([plan(4, true)]);
    zeige();
    await screen.findByRole("button", { name: "Diese 1 Wort als tägliche Wiederholung zuweisen" });

    fireEvent.click(screen.getByRole("checkbox", { name: /nur schwache/ }));
    await vi.waitFor(() => expect(abruf).toHaveBeenCalledWith(1, expect.objectContaining({ onlyWeak: false })));
    await act(async () => {});
    expect(await screen.findByText("dog")).toBeTruthy();
    expect(screen.queryByRole("button", { name: /als tägliche Wiederholung/ })).toBeNull();
  });

  // B-584 Entscheidung 4: bei mehreren spielbaren Plänen wählt der Vater. Rot, wenn die Auswahl fehlt oder die Wahl nicht
  // beim Server ankommt (dann ginge es immer in den ersten Plan).
  it("lässt bei mehreren spielbaren Plänen wählen und schickt an den gewählten", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({ items: [wort(7, "dog")], total: 1 });
    vi.spyOn(api, "plans").mockResolvedValue([plan(4, true), plan(5, true)]);
    const zuweisen = vi.spyOn(api, "addReviewFromWords").mockResolvedValue({ id: 99 } as never);
    zeige();

    fireEvent.change(await screen.findByRole("combobox", { name: "Plan für die Wiederholung" }), { target: { value: "5" } });
    fireEvent.click(screen.getByRole("button", { name: "Diese 1 Wort als tägliche Wiederholung zuweisen" }));
    await screen.findByText("1 Wort ist jetzt eine tägliche Wiederholung.");
    expect(zuweisen).toHaveBeenCalledWith(5, { vocabularyIds: [7] });
  });

  // Review-Blocker: lernt das Kind zwei Sprachen, nimmt eine Wiederholung nur ein Paar. Rot, wenn die Seite alle Wörter in
  // einen Knopf packt (dann lehnte der Server ab).
  it("bietet je Sprachpaar einen eigenen Knopf", async () => {
    vi.spyOn(api, "childWordMastery").mockResolvedValue({
      items: [wort(7, "dog", "en"), wort(8, "la gare", "fr"), wort(9, "cat", "en")], total: 3,
    });
    vi.spyOn(api, "plans").mockResolvedValue([plan(4, true)]);
    const zuweisen = vi.spyOn(api, "addReviewFromWords").mockResolvedValue({ id: 99 } as never);
    zeige();

    const englisch = await screen.findByRole("button", { name: "Diese 2 Wörter (en → de) als tägliche Wiederholung zuweisen" });
    expect(screen.getByRole("button", { name: "Diese 1 Wort (fr → de) als tägliche Wiederholung zuweisen" })).toBeTruthy();
    await vi.waitFor(() => expect((englisch as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(englisch);
    await vi.waitFor(() => expect(zuweisen).toHaveBeenCalledWith(4, { vocabularyIds: [7, 9] }));
  });
});
