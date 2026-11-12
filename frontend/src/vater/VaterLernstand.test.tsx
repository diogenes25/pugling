import { render, screen } from "@testing-library/react";
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
    expect(await screen.findByText(/Noch kein Lernstand: das Kind hat noch keine Frage beantwortet/)).toBeTruthy();
    expect(screen.queryByText(/sitzt alles/)).toBeNull();
  });

  // Rot, wenn der Erfolg ganz wegfällt: hat das Kind Wörter und keins ist schwach, ist „es sitzt alles" wahr.
  it("meldet den Erfolg, wenn es Wörter gibt und keins schwach ist", async () => {
    vi.spyOn(api, "childWordMastery").mockImplementation(async (_id, p = {}) => page(p.onlyWeak ? 0 : 3));
    show();

    expect(await screen.findByText(/sitzt alles/)).toBeTruthy();
  });
});
