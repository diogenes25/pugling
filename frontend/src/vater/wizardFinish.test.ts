import { describe, expect, it } from "vitest";
import { newWizardProgress, runWizardFinish } from "./wizardFinish";
import type { WizardFinishInput, WizardWriter } from "./wizardFinish";
import type { CreateChildDto, CreatePlanDto, CreatePositionDto } from "../lib/types";

/**
 * Der Abschluss des Lehrplan-Assistenten – die teuerste Doppelklick-Stelle der App: hier entstehen bei
 * zwei Klicks im selben Tick nicht eine doppelte Mutation, sondern **zwei Kinder samt zwei Lehrplänen und
 * allen Positionen**, und das auf dem meistbegangenen Weg eines neuen Vaters.
 *
 * Zwei Dinge, die man leicht verwechselt und die dieselbe Ablage benutzen: die **Wiederaufnahme** nach
 * einem Fehler (sequenziell) und der **Wiedereintritt** (nebenläufig). Beide stehen unten.
 */

/** Ein Mitschreiber statt `api.ts`: er hält fest, was aufgerufen wurde, und darf auf Kommando scheitern. */
function schreiber(fehler: { bei: "child" | "plan" | "position"; wann?: number } | null = null) {
  const kinder: CreateChildDto[] = [];
  const plaene: CreatePlanDto[] = [];
  const positionen: { planId: number; dto: CreatePositionDto }[] = [];
  const writer: WizardWriter = {
    async createChild(dto) {
      kinder.push(dto);
      if (fehler?.bei === "child") throw new Error("Kind ging nicht");
      return { id: 100 + kinder.length };
    },
    async createPlan(dto) {
      plaene.push(dto);
      if (fehler?.bei === "plan") throw new Error("Plan ging nicht");
      return { id: 200 + plaene.length };
    },
    async addPosition(planId, dto) {
      positionen.push({ planId, dto });
      if (fehler?.bei === "position" && positionen.length === (fehler.wann ?? 1)) throw new Error("Übung ist leer");
      return {};
    },
  };
  return { writer, kinder, plaene, positionen };
}

const POSITION: Omit<CreatePositionDto, "exerciseId"> = {
  cadence: "Daily", stage: 4, goalThreshold: 80, useLeitner: true, requireTypedTest: true,
  pointsGoalMet: 20, penaltyCoins: 5, comboThreshold: 5, comboBonusPoints: 5,
};

function auftrag(ueber: Partial<WizardFinishInput> = {}): WizardFinishInput {
  return {
    newChild: { name: "Lena", schoolType: "Gymnasium" },
    existingChildId: null,
    plan: { title: "Englisch – Unit 1", subjectId: 3, durationDays: 14, startDate: "2026-08-01" },
    exerciseIds: [11, 12],
    position: POSITION,
    titleOf: (id) => `Übung ${id}`,
    ...ueber,
  };
}

describe("runWizardFinish – Wiedereintritt", () => {
  it("legt bei zwei Klicks im selben Tick ein Kind und einen Plan an", async () => {
    const s = schreiber();
    const progress = newWizardProgress();
    const eingabe = auftrag();

    // Beide Aufrufe starten, bevor der erste sein erstes `await` hinter sich hat – genau der Doppelklick.
    const [erster, zweiter] = await Promise.all([
      runWizardFinish(progress, eingabe, s.writer),
      runWizardFinish(progress, eingabe, s.writer),
    ]);

    expect(s.kinder).toHaveLength(1);
    expect(s.plaene).toHaveLength(1);
    expect(s.positionen).toHaveLength(2);
    expect(erster).toBe(201);
    // Der zweite Durchgang liefert `null` – kein Fehler, der Bildschirm tut schlicht nichts.
    expect(zweiter).toBeNull();
  });

  it("gibt nach dem Durchgang wieder frei – der Assistent ist nicht einmalig", async () => {
    const s = schreiber();
    const progress = newWizardProgress();

    await runWizardFinish(progress, auftrag(), s.writer);
    // Zweiter Anlauf **nach** dem ersten: er findet Kind und Plan vor und legt nur nichts Neues an.
    const nochmal = await runWizardFinish(progress, auftrag(), s.writer);

    expect(nochmal).toBe(201);
    expect(s.kinder).toHaveLength(1);
    expect(s.positionen).toHaveLength(2);
  });
});

describe("runWizardFinish – Wiederaufnahme nach einem Fehler", () => {
  it("legt kein zweites Kind an, wenn der Plan gescheitert ist", async () => {
    const kaputt = schreiber({ bei: "plan" });
    const progress = newWizardProgress();

    await expect(runWizardFinish(progress, auftrag(), kaputt.writer)).rejects.toThrow("Plan ging nicht");
    expect(kaputt.kinder).toHaveLength(1);
    expect(progress.childId).toBe(101);

    // Zweiter Anlauf mit heilem Server: das Kind ist vermerkt, es entsteht nur der fehlende Plan.
    const heil = schreiber();
    const planId = await runWizardFinish(progress, auftrag(), heil.writer);

    expect(heil.kinder).toHaveLength(0);
    expect(planId).toBe(201);
    // Der Plan hängt am **schon angelegten** Kind, nicht an einem neuen.
    expect(heil.plaene[0].childId).toBe(101);
  });

  it("setzt bei den Positionen dort fort, wo es hakte", async () => {
    const kaputt = schreiber({ bei: "position", wann: 2 });
    const progress = newWizardProgress();
    const eingabe = auftrag({ exerciseIds: [11, 12, 13] });

    await expect(runWizardFinish(progress, eingabe, kaputt.writer)).rejects.toThrow(/Übung 12/);
    expect(progress.positions).toEqual([11]);

    const heil = schreiber();
    await runWizardFinish(progress, eingabe, heil.writer);

    // Nur die beiden offenen – Übung 11 stand schon, ein zweites Mal wäre eine doppelte Position.
    expect(heil.positionen.map((p) => p.dto.exerciseId)).toEqual([12, 13]);
    expect(heil.plaene).toHaveLength(0);
  });

  it("nennt im Fehler die Übung, die es traf – der Server tut das nicht", async () => {
    const s = schreiber({ bei: "position", wann: 1 });

    await expect(runWizardFinish(newWizardProgress(), auftrag(), s.writer))
      // Backticks, weil der erwartete Text selbst ein Anführungszeichen trägt.
      .rejects.toThrow(`„Übung 11": Übung ist leer Der Plan ist angelegt – nimm die Übung ab und versuche es erneut.`);
  });
});

describe("runWizardFinish – der Auftrag", () => {
  it("nimmt das bestehende Kind, ohne eines anzulegen", async () => {
    const s = schreiber();

    const planId = await runWizardFinish(
      newWizardProgress(), auftrag({ newChild: null, existingChildId: 42 }), s.writer);

    expect(s.kinder).toHaveLength(0);
    expect(s.plaene[0].childId).toBe(42);
    expect(planId).toBe(201);
  });

  it("gibt jeder Position die Feinschliff-Werte und ihre eigene Übung", async () => {
    const s = schreiber();

    await runWizardFinish(newWizardProgress(), auftrag(), s.writer);

    expect(s.positionen.map((p) => p.planId)).toEqual([201, 201]);
    expect(s.positionen[0].dto).toEqual({ ...POSITION, exerciseId: 11 });
    expect(s.positionen[1].dto).toEqual({ ...POSITION, exerciseId: 12 });
  });
});
