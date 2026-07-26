import { useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { Modal } from "../components/Modal";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import type {
  CreateExercisePayload, ExerciseDetail, ExerciseSummary, ExerciseTypeKey, SchoolType,
  VocabItemResponse, VocabularyResponse,
} from "../lib/types";
import { LANGUAGES } from "../lib/languages";
import {
  ConfigEditor, TYPE_LABEL, TYPE_ROUTE, VOCAB_FORMS, buildTypeConfig, configToEditorState, emptyRow,
  firstRowIncomplete, isContentEditable, type Row,
} from "./exerciseConfig";

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
 */
export function ExerciseEditModal({ exercise, onClose, onSaved }: {
  exercise: ExerciseSummary;
  onClose: () => void;
  /** Nach erfolgreichem Speichern der Metadaten (Liste neu laden + Dialog schließen). */
  onSaved: () => void;
}) {
  const type = exercise.type as ExerciseTypeKey;
  const [detail, setDetail] = useState<ExerciseDetail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    api.getExercise(exercise.id).then(setDetail).catch((e) => setLoadError(errorMessage(e)));
  }, [exercise.id]);

  return (
    <Modal label={`Übung bearbeiten: ${exercise.title}`} onClose={onClose} maxWidth={720}>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <h3 style={{ margin: 0 }}>✏️ Bearbeiten · {exercise.title}</h3>
        <span className="muted">{TYPE_LABEL[type] ?? exercise.type}</span>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          onClick={onClose} aria-label="Schließen">×</button>
      </div>

      {loadError && <div className="banner err">{loadError}</div>}
      {!detail && !loadError && <div className="loading">Lade…</div>}
      {detail && (
        <>
          {type === "Vocabulary" && <ItemEditor detail={detail} />}
          {type !== "Vocabulary" && isContentEditable(type) && (
            <ContentEditor detail={detail} type={type} onSaved={onSaved} />
          )}
          {type !== "Vocabulary" && !isContentEditable(type) && (
            <div className="banner">
              Der <b>Inhalt</b> dieses Typs wird hier nicht bearbeitet: seine Sätze und Wörter tragen Ids, an
              denen der Wort-Austausch und die Vokabel-Bindung hängen – ein Vollersatz würde sie neu vergeben.
              Beschreibung und Einordnung lassen sich unten ändern.
            </div>
          )}
          <MetaEditor detail={detail} type={type} onSaved={onSaved} />
        </>
      )}
    </Modal>
  );
}

// ─── Metadaten (alle Typen) ───────────────────────────────────────────────────

/**
 * Baut die PUT-Nutzlast aus dem geladenen Stand plus den geänderten Feldern. Zentral, weil beide Editoren
 * (Metadaten und Inhalt) denselben Vollersatz schicken müssen – nur mit unterschiedlicher `config`.
 */
function payloadFrom(detail: ExerciseDetail, form: MetaForm, config: unknown): CreateExercisePayload {
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
  };
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
}

function initialForm(d: ExerciseDetail): MetaForm {
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

function MetaEditor({ detail, type, onSaved }: { detail: ExerciseDetail; type: ExerciseTypeKey; onSaved: () => void }) {
  const [form, setForm] = useState<MetaForm>(() => initialForm(detail));
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  function up<K extends keyof MetaForm>(k: K, v: MetaForm[K]) { setForm((f) => ({ ...f, [k]: v })); }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    if (!form.title.trim()) { setMsg({ ok: false, text: "Bitte einen Titel angeben." }); return; }
    setBusy(true);
    try {
      // Die Config bleibt inhaltlich, wie sie ist – nur das Sprachpaar der Vokabelübung kommt aus dem
      // Formular (ohne es kann der Server kein inline ergänztes Wort im Store anlegen).
      const config = type === "Vocabulary"
        ? { ...(detail.config as Record<string, unknown>),
            sourceLang: form.sourceLang || null, targetLang: form.targetLang || null }
        : detail.config;
      await api.updateExercise(detail.subjectId, detail.chapterId, TYPE_ROUTE[type], detail.id,
        payloadFrom(detail, form, config));
      setMsg({ ok: true, text: "Gespeichert." });
      onSaved();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <h4 className="h-section" style={{ fontSize: 16, margin: 0 }}>Beschreibung & Einordnung</h4>
      <div className="form-grid">
        <div className="field"><label htmlFor="ed-title">Titel</label>
          <input id="ed-title" value={form.title} onChange={(e) => up("title", e.target.value)} /></div>
        <div className="field"><label htmlFor="ed-points">Punkte</label>
          <input id="ed-points" type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ed-grade-min">Klasse von</label>
          <input id="ed-grade-min" type="number" min={1} max={13} value={form.gradeMin}
            onChange={(e) => up("gradeMin", e.target.value === "" ? "" : Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ed-grade-max">Klasse bis</label>
          <input id="ed-grade-max" type="number" min={1} max={13} value={form.gradeMax}
            onChange={(e) => up("gradeMax", e.target.value === "" ? "" : Number(e.target.value))} /></div>
        <div className="field"><label htmlFor="ed-source">Quelle (Lehrbuch)</label>
          <input id="ed-source" value={form.source} onChange={(e) => up("source", e.target.value)} /></div>
        <div className="field"><label htmlFor="ed-item-count">Standard-Menge</label>
          <input id="ed-item-count" type="number" min={1} placeholder="alle" value={form.defaultItemCount}
            onChange={(e) => up("defaultItemCount", e.target.value === "" ? "" : Number(e.target.value))} /></div>
      </div>
      <div className="field">
        <label htmlFor="ed-description">Beschreibung <span className="muted">(optional)</span></label>
        <textarea id="ed-description" rows={2} value={form.description} onChange={(e) => up("description", e.target.value)} />
      </div>
      <div className="field">
        <label>Schularten</label>
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
      <div className="field">
        <label>Lern-Standards <span className="muted">(Lehrplan-Positionen erben diese, können sie aber übersteuern)</span></label>
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
            <label htmlFor="ed-stage">Standard-Abfrageform</label>
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
      <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>
        {busy ? "…" : "Beschreibung speichern"}
      </button>
      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} role="status" aria-live="polite">{msg.text}</div>}
    </form>
  );
}

// ─── Inhalt der Nicht-Vokabel-Typen (Config als Ganzes) ───────────────────────

/**
 * Diese Typen tragen ihren Inhalt in der Config, also wird sie als Ganzes ersetzt. Der Zeilen-Editor ist
 * derselbe wie beim Anlegen; vorbelegt wird über `configToEditorState` – die Gegenrichtung zu
 * `buildTypeConfig`.
 */
function ContentEditor({ detail, type, onSaved }: { detail: ExerciseDetail; type: ExerciseTypeKey; onSaved: () => void }) {
  const initial = configToEditorState(type, detail.config);
  const [rows, setRows] = useState<Row[]>(initial.rows);
  const [extra, setExtra] = useState<Row>(initial.extra);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  function patchRow(i: number, patch: Row) { setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...patch } : r))); }
  function addRow() { setRows((rs) => [...rs, emptyRow(type)]); }
  function removeRow(i: number) { setRows((rs) => (rs.length > 1 ? rs.filter((_, idx) => idx !== i) : rs)); }

  async function save() {
    setMsg(null);
    if (firstRowIncomplete(type, rows, extra, 0)) { setMsg({ ok: false, text: "Bitte mindestens einen vollständigen Inhalt angeben." }); return; }
    setBusy(true);
    try {
      await api.updateExercise(detail.subjectId, detail.chapterId, TYPE_ROUTE[type], detail.id,
        payloadFrom(detail, initialForm(detail), buildTypeConfig(type, rows, extra)));
      setMsg({ ok: true, text: "Inhalt gespeichert." });
      onSaved();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>Inhalt · {TYPE_LABEL[type]}</h4>
      <ConfigEditor type={type} rows={rows} extra={extra} setExtra={setExtra}
        patchRow={patchRow} addRow={addRow} removeRow={removeRow} />
      <button type="button" className="btn inline-btn" style={{ width: "auto", marginTop: 10 }} disabled={busy} onClick={save}>
        {busy ? "…" : "Inhalt speichern"}
      </button>
      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 8 }} role="status" aria-live="polite">{msg.text}</div>}
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
  const [items, setItems] = useState<VocabItemResponse[] | null>(null);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const reload = () => api.exerciseItems(detail.subjectId, detail.chapterId, detail.id).then(setItems);
  useEffect(() => { reload().catch((e) => setMsg({ ok: false, text: errorMessage(e) })); },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [detail.id]);

  async function act(fn: () => Promise<unknown>, okText: string) {
    setBusy(true); setMsg(null);
    try { await fn(); await reload(); setMsg({ ok: true, text: okText }); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>
        Wortpaare {items ? `(${items.length})` : ""}
      </h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Wörter kommen aus dem Vokabel-Store und bleiben über Übungen hinweg verknüpft. Der Lernstand deines
        Kindes hängt am einzelnen Wort – Ergänzen und Entfernen lässt die übrigen unberührt.
      </p>

      {items === null ? <div className="loading">Lade Wortpaare…</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>#</th><th>Wort</th><th>Übersetzung</th><th>Hinweis</th><th /></tr></thead>
            <tbody>
              {items.map((it, i) => (
                <ItemRow key={it.id} item={it} position={i + 1} busy={busy}
                  onHint={(hint) => act(() => api.patchExerciseItem(detail.subjectId, detail.chapterId, detail.id, it.id, { hint }), "Hinweis gespeichert.")}
                  onRemove={() => {
                    if (!confirmAction(`„${it.front} → ${it.back}" aus dieser Übung entfernen? Die Vokabel bleibt im Store.`)) return;
                    act(() => api.deleteExerciseItem(detail.subjectId, detail.chapterId, detail.id, it.id), "Wort entfernt.");
                  }} />
              ))}
              {items.length === 0 && <tr><td colSpan={5} className="muted">Noch keine Wörter – unten hinzufügen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <AddItem detail={detail} busy={busy}
        onAdd={(body) => act(() => api.addExerciseItem(detail.subjectId, detail.chapterId, detail.id, body), "Wort hinzugefügt.")} />

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 8 }} role="status" aria-live="polite">{msg.text}</div>}
    </section>
  );
}

function ItemRow({ item, position, busy, onHint, onRemove }: {
  item: VocabItemResponse; position: number; busy: boolean;
  onHint: (hint: string) => void; onRemove: () => void;
}) {
  const [hint, setHint] = useState(item.hint ?? "");
  const dirty = hint !== (item.hint ?? "");
  return (
    <tr>
      <td className="num">{position}</td>
      <td>{item.front}</td>
      <td className="muted">{item.back}</td>
      <td>
        <span className="row" style={{ gap: 4 }}>
          <input aria-label={`Hinweis für ${item.front}`} value={hint} onChange={(e) => setHint(e.target.value)}
            placeholder="–" style={{ maxWidth: 160 }} />
          {dirty && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            disabled={busy} onClick={() => onHint(hint)}>OK</button>}
        </span>
      </td>
      <td style={{ textAlign: "right" }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          aria-label={`${item.front} entfernen`} disabled={busy} onClick={onRemove}>Entfernen</button>
      </td>
    </tr>
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
