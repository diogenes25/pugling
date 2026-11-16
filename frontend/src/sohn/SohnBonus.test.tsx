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
  completedRuns: 0, done: false, nextForm: "Typed", coinsPerHit: 3, openRunId: null, nextRunPercent: 100, ...over,
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
 * B-538 AK 6 – der Vorrat zeigt, was der nächste Lauf heute wert ist, und eine Übung, die heute nichts mehr bringt, bietet
 * keinen Start an.
 */
describe("Bonus-Übersicht – Wert des nächsten Laufs (B-538)", () => {
  it("nennt den Prozentwert ab der zweiten Runde und sperrt eine Übung, die heute nichts mehr bringt", async () => {
    // Rot, wenn die Zeile `nextRunPercent` nicht zeigt (dann fehlte „heute noch 50 % wert“), ihn auch bei 100 % zeigte,
    // oder wenn eine erledigte Übung weiter einen Link hätte.
    extraTraining.mockResolvedValue({
      unlocked: true, lockReason: null, outstanding: [], coinsToday: 9, dailyCoinCap: 90,
      exercises: [
        uebung({ exerciseId: 1, title: "Tiere" }),
        uebung({ exerciseId: 2, title: "Rechnen", type: "Arithmetic", repeatable: false, nextRunPercent: 50 }),
        uebung({ exerciseId: 3, title: "Lücken", type: "Cloze", repeatable: false, nextRunPercent: 0, done: true }),
      ],
    } as ExtraTrainingOverview);
    render(<MemoryRouter><SohnBonus /></MemoryRouter>);
    await screen.findByText("Tiere");

    expect(screen.getAllByText(/heute noch 50 % wert/)).toHaveLength(1);
    expect(screen.getByRole("link", { name: "Spielen: Rechnen" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Lücken/ })).toBeNull();
    expect(screen.getByText(/Für heute geschafft – morgen zählt sie wieder voll/)).toBeTruthy();
    expect(screen.queryByText(/100 % wert/)).toBeNull();
  });

  // Review-Fund. Rot, wenn eine Vokabel-Übung bei 0 % wie eine Übung fester Form gesperrt wird (der Server lässt sie ohne
  // Münzen weiterspielen), oder wenn am vollen Deckel noch ein Prozentwert steht, der dort nichts mehr bedeutet.
  it("lässt Vokabeln bei 0 % weiterspielen und nennt am vollen Deckel keinen Prozentwert", async () => {
    extraTraining.mockResolvedValue({
      unlocked: true, lockReason: null, outstanding: [], coinsToday: 90, dailyCoinCap: 90,
      exercises: [
        uebung({ exerciseId: 1, title: "Tiere", nextRunPercent: 0, done: true, completedRuns: 3 }),
        uebung({ exerciseId: 2, title: "Wörter", nextRunPercent: 50 }),
      ],
    } as ExtraTrainingOverview);
    render(<MemoryRouter><SohnBonus /></MemoryRouter>);
    await screen.findByText("Tiere");

    expect(screen.getByRole("link", { name: "Spielen: Tiere" })).toBeTruthy();
    expect(screen.queryByText(/Für heute geschafft/)).toBeNull();
    expect(screen.queryByText(/% wert/)).toBeNull();
  });
});

/*
 * B-538, Review-Fund – die Runde nennt ihren heutigen Wert, nicht den vollen Satz: der Hinweis über den Karten versprach
 * sonst „3 🪙 je Treffer“, während die Abgabe nur die Hälfte bucht.
 */
describe("Bonus-Runde – Hinweis zum Wert", () => {
  const starte = async (runPercent: number) => {
    startExtraRun.mockResolvedValue({
      runId: 50 + runPercent, exerciseId: 2, title: "Rechnen", type: "Arithmetic", form: "Typed", stage: 1, typed: true,
      coinsPerHit: 3, runPercent, cards: [{ itemIndex: 0, prompt: "1 + 1" }, { itemIndex: 1, prompt: "2 + 2" }],
    } as unknown as ExtraRunResponse);
    render(
      <MemoryRouter initialEntries={["/sohn/bonus/2"]}>
        <Routes><Route path="/sohn/bonus/:exerciseId" element={<SohnBonusRun />} /></Routes>
      </MemoryRouter>,
    );
    await screen.findByText("1 + 1");
  };

  // Rot, wenn der Hinweis `runPercent` übergeht (dann stünde der volle Satz da), oder bei 100 % schon von Prozent spricht.
  it("nennt bei halbem Wert den Prozentsatz, bei vollem den Satz je Treffer", async () => {
    await starte(50);
    expect(screen.getByText(/Diese Runde zählt heute 50 %: 3 🪙 je richtige Antwort/)).toBeTruthy();
  });

  it("nennt bei 0 % keine Münzen", async () => {
    await starte(0);
    expect(screen.getByText(/bringt heute keine Münzen mehr/)).toBeTruthy();
    expect(screen.queryByText(/bringt 3 🪙/)).toBeNull();
  });
});

/*
 * B-543 – bei vollem Tagesdeckel zeigt eine Übung fester Form ihre Lösungen nicht für 0 Münzen: der Server weist ihren Start ab, die
 * Zeile bietet ihn darum gar nicht erst an und sagt, wann sie wieder geht.
 */
describe("Bonus-Übersicht am Tagesdeckel", () => {
  it("bietet bei vollem Deckel nur noch Vokabeln an und sagt es den Übungen fester Form ehrlich", async () => {
    // Rot, wenn `BonusZeile` den Link ohne Blick auf `capReached`/`repeatable` rendert (dann hätten „Rechnen“ und
    // „Lücken“ einen Link), wenn der Satz an der Vokabelzeile stünde, oder wenn die Deckel-Karte den alten Text trägt.
    await zeige(30, 30);

    expect(screen.getByRole("link", { name: "Spielen: Tiere" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Rechnen/ })).toBeNull();
    expect(screen.queryByRole("link", { name: /Lücken/ })).toBeNull();
    expect(screen.getAllByText("Morgen wieder – heute würde sie dir nur die Lösungen zeigen, ohne Münzen")).toHaveLength(2);
    expect(screen.getByText(
      "Tagesdeckel erreicht – Vokabeln üben darfst du weiter, ohne Münzen. Die anderen Übungen warten bis morgen.",
    )).toBeTruthy();
  });

  it("verspricht bei einem Deckel von 0 kein „morgen“", async () => {
    // Rot, wenn Zeile oder Karte bei Deckel 0 „morgen“ zusagen – morgen zahlt die Übung genauso wenig – oder wenn die
    // Übungen fester Form bei 0 doch einen Link bekommen.
    await zeige(0, 0);

    expect(screen.getByRole("link", { name: "Spielen: Tiere" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Rechnen/ })).toBeNull();
    expect(screen.getAllByText("Wartet, bis es wieder Bonus-Münzen gibt")).toHaveLength(2);
    expect(screen.queryByText(/morgen/)).toBeNull();
  });

  it("lässt Übungen fester Form unter dem Deckel spielen", async () => {
    // Rot, wenn die Zeile den Start schon ohne vollen Deckel wegnimmt (etwa nur nach `repeatable` entscheidet).
    await zeige(29, 30);

    expect(screen.getByRole("link", { name: "Spielen: Rechnen" })).toBeTruthy();
    expect(screen.getByRole("link", { name: "Weiterspielen: Lücken" })).toBeTruthy();
    expect(screen.queryByText("Morgen wieder – heute würde sie dir nur die Lösungen zeigen, ohne Münzen")).toBeNull();
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
      .toBe("Dein Bonus-Deckel ist voll – diese Übung würde dir jetzt nur die Lösungen zeigen, ohne Münzen. Darum geht sie gerade nicht.");
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

/*
 * Die Auswertung eines Lückentexts: jede Zeile trug vorher den ganzen Absatz mit rohen `{{n}}`, und was das Kind
 * geantwortet hatte, stand nirgends – nicht zu erkennen, was gefragt, geantwortet und richtig war.
 */
describe("Bonus-Auswertung – Frage, Antwort, Lösung", () => {
  it("zeigt je Lücke nur ihren Satz, die falsche Antwort und die Lösung", async () => {
    const text = "1. Your sister plays the drums, {{1}} 2. We aren't late, {{2}}";
    startExtraRun.mockResolvedValue({
      runId: 300, exerciseId: 4, title: "Tags", type: "Cloze", form: "Typed", stage: 1, typed: true, coinsPerHit: 3,
      runPercent: 100, cards: [{ itemIndex: 0, prompt: text, gapIndex: 1 }, { itemIndex: 1, prompt: text, gapIndex: 2 }],
    } as unknown as ExtraRunResponse);
    submitExtraRun.mockResolvedValue({
      runId: 300, form: "Typed", total: 2, correct: 1, coinsAwarded: 1, capReached: false, coinsToday: 1, dailyCoinCap: null,
      coinsPerHit: 3, runPercent: 50, coinsEarned: 1, coinsForHits: 3,
      items: [
        { itemIndex: 0, prompt: text, expected: "doesn't she?", givenAnswer: "doesn't she?", wasCorrect: true, gapIndex: 1 },
        { itemIndex: 1, prompt: text, expected: "are we?", givenAnswer: "aren't we?", wasCorrect: false, gapIndex: 2 },
      ],
    } as ExtraRunResult);
    extraTraining.mockResolvedValue({ unlocked: true, lockReason: null, outstanding: [], coinsToday: 3, dailyCoinCap: null, exercises: [] } as ExtraTrainingOverview);
    sessionStorage.setItem("pugling.bonus.run.300", JSON.stringify([
      { itemIndex: 0, givenAnswer: "doesn't she?" }, { itemIndex: 1, givenAnswer: "aren't we?" }]));
    render(
      <MemoryRouter initialEntries={["/sohn/bonus/4"]}>
        <Routes><Route path="/sohn/bonus/:exerciseId" element={<SohnBonusRun />} /></Routes>
      </MemoryRouter>,
    );
    await screen.findByText("1 / 2 richtig");

    // Rot, wenn die Zeile wieder den ganzen Absatz trägt: dann käme „We aren't late" auch in der ersten Zeile vor.
    expect(screen.getAllByText(/We aren't late/)).toHaveLength(1);
    // Rot, wenn rohe Platzhalter durchschlagen (Lücke nicht gezeichnet).
    expect(screen.queryByText(/\{\{/)).toBeNull();
    // Rot, wenn die eigene falsche Antwort fehlt – oder auch bei der richtigen Zeile stünde.
    expect(screen.getByText("Du: aren't we?")).toBeTruthy();
    expect(screen.queryByText("Du: doesn't she?")).toBeNull();
    expect(screen.getByText("Richtig: are we?")).toBeTruthy();
    expect(screen.getByText("Richtig: doesn't she?")).toBeTruthy();

    // Die Rechnung in einer Zeile bis zum gebuchten Betrag, und kein Betrag an den Aufgaben – „+3“ je Zeile las sich als
    // Summe. Rot, wenn ein Zeilenbetrag zurückkommt, der Prozentschritt fehlt (dann bliebe offen, warum aus 3 🪙 nur 1
    // wurde), die Summe nicht den gebuchten Betrag nennt oder der Deckel ohne Kappung erscheint.
    expect(screen.queryByText("+3 🪙")).toBeNull();
    expect(screen.getByText("+1 🪙")).toBeTruthy();
    expect(document.querySelector(".erloes-summe")?.textContent).toBe("1 richtig × 3 🪙 = 3 🪙 · heute 50 % → +1 🪙");
  });

  // B-597/B-599: Jeder Treffer zahlt nach der Form seiner Karte. Mischt die Runde Auswahl (1) und Schreiben (3), ist die
  // Summe kein „richtig × Satz“. Rot, wenn die Zeile weiter „2 richtig × 3 🪙 = 6 🪙“ rechnete, obwohl 4 verdient wurden.
  it("nennt bei gemischten Karten die Summe der Treffer statt richtig × Satz", async () => {
    startExtraRun.mockResolvedValue({
      runId: 301, exerciseId: 5, title: "Beides", type: "Vocabulary", form: "Typed", stage: 4, typed: true, coinsPerHit: 3,
      runPercent: 100, cards: [{ itemIndex: 0, prompt: "walk" }, { itemIndex: 1, prompt: "gehen" }],
    } as unknown as ExtraRunResponse);
    submitExtraRun.mockResolvedValue({
      runId: 301, form: "Typed", total: 2, correct: 2, coinsAwarded: 4, capReached: false, coinsToday: 4, dailyCoinCap: null,
      coinsPerHit: 3, runPercent: 100, coinsEarned: 4, coinsForHits: 4,
      items: [
        { itemIndex: 0, prompt: "walk", expected: "gehen", givenAnswer: "gehen", wasCorrect: true, gapIndex: null },
        { itemIndex: 1, prompt: "gehen", expected: "walk", givenAnswer: "walk", wasCorrect: true, gapIndex: null },
      ],
    } as ExtraRunResult);
    extraTraining.mockResolvedValue({ unlocked: true, lockReason: null, outstanding: [], coinsToday: 4, dailyCoinCap: null, exercises: [] } as ExtraTrainingOverview);
    sessionStorage.setItem("pugling.bonus.run.301", JSON.stringify([
      { itemIndex: 0, givenAnswer: "gehen" }, { itemIndex: 1, givenAnswer: "walk" }]));
    render(
      <MemoryRouter initialEntries={["/sohn/bonus/5"]}>
        <Routes><Route path="/sohn/bonus/:exerciseId" element={<SohnBonusRun />} /></Routes>
      </MemoryRouter>,
    );
    await screen.findByText("2 / 2 richtig");

    expect(document.querySelector(".erloes-summe")?.textContent)
      .toBe("2 richtig = 4 🪙 (Auswählen zählt weniger als Schreiben) → +4 🪙");
  });
});
