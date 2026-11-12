import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { Paged } from "../lib/uiTypes";
import type { WordMastery } from "../lib/types";
import { WeakWords } from "./VaterLernstand";

/*
 * B-467: „nur schwache" ist die Vorgabe. Eine leere Liste darunter heißt zweierlei – „kein Wort ist schwach" oder „es gibt
 * noch gar kein Wort". Nur das erste ist ein Erfolg. Gefälscht wird der Abruf, nicht das `fetch` (Vorbild
 * `TextbookForm.test.tsx`).
 */
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
