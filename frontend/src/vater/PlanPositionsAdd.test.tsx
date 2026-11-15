import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseSummary, Paged } from "../lib/types";
import { PlanPositions } from "./PlanPositions";
import { NICHT_ZUWEISBAR_GRUND } from "./wizardSearch";

/*
 * B-534 (Review): die Rückmeldung des Hinzufügens landet am Hinzufügen, nicht über der Tabelle. In einem langen Plan
 * stand „hinzugefügt" bzw. ein 409 sonst außer Sicht. Gefälscht wird der Abruf, nicht das `fetch`.
 */
const EX = {
  id: 5, type: "Cloze", title: "Lückentext 1", defaultUseLeitner: false, defaultRequireTypedTest: false,
  defaultItemCount: null, gradeMin: null, gradeMax: null, categoryName: null, source: null, description: null,
  averageRating: null, ratingCount: 0, itemCount: 3, canAssign: true,
} as unknown as ExerciseSummary;

describe("PlanPositions – Hinzufügen", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, solange `AddPosition` die geteilte Aktion der Zeilen benutzt: dann steht die Meldung im Banner über der
  // Tabelle, außerhalb des Panels.
  it.each([
    ["Erfolg", () => Promise.resolve({}), "Übung als Position hinzugefügt."],
    ["409", () => Promise.reject(new Error("Die Übung steckt schon im Plan.")), "Die Übung steckt schon im Plan."],
  ])("zeigt die Meldung (%s) im Hinzufügen-Panel", async (_fall, antwort, text) => {
    vi.spyOn(api, "positions").mockResolvedValue([]);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [EX], total: 1 } as unknown as Paged<ExerciseSummary>);
    vi.spyOn(api, "addPosition").mockImplementation(antwort as never);
    render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);

    // Leerer Plan: das Panel ist offen.
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext 1/ }));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "+ Position hinzufügen" })); });

    const meldung = await screen.findByText(text);
    expect(meldung.closest(".add-panel")).not.toBeNull();
  });
});

describe("PlanPositions – Lesefehler", () => {
  afterEach(() => vi.restoreAllMocks());

  // B-183. Rot, solange der Ausfall ein nacktes `banner err` ist: dann gibt es keinen „Erneut laden"-Knopf, und der
  // Vater käme nur per F5 an seine Positionen.
  it("bietet nach gescheitertem Laden „Erneut laden“ und zeigt danach die Positionen", async () => {
    const positions = vi.spyOn(api, "positions")
      .mockRejectedValueOnce(new Error("Netz weg"))
      .mockResolvedValue([]);
    vi.spyOn(api, "subjects").mockResolvedValue([]);
    vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
    vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
    vi.spyOn(api, "searchExercises").mockResolvedValue({ items: [], total: 0 } as unknown as Paged<ExerciseSummary>);
    render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);
    // Den abgelehnten Abruf erst durchlaufen lassen (B-284), sonst steht noch „Lade Positionen…".
    await act(async () => {});

    await act(async () => { fireEvent.click(await screen.findByRole("button", { name: "Erneut laden" })); });

    expect(positions).toHaveBeenCalledTimes(2);
    expect(await screen.findByText(/Noch keine Übungen im Plan/)).toBeTruthy();
  });
});

/*
 * B-540: Umsortieren ist dieselbe Suche. Die zweite Seite kommt vom Mock je nach `sort` – so lässt sich steuern, ob die
 * gewählte Übung nach dem Umsortieren noch auf der Seite steht.
 */
const EX_B = { ...EX, id: 6, title: "Lückentext 2" } as unknown as ExerciseSummary;
const seite = (...items: ExerciseSummary[]) => ({ items, total: items.length }) as unknown as Paged<ExerciseSummary>;

function planbauMitSuche(suche: (q: { sort?: string }) => Paged<ExerciseSummary>) {
  vi.spyOn(api, "positions").mockResolvedValue([]);
  vi.spyOn(api, "subjects").mockResolvedValue([]);
  vi.spyOn(api, "exerciseTypes").mockResolvedValue([]);
  vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
  vi.spyOn(api, "searchExercises").mockImplementation(((q: { sort?: string }) => Promise.resolve(suche(q ?? {}))) as never);
  render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);
}

async function umsortieren() {
  await act(async () => {
    fireEvent.change(screen.getByLabelText("Trefferliste sortieren"), { target: { value: "rating" } });
  });
  await act(async () => {});
}

describe("PlanPositions – Umsortieren behält die Auswahl (B-540)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 2. Rot, wenn der Auswahl-Schlüssel (`filterKey`) die Sortierung wieder enthält (`JSON.stringify(kriterien)`): dann
  // leert der Filterwechsel-Effekt die Auswahl und meldet „weil sich die Suche geändert hat".
  it("A bleibt gewählt, wenn die umsortierte Seite A enthält", async () => {
    planbauMitSuche((q) => (q.sort === "rating" ? seite(EX_B, EX) : seite(EX, EX_B)));
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext 1/ }));
    await umsortieren();

    expect((screen.getByRole("radio", { name: /Lückentext 1/ }) as HTMLInputElement).checked).toBe(true);
    expect(screen.queryByText(/Auswahl zurückgesetzt/)).toBeNull();
    expect((screen.getByRole("button", { name: "+ Position hinzufügen" }) as HTMLButtonElement).disabled).toBe(false);
  });

  // AK 3. Rot, wenn der Effekt auf `exercises.data` (auswahlNachUmsortieren) fehlt: dann bleibt `exerciseId` auf A, das
  // nicht mehr in der Liste steht – der Absende-Knopf bliebe bedienbar und es gäbe keinen Hinweis (B-167).
  it("die Auswahl fällt mit Hinweis, wenn die umsortierte Seite A nicht enthält", async () => {
    planbauMitSuche((q) => (q.sort === "rating" ? seite(EX_B) : seite(EX, EX_B)));
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext 1/ }));
    await umsortieren();

    expect(screen.getByText(
      "Auswahl zurückgesetzt (1 Übung), weil sie nach dem Neuladen nicht mehr in der Liste steht.")).toBeTruthy();
    expect((screen.getByRole("button", { name: "+ Position hinzufügen" }) as HTMLButtonElement).disabled).toBe(true);
  });

  // AK 4 (Regression). Rot, wenn der Auswahl-Schlüssel ein Filterfeld nicht mehr enthält (hier: die Freitextsuche) – dann
  // bliebe die Auswahl über einen echten Filterwechsel stehen.
  it("ein Filterwechsel leert die Auswahl weiter mit dem bisherigen Hinweis", async () => {
    planbauMitSuche(() => seite(EX, EX_B));
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext 1/ }));
    await act(async () => { fireEvent.change(screen.getByLabelText("Freitext-Filter"), { target: { value: "Lücke" } }); });
    await act(async () => {});

    expect(screen.getByText("Auswahl zurückgesetzt (1 Übung), weil sich die Suche geändert hat.")).toBeTruthy();
    expect((screen.getByRole("radio", { name: /Lückentext 1/ }) as HTMLInputElement).checked).toBe(false);
  });
});

/*
 * B-140 AK 3: eine Übung, die der Vater nicht zuweisen darf (`canAssign: false`, vom Server gerechnet), ist im Planbau
 * nicht wählbar und nennt den Grund – per `aria-describedby` am Radio, nicht als `title` (B-160).
 */
describe("PlanPositions – nicht zuweisbare Übung (B-140)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn `disabled` am Radio nur an `zeilenVeraltet` hängt (der Ausgangszustand) oder der Grund nicht per
  // `aria-describedby` am Radio hängt. Die zuweisbare Zeile ist die Gegenprobe: ohne sie wäre „alles gesperrt" auch grün.
  it("sperrt das Radio, nennt den Grund und lässt die zuweisbare Zeile frei", async () => {
    const gesperrt = { ...EX, id: 8, title: "Zurückgezogen 1", canAssign: false } as unknown as ExerciseSummary;
    planbauMitSuche(() => seite(EX, gesperrt));

    const radio = await screen.findByRole("radio", { name: /Zurückgezogen 1/ }) as HTMLInputElement;
    expect(radio.disabled).toBe(true);
    const grundId = radio.getAttribute("aria-describedby");
    expect(grundId).toBeTruthy();
    expect(document.getElementById(grundId!)?.textContent).toBe(NICHT_ZUWEISBAR_GRUND);

    const frei = screen.getByRole("radio", { name: /Lückentext 1/ }) as HTMLInputElement;
    expect(frei.disabled).toBe(false);
    expect(frei.getAttribute("aria-describedby")).toBeNull();
    // Ausprobieren bleibt möglich – Lesen ist global (Entscheidung 4).
    expect((screen.getByRole("button", { name: "„Zurückgezogen 1\" ausprobieren" }) as HTMLButtonElement).disabled).toBe(false);
  });
});
