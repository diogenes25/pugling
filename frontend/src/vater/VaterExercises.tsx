import { useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { ExerciseAttribution } from "./ExerciseAttribution";
import { ExerciseEditModal } from "./ExerciseEditModal";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { PAGE_SIZE, Pager, SortControl } from "../components/ListControls";
import { SCHOOL_TYPES } from "../lib/labels";
import { LANGUAGES } from "../lib/languages";
import type {
  ChapterResponse, CreateExercisePayload, ExerciseSortKey, ExerciseSummary, ExerciseTypeKey, ExerciseUsage,
  Paged, PartOfSpeech, SchoolType, SortDir, SubjectResponse, VocabTagResponse, VocabularyResponse,
} from "../lib/types";
import { POS, POS_LABEL } from "../lib/vocab";
// Die typ-spezifische Inhalts-Maschinerie ist mit dem Bearbeiten-Dialog geteilt: Schreiben (buildTypeConfig)
// und Zurücklesen (configToEditorState) müssen zueinander passen, sonst verliert Bearbeiten Inhalte.
import {
  ConfigEditor, TYPE_LABEL, TYPE_ROUTE, VOCAB_FORMS, buildTypeConfig, emptyExtra, emptyRow,
  firstRowIncomplete, isKnownType, type Row,
} from "./exerciseConfig";

export function VaterExercises() {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const [subjectId, setSubjectId] = useState<number | "">("");
  const [newSubject, setNewSubject] = useState("");
  const [chapterId, setChapterId] = useState<number | "">("");
  const [newChapter, setNewChapter] = useState("");

  const [type, setType] = useState<ExerciseTypeKey>("Vocabulary");
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
  // Testmodus („Ausprobieren"): die aktuell durchzuspielende Übung (frisch angelegt oder aus der Liste).
  const [preview, setPreview] = useState<{ id: number; title: string } | null>(null);
  const [justCreated, setJustCreated] = useState<{ id: number; title: string } | null>(null);
  // Bearbeiten-Dialog: die aktuell offene Übung (Metadaten + Inhalt korrigieren statt neu anlegen).
  const [editing, setEditing] = useState<ExerciseSummary | null>(null);

  // Verwaltung zeigt standardmäßig nur eigene Übungen (mineOnly); optional auch die geteilte Bibliothek.
  const [showShared, setShowShared] = useState(false);

  const chapters = useAsync<ChapterResponse[]>(
    () => (subjectId ? api.chapters(Number(subjectId)) : Promise.resolve([])), [subjectId]);
  // Sortierung (Whitelist title/type/grade/source/created) + Paginierung; das Kapitel wird server-seitig
  // gefiltert (chapterId-Param), damit die Seitenzählung stimmt – kein In-Memory-Filter mehr.
  const [sort, setSort] = useState<ExerciseSortKey>("title");
  const [dir, setDir] = useState<SortDir>("asc");
  const [skip, setSkip] = useState(0);
  const existing = useAsync<Paged<ExerciseSummary>>(
    () => (subjectId
      ? api.searchExercises({
        subjectId: Number(subjectId),
        chapterId: chapterId !== "" ? Number(chapterId) : undefined,
        mineOnly: !showShared, sort, dir, skip, take: PAGE_SIZE,
      })
      : Promise.resolve({ items: [], total: 0 })),
    [subjectId, chapterId, okMsg, showShared, sort, dir, skip]);
  // Filter-/Sortier-Wechsel springt auf Seite 1 zurück (sonst leere Seite jenseits des Bestands). Der Reset
  // geschieht in der Render-Phase (nicht per Effekt), damit die Liste nicht erst mit altem skip nachlädt.
  const filterKey = `${subjectId}|${chapterId}|${showShared}|${sort}|${dir}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (prevFilterKey !== filterKey) { setPrevFilterKey(filterKey); setSkip(0); }

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

  async function createSubject() {
    if (!newSubject.trim()) return;
    try {
      const s = await api.createSubject(newSubject.trim());
      setNewSubject("");
      subjects.reload();
      setSubjectId(s.id);
    } catch (e) { setError(errorMessage(e)); }
  }
  async function createChapter() {
    if (!subjectId || !newChapter.trim()) return;
    try {
      const next = (chapters.data?.length ?? 0) + 1;
      const c = await api.createChapter(Number(subjectId), newChapter.trim(), next);
      setNewChapter("");
      chapters.reload();
      setChapterId(c.id);
    } catch (e) { setError(errorMessage(e)); }
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null); setOkMsg(null);
    if (!subjectId) { setError("Bitte ein Fach wählen oder anlegen."); return; }
    if (!chapterId) { setError("Bitte ein Kapitel wählen oder anlegen."); return; }
    if (!title.trim()) { setError("Bitte einen Titel angeben."); return; }
    if (firstRowIncomplete(type, rows, extra, vocabRefs.length)) { setError("Bitte mindestens einen vollständigen Inhalt angeben."); return; }

    setBusy(true);
    try {
      // orderIndex ans Ende der EIGENEN Übungen setzen – nicht der (evtl. mitgezählten) geteilten Bibliothek.
      const own = await api.searchExercises({
        subjectId: Number(subjectId), chapterId: Number(chapterId),
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
      const created = await api.createExercise(Number(subjectId), Number(chapterId), TYPE_ROUTE[type], payload);
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

  return (
    // Die Dialoge stehen bewusst NEBEN dem Formular, nicht darin: sie bringen eigene `<form>`s mit, und
    // verschachtelte Formulare sind ungültiges HTML – ein „Suchen" im Dialog könnte das äußere Formular
    // abschicken und dabei eine Übung anlegen.
    <>
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <h2 className="h-section">Übungen anlegen</h2>

      {/* Fach & Kapitel */}
      <section className="card">
        <h3 style={{ marginTop: 0 }}>Fach & Kapitel</h3>
        <div className="form-grid">
          <div className="field">
            <label>Fach</label>
            <select aria-label="Fach" value={subjectId} onChange={(e) => { setSubjectId(e.target.value ? Number(e.target.value) : ""); setChapterId(""); }}>
              <option value="">– wählen –</option>
              {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div className="field">
            <label>Neues Fach</label>
            <div className="row" style={{ gap: 6 }}>
              <input placeholder="z. B. Französisch" value={newSubject} onChange={(e) => setNewSubject(e.target.value)} />
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} aria-label="Fach anlegen" onClick={createSubject}>+</button>
            </div>
          </div>
          <div className="field">
            <label>Kapitel</label>
            <select aria-label="Kapitel" value={chapterId} disabled={!subjectId} onChange={(e) => setChapterId(e.target.value ? Number(e.target.value) : "")}>
              <option value="">– wählen –</option>
              {chapters.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div className="field">
            <label>Neues Kapitel</label>
            <div className="row" style={{ gap: 6 }}>
              <input placeholder="z. B. Unit 1" value={newChapter} disabled={!subjectId} onChange={(e) => setNewChapter(e.target.value)} />
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} aria-label="Kapitel anlegen" disabled={!subjectId} onClick={createChapter}>+</button>
            </div>
          </div>
        </div>
      </section>

      {/* Typ & Metadaten */}
      <section className="card">
        <div className="form-grid">
          <div className="field">
            <label>Übungstyp</label>
            <select aria-label="Übungstyp" value={type} onChange={(e) => setType(e.target.value as ExerciseTypeKey)}>
              {(Object.keys(TYPE_ROUTE) as ExerciseTypeKey[]).map((t) => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
            </select>
          </div>
          <div className="field"><label htmlFor="ex-title">Titel</label><input id="ex-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="z. B. Vokabeln Unit 1" /></div>
          <div className="field"><label htmlFor="ex-points">Punkte</label><input id="ex-points" type="number" min={0} value={rewardPoints} onChange={(e) => setRewardPoints(Number(e.target.value))} /></div>
          <div className="field"><label htmlFor="ex-grade-min">Klasse von</label><input id="ex-grade-min" type="number" min={1} max={13} value={gradeMin} onChange={(e) => setGradeMin(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><label htmlFor="ex-grade-max">Klasse bis</label><input id="ex-grade-max" type="number" min={1} max={13} value={gradeMax} onChange={(e) => setGradeMax(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><label htmlFor="ex-source">Quelle (Lehrbuch)</label><input id="ex-source" value={source} onChange={(e) => setSource(e.target.value)} placeholder="z. B. Green Line 1, Unit 1" /></div>
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label htmlFor="ex-description">Beschreibung <span className="muted">(optional)</span></label>
          <textarea id="ex-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2}
            placeholder="Worum geht es, worauf achten? Hilft beim Wiederfinden im Lehrplan-Bau." />
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label>Schularten</label>
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
          <label htmlFor="ex-default-item-count">Standard-Menge</label>
          <input id="ex-default-item-count" type="number" min={1} value={defaultItemCount}
            placeholder="alle" onChange={(e) => setDefaultItemCount(e.target.value === "" ? "" : Number(e.target.value))} />
        </div>
        {type === "Vocabulary" && (
          <div className="field" style={{ marginTop: 10, maxWidth: 300 }}>
            <label>Standard-Abfrageform</label>
            <select aria-label="Standard-Abfrageform" value={defaultStage}
              onChange={(e) => setDefaultStage(e.target.value === "" ? "" : Number(e.target.value))}>
              {VOCAB_FORMS.map((f) => <option key={f.label} value={f.value}>{f.label}</option>)}
            </select>
          </div>
        )}
      </section>

      {/* Typ-spezifischer Inhalts-Editor */}
      <section className="card">
        <h3 style={{ marginTop: 0 }}>Inhalt · {TYPE_LABEL[type]}</h3>
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
        </div>
      )}

      <button type="submit" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} disabled={busy}>
        {busy ? "…" : "Übung anlegen"}
      </button>

      {/* Vorhandene Übungen im gewählten Kapitel */}
      {chapterId !== "" && (
        <section className="card">
          <div className="row" style={{ alignItems: "center", gap: 8, marginBottom: 4 }}>
            <h3 style={{ margin: 0 }}>Übungen in diesem Kapitel <span className="muted">({existing.data?.total ?? 0})</span></h3>
            {/* Verwaltung = eigene Übungen; bei Bedarf die geteilte Bibliothek anderer Väter einblenden. */}
            <label className="row" style={{ marginLeft: "auto", gap: 6, alignItems: "center", fontSize: 13 }}>
              <input type="checkbox" checked={showShared} onChange={(e) => setShowShared(e.target.checked)} />
              geteilte Übungen anderer Väter anzeigen
            </label>
          </div>
          <div className="row" style={{ marginBottom: 8 }}>
            <SortControl<ExerciseSortKey>
              options={[
                { key: "title", label: "Titel" }, { key: "type", label: "Typ" }, { key: "grade", label: "Klasse" },
                { key: "source", label: "Quelle" }, { key: "created", label: "Erstellt" },
              ]}
              value={sort} dir={dir} onChange={(k, d) => { setSort(k); setDir(d); }} />
          </div>
          {existing.loading ? <div className="loading">Lade…</div>
            : (existing.data?.items.length ?? 0) === 0 ? <div className="muted">Noch keine Übungen.</div> : (
            <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
              {existing.data?.items.map((e) => (
                <ExerciseManageRow key={e.id} exercise={e} subjectId={Number(subjectId)} onChanged={existing.reload}
                  onPreview={() => setPreview({ id: e.id, title: e.title })} onEdit={() => setEditing(e)} />
              ))}
            </div>
          )}
          {existing.data && <Pager skip={skip} take={PAGE_SIZE} total={existing.data.total} onSkip={setSkip} />}
        </section>
      )}

    </form>

    {preview && <ExercisePreviewModal exerciseId={preview.id} title={preview.title} onClose={() => setPreview(null)} />}
    {editing && (
      <ExerciseEditModal
        exercise={editing}
        onClose={() => setEditing(null)}
        // Nur die Liste auffrischen, den Dialog offen lassen: Beschreibung und Inhalt sind getrennte
        // Speicher-Schritte, und ein zuklappender Dialog nähme die Bestätigung gleich wieder mit.
        onSaved={existing.reload}
      />
    )}
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
      {store.loading ? <div className="loading">Lade…</div> : (
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

/** Eine Zeile der Kapitel-Übungsliste mit Verwendungs-Anzeige, Testmodus und Löschen (409-bewusst). */
function ExerciseManageRow({ exercise, subjectId, onChanged, onPreview, onEdit }: {
  exercise: ExerciseSummary; subjectId: number; onChanged: () => void; onPreview: () => void; onEdit: () => void;
}) {
  const [usage, setUsage] = useState<ExerciseUsage | null>(null);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const known = isKnownType(exercise.type);

  async function toggleUsage() {
    if (open) { setOpen(false); return; }
    setBusy(true); setErr(null);
    try { setUsage(await api.exerciseUsage(exercise.id)); setOpen(true); }
    catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }
  async function remove() {
    if (!confirmAction("Diese Übung wirklich löschen? Zuordnungen in Lehrplänen können betroffen sein.")) return;
    setBusy(true); setErr(null);
    try { await api.deleteExercise(subjectId, exercise.chapterId, TYPE_ROUTE[exercise.type as ExerciseTypeKey] ?? "", exercise.id); onChanged(); }
    catch (e) { setErr(errorMessage(e)); setBusy(false); }
  }

  return (
    <div style={{ border: "1px solid var(--stroke)", borderRadius: 8, padding: "6px 10px" }}>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <span>{exercise.title}</span>
        <span className="muted">· {TYPE_LABEL[exercise.type as ExerciseTypeKey] ?? exercise.type}</span>
        {/* Attribution der geteilten Bibliothek: eigene vs. von anderen Vätern erstellt vs. System. */}
        <ExerciseAttribution e={exercise} />
        <span style={{ marginLeft: "auto" }} />
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onPreview}>🧪 Ausprobieren</button>
        {/*
          Bearbeiten und Löschen brauchen Schreibrecht (isOwn = Owner oder Write-Grant) UND einen Typ, den
          dieses UI kennt: das Routen-Segment kommt aus TYPE_ROUTE, und für die übrigen Backend-Typen
          (Reading, Grammar, Translation …) gäbe es keins – die Aufrufe liefen ins Leere.
        */}
        {exercise.isOwn && known && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onEdit}>✏️ Bearbeiten</button>
        )}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={toggleUsage}>Verwendung</button>
        {exercise.isOwn && known && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Löschen</button>
        )}
        {exercise.isOwn && !known && (
          <span className="muted" style={{ fontSize: 12 }}>Typ hier nicht bearbeitbar</span>
        )}
      </div>
      {exercise.description && <div className="muted" style={{ marginTop: 2, fontSize: 13 }}>{exercise.description}</div>}
      {err && <div className="banner err" style={{ marginTop: 6 }}>{err}</div>}
      {open && usage && (
        <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
          <div>Lehrpläne: {usage.plans.length === 0 ? "—" : usage.plans.map((p) => `${p.planTitle} (${p.childName})`).join(", ")}</div>
          <div>Klassenarbeiten: {usage.classTests.length === 0 ? "—" : usage.classTests.map((c) => `${c.title} (${c.childName})`).join(", ")}</div>
        </div>
      )}
    </div>
  );
}
