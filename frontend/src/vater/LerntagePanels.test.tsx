import { act, fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { afterEach, describe, expect, it, onTestFinished, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ChildPause, ChildResponse } from "../lib/types";
import { LeitplankenSection, PauseSection, leitplankenFehler } from "./ChildCarePanels";

/*
 * B-731: die Pausen und die Leitplanken des Tagesplaners im Vater-Web. Gerechnet wird am Server; hier steht, dass die
 * Maske einträgt, was der Vater tippt, und danach zeigt, was der Server zurückgibt (build + read-back, AK 8).
 */
const kind = (limits: Partial<ChildResponse> = {}): ChildResponse => ({
  id: 4, name: "Friedrich", birthYear: null, grade: 7, schoolType: "Gymnasium", gender: "Male", interests: [],
  profileNotes: null, allowedContentRating: "Everyone", createdAt: "2026-11-01T00:00:00Z", coins: 0, gems: 0,
  bonusDailyCoinCap: null, towerGameMinutesPerDay: null, homeworkCoins: null, homeworkPenaltyCoins: null,
  dailyTasksMin: null, dailyTasksMax: null, dailyNewWordsMin: null, dailyNewWordsMax: null, penaltyCoinsPerMissedTask: null,
  plannerMode: "Off", plannerSince: null,
  ...limits,
} as ChildResponse);

const gesetzt = { dailyTasksMin: 2, dailyTasksMax: 5, dailyNewWordsMin: 3, dailyNewWordsMax: 8, penaltyCoinsPerMissedTask: 4 };

describe("Leitplanken (B-731 AK 6/8)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn die Maske einen Boden 0, einen Boden über dem Deckel oder einen negativen Malus durchließe.
  it("prüft Boden, Deckel und Malus wie der Server", () => {
    expect(leitplankenFehler(gesetzt)).toBeNull();
    expect(leitplankenFehler({ ...gesetzt, dailyTasksMin: 0 })).toMatch(/mindestens/);
    expect(leitplankenFehler({ ...gesetzt, dailyNewWordsMin: 9 })).toMatch(/neuen Wörtern/);
    expect(leitplankenFehler({ ...gesetzt, dailyTasksMin: 6 })).toMatch(/Pflichtaufgaben/);
    expect(leitplankenFehler({ ...gesetzt, penaltyCoinsPerMissedTask: -1 })).toMatch(/0 bis 1000/);
  });

  // Rot, wenn die Maske ein anderes Feld schickt als getippt oder nach dem Speichern nicht neu lädt.
  it("schickt die fünf Zahlen und lädt danach den Serverstand", async () => {
    const speichern = vi.spyOn(api, "updateChild").mockResolvedValue(kind(gesetzt));
    const neuLaden = vi.fn();
    render(<LeitplankenSection child={kind()} onSaved={neuLaden} />);

    const felder: [string, string][] = [
      ["Pflichtaufgaben am Tag – mindestens", "2"], ["Pflichtaufgaben am Tag – höchstens", "5"],
      ["Neue Wörter am Tag – mindestens", "3"], ["Neue Wörter am Tag – höchstens", "8"],
      ["Malus je verpasster Pflichtaufgabe (🪙)", "4"],
    ];
    for (const [name, wert] of felder) fireEvent.change(screen.getByLabelText(name, { exact: true }), { target: { value: wert } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));

    await vi.waitFor(() => expect(neuLaden).toHaveBeenCalled());
    expect(speichern).toHaveBeenCalledWith(4, gesetzt);
  });

  // B-1020: nach dem Speichern lädt der Aufrufer das Kind neu und reicht den Serverstand herein – hier ein anderer Deckel
  // (6) als getippt (5), etwa weil der Server zwischendurch von einem zweiten Gerät geändert wurde. Rot, wenn die Felder
  // den neuen Stand nicht nachziehen (die Synchronisation fehlt: „5“ bliebe stehen) oder die Bestätigung dabei verschwindet.
  // Dass der Kind-Hub die Maske dabei nicht neu einhängt (`key` nur am Kind), hält `e2e/lerntage-und-pause.spec.ts`.
  it("zieht nach dem Speichern den Serverstand nach und behält die Bestätigung", async () => {
    const server = { ...gesetzt, dailyTasksMax: 6 };
    vi.spyOn(api, "updateChild").mockResolvedValue(kind(server));
    function Hub() {
      const [child, setChild] = useState(kind());
      return <LeitplankenSection key={child.id} child={child} onSaved={() => setChild(kind(server))} />;
    }
    render(<Hub />);

    const felder: [string, string][] = [
      ["Pflichtaufgaben am Tag – mindestens", "2"], ["Pflichtaufgaben am Tag – höchstens", "5"],
      ["Neue Wörter am Tag – mindestens", "3"], ["Neue Wörter am Tag – höchstens", "8"],
      ["Malus je verpasster Pflichtaufgabe (🪙)", "4"],
    ];
    for (const [name, wert] of felder) fireEvent.change(screen.getByLabelText(name, { exact: true }), { target: { value: wert } });
    fireEvent.click(screen.getByRole("button", { name: "Speichern" }));
    await act(async () => {});

    expect(screen.getByText("Leitplanken gespeichert.")).toBeTruthy();
    expect((screen.getByLabelText("Pflichtaufgaben am Tag – höchstens", { exact: true }) as HTMLInputElement).value).toBe("6");
    expect((screen.getByLabelText("Neue Wörter am Tag – höchstens", { exact: true }) as HTMLInputElement).value).toBe("8");
  });

  // Rot, wenn „Leitplanken entfernen“ ohne Rückfrage schickt: ein Fehlgriff löschte alle fünf Zahlen.
  it("fragt vor dem Entfernen nach und schickt bei „Abbrechen“ nichts", () => {
    const entfernen = vi.spyOn(api, "updateChild").mockResolvedValue(kind());
    const frage = vi.fn(() => false);
    vi.stubGlobal("confirm", frage);
    onTestFinished(() => { vi.unstubAllGlobals(); });
    render(<LeitplankenSection child={kind(gesetzt)} onSaved={() => {}} />);

    fireEvent.click(screen.getByRole("button", { name: "Leitplanken entfernen" }));

    expect(frage).toHaveBeenCalled();
    expect(entfernen).not.toHaveBeenCalled();
  });

  // Eine Erklärung für die fünf Felder, nicht fünf gleiche ⓘ. Rot, wenn jedes Feld wieder sein eigenes `FieldLabel` trägt.
  it("trägt genau einen Hinweis-Knopf über den fünf Feldern", () => {
    render(<LeitplankenSection child={kind()} onSaved={() => {}} />);
    expect(screen.getAllByRole("button", { name: /^Erklärung zu/ })).toHaveLength(1);
  });

  // Rot, wenn die Gruppen-id wieder an der ganzen Zeile sitzt: der Gruppenname schlüge dann den ⓘ-Namen mit ein.
  it("nennt die Gruppe nur „Leitplanken“", () => {
    render(<LeitplankenSection child={kind()} onSaved={() => {}} />);
    expect(screen.getByRole("group", { name: "Leitplanken" })).toBeTruthy();
  });

  // AK 8: Rot, wenn die Maske ihren Startwert nicht vom Server liest (leere Felder trotz gespeicherter Leitplanken).
  it("zeigt den gelesenen Stand", () => {
    render(<LeitplankenSection child={kind(gesetzt)} onSaved={() => {}} />);
    expect((screen.getByLabelText("Neue Wörter am Tag – höchstens", { exact: true }) as HTMLInputElement).value).toBe("8");
    expect(screen.getByRole("button", { name: "Leitplanken entfernen" })).toBeTruthy();
  });
});

describe("Pausen (B-731 AK 8)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn die Maske nach dem Eintragen nicht vom Server neu liest (die Pause erschiene nicht in der Liste).
  it("trägt eine Pause ein und zeigt danach die Liste des Servers", async () => {
    const pause: ChildPause = { id: 9, from: "2026-11-27", until: "2026-11-28", note: "krank", createdAt: "2026-11-30T10:00:00Z" };
    const liste = vi.spyOn(api, "childPauses").mockResolvedValueOnce([]).mockResolvedValue([pause]);
    const eintragen = vi.spyOn(api, "addChildPause").mockResolvedValue(pause);
    render(<AuthProvider><PauseSection childId={4} childName="Friedrich" /></AuthProvider>);

    await screen.findByText("Keine Pause eingetragen.");
    fireEvent.change(screen.getByLabelText("Von"), { target: { value: "2026-11-27" } });
    fireEvent.change(screen.getByLabelText("Bis (einschließlich)"), { target: { value: "2026-11-28" } });
    fireEvent.change(screen.getByLabelText("Notiz", { exact: false }), { target: { value: "krank" } });
    fireEvent.click(screen.getByRole("button", { name: "Pause eintragen" }));

    await screen.findByText("krank");
    expect(eintragen).toHaveBeenCalledWith(4, { from: "2026-11-27", until: "2026-11-28", note: "krank" });
    expect(liste).toHaveBeenCalledTimes(2);
  });

  // Rot, wenn die Maske „Bis vor Von“ schicken ließe.
  it("sperrt eine Pause, die vor ihrem Beginn endet", async () => {
    vi.spyOn(api, "childPauses").mockResolvedValue([]);
    render(<AuthProvider><PauseSection childId={4} childName="Friedrich" /></AuthProvider>);
    await screen.findByText("Keine Pause eingetragen.");

    fireEvent.change(screen.getByLabelText("Von"), { target: { value: "2026-11-28" } });
    fireEvent.change(screen.getByLabelText("Bis (einschließlich)"), { target: { value: "2026-11-27" } });

    expect((screen.getByRole("button", { name: "Pause eintragen" }) as HTMLButtonElement).disabled).toBe(true);
  });
});
