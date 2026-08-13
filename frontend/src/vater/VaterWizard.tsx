import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import { SCHOOL_TYPES } from "../lib/labels";
import { TruncationHint } from "../components/ListControls";
import { FieldLabel, InfoHint } from "../components/InfoHint";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { authorText } from "./ExerciseAttribution";
import { auswahlNachFilterwechsel, unsichtbareAuswahl, wizardFilterKey, wizardSearchParams } from "./wizardSearch";
import { newWizardProgress, runWizardFinish } from "./wizardFinish";
import type { WizardProgress } from "./wizardFinish";
import type {
  CategoryResponse, ChildResponse, ExerciseSummary, Paged, SchoolType, SubjectResponse,
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
  const [topic, setTopic] = useState("");
  const [goal, setGoal] = useState<Goal>("Klassenarbeit");
  const [intensity, setIntensity] = useState<Intensity>("Normal");

  // --- Schritt 3: Übungen (Katalog) ---
  const [contentSearch, setContentSearch] = useState("");
  // Drei weitere Filter, die der Server längst kann und der Assistent bisher nicht anbot (B-18). Alle
  // optional und AND-verknüpft wie serverseitig; leer heißt „alle".
  const [categoryId, setCategoryId] = useState<number | "">("");
  const [typeKey, setTypeKey] = useState("");
  const [sourceSearch, setSourceSearch] = useState("");
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
  const [touchedFineTune, setTouchedFineTune] = useState(false);
  // „Der Vater hat die Kind-Art selbst gewählt" – dieselbe Idee wie `touchedFineTune`: eine Vorbelegung
  // darf nur greifen, solange niemand entschieden hat.
  const [touchedMode, setTouchedMode] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
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

  // Passende Katalog-Übungen (die Bausteine des Plans – jede wird zu einer Position). Die Gesamtzahl wird
  // mitgeführt, weil der Server nur eine Seite liefert: ohne sie wäre eine gekappte Liste nicht erkennbar.
  const exercises = useAsync<Paged<ExerciseSummary>>(
    // Gefiltert wird SERVERSEITIG, nicht über der geladenen Seite: sonst durchsuchten die Felder nur die
    // erste Seite und meldeten „nichts gefunden", obwohl es Treffer gibt. Die Abbildung steht in
    // `wizardSearch.ts`, weil „Alle wählen" dieselbe braucht.
    () => (subjectId === "" ? Promise.resolve({ items: [], total: 0 })
      : api.searchExercises(wizardSearchParams({
        subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
        search: contentSearch, categoryId, type: typeKey, source: sourceSearch,
      }))),
    [subjectId, effectiveGrade, effectiveSchoolType, contentSearch, categoryId, typeKey, sourceSearch],
  );

  // Arten des gewählten Fachs; die Liste hängt am Fach, nicht am Katalog insgesamt.
  const categories = useAsync<CategoryResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.categories(Number(subjectId))), [subjectId]);
  // Übungstypen kommen aus dem Server-Manifest, nie aus einer Tabelle im Frontend (frontend/CLAUDE.md).
  const types = useExerciseTypes();

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
   * Der Schlüssel kommt aus `wizardFilterKey`, nicht aus der Abhängigkeitsliste oben: ein Objektvergleich
   * per Referenz feuerte bei jedem Rendern, und getrimmte Eingaben („abc " → "abc") sind dieselbe Suche.
   */
  const filterKey = subjectId === "" ? "" : wizardFilterKey({
    subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
    search: contentSearch, categoryId, type: typeKey, source: sourceSearch,
  });
  /*
   * Der Schlüssel, für den die aktuelle Auswahl gilt. Nach dem Effekt trägt die Ref immer den *aktuellen*
   * Schlüssel – „vorig" ist sie nur innerhalb des Effektrumpfs. Sie ist damit auch das Generationen-Gate
   * für `selectAll`: dessen Nachschlag darf nur zurückschreiben, wenn seither niemand die Suche geändert
   * hat.
   */
  const geltenderFilterKey = useRef(filterKey);
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
  useEffect(() => { merken(exercises.data?.items ?? []); }, [exercises.data]);

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
    if (total <= filteredExercises.length) { setSelected(filteredExercises.map((e) => e.id)); return; }

    const keyBeimStart = filterKey;
    setSelectAllBusy(true);
    try {
      const alle = await api.searchExercises(wizardSearchParams({
        subjectId: Number(subjectId), grade: effectiveGrade, schoolType: effectiveSchoolType,
        search: contentSearch, categoryId, type: typeKey, source: sourceSearch,
      }, 500));
      // `merken` darf auch bei veralteter Antwort laufen - ein Metadaten-Vorrat schadet nie.
      merken(alle.items);
      if (geltenderFilterKey.current !== keyBeimStart) return;
      setSelected(alle.items.map((e) => e.id));
    } catch (err) {
      if (geltenderFilterKey.current !== keyBeimStart) return;
      // Zurückfallen ja, schweigen nein: Sonst stünden 100 statt 400 Positionen im Plan — genau die
      // Kappung, die dieser Knopf abschaffen soll, nur unbeobachtbar.
      setSelected(filteredExercises.map((e) => e.id));
      setError(`Konnte nicht alle Treffer laden – es sind nur die ${filteredExercises.length} geladenen `
        + `gewählt. ${errorMessage(err)}`);
    } finally {
      setSelectAllBusy(false);
    }
  }

  function canAdvance(): string | null {
    if (step === 0) {
      if (mode === "existing") return childId === "" ? "Bitte ein Kind wählen." : null;
      return newName.trim() ? null : "Bitte einen Namen eingeben.";
    }
    if (step === 1) return subjectId === "" ? "Bitte ein Fach wählen." : null;
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
          subjectId: subjectId === "" ? null : Number(subjectId),
          durationDays,
          startDate,
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
        },
        titleOf: (id) => gesehen.current.get(id)?.title ?? `#${id}`,
        typeOf: (id) => gesehen.current.get(id)?.type ?? "",
      }, api);
      // `null` heißt: es läuft schon ein Durchgang (zweiter Klick). Der erste besitzt das Ergebnis und
      // navigiert – `busy` bleibt absichtlich stehen.
      if (planId === null) return;
      nav(`/vater/plan/${planId}`);
    } catch (err) {
      setError(errorMessage(err));
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

      <Stepper step={step} onGo={(i) => i < step && setStep(i)} />

      {/* -------- Schritt 1: Kind -------- */}
      {step === 0 && (
        <section className="card" style={{ display: "flex", flexDirection: "column", gap: 14 }}>
          <h3 style={{ margin: 0 }}>Für welches Kind?</h3>
          <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
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
            <div className="form-grid">
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
          <div className="form-grid">
            <div className="field"><label>Fach</label>
              <select aria-label="Fach" value={subjectId} onChange={(e) => {
                // Die Art gehört zum alten Fach – stehen gelassen filterte sie auf eine Kategorie, die es
                // im neuen gar nicht gibt, und die Trefferliste bliebe ohne sichtbaren Grund leer. Hier
                // statt in einem Effekt, weil hier die Entscheidung fällt: ein Effekt liefe erst NACH der
                // Suchabfrage und schickte eine überflüssige Runde mit altem Filter los.
                // Typ und Quelle bleiben: der Typ kommt aus dem globalen Manifest, die Quelle ist Freitext
                // über Lehrwerke – beides fachunabhängig, ein Reset würde Eingabe wegwerfen.
                setCategoryId("");
                setSubjectId(e.target.value === "" ? "" : Number(e.target.value));
              }}>
                <option value="">– Fach wählen –</option>
                {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div className="field"><label htmlFor="wiz-topic">Thema / Kapitel (optional)</label><input id="wiz-topic" value={topic} onChange={(e) => setTopic(e.target.value)} placeholder="z.B. Unité 2 – En ville" /></div>
          </div>

          <div>
            <label className="sub" style={{ display: "block", marginBottom: 8 }}>Was ist das Ziel?</label>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
              {(Object.keys(GOALS) as Goal[]).map((g) => (
                <OptionCard key={g} active={goal === g} onClick={() => setGoal(g)}
                  title={`${GOALS[g].emoji} ${GOALS[g].label}`} hint={GOALS[g].hint} />
              ))}
            </div>
          </div>

          <div>
            <label className="sub" style={{ display: "block", marginBottom: 8 }}>Wie intensiv soll geübt werden?</label>
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
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
          <div className="row" style={{ gap: 8, flexWrap: "wrap", alignItems: "flex-end" }}>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="wiz-category">Art</label>
              <select id="wiz-category" value={categoryId}
                onChange={(e) => setCategoryId(e.target.value === "" ? "" : Number(e.target.value))}>
                <option value="">– alle Arten –</option>
                {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="wiz-type">Typ</label>
              <select id="wiz-type" value={typeKey} onChange={(e) => setTypeKey(e.target.value)}>
                <option value="">– alle Typen –</option>
                {types?.all.map((t) => <option key={t.type} value={t.type}>{t.label}</option>)}
              </select>
            </div>
            <input aria-label="Quelle" placeholder="Quelle, z. B. Green Line 1…" style={{ maxWidth: 220 }}
              value={sourceSearch} onChange={(e) => setSourceSearch(e.target.value)} />
          </div>

          <div className="row">
            <span className="sub">{filteredExercises.length} passende Übungen</span>
            {filteredExercises.length > 0 && (
              <button type="button" className="btn ghost inline-btn" style={{ marginLeft: "auto" }}
                disabled={selectAllBusy || exercises.loading} onClick={selectAll}>
                {selectAllBusy ? "Lade alle…" : "Alle wählen"}
              </button>
            )}
          </div>
          <TruncationHint shown={exercises.data?.items.length ?? 0} total={exercises.data?.total ?? 0} />

          {exercises.loading && exercises.data === null ? <div className="loading">Lade Übungen…</div> : filteredExercises.length === 0 ? (
            <div className="banner err">
              Keine passenden Übungen im Katalog. Lege welche unter „Übungen" an (Fach {subject?.name ?? "?"}).
            </div>
          ) : (
            <div style={{ maxHeight: 340, overflowY: "auto", display: "grid", gap: 6 }}>
              {filteredExercises.map((e) => {
                const by = authorText(e);
                return (
                  <label key={e.id} className="checkline" style={{ padding: 8, border: "1px solid var(--stroke)", borderRadius: 8 }}>
                    <input type="checkbox" checked={selected.includes(e.id)} onChange={() => toggle(e.id)} />
                    {/* Anzeigename aus dem Manifest, nicht der Schlüssel: sonst bietet das Pulldown darüber
                        „Vokabelkarten" an, während die Zeile darunter „Vocabulary" sagt — ein Widerspruch in
                        einem Bildschirm. Der Rückfall auf den Schlüssel greift nur, solange das Manifest lädt. */}
                    <span>{e.title} <span className="muted">· {types?.label(e.type) ?? e.type}{by ? ` – ${by}` : ""}{e.source ? ` [${e.source}]` : ""}</span></span>
                  </label>
                );
              })}
            </div>
          )}
        </section>
      )}

      {/* -------- Schritt 4: Feinschliff -------- */}
      {step === 3 && (
        <section className="card">
          <h3 style={{ margin: "0 0 12px" }}>Feinschliff <span className="muted">(gilt für alle {selected.length} Positionen)</span></h3>
          <div className="form-grid" onChange={() => setTouchedFineTune(true)}>
            <div className="field"><label htmlFor="wiz-title">Titel</label><input id="wiz-title" title="Titel" value={title} onChange={(e) => setTitle(e.target.value)} /></div>
            <div className="field"><label htmlFor="wiz-start">Start</label><input id="wiz-start" title="Startdatum" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-duration" topic="planDuration">Dauer (Tage)</FieldLabel><input id="wiz-duration" title="Dauer" type="number" min={1} value={durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-pass" topic="goalThreshold">Bestehen ab %</FieldLabel><input id="wiz-pass" title="Bestehen ab Prozent" type="number" min={1} max={100} value={passPercent} onChange={(e) => setPassPercent(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-points" topic="pointsGoalMet">Punkte je Ziel</FieldLabel><input id="wiz-points" title="Punkte je erreichtem Ziel" type="number" min={0} value={pointsGoalMet} onChange={(e) => setPointsGoalMet(Number(e.target.value))} /></div>
            {/* Der „Stick": verpasste Pflicht kostet Münzen. 0 = reine Belohnung, Schulden sind erlaubt. */}
            <div className="field"><FieldLabel htmlFor="wiz-penalty" topic="penaltyCoins">Münz-Malus bei Versäumnis</FieldLabel><input id="wiz-penalty" title="Münz-Malus bei gerissener Pflicht" type="number" min={0} value={penaltyCoins} onChange={(e) => setPenaltyCoins(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-combo-threshold" topic="comboThreshold">Combo alle … Treffer</FieldLabel><input id="wiz-combo-threshold" title="Combo-Schwelle" type="number" min={0} value={comboThreshold} onChange={(e) => setComboThreshold(Number(e.target.value))} /></div>
            <div className="field"><FieldLabel htmlFor="wiz-combo-bonus" topic="comboBonusPoints">Combo-Bonuspunkte</FieldLabel><input id="wiz-combo-bonus" title="Combo-Bonuspunkte" type="number" min={0} value={comboBonusPoints} onChange={(e) => setComboBonusPoints(Number(e.target.value))} /></div>
            {/* Nur Vokabelübungen kennen diese Skala; für alle anderen Typen wird die Stufe weggelassen
                (siehe `wizardFinish.typeOf`) – darum sagt es hier auch dran, statt still zu wirken. */}
            <div className="field"><FieldLabel topic="defaultStage">Test-Stufe <span className="muted">(nur Vokabelübungen)</span></FieldLabel>
              <select aria-label="Test-Stufe" value={defaultStage} onChange={(e) => setDefaultStage(Number(e.target.value))}>
                <option value={1}>1 · Zeigen</option>
                <option value={2}>2 · Selbstcheck</option>
                <option value={3}>3 · Buchstaben</option>
                <option value={4}>4 · Tippen</option>
                <option value={5}>5 · Hören</option>
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
          <SummaryRow label="Zeitraum" value={`${startDate} · ${durationDays} Tage`} />
          <SummaryRow label="Je Position" value={`Test-Stufe ${defaultStage} · bestehen ab ${passPercent}% · ${pointsGoalMet} Punkte/Ziel`} />
          <SummaryRow label="Versäumnis" value={penaltyCoins > 0 ? `−${penaltyCoins} 🪙 je gerissenem Tagesziel` : "kein Malus (reine Belohnung)"} />
          <SummaryRow label="Übungen" value={`${selected.length} als Tagesziel-Positionen`} />
          <p className="sub">Danach erscheint der Plan unter „Zuweisen"; der Sohn sieht ihn sofort in seiner App.</p>
        </section>
      )}

      {error && <div className="banner err" role="status" aria-live="polite">{error}</div>}

      <div className="row" style={{ gap: 10 }}>
        {step > 0 && <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={back}>← Zurück</button>}
        <span style={{ marginLeft: "auto" }} />
        {step < STEPS.length - 1
          ? <button type="button" className="btn" style={{ width: "auto" }} onClick={next}>Weiter →</button>
          : <button type="button" className="btn lime" style={{ width: "auto" }} disabled={busy} onClick={finish}>{busy ? "…" : "✅ Lehrplan erstellen"}</button>}
      </div>
    </div>
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
    <button type="button" disabled={disabled} onClick={onClick}
      className={`btn ${active ? "" : "ghost"} inline-btn`} style={{ width: "auto", opacity: disabled ? 0.4 : 1 }}>
      {children}
    </button>
  );
}

function OptionCard({ active, onClick, title, hint }: { active: boolean; onClick: () => void; title: string; hint: string }) {
  return (
    <button type="button" onClick={onClick}
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
