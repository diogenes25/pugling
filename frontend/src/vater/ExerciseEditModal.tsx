import { useEffect, useId, useRef, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { Modal } from "../components/Modal";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel } from "../components/InfoHint";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type {
  CreateExercisePayload, ExerciseDetail, ExerciseSummary, ExerciseTypeKey, InterestTagResponse, SchoolType,
  VocabItemResponse, VocabularyResponse,
} from "../lib/types";
import { LANGUAGES } from "../lib/languages";
import {
  ConfigEditor, VOCAB_FORMS, buildTypeConfig, configToEditorState, contentProblem, emptyRow,
  isContentEditable, type Row,
} from "./exerciseConfig";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { ExerciseCoverSection, GrantsSection } from "./ExerciseSharingPanels";
import { ExerciseItemMediaPanel } from "./VocabMediaPanel";

/**
 * Eine bestehende Übung ändern. Ohne diesen Dialog wäre ein Tippfehler nur durch Löschen und Neuanlegen
 * zu beheben – und Löschen ist gesperrt, sobald die Übung in einem Lehrplan steckt.
 *
 * Zwei Dinge sind hier nicht offensichtlich und tragen die ganze Mechanik:
 *
 * 1. **Der Server ersetzt die Übung vollständig** (PUT). Alles, was das Formular nicht anfasst
 *    (Bonus-Vorschlag, Ausführ-Sichtbarkeit, Kategorie), muss also aus dem geladenen Stand
 *    **mitgeschickt** werden – sonst fällt es auf den Record-Default zurück und wäre still gelöscht.
 * 2. **Vokabelpaare sind kein Config-Inhalt.** Sie liegen als eigene Item-Ebene mit stabilen Ids, an denen
 *    der Lernstand des Kindes hängt. Sie werden darum einzeln über `/items` gepflegt, und der Metadaten-PUT
 *    schickt bewusst **keine** Items/Refs mit – ein reiner Einstellungs-PUT lässt die Wortmenge unangetastet.
 * 3. **Der geladene Stand wird nach jedem Teil-Speichern nachgezogen** (B-210). Aus (1) folgt, dass die
 *    *nächste* Nutzlast auf dem Stand **danach** aufsetzen muss – sonst trägt der zweite Knopf den Inhalt
 *    vom Öffnen zurück und löscht, was der erste gerade gespeichert hat.
 */
export function ExerciseEditModal({ exercise, onClose, onSaved }: {
  exercise: ExerciseSummary;
  onClose: () => void;
  /** Nach erfolgreichem Speichern der Metadaten (Liste neu laden + Dialog schließen). */
  onSaved: () => void;
}) {
  const type = exercise.type as ExerciseTypeKey;
  const types = useExerciseTypes();
  const typeLabel = types.label(type);
  const route = types.route(type);
  const [detail, setDetail] = useState<ExerciseDetail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  /*
   * Das Metadaten-Formular liegt **hier**, nicht im `MetaEditor` — weil der Dialog zwei Speicher-Knöpfe
   * hat und beide denselben Vollersatz-PUT schicken. Solange der Inhalts-Knopf sich seine Nutzlast aus
   * `detail` zusammenbaute, drehte er jede Änderung der Metadaten still zurück: sichtbar wurde das erst
   * mit der Einkleidung (B-206), die man seither ändern *kann* — der Chip war weg, ein „Inhalt speichern"
   * schickte ihn wieder hin. Ein State, zwei Leser.
   */
  const [form, setForm] = useState<MetaForm | null>(null);
  /*
   * „Der geladene Stand ist nicht mehr der des Servers." Gesetzt, sobald ein Teil-Speichern durch ist, und
   * erst zurückgenommen, wenn das Nachladen gelandet ist. Solange er steht, sperrt er **beide**
   * Speicher-Knöpfe: jeder von ihnen schickt einen Vollersatz-PUT, und ein Vollersatz aus einem veralteten
   * Stand ist genau der stille Datenverlust, um den es hier geht (B-210).
   */
  const [veraltet, setVeraltet] = useState(false);
  const [nachladeFehler, setNachladeFehler] = useState<string | null>(null);
  const [laedtNach, setLaedtNach] = useState(false);
  /*
   * Jeder Fehlversuch zählt hoch, auch wenn der Text derselbe ist. Der Zähler steuert den Fokus-Effekt:
   * ohne ihn ändert React beim zweiten Scheitern keinen Knoten, `role="alert"` sagt nichts erneut an, und
   * für einen Screenreader ist „wieder gescheitert" von „der Knopf tut nichts" nicht zu unterscheiden.
   */
  const [fehlversuche, setFehlversuche] = useState(0);
  const erneutLadenRef = useRef<HTMLButtonElement>(null);
  /*
   * „Ein Vollersatz-PUT ist unterwegs." Der Dialog hält das selbst, weil die beiden Editoren **zwei**
   * `useAction`-Instanzen haben und deren Sperre je Instanz gilt (`frontend/CLAUDE.md`). Ohne diesen Riegel
   * bliebe der eine Knopf klickbar, während der andere speichert – und der zweite PUT ginge mit dem Stand
   * von **vor** dem ersten raus. Ohne ETag am Server gewinnt der letzte Schreiber, also wäre das derselbe
   * lautlose Verlust wie in B-210, nur durch zwei ungeduldige Klicks statt durch zwei Schritte.
   */
  const [schreibt, setSchreibt] = useState(false);

  useEffect(() => {
    api.getExercise(exercise.id)
      .then((d) => { setDetail(d); setForm(initialForm(d)); })
      .catch((e) => setLoadError(errorMessage(e)));
  }, [exercise.id]);

  /**
   * Zieht den geladenen Stand nach. **Ohne `setDetail(null)`**: das hängte die Editoren aus, und der
   * Zeilen-Editor verlöre dabei seinen Zustand – nach einem Metadaten-Speichern wären ungesicherte
   * Inhaltsänderungen weg. Der Austausch findet an Ort und Stelle statt.
   *
   * Das Formular bleibt bewusst unangetastet: Es trägt bereits, was gerade gespeichert wurde, und ein
   * Zurücksetzen würde eine noch nicht gespeicherte Eingabe daneben verwerfen.
   */
  async function nachladen() {
    setVeraltet(true);
    setLaedtNach(true);
    try {
      setDetail(await api.getExercise(exercise.id));
      setNachladeFehler(null);
      setVeraltet(false);
    } catch (e) {
      // `veraltet` bleibt stehen: der PUT ist durch, unser Stand ist es nicht.
      setNachladeFehler(errorMessage(e));
      setFehlversuche((n) => n + 1);
    } finally {
      setLaedtNach(false);
    }
  }

  // Der Fokus liegt nach dem Klick auf dem Speichern-Knopf, der gerade `disabled` wurde – er fällt damit
  // auf `<body>`, und dort greift die Fokus-Falle des Dialogs nicht mehr: Tab liefe in die Seite dahinter.
  // Im Fehlerfall bleibt das dauerhaft, also holen wir ihn auf den einen Knopf, der jetzt zählt.
  useEffect(() => {
    if (nachladeFehler) erneutLadenRef.current?.focus();
  }, [nachladeFehler, fehlversuche]);

  /**
   * Der einzige Weg, den Vollersatz-PUT abzuschicken – beide Editoren gehen hier durch. Zusammengelegt,
   * damit `schreibt` **einen** Ort hat: zwei Knöpfe, die dieselbe Übung vollständig ersetzen, sind
   * fachlich eine Liste und teilen sich darum die Sperre.
   *
   * `detail`/`route` sind hier gesetzt: Der Aufruf kommt nur aus den Editoren, und die rendert der Dialog
   * erst, wenn beides steht.
   */
  async function speichern(payload: CreateExercisePayload) {
    setSchreibt(true);
    try {
      await api.updateExercise(detail!.seriesId, detail!.seriesUnitId, route!, detail!.id, payload);
    } finally {
      setSchreibt(false);
    }
  }

  /** Nach einem erfolgreichen Teil-Speichern: erst den eigenen Stand, dann die Liste dahinter. */
  async function gespeichert() {
    await nachladen();
    onSaved();
  }

  return (
    // Titel aus dem GELADENEN Stand, nicht aus der Listenzeile: Nach einem Umbenennen stünde oben sonst
    // weiter der alte Name neben dem neuen im Feld – und der vorgelesene Dialogname wäre falsch.
    <Modal label={`Übung bearbeiten: ${detail?.title ?? exercise.title}`} onClose={onClose} maxWidth={720}>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <h3 style={{ margin: 0 }}>✏️ Bearbeiten · {detail?.title ?? exercise.title}</h3>
        <span className="muted">{typeLabel}</span>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          onClick={onClose} aria-label="Schließen">×</button>
      </div>

      {loadError && <div className="banner err">{loadError}</div>}
      {/* Der Fehlerzweig des Nachladens braucht seine eigene Fläche: Der PUT war erfolgreich, das
          Erfolgsbanner steht also zu Recht – aber ab hier ist der Dialog blind, und ein weiterer
          Vollersatz aus diesem Stand würde das gerade Gespeicherte überschreiben. Darum sagt der Kasten
          beides und bietet den Weg zurück an. */}
      {nachladeFehler && (
        <div className="banner err" role="alert">
          {/* Die Servermeldung in ein eigenes Element: `errorMessage` liefert einen rohen `Error.message`
              ohne Satzzeichen („Failed to fetch"), im Fließtext liefe sie mit dem nächsten Satz zusammen. */}
          <p style={{ margin: 0 }}>Gespeichert – aber der Stand ließ sich nicht neu laden: {nachladeFehler}</p>
          <p style={{ margin: "4px 0 0" }}>
            Weiteres Speichern ist gesperrt, damit dieser Dialog nicht mit dem alten Stand über den neuen
            schreibt.
            <button type="button" ref={erneutLadenRef} className="btn ghost inline-btn"
              style={{ width: "auto", marginLeft: 8 }} disabled={laedtNach} onClick={nachladen}>
              {laedtNach ? "Lade…" : "Erneut laden"}
            </button>
          </p>
        </div>
      )}
      {!detail && !loadError && <div className="loading">Lade…</div>}
      {detail && form && (
        <>
          {type === "Vocabulary" && <ItemEditor detail={detail} />}
          {type !== "Vocabulary" && isContentEditable(type) && (
            <ContentEditor detail={detail} form={form} type={type} speichern={speichern}
              onSaved={gespeichert} gesperrt={veraltet || schreibt} />
          )}
          {type !== "Vocabulary" && !isContentEditable(type) && (
            <div className="banner">
              Der <b>Inhalt</b> dieses Typs wird hier nicht bearbeitet: seine Sätze und Wörter tragen Ids, an
              denen der Wort-Austausch und die Vokabel-Bindung hängen – ein Vollersatz würde sie neu vergeben.
              Beschreibung und Einordnung lassen sich unten ändern.
            </div>
          )}
          <MetaEditor detail={detail} form={form} setForm={setForm} type={type} speichern={speichern}
            onSaved={gespeichert} gesperrt={veraltet || schreibt} />
          {/* Titelbild und Rechte gehören zur Übung, nicht zu ihrem Inhalt – darum unter den Metadaten. */}
          <ExerciseCoverSection exerciseId={detail.id} canWrite={detail.isOwn} />
          <GrantsSection exerciseId={detail.id} isOwner={detail.isOwner} />
        </>
      )}
    </Modal>
  );
}

// ─── Metadaten (alle Typen) ───────────────────────────────────────────────────

/**
 * Baut die PUT-Nutzlast aus dem geladenen Stand plus den geänderten Feldern. Zentral, weil beide Editoren
 * (Metadaten und Inhalt) denselben Vollersatz schicken müssen – nur mit unterschiedlicher `config`.
 *
 * **Exportiert für den Test**, und der Grund ist gemessen: Die Durchreichung der Felder, die das Formular
 * *nicht* anzeigt, hing bis B-46 an zwei Kommentaren und keiner Zusicherung — und ist mit der Einkleidung
 * zum dritten Fall derselben Falle geworden. Was man nicht sieht, kann man nicht bewusst behalten.
 */
export function payloadFrom(detail: ExerciseDetail, form: MetaForm, config: unknown): CreateExercisePayload {
  return {
    title: form.title.trim(),
    description: form.description.trim() || null,
    orderIndex: detail.orderIndex,
    rewardPoints: form.rewardPoints,
    config,
    gradeMin: form.gradeMin === "" ? null : Number(form.gradeMin),
    gradeMax: form.gradeMax === "" ? null : Number(form.gradeMax),
    schoolTypes: form.schoolTypes.length > 0 ? form.schoolTypes.join(", ") : "None",
    source: form.source.trim() || null,
    categoryId: detail.categoryId,
    defaultUseLeitner: form.defaultUseLeitner,
    defaultRequireTypedTest: form.defaultRequireTypedTest,
    defaultStage: form.defaultStage === "" ? null : Number(form.defaultStage),
    defaultItemCount: form.defaultItemCount === "" ? null : Number(form.defaultItemCount),
    // Unverändert durchreichen: der PUT ersetzt die Übung, ein Weglassen würde den Bonus-Vorschlag löschen
    // bzw. die Sichtbarkeit umschalten (Letzteres verlangt Owner-Rechte → 403 für einen Write-Grantee).
    suggestedBonus: detail.suggestedBonus,
    executePublic: detail.executePublic,
    // Die Einkleidung (B-46) kommt seit B-206 aus dem FORMULAR, nicht mehr aus dem geladenen Stand: sie
    // ist jetzt sichtbar und änderbar. Die Zusicherung darüber bleibt trotzdem nötig – sie prüft nun, dass
    // der Rundlauf stimmt, statt dass ein unsichtbares Feld überlebt.
    interestTagIds: form.interestTagIds,
  };
}

/**
 * Was den Vollersatz-PUT sicher scheitern ließe, geprüft **vor** dem Absenden. Beide Knöpfe schicken
 * dieselbe Nutzlast aus demselben Formular – seit das Formular über beiden liegt (B-206), muss darum auch
 * beiden dasselbe fehlen dürfen. „Inhalt speichern" prüfte den Titel nicht und holte sich für einen
 * leergeräumten Titel einen Server-`400`, den der Vater nicht seinem Titelfeld zuordnen konnte.
 */
function metaProblem(form: MetaForm): string | null {
  return form.title.trim() ? null : "Bitte einen Titel angeben.";
}

interface MetaForm {
  title: string;
  description: string;
  rewardPoints: number;
  gradeMin: number | "";
  gradeMax: number | "";
  source: string;
  schoolTypes: SchoolType[];
  defaultUseLeitner: boolean;
  defaultRequireTypedTest: boolean;
  defaultStage: number | "";
  defaultItemCount: number | "";
  /** Nur Vokabeln: Sprachpaar der Übung (steckt in der Config, nicht in den Metadaten-Spalten). */
  sourceLang: string;
  targetLang: string;
  /** Die Einkleidung: Ids der Interessen-Stichworte, in die diese Übung gekleidet ist (B-46/B-206). */
  interestTagIds: number[];
}

export function initialForm(d: ExerciseDetail): MetaForm {
  return {
    title: d.title,
    description: d.description ?? "",
    rewardPoints: d.rewardPoints,
    gradeMin: d.gradeMin ?? "",
    gradeMax: d.gradeMax ?? "",
    source: d.source ?? "",
    // Der Server liefert Flags als kommaseparierten String ("Realschule, Gymnasium").
    schoolTypes: (d.schoolTypes ?? "").split(",").map((s) => s.trim())
      .filter((s): s is SchoolType => SCHOOL_TYPES.includes(s as SchoolType)),
    defaultUseLeitner: d.defaultUseLeitner,
    defaultRequireTypedTest: d.defaultRequireTypedTest,
    defaultStage: d.defaultStage ?? "",
    defaultItemCount: d.defaultItemCount ?? "",
    // Kopie, keine Referenz: die Chips ändern die Liste, und der geladene Stand ist der Bezugspunkt,
    // gegen den der Dialog seine Änderung misst.
    interestTagIds: [...d.interestTagIds],
    ...langsOf(d),
  };
}

/**
 * Das Sprachpaar aus der Config. Übungen, die die frühere Oberfläche angelegt hat, haben **keins** – und
 * ohne es kann der Server kein inline ergänztes Wort im Store anlegen (der Item-Endpunkt braucht die
 * Sprachcodes). Genau darum ist es hier editierbar: sonst wäre so eine Übung nicht mehr zu reparieren.
 */
function langsOf(d: ExerciseDetail): { sourceLang: string; targetLang: string } {
  const c = (d.config ?? {}) as { sourceLang?: string | null; targetLang?: string | null };
  return { sourceLang: c.sourceLang ?? "", targetLang: c.targetLang ?? "" };
}

function MetaEditor({ detail, form, setForm, type, speichern, onSaved, gesperrt }: {
  detail: ExerciseDetail; form: MetaForm; setForm: (f: (prev: MetaForm | null) => MetaForm) => void;
  type: ExerciseTypeKey; speichern: (payload: CreateExercisePayload) => Promise<void>; onSaved: () => void;
  /**
   * Der Dialog erlaubt gerade kein Schreiben: der geladene Stand wird nachgezogen, das ist gescheitert,
   * oder der **andere** Knopf speichert bereits.
   */
  gesperrt: boolean;
}) {
  const action = useAction();
  // Namen der Kästchen-Gruppen (B-236): die Überschrift benennt die Gruppe per `aria-labelledby`.
  const groupId = useId();

  function up<K extends keyof MetaForm>(k: K, v: MetaForm[K]) {
    setForm((f) => ({ ...(f ?? form), [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const problem = metaProblem(form);
    if (problem) { action.fail(problem); return; }
    /*
     * Die Config bleibt inhaltlich, wie sie ist – nur das Sprachpaar der Vokabelübung kommt aus dem
     * Formular (ohne es kann der Server kein inline ergänztes Wort im Store anlegen).
     *
     * Dass `detail.config` hier **der Server-Stand von jetzt** ist und nicht der vom Öffnen, hängt am
     * Nachladen im Dialog (B-210). Die Alternative wäre gewesen, sich zu merken, was der Inhalts-Knopf
     * zuletzt geschickt hat – aber der Server normalisiert die Config beim Speichern und formt sie für
     * die Antwort (`NormalizeConfig`, `ConfigForResponse`: vergebene Wort-Ids, `_self`-Links). „Was ich
     * geschickt habe" wäre damit eine dritte Wahrheit, die von der des Servers abweichen darf.
     */
    const config = type === "Vocabulary"
      ? { ...(detail.config as Record<string, unknown>),
          sourceLang: form.sourceLang || null, targetLang: form.targetLang || null }
      : detail.config;
    const ok = await action.run(() => speichern(payloadFrom(detail, form, config)), "Gespeichert.");
    if (ok) onSaved();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <h4 className="h-section" style={{ fontSize: 16, margin: 0 }}>Beschreibung & Einordnung</h4>
      <div className="form-grid">
        <div className="field"><label htmlFor="ed-title">Titel</label>
          <input id="ed-title" value={form.title} onChange={(e) => up("title", e.target.value)} /></div>
        <div className="field"><FieldLabel htmlFor="ed-points" topic="exercisePoints">Punkte</FieldLabel>
          <input id="ed-points" type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ed-grade-min">Klasse von</label>
          <input id="ed-grade-min" type="number" min={1} max={13} value={form.gradeMin}
            onChange={(e) => up("gradeMin", e.target.value === "" ? "" : Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ed-grade-max">Klasse bis</label>
          <input id="ed-grade-max" type="number" min={1} max={13} value={form.gradeMax}
            onChange={(e) => up("gradeMax", e.target.value === "" ? "" : Number(e.target.value))} /></div>
        <div className="field"><FieldLabel htmlFor="ed-source" topic="exerciseSource">Quelle (Lehrbuch)</FieldLabel>
          <input id="ed-source" value={form.source} onChange={(e) => up("source", e.target.value)} /></div>
        <div className="field"><FieldLabel htmlFor="ed-item-count" topic="defaultItemCount">Standard-Menge</FieldLabel>
          <input id="ed-item-count" type="number" min={1} placeholder="alle" value={form.defaultItemCount}
            onChange={(e) => up("defaultItemCount", e.target.value === "" ? "" : Number(e.target.value))} /></div>
      </div>
      <div className="field">
        <label htmlFor="ed-description">Beschreibung <span className="muted">(optional)</span></label>
        <textarea id="ed-description" rows={2} value={form.description} onChange={(e) => up("description", e.target.value)} />
      </div>
      <div className="field" role="group" aria-labelledby={`${groupId}-schools`}>
        <FieldLabel id={`${groupId}-schools`} topic="exerciseSchoolTypes">Schularten</FieldLabel>
        <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
          {SCHOOL_TYPES.map((s) => (
            <label key={s} className="checkline">
              <input type="checkbox" checked={form.schoolTypes.includes(s)}
                onChange={() => up("schoolTypes", form.schoolTypes.includes(s)
                  ? form.schoolTypes.filter((x) => x !== s) : [...form.schoolTypes, s])} /> {s}
            </label>
          ))}
        </div>
      </div>
      <InterestDressing
        tagIds={form.interestTagIds}
        onChange={(ids) => up("interestTagIds", ids)}
        disabled={action.busy}
      />
      <div className="field" role="group" aria-labelledby={`${groupId}-standards`}>
        <label><span id={`${groupId}-standards`}>Lern-Standards</span> <span className="muted">(Lehrplan-Positionen erben diese, können sie aber übersteuern)</span></label>
        <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
          <label className="checkline"><input type="checkbox" checked={form.defaultUseLeitner}
            onChange={(e) => up("defaultUseLeitner", e.target.checked)} /> Leitner-Kasten</label>
          <label className="checkline"><input type="checkbox" checked={form.defaultRequireTypedTest}
            onChange={(e) => up("defaultRequireTypedTest", e.target.checked)} /> nur getippte Tests</label>
        </div>
      </div>
      {type === "Vocabulary" && (
        <>
          <div className="field" style={{ maxWidth: 300 }}>
            <FieldLabel htmlFor="ed-stage" topic="defaultStage">Standard-Abfrageform</FieldLabel>
            <select id="ed-stage" value={form.defaultStage}
              onChange={(e) => up("defaultStage", e.target.value === "" ? "" : Number(e.target.value))}>
              {VOCAB_FORMS.map((f) => <option key={f.label} value={f.value}>{f.label}</option>)}
            </select>
          </div>
          <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="ed-src">Quellsprache</label>
              <select id="ed-src" value={form.sourceLang} onChange={(e) => up("sourceLang", e.target.value)}>
                <option value="">– nicht gesetzt –</option>
                {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 180 }}>
              <label htmlFor="ed-tgt">Zielsprache</label>
              <select id="ed-tgt" value={form.targetLang} onChange={(e) => up("targetLang", e.target.value)}>
                <option value="">– nicht gesetzt –</option>
                {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
              </select>
            </div>
          </div>
          {(!form.sourceLang || !form.targetLang) && (
            <p className="sub" style={{ margin: 0 }}>
              Ohne Sprachpaar kann ein neues Wort nur <strong>aus dem Store</strong> übernommen werden –
              „anlegen &amp; hinzufügen" braucht die Sprachcodes. Setze sie hier und speichere.
            </p>
          )}
        </>
      )}
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }}
        disabled={action.busy || gesperrt}>
        {action.busy ? "Speichere…" : "Beschreibung speichern"}
      </button>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}

// ─── Inhalt der Nicht-Vokabel-Typen (Config als Ganzes) ───────────────────────

/**
 * Diese Typen tragen ihren Inhalt in der Config, also wird sie als Ganzes ersetzt. Der Zeilen-Editor ist
 * derselbe wie beim Anlegen; vorbelegt wird über `configToEditorState` – die Gegenrichtung zu
 * `buildTypeConfig`.
 */
function ContentEditor({ detail, form, type, speichern, onSaved, gesperrt }: {
  detail: ExerciseDetail; form: MetaForm; type: ExerciseTypeKey;
  speichern: (payload: CreateExercisePayload) => Promise<void>; onSaved: () => void;
  /** Siehe `MetaEditor`: derselbe Riegel, weil derselbe Vollersatz-PUT dahinter steht. */
  gesperrt: boolean;
}) {
  const initial = configToEditorState(type, detail.config);
  const [rows, setRows] = useState<Row[]>(initial.rows);
  const [extra, setExtra] = useState<Row>(initial.extra);
  const action = useAction();

  function patchRow(i: number, patch: Row) { setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...patch } : r))); }
  function addRow() { setRows((rs) => [...rs, emptyRow(type)]); }
  function removeRow(i: number) { setRows((rs) => (rs.length > 1 ? rs.filter((_, idx) => idx !== i) : rs)); }

  async function save() {
    // Erst die fachliche Prüfung des Editors – sie erspart einen Rundlauf mit sicherem 400. Der Titel
    // gehört dazu: dieser Knopf schickt ihn mit, also kann er ihn auch leer mitschicken.
    const problem = metaProblem(form) ?? contentProblem(type, rows, extra, 0);
    if (problem) { action.fail(problem); return; }
    const ok = await action.run(() => speichern(
      // Das geteilte Formular, nicht `initialForm(detail)`: sonst schickt dieser Knopf den Stand von vor
      // dem Öffnen zurück und macht eine gerade gespeicherte Metadaten-Änderung wieder rückgängig.
      payloadFrom(detail, form, buildTypeConfig(type, rows, extra))), "Inhalt gespeichert.");
    if (ok) onSaved();
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>Inhalt</h4>
      <ConfigEditor type={type} rows={rows} extra={extra} setExtra={setExtra}
        patchRow={patchRow} addRow={addRow} removeRow={removeRow} />
      <button type="button" className="btn inline-btn" style={{ width: "auto", marginTop: 10 }}
        disabled={action.busy || gesperrt} onClick={save}>
        {action.busy ? "Speichere…" : "Inhalt speichern"}
      </button>
      <StatusBanner message={action.message} />
    </section>
  );
}

// ─── Vokabelpaare (eigene Item-Ebene mit stabilen Ids) ────────────────────────

/**
 * Die Wortpaare einer Vokabelübung. Jede Änderung geht einzeln über `/items`, damit die Ids – und damit der
 * Lernstand des Kindes an den überlebenden Wörtern – erhalten bleiben. Neue Wörter landen am Ende; das ist
 * die einzige Einfügeart, die keine Position verschiebt und deshalb auch bei einer laufenden Übung erlaubt ist.
 */
function ItemEditor({ detail }: { detail: ExerciseDetail }) {
  const items = useAsync<VocabItemResponse[]>(
    () => api.exerciseItems(detail.seriesId, detail.seriesUnitId, detail.id), [detail.id]);
  const action = useAction();

  async function act(fn: () => Promise<unknown>, okText: string) {
    if (await action.run(fn, okText)) items.reload();
  }

  /*
   * Weg vom Wortpaar zum Store-Eintrag: Die Übung zeigt nur Wort und Übersetzung, gepflegt werden Wortart,
   * Grundform, Tags und Bilder aber im Vokabel-Store – und dorthin gab es von hier keinen Weg. Sprachpaar
   * kommt mit, damit die Zielseite nicht auf ihrem Standard (en→de) stehen bleibt und den Treffer verdeckt.
   */
  const { sourceLang, targetLang } = langsOf(detail);
  function storeHref(word: string): string {
    const p = new URLSearchParams({ search: word });
    if (sourceLang) p.set("src", sourceLang);
    if (targetLang) p.set("tgt", targetLang);
    return `/vater/vocab?${p}`;
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>
        Wortpaare {items.data ? `(${items.data.length})` : ""}
      </h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Wörter kommen aus dem Vokabel-Store und bleiben über Übungen hinweg verknüpft. Der Lernstand deines
        Kindes hängt am einzelnen Wort – Ergänzen und Entfernen lässt die übrigen unberührt.
      </p>

      {items.error && <div className="banner err">{items.error}</div>}
      {/* frontend/CLAUDE.md, "Wiederkehrende Falle beim Platzhalter": both parts of
          `loading && data === null` are required. `loading` alone would hide the already visible table
          during a `reload`, since `useAsync` keeps `data` but resets `loading`; `data === null` alone
          would show the spinner forever next to an error, since `data` stays null after a failed load. */}
      {items.loading && items.data === null ? <div className="loading">Lade Wortpaare…</div> : items.data && (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>#</th><th>Wort</th><th>Übersetzung</th><th>Hinweis</th><th /></tr></thead>
            <tbody>
              {items.data.map((it, i) => (
                <ItemRow key={it.id} item={it} position={i + 1} busy={action.busy} exerciseId={detail.id}
                  storeHref={storeHref(it.front)}
                  onHint={(hint) => act(() => api.patchExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id, { hint }), "Hinweis gespeichert.")}
                  onRemove={() => {
                    if (!confirmAction(`„${it.front} → ${it.back}" aus dieser Übung entfernen? Die Vokabel bleibt im Store.`)) return;
                    act(() => api.deleteExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id), "Wort entfernt.");
                  }} />
              ))}
              {items.data.length === 0 && <tr><td colSpan={5} className="muted">Noch keine Wörter – unten hinzufügen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <AddItem detail={detail} busy={action.busy}
        onAdd={(body) => act(() => api.addExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, body), "Wort hinzugefügt.")} />

      <StatusBanner message={action.message} />
    </section>
  );
}

function ItemRow({ item, position, busy, exerciseId, storeHref, onHint, onRemove }: {
  item: VocabItemResponse; position: number; busy: boolean; exerciseId: number;
  /** Ziel im Vokabel-Store, vorgefiltert auf dieses Wort. */
  storeHref: string;
  onHint: (hint: string) => void; onRemove: () => void;
}) {
  const [hint, setHint] = useState(item.hint ?? "");
  // Das Bild-Panel steht eingeklappt: es lädt eigene Daten, und bei 30 Wörtern wären das 30 Abfragen.
  const [showMedia, setShowMedia] = useState(false);
  const dirty = hint !== (item.hint ?? "");
  return (
    <>
      <tr>
        <td className="num">{position}</td>
        <td>
          {/*
            Bewusst ein neues Tab (`target`) und kein Router-Link: Dieser Dialog hält ungespeicherte Eingaben
            (Hinweise, Metadaten). Ein Wechsel im gleichen Tab würde ihn schließen und sie verwerfen.
          */}
          <a href={storeHref} target="_blank" rel="noreferrer"
            title={`„${item.front}" im Vokabel-Store öffnen (Wortart, Grundform, Tags, Bilder)`}>
            {item.front}
          </a>
        </td>
        <td className="muted">{item.back}</td>
        <td>
          <span className="row" style={{ gap: 4 }}>
            <input aria-label={`Hinweis für ${item.front}`} value={hint} onChange={(e) => setHint(e.target.value)}
              placeholder="–" style={{ maxWidth: 160 }} />
            {dirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              disabled={busy} onClick={() => onHint(hint)}>OK</button>}
          </span>
        </td>
        <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            aria-expanded={showMedia} aria-label={`Bild für ${item.front} nur in dieser Übung`}
            onClick={() => setShowMedia((v) => !v)}>🖼️ Bild</button>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            aria-label={`${item.front} entfernen`} disabled={busy} onClick={onRemove}>Entfernen</button>
        </td>
      </tr>
      {showMedia && (
        <tr>
          <td colSpan={5}>
            <ExerciseItemMediaPanel exerciseId={exerciseId} itemId={item.id} word={item.front} />
          </td>
        </tr>
      )}
    </>
  );
}

/**
 * Ein Wort ergänzen – aus dem Store gesucht oder frisch angelegt. Die Store-Suche ist der Regelweg: nur so
 * teilt die Übung das Wort (und seine Bilder/Audios) mit allen anderen Übungen, die es benutzen.
 */
function AddItem({ detail, busy, onAdd }: {
  detail: ExerciseDetail; busy: boolean; onAdd: (body: { vocabularyId?: number; front?: string; back?: string }) => void;
}) {
  const [search, setSearch] = useState("");
  const [hits, setHits] = useState<VocabularyResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [front, setFront] = useState("");
  const [back, setBack] = useState("");
  const [err, setErr] = useState<string | null>(null);

  // Sprachpaar der Übung: inline angelegte Wörter brauchen es, und es hält fremdsprachige Treffer heraus.
  const { sourceLang, targetLang } = langsOf(detail);
  const canCreateInline = !!sourceLang && !!targetLang;

  async function doSearch(e: React.FormEvent) {
    e.preventDefault();
    setSearching(true);
    setErr(null);
    try {
      const page = await api.vocabulary({
        search: search.trim() || undefined,
        sourceLanguage: sourceLang || undefined, targetLanguage: targetLang || undefined, take: 20,
      });
      setHits(page.items);
    } catch (e2) {
      // Ohne diese Meldung sähe eine fehlgeschlagene Suche wie „keine Treffer" aus.
      setErr(errorMessage(e2));
    } finally { setSearching(false); }
  }

  return (
    <div className="card" style={{ marginTop: 10 }}>
      <h5 style={{ margin: "0 0 8px" }}>Wort ergänzen</h5>
      <form className="row" style={{ gap: 6, marginBottom: 8 }} onSubmit={doSearch}>
        <input aria-label="Vokabel-Store durchsuchen" value={search} onChange={(e) => setSearch(e.target.value)}
          placeholder="Im Store suchen…" style={{ maxWidth: 260 }} />
        <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={searching}>Suchen</button>
      </form>
      {err && <div className="banner err" style={{ marginBottom: 8 }}>{err}</div>}
      {hits.length > 0 && (
        <div style={{ maxHeight: 160, overflowY: "auto", display: "grid", gap: 4, marginBottom: 8 }}>
          {hits.map((v) => (
            <div key={v.id} className="row" style={{ gap: 8, alignItems: "center" }}>
              <span>{v.word} <span className="muted">→ {v.translation}</span></span>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
                disabled={busy} onClick={() => onAdd({ vocabularyId: v.id })}>+ übernehmen</button>
            </div>
          ))}
        </div>
      )}
      <div className="row" style={{ gap: 6, alignItems: "flex-end" }}>
        <div className="field" style={{ flex: 1 }}><label htmlFor="ai-front">Neu: Wort</label>
          <input id="ai-front" value={front} onChange={(e) => setFront(e.target.value)} /></div>
        <div className="field" style={{ flex: 1 }}><label htmlFor="ai-back">Übersetzung</label>
          <input id="ai-back" value={back} onChange={(e) => setBack(e.target.value)} /></div>
        {/* Ohne Sprachpaar in der Config kann der Server das Wort nicht im Store anlegen (400) – dann
            bleibt der Weg über die Suche, und die Beschreibung oben nennt die Abhilfe. */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={busy || !canCreateInline || !front.trim() || !back.trim()}
          title={canCreateInline ? undefined : "Erst das Sprachpaar der Übung setzen – unten bei „Beschreibung & Einordnung“."}
          onClick={() => { onAdd({ front: front.trim(), back: back.trim() }); setFront(""); setBack(""); }}>
          + anlegen &amp; hinzufügen
        </button>
      </div>
    </div>
  );
}

// ─── Einkleidung (B-206) ──────────────────────────────────────────────────────

/**
 * Zeigt und ändert, in welche Interessen eine Übung **eingekleidet** ist.
 *
 * Vorher reiste das Feld unsichtbar durch den Vollersatz-PUT: Der KI-Creator trug es ein, niemand sah es,
 * und ein anderswo gelöschtes Stichwort machte das Speichern unmöglich – mit einer Fehlermeldung, die eine
 * Größe nannte, für die es kein Feld gab.
 *
 * Lädt die Auswahl selbst, gibt seinen Zustand aber **nach oben**: Die Einkleidung gehört ins `MetaForm`,
 * weil `payloadFrom` sie mitschicken muss.
 */
function InterestDressing({ tagIds, onChange, disabled }: {
  tagIds: number[];
  onChange: (ids: number[]) => void;
  disabled: boolean;
}) {
  const known = useAsync<InterestTagResponse[]>(() => api.interestTags(), []);

  return (
    <div className="field">
      <FieldLabel htmlFor="ed-dressing" topic="exerciseDressing">Eingekleidet in</FieldLabel>
      {/* Ein Ausfall der Liste darf die Einkleidung nicht verstecken: die Chips unten stehen auch ohne sie
          (dann mit Id statt Name), und nur das Nachtragen fällt weg. Sonst sähe „Laden gescheitert" aus wie
          „nicht eingekleidet" – die Verwechslung aus B-174. */}
      {known.error && <div className="banner err" style={{ marginBottom: 6 }}>{known.error}</div>}
      <InterestChips
        tagIds={tagIds}
        known={known.data ?? []}
        loading={known.data === null && !known.error}
        onChange={onChange}
        disabled={disabled || known.loading}
      />
    </div>
  );
}

/**
 * Der reine, props-getriebene Teil – **darum** getrennt: so ist er ohne gefälschtes `fetch` prüfbar
 * (`frontend/CLAUDE.md`: kein nachgebauter Bildschirm mit Netz).
 */
export function InterestChips({ tagIds, known, onChange, disabled, loading = false }: {
  tagIds: number[];
  known: InterestTagResponse[];
  onChange: (ids: number[]) => void;
  disabled: boolean;
  /** Die Auswahl ist noch unterwegs – dann ist „Name nicht dabei" noch keine Aussage. */
  loading?: boolean;
}) {
  const [pick, setPick] = useState("");
  const available = known.filter((t) => !tagIds.includes(t.id));

  return (
    <>
      <div className="row" style={{ gap: 6, flexWrap: "wrap", marginBottom: 8 }}>
        {tagIds.length === 0
          // Ausdrücklich, nicht leer: „nicht eingekleidet" darf nicht aussehen wie „Feld gibt es nicht".
          ? <span className="muted">Nicht eingekleidet – diese Übung erzählt neutral.</span>
          : tagIds.map((id) => {
            // Nach Id auflösen. Fehlt der Name (die Liste ist gedeckelt), steht die Id da statt gar nichts:
            // ein Chip ohne Beschriftung wäre wieder eine Größe, die man nicht sieht. **Während** die
            // Liste lädt, ist „nicht dabei" aber noch keine Aussage – dann würde bei jedem Öffnen des
            // Dialogs für einen Anstrich eine Id aufblitzen, obwohl AK 1 Namen zusagt.
            const tag = known.find((t) => t.id === id);
            const name = tag ? tag.label : loading ? "…" : `Stichwort #${id}`;
            return (
              <span key={id} className="chip">
                {name}
                <button
                  type="button" className="btn ghost small" style={{ width: "auto", padding: "2px 8px" }}
                  aria-label={`Einkleidung „${name}" entfernen`}
                  disabled={disabled}
                  onClick={() => onChange(tagIds.filter((x) => x !== id))}
                >×</button>
              </span>
            );
          })}
      </div>
      <div className="row" style={{ gap: 8 }}>
        <select
          id="ed-dressing" value={pick} disabled={disabled || available.length === 0}
          onChange={(e) => setPick(e.target.value)} style={{ maxWidth: 260 }}
        >
          <option value="">
            {loading ? "– Liste lädt –"
              : available.length === 0 ? "– nichts mehr zur Auswahl –"
              : "– Interesse wählen –"}
          </option>
          {available.map((t) => <option key={t.id} value={t.id}>{t.label}</option>)}
        </select>
        <button
          type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={disabled || pick === ""}
          onClick={() => { onChange([...tagIds, Number(pick)]); setPick(""); }}
        >Einkleidung hinzufügen</button>
      </div>
    </>
  );
}
