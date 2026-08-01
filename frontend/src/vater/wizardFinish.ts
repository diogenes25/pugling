import { errorMessage } from "../lib/api";
import type { CreateChildDto, CreatePlanDto, CreatePositionDto } from "../lib/types";

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
  running: boolean;
}

/** Ein frischer Fortschritt – ein Durchgang je Assistent. */
export function newWizardProgress(): WizardProgress {
  return { childId: null, planId: null, positions: [], running: false };
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
}

/** Die drei Schreibzugriffe. Als Parameter, damit die Prüfung ohne `api.ts` und ohne `fetch` auskommt. */
export interface WizardWriter {
  createChild(dto: CreateChildDto): Promise<{ id: number }>;
  createPlan(dto: CreatePlanDto): Promise<{ id: number }>;
  addPosition(planId: number, dto: CreatePositionDto): Promise<unknown>;
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
    }

    for (const exerciseId of input.exerciseIds) {
      if (progress.positions.includes(exerciseId)) continue;
      try {
        await writer.addPosition(planId, { ...input.position, exerciseId });
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
    return planId;
  } finally {
    // Ohne das `finally` bliebe der Assistent nach dem ersten Fehler tot – und AK 2 verlangt genau den
    // zweiten Anlauf.
    progress.running = false;
  }
}
