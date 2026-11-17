import { useEffect, useId, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { pinProblem } from "../lib/pin";
import { useAsync } from "../lib/useAsync";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { useAuth } from "../lib/auth";
import { SCHOOL_TYPES } from "../lib/labels";
import { TruncationHint } from "../components/ListControls";
import { FieldLabel, InfoHint } from "../components/InfoHint";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { useGemerkterFehler } from "../lib/useGemerkterFehler";
import { ListenAusfall } from "../components/ListenAusfall";
import { Pflicht } from "../components/Pflicht";
import { authorText } from "./ExerciseAttribution";
import { ratingSummary } from "./ExerciseRatingPanel";
import {
  auswahlNachFilterwechsel, NICHT_ZUWEISBAR_GRUND, nurZuweisbare, unsichtbareAuswahl, wizardFilterKey, wizardSearchParams,
} from "./wizardSearch";
import {
  dosisZeile, haekchenZeile, KlassenarbeitFehlt, leiterText, newWizardProgress, planVorgaben, plusTage, runWizardFinish,
  terminProblem,
} from "./wizardFinish";
import type { WizardProgress } from "./wizardFinish";
import { PlanUnitSelect } from "./PlanUnitSelect";
import { radioGroupKeys } from "../lib/ui";
import { formatDate } from "../lib/datum";
import type {
  CategoryResponse, ChildResponse, ExerciseSummary, GrammarTopicResponse, Paged, SchoolType, SubjectResponse,
} from "../lib/types";

/*
 * Lehrplan-Assistent: führt den Vater in fünf Schritten von „welches Kind" über einen kurzen
 * Fragenkatalog (Problemfeld, Ziel, Intensität) und die Auswahl vorhandener Katalog-Übungen bis zum
 * fertigen Lehrplan. Der Plan ist ein Container; für jede gewählte Übung legt der Assistent eine
 * Position mit Ziel/Stufe/Punkten an (aus Ziel + Intensität abgeleitet, im Feinschliff überschreibbar).
 * Der manuelle Weg (Dashboard → Neuer Plan → Positionen) bleibt daneben bestehen.
 */

type Goal = "Klassenarbeit" | "Aufholen" | "Regelmaessig";
type Intensity = "Locker" | "Normal" | "Intensiv";

/*
 * Intensität → Bestehensgrenze, Punkte je erreichtem Ziel und **Münz-Malus** für eine gerissene Pflicht.
 * Der Malus ist der „Stick" des Produkts: ohne ihn ist ein Pflichtziel bloß ein Angebot. Er steigt mit der
 * Intensität, bleibt aber bei „Locker" bewusst bei 0 – reine Belohnung, wenn der Vater sanft anfangen will.
 */
const INTENSITY: Record<Intensity, { pass: number; points: number; penalty: number; label: string; hint: string }> = {
  Locker: { pass: 70, points: 10, penalty: 0, label: "Locker", hint: "Bestehen ab 70 % · 10 Punkte/Ziel · kein Malus" },
  Normal: { pass: 80, points: 20, penalty: 5, label: "Normal", hint: "Bestehen ab 80 % · 20 Punkte/Ziel · −5 🪙 bei Versäumnis" },
  Intensiv: { pass: 90, points: 30, penalty: 10, label: "Intensiv", hint: "Bestehen ab 90 % · 30 Punkte/Ziel · −10 🪙 bei Versäumnis" },
};

const GOALS: Record<Goal, { label: string; emoji: string; hint: string; duration: number; stage: number; typed: boolean }> = {
  Klassenarbeit: { label: "Klassenarbeit vorbereiten", emoji: "📝", hint: "Kurzer, strammer Plan mit getipptem Test", duration: 14, stage: 4, typed: true },
  Aufholen: { label: "Rückstand aufholen", emoji: "🪜", hint: "Mehrere Wochen dranbleiben", duration: 21, stage: 2, typed: false },
  Regelmaessig: { label: "Regelmäßig üben", emoji: "🔁", hint: "Dauerhaftes, entspanntes Training", duration: 30, stage: 2, typed: false },
};

const STEPS = ["Kind", "Problemfeld", "Übungen", "Feinschliff", "Überblick"] as const;

function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export function VaterWizard() {
  const { session } = useAuth();
  const nav = useNavigate();
  const children = useAsync<ChildResponse[]>(() => api.children(), [session!.id]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const [step, setStep] = useState(0);

  // --- Schritt 1: Kind ---
  const [mode, setMode] = useState<"existing" | "new">("new");
  const [childId, setChildId] = useState<number | "">("");
  const [newName, setNewName] = useState("");
  const [newBirthYear, setNewBirthYear] = useState("");
  const [grade, setGrade] = useState<number | "">("");
  const [schoolType, setSchoolType] = useState<SchoolType>("Gymnasium");
  const [newPin, setNewPin] = useState("");

  // --- Schritt 2: Fragenkatalog ---
  const [subjectId, setSubjectId] = useState<number | "">("");
  // Optional die Unit, die der Plan durcharbeitet (B-539); sie gehört zum gewählten Fach.
  const [seriesUnitId, setSeriesUnitId] = useState<number | "">("");
  const [topic, setTopic] = useState("");
  const [goal, setGoal] = useState<Goal>("Klassenarbeit");
  const [intensity, setIntensity] = useState<Intensity>("Normal");
  // B-423: beim Ziel „Klassenarbeit" ist der Termin Pflicht – Planende, Leiter und die Arbeit selbst folgen daraus.
  const [termin, setTermin] = useState("");

  // --- Schritt 3: Übungen (Katalog) ---
  const [contentSearch, setContentSearch] = useState("");
  // Drei weitere Filter, die der Server längst kann und der Assistent bisher nicht anbot (B-18). Alle
  // optional und AND-verknüpft wie serverseitig; leer heißt „alle".
  const [categoryId, setCategoryId] = useState<number | "">("");
  const [typeKey, setTypeKey] = useState("");
  const [sourceSearch, setSourceSearch] = useState("");
  // Die achte Facette (B-155): ein Thema des gewählten Fachs; wie die Art fällt sie mit dem Fachwechsel.
  const [grammarTopicId, setGrammarTopicId] = useState<number | "">("");
  // B-408: „beste Bewertung zuerst" wie im Planbau – eine Reihenfolge, kein Suchkriterium (verwirft die Auswahl nicht).
  const [besteZuerst, setBesteZuerst] = useState(false);
  const [selected, setSelected] = useState<number[]>([]);
  /** Sagt, dass ein Filterwechsel die Auswahl verworfen hat – nur wenn wirklich etwas wegfiel (B-161). */
  const [auswahlHinweis, setAuswahlHinweis] = useState<string | null>(null);

  // --- Schritt 4: Feinschliff (Positions-Defaults) ---
  const [title, setTitle] = useState("");
  const [startDate, setStartDate] = useState(todayIso());
  const [durationDays, setDurationDays] = useState(14);
  const [passPercent, setPassPercent] = useState(80);
  const [pointsGoalMet, setPointsGoalMet] = useState(20);
  const [penaltyCoins, setPenaltyCoins] = useState(5);
  const [defaultStage, setDefaultStage] = useState(4);
  const [requireTyped, setRequireTyped] = useState(true);
  const [useLeitner, setUseLeitner] = useState(true);
  const [comboThreshold, setComboThreshold] = useState(5);
  const [comboBonusPoints, setComboBonusPoints] = useState(5);
  // B-04: a dose by default, so a large exercise becomes a daily portion instead of one first day with everything.
  const [newItemsPerDay, setNewItemsPerDay] = useState(10);
  // B-423: der im Feinschliff getippte Tag der zweiten Stufe – Rohtext, geklemmt wird in `planVorgaben` (leer = Vorgabe).
  const [schwelleText, setSchwelleText] = useState("");
  const [touchedFineTune, setTouchedFineTune] = useState(false);
  // „Der Vater hat die Kind-Art selbst gewählt" – dieselbe Idee wie `touchedFineTune`: eine Vorbelegung
  // darf nur greifen, solange niemand entschieden hat.
  const [touchedMode, setTouchedMode] = useState(false);

  const [error, setError] = useState<string | null>(null);
  // B-423, AK 5: Plan steht, die Klassenarbeit nicht – eigener Kasten mit Links statt der Fehlerzeile.
  const [klassenarbeitFehlt, setKlassenarbeitFehlt] = useState<KlassenarbeitFehlt | null>(null);
  const [busy, setBusy] = useState(false);
  // B-424: die Übung, die der Vater gerade ausprobiert – genau eine, der Dialog liegt über dem ganzen Assistenten.
  const [vorschau, setVorschau] = useState<{ id: number; title: string } | null>(null);
  /*
   * Was beim Abschluss schon geschrieben wurde, plus die Wiedereintritts-Sperre. Ein Ref, nicht State: es
   * wird mitten im Ablauf gelesen und geschrieben, und ein State-Update käme erst im nächsten Render an –
   * der laufende Durchgang würde seinen eigenen Fortschritt nicht sehen und Positionen doppelt anlegen.
   * Was daran welchen Fall abdeckt, steht bei `WizardProgress`.
   */
  const progress = useRef<WizardProgress>(newWizardProgress());

  const selectedChild = children.data?.find((c) => c.id === childId);
  const effectiveGrade = mode === "new" ? (grade === "" ? undefined : Number(grade)) : selectedChild?.grade ?? undefined;
  const effectiveSchoolType: SchoolType | undefined =
    mode === "new" ? schoolType : (selectedChild?.schoolType as SchoolType | undefined);
  const subject = subjects.data?.find((s) => s.id === subjectId);

  /*
   * Was das Ziel aus Start, Dauer und Termin macht (B-423) – abgeleitet, nicht in den State geschrieben: so gilt es auch,
   * wenn der Vater den Feinschliff schon angefasst hat (`applyDefaults` schweigt dann) oder danach das Ziel wechselt.
   */
  const heute = todayIso();
  const istKlassenarbeit = goal === "Klassenarbeit";
  const terminGrund = istKlassenarbeit ? terminProblem(termin, heute) : null;
  const vorgaben = planVorgaben(goal, { startDate, durationDays }, heute, termin, defaultStage, schwelleText);
  /*
   * Steht der Plan schon (ein Anlauf scheiterte danach), ist der Assistent nur noch ein Wiederholen: zurück ließe sich
   * Termin oder Auswahl ändern, aber Plan und Positionen sind geschrieben (B-423, Review). Gelesen beim Rendern – jeder
   * gescheiterte Anlauf setzt `busy` zurück und rendert damit neu.
   */
  const planSteht = progress.current.planId !== null;
  // Übungstypen kommen aus dem Server-Manifest, nie aus einer Tabelle im Frontend (frontend/CLAUDE.md) – auch die Namen
  // der Leiterstufen (Review B-423). Hier oben, weil die Leiter darunter schon gerechnet wird.
  const types = useExerciseTypes();
  const stufenName = (stage: number) => types.stages("Vocabulary").find((o) => o.value === stage)?.label;
  const leiter = leiterText(vorgaben.stageSchedule, stufenName);

  // Der Schlüssel über die Suchkriterien (B-161) – gebildet aus `wizardSearchParams`, also aus genau der Abfrage. Er ist
  // zugleich die Abhängigkeit der Suche (B-164): getrimmte Eingaben („abc " → "abc") sind dieselbe Suche, lösen keine
  // Abfrage aus und machen die gezeigten Zeilen nicht veraltet.
  const filterKey = subjectId === "" ? "" : wizardFilterKey({
    subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
    search: contentSearch, categoryId, type: typeKey, source: sourceSearch, grammarTopicId,
  });

  // Passende Katalog-Übungen (die Bausteine des Plans – jede wird zu einer Position). Die Gesamtzahl wird
  // mitgeführt, weil der Server nur eine Seite liefert: ohne sie wäre eine gekappte Liste nicht erkennbar.
  const exercises = useAsync<Paged<ExerciseSummary>>(
    // Gefiltert wird SERVERSEITIG, nicht über der geladenen Seite: sonst durchsuchten die Felder nur die
    // erste Seite und meldeten „nichts gefunden", obwohl es Treffer gibt. Die Abbildung steht in
    // `wizardSearch.ts`, weil „Alle wählen" dieselbe braucht.
    () => (subjectId === "" ? Promise.resolve({ items: [], total: 0 })
      : api.searchExercises(wizardSearchParams({
        subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
        search: contentSearch, categoryId, type: typeKey, source: sourceSearch, grammarTopicId, besteZuerst,
      }))),
    // Die Reihenfolge lädt neu, gehört aber nicht in `filterKey` – sonst verwürfe Umsortieren die Auswahl (B-408).
    [filterKey, besteZuerst],
  );

  // Arten des gewählten Fachs; die Liste hängt am Fach, nicht am Katalog insgesamt.
  const categories = useAsync<CategoryResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.categories(Number(subjectId))), [subjectId]);
  // Grammatik-Themen des gewählten Fachs (B-155); die Liste hängt am Fach wie die Arten.
  const grammarTopics = useAsync<GrammarTopicResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.grammarTopics(Number(subjectId))), [subjectId]);

  /*
   * Erstes Kind vorwählen; wenn schon Kinder existieren, „bestehendes" als Standard.
   *
   * `touchedMode` ist die tragende Bedingung, nicht Kosmetik: Der Effekt feuert, sobald die Kinderliste
   * ankommt — und das kann NACH dem ersten Klick des Vaters sein. Ohne die Sperre nähme er ihm dann
   * „Neues Kind anlegen" wieder weg, während er schon tippt. Der bisherige Wächter (`childId === ""`)
   * konnte das nicht verhindern: Genau so sieht der Zustand nach dem bewussten Klick aus.
   *
   * Aufgefallen ist es beim Bauen von B-18, weil zwei weitere Ladevorgänge auf derselben Seite das Rennen
   * verschoben haben — der Defekt ist älter, `e2e/assistent.spec.ts:30-31` beschreibt ihn als Eigenart.
   */
  useEffect(() => {
    if (touchedMode) return;
    if (children.data && children.data.length > 0) {
      if (mode === "new" && childId === "") { setMode("existing"); setChildId(children.data[0].id); }
    }
  }, [children.data]); // eslint-disable-line react-hooks/exhaustive-deps

  // Gefiltert wird serverseitig (siehe `search` oben) – hier steht nur noch, was angekommen ist.
  const filteredExercises = exercises.data?.items ?? [];

  /*
   * B-161: Die Auswahl fällt, sobald sich ein Suchkriterium ändert.
   *
   * Vorher überlebte sie jeden Filterwechsel, und weil „Alle wählen" bis zu 500 Ids schreibt, während nur
   * die geladene Seite gerendert wird, konnte der Plan Positionen tragen, die der gezeigte Filter
   * ausschließt – mit Pflichtziel, also mit Münz-Malus fürs Kind. `selected` bedeutete „aus dieser
   * Trefferliste gewählt" UND „irgendwann früher gewählt".
   *
   * Der Hinweis erscheint NUR, wenn wirklich etwas verloren ging: sonst begrüßte der Assistent den Vater
   * mit „Auswahl zurückgesetzt", bevor er etwas getan hat. Stumm zu leeren wäre die andere Falle (B-116) –
   * eine Änderung ohne Rückmeldung liest sich als Fehler.
   *
   * Verglichen wird `filterKey` (oben, B-161), kein Objekt: ein Vergleich per Referenz feuerte bei jedem Rendern.
   */
  /*
   * Der Schlüssel, für den die aktuelle Auswahl gilt. Nach dem Effekt trägt die Ref immer den *aktuellen*
   * Schlüssel – „vorig" ist sie nur innerhalb des Effektrumpfs. Sie ist damit auch das Generationen-Gate
   * für `selectAll`: dessen Nachschlag darf nur zurückschreiben, wenn seither niemand die Suche geändert
   * hat.
   */
  const geltenderFilterKey = useRef(filterKey);
  /*
   * Ob die **gerenderten Zeilen** zu den eingestellten Kriterien gehören: `useAsync` behält `data` über den Wechsel
   * (B-116, gewollt), also stehen die Zeilen des vorigen Filters für die Dauer einer Abfrage noch da. Ein Haken darin
   * gehörte zu einem Filter, der nicht mehr gilt, und der Effekt oben leert ihn nie wieder (B-169). Das sagt seit B-164
   * der Hook selbst (`stale`) – und nicht `loading`: „Erneut suchen" ruft `exercises.reload()` (B-162), und dieselbe
   * Suche neu geladen darf frische Zeilen nicht sperren (B-199). Scheitert die neue Abfrage, bleiben die alten Zeilen
   * gesperrt.
   */
  const zeilenVeraltet = exercises.stale;
  useEffect(() => {
    // `selected` bewusst nicht in der Abhängigkeitsliste: der Effekt soll auf den Kriterien-Wechsel
    // reagieren, nicht auf jedes Anklicken. Gelesen wird der Stand zum Zeitpunkt des Wechsels.
    const folge = auswahlNachFilterwechsel(selected, geltenderFilterKey.current, filterKey);
    if (!folge) return;
    geltenderFilterKey.current = filterKey;
    setSelected(folge.selected);
    // Nur überschreiben, wenn es etwas zu melden gibt: ein zweiter Tastendruck im Suchfeld hat nichts mehr
    // zu verlieren und darf die Meldung des ersten nicht wegwischen.
    if (folge.hinweis) setAuswahlHinweis(folge.hinweis);
  }, [filterKey]); // eslint-disable-line react-hooks/exhaustive-deps

  /** Wie viele Gewählte stehen nicht in der Liste – nach „Alle wählen" bis zu 400 (B-161). */
  const unsichtbar = unsichtbareAuswahl(selected, filteredExercises.map((e) => e.id));

  /** Der einzige Weg zurück, wenn die Auswahl über die geladene Seite hinausreicht: `toggle` gibt es nur an einer gerenderten Zeile. */
  function auswahlLeeren() {
    setSelected([]);
    setAuswahlHinweis(null);
  }

  // Voreinstellungen aus dem Fragenkatalog ableiten, solange der Vater den Feinschliff nicht angefasst hat.
  function applyDefaults() {
    if (touchedFineTune) return;
    const g = GOALS[goal];
    const it = INTENSITY[intensity];
    setDurationDays(g.duration);
    setDefaultStage(g.stage);
    setRequireTyped(g.typed);
    setPassPercent(it.pass);
    setPointsGoalMet(it.points);
    setPenaltyCoins(it.penalty);
    const subjName = subject?.name ?? "Lernplan";
    setTitle(topic.trim() ? `${subjName} – ${topic.trim()}` : `${subjName} – ${g.label}`);
  }

  function toggle(id: number) {
    // Der Hinweis über die verworfene Auswahl gilt bis zur nächsten Wahl – danach ist er Vergangenheit.
    setAuswahlHinweis(null);
    setSelected((sel) => (sel.includes(id) ? sel.filter((k) => k !== id) : [...sel, id]));
  }
  const [selectAllBusy, setSelectAllBusy] = useState(false);

  /*
   * Alles, was je geladen wurde — nicht nur die aktuelle Seite.
   *
   * `selected` trägt Ids, aber der Abschluss braucht zu jeder gewählten Übung **Titel und Typ**: Der Typ
   * entscheidet, ob die Test-Stufe mitgeschickt wird (`wizardFinish.ts`, nur `Vocabulary` hat eine), der
   * Titel benennt im Fehlerfall die schuldige Übung. Beides aus der geladenen Seite zu holen ging gut,
   * solange nur daraus gewählt werden konnte. Seit „Alle wählen" bis zu 500 Treffer übernimmt, sind die
   * meisten davon **nicht** auf der Seite: Der Typ käme als `""` zurück, die Stufe fiele für sie still
   * weg — und der Vater hat sie im Überblick gerade für „alle Positionen" bestätigt.
   */
  const gesehen = useRef(new Map<number, ExerciseSummary>());
  function merken(items: ExerciseSummary[]) {
    for (const e of items) gesehen.current.set(e.id, e);
  }
  useEffect(() => {
    merken(exercises.data?.items ?? []);
  }, [exercises.data]);

  /*
   * „Alle wählen" hieß bisher „die ersten 100 wählen" (B-18): Der Server liefert eine Seite, und der Knopf
   * übernahm genau die - bei 250 Treffern entstand ein Plan mit 100 Positionen, und `TruncationHint` war
   * der einzige Hinweis darauf.
   *
   * Bei mehr Treffern als geladen wird darum einmal mit `take=500` nachgefragt (die Obergrenze des
   * Servers, `PagingExtensions.MaxTake`). Darüber hinaus bleibt es bei einer Kappung - bewusst: ein
   * vollständiges Cursor-Paging im Assistenten wäre eine eigene Story, und `TruncationHint` zeigt den
   * Rest weiterhin an.
   *
   * **Generationen-Gate** (B-161): Dieser Nachschlag ist der einzige Ladeweg neben `useAsync`, und der hat
   * sein eigenes `cancelled`-Flag. Ohne Gate schreibt eine spät eintreffende Antwort die Ids einer längst
   * verworfenen Suche zurück - der Effekt oben hatte die Auswahl korrekt geleert, und danach steht sie
   * wieder da, ohne dass irgendetwas sie noch einmal leert. Dasselbe machte "Auswahl leeren" während des
   * Ladens stumm rückgängig. Der Schaden wäre genau der, um den es in dieser Story geht: Positionen mit
   * Pflichtziel, die niemand gewählt hat.
   */
  async function selectAll() {
    setAuswahlHinweis(null);
    const total = exercises.data?.total ?? 0;
    // B-140: nur zuweisbare übernehmen – und sagen, wie viele übersprungen wurden.
    const uebernehmen = (items: readonly ExerciseSummary[]) => {
      const folge = nurZuweisbare(items);
      setSelected(folge.ids);
      setAuswahlHinweis(folge.hinweis);
    };
    if (total <= filteredExercises.length) { uebernehmen(filteredExercises); return; }

    const keyBeimStart = filterKey;
    setSelectAllBusy(true);
    try {
      const alle = await api.searchExercises(wizardSearchParams({
        subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
        search: contentSearch, categoryId, type: typeKey, source: sourceSearch, grammarTopicId, besteZuerst,
      }, 500));
      // `merken` darf auch bei veralteter Antwort laufen - ein Metadaten-Vorrat schadet nie.
      merken(alle.items);
      if (geltenderFilterKey.current !== keyBeimStart) return;
      uebernehmen(alle.items);
    } catch (err) {
      if (geltenderFilterKey.current !== keyBeimStart) return;
      // Zurückfallen ja, schweigen nein: Sonst stünden 100 statt 400 Positionen im Plan — genau die
      // Kappung, die dieser Knopf abschaffen soll, nur unbeobachtbar.
      uebernehmen(filteredExercises);
      setError(`Konnte nicht alle Treffer laden – es sind nur die ${filteredExercises.length} geladenen `
        + `gewählt. ${errorMessage(err)}`);
    } finally {
      setSelectAllBusy(false);
    }
  }

  function canAdvance(): string | null {
    if (step === 0) {
      if (mode === "existing") return childId === "" ? "Bitte ein Kind wählen." : null;
      return newName.trim() ? pinProblem(newPin) : "Bitte einen Namen eingeben.";
    }
    // Beides sperrt auch den Knopf (eine Regel je Bildschirm, Hausregel „Pflichtfeld → gesperrt"); die Zeile hält Enter.
    if (step === 1) return subjectId === "" ? "Bitte ein Fach wählen." : terminGrund;
    if (step === 2) return selected.length === 0 ? "Bitte mindestens eine Übung wählen." : null;
    return null;
  }

  function next() {
    const problem = canAdvance();
    if (problem) { setError(problem); return; }
    setError(null);
    if (step === 1) applyDefaults();
    setStep((s) => Math.min(s + 1, STEPS.length - 1));
  }
  function back() { setError(null); setStep((s) => Math.max(s - 1, 0)); }

  /**
   * Sammelt die fünf Schritte zu einem Auftrag und lässt ihn schreiben; der Ablauf selbst steht in
   * [wizardFinish.ts](wizardFinish.ts) – dort ist er ohne Router und Bildschirm prüfbar.
   */
  async function finish() {
    setError(null);
    setKlassenarbeitFehlt(null);
    setBusy(true);
    try {
      const planId = await runWizardFinish(progress.current, {
        newChild: mode === "new" ? {
          name: newName.trim(),
          pin: newPin || undefined,
          birthYear: newBirthYear ? Number(newBirthYear) : null,
          grade: grade === "" ? null : Number(grade),
          schoolType,
        } : null,
        existingChildId: mode === "existing" ? Number(childId) : null,
        plan: {
          title: title.trim() || "Neuer Lehrplan",
          // Pflicht (B-539): „Weiter" ist in Schritt 2 ohne Fach gesperrt, hier steht es also immer.
          subjectId: subjectId === "" ? null : Number(subjectId),
          seriesUnitId: seriesUnitId === "" ? null : seriesUnitId,
          durationDays: vorgaben.durationDays,
          startDate: vorgaben.startDate,
        },
        exerciseIds: selected,
        // Jede gewählte Übung wird ein Tagesziel mit den Feinschliff-Werten.
        position: {
          cadence: "Daily",
          stage: defaultStage,
          goalThreshold: passPercent,
          useLeitner,
          requireTypedTest: requireTyped,
          pointsGoalMet,
          penaltyCoins,
          comboThreshold,
          comboBonusPoints,
          // Ohne Leitner-Kasten keine Dosis – der Server lehnt die Kombination ab.
          newItemsPerDay: useLeitner ? Math.max(0, newItemsPerDay) : 0,
        },
        titleOf: (id) => gesehen.current.get(id)?.title ?? `#${id}`,
        typeOf: (id) => gesehen.current.get(id)?.type ?? "",
        stageSchedule: vorgaben.stageSchedule,
        classTest: vorgaben.klassenarbeitTermin === null ? null : {
          title: `${subject?.name ?? "Klassen"}-Arbeit`,
          topic: topic.trim() || null,
          subjectId: subjectId === "" ? null : Number(subjectId),
          scheduledDate: vorgaben.klassenarbeitTermin,
        },
      }, api);
      // `null` heißt: es läuft schon ein Durchgang (zweiter Klick). Der erste besitzt das Ergebnis und
      // navigiert – `busy` bleibt absichtlich stehen.
      if (planId === null) return;
      nav(`/vater/plan/${planId}`);
    } catch (err) {
      if (err instanceof KlassenarbeitFehlt) setKlassenarbeitFehlt(err);
      else setError(errorMessage(err));
      setBusy(false);
    }
  }

  const currentYear = new Date().getFullYear();
  const gradeHint = newBirthYear && Number(newBirthYear) > 1990
    ? `≈ ${Math.max(1, currentYear - Number(newBirthYear) - 6)}. Klasse`
    : null;

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <div className="row">
        <h2 className="h-section" style={{ margin: 0 }}>🧭 Lehrplan-Assistent</h2>
        <span className="muted" style={{ marginLeft: "auto", fontSize: 13 }}>Schritt {step + 1} von {STEPS.length}</span>
      </div>

      <Stepper step={step} onGo={(i) => !planSteht && i < step && setStep(i)} />

      {/* -------- Schritt 1: Kind -------- */}
      {step === 0 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h3 style={{ margin: 0 }}>Für welches Kind?</h3>
          {/* Der schwerste der drei Ausfälle (B-188): Scheitert `api.children()`, bleibt `data` auf `null`,
              und „Bestehendes Kind" ist dann gesperrt. Der Assistent behauptet damit stumm „du hast keine
              Kinder" und lässt nur noch das Anlegen offen — der Vater legt ein Kind ein zweites Mal an,
              das es längst gibt. Der Knopf bleibt zu Recht gesperrt (es gibt ja keine Liste); was fehlte,
              war der Unterschied zwischen „keine da" und „nicht geladen". */}
          <ListenAusfall was="Deine Kinder" fehler={children.error} laedt={children.loading}
            onRetry={children.reload} />
          <div className="row" style={{ gap: 10, flexWrap: "wrap" }} role="radiogroup" aria-label="Welches Kind?" onKeyDown={radioGroupKeys}>
            <ChoicePill active={mode === "existing"} disabled={(children.data?.length ?? 0) === 0}
              onClick={() => {
                setTouchedMode(true); setMode("existing");
                // Der Effekt oben schweigt jetzt zu Recht (der Vater hat gewählt) – die Vorbelegung muss
                // darum hier passieren, sonst steht das Pulldown leer und „Weiter" verlangt eine Auswahl,
                // die nie angeboten wurde.
                if (childId === "" && children.data?.length) setChildId(children.data[0].id);
              }}>Bestehendes Kind</ChoicePill>
            <ChoicePill active={mode === "new"}
              onClick={() => { setTouchedMode(true); setMode("new"); }}>Neues Kind anlegen</ChoicePill>
          </div>

          {mode === "existing" ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label>Kind</label>
              <select aria-label="Kind" value={childId} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data?.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}{c.grade ? ` · ${c.grade}. Klasse` : ""} (#{c.id})</option>
                ))}
              </select>
            </div>
          ) : (
            /* Wer hier etwas einträgt, hat sich für ein neues Kind entschieden – genauso bindend wie der Klick
               auf die Pille. Ohne das schaltete eine spät ankommende Kinderliste den Effekt oben noch nach
               Schritt 0 auf das erste bestehende Kind um, still, und der Plan ginge an ein anderes Kind
               (B-177). `onChange` am Container fängt die Ereignisse aller fünf Felder. */
            <div className="form-grid" onChange={() => setTouchedMode(true)}>
              <div className="field"><label htmlFor="wiz-name">Name</label><input id="wiz-name" value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="z.B. Max" /></div>
              <div className="field"><label htmlFor="wiz-birthyear">Geburtsjahr</label><input id="wiz-birthyear" type="number" value={newBirthYear} onChange={(e) => setNewBirthYear(e.target.value)} placeholder="z.B. 2012" />{gradeHint && <span className="sub">{gradeHint}</span>}</div>
              <div className="field"><label htmlFor="wiz-grade">Klasse</label><input id="wiz-grade" type="number" min={1} max={13} value={grade} onChange={(e) => setGrade(e.target.value === "" ? "" : Number(e.target.value))} placeholder="z.B. 8" /></div>
              <div className="field"><label>Schulart</label>
                <select aria-label="Schulart" value={schoolType} onChange={(e) => setSchoolType(e.target.value as SchoolType)}>
                  {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div className="field"><label htmlFor="wiz-pin">PIN (Login des Kindes)</label><input id="wiz-pin" value={newPin} onChange={(e) => setNewPin(e.target.value)} placeholder="z.B. 1111" /></div>
            </div>
          )}
        </section>
      )}

      {/* -------- Schritt 2: Fragenkatalog -------- */}
      {step === 1 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <h3 style={{ margin: 0 }}>Wo hakt es?</h3>
          <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading}
            onRetry={subjects.reload} />
          <div className="form-grid">
            <div className="field"><FieldLabel htmlFor="wiz-fach" topic="planSubject">Fach <Pflicht /></FieldLabel>
              <select id="wiz-fach" value={subjectId} onChange={(e) => {
                // Die Art gehört zum alten Fach – stehen gelassen filterte sie auf eine Kategorie, die es
                // im neuen gar nicht gibt, und die Trefferliste bliebe ohne sichtbaren Grund leer. Hier
                // statt in einem Effekt, weil hier die Entscheidung fällt: ein Effekt liefe erst NACH der
                // Suchabfrage und schickte eine überflüssige Runde mit altem Filter los.
                // Typ und Quelle bleiben: der Typ kommt aus dem globalen Manifest, die Quelle ist Freitext
                // über Lehrwerke – beides fachunabhängig, ein Reset würde Eingabe wegwerfen.
                setCategoryId("");
                setGrammarTopicId("");
                setSeriesUnitId("");
                setSubjectId(e.target.value === "" ? "" : Number(e.target.value));
              }}>
                <option value="">– Fach wählen –</option>
                {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div className="field"><FieldLabel htmlFor="wiz-unit" topic="planUnit">Unit <span className="muted">(optional)</span></FieldLabel>
              <PlanUnitSelect id="wiz-unit" subjectId={subjectId} value={seriesUnitId} onChange={setSeriesUnitId} />
            </div>
            <div className="field"><label htmlFor="wiz-topic">Thema / Kapitel (optional)</label><input id="wiz-topic" value={topic} onChange={(e) => setTopic(e.target.value)} placeholder="z.B. Unité 2 – En ville" /></div>
          </div>

          <div>
            <span id="wiz-ziel" className="sub" style={{ display: "block", marginBottom: 8 }}>Was ist das Ziel?</span>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }} role="radiogroup" aria-labelledby="wiz-ziel" onKeyDown={radioGroupKeys}>
              {(Object.keys(GOALS) as Goal[]).map((g) => (
                <OptionCard key={g} active={goal === g} onClick={() => setGoal(g)}
                  title={`${GOALS[g].emoji} ${GOALS[g].label}`} hint={GOALS[g].hint} />
              ))}
            </div>
          </div>
          {istKlassenarbeit && <KlassenarbeitTerminFeld termin={termin} heute={heute} onChange={setTermin} />}

          <div>
            <span id="wiz-intensitaet" className="sub" style={{ display: "block", marginBottom: 8 }}>Wie intensiv soll geübt werden?</span>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }} role="radiogroup" aria-labelledby="wiz-intensitaet" onKeyDown={radioGroupKeys}>
              {(Object.keys(INTENSITY) as Intensity[]).map((i) => (
                <OptionCard key={i} active={intensity === i} onClick={() => setIntensity(i)}
                  title={INTENSITY[i].label} hint={INTENSITY[i].hint} />
              ))}
            </div>
          </div>
          <p className="sub">Aus diesen Angaben schlägt der Assistent Dauer, Test-Stufe und Punkte je Übung vor – anpassbar im Schritt „Feinschliff".</p>
        </section>
      )}

      {/* -------- Schritt 3: Übungen -------- */}
      {step === 2 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <div className="row">
            {/* Die Zahl nennt die Unsichtbaren mit (B-161): „(500 gewählt)" neben „5 passende Übungen" war
                ein Widerspruch, den nur der Vater ausbaden konnte. */}
            <h3 style={{ margin: 0 }}>Übungen wählen <span className="muted">
              ({selected.length} gewählt{unsichtbar > 0 ? `, davon ${unsichtbar} unten nicht sichtbar` : ""})
            </span></h3>
            {/* Bleibt montiert und wird nur gesperrt: hängt man ihn aus, verschwindet er nach dem Klick
                unter dem Finger, der Fokus fällt auf `<body>` und ein Tastaturnutzer verliert seine
                Position. Während des „Alle wählen"-Nachschlags ist Leeren ohnehin wirkungslos. */}
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              disabled={selected.length === 0 || selectAllBusy}
              aria-label={`Auswahl leeren (${selected.length} gewählt)`} onClick={auswahlLeeren}>
              Auswahl leeren
            </button>
            <input style={{ marginLeft: "auto", maxWidth: 220 }} placeholder="Übung suchen…" value={contentSearch} onChange={(e) => setContentSearch(e.target.value)} aria-label="Übung suchen" />
          </div>
          <p className="sub">
            Übungen aus der geteilten Bibliothek für {subject?.name ?? "das Fach"}{effectiveGrade ? `, ${effectiveGrade}. Klasse` : ""} – jede wird zu einer Tagesziel-Position im Plan.
          </p>
          {/* Die Region steht **immer** im DOM, sichtbar wird nur der Kasten darin – dasselbe Muster wie
              `StatusBanner`/`DerivedHint`: viele Screenreader sagen nur an, was in eine *bereits vorhandene*
              Region hineinwächst. Entstünden Region und Text gleichzeitig, bliebe die Ansage aus (B-132).
              `info` statt `ok`/`err`, weil hier nichts gelang und nichts fehlschlug – die App hat von sich
              aus etwas getan. */}
          <div role="status" aria-live="polite">
            {auswahlHinweis && <div className="banner info">{auswahlHinweis}</div>}
          </div>

          {/* Drei optionale Filter, die der Server schon konnte (B-18). Alle mit „alle" als Vorgabe: Ein
              Fach ohne Arten zeigt sonst ein leeres Pulldown und wirkt kaputt.

              Die Beschriftungen sind **sichtbar**, nicht nur `aria-label` (B-163): „Art" und „Typ" sind zwei
              verschiedene Achsen mit ähnlichem Wortmaterial, und der Platzhalter verschwindet, sobald man
              etwas auswählt — danach wäre die Achse für Sehende gar nicht mehr benannt, während ein
              Screenreader sie weiter vorliest. Muster: `ExerciseFilterBar` — dort sind die Beschriftungen
              allerdings nur optisch, der Name kommt aus `aria-label`; hier sind sie per `htmlFor` verknüpft.
              Ein „Vereinheitlichen" darf die Verknüpfung also nicht wieder herausnehmen.
              Das Quelle-Feld daneben bleibt bewusst unbeschriftet: Entscheidung 5 gilt für die zwei Achsen
              mit ähnlichem Wortmaterial. Sein Platzhalter verschwindet zwar genauso beim Tippen — das ist
              derselbe Mangel, aber eine eigene Entscheidung. */}
          {/* Bei der Filterreihe, nicht oben unter der Überschrift: Der Ausfall betrifft **dieses**
              Pulldown. Stünde er über Zählzeile und Trefferbereich, läge er bei gleichzeitigem Suchausfall
              über einem zweiten Banner, das zur oberen Hälfte gehört – zwei Meldungen in umgekehrter
              Reihenfolge zu ihren Bedienelementen. Fällt die Liste aus, steht das Pulldown auf „alle" und
              filtert nichts; sichtbar wäre sonst nur, dass mehr Treffer kommen als erwartet. */}
          <ListenAusfall was="Die Arten dieses Fachs" fehler={categories.error} laedt={categories.loading}
            onRetry={categories.reload} />
          <div className="row" style={{ gap: 8, flexWrap: "wrap", alignItems: "flex-end" }}>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="wiz-category">Art</label>
              <select id="wiz-category" value={categoryId}
                onChange={(e) => setCategoryId(e.target.value === "" ? "" : Number(e.target.value))}>
                <option value="">– alle Arten –</option>
                {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 200 }}>
              <label htmlFor="wiz-grammar-topic">Grammatik-Thema</label>
              <select id="wiz-grammar-topic" value={grammarTopicId}
                onChange={(e) => setGrammarTopicId(e.target.value === "" ? "" : Number(e.target.value))}>
                <option value="">– alle Themen –</option>
                {grammarTopics.data?.map((t) => <option key={t.id} value={t.id}>{t.label}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="wiz-type">Typ</label>
              <select id="wiz-type" value={typeKey} onChange={(e) => setTypeKey(e.target.value)}>
                <option value="">– alle Typen –</option>
                {types.all.map((t) => <option key={t.type} value={t.type}>{t.label}</option>)}
              </select>
            </div>
            <input aria-label="Quelle" placeholder="Quelle, z. B. Green Line 1…" style={{ maxWidth: 220 }}
              value={sourceSearch} onChange={(e) => setSourceSearch(e.target.value)} />
          </div>
          {/* B-408: dieselbe Wahl wie im Planbau (B-270). Umsortieren lädt neu, die Auswahl bleibt vollständig – was dabei
              aus der Seite rutscht, zählt „davon N unten nicht sichtbar". */}
          <div className="row" style={{ gap: 6, alignItems: "center", margin: "4px 0" }}>
            <label className="muted" htmlFor="wiz-reihenfolge" style={{ fontSize: 12 }}>Trefferliste sortieren</label>
            <select id="wiz-reihenfolge" value={besteZuerst ? "rating" : "catalog"}
              onChange={(e) => setBesteZuerst(e.target.value === "rating")}>
              <option value="catalog">wie im Katalog</option>
              <option value="rating">beste Bewertung zuerst</option>
            </select>
          </div>

          {/* `platzhalter` ist hier heute wirkungslos: ohne gewähltes Fach liefert die Suche sofort
              `{items:[],total:0}`, `data` ist danach nie wieder `null`. Sobald der Assistent ein Fach
              per Query vorbelegt, ändert sich das – dann ergibt „erste Suche gescheitert, danach Filter
              gewechselt" kurz `platzhalter=false, treffer=[], fehler=null` und damit wieder den falschen
              Satz „Keine passenden Übungen im Katalog" aus B-162. Unter der Bedingung vor B-203 stand in
              diesem Fenster der Platzhalter; die Marge ist also dünner geworden (Reviewer-Fund zu B-203). */}
          <WizardTrefferbereich
            treffer={filteredExercises} geladen={exercises.data?.items.length ?? 0}
            gesamt={exercises.data?.total ?? 0} platzhalter={exercises.showPlaceholder}
            fehler={exercises.error} laedt={exercises.loading} zeilenVeraltet={zeilenVeraltet} schluessel={filterKey}
            fachName={subject?.name} selected={selected} selectAllBusy={selectAllBusy}
            onToggle={toggle} onSelectAll={selectAll} onRetry={exercises.reload}
            typLabel={types.label} umfang={types.umfang} onTry={(e) => setVorschau({ id: e.id, title: e.title })} />
        </section>
      )}

      {/* -------- Schritt 4: Feinschliff -------- */}
      {step === 3 && (
        <section className="card">
          <h3 style={{ margin: "0 0 12px" }}>Feinschliff <span className="muted">(gilt für alle {selected.length} Positionen)</span></h3>
          <div className="form-grid" onChange={() => setTouchedFineTune(true)}>
            <div className="field"><label htmlFor="wiz-title">Titel</label><input id="wiz-title" title="Titel" value={title} onChange={(e) => setTitle(e.target.value)} /></div>
            {/* Beim Ziel „Klassenarbeit" folgen Start und Dauer aus dem Termin (B-423, Entscheidung 1) – nur Anzeige. */}
            <div className="field"><label htmlFor="wiz-start">Start</label><input id="wiz-start" title="Startdatum" type="date" readOnly={istKlassenarbeit} value={vorgaben.startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-duration" topic="planDuration">Dauer (Tage)</FieldLabel><input id="wiz-duration" title="Dauer" type="number" min={1} readOnly={istKlassenarbeit} value={vorgaben.durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} />
              {istKlassenarbeit && <span className="sub">bis zum Vortag der Klassenarbeit</span>}</div>
            <div className="field"><FieldLabel htmlFor="wiz-pass" topic="goalThreshold">Bestehen ab %</FieldLabel><input id="wiz-pass" title="Bestehen ab Prozent" type="number" min={1} max={100} value={passPercent} onChange={(e) => setPassPercent(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-points" topic="pointsGoalMet">Punkte je Ziel</FieldLabel><input id="wiz-points" title="Punkte je erreichtem Ziel" type="number" min={0} value={pointsGoalMet} onChange={(e) => setPointsGoalMet(Number(e.target.value))} /></div>
            {/* Der „Stick": verpasste Pflicht kostet Münzen. 0 = reine Belohnung, Schulden sind erlaubt. */}
            <div className="field"><FieldLabel htmlFor="wiz-penalty" topic="penaltyCoins">Münz-Malus bei Versäumnis</FieldLabel><input id="wiz-penalty" title="Münz-Malus bei gerissener Pflicht" type="number" min={0} value={penaltyCoins} onChange={(e) => setPenaltyCoins(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-combo-threshold" topic="comboThreshold">Combo alle … Treffer</FieldLabel><input id="wiz-combo-threshold" title="Combo-Schwelle" type="number" min={0} value={comboThreshold} onChange={(e) => setComboThreshold(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-combo-bonus" topic="comboBonusPoints">Combo-Bonuspunkte</FieldLabel><input id="wiz-combo-bonus" title="Combo-Bonuspunkte" type="number" min={0} value={comboBonusPoints} onChange={(e) => setComboBonusPoints(Number(e.target.value))} /></div>
            {/* Listen bekommen keine Dosis (B-477, `wizardFinish`): eine ungeordnete lehnt der Server ab. Das Etikett sagt es, statt
                still zu wirken – `aria-label` hält den Feldnamen dabei stabil. */}
            <div className="field"><FieldLabel htmlFor="wiz-dose" topic="newItemsPerDay">Neue Inhalte je Tag <span className="muted">(nicht für Listen)</span></FieldLabel><input id="wiz-dose" aria-label="Neue Inhalte je Tag" title="Neue Inhalte je Tag" type="number" min={0} max={200} disabled={!useLeitner} value={newItemsPerDay} onChange={(e) => setNewItemsPerDay(Number(e.target.value))} /></div>
            {/* Nur Vokabelübungen kennen diese Skala; für alle anderen Typen wird die Stufe weggelassen
                (siehe `wizardFinish.typeOf`) – darum sagt es hier auch dran, statt still zu wirken. */}
            <div className="field"><FieldLabel topic="defaultStage">Test-Stufe <span className="muted">(nur Vokabelübungen)</span></FieldLabel>
              {/* Aus dem Manifest wie Anlegen und Bearbeiten. Die Nummer bleibt davor, die Zusammenfassung unten nennt sie. */}
              <select aria-label="Test-Stufe" value={defaultStage} onChange={(e) => setDefaultStage(Number(e.target.value))}>
                {/* B-599: die Stufen der Vokabeln aus dem Server-Manifest; die Vorgabe des Assistenten (4) bleibt fest. */}
                {types.stages("Vocabulary").map((f) => (
                  <option key={f.value} value={f.value}>{f.value} · {f.label}</option>
                ))}
              </select>
            </div>
          </div>
          <div className="row" style={{ marginTop: 12, gap: 20, flexWrap: "wrap" }}>
            <span className="label-row">
              <label className="checkline"><input type="checkbox" checked={useLeitner} onChange={(e) => { setTouchedFineTune(true); setUseLeitner(e.target.checked); }} /> Leitner-Kasten (Übungspunkte)</label>
              <InfoHint topic="useLeitner" />
            </span>
            <span className="label-row">
              <label className="checkline"><input type="checkbox" checked={requireTyped} onChange={(e) => { setTouchedFineTune(true); setRequireTyped(e.target.checked); }} /> Nur getippte Tests zählen</label>
              <InfoHint topic="requireTypedTest" />
            </span>
          </div>
          {/* B-423, Entscheidungen 2 und 5: die Leiter als ein Satz mit einem änderbaren Tag. Vokabel-Positionen mit Leiter
              gehen ohne „Nur getippte Tests zählen" an den Server – das Häkchen gilt dann nur für die übrigen Übungen,
              und der Satz sagt das, statt still zu wirken. */}
          {vorgaben.stageSchedule && leiter && (
            <div className="row" style={{ marginTop: 12, gap: 8, alignItems: "center", flexWrap: "wrap" }}>
              <span>Vokabeln: {leiter}. „Nur getippte Tests zählen" gilt hier nur für die übrigen Übungen.</span>
              <SchwellenFeld wert={schwelleText} onWert={setSchwelleText}
                angewandt={vorgaben.stageSchedule[1].dayNumber} max={vorgaben.durationDays} />
            </div>
          )}
          {/* B-552, Entscheidung 5: auf der festen Stufe Selbsteinschätzung fällt das Häkchen für Vokabeln ebenso weg – der Satz
              sagt es, statt still zu wirken. */}
          {haekchenZeile(requireTyped, defaultStage, vorgaben.stageSchedule) && (
            <p style={{ marginTop: 12 }}>{haekchenZeile(requireTyped, defaultStage, vorgaben.stageSchedule)}</p>
          )}
          <p className="sub" style={{ marginTop: 12 }}>
            Der <strong>Münz-Malus</strong> greift, wenn ein Tagesziel am Ende des Tages nicht erreicht wurde –
            das Kind kann dadurch ins Minus geraten. Setze ihn auf 0, wenn du nur belohnen willst.
          </p>
        </section>
      )}

      {/* -------- Schritt 5: Überblick -------- */}
      {step === 4 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          <h3 style={{ margin: 0 }}>Überblick</h3>
          <SummaryRow label="Kind" value={mode === "new" ? `${newName || "?"} (neu${grade ? `, ${grade}. Klasse` : ""})` : `${selectedChild?.name ?? "?"}${selectedChild?.grade ? `, ${selectedChild.grade}. Klasse` : ""}`} />
          <SummaryRow label="Fach" value={subject?.name ?? "–"} />
          <SummaryRow label="Ziel · Intensität" value={`${GOALS[goal].label} · ${INTENSITY[intensity].label}`} />
          <SummaryRow label="Titel" value={title || "Neuer Lehrplan"} />
          <SummaryRow label="Zeitraum" value={zeitraumZeile(vorgaben.startDate, vorgaben.durationDays, vorgaben.klassenarbeitTermin !== null)} />
          <SummaryRow label="Je Position" value={`Test-Stufe ${defaultStage} · bestehen ab ${passPercent}% · ${pointsGoalMet} Punkte/Ziel`} />
          {leiter && <SummaryRow label="Stufenleiter" value={`Vokabeln: ${leiter}`} />}
          {haekchenZeile(requireTyped, defaultStage, vorgaben.stageSchedule)
            && <SummaryRow label="Nur getippt" value={haekchenZeile(requireTyped, defaultStage, vorgaben.stageSchedule)!} />}
          {vorgaben.klassenarbeitTermin && <SummaryRow label="Klassenarbeit" value={klassenarbeitZeile(vorgaben.klassenarbeitTermin)} />}
          <SummaryRow label="Tagesdosis" value={dosisZeile(useLeitner, newItemsPerDay, selected.map((id) => gesehen.current.get(id)?.type ?? ""))} />
          <SummaryRow label="Versäumnis" value={penaltyCoins > 0 ? `−${penaltyCoins} 🪙 je gerissenem Tagesziel` : "kein Malus (reine Belohnung)"} />
          <SummaryRow label="Übungen" value={`${selected.length} als Tagesziel-Positionen`} />
          <p className="sub">Danach erscheint der Plan unter „Zuweisen"; der Sohn sieht ihn sofort in seiner App.</p>
        </section>
      )}

      <div className={`live-region${error ? " banner err" : ""}`} role="status" aria-live="polite">{error}</div>
      {klassenarbeitFehlt && (
        <div className="banner err" role="alert">
          {klassenarbeitFehlt.message}{" "}
          <Link to={`/vater/class-tests?childId=${klassenarbeitFehlt.childId}`}>Klassenarbeit von Hand anlegen</Link>
          {" · "}<Link to={`/vater/plan/${klassenarbeitFehlt.planId}`}>Zum Plan</Link>
          {" · "}oder unten erneut versuchen – der Plan entsteht dabei nicht doppelt. Eine von Hand angelegte Arbeit
          bekommt ihre Übungen dort über „Übungen &amp; Vorbereiten“.
        </div>
      )}
      {vorschau && <ExercisePreviewModal exerciseId={vorschau.id} title={vorschau.title} onClose={() => setVorschau(null)} />}

      <div className="row" style={{ gap: 10 }}>
        {/* Gesperrt statt ausgeblendet, sobald der Plan steht: der Knopf verschwände sonst unter dem Finger. */}
        {step > 0 && <button type="button" className="btn ghost" style={{ width: "auto" }} disabled={planSteht} onClick={back}>← Zurück</button>}
        <span style={{ marginLeft: "auto" }} />
        {step < STEPS.length - 1
          // Gesperrt an Fach und Termin (B-423, Hausregel „Pflichtfeld → gesperrt"): der Grund steht am Feld.
          ? <button type="button" className="btn" style={{ width: "auto" }} disabled={step === 1 && (subjectId === "" || terminGrund !== null)} onClick={next}>Weiter →</button>
          : <button type="button" className="btn lime" style={{ width: "auto" }} disabled={busy} onClick={finish}>
            {busy ? "…" : klassenarbeitFehlt ? "Klassenarbeit erneut anlegen" : "✅ Lehrplan erstellen"}</button>}
      </div>
    </div>
  );
}

/**
 * Der Trefferbereich von Schritt 3 – Zählzeile, „Alle wählen", und darunter genau EINER von vier
 * Zuständen: Platzhalter, Fehler, echte Leermenge, Liste.
 *
 * Exportiert **nur für den Test** (B-162): Der Fehlerfall verlangt einen scheiternden Abruf, und
 * `VaterWizard` ist eine ganze Seite am Netz. Derselbe Schnitt wie bei `CategoryRows` (B-154) und
 * `SeriesForm` (B-143) – der Baustein braucht kein gefälschtes `fetch`, er ist reine Prop-Arbeit.
 *
 * Der Fehler war, dass `treffer.length === 0` zwei Bedeutungen trug: „die Suche lief und fand nichts"
 * und „die Suche lief nicht". Der Satz war dabei handlungsleitend FALSCH – er schickte den Vater ins
 * Anlege-Formular, während der Katalog voll war.
 */
export function WizardTrefferbereich({
  treffer, geladen, gesamt, platzhalter, fehler, laedt, zeilenVeraltet, schluessel, fachName,
  selected, selectAllBusy, onToggle, onSelectAll, onRetry, typLabel, umfang, onTry,
}: {
  treffer: ExerciseSummary[];
  geladen: number;
  gesamt: number;
  /**
   * `useAsync.showPlaceholder` – die Bedingung steht dort und wird hier absichtlich **nicht**
   * nachformuliert: die vorige Fassung („nichts da UND kein Grund dafür", B-184) beschrieb seit B-203
   * eine Bedingung, die es nicht mehr gibt. Zwei Formulierungen desselben Begriffs werden zwei
   * Bedeutungen (`frontend/CLAUDE.md`).
   */
  platzhalter: boolean;
  fehler: string | null;
  laedt: boolean;
  zeilenVeraltet: boolean;
  /**
   * Der Schlüssel der Suche, zu der `fehler`/`laedt` gehören (`filterKey`). Das Fehler-Gedächtnis gilt nur
   * unter ihm: ein Filterwechsel ist eine andere Abfrage, keine Wiederholung (B-227).
   */
  schluessel: string;
  fachName: string | undefined;
  selected: number[];
  selectAllBusy: boolean;
  onToggle: (id: number) => void;
  onSelectAll: () => void;
  onRetry: () => void;
  typLabel: (type: string) => string;
  /** Der Umfang der Übung in Worten (B-424, `ExerciseTypeLookup.umfang`); `null` = nichts zu zählen. */
  umfang: (type: string, itemCount: number | null | undefined) => string | null;
  /** Öffnet den Testmodus für genau diese Übung (B-424); die Auswahl bleibt dabei, wie sie ist. */
  onTry: (exercise: ExerciseSummary) => void;
}) {
  // Der letzte Fehler überlebt den Start der Wiederholung (B-216): `useAsync.reload()` setzt `error` auf
  // `null` und `loading` auf `true` im selben Schritt – an `fehler` allein hinge das Banner samt Knopf im
  // Moment des Klicks aus. Dieselbe Regel wie in `ListenAusfall`, aus demselben Hook – hier mit Schlüssel,
  // weil die Suche wechselnde Abhängigkeiten hat (B-227).
  const gescheitert = useGemerkterFehler(fehler, laedt, schluessel);
  // Ein gesperrter Knopf verliert in Chromium den Fokus – darum wandert er beim Klick auf den Kasten
  // (Begründung in `ListenAusfall.tsx`).
  const kasten = useRef<HTMLDivElement>(null);
  // Ids für den Grund an gesperrten Zeilen (B-140), damit das Kästchen ihn per `aria-describedby` nennt.
  const uid = useId();
  return (
    <>
      <div className="row">
        {/* Die Zahl gehört zu einer Abfrage, nicht zu einem Bildschirm: Solange die Zeilen zum vorigen
            Filter gehören, wäre „N passende Übungen" die Zahl von vorhin – neben einer Meldung, die vom
            Jetzt spricht. Dieselbe Wahrheit (`zeilenVeraltet`) trägt schon Kästchen und Knopf. */}
        <span className="sub">
          {gescheitert ? "Suche gescheitert" : zeilenVeraltet ? "Suche läuft…" : `${treffer.length} passende Übungen`}
        </span>
        {/* `zeilenVeraltet` muss am Knopf mit, nicht nur am Kästchen (B-169): `laedt` deckt das
            Ladefenster, aber NICHT den Fehlerzweig. Scheitert die Abfrage, bleibt `data` die alte Seite und
            `loading` fällt auf `false` — ohne `zeilenVeraltet` wäre der Knopf wieder bedienbar und wählte die
            Ids der veralteten Liste. `exercises.stale` bleibt in dem Fall wahr, weil die Deps der Daten nur bei
            Erfolg nachziehen (B-164). Eine Wahrheit, an drei Steuerelementen, kein zweites Gate. */}
        {treffer.length > 0 && (
          <button type="button" className="btn ghost inline-btn" style={{ marginLeft: "auto" }}
            disabled={selectAllBusy || laedt || zeilenVeraltet} onClick={onSelectAll}>
            {selectAllBusy ? "Lade alle…" : "Alle wählen"}
          </button>
        )}
      </div>
      <TruncationHint shown={geladen} total={gesamt} />

      {/* Ohne den Knopf wäre die neue Meldung eine Verschlechterung: Über den Stepper zurück und wieder
          vor ändert KEINE Abhängigkeit von `useAsync`, es fliegt also keine neue Abfrage — ausgerechnet
          die Bewegung, die ein Nutzer versucht, ist die wirkungslose. Der einzige verbleibende Ausweg
          wäre F5, und der kostet den ganzen Assistenten (es persistiert kein Zustand). */}
      {gescheitert && (
        <div className="banner err" role="alert" tabIndex={-1} ref={kasten}>
          Die Übungssuche ist gescheitert: {gescheitert}
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: 8 }}
            disabled={laedt} onClick={() => { kasten.current?.focus(); onRetry(); }}>
            {laedt ? "Lädt…" : "Erneut suchen"}
          </button>
        </div>
      )}

      {platzhalter ? <div className="loading">Lade Übungen…</div> : treffer.length === 0 ? (
        // Der Satz gilt NUR, wenn die Suche wirklich gelaufen ist. Im Fehlerfall steht oben schon das
        // Banner; ihn zusätzlich zu zeigen hieße, im selben Bildschirm zwei Ursachen zu behaupten.
        // `gescheitert`, nicht `fehler`: Im Fenster der Wiederholung ist `fehler` schon `null`, das Banner
        // steht aber noch – an `fehler` stünden hier beide Sätze zugleich (Reviewer-Fund, B-216).
        gescheitert ? null : (
          <div className="banner err">
            Keine passenden Übungen im Katalog. Lege welche unter „Übungen" an (Fach {fachName ?? "?"}).
          </div>
        )
      ) : (
        <div style={{ maxHeight: 340, overflowY: "auto", display: "grid", gap: 6 }}>
          {treffer.map((e) => {
            const by = authorText(e);
            const groesse = umfang(e.type, e.itemCount);
            return (
              // Der Knopf steht NEBEN dem Label, nicht darin: ein Klick auf „Ausprobieren" darf die Auswahl nicht
              // umschalten, und ein Knopf im Label wäre für einen Screenreader Teil des Kästchen-Namens (B-424).
              <div key={e.id} className="row" style={{ gap: 8, padding: 8, border: "1px solid var(--stroke)", borderRadius: 8, alignItems: "center" }}>
              <label className="checkline" style={{ flex: 1 }}>
                {/* Gesperrt, solange die Zeilen zum vorigen Filter gehören (B-169) – und wenn der Vater die Übung nicht
                    zuweisen darf (B-140): dann nennt die Zeile den Grund, statt erst der Abschluss mit
                    `exercise_not_executable` zu scheitern. */}
                <input type="checkbox" disabled={zeilenVeraltet || !e.canAssign}
                  aria-describedby={e.canAssign ? undefined : `${uid}-gesperrt-${e.id}`}
                  checked={selected.includes(e.id)} onChange={() => onToggle(e.id)} />
                {/* Anzeigename aus dem Manifest, nicht der Schlüssel: sonst bietet das Pulldown darüber
                    „Vokabelkarten" an, während die Zeile darunter „Vocabulary" sagt — ein Widerspruch in
                    einem Bildschirm. Der Rückfall auf den Schlüssel greift, solange das Manifest lädt – und
                    wenn sein Abruf scheiterte: Im Assistenten bleibt der Ausfall bewusst stumm (B-215,
                    Entscheidung 2), die Zeile liest sich dann als Schlüssel. */}
                <span>{e.title} <span className="muted">· {typLabel(e.type)}{groesse ? ` · ${groesse}` : ""}{by ? ` – ${by}` : ""}{e.source ? ` [${e.source}]` : ""} · {ratingSummary(e.averageRating, e.ratingCount)}</span>
                  {/* Als Text, nicht als `title` (B-160): der erscheint am gesperrten Element nicht. */}
                  {!e.canAssign && <span id={`${uid}-gesperrt-${e.id}`} className="pill mag" style={{ marginLeft: 6 }}>{NICHT_ZUWEISBAR_GRUND}</span>}
                </span>
              </label>
              {/* Gesperrt wie das Kästchen (B-169): verschwände die Zeile bei offenem Dialog, fiele der Fokus beim Schließen ins Leere. */}
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={zeilenVeraltet}
                aria-label={`„${e.title}" ausprobieren`} onClick={() => onTry(e)}>🧪 Ausprobieren</button>
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}

/**
 * Das Terminfeld des Ziels „Klassenarbeit" (B-423, AK 1): Pflicht, und ein zu naher Termin nennt seinen Grund am Feld –
 * der „Weiter"-Knopf ist dann gesperrt. Exportiert für den Test; die Regel selbst steht in `terminProblem`.
 */
export function KlassenarbeitTerminFeld({ termin, heute, onChange }: {
  termin: string; heute: string; onChange: (termin: string) => void;
}) {
  const grund = terminProblem(termin, heute);
  // Leer sagt „(Pflicht)" genug; der zweite Grund (zu nah) steht als Text am Feld, nicht in einem `title` (B-160).
  const zuNah = termin !== "" && grund !== null;
  return (
    <div className="field" style={{ maxWidth: 320 }}>
      <label htmlFor="wiz-termin">Termin der Klassenarbeit <Pflicht /></label>
      <input id="wiz-termin" type="date" min={plusTage(heute, 2)} value={termin}
        aria-describedby={zuNah ? "wiz-termin-grund" : undefined} onChange={(e) => onChange(e.target.value)} />
      {zuNah
        ? <span id="wiz-termin-grund" className="sub">{grund}</span>
        : <span className="sub">Der Plan läuft bis zum Vortag; die Klassenarbeit wird mit angelegt.</span>}
    </div>
  );
}

/** Die Zeile „Zeitraum" im Überblick (B-427 deutsch, B-423 mit Ende): „14.11.2026 – 21.11.2026 · 8 Tage". */
export function zeitraumZeile(startDate: string, durationDays: number, mitEnde: boolean): string {
  return mitEnde
    ? `${formatDate(startDate)} – ${formatDate(plusTage(startDate, durationDays - 1))} · ${durationDays} Tage`
    : `${formatDate(startDate)} · ${durationDays} Tage`;
}

/** Die Zeile „Klassenarbeit" im Überblick (B-423, AK 2): sagt vorher, was der Abschluss zusätzlich anlegt. */
export function klassenarbeitZeile(termin: string): string {
  return `legt die Klassenarbeit am ${formatDate(termin)} an`;
}

/**
 * Der Tag der zweiten Leiter-Stufe (B-423, Review). Das Feld hält den **Rohtext**: geklemmt wird in `planVorgaben`, und
 * erst beim Verlassen zeigt das Feld den angewandten Tag – sonst spränge „12" schon nach der „1" auf die Untergrenze 2.
 */
export function SchwellenFeld({ wert, onWert, angewandt, max }: {
  wert: string; onWert: (wert: string) => void; angewandt: number; max: number;
}) {
  return (
    <>
      <label htmlFor="wiz-schwelle" className="muted" style={{ fontSize: 12 }}>Zweite Stufe ab Tag</label>
      <input id="wiz-schwelle" type="number" min={2} max={max} style={{ maxWidth: 80 }}
        value={wert === "" ? String(angewandt) : wert}
        onChange={(e) => onWert(e.target.value)} onBlur={() => onWert(String(angewandt))} />
    </>
  );
}

function Stepper({ step, onGo }: { step: number; onGo: (i: number) => void }) {
  return (
    <div className="row" style={{ gap: 6, flexWrap: "wrap" }}>
      {STEPS.map((s, i) => (
        <button key={s} type="button" onClick={() => onGo(i)}
          className={`pill ${i === step ? "cyan" : i < step ? "lime" : ""}`}
          style={{ border: "1.5px solid var(--stroke)", cursor: i < step ? "pointer" : "default", background: i === step ? undefined : i < step ? undefined : "#0c0e2c" }}>
          {i + 1}. {s}
        </button>
      ))}
    </div>
  );
}

function ChoicePill({ active, disabled, onClick, children }: { active: boolean; disabled?: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    // Ein Radio, kein Umschalter: die Gruppe hat immer genau eine Wahl, ein zweiter Klick löst sie nicht – dieselbe
    // Auszeichnung wie die Pillen in Anmeldung, Bildern und Lernstand. Sonst steckte die Wahl nur in der Farbe (B-348).
    // Nur die gewählte Option ist ein Tab-Stopp; die Pfeiltasten der Gruppe liefert `radioGroupKeys` (B-352).
    <button type="button" role="radio" aria-checked={active} tabIndex={active ? 0 : -1} disabled={disabled} onClick={onClick}
      className={`btn ${active ? "" : "ghost"} inline-btn`} style={{ width: "auto", opacity: disabled ? 0.4 : 1 }}>
      {children}
    </button>
  );
}

function OptionCard({ active, onClick, title, hint }: { active: boolean; onClick: () => void; title: string; hint: string }) {
  return (
    <button type="button" role="radio" aria-checked={active} tabIndex={active ? 0 : -1} onClick={onClick}
      style={{
        flex: "1 1 180px", textAlign: "left", cursor: "pointer", padding: "12px 14px", borderRadius: 14,
        border: active ? "2px solid var(--cyan)" : "1.5px solid var(--stroke)",
        background: active ? "rgba(38,217,255,.1)" : "#0c0e2c", color: "var(--ink)",
        boxShadow: active ? "0 0 0 3px rgba(38,217,255,.15)" : "none",
      }}>
      <div style={{ fontWeight: 800, fontSize: 14 }}>{title}</div>
      <div className="muted" style={{ fontSize: 12, marginTop: 4 }}>{hint}</div>
    </button>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="row" style={{ alignItems: "baseline", gap: 12 }}>
      <span className="muted" style={{ minWidth: 150, fontSize: 12, textTransform: "uppercase", letterSpacing: ".06em" }}>{label}</span>
      <span style={{ fontWeight: 600 }}>{value}</span>
    </div>
  );
}
