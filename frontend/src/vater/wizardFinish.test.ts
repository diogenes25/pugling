import { describe, expect, it } from "vitest";
import {
  dosisZeile, KlassenarbeitFehlt, leiterText, newWizardProgress, planVorgaben, runWizardFinish, stageScheduleFor, terminProblem,
} from "./wizardFinish";
import type { WizardFinishInput, WizardWriter } from "./wizardFinish";
import type { CreateChildDto, CreateKlassenarbeitDto, CreatePlanDto, CreatePositionDto } from "../lib/types";

/**
 * Der Abschluss des Lehrplan-Assistenten – die teuerste Doppelklick-Stelle der App: hier entstehen bei
 * zwei Klicks im selben Tick nicht eine doppelte Mutation, sondern **zwei Kinder samt zwei Lehrplänen und
 * allen Positionen**, und das auf dem meistbegangenen Weg eines neuen Vaters.
 *
 * Zwei Dinge, die man leicht verwechselt und die dieselbe Ablage benutzen: die **Wiederaufnahme** nach
 * einem Fehler (sequenziell) und der **Wiedereintritt** (nebenläufig). Beide stehen unten.
 */

/** Ein Mitschreiber statt `api.ts`: er hält fest, was aufgerufen wurde, und darf auf Kommando scheitern. */
function schreiber(fehler: { bei: "child" | "plan" | "position" | "classTest"; wann?: number } | null = null) {
  const kinder: CreateChildDto[] = [];
  const arbeiten: CreateKlassenarbeitDto[] = [];
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
    async createClassTest(dto) {
      arbeiten.push(dto);
      if (fehler?.bei === "classTest") throw new Error("Arbeit ging nicht");
      return { klassenarbeit: { id: 300 + arbeiten.length } };
    },
  };
  return { writer, kinder, plaene, positionen, arbeiten };
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
    // Vorgabe „alles Vokabeln": die Stufen-Weiche prüfen die Fälle unten ausdrücklich.
    typeOf: () => "Vocabulary",
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

  /*
   * Die „Test-Stufe" des Feinschliffs ist die Vokabel-Skala, und jeder Typ liest die Zahl anders: 2 heißt
   * bei der Vokabel „Selbstcheck", bei der Zuordnung „mit Ablenkern". Ungefiltert verwandelte das Ziel
   * „Rückstand aufholen" (Stufe 2) jede gewählte Zuordnung in Multiple-Choice – unsichtbar, weil der
   * Testmodus des Vaters weiter Freitext zeigte.
   */
  it("schickt die Stufe nur an Vokabelübungen", async () => {
    const s = schreiber();

    await runWizardFinish(newWizardProgress(), auftrag({
      exerciseIds: [11, 12],
      typeOf: (id) => (id === 11 ? "Vocabulary" : "Matching"),
    }), s.writer);

    expect(s.positionen[0].dto).toEqual({ ...POSITION, exerciseId: 11 });
    // Ohne `stage` entscheidet der Server: Übungs-Vorgabe, sonst Typ-Vorgabe.
    expect(s.positionen[1].dto).not.toHaveProperty("stage");
    // Alles andere aus dem Feinschliff gilt weiter – gefiltert wird genau ein Feld.
    expect(s.positionen[1].dto.goalThreshold).toBe(80);
    expect(s.positionen[1].dto.pointsGoalMet).toBe(20);
  });

  /*
   * B-477: Die Tagesdosis des Feinschliffs gilt für alle Übungen – eine Liste kann aber als Menge werten (ungeordnet),
   * und dort lehnt der Server jede Dosis ab (B-04, Entscheidung 9). Ungefiltert brach das Anlegen mitten im Plan ab.
   * Rot, sobald die Liste die Dosis wieder bekommt; die Vokabelübung daneben behält sie.
   */
  it("schickt keine Tagesdosis an Listen", async () => {
    const s = schreiber();

    await runWizardFinish(newWizardProgress(), auftrag({
      exerciseIds: [11, 12],
      position: { ...POSITION, newItemsPerDay: 10 },
      typeOf: (id) => (id === 11 ? "Vocabulary" : "List"),
    }), s.writer);

    expect(s.positionen[0].dto.newItemsPerDay).toBe(10);
    expect(s.positionen[1].dto.newItemsPerDay).toBe(0);
    // Gefiltert wird nur die Dosis: der Kasten bleibt, wie der Feinschliff ihn setzt.
    expect(s.positionen[1].dto.useLeitner).toBe(true);
  });
});

// B-480: der Überblick nennt eine Tagesdosis nur, wo sie eine Position bekommt. Rot, wenn „höchstens 10 … (außer Listen)"
// stehen bleibt, obwohl jede gewählte Übung eine Liste ist (keine Position bekommt dann eine Dosis).
describe("Überblick – Tagesdosis", () => {
  it("nennt die Dosis nur, wenn eine Nicht-Liste sie bekommt", () => {
    expect(dosisZeile(true, 10, ["Vocabulary", "List"])).toMatch(/^höchstens 10/);
    expect(dosisZeile(true, 10, ["List", "List"])).toMatch(/^keine – Listen bekommen keine Tagesdosis/);
    expect(dosisZeile(false, 10, ["Vocabulary"])).toBe("keine – alle Inhalte ab dem ersten Tag");
    expect(dosisZeile(true, 0, ["Vocabulary"])).toBe("keine – alle Inhalte ab dem ersten Tag");
  });
});

/*
 * B-423: das Ziel „Klassenarbeit" leitet aus dem Termin Planende, Stufenleiter und die Klassenarbeit selbst ab.
 */
describe("Klassenarbeit – Ableitungen (B-423)", () => {
  // AK 3. Rot, wenn `stageScheduleFor` den Schwellentag falsch rundet (`floor` statt `ceil`: 7 → 3, 14 → 5; ohne `+ 1`: 3, 3, 5).
  it("legt die Tipp-Stufe nach dem ersten Drittel: n = 7, 9, 14 → Tag 4, 4, 6", () => {
    expect(stageScheduleFor(7, 4)).toEqual([{ dayNumber: 1, stage: 2 }, { dayNumber: 4, stage: 4 }]);
    expect(stageScheduleFor(9, 4)[1].dayNumber).toBe(4);
    expect(stageScheduleFor(14, 5)).toEqual([{ dayNumber: 1, stage: 2 }, { dayNumber: 6, stage: 5 }]);
  });

  // Rot, wenn ein geänderter Schwellentag Tag 1 überschreibt (dann begänne der Plan getippt) oder hinter das Planende fiele.
  it("klemmt einen geänderten Schwellentag in den Plan", () => {
    expect(stageScheduleFor(7, 4, 1)[1].dayNumber).toBe(2);
    expect(stageScheduleFor(7, 4, 30)[1].dayNumber).toBe(7);
    expect(stageScheduleFor(7, 4, 5)[1].dayNumber).toBe(5);
  });

  // AK 1. Rot, wenn der leere Termin durchgeht oder die Grenze um einen Tag verrutscht (`< heute + 1`, `<= heute + 2`).
  it("verlangt einen Termin frühestens übermorgen", () => {
    expect(terminProblem("", "2026-11-14")).toBe("Termin fehlt.");
    expect(terminProblem("2026-11-15", "2026-11-14")).toMatch(/^Frühestens am 16\.11\.2026/);
    expect(terminProblem("2026-11-16", "2026-11-14")).toBeNull();
  });

  // AK 2. Rot, wenn die Dauer bis zum Termin statt bis zum Vortag reicht (`+ 1`) – dann endete der Plan am Tag der Arbeit.
  it("lässt den Plan heute beginnen und am Vortag des Termins enden", () => {
    const v = planVorgaben("Klassenarbeit", { startDate: "2026-11-20", durationDays: 30 }, "2026-11-14", "2026-11-22", 4, "");
    // Ende = Start + Dauer − 1 (StudyPlansController) = 14. + 8 − 1 = 21.11.
    expect(v).toEqual({
      startDate: "2026-11-14", durationDays: 8, klassenarbeitTermin: "2026-11-22",
      stageSchedule: [{ dayNumber: 1, stage: 2 }, { dayNumber: 4, stage: 4 }],
    });
  });

  // AK 6. Rot, wenn die Ableitung nicht am Ziel hängt – dann bekämen „Aufholen"/„Regelmäßig" Leiter, Arbeit und Termin-Dauer.
  it("lässt die Ziele „Aufholen“ und „Regelmäßig“ unverändert", () => {
    for (const ziel of ["Aufholen", "Regelmaessig"]) {
      expect(planVorgaben(ziel, { startDate: "2026-11-20", durationDays: 30 }, "2026-11-14", "2026-11-22", 4, ""))
        .toEqual({ startDate: "2026-11-20", durationDays: 30, stageSchedule: null, klassenarbeitTermin: null });
    }
  });

  // AK 7. Rot, wenn der Satz den Tag der Schwelle statt den Vortag nennt oder den Klammerzusatz mitschleppt.
  it("liest die Leiter als Satz", () => {
    const labels: Record<number, string> = { 2: "Selbsteinschätzung", 4: "Freitext (tippen)" };
    expect(leiterText([{ dayNumber: 4, stage: 4 }, { dayNumber: 1, stage: 2 }], (s) => labels[s]))
      .toBe("Selbsteinschätzung bis Tag 3, dann Freitext");
    expect(leiterText(null, (s) => labels[s])).toBeNull();
  });
});

describe("runWizardFinish – Klassenarbeit und Leiter (B-423)", () => {
  const LEITER = [{ dayNumber: 1, stage: 2 }, { dayNumber: 4, stage: 4 }];
  const ARBEIT = { title: "Englisch-Arbeit", topic: "Unit 2", subjectId: 3, scheduledDate: "2026-11-22" };

  // AK 3 + AK 8. Rot, wenn `wizardFinish` das Häkchen ungeprüft in jede Position schreibt (dann trägt die Vokabel-Position
  // `requireTypedTest: true`) oder die Leiter an die Zuordnung hängt.
  it("gibt der Vokabel-Position die Leiter ohne „nur getippt“, der anderen das Häkchen", async () => {
    const s = schreiber();
    await runWizardFinish(newWizardProgress(), auftrag({
      exerciseIds: [11, 12], stageSchedule: LEITER,
      position: { ...POSITION, requireTypedTest: true },
      typeOf: (id) => (id === 11 ? "Vocabulary" : "Matching"),
    }), s.writer);

    expect(s.positionen[0].dto.stageSchedule).toEqual(LEITER);
    expect(s.positionen[0].dto.requireTypedTest).toBe(false);
    expect(s.positionen[1].dto).not.toHaveProperty("stageSchedule");
    expect(s.positionen[1].dto.requireTypedTest).toBe(true);
  });

  // Gegenprobe zu AK 8: ohne Leiter bleibt das Häkchen auch an der Vokabel-Position. Rot, wenn der Schnitt immer greift.
  it("lässt das Häkchen an Vokabel-Positionen ohne Leiter stehen", async () => {
    const s = schreiber();
    await runWizardFinish(newWizardProgress(), auftrag({ exerciseIds: [11] }), s.writer);
    expect(s.positionen[0].dto.requireTypedTest).toBe(true);
    expect(s.positionen[0].dto).not.toHaveProperty("stageSchedule");
  });

  // AK 4. Rot, wenn die Arbeit ohne Übungen oder je Übung einzeln angelegt wird (Entscheidung 6: ein Aufruf).
  it("legt die Klassenarbeit mit einem Aufruf samt aller Übungen an", async () => {
    const s = schreiber();
    await runWizardFinish(newWizardProgress(), auftrag({ newChild: null, existingChildId: 42, classTest: ARBEIT }), s.writer);
    expect(s.arbeiten).toEqual([{ ...ARBEIT, childId: 42, exerciseIds: [11, 12] }]);
  });

  // AK 5. Rot, wenn ein Fehler der Arbeit als gewöhnlicher Fehler kommt (dann fehlt der Link) oder ein zweiter Anlauf den
  // Plan erneut anlegt (Risiko 3).
  it("meldet die fehlende Arbeit mit Plan-Id, und ein zweiter Anlauf legt nur sie an", async () => {
    const kaputt = schreiber({ bei: "classTest" });
    const progress = newWizardProgress();
    const eingabe = auftrag({ newChild: null, existingChildId: 42, classTest: ARBEIT });

    const fehler = await runWizardFinish(progress, eingabe, kaputt.writer).catch((e: unknown) => e);
    expect(fehler).toBeInstanceOf(KlassenarbeitFehlt);
    expect((fehler as KlassenarbeitFehlt).planId).toBe(201);
    expect((fehler as KlassenarbeitFehlt).childId).toBe(42);

    const heil = schreiber();
    expect(await runWizardFinish(progress, eingabe, heil.writer)).toBe(201);
    expect(heil.plaene).toHaveLength(0);
    expect(heil.positionen).toHaveLength(0);
    expect(heil.arbeiten).toHaveLength(1);
  });

  // AK 6. Rot, wenn ohne Ziel „Klassenarbeit" trotzdem eine Arbeit entsteht.
  it("legt ohne Klassenarbeit-Auftrag keine Arbeit an", async () => {
    const s = schreiber();
    await runWizardFinish(newWizardProgress(), auftrag(), s.writer);
    expect(s.arbeiten).toHaveLength(0);
  });
});

describe("Klassenarbeit – Review B-423", () => {
  // Fund 1. Rot, wenn der zweite Anlauf die Arbeit aus der aktuellen Eingabe baut (`input.classTest`): dann stünde sie am
  // neuen Termin, während der Plan schon am alten endet.
  it("legt die Arbeit beim zweiten Anlauf am Termin des Plans an, nicht an einem geänderten", async () => {
    const progress = newWizardProgress();
    const alt = { title: "Englisch-Arbeit", topic: null, subjectId: 3, scheduledDate: "2026-11-22" };
    const kaputt = schreiber({ bei: "classTest" });
    await runWizardFinish(progress, auftrag({ newChild: null, existingChildId: 42, classTest: alt }), kaputt.writer).catch(() => {});

    const heil = schreiber();
    await runWizardFinish(progress, auftrag({ newChild: null, existingChildId: 42,
      classTest: { ...alt, scheduledDate: "2026-12-01" } }), heil.writer);
    expect(heil.arbeiten[0].scheduledDate).toBe("2026-11-22");
  });

  // Fund 2 (Entscheidung 7). Rot, wenn die Leiter auch bei Stufe ≤ 2 entsteht (`defaultStage > 2` fehlt): dann wäre sie
  // ein Schritt von 2 auf 2 bzw. ein nicht testbarer Abschnitt auf „Nur anzeigen".
  it("baut keine Leiter, wenn die gewählte Stufe nicht über der Selbsteinschätzung liegt", () => {
    for (const stufe of [1, 2]) {
      expect(planVorgaben("Klassenarbeit", { startDate: "2026-11-14", durationDays: 8 }, "2026-11-14", "2026-11-22", stufe, "")
        .stageSchedule).toBeNull();
    }
    expect(planVorgaben("Klassenarbeit", { startDate: "2026-11-14", durationDays: 8 }, "2026-11-14", "2026-11-22", 6, "")
      .stageSchedule).toEqual([{ dayNumber: 1, stage: 2 }, { dayNumber: 4, stage: 6 }]);
  });

  // Fund 2. Rot, wenn der Satz die Stufe pauschal „getippt" nennt oder doppelte Klammern stehen lässt.
  it("nennt im Satz den Namen der zweiten Stufe", () => {
    const labels: Record<number, string> = { 2: "Selbsteinschätzung", 6: "Multiple-Choice (Auswahl)" };
    expect(leiterText([{ dayNumber: 1, stage: 2 }, { dayNumber: 4, stage: 6 }], (s) => labels[s]))
      .toBe("Selbsteinschätzung bis Tag 3, dann Multiple-Choice");
  });

  // Fund 3. Rot, wenn `planVorgaben` den Rohtext nicht liest (dann gilt immer die Vorgabe) bzw. Unlesbares nicht auf die
  // Vorgabe zurückfällt.
  it("liest den Schwellentag aus dem Rohtext und fällt bei Leerem auf die Vorgabe", () => {
    const tag = (text: string) => planVorgaben("Klassenarbeit", { startDate: "2026-11-14", durationDays: 8 }, "2026-11-01",
      "2026-11-22", 4, text).stageSchedule![1].dayNumber;
    expect(tag("12")).toBe(12);
    expect(tag("")).toBe(8);
    expect(tag("1")).toBe(2);
  });
});
