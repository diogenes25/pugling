import { useEffect, useId, useRef, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { Modal } from "../components/Modal";
import { GrammarTopicPicker } from "../components/GrammarTopicChips";
import { StatusBanner } from "../components/StatusBanner";
import { RepeatedTextFields, nonEmpty } from "../components/RepeatedTextFields";
import { FieldLabel } from "../components/InfoHint";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type {
  CategoryResponse, CreateExercisePayload, ExerciseDetail, ExerciseSummary, ExerciseTypeKey, InterestTagResponse, SchoolType,
  VocabItemInput, VocabItemResponse, VocabularyResponse,
} from "../lib/types";
import { LANGUAGES } from "../lib/languages";
import {
  ArticleRequiredCheck, ConfigEditor, VOCAB_FORMS, buildTypeConfig, configToEditorState, contentProblem, emptyRow,
  isContentEditable, type Row,
} from "./exerciseConfig";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { ExerciseCoverSection, GrantsSection } from "./ExerciseSharingPanels";
import { ExerciseItemMediaPanel } from "./VocabMediaPanel";
import { ListenAusfall } from "../components/ListenAusfall";

/**
 * Die Frage vor dem Entfernen einer Vokabel aus der Übung. Der Server nimmt sie nur aus einer Übung, die in keinem
 * Lehrplan steckt (sonst `exercise_items_locked`); gespielt sein kann sie trotzdem – nach dem Löschen ihres Plans. Mit
 * dem Eintrag geht dann der Lernstand der Kinder an ihm (Cascade `ExerciseItem` → `ItemProgress`, B-502); die Vokabel
 * selbst bleibt im Store (Restrict).
 */
export const removeItemQuestion = (front: string, back: string) =>
  `„${front} → ${back}" aus dieser Übung entfernen? Die Vokabel bleibt im Store; was Kinder an diesem Eintrag gelernt `
  + "haben, geht im Lernstand mit verloren.";

/**
 * Eine bestehende Übung ändern. Ohne diesen Dialog wäre ein Tippfehler nur durch Löschen und Neuanlegen
 * zu beheben – und Löschen ist gesperrt, sobald die Übung in einem Lehrplan steckt.
 *
 * Zwei Dinge sind hier nicht offensichtlich und tragen die ganze Mechanik:
 *
 * 1. **Der Server ersetzt die Übung vollständig** (PUT). Alles, was das Formular nicht anfasst
 *    (Bonus-Vorschlag, Ausführ-Sichtbarkeit), muss also aus dem geladenen Stand
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
   * Metadaten **und** Inhalt liegen hier, weil der Dialog **einen** Speichern-Knopf hat (B-534) und der eine
   * Vollersatz-PUT beides trägt. Vorher hatte jeder Teil seinen eigenen Knopf, und jeder musste den Stand des
   * anderen mitschicken – genau daran hingen B-206 und B-210 (ein Knopf drehte die Änderung des anderen zurück).
   * Ein Knopf, eine Nutzlast: diese Fehlerklasse gibt es so nicht mehr.
   */
  const [form, setForm] = useState<MetaForm | null>(null);
  const [content, setContent] = useState<ContentState | null>(null);
  const action = useAction();
  /*
   * „Der geladene Stand ist nicht mehr der des Servers." Gesetzt, sobald ein Speichern durch ist, und erst
   * zurückgenommen, wenn das Nachladen gelandet ist. Solange er steht, sperrt er den Speichern-Knopf: die
   * Vokabel-Config kommt aus `detail` (die Wortpaare ändern sie über eigene Endpunkte), und ein Vollersatz aus
   * einem veralteten Stand ist genau der stille Datenverlust aus B-210.
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

  useEffect(() => {
    api.getExercise(exercise.id)
      .then((d) => { setDetail(d); setForm(initialForm(d)); setContent(configToEditorState(type, d.config)); })
      .catch((e) => setLoadError(errorMessage(e)));
  }, [exercise.id, type]);

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
   * Der eine Vollersatz-PUT: Metadaten aus dem Formular, die Config je nach Typ aus dem Inhalts-Editor
   * (Config-Typen), aus dem Server-Stand plus Sprachpaar (Vokabeln) oder unverändert (nicht editierbare Typen).
   *
   * Dass `detail.config` hier **der Server-Stand von jetzt** ist und nicht der vom Öffnen, hängt am Nachladen
   * (B-210): der Server normalisiert die Config beim Speichern (`NormalizeConfig`, `ConfigForResponse`), und
   * „was ich geschickt habe" wäre eine dritte Wahrheit, die von der des Servers abweichen darf.
   */
  async function save(e?: React.FormEvent) {
    e?.preventDefault();
    // Dieselbe Sperre wie am Knopf, hier für Enter: nach gescheitertem Nachladen ist `detail.config` veraltet,
    // und ein Vollersatz daraus schriebe den alten Stand über den neuen (B-210).
    if (veraltet) return;
    if (!detail || !form || !route) return;
    const editable = type !== "Vocabulary" && isContentEditable(type) && content !== null;
    // Erst die fachliche Prüfung – sie erspart einen Rundlauf mit sicherem 400.
    const problem = metaProblem(form) ?? (editable ? contentProblem(type, content.rows, content.extra, 0) : null);
    if (problem) { action.fail(problem); return; }
    const config = type === "Vocabulary" ? vocabSettingsConfig(detail.config, form)
      : editable ? buildTypeConfig(type, content.rows, content.extra)
      : detail.config;
    const ok = await action.run(
      () => api.updateExercise(detail.seriesId, detail.seriesUnitId, route, detail.id, payloadFrom(detail, form, config)),
      "Gespeichert.");
    if (!ok) return;
    await nachladen();
    onSaved();
  }

  const gesperrt = action.busy || veraltet;
  // Das Formular der Metadaten und der Speichern-Knopf darunter gehören über `form=` zusammen: so speichert
  // Enter in einem Feld, und es ist genauso gesperrt wie der Knopf (implizites Absenden über einen
  // `disabled`-Standardknopf findet nicht statt).
  const formId = useId();

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
          {type !== "Vocabulary" && isContentEditable(type) && content && (
            <ContentEditor type={type} content={content} setContent={setContent} />
          )}
          {type !== "Vocabulary" && !isContentEditable(type) && (
            <div className="banner">
              Der <b>Inhalt</b> dieses Typs wird hier nicht bearbeitet: seine Sätze und Wörter tragen Ids, an
              denen der Wort-Austausch und die Vokabel-Bindung hängen – ein Vollersatz würde sie neu vergeben.
              Beschreibung und Einordnung lassen sich unten ändern.
            </div>
          )}
          <MetaEditor detail={detail} form={form} setForm={setForm} type={type} onSubmit={save} busy={action.busy} formId={formId} />
          {/* Der einzige Speichern-Knopf des Dialogs (B-534): er sichert Inhalt und Beschreibung zusammen.
              Titelbild und Rechte darunter wirken sofort und brauchen ihn nicht. */}
          <div className="row" style={{ gap: 10, alignItems: "center" }}>
            <button type="submit" form={formId} className="btn inline-btn" style={{ width: "auto" }} disabled={gesperrt}>
              {action.busy ? "Speichere…" : "Übung speichern"}
            </button>
            <span className="sub" style={{ margin: 0 }}>
              {type === "Vocabulary" ? "Sichert Beschreibung und Einstellungen – die Wortpaare oben speichern sich einzeln."
                : isContentEditable(type) ? "Sichert Inhalt und Beschreibung zusammen."
                : "Sichert Beschreibung und Einordnung."}
            </span>
          </div>
          <StatusBanner message={action.message} style={{ marginTop: 0 }} />
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
    // Seit B-407 aus dem FORMULAR: die Art ist im Dialog änderbar und entfernbar – der einzige Ausweg, wenn eine gespielte
    // Übung mit einer Art des alten Fachs den Fachwechsel ihrer Reihe sperrt. `initialForm` füllt sie mit der geladenen.
    categoryId: form.categoryId,
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
    // Dieselbe Falle, viertes Feld (B-155): ohne diese Zeile nähme der Vollersatz der Übung ihre Themen.
    grammarTopicIds: form.grammarTopicIds,
  };
}

/**
 * Was den Vollersatz-PUT sicher scheitern ließe, geprüft **vor** dem Absenden – sonst holte sich ein
 * leergeräumter Titel einen Server-`400`, den der Vater nicht seinem Titelfeld zuordnen kann.
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
  /** Nur Vokabeln (B-421): die Übung trainiert den Artikel – steckt ebenfalls in der Config. */
  articleRequired: boolean;
  /** Die Einkleidung: Ids der Interessen-Stichworte, in die diese Übung gekleidet ist (B-46/B-206). */
  interestTagIds: number[];
  /** Die Grammatik-Themen der Übung (B-155) – aus dem fachgebundenen Vokabular. */
  grammarTopicIds: number[];
  /** Die Art (Kategorie) der Übung, `null` = keine (B-407) – aus den Arten des Reihen-Fachs. */
  categoryId: number | null;
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
    grammarTopicIds: [...d.grammarTopicIds],
    categoryId: d.categoryId ?? null,
    ...langsOf(d),
    articleRequired: (d.config as { articleRequired?: unknown } | null)?.articleRequired === true,
  };
}

/**
 * Die Config, die „Übung speichern" für eine Vokabelübung schickt: der geladene Server-Stand, darüber nur die
 * Einstellungen, die dieses Formular trägt (Sprachpaar, Artikel-Pflicht). Als eigene Funktion, damit der Weg ohne
 * nachgebauten Dialog prüfbar ist.
 */
export function vocabSettingsConfig(config: unknown, form: Pick<MetaForm, "sourceLang" | "targetLang" | "articleRequired">) {
  return {
    ...(config as Record<string, unknown>),
    sourceLang: form.sourceLang || null, targetLang: form.targetLang || null,
    articleRequired: form.articleRequired,
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

function MetaEditor({ detail, form, setForm, type, onSubmit, busy, formId }: {
  detail: ExerciseDetail; form: MetaForm; setForm: (f: (prev: MetaForm | null) => MetaForm) => void;
  type: ExerciseTypeKey;
  /** Enter in einem Feld speichert die ganze Übung – derselbe Weg wie der Knopf des Dialogs. */
  onSubmit: (e: React.FormEvent) => void;
  busy: boolean;
  /** Id des Formulars: der Speichern-Knopf des Dialogs steht außerhalb und hängt sich per `form=` daran. */
  formId: string;
}) {
  // Namen der Kästchen-Gruppen (B-236): die Überschrift benennt die Gruppe per `aria-labelledby`.
  const groupId = useId();

  function up<K extends keyof MetaForm>(k: K, v: MetaForm[K]) {
    setForm((f) => ({ ...(f ?? form), [k]: v }));
  }

  return (
    <form id={formId} onSubmit={onSubmit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
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
      <CategoryField
        subjectId={detail.subjectId ?? null}
        loaded={{ id: detail.categoryId ?? null, name: detail.categoryName ?? null }}
        value={form.categoryId}
        onChange={(id) => up("categoryId", id)}
        disabled={busy}
      />
      <InterestDressing
        tagIds={form.interestTagIds}
        onChange={(ids) => up("interestTagIds", ids)}
        disabled={busy}
      />
      <GrammarTopicPicker
        subjectId={detail.subjectId ?? null}
        topicIds={form.grammarTopicIds}
        onChange={(ids) => up("grammarTopicIds", ids)}
        disabled={busy}
        id="ed-grammar-topics"
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
          <ArticleRequiredCheck checked={form.articleRequired} onChange={(v) => up("articleRequired", v)} />
          {(!form.sourceLang || !form.targetLang) && (
            <p className="sub" style={{ margin: 0 }}>
              Ohne Sprachpaar kann ein neues Wort nur <strong>aus dem Store</strong> übernommen werden –
              „anlegen &amp; hinzufügen" braucht die Sprachcodes. Setze sie hier und speichere die Übung.
            </p>
          )}
        </>
      )}
    </form>
  );
}

// ─── Inhalt der Nicht-Vokabel-Typen (Config als Ganzes) ───────────────────────

/** Der Zustand des Inhalts-Editors: die Zeilen plus das Zusatzfeld des Typs. */
type ContentState = { rows: Row[]; extra: Row };

/**
 * Diese Typen tragen ihren Inhalt in der Config, also wird sie als Ganzes ersetzt. Der Zeilen-Editor ist
 * derselbe wie beim Anlegen; vorbelegt wird über `configToEditorState` – die Gegenrichtung zu
 * `buildTypeConfig`. Der Zustand liegt im Dialog, gespeichert wird mit dessen einem Knopf.
 */
function ContentEditor({ type, content, setContent }: {
  type: ExerciseTypeKey;
  content: ContentState;
  setContent: (f: (prev: ContentState | null) => ContentState) => void;
}) {
  function setRows(f: (rs: Row[]) => Row[]) { setContent((c) => ({ ...(c ?? content), rows: f((c ?? content).rows) })); }
  function patchRow(i: number, patch: Row) { setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...patch } : r))); }
  function addRow() { setRows((rs) => [...rs, emptyRow(type)]); }
  function removeRow(i: number) { setRows((rs) => (rs.length > 1 ? rs.filter((_, idx) => idx !== i) : rs)); }
  // Ein Updater, kein Wert: `ConfigEditor` reicht `(e) => ({ ...e, ...patch })` herein (`EditorProps.setExtra`).
  function setExtra(f: (e: Row) => Row) { setContent((c) => ({ ...(c ?? content), extra: f((c ?? content).extra) })); }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>Inhalt</h4>
      <ConfigEditor type={type} rows={content.rows} extra={content.extra} setExtra={setExtra}
        patchRow={patchRow} addRow={addRow} removeRow={removeRow} />
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
        Kindes hängt am einzelnen Wort – Ergänzen und Entfernen lässt die übrigen unberührt. „Auch richtig"
        gehört zum Wort im Store und gilt darum in jeder Übung, die es benutzt.
      </p>

      <ListenAusfall was="Die Einträge der Übung" fehler={items.error} laedt={items.loading} onRetry={items.reload} schluessel={detail.id} />
      {/* frontend/CLAUDE.md, "Wiederkehrende Falle beim Platzhalter": both parts of
          `loading && data === null` are required. `loading` alone would hide the already visible table
          during a `reload`, since `useAsync` keeps `data` but resets `loading`; `data === null` alone
          would show the spinner forever next to an error, since `data` stays null after a failed load. */}
      {items.loading && items.data === null ? <div className="loading">Lade Wortpaare…</div> : items.data && (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>#</th><th>Wort</th><th>Übersetzung</th><th>Auch richtig</th><th>Hinweis</th><th>Erklärung (nach der Antwort)</th><th /></tr></thead>
            <tbody>
              {items.data.map((it, i) => (
                <ItemRow key={it.id} item={it} position={i + 1} busy={action.busy} exerciseId={detail.id}
                  storeHref={storeHref(it.front)}
                  onHint={(hint) => act(() => api.patchExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id, { hint }), "Hinweis gespeichert.")}
                  // B-556: leer löscht beim Server, wie beim Hinweis.
                  onExplanation={(explanation) => act(() => api.patchExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id,
                    { explanation }), "Erklärung gespeichert.")}
                  // Eine ausdrücklich leere Liste löscht (B-71); `null` hieße beim Server „unverändert“.
                  onAlternatives={async (translationAlternatives) => {
                    // Die Antwort zurückgeben: der Server bereinigt die Liste, und nur sie sagt, was jetzt gilt.
                    const res = await action.runFor(() => api.patchExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id,
                      { translationAlternatives }), "Gleichwertige Übersetzungen gespeichert.");
                    if (res) items.reload();
                    return res;
                  }}
                  onRemove={() => {
                    if (!confirmAction(removeItemQuestion(it.front, it.back))) return;
                    act(() => api.deleteExerciseItem(detail.seriesId, detail.seriesUnitId, detail.id, it.id), "Wort entfernt.");
                  }} />
              ))}
              {items.data.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Wörter – unten hinzufügen.</td></tr>}
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

function ItemRow({ item, position, busy, exerciseId, storeHref, onHint, onExplanation, onAlternatives, onRemove }: {
  item: VocabItemResponse; position: number; busy: boolean; exerciseId: number;
  /** Ziel im Vokabel-Store, vorgefiltert auf dieses Wort. */
  storeHref: string;
  onHint: (hint: string) => void;
  /** B-556: die Erklärung, die das Kind erst nach seiner Antwort sieht; leer löscht. */
  onExplanation: (explanation: string) => void;
  /** Die bereinigte Liste; leer heißt löschen. Liefert die Antwort des Servers bzw. `null` beim Fehlschlag. */
  onAlternatives: (alternatives: string[]) => Promise<VocabItemResponse | null>;
  onRemove: () => void;
}) {
  const [hint, setHint] = useState(item.hint ?? "");
  // Gleichwertige Übersetzungen des Store-Eintrags (B-71). Verglichen wird bereinigt gegen bereinigt: ein
  // angefangenes, noch leeres Feld ist keine Änderung und soll keinen OK-Knopf zeigen.
  const geladeneAlternativen = item.translationAlternatives ?? [];
  const [alternatives, setAlternatives] = useState<string[]>(geladeneAlternativen);
  const bereinigt = nonEmpty(alternatives) ?? [];
  const altDirty = JSON.stringify(bereinigt) !== JSON.stringify(geladeneAlternativen);

  // Ändert sich die Liste von außen (Neuladen nach einem anderen Speichern), zieht das Feld nach. Den Fall
  // „eigenes Speichern, Liste bleibt gleich“ deckt der OK-Knopf selbst ab (siehe dort).
  const geladenSchluessel = JSON.stringify(geladeneAlternativen);
  useEffect(() => { setAlternatives(JSON.parse(geladenSchluessel) as string[]); }, [geladenSchluessel]);
  // Das Bild-Panel steht eingeklappt: es lädt eigene Daten, und bei 30 Wörtern wären das 30 Abfragen.
  const [showMedia, setShowMedia] = useState(false);
  const dirty = hint !== (item.hint ?? "");
  const [explanation, setExplanation] = useState(item.explanation ?? "");
  const explanationDirty = explanation !== (item.explanation ?? "");
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
          <RepeatedTextFields label="Auch richtig" scope={item.front} values={alternatives}
            onChange={setAlternatives} placeholder="zählt auch als richtig" addLabel="+ Variante" />
          {altDirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginTop: 4 }}
            aria-label={`Auch richtig für ${item.front} speichern`}
            disabled={busy} onClick={async () => {
              /*
               * Aus der ANTWORT übernehmen, nicht auf das Neuladen warten: bereinigt der Server nur weg, was
               * ohnehin nicht zählt (die Hauptübersetzung, eine Dublette in anderer Schreibweise), bleibt seine
               * Liste gleich – der Effekt oben liefe dann nie, und „OK“ stünde neben einer Eingabe, die er so
               * nie gespeichert hat.
               */
              const res = await onAlternatives(bereinigt);
              if (res) setAlternatives(res.translationAlternatives ?? []);
            }}>OK</button>}
        </td>
        <td>
          <span className="row" style={{ gap: 4 }}>
            <input aria-label={`Hinweis für ${item.front}`} value={hint} onChange={(e) => setHint(e.target.value)}
              placeholder="–" style={{ maxWidth: 160 }} />
            {dirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              disabled={busy} onClick={() => onHint(hint)}>OK</button>}
          </span>
        </td>
        <td>
          {/* B-556: „warum" – das Kind sieht es erst nach der Antwort; der Hinweis daneben hilft davor. */}
          <span className="row" style={{ gap: 4 }}>
            <input aria-label={`Erklärung für ${item.front}`} value={explanation} onChange={(e) => setExplanation(e.target.value)}
              placeholder="–" style={{ maxWidth: 220 }} />
            {explanationDirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-label={`Erklärung für ${item.front} speichern`} disabled={busy} onClick={() => onExplanation(explanation)}>OK</button>}
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
          <td colSpan={7}>
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
  detail: ExerciseDetail; busy: boolean; onAdd: (body: VocabItemInput) => void;
}) {
  const [search, setSearch] = useState("");
  const [hits, setHits] = useState<VocabularyResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [front, setFront] = useState("");
  const [back, setBack] = useState("");
  const [alternatives, setAlternatives] = useState<string[]>([]);
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
        <div className="field" style={{ flex: 1 }}>
          <span className="label-row group-label">Auch richtig <span className="muted">(optional)</span></span>
          <RepeatedTextFields label="Auch richtig" scope="neues Wort" values={alternatives}
            onChange={setAlternatives} placeholder="zählt auch als richtig" addLabel="+ Variante" />
        </div>
        {/* Ohne Sprachpaar in der Config kann der Server das Wort nicht im Store anlegen (400) – dann
            bleibt der Weg über die Suche, und die Beschreibung oben nennt die Abhilfe. */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={busy || !canCreateInline || !front.trim() || !back.trim()}
          title={canCreateInline ? undefined : "Erst das Sprachpaar der Übung setzen – unten bei „Beschreibung & Einordnung“."}
          // Ohne Varianten geht `null` mit („nicht angegeben“): ein schon vorhandenes Wort behält dann seine Liste (B-71).
          onClick={() => {
            onAdd({ front: front.trim(), back: back.trim(), translationAlternatives: nonEmpty(alternatives) ?? null });
            setFront(""); setBack(""); setAlternatives([]);
          }}>
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
      <ListenAusfall was="Die Interessen-Stichwörter" fehler={known.error} laedt={known.loading} onRetry={known.reload}
        style={{ marginBottom: 6 }} />
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
 * Die Art der Übung (B-407): die Arten des Reihen-Fachs plus „– keine Art –". Mehr nimmt der Server nicht an
 * (`invalid_reference`), darum bietet das Feld nicht mehr an.
 *
 * Die geladene Art steht **immer** als Option da, auch wenn die Liste sie nicht kennt (Ladeausfall, eine Reihe ohne
 * Fach aus dem Altbestand): sonst zeigte das `<select>` einen anderen Wert, als das Formular trägt, und ein
 * unberührtes Feld sähe aus wie geleert. Scheitert die Liste, bleibt die Art also unverändert wählbar.
 */
function CategoryField({ subjectId, loaded, value, onChange, disabled }: {
  subjectId: number | null;
  /** Die Art im geladenen Stand – sie bleibt wählbar, auch wenn die Liste sie nicht führt. */
  loaded: { id: number | null; name: string | null };
  value: number | null;
  onChange: (id: number | null) => void;
  disabled: boolean;
}) {
  const list = useAsync<CategoryResponse[]>(
    () => (subjectId === null ? Promise.resolve([]) : api.categories(subjectId)), [subjectId]);
  const options = list.data ?? [];
  const loadedMissing = loaded.id !== null && !options.some((c) => c.id === loaded.id);

  return (
    <div className="field" style={{ maxWidth: 300 }}>
      <label htmlFor="ed-category">Art</label>
      <ListenAusfall was="Die Arten des Fachs" fehler={list.error} laedt={list.loading} onRetry={list.reload}
        schluessel={subjectId} style={{ marginBottom: 6 }} />
      <select id="ed-category" value={value ?? ""} disabled={disabled}
        onChange={(e) => onChange(e.target.value === "" ? null : Number(e.target.value))}>
        <option value="">– keine Art –</option>
        {loadedMissing && <option value={loaded.id!}>{loaded.name ?? `Art ${loaded.id}`}</option>}
        {options.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>
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
