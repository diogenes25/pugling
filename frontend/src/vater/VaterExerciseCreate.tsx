import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { FieldLabel } from "../components/InfoHint";
import { SCHOOL_TYPES } from "../lib/labels";
import { LANGUAGES } from "../lib/languages";
import type {
  CreateExercisePayload, ExerciseTypeKey, PartOfSpeech, SchoolType, SubjectResponse,
  TextbookSeriesResponse, SeriesUnitResponse, VocabTagResponse, VocabularyResponse,
} from "../lib/types";
import { POS, POS_LABEL } from "../lib/vocab";
// Die typ-spezifische Inhalts-Maschinerie ist mit dem Bearbeiten-Dialog geteilt: Schreiben (buildTypeConfig)
// und Zurücklesen (configToEditorState) müssen zueinander passen, sonst verliert Bearbeiten Inhalte.
import {
  AUTHORABLE_TYPES, ConfigEditor, VOCAB_FORMS, buildTypeConfig, contentProblem, emptyExtra, emptyRow,
  type Row,
} from "./exerciseConfig";
import { useExerciseTypes } from "../lib/exerciseTypes";

/**
 * Eine Übung anlegen – **nur** das.
 *
 * Anlegen und Verwalten lagen zusammen mit Katalog und Lückentext-Store auf einer Route und sogar in
 * einem `<form>` (Anmerkung 11: „das ist mir zu unaufgeräumt"). Erstellen ist ein abgeschlossener
 * Vorgang; die Bestandsliste ist eine Daueraufgabe. Siehe docs/vater-informationsarchitektur-plan.md.
 *
 * Fach, Reihe und Unit sind hier reine **Auswahl**. Das Anlegen von Fach/Reihe/Unit saß früher als
 * „Neues Fach" / „Neues Kapitel" gleichberechtigt neben den Pulldowns – prominent für etwas, das man
 * selten tut, und dazu am falschen Ort: der Katalog ist unter allen Vätern geteilt. Fach hat seine
 * eigene Seite (`/vater/katalog`), Reihe/Unit ihre eigene (`/vater/lehrwerke`); hier steht nur der Weg
 * dorthin. Seit B-106 hängt jede Übung zwingend an einer Lehrwerk-Unit statt an einem Kapitel –
 * Katalogisierung ist Pflicht.
 */
export function VaterExerciseCreate() {
  // Reihe/Unit kommen aus der Verwaltung mit (`+ Neue Übung` reicht sie als Query durch), damit der
  // Vater seine Auswahl nicht zweimal trifft.
  const [params] = useSearchParams();
  const [subjectId, setSubjectId] = useState<number | "">(Number(params.get("subjectId")) || "");
  const [seriesId, setSeriesId] = useState<number | "">(Number(params.get("seriesId")) || "");
  const [seriesUnitId, setSeriesUnitId] = useState<number | "">(Number(params.get("seriesUnitId")) || "");

  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const series = useAsync<TextbookSeriesResponse[]>(
    () => api.textbookSeries(subjectId ? { subjectId: Number(subjectId) } : {}), [subjectId]);
  const units = useAsync<SeriesUnitResponse[]>(
    () => (seriesId ? api.seriesUnits(Number(seriesId)) : Promise.resolve([])), [seriesId]);

  // Routen-Segment und Anzeigename der Typen kommen vom Server (Typ-Manifest), nicht aus einer Tabelle hier.
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;

  const [type, setType] = useState<ExerciseTypeKey>("Vocabulary");
  // Routen-Segment des gewählten Typs. `null`, solange das Manifest lädt oder der Server den Typ nicht
  // führt – dann bleibt das Anlegen gesperrt, statt gegen eine geratene Route zu posten.
  const route = types?.route(type) ?? null;
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [rewardPoints, setRewardPoints] = useState(10);
  const [gradeMin, setGradeMin] = useState<number | "">("");
  const [gradeMax, setGradeMax] = useState<number | "">("");
  const [source, setSource] = useState("");
  const [schoolTypes, setSchoolTypes] = useState<SchoolType[]>([]);
  // Lern-Standards, die eine Lehrplan-Position von dieser Übung erbt (Hybrid-Prinzip).
  const [defaultUseLeitner, setDefaultUseLeitner] = useState(false);
  const [defaultRequireTypedTest, setDefaultRequireTypedTest] = useState(false);
  // Standard-Abfrageform (nur Vokabeln): "" = Verfahrens-Standard, sonst TestStage-Wert (z. B. 6 = Multiple-Choice).
  const [defaultStage, setDefaultStage] = useState<number | "">("");
  const [defaultItemCount, setDefaultItemCount] = useState<number | "">("");

  // Typ-spezifisch: Zeilen + Extra-Felder (Richtung/Trägertext/Anweisung/Sprachen …).
  const [rows, setRows] = useState<Row[]>([emptyRow("Vocabulary")]);
  const [extra, setExtra] = useState<Row>(emptyExtra("Vocabulary"));
  // Vokabel-Übung: Store-Referenzen (per Id) statt inline-Wörter (Verknüpfung über Übungen hinweg).
  // Key wird nur für die Anzeige mitgeführt; ans Backend geht die vocabularyId.
  const [vocabRefs, setVocabRefs] = useState<{ key: string; vocabularyId: number }[]>([]);

  const [error, setError] = useState<string | null>(null);
  const [okMsg, setOkMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // Testmodus („Ausprobieren"): die frisch angelegte Übung gleich durchspielen.
  const [preview, setPreview] = useState<{ id: number; title: string } | null>(null);
  const [justCreated, setJustCreated] = useState<{ id: number; title: string } | null>(null);

  // Beim Typwechsel den Editor zurücksetzen (eine leere Zeile + passende Extra-Defaults).
  useEffect(() => {
    setRows([emptyRow(type)]);
    setVocabRefs([]);
    setDefaultStage("");
    setDefaultItemCount("");
    setExtra(emptyExtra(type));
  }, [type]);

  function patchRow(i: number, patch: Row) {
    setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  }
  function addRow() { setRows((rs) => [...rs, emptyRow(type)]); }
  function removeRow(i: number) { setRows((rs) => (rs.length > 1 ? rs.filter((_, idx) => idx !== i) : rs)); }

  function toggleSchool(s: SchoolType) {
    setSchoolTypes((cur) => (cur.includes(s) ? cur.filter((x) => x !== s) : [...cur, s]));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null); setOkMsg(null);
    if (!seriesId) { setError("Bitte eine Lehrwerk-Reihe wählen."); return; }
    if (!seriesUnitId) { setError("Bitte eine Unit wählen."); return; }
    if (!title.trim()) { setError("Bitte einen Titel angeben."); return; }
    // Die Meldung kommt aus der Typ-Prüfung: sie nennt, WAS fehlt (bei zwölf Typen sagt ein
    // Sammelsatz wie „Inhalt angeben" zu wenig).
    const problem = contentProblem(type, rows, extra, vocabRefs.length);
    if (problem) { setError(problem); return; }
    if (!route) { setError("Diesen Übungstyp kennt der Server nicht."); return; }

    setBusy(true);
    try {
      // orderIndex ans Ende der EIGENEN Übungen setzen – nicht der (evtl. mitgezählten) geteilten Bibliothek.
      const own = await api.searchExercises({
        seriesUnitId: Number(seriesUnitId),
        mineOnly: true, take: 1,
      });
      const payload: CreateExercisePayload = {
        title: title.trim(),
        description: description.trim() || null,
        orderIndex: own.total + 1,
        rewardPoints,
        // Store-Referenzen (per Id) statt inline-Wörter → dieselbe Vokabel bleibt über Übungen verknüpft.
        config: buildTypeConfig(type, rows, extra, { vocabRefs }),
        gradeMin: gradeMin === "" ? null : Number(gradeMin),
        gradeMax: gradeMax === "" ? null : Number(gradeMax),
        schoolTypes: schoolTypes.length > 0 ? schoolTypes.join(", ") : undefined,
        source: source.trim() || null,
        defaultUseLeitner,
        defaultRequireTypedTest,
        defaultStage: type === "Vocabulary" && defaultStage !== "" ? Number(defaultStage) : null,
        defaultItemCount: defaultItemCount === "" ? null : Number(defaultItemCount),
      };
      const created = await api.createExercise(Number(seriesId), Number(seriesUnitId), route, payload);
      setOkMsg(`Übung „${payload.title}" angelegt.`);
      setJustCreated({ id: created.id, title: payload.title });
      setTitle("");
      setDescription("");
      setRows([emptyRow(type)]);
      setVocabRefs([]);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  /** Zurück in die Verwaltung – mit derselben Auswahl, damit die Liste gleich dieselbe Unit zeigt. */
  const manageParams = new URLSearchParams();
  if (subjectId) manageParams.set("subjectId", String(subjectId));
  if (seriesId) manageParams.set("seriesId", String(seriesId));
  if (seriesUnitId) manageParams.set("seriesUnitId", String(seriesUnitId));
  const manageHref = `/vater/exercises${manageParams.size > 0 ? `?${manageParams}` : ""}`;

  return (
    // Der Testmodus-Dialog steht bewusst NEBEN dem Formular, nicht darin: er bringt ein eigenes `<form>`
    // mit, und verschachtelte Formulare sind ungültiges HTML – ein Knopf darin könnte das äußere Formular
    // abschicken und dabei eine zweite Übung anlegen.
    <>
    <div className="row" style={{ alignItems: "center", gap: 8 }}>
      <h2 className="h-section">Übung anlegen</h2>
      <Link to={manageHref} className="btn ghost inline-btn"
        style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>
        ← Übungen verwalten
      </Link>
    </div>

    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      {/* Fach (nur Filter) & Lehrwerk-Reihe/Unit – Auswahl, nicht Anlegen (das gehört in den Katalog). */}
      <section className="card">
        <h3 style={{ marginTop: 0 }}>Lehrwerk-Reihe &amp; Unit</h3>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="ex-subject">Fach <span className="muted">(filtert nur die Reihen)</span></label>
            <select id="ex-subject" aria-label="Fach" value={subjectId}
              onChange={(e) => { setSubjectId(e.target.value ? Number(e.target.value) : ""); setSeriesId(""); setSeriesUnitId(""); }}>
              <option value="">– alle –</option>
              {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div className="field">
            <label htmlFor="ex-series">Reihe</label>
            <select id="ex-series" aria-label="Reihe" value={seriesId}
              onChange={(e) => { setSeriesId(e.target.value ? Number(e.target.value) : ""); setSeriesUnitId(""); }}>
              <option value="">– wählen –</option>
              {series.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div className="field">
            <label htmlFor="ex-unit">Unit</label>
            <select id="ex-unit" aria-label="Unit" value={seriesUnitId} disabled={!seriesId}
              onChange={(e) => setSeriesUnitId(e.target.value ? Number(e.target.value) : "")}>
              <option value="">– wählen –</option>
              {units.data?.map((u) => <option key={u.id} value={u.id}>{u.label}</option>)}
            </select>
          </div>
        </div>
        <p className="muted" style={{ fontSize: 13, marginBottom: 0 }}>
          Fehlt eine Reihe oder Unit? Beides legst du unter <Link to="/vater/lehrwerke">📕 Lehrwerke</Link> an – sie
          gelten für <strong>alle</strong> Väter und gehören darum nicht ins Anlege-Formular.
        </p>
      </section>

      {/* Typ & Metadaten */}
      <section className="card">
        <div className="form-grid">
          <div className="field">
            <label htmlFor="ex-type">Übungstyp</label>
            <select id="ex-type" aria-label="Übungstyp" value={type} onChange={(e) => setType(e.target.value as ExerciseTypeKey)}>
              {AUTHORABLE_TYPES.map((t) => <option key={t} value={t}>{typeLabel(t)}</option>)}
            </select>
          </div>
          <div className="field"><label htmlFor="ex-title">Titel</label><input id="ex-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="z. B. Vokabeln Unit 1" /></div>
          <div className="field"><FieldLabel htmlFor="ex-points" topic="exercisePoints">Punkte</FieldLabel><input id="ex-points" type="number" min={0} value={rewardPoints} onChange={(e) => setRewardPoints(Number(e.target.value))} /></div>
          <div className="field"><label htmlFor="ex-grade-min">Klasse von</label><input id="ex-grade-min" type="number" min={1} max={13} value={gradeMin} onChange={(e) => setGradeMin(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><label htmlFor="ex-grade-max">Klasse bis</label><input id="ex-grade-max" type="number" min={1} max={13} value={gradeMax} onChange={(e) => setGradeMax(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><FieldLabel htmlFor="ex-source" topic="exerciseSource">Quelle (Lehrbuch)</FieldLabel><input id="ex-source" value={source} onChange={(e) => setSource(e.target.value)} placeholder="z. B. Green Line 1, Unit 1" /></div>
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label htmlFor="ex-description">Beschreibung <span className="muted">(optional)</span></label>
          <textarea id="ex-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2}
            placeholder="Worum geht es, worauf achten? Hilft beim Wiederfinden im Lehrplan-Bau." />
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <FieldLabel topic="exerciseSchoolTypes">Schularten</FieldLabel>
          <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
            {SCHOOL_TYPES.map((s) => (
              <label key={s} className="checkline"><input type="checkbox" checked={schoolTypes.includes(s)} onChange={() => toggleSchool(s)} /> {s}</label>
            ))}
          </div>
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label>Lern-Standards <span className="muted">(Lehrplan-Positionen erben diese, können sie aber übersteuern)</span></label>
          <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
            <label className="checkline"><input type="checkbox" checked={defaultUseLeitner} onChange={(e) => setDefaultUseLeitner(e.target.checked)} /> Leitner-Kasten</label>
            <label className="checkline"><input type="checkbox" checked={defaultRequireTypedTest} onChange={(e) => setDefaultRequireTypedTest(e.target.checked)} /> nur getippte Tests</label>
          </div>
        </div>
        <div className="field" style={{ marginTop: 10, maxWidth: 220 }}>
          <FieldLabel htmlFor="ex-default-item-count" topic="defaultItemCount">Standard-Menge</FieldLabel>
          <input id="ex-default-item-count" type="number" min={1} value={defaultItemCount}
            placeholder="alle" onChange={(e) => setDefaultItemCount(e.target.value === "" ? "" : Number(e.target.value))} />
        </div>
        {type === "Vocabulary" && (
          <div className="field" style={{ marginTop: 10, maxWidth: 300 }}>
            <FieldLabel topic="defaultStage">Standard-Abfrageform</FieldLabel>
            <select aria-label="Standard-Abfrageform" value={defaultStage}
              onChange={(e) => setDefaultStage(e.target.value === "" ? "" : Number(e.target.value))}>
              {VOCAB_FORMS.map((f) => <option key={f.label} value={f.value}>{f.label}</option>)}
            </select>
          </div>
        )}
      </section>

      {/* Typ-spezifischer Inhalts-Editor */}
      <section className="card">
        <h3 style={{ marginTop: 0 }}>Inhalt · {typeLabel(type)}</h3>
        {type === "Vocabulary"
          ? <VocabRefPicker selected={vocabRefs} setSelected={setVocabRefs} extra={extra} setExtra={setExtra} />
          : <ConfigEditor type={type} rows={rows} extra={extra} setExtra={setExtra}
              patchRow={patchRow} addRow={addRow} removeRow={removeRow} />}
      </section>

      {error && <div className="banner err" role="status" aria-live="polite">{error}</div>}
      {okMsg && (
        <div className="banner ok row" role="status" aria-live="polite" style={{ alignItems: "center", gap: 10 }}>
          <span>{okMsg}</span>
          {justCreated && (
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
              onClick={() => setPreview(justCreated)}>🧪 Ausprobieren</button>
          )}
          <Link to={manageHref} className="btn ghost inline-btn"
            style={{ width: "auto", textDecoration: "none", textAlign: "center" }}>Verwalten</Link>
        </div>
      )}

      <button type="submit" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} disabled={busy || !route}>
        {busy ? "…" : "Übung anlegen"}
      </button>
    </form>

    {preview && <ExercisePreviewModal exerciseId={preview.id} title={preview.title} onClose={() => setPreview(null)} />}
    </>
  );
}

/** Vokabel-Inhalt: wählt Store-Vokabeln (Komplextyp) per Id statt inline-Wörter; erlaubt „einfach anlegen". */
function VocabRefPicker({ selected, setSelected, extra, setExtra }: {
  selected: { key: string; vocabularyId: number }[];
  setSelected: (updater: (k: { key: string; vocabularyId: number }[]) => { key: string; vocabularyId: number }[]) => void;
  extra: Row;
  setExtra: (updater: (e: Row) => Row) => void;
}) {
  const [search, setSearch] = useState("");
  // Feste Suchparameter zum Finden der Store-Vokabeln (zusätzlich zum Freitext): Wortart + Tags.
  const [posFilter, setPosFilter] = useState<PartOfSpeech | "">("");
  const [tagFilter, setTagFilter] = useState<string[]>([]);
  // Sprach-Kombination des Stores: Vokabeln sind sprachgebunden – ohne Filter mischt der Store alle Sprachen
  // (z. B. französische Vokabeln in einer englischen Übung). Standard en→de, frei umstellbar.
  // Das Sprachpaar steht in `extra`, nicht lokal: es wandert mit in die Config, damit der Server später
  // inline ergänzte Wörter im Store anlegen kann (der Item-Endpunkt braucht die Sprachcodes).
  const src = extra.sourceLang || "en";
  const tgt = extra.targetLang || "de";
  const setSrc = (v: string) => setExtra((x) => ({ ...x, sourceLang: v }));
  const setTgt = (v: string) => setExtra((x) => ({ ...x, targetLang: v }));
  const store = useAsync<VocabularyResponse[]>(
    () => api.vocabulary({
      search: search.trim() || undefined,
      sourceLanguage: src, targetLanguage: tgt,
      partOfSpeech: posFilter || undefined,
      tags: tagFilter.length > 0 ? tagFilter : undefined,
    }).then((r) => r.items),
    [search, src, tgt, posFilter, tagFilter]);
  const tagOptions = useAsync<VocabTagResponse[]>(() => api.vocabTags(), []);
  const [qWord, setQWord] = useState("");
  const [qTrans, setQTrans] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const toggle = (v: VocabularyResponse) =>
    setSelected((cur) => (cur.some((s) => s.key === v.key)
      ? cur.filter((s) => s.key !== v.key)
      : [...cur, { key: v.key, vocabularyId: v.id }]));
  const removeKey = (key: string) => setSelected((cur) => cur.filter((s) => s.key !== key));

  async function quickAdd() {
    if (!qWord.trim() || !qTrans.trim()) return;
    setBusy(true); setErr(null);
    try {
      const v = await api.createVocabulary({ sourceLanguage: src, targetLanguage: tgt, word: qWord.trim(), translation: qTrans.trim() });
      setSelected((cur) => (cur.some((s) => s.key === v.key) ? cur : [...cur, { key: v.key, vocabularyId: v.id }]));
      setQWord(""); setQTrans(""); store.reload();
    } catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }

  const byKey = new Map((store.data ?? []).map((v) => [v.key, v]));
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Sprach-Kombination des Stores + Abfragerichtung. Der Sprachfilter verhindert, dass fremdsprachige
          Vokabeln (z. B. Französisch) in einer Übung anderer Sprache auftauchen. */}
      <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
        <div className="field" style={{ maxWidth: 180 }}>
          <label>Quellsprache</label>
          <select aria-label="Quellsprache" value={src} onChange={(e) => setSrc(e.target.value)}>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <span style={{ fontSize: 20, alignSelf: "center", paddingBottom: 4 }} aria-hidden>→</span>
        <div className="field" style={{ maxWidth: 180 }}>
          <label>Zielsprache</label>
          <select aria-label="Zielsprache" value={tgt} onChange={(e) => setTgt(e.target.value)}>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field" style={{ maxWidth: 200 }}>
          <label>Abfragerichtung</label>
          <select aria-label="Abfragerichtung" value={extra.direction ?? "front-to-back"} onChange={(e) => setExtra((x) => ({ ...x, direction: e.target.value }))}>
            <option value="front-to-back">vorne → hinten</option>
            <option value="back-to-front">hinten → vorne</option>
            <option value="both">beide</option>
          </select>
        </div>
      </div>

      {selected.length > 0 && (
        <div className="tokenlist">
          {selected.map((sel) => {
            const v = byKey.get(sel.key);
            return <span className="token" key={sel.key}>{v ? `${v.word}→${v.translation}` : sel.key}<button type="button" aria-label="Entfernen" onClick={() => removeKey(sel.key)}>×</button></span>;
          })}
        </div>
      )}

      {/* Feste Suchparameter: Wortart + Tags (zusätzlich zur Freitextsuche). */}
      <div className="row" style={{ gap: 10, alignItems: "center", flexWrap: "wrap" }}>
        <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
          <span className="muted">Wortart</span>
          <select aria-label="Wortart-Filter" value={posFilter} onChange={(e) => setPosFilter(e.target.value as PartOfSpeech | "")}>
            <option value="">– alle –</option>
            {POS.map((p) => <option key={p} value={p}>{POS_LABEL[p]}</option>)}
          </select>
        </label>
        <span className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
          <span className="muted">Tags</span>
          {tagFilter.map((name) => (
            <span className="chip" key={name}>{name}
              <button type="button" aria-label={`Tag ${name} entfernen`} onClick={() => setTagFilter((cur) => cur.filter((t) => t !== name))}
                style={{ background: "none", border: "none", color: "inherit", cursor: "pointer", padding: 0, fontSize: 14, lineHeight: 1 }}>×</button>
            </span>
          ))}
          <select aria-label="Tag-Filter hinzufügen" value=""
            onChange={(e) => { const n = e.target.value; if (n) setTagFilter((cur) => (cur.includes(n) ? cur : [...cur, n])); }}>
            <option value="">+ Tag…</option>
            {(tagOptions.data ?? []).filter((t) => !tagFilter.includes(t.name)).map((t) => <option key={t.id} value={t.name}>{t.name}</option>)}
          </select>
        </span>
      </div>
      <input placeholder="Store durchsuchen…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Vokabel-Store durchsuchen" />
      {store.loading && store.data === null ? <div className="loading">Lade…</div> : (
        <div style={{ maxHeight: 240, overflowY: "auto", display: "grid", gridTemplateColumns: "repeat(auto-fill,minmax(200px,1fr))", gap: 6 }}>
          {(store.data ?? []).map((v) => (
            <label key={v.id} className="checkline" style={{ padding: 6, border: "1px solid var(--stroke)", borderRadius: 8 }}>
              <input type="checkbox" checked={selected.some((s) => s.key === v.key)} onChange={() => toggle(v)} />
              <span>{v.word} <span className="muted">→ {v.translation}</span></span>
            </label>
          ))}
          {(store.data?.length ?? 0) === 0 && <span className="muted">Keine Treffer.</span>}
        </div>
      )}

      <div className="row" style={{ gap: 6, alignItems: "flex-end" }}>
        <div className="field" style={{ flex: 1 }}><label htmlFor="vp-word">Neu: Wort</label>
          <input id="vp-word" value={qWord} onChange={(e) => setQWord(e.target.value)} /></div>
        <div className="field" style={{ flex: 1 }}><label htmlFor="vp-translation">Übersetzung</label>
          <input id="vp-translation" value={qTrans} onChange={(e) => setQTrans(e.target.value)} /></div>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={quickAdd}>+ anlegen &amp; wählen</button>
      </div>
      {err && <div className="banner err">{err}</div>}
      <p className="muted" style={{ margin: 0 }}>Vokabeln kommen aus dem Store (Komplextyp) und bleiben über Übungen hinweg verknüpft.</p>
    </div>
  );
}
