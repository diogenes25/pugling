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
