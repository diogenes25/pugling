import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ChildResponse, DailyPlan } from "../lib/types";
import { LeitplankenSection } from "./ChildCarePanels";
import { TagesplanerSection, planWirkung, planerSchalterHinweis } from "./Tagesplaene";

/*
 * B-734: der Tagesplaner im Vater-Web. Geplant wird am Server; hier steht, dass der Schalter nur mit Leitplanken schaltet,
 * jeder Plan seine Begründung je Aufgabe zeigt und ein Schattenplan nie als wirksam beschriftet ist (AK 11).
 */
const gesetzt = { dailyTasksMin: 1, dailyTasksMax: 4, dailyNewWordsMin: 2, dailyNewWordsMax: 6, penaltyCoinsPerMissedTask: 0 };

const kind = (extra: Partial<ChildResponse> = {}): ChildResponse => ({
  id: 4, name: "Friedrich", birthYear: null, grade: 7, schoolType: "Gymnasium", gender: "Male", interests: [],
  profileNotes: null, allowedContentRating: "Everyone", createdAt: "2026-11-01T00:00:00Z", coins: 0, gems: 0,
  bonusDailyCoinCap: null, towerGameMinutesPerDay: null, homeworkCoins: null, homeworkPenaltyCoins: null,
  dailyTasksMin: null, dailyTasksMax: null, dailyNewWordsMin: null, dailyNewWordsMax: null, penaltyCoinsPerMissedTask: null,
  plannerMode: "Off", plannerSince: null,
  ...extra,
} as ChildResponse);

const plan: DailyPlan = {
  day: "2026-11-30", createdAt: "2026-11-30T05:00:00Z", mode: "Shadow", newWordsBudget: 2,
  warnings: ["Nur 1 Positionen in den aktiven Plänen – weniger als der Boden von 2 Aufgaben."],
  tasks: [{
    planPositionId: 7, exerciseId: 9, exerciseTitle: "Unité 5", subjectId: 1, subjectName: "Französisch", order: 1,
    dueReviews: 3, newWords: 2, stage: null, reason: "3 fällige Wiederholungen; 2 neue Wörter (Boden 2, noch kein Tempo)",
  }],
};

describe("Tagesplaner (B-734 AK 11)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn der Schalter ohne alle vier Grenzen schalten ließe – der Server lehnte dann mit 400 ab.
  it("verlangt alle vier Leitplanken vor dem Einschalten", () => {
    expect(planerSchalterHinweis(kind(gesetzt))).toBeNull();
    expect(planerSchalterHinweis(kind({ ...gesetzt, dailyNewWordsMax: null }))).toMatch(/Leitplanken/);
  });

  // Rot, wenn ein Schattenplan als wirksam oder ohne seinen Modus beschriftet wäre.
  it("nennt einen Schattenplan nie wirksam", () => {
    const text = planWirkung(plan, "Friedrich");
    expect(text).toContain("wirkt nicht auf Pflicht, Malus und die App von Friedrich");
    expect(text).not.toMatch(/wirksam/);
  });

  // Rot, wenn der Knopf ohne Leitplanken aktiv wäre oder nach dem Einschalten etwas anderes schickt als den Schattenmodus.
  it("schaltet mit Leitplanken in den Schattenmodus und lädt das Kind neu", async () => {
    const schalten = vi.spyOn(api, "updateChild").mockResolvedValue(kind({ ...gesetzt, plannerMode: "Shadow" }));
    const neuLaden = vi.fn();
    const { rerender } = render(<AuthProvider><TagesplanerSection child={kind()} onSaved={neuLaden} /></AuthProvider>);
    expect((screen.getByRole("button", { name: "Im Schattenmodus einschalten" }) as HTMLButtonElement).disabled).toBe(true);

    rerender(<AuthProvider><TagesplanerSection child={kind(gesetzt)} onSaved={neuLaden} /></AuthProvider>);
    fireEvent.click(screen.getByRole("button", { name: "Im Schattenmodus einschalten" }));
    await act(async () => {});

    expect(schalten).toHaveBeenCalledWith(4, { plannerMode: "Shadow" });
    expect(neuLaden).toHaveBeenCalled();
  });

  // Rot, wenn eine Aufgabe ohne ihre Begründung erscheint, die Warnung fehlt oder die Tage ohne Plan nicht gemeldet werden.
  it("zeigt je Aufgabe die Begründung und meldet Tage ohne Plan", async () => {
    vi.spyOn(api, "childDailyPlans").mockResolvedValue([plan]);
    vi.spyOn(api, "childMissingDailyPlans").mockResolvedValue(["2026-11-27"]);
    render(<AuthProvider>
      <TagesplanerSection child={kind({ ...gesetzt, plannerMode: "Shadow", plannerSince: "2026-11-25" })} onSaved={vi.fn()} />
    </AuthProvider>);

    const zeile = await screen.findByRole("row", { name: /Unité 5/ });
    expect(zeile.textContent).toContain("3 fällige Wiederholungen; 2 neue Wörter (Boden 2, noch kein Tempo)");
    expect(screen.getByText(/Schattenplan – wirkt nicht/)).toBeTruthy();
    expect(screen.getByText(/weniger als der Boden von 2 Aufgaben/)).toBeTruthy();
    expect((await screen.findByText(/Ohne Plan/)).textContent).toMatch(/27\.11\.2026.*zählen wie eine Pause/);
  });

  // Review-Fund: nach dem Einschalten trug `useAsync` noch das Ergebnis des ausgeschalteten Stands. Rot, wenn der
  // ausgeschaltete Stand „leer“ statt „nichts geladen“ liefert: dann erscheint „Noch kein Plan“, während der Server den
  // heutigen Plan erst anlegt (und bliebe bei einem Ladefehler stehen).
  it("sagt nach dem Einschalten nicht „Noch kein Plan“ aus dem alten Stand", async () => {
    vi.spyOn(api, "childMissingDailyPlans").mockResolvedValue([]);
    vi.spyOn(api, "childDailyPlans").mockReturnValue(new Promise<DailyPlan[]>(() => {}));
    const { rerender } = render(<AuthProvider><TagesplanerSection child={kind(gesetzt)} onSaved={vi.fn()} /></AuthProvider>);
    await act(async () => {});

    rerender(<AuthProvider><TagesplanerSection child={kind({ ...gesetzt, plannerMode: "Shadow" })} onSaved={vi.fn()} /></AuthProvider>);
    await act(async () => {});
    expect(screen.queryByText(/Noch kein Plan/)).toBeNull();
    expect(screen.getByText("Lade…")).toBeTruthy();

  });

  // Rot, wenn „Leitplanken entfernen“ bei laufendem Planer klickbar bliebe – der Server lehnte mit 400 ab.
  it("sperrt das Entfernen der Leitplanken, solange der Planer läuft", () => {
    render(<LeitplankenSection child={kind({ ...gesetzt, plannerMode: "Shadow" })} onSaved={vi.fn()} />);

    expect((screen.getByRole("button", { name: "Leitplanken entfernen" }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText(/erst den Planer ausschalten/)).toBeTruthy();
  });
});
