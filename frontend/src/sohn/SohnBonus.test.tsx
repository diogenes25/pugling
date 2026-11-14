import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { ExtraAnswer, ExtraExercise, ExtraRunResponse, ExtraRunResult, ExtraTrainingOverview } from "../lib/types";

vi.mock("./SohnApp", () => ({
  useSohn: () => ({ childId: 7, skin: DEFAULT_SKIN, refreshWallet: () => {}, celebrate: () => {} }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));

const extraTraining = vi.fn<(childId: number) => Promise<ExtraTrainingOverview>>();
const startExtraRun = vi.fn<(childId: number, exerciseId: number) => Promise<ExtraRunResponse>>();
const submitExtraRun = vi.fn<(childId: number, runId: number, answers: ExtraAnswer[]) => Promise<ExtraRunResult>>();
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      ...original.api,
      extraTraining: (childId: number) => extraTraining(childId),
      startExtraRun: (childId: number, exerciseId: number) => startExtraRun(childId, exerciseId),
      submitExtraRun: (childId: number, runId: number, answers: ExtraAnswer[]) => submitExtraRun(childId, runId, answers),
    },
  };
});

const { ApiError } = await import("../lib/api");
const { SohnBonus, SohnBonusRun } = await import("./SohnBonus");

afterEach(() => sessionStorage.clear());

const uebung = (over: Partial<ExtraExercise>): ExtraExercise => ({
  exerciseId: 1, title: "Tiere", type: "Vocabulary", unitLabel: "Unit 1", itemCount: 4, repeatable: true,
  completedRuns: 0, done: false, nextForm: "Typed", coinsPerHit: 2, openRunId: null, ...over,
});

const zeige = async (coinsToday: number, dailyCoinCap: number | null) => {
  extraTraining.mockResolvedValue({
    unlocked: true, lockReason: null, outstanding: [], coinsToday, dailyCoinCap,
    exercises: [
      uebung({ exerciseId: 1, title: "Tiere" }),
      uebung({ exerciseId: 2, title: "Rechnen", type: "Arithmetic", repeatable: false }),
      uebung({ exerciseId: 3, title: "Lücken", type: "Cloze", repeatable: false, openRunId: 9 }),
    ],
  } as ExtraTrainingOverview);
  render(<MemoryRouter><SohnBonus /></MemoryRouter>);
  await screen.findByText("Tiere");
};

/*
 * B-543 – bei vollem Tagesdeckel verbrennt eine Einmal-Übung nicht für 0 Münzen: der Server weist ihren Start ab, die
 * Zeile bietet ihn darum gar nicht erst an und sagt, wann sie wieder geht.
 */
describe("Bonus-Übersicht am Tagesdeckel", () => {
  it("bietet bei vollem Deckel nur noch Vokabeln an und sagt es den Einmal-Übungen ehrlich", async () => {
    // Rot, wenn `BonusZeile` den Link ohne Blick auf `capReached`/`repeatable` rendert (dann hätten „Rechnen“ und
    // „Lücken“ einen Link), wenn der Satz an der Vokabelzeile stünde, oder wenn die Deckel-Karte den alten Text trägt.
    await zeige(30, 30);

    expect(screen.getByRole("link", { name: "Spielen: Tiere" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Rechnen/ })).toBeNull();
    expect(screen.queryByRole("link", { name: /Lücken/ })).toBeNull();
    expect(screen.getAllByText("Zahlt nur einmal – morgen wieder")).toHaveLength(2);
    expect(screen.getByText(
      "Tagesdeckel erreicht – Vokabeln üben darfst du weiter, ohne Münzen. Übungen, die nur einmal zahlen, warten bis morgen.",
    )).toBeTruthy();
  });

  it("verspricht bei einem Deckel von 0 kein „morgen“", async () => {
    // Rot, wenn Zeile oder Karte bei Deckel 0 „morgen“ zusagen – morgen zahlt die Übung genauso wenig – oder wenn die
    // Einmal-Übungen bei 0 doch einen Link bekommen.
    await zeige(0, 0);

    expect(screen.getByRole("link", { name: "Spielen: Tiere" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Rechnen/ })).toBeNull();
    expect(screen.getAllByText("Zahlt nur einmal – gerade gibt es keine Bonus-Münzen")).toHaveLength(2);
    expect(screen.queryByText(/morgen/)).toBeNull();
  });

  it("lässt Einmal-Übungen unter dem Deckel spielen", async () => {
    // Rot, wenn die Zeile den Start schon ohne vollen Deckel wegnimmt (etwa nur nach `repeatable` entscheidet).
    await zeige(29, 30);

    expect(screen.getByRole("link", { name: "Spielen: Rechnen" })).toBeTruthy();
    expect(screen.getByRole("link", { name: "Weiterspielen: Lücken" })).toBeTruthy();
    expect(screen.queryByText("Zahlt nur einmal – morgen wieder")).toBeNull();
  });
});

/*
 * B-543 Entscheidung 2 – eine Abgabe, die am vollen Deckel abprallt, verbraucht die Runde nicht; die Antworten liegen nur
 * im Browser und müssen darum bis zur nächsten Abgabe liegen bleiben.
 */
describe("Bonus-Runde am vollen Deckel", () => {
  it("behält die Antworten und zeigt den Satz, wenn die Abgabe extra_cap_reached bekommt", async () => {
    // Rot, wenn die Lauf-Seite die gespeicherten Antworten auch bei einem Fehlschlag löscht (morgen hieße
    // „Weiterspielen“ dann „von vorn“), oder wenn die Fehlerbox den Satz zum Code nicht zeigt.
    const saved: ExtraAnswer[] = [{ itemIndex: 0, givenAnswer: "2", wasKnown: null }];
    sessionStorage.setItem("pugling.bonus.run.41", JSON.stringify(saved));
    startExtraRun.mockResolvedValue({
      runId: 41, exerciseId: 2, title: "Rechnen", type: "Arithmetic", form: "Typed", stage: 1, typed: true,
      coinsPerHit: 2, cards: [{ itemIndex: 0, prompt: "1 + 1" }],
    } as unknown as ExtraRunResponse);
    submitExtraRun.mockRejectedValue(new ApiError(409, "Technical server line.", "trace-7", "extra_cap_reached"));

    render(
      <MemoryRouter initialEntries={["/sohn/bonus/2"]}>
        <Routes><Route path="/sohn/bonus/:exerciseId" element={<SohnBonusRun />} /></Routes>
      </MemoryRouter>,
    );

    expect((await screen.findByRole("alert")).textContent)
      .toBe("Dein Bonus-Deckel ist voll, und diese Übung zahlt nur einmal – darum geht sie gerade nicht.");
    expect(submitExtraRun).toHaveBeenCalledWith(7, 41, saved);
    expect(JSON.parse(sessionStorage.getItem("pugling.bonus.run.41") ?? "[]")).toEqual(saved);
  });
});

/*
 * B-545 – hat sich die Übung während der Runde geändert, verwirft der Server die Runde. Ihre gespeicherten Antworten
 * gehören zu Karten, die es nicht mehr gibt: sie werden geräumt, und der Satz sagt, wo es weitergeht.
 */
describe("Bonus-Runde einer geänderten Übung", () => {
  it("räumt die Antworten und nennt den Neustart, wenn die Abgabe extra_run_stale bekommt", async () => {
    // Rot, wenn die Lauf-Seite die Antworten bei extra_run_stale stehen lässt (ein Eintrag ohne Runde, den nichts mehr
    // liest – Run-Ids werden nicht wiederverwendet), oder wenn der Code keinen eigenen Satz hat.
    const saved: ExtraAnswer[] = [{ itemIndex: 0, givenAnswer: "2", wasKnown: null }];
    sessionStorage.setItem("pugling.bonus.run.42", JSON.stringify(saved));
    startExtraRun.mockResolvedValue({
      runId: 42, exerciseId: 2, title: "Rechnen", type: "Arithmetic", form: "Typed", stage: 1, typed: true,
      coinsPerHit: 2, cards: [{ itemIndex: 0, prompt: "1 + 1" }],
    } as unknown as ExtraRunResponse);
    submitExtraRun.mockRejectedValue(new ApiError(409, "Technical server line.", "trace-8", "extra_run_stale"));

    render(
      <MemoryRouter initialEntries={["/sohn/bonus/2"]}>
        <Routes><Route path="/sohn/bonus/:exerciseId" element={<SohnBonusRun />} /></Routes>
      </MemoryRouter>,
    );

    expect((await screen.findByRole("alert")).textContent)
      .toBe("Diese Übung wurde geändert, seit du angefangen hast – fang sie im Bonus-Training neu an.");
    expect(sessionStorage.getItem("pugling.bonus.run.42")).toBeNull();
  });
});
