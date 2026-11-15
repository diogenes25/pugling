import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ExerciseSummary, ExerciseTypeManifest, Paged, PositionResponse } from "../lib/types";
import { PlanPositions, STAGE_DEFAULT_LABEL } from "./PlanPositions";

/*
 * B-16: das Stufen-Feld der Position kommt aus dem Manifest des Übungstyps – beim Anlegen (Typ der gewählten Übung) wie
 * beim Bearbeiten (Typ der Position). Eigene Datei, weil `loadExerciseTypes` das Manifest modulweit zwischenspeichert:
 * hier tragen alle Fälle dasselbe Manifest mit Stufen, die Nachbardateien ein leeres.
 */
const MANIFEST = [
  { type: "Cloze", label: "Lückentext", checkMode: "StudyPlanTest", selfAssessedTest: true,
    stages: [{ value: 1, label: "Aufdecken" }, { value: 4, label: "Tippen" }] },
  { type: "Essay", label: "Aufsatz", checkMode: "None", stages: [] },
] as unknown as ExerciseTypeManifest[];

const ex = (id: number, type: string, title: string) => ({
  id, type, title, defaultUseLeitner: false, defaultRequireTypedTest: false, defaultItemCount: null, gradeMin: null,
  gradeMax: null, categoryName: null, source: null, description: null, averageRating: null, ratingCount: 0, itemCount: 3,
  canAssign: true,
}) as unknown as ExerciseSummary;

const CLOZE_A = ex(5, "Cloze", "Lückentext A");
const CLOZE_B = ex(6, "Cloze", "Lückentext B");
const ESSAY = ex(7, "Essay", "Aufsatz C");

function rendere(positions: PositionResponse[]) {
  vi.spyOn(api, "positions").mockResolvedValue(positions);
  vi.spyOn(api, "subjects").mockResolvedValue([]);
  vi.spyOn(api, "exerciseTypes").mockResolvedValue(MANIFEST);
  vi.spyOn(api, "textbookSeries").mockResolvedValue([]);
  vi.spyOn(api, "searchExercises").mockResolvedValue(
    { items: [CLOZE_A, CLOZE_B, ESSAY], total: 3 } as unknown as Paged<ExerciseSummary>);
  render(<AuthProvider><PlanPositions planId={1} /></AuthProvider>);
}

const optionen = (select: HTMLElement) => within(select).getAllByRole("option").map((o) => o.textContent);

const position = (over: Partial<PositionResponse> = {}) => ({
  id: 9, order: 0, exerciseId: 5, exerciseTitle: "Lückentext A", exerciseType: "Cloze", stage: 4, scope: "All",
  cadence: "Daily", goalThreshold: null, itemCount: null, orderStrategy: "WeakestFirst", pointsGoalMet: 20,
  penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5, speedThresholdSeconds: 0,
  speedBonusPoints: 0, useLeitner: false, requireTypedTest: false, newItemsPerDay: 0, dailyDoseInForce: 0, maxBox: 5,
  timeSlots: null, ...over,
}) as unknown as PositionResponse;

describe("Positions-Formular – Stufe aus dem Manifest (B-16)", () => {
  afterEach(() => vi.restoreAllMocks());

  // AK 3 (Anlegen). Rot, wenn das Formular eine feste Liste (`VOCAB_FORMS`) statt der Manifest-Stufen des Typs zeigt, oder
  // wenn das Feld bei einem Typ ohne Stufen (Aufsatz) dennoch erscheint.
  it("zeigt beim Anlegen „Standard der Übung“ plus genau die Stufen des Typs – und keins für einen Typ ohne", async () => {
    rendere([]);
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext A/ }));
    await act(async () => {});
    expect(optionen(await screen.findByRole("combobox", { name: "Stufe" }))).toEqual([STAGE_DEFAULT_LABEL, "Aufdecken", "Tippen"]);

    fireEvent.click(screen.getByRole("radio", { name: /Aufsatz C/ }));
    expect(screen.queryByRole("combobox", { name: "Stufe" })).toBeNull();
  });

  // AK 5. Rot, wenn der `onChange` der Radio-Zeile `stage` nicht zurücksetzt: dann trüge Übung B die Stufe, die der Vater
  // für Übung A gewählt hat.
  it("ein Übungswechsel setzt die Stufe auf „Standard der Übung“ zurück", async () => {
    rendere([]);
    fireEvent.click(await screen.findByRole("radio", { name: /Lückentext A/ }));
    await act(async () => {});
    fireEvent.change(await screen.findByRole("combobox", { name: "Stufe" }), { target: { value: "4" } });
    expect((screen.getByRole("combobox", { name: "Stufe" }) as HTMLSelectElement).value).toBe("4");

    fireEvent.click(screen.getByRole("radio", { name: /Lückentext B/ }));
    expect((screen.getByRole("combobox", { name: "Stufe" }) as HTMLSelectElement).value).toBe("");
  });

  // AK 3 (Bearbeiten). Rot, wenn die Zeile im Bearbeiten-Modus ihre Stufen nicht aus `pos.exerciseType` liest (etwa eine
  // leere Liste übergibt): dann fehlte das Feld genau dort, wo der Vater eine Stufe zurücknehmen will.
  it("zeigt beim Bearbeiten die Stufen des Positionstyps mit der gespeicherten Stufe", async () => {
    rendere([position()]);
    fireEvent.click(await screen.findByRole("button", { name: "Bearbeiten" }));
    await act(async () => {});

    const stufe = await screen.findByRole("combobox", { name: "Stufe" }) as HTMLSelectElement;
    expect(optionen(stufe)).toEqual([STAGE_DEFAULT_LABEL, "Aufdecken", "Tippen"]);
    expect(stufe.value).toBe("4");
  });
});

/*
 * B-469 AK 2 und 4: die Zeile nennt die Lernkontrolle statt „· getippt" und erklärt sie per InfoHint. Hier, weil der Text
 * am Manifest (`checkMode`) hängt – diese Datei trägt eines.
 */
describe("Positionszeile – Lernkontrolle sichtbar (B-469)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn `PositionRow` den Text nicht rendert (Ausgangszustand: bei ungetippt steht nichts), ihn nicht als `pill amber`
  // hervorhebt oder das alte „· getippt" daneben stehen lässt.
  it("zeigt je Position die Art der Prüfung, die schwachen hervorgehoben", async () => {
    rendere([
      // Die Art kommt vom Server (B-550) – die Fixtures tragen sie wie die echte Antwort.
      position({ id: 1, exerciseTitle: "Getippt", requireTypedTest: true, learningCheck: { kind: "Typed" } } as Partial<PositionResponse>),
      position({ id: 2, exerciseTitle: "Ungetippt", requireTypedTest: false, learningCheck: { kind: "SelfAssessed" } } as Partial<PositionResponse>),
      position({ id: 3, exerciseTitle: "Grammatik", exerciseType: "Essay", stage: null, learningCheck: { kind: "NoTest" } } as Partial<PositionResponse>),
    ]);
    const zeile = async (titel: string) => (await screen.findByText(titel)).closest("tr") as HTMLElement;

    const getippt = await zeile("Getippt");
    // `findBy`: Positionen und Manifest kommen aus zwei Abrufen; ohne Warten prüfte die Zeile mal vor dem Manifest (AK 3:
    // dann steht dort bewusst nichts) – gemessen als sporadisch rot.
    expect((await within(getippt).findByText("Test getippt")).className).not.toMatch(/amber/);
    expect(getippt.textContent).not.toMatch(/· getippt/);
    expect(within(await zeile("Ungetippt")).getByText("Test, Selbsteinschätzung zählt").className).toBe("pill amber");
    expect(within(await zeile("Grammatik")).getByText("kein Test – zählt Durchspielen").className).toBe("pill amber");
    // AK 4: die Erklärung hängt an der Angabe.
    expect(within(getippt).getByRole("button", { name: "Erklärung zu „Lernkontrolle\"" })).toBeTruthy();
  });
});

/*
 * B-550: die Zeile rendert, was der Server über die Lernkontrolle sagt. Das Manifest (Lückentext: `selfAssessedTest`) und
 * das fehlende Häkchen ergäben nach der alten Ableitung „Selbsteinschätzung zählt“ – der Server sagt „Typed“ (etwa wegen
 * der Stufe). Rot, sobald die Zeile wieder selbst rechnet.
 */
describe("Positionszeile – Lernkontrolle vom Server (B-550)", () => {
  afterEach(() => vi.restoreAllMocks());

  it("zeigt die Art des Servers, nicht eine eigene Ableitung", async () => {
    rendere([position({ requireTypedTest: false, learningCheck: { kind: "Typed" } } as Partial<PositionResponse>)]);
    const zeile = await screen.findByRole("row", { name: /Lückentext A/ });
    expect(zeile.textContent).toContain("Test getippt");
    expect(zeile.textContent).not.toContain("Selbsteinschätzung zählt");
  });

  it("zeigt eine unerreichbare Kombination rot", async () => {
    rendere([position({ learningCheck: { kind: "Unreachable" } } as Partial<PositionResponse>)]);
    const zeile = await screen.findByRole("row", { name: /Lückentext A/ });
    expect(within(zeile).getByText(/^Ziel nicht erreichbar/).className).toBe("pill mag");
  });
});

/*
 * B-547, Review-Fund: die Zeile bleibt über das Neuladen nach dem Speichern montiert. Merkte sie sich die Werte nur beim
 * ersten Rendern, schickte ein zweites „zurück auf Standard" keinen Schalter – und „Gespeichert." stimmte nicht.
 */
describe("Positionszeile – zweimal bearbeiten (B-547)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn „Bearbeiten" die Einstellungen nicht frisch aus der neu geladenen Position übernimmt: dann stünde
  // `itemCountStored: null` aus dem ersten Rendern, und der zweite PATCH trüge `clearItemCount: false`.
  it("schickt nach einem Speichern beim Leeren den Schalter", async () => {
    const update = vi.spyOn(api, "updatePosition").mockResolvedValue(position({ itemCount: 5 }));
    rendere([position()]);
    vi.mocked(api.positions).mockResolvedValue([position({ itemCount: 5 })]);

    fireEvent.click(await screen.findByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Inhalte", { exact: true }), { target: { value: "5" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "OK" })); });
    await act(async () => {});

    fireEvent.click(await screen.findByRole("button", { name: "Bearbeiten" }));
    expect((screen.getByLabelText("Inhalte", { exact: true }) as HTMLInputElement).value).toBe("5");
    fireEvent.change(screen.getByLabelText("Inhalte", { exact: true }), { target: { value: "" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "OK" })); });

    expect(update).toHaveBeenCalledTimes(2);
    expect(update.mock.calls[1][2]).toMatchObject({ itemCount: null, clearItemCount: true });
  });
});

/*
 * B-538 AK 7: eine Position ohne Pflicht zahlt keine Münzen – der Editor blendet ihre Punkte-Felder aus und sagt warum.
 */
describe("Positions-Formular – ohne Pflicht keine Punkte-Felder (B-538)", () => {
  afterEach(() => vi.restoreAllMocks());

  // Rot, wenn die Felder bei `cadence: "None"` stehen bleiben (dann gäbe es „Punkte bei erreichtem Ziel“), oder wenn sie
  // auch bei einer Pflicht fehlen.
  it("zeigt die Punkte-Felder nur bei einer Pflicht", async () => {
    rendere([position({ id: 1, exerciseTitle: "Frei", cadence: "None" }), position({ id: 2, exerciseTitle: "Pflicht" })]);
    const frei = (await screen.findByText("Frei")).closest("tr") as HTMLElement;
    fireEvent.click(within(frei).getByRole("button", { name: "Bearbeiten" }));
    expect(screen.queryByRole("spinbutton", { name: "Punkte bei erreichtem Ziel" })).toBeNull();
    expect(screen.getByText("Ohne Pflicht bringt diese Position keine Punkte.")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Abbrechen" }));

    const pflicht = (await screen.findByText("Pflicht")).closest("tr") as HTMLElement;
    fireEvent.click(within(pflicht).getByRole("button", { name: "Bearbeiten" }));
    expect(screen.getByRole("spinbutton", { name: "Punkte bei erreichtem Ziel" })).toBeTruthy();
  });
});
