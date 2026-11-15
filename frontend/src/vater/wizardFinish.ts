import { errorMessage } from "../lib/api";
import type {
  CreateChildDto, CreateKlassenarbeitDto, CreatePlanDto, CreatePositionDto, StageStep,
} from "../lib/types";

/*
 * Der Abschluss des Lehrplan-Assistenten: Kind (optional), Plan und je Übung eine Position – mehrere
 * Schreibschritte, von denen jeder für sich schon Wirkung hat.
 *
 * Ausgelöst aus dem Bildschirm heraus, damit der Ablauf **einzeln** prüfbar ist. Er trägt die teuerste
 * Doppelklick-Stelle der App (zwei Klicks legten zwei Kinder samt zwei Lehrplänen an), und die Prüfung
 * dafür soll nicht `api.ts`, den Router und fünf Assistenten-Schritte hereinziehen müssen.
 */

/**
 * Was der Abschluss schon geschrieben hat – und ob er gerade läuft. Der Aufrufer hält das über einen
 * Durchgang hinweg (im Bildschirm ein `useRef`), denn beides wird **mitten** im Ablauf gelesen und
 * geschrieben: ein State-Update käme erst im nächsten Render an, und der laufende Durchgang sähe seinen
 * eigenen Fortschritt nicht.
 *
 * Zwei verschiedene Zwecke, die man nicht verwechseln darf: `childId`/`planId`/`positions` sichern die
 * **Wiederaufnahme nach einem Fehler** (sequenziell, ein zweiter Anlauf macht dort weiter, wo es hakte),
 * `running` den **Wiedereintritt** (nebenläufig, zwei Klicks im selben Tick).
 */
export interface WizardProgress {
  childId: number | null;
  planId: number | null;
  positions: number[];
  /** Die angelegte Klassenarbeit (B-423) – ein zweiter Anlauf nach einem Teilfehler legt sie nicht doppelt an. */
  classTestId: number | null;
  /**
   * Der Klassenarbeit-Auftrag, **mit dem der Plan entstand** (B-423, Review). Ein zweiter Anlauf nimmt ihn statt der
   * aktuellen Eingabe: sonst endete der Plan am alten Termin und die Arbeit stünde am neuen. `undefined` = noch kein Plan.
   */
  classTestAuftrag?: WizardFinishInput["classTest"];
  running: boolean;
}

/** Ein frischer Fortschritt – ein Durchgang je Assistent. */
export function newWizardProgress(): WizardProgress {
  return { childId: null, planId: null, positions: [], classTestId: null, running: false };
}

/** Was angelegt werden soll; der Bildschirm sammelt es aus seinen fünf Schritten. */
export interface WizardFinishInput {
  /** Das anzulegende Kind – oder `null`, wenn ein bestehendes gewählt wurde. */
  newChild: CreateChildDto | null;
  /** Das gewählte bestehende Kind; `null`, wenn eines angelegt wird. */
  existingChildId: number | null;
  /** Der Plan **ohne** `childId`: die entsteht unter Umständen erst hier. */
  plan: Omit<CreatePlanDto, "childId">;
  /** Je Übung entsteht eine Position. */
  exerciseIds: number[];
  /** Die Feinschliff-Werte, die für **alle** Positionen gelten. */
  position: Omit<CreatePositionDto, "exerciseId">;
  /** Titel einer Übung für die Fehlermeldung – die Server-Meldung sagt nicht, *welche* Übung es traf. */
  titleOf: (exerciseId: number) => string;
  /**
   * Typ einer Übung. Nötig, weil die „Test-Stufe" des Feinschliffs die **Vokabel-Skala** ist (`TestStage`)
   * und jeder Typ die Zahl anders liest: eine 2 heißt bei der Vokabel „Selbstcheck", bei der Zuordnung
   * „mit Ablenkern". Ungefiltert verwandelte das Ziel „Rückstand aufholen" (Stufe 2) jede gewählte
   * Zuordnung in Multiple-Choice, ohne dass die Oberfläche es zeigte.
   */
  typeOf: (exerciseId: number) => string;
  /**
   * Die Stufenleiter der Vokabel-Positionen (B-423) – `null`/weggelassen heißt „keine". Wie `stage` nur für
   * `Vocabulary`: die Zahlen sind die Vokabel-Skala.
   */
  stageSchedule?: StageStep[] | null;
  /**
   * Die Klassenarbeit, die nach dem Plan entsteht (B-423, Ziel „Klassenarbeit") – ohne `childId` und `exerciseIds`, die
   * kommen aus dem Durchgang. `null`/weggelassen: keine.
   */
  classTest?: Omit<CreateKlassenarbeitDto, "childId" | "exerciseIds"> | null;
}

/**
 * Plan und Positionen stehen, nur die Klassenarbeit fehlt (B-423, AK 5). Eigener Fehler, weil der Bildschirm dann
 * anders reagiert als bei einem gescheiterten Plan: er nennt die fehlende Arbeit mit Link und bietet den Plan an.
 */
export class KlassenarbeitFehlt extends Error {
  constructor(readonly planId: number, readonly childId: number, readonly ursache: string) {
    super(`Der Plan ist angelegt, aber die Klassenarbeit fehlt: ${ursache}`);
  }
}

/** Die drei Schreibzugriffe. Als Parameter, damit die Prüfung ohne `api.ts` und ohne `fetch` auskommt. */
export interface WizardWriter {
  createChild(dto: CreateChildDto): Promise<{ id: number }>;
  createPlan(dto: CreatePlanDto): Promise<{ id: number }>;
  addPosition(planId: number, dto: CreatePositionDto): Promise<unknown>;
  createClassTest(dto: CreateKlassenarbeitDto): Promise<{ klassenarbeit: { id: number } }>;
}

/**
 * Legt Kind (optional), Plan und Positionen an und liefert die Plan-Id.
 *
 * Liefert **`null`**, wenn schon ein Durchgang läuft – dann besitzt der erste das Ergebnis, und der
 * Aufrufer tut nichts (kein Fehler: der Nutzer hat nur zweimal geklickt).
 *
 * Wirft, wenn ein Schritt scheitert. Der Fortschritt bleibt dabei stehen, damit ein zweiter Anlauf
 * weitermacht statt zu verdoppeln – ein bereits angelegter Plan darf nicht doppelt entstehen, nur weil
 * die dritte Position eine leere Vokabelübung war.
 */
export async function runWizardFinish(
  progress: WizardProgress,
  input: WizardFinishInput,
  writer: WizardWriter,
): Promise<number | null> {
  /*
   * Die Wiedereintritts-Sperre, und sie muss **vor** dem ersten `await` stehen: `busy` als State steht
   * erst nach dem Re-Render am Knopf, zwei Klicks im selben Tick kamen darum beide bis zum `createChild`.
   */
  if (progress.running) return null;
  progress.running = true;
  try {
    let childId = progress.childId ?? input.existingChildId;
    if (input.newChild !== null && progress.childId === null) {
      childId = (await writer.createChild(input.newChild)).id;
      // Sofort vermerken – sonst legte ein zweiter Anlauf nach einem späteren Fehler ein zweites Kind an.
      progress.childId = childId;
    }
    if (childId === null) throw new Error("Kein Kind gewählt.");

    let planId = progress.planId;
    if (planId === null) {
      planId = (await writer.createPlan({ ...input.plan, childId })).id;
      progress.planId = planId;
      progress.classTestAuftrag = input.classTest ?? null;
    }

    for (const exerciseId of input.exerciseIds) {
      if (progress.positions.includes(exerciseId)) continue;
      try {
        /*
         * Die Stufe nur dort mitschicken, wo sie definiert ist – dasselbe Gate wie in
         * `VaterExerciseCreate`/`ExerciseEditModal` (`type === "Vocabulary"`). Weggelassen entscheidet der
         * Server (Übungs-Vorgabe, sonst Typ-Vorgabe), statt eine fremde Skala falsch zu deuten.
         */
        const type = input.typeOf(exerciseId);
        const { stage, ...withoutStage } = input.position;
        const leiter = input.stageSchedule ?? null;
        const dto: CreatePositionDto = type !== "Vocabulary"
          ? { ...withoutStage, exerciseId }
          /*
           * Mit Leiter zählt der selbst eingeschätzte Test der ersten Phase (B-423, Entscheidung 5): der Test auf Stufe 2
           * ist ungewertet, und `requireTypedTest` machte das Tagesziel dort unerreichbar – der Malus liefe gegen ein Kind,
           * das nichts falsch gemacht hat. Die Strenge der zweiten Phase legt der Server über die Leiter fest.
           */
          : leiter !== null
            ? { ...input.position, stageSchedule: leiter, requireTypedTest: false, exerciseId }
            : { ...input.position, exerciseId };
        /*
         * Eine Liste kann als Menge werten (ungeordnet), und dort lehnt der Server jede Tagesdosis ab (B-04,
         * Entscheidung 9): die Dosis friert einen Teil der Einträge ein, eine richtige Nennung außerhalb davon
         * zählte falsch. Ob sie geordnet ist, steht nicht in der Übersicht – darum gilt es für jede Liste (B-477).
         */
        if (type === "List") dto.newItemsPerDay = 0;
        await writer.addPosition(planId, dto);
      } catch (err) {
        /*
         * Den Titel mitgeben: der Plan ist an dieser Stelle **schon angelegt**, und die Server-Meldung
         * spricht von „dieser Übung", ohne zu sagen welcher. Bei zehn gewählten Übungen wäre der Nutzer
         * damit allein – am häufigsten trifft es eine noch nicht gefüllte Vokabelübung (`exercise_empty`).
         */
        const title = input.titleOf(exerciseId);
        throw new Error(`„${title}": ${errorMessage(err)} Der Plan ist angelegt – nimm die Übung ab und versuche es erneut.`);
      }
      progress.positions.push(exerciseId);
    }

    // Erst nach allen Positionen: die Arbeit trägt die Übungen des Plans, und ein Teilfehler davor bricht ohnehin ab.
    // Der Auftrag aus dem Durchgang, der den Plan anlegte – nicht die Eingabe dieses Anlaufs (siehe `classTestAuftrag`).
    const auftrag = progress.classTestAuftrag !== undefined ? progress.classTestAuftrag : input.classTest;
    if (auftrag && progress.classTestId === null) {
      try {
        // Ein Aufruf samt Übungen (Entscheidung 6) – kein `POST …/exercises` je Übung.
        progress.classTestId = (await writer.createClassTest({
          ...auftrag, childId, exerciseIds: input.exerciseIds,
        })).klassenarbeit.id;
      } catch (err) {
        throw new KlassenarbeitFehlt(planId, childId, errorMessage(err));
      }
    }
    return planId;
  } finally {
    // Ohne das `finally` bliebe der Assistent nach dem ersten Fehler tot – und AK 2 verlangt genau den
    // zweiten Anlauf.
    progress.running = false;
  }
}

/**
 * Die Zeile „Tagesdosis" im Überblick des Assistenten – aus derselben Regel wie das Anlegen oben: eine Liste bekommt keine
 * Dosis. Sind alle gewählten Übungen Listen, bekommt keine Position eine, und „höchstens 10 … (außer Listen)" wäre eine
 * Zusage über nichts (B-480).
 */
export function dosisZeile(useLeitner: boolean, newItemsPerDay: number, types: string[]): string {
  if (!useLeitner || newItemsPerDay <= 0) return "keine – alle Inhalte ab dem ersten Tag";
  if (types.length > 0 && types.every((t) => t === "List"))
    return "keine – Listen bekommen keine Tagesdosis, alle Einträge ab dem ersten Tag";
  return `höchstens ${newItemsPerDay} neue Inhalte je Tag und Übung (außer Listen)`;
}

/** Tage seit 1970 für einen `DateOnly` („2026-11-14") – als Text zerlegt, ohne Uhrzeit und Zeitzone (B-423, Risiko 2). */
export function tagNummer(iso: string): number {
  const [j, m, t] = iso.split("-").map(Number);
  return Math.round(Date.UTC(j, m - 1, t) / 86_400_000);
}

/** `iso` plus `tage` Kalendertage, wieder als `DateOnly`-Text. */
export function plusTage(iso: string, tage: number): string {
  return new Date((tagNummer(iso) + tage) * 86_400_000).toISOString().slice(0, 10);
}

/**
 * Warum der Termin der Klassenarbeit nicht taugt (B-423, AK 1) – `null` heißt: taugt. Der Plan läuft von heute bis zum
 * Vortag, braucht also mindestens zwei Tage; ein Termin morgen ließe einen Plan von null Tagen.
 */
export function terminProblem(termin: string, heute: string): string | null {
  if (termin === "") return "Termin fehlt.";
  const fruehestens = plusTage(heute, 2);
  return tagNummer(termin) < tagNummer(fruehestens)
    ? `Frühestens am ${fruehestens.split("-").reverse().join(".")} – der Plan braucht mindestens zwei Tage vor der Arbeit.`
    : null;
}

/** Was das Ziel aus Start, Dauer und Termin macht (B-423). Nur „Klassenarbeit" leitet ab; die anderen Ziele bleiben frei (AK 6). */
export interface PlanVorgaben {
  startDate: string;
  durationDays: number;
  /** Die Leiter der Vokabel-Positionen, `null` = keine. */
  stageSchedule: StageStep[] | null;
  /** Der Termin der anzulegenden Klassenarbeit, `null` = keine. */
  klassenarbeitTermin: string | null;
}

/**
 * Start, Dauer, Leiter und Termin aus dem Ziel (B-423, Entscheidungen 1–2). Beim Ziel „Klassenarbeit" startet der Plan
 * heute und endet am Vortag des Termins (Ende = Start + Dauer − 1, `StudyPlansController`), also Dauer = Termin − heute;
 * `schwelleText` ist der im Feinschliff getippte Tag der zweiten Stufe, als Rohtext (leer = Vorgabe) – geklemmt wird erst
 * hier, sonst sprünge eine zweistellige Eingabe schon nach der ersten Ziffer auf die Grenze.
 *
 * Eine Leiter gibt es nur, wenn die gewählte Stufe **über** der Selbsteinschätzung liegt (Entscheidung 7): bei Stufe 2
 * wäre sie ein Schritt von 2 auf 2, bei Stufe 1 („Nur anzeigen") ein nicht testbarer zweiter Abschnitt.
 */
export function planVorgaben(ziel: string, frei: { startDate: string; durationDays: number }, heute: string,
  termin: string, defaultStage: number, schwelleText: string): PlanVorgaben {
  if (ziel !== "Klassenarbeit") return { ...frei, stageSchedule: null, klassenarbeitTermin: null };
  const durationDays = Math.max(1, tagNummer(termin) - tagNummer(heute));
  const schwelle = /^\d+$/.test(schwelleText.trim()) ? Number(schwelleText.trim()) : null;
  return {
    startDate: heute, durationDays,
    stageSchedule: defaultStage > 2 ? stageScheduleFor(durationDays, defaultStage, schwelle) : null,
    klassenarbeitTermin: termin,
  };
}

/** Der Vorgabe-Tag, ab dem getippt wird: nach dem ersten Drittel des Plans (B-423, Entscheidung 2). */
export function leiterSchwelle(n: number): number {
  return Math.ceil(n / 3) + 1;
}

/**
 * Die Stufenleiter einer Vokabel-Position (B-423, Entscheidung 2): erstes Drittel Selbsteinschätzung (Stufe 2), danach
 * die gewählte Tipp-Stufe. Kein Tag auf „Nur anzeigen" (Stufe 1): die Stufe ist nicht testbar, das Tagesziel risse ohne
 * Test. Ein geänderter Schwellentag wird in den Plan geklemmt (2 … n) – Tag 1 gehört immer der Selbsteinschätzung.
 */
export function stageScheduleFor(n: number, defaultStage: number, schwelle: number | null = null): StageStep[] {
  const tag = Math.min(Math.max(schwelle ?? leiterSchwelle(n), 2), Math.max(n, 2));
  return [{ dayNumber: 1, stage: 2 }, { dayNumber: tag, stage: defaultStage }];
}

/**
 * Die Leiter als Satz (B-423, AK 7): „Selbsteinschätzung bis Tag 3, dann Freitext". Eine Fassung für Feinschliff und
 * Plan-Detailseite – zwei Formulierungen derselben Leiter würden zwei Bedeutungen. Der Klammerzusatz der Stufennamen
 * („Freitext (tippen)") fällt weg, er erklärt die Stufe, nicht die Leiter. `null` = keine Leiter.
 */
export function leiterText(schedule: readonly StageStep[] | null | undefined,
  labelOf: (stage: number) => string | undefined): string | null {
  if (!schedule || schedule.length === 0) return null;
  const stufen = [...schedule].sort((a, b) => a.dayNumber - b.dayNumber);
  const name = (stage: number) => (labelOf(stage) ?? `Stufe ${stage}`).replace(/\s*\(.*\)$/, "");
  return stufen
    .map((s, i) => (i < stufen.length - 1 ? `${name(s.stage)} bis Tag ${stufen[i + 1].dayNumber - 1}` : name(s.stage)))
    .join(", dann ");
}
