import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { ExtraTrainingOverview, MixedReviewAnswer, MixedReviewOverview, MixedReviewResult, MixedReviewRunResponse } from "../lib/types";

vi.mock("./SohnApp", () => ({
  useSohn: () => ({ childId: 7, skin: DEFAULT_SKIN, refreshWallet: () => {}, celebrate: () => {} }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const extraTraining = vi.fn<(childId: number) => Promise<ExtraTrainingOverview>>();
const mixedReview = vi.fn<(childId: number) => Promise<MixedReviewOverview>>();
const startMixedReview = vi.fn<(childId: number) => Promise<MixedReviewRunResponse>>();
const submitMixedReview = vi.fn<(childId: number, runId: number, answers: MixedReviewAnswer[]) => Promise<MixedReviewResult>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      extraTraining: (childId: number) => extraTraining(childId),
      mixedReview: (childId: number) => mixedReview(childId),
      startMixedReview: (childId: number) => startMixedReview(childId),
      submitMixedReview: (childId: number, runId: number, answers: MixedReviewAnswer[]) => submitMixedReview(childId, runId, answers),
    },
  };
});

const { ApiError } = await import("../lib/api");
const { SohnBonus } = await import("./SohnBonus");
const { MischrundeEinstieg, Ergebnis, SohnMischrunde } = await import("./SohnMischrunde");

afterEach(() => { vi.clearAllMocks(); sessionStorage.clear(); });

const bonus = (unlocked: boolean): ExtraTrainingOverview => ({
  unlocked, lockReason: unlocked ? null : "duty_open", outstanding: unlocked ? [] : ["Wörter (Tagesziel) offen"],
  coinsToday: 0, dailyCoinCap: 90, exercises: [], rewardCadence: "Daily",
} as ExtraTrainingOverview);

const misch = (over: Partial<MixedReviewOverview>): MixedReviewOverview => ({
  unlocked: true, lockReason: null, outstanding: [], dueCards: 3, roundSize: 10, openRunId: null, coinsToday: 0,
  dailyCoinCap: 90, ...over,
});

const START = "Mischrunde starten";

/*
 * B-766 AK 1 – der Einstieg steht nur, wenn Karten fällig sind und die Sperre offen ist; gesperrt steht der Sperrgrund.
 */
describe("Mischrunde – Einstieg im Bonus-Training (B-766 AK 1)", () => {
  it("steht neben den Bonus-Übungen, wenn Karten fällig sind", async () => {
    extraTraining.mockResolvedValue(bonus(true));
    mixedReview.mockResolvedValue(misch({ dueCards: 3 }));
    render(<MemoryRouter><SohnBonus /></MemoryRouter>);
    expect(await screen.findByRole("button", { name: START })).toBeTruthy();
    expect(screen.getByText(/^3 fällige Karten aus all deinen Plänen – freiwillig, ohne Malus/)).toBeTruthy();
  });

  it("fehlt, wenn nichts fällig ist", async () => {
    extraTraining.mockResolvedValue(bonus(true));
    mixedReview.mockResolvedValue(misch({ dueCards: 0 }));
    render(<MemoryRouter><SohnBonus /></MemoryRouter>);
    await screen.findByRole("heading", { name: "Bonus-Training" });
    await act(async () => {});
    expect(screen.queryByRole("button", { name: START })).toBeNull();
  });

  // Sichert den Sperrgrund der Seite; dass der Einstieg selbst bei Sperre keinen Start anbietet, trägt der nächste Fall.
  it("zeigt bei gesperrtem Bonus-Training den Grund statt des Einstiegs", async () => {
    extraTraining.mockResolvedValue(bonus(false));
    mixedReview.mockResolvedValue(misch({ unlocked: false, lockReason: "duty_open", dueCards: 3 }));
    render(<MemoryRouter><SohnBonus /></MemoryRouter>);
    expect(await screen.findByText("Erst die Tagesziele – dann öffnet das Bonus-Training.")).toBeTruthy();
    await act(async () => {});
    expect(screen.queryByRole("button", { name: START })).toBeNull();
  });

  // Rot, wenn der Einstieg die Sperre nicht selbst liest: allein gerendert (ein künftiger Platz außerhalb des offenen
  // Zweigs) böte er sonst den Start an, den der Server mit extra_training_locked abwiese.
  it("bietet allein gerendert bei Sperre keinen Start an", async () => {
    mixedReview.mockResolvedValue(misch({ unlocked: false, lockReason: "duty_open", dueCards: 3 }));
    render(<MemoryRouter><MischrundeEinstieg /></MemoryRouter>);
    await act(async () => {});
    expect(screen.queryByRole("button", { name: START })).toBeNull();
  });
});

/* B-766 AK 2 – das Starten ist beim Senden gesperrt: zweimal tippen startet einen Lauf. */
describe("Mischrunde – Start beim Senden gesperrt (B-766 AK 2)", () => {
  it("schickt bei einem Doppeltipp einen Start und sperrt den Knopf", async () => {
    mixedReview.mockResolvedValue(misch({}));
    startMixedReview.mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><MischrundeEinstieg /></MemoryRouter>);
    const knopf = await screen.findByRole("button", { name: START });
    await act(async () => { fireEvent.click(knopf); fireEvent.click(knopf); });
    expect(startMixedReview).toHaveBeenCalledTimes(1);
    expect((knopf as HTMLButtonElement).disabled).toBe(true);
  });
});

/* B-766 AK 4 – ein vom Server abgewiesener Start zeigt die Meldung am Platz. */
describe("Mischrunde – abgewiesener Start (B-766 AK 4)", () => {
  it("nennt mixed_review_empty am Einstieg", async () => {
    mixedReview.mockResolvedValue(misch({}));
    startMixedReview.mockRejectedValue(new ApiError(409, "Technical server line.", "trace-9", "mixed_review_empty"));
    render(<MemoryRouter><MischrundeEinstieg /></MemoryRouter>);
    fireEvent.click(await screen.findByRole("button", { name: START }));
    expect(await screen.findByText("Gerade ist keine Karte fällig, die in eine Mischrunde darf.")).toBeTruthy();
  });
});

const ergebnis = (over: Partial<MixedReviewResult>): MixedReviewResult => ({
  runId: 1, total: 2, correct: 2, coinsAwarded: 6, coinsEarned: 6, capReached: false, coinsToday: 20, dailyCoinCap: 90,
  items: [
    { cardId: 1, positionId: 3, exerciseTitle: "Tiere", prompt: "Hund", expected: "dog", givenAnswer: "dog", wasCorrect: true,
      skipped: false, coinsAwarded: 3, box: 4, dueOn: "2026-11-30", gapIndex: null },
    { cardId: 2, positionId: 4, exerciseTitle: "Farben", prompt: null, expected: "", givenAnswer: null, wasCorrect: false,
      skipped: true, coinsAwarded: 0, box: null, dueOn: null, gapIndex: null },
  ],
  ...over,
} as MixedReviewResult);

/* B-766 AK 3 – das Ergebnis zeigt Münzen und Deckelstand; ohne Münzen steht der Grund. */
describe("Mischrunde – Ergebnis mit Deckelstand (B-766 AK 3)", () => {
  it("nennt die Münzen und den Tagesdeckel über beide Quellen", () => {
    render(<Ergebnis result={ergebnis({})} getippt={[1, 2]} onZurueck={() => {}} skin={DEFAULT_SKIN} />);
    expect(screen.getByText("+6 🪙 verdient")).toBeTruthy();
    expect(screen.getByText("Heute verdient: 20 / 90 🪙")).toBeTruthy();
    expect(screen.getByText("Farben: übersprungen – inzwischen geübt oder geändert, zählt nicht")).toBeTruthy();
  });

  it("nennt bei Deckel 0 ohne Münzen den Grund", () => {
    render(<Ergebnis result={ergebnis({ coinsAwarded: 0, coinsEarned: 6, capReached: true, coinsToday: 0, dailyCoinCap: 0 })}
      getippt={[1, 2]} onZurueck={() => {}} skin={DEFAULT_SKIN} />);
    expect(screen.getByText("Ohne Münzen: Bonus-Münzen gibt es gerade keine.")).toBeTruthy();
    expect(screen.getByText("Heute verdient: 0 / 0 🪙")).toBeTruthy();
  });

  it("nennt bei heute schon gesehenen Wörtern den Grund", () => {
    render(<Ergebnis result={ergebnis({ coinsAwarded: 0, coinsEarned: 0 })} getippt={[1, 2]} onZurueck={() => {}} skin={DEFAULT_SKIN} />);
    expect(screen.getByText("Ohne Münzen: diese Wörter hattest du heute schon gesehen – sie rücken trotzdem in ihrem Kasten vor.")).toBeTruthy();
  });

  // Rot, wenn das Ergebnis jeden Treffer ohne Münzen für „heute schon gesehen" hält: selbst eingeschätzte Karten zahlen nie.
  it("nennt bei nur selbst eingeschätzten Treffern deren eigenen Grund", () => {
    render(<Ergebnis result={ergebnis({ coinsAwarded: 0, coinsEarned: 0 })} getippt={[]} onZurueck={() => {}} skin={DEFAULT_SKIN} />);
    expect(screen.getByText("Ohne Münzen: selbst eingeschätzte Karten bringen keine – sie rücken aber in ihrem Kasten vor.")).toBeTruthy();
  });
});

const runde: MixedReviewRunResponse = {
  runId: 51, coinsLeftToday: 90,
  cards: [{ cardId: 9, positionId: 3, planTitle: "Englisch-Plan", subjectName: "Englisch", exerciseTitle: "Tiere", stage: 4, typed: true,
    card: { itemIndex: 0, prompt: "Hund", coinsPerHit: 3, answerLength: null } }],
} as unknown as MixedReviewRunResponse;

// Zeigt den Verlaufs-State der Seite, damit ein Fall sehen kann, ob die mitgegebene Runde verbraucht wurde.
function StateZeiger() {
  return <output data-testid="state">{JSON.stringify(useLocation().state)}</output>;
}

const spiele = (state: unknown) => render(
  <MemoryRouter initialEntries={[{ pathname: "/sohn/bonus/misch", state }]}>
    <Routes><Route path="/sohn/bonus/misch" element={<><SohnMischrunde /><StateZeiger /></>} /></Routes>
  </MemoryRouter>,
);

/* Review-Fund Sprint 131 (🟡 3): die Spielseite selbst. */
describe("Mischrunde – Spielseite", () => {
  // Rot, wenn die Seite die mitgegebene Runde ignoriert und noch einmal startet.
  it("spielt die vom Einstieg mitgegebene Runde ohne zweiten Start", async () => {
    spiele({ run: runde });
    expect(await screen.findByText("Karte 1 / 1")).toBeTruthy();
    expect(startMixedReview).not.toHaveBeenCalled();
    // Verbraucht: ein „Zurück" nach dem Ergebnis fände die abgegebene Runde sonst wieder im Verlauf (Review-Fund 🟡 1).
    expect(screen.getByTestId("state").textContent).toBe("null");
  });

  // Rot, wenn die Seite ohne mitgegebene Runde nicht selbst startet (Neuladen).
  it("startet ohne mitgegebene Runde selbst und setzt fort", async () => {
    startMixedReview.mockResolvedValue(runde);
    spiele(null);
    expect(await screen.findByText("Karte 1 / 1")).toBeTruthy();
    expect(startMixedReview).toHaveBeenCalledTimes(1);
  });

  // Rot, wenn die gespeicherten Antworten einer verworfenen Runde liegen bleiben.
  it("räumt die Antworten und nennt den Grund, wenn die Abgabe mixed_review_stale bekommt", async () => {
    const saved: MixedReviewAnswer[] = [{ cardId: 9, givenAnswer: "dog", wasKnown: null }];
    sessionStorage.setItem("pugling.mix.run.51", JSON.stringify(saved));
    submitMixedReview.mockRejectedValue(new ApiError(409, "Technical server line.", "trace-3", "mixed_review_stale"));
    spiele({ run: runde });
    expect((await screen.findByRole("alert")).textContent).toBe("Diese Mischrunde ist von gestern – starte eine neue.");
    expect(submitMixedReview).toHaveBeenCalledWith(7, 51, saved);
    expect(sessionStorage.getItem("pugling.mix.run.51")).toBeNull();
  });
});
