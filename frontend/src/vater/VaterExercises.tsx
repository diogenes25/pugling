import { useEffect, useMemo, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { ExerciseAttribution } from "./ExerciseAttribution";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import type {
  ChapterResponse, CreateExercisePayload, ExerciseSummary, ExerciseTypeKey, ExerciseUsage,
  SchoolType, SubjectResponse, VocabularyResponse,
} from "../lib/types";

// Übungstyp → Routen-Segment (Backend: .../chapters/{c}/<segment>).
const TYPE_ROUTE: Record<ExerciseTypeKey, string> = {
  Vocabulary: "vocabulary", Arithmetic: "arithmetic", Cloze: "cloze",
  Matching: "matching", List: "list", Birkenbihl: "birkenbihl",
};
const TYPE_LABEL: Record<ExerciseTypeKey, string> = {
  Vocabulary: "Vokabeln", Arithmetic: "Rechnen (feste Aufgaben)", Cloze: "Lückentext",
  Matching: "Zuordnung (Paare)", List: "Liste (auswendig)", Birkenbihl: "Birkenbihl",
};
const SCHOOL_TYPES: SchoolType[] = ["Grundschule", "Hauptschule", "Realschule", "Gymnasium", "Gesamtschule", "Berufsschule"];

// Kommaseparierten Text in eine getrimmte Liste (oder undefined) wandeln – für Alternativen/Wortpool.
function splitList(s: string): string[] | undefined {
  const list = s.split(",").map((x) => x.trim()).filter(Boolean);
  return list.length > 0 ? list : undefined;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type Row = Record<string, any>;

/** Leere Anfangszeile je Typ (die Felder, die der jeweilige Editor bearbeitet). */
function emptyRow(type: ExerciseTypeKey): Row {
  switch (type) {
    case "Vocabulary": return { front: "", back: "", hint: "" };
    case "Arithmetic": return { prompt: "", answer: "", tolerance: "0" };
    case "Cloze": return { index: 1, answer: "", alternatives: "" };
    case "Matching": return { left: "", right: "" };
    case "List": return { value: "", alternatives: "" };
    case "Birkenbihl": return { text: "", decoding: "", naturalTranslation: "" };
  }
}

export function VaterExercises() {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const [subjectId, setSubjectId] = useState<number | "">("");
  const [newSubject, setNewSubject] = useState("");
  const [chapterId, setChapterId] = useState<number | "">("");
  const [newChapter, setNewChapter] = useState("");

  const [type, setType] = useState<ExerciseTypeKey>("Vocabulary");
  const [title, setTitle] = useState("");
  const [rewardPoints, setRewardPoints] = useState(10);
  const [gradeMin, setGradeMin] = useState<number | "">("");
  const [gradeMax, setGradeMax] = useState<number | "">("");
  const [source, setSource] = useState("");
  const [schoolTypes, setSchoolTypes] = useState<SchoolType[]>([]);

  // Typ-spezifisch: Zeilen + Extra-Felder (Richtung/Trägertext/Anweisung/Sprachen …).
  const [rows, setRows] = useState<Row[]>([emptyRow("Vocabulary")]);
  const [extra, setExtra] = useState<Row>({ direction: "front-to-back" });
  // Vokabel-Übung: Store-Referenzen (Keys) statt inline-Wörter (Verknüpfung über Übungen hinweg).
  const [vocabKeys, setVocabKeys] = useState<string[]>([]);

  const [error, setError] = useState<string | null>(null);
  const [okMsg, setOkMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // Testmodus („Ausprobieren"): die aktuell durchzuspielende Übung (frisch angelegt oder aus der Liste).
  const [preview, setPreview] = useState<{ id: number; title: string } | null>(null);
  const [justCreated, setJustCreated] = useState<{ id: number; title: string } | null>(null);

  // Verwaltung zeigt standardmäßig nur eigene Übungen (mineOnly); optional auch die geteilte Bibliothek.
  const [showShared, setShowShared] = useState(false);

  const chapters = useAsync<ChapterResponse[]>(
    () => (subjectId ? api.chapters(Number(subjectId)) : Promise.resolve([])), [subjectId]);
  const existing = useAsync<ExerciseSummary[]>(
    () => (subjectId ? api.searchExercises({ subjectId: Number(subjectId), mineOnly: !showShared }) : Promise.resolve([])),
    [subjectId, okMsg, showShared]);

  const chapterExercises = useMemo(
    () => (existing.data ?? []).filter((e) => chapterId !== "" && e.chapterId === Number(chapterId)),
    [existing.data, chapterId]);

  // Beim Typwechsel den Editor zurücksetzen (eine leere Zeile + passende Extra-Defaults).
  useEffect(() => {
    setRows([emptyRow(type)]);
    setVocabKeys([]);
    setExtra(type === "Vocabulary" ? { direction: "front-to-back" }
      : type === "List" ? { ordered: false } : {});
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

  // Baut die typ-spezifische Config aus den Zeilen/Extra-Feldern (Form entspricht den Backend-*Config-Klassen).
  function buildConfig(): unknown {
    switch (type) {
      case "Vocabulary":
        // Store-Referenzen (Keys) statt inline-Wörter → dieselbe Vokabel bleibt über Übungen verknüpft.
        return { direction: extra.direction || "front-to-back", refs: vocabKeys };
      case "Arithmetic":
        return { problems: rows.map((r) => ({ prompt: r.prompt, answer: Number(r.answer), tolerance: Number(r.tolerance) || 0 })) };
      case "Cloze":
        return { text: extra.text ?? "", wordBank: splitList(extra.wordBank ?? ""),
          gaps: rows.map((r) => ({ index: Number(r.index), answer: r.answer, alternatives: splitList(r.alternatives ?? "") })) };
      case "Matching":
        return { instruction: extra.instruction?.trim() || null, pairs: rows.map((r) => ({ left: r.left, right: r.right })) };
      case "List":
        return { instruction: extra.instruction?.trim() || null, ordered: !!extra.ordered,
          items: rows.map((r) => ({ value: r.value, alternatives: splitList(r.alternatives ?? "") })) };
      case "Birkenbihl":
        return { learningLang: extra.learningLang ?? "", nativeLang: extra.nativeLang ?? "",
          sentences: rows.map((r) => ({ text: r.text, naturalTranslation: r.naturalTranslation,
            // Dekodierung als "Wort:wörtlich, Wort:wörtlich" eingegeben – hier in WordPair-Liste geparst.
            decoding: (r.decoding ?? "").split(",").map((p: string) => p.split(":"))
              .filter((kv: string[]) => kv[0]?.trim())
              .map((kv: string[]) => ({ word: kv[0].trim(), literal: (kv[1] ?? "").trim() })) })) };
    }
  }

  function firstEmptyRow(): boolean {
    // Grobe Pflichtprüfung je Typ: mindestens die Kernfelder der ersten Zeile gefüllt.
    const r = rows[0];
    switch (type) {
      case "Vocabulary": return vocabKeys.length === 0;
      case "Arithmetic": return !r.prompt || r.answer === "";
      case "Cloze": return !extra.text || !r.answer;
      case "Matching": return !r.left || !r.right;
      case "List": return !r.value;
      case "Birkenbihl": return !r.text || !r.naturalTranslation;
    }
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null); setOkMsg(null);
    if (!subjectId) { setError("Bitte ein Fach wählen oder anlegen."); return; }
    if (!chapterId) { setError("Bitte ein Kapitel wählen oder anlegen."); return; }
    if (!title.trim()) { setError("Bitte einen Titel angeben."); return; }
    if (firstEmptyRow()) { setError("Bitte mindestens einen vollständigen Inhalt angeben."); return; }

    const payload: CreateExercisePayload = {
      title: title.trim(),
      orderIndex: chapterExercises.length + 1,
      rewardPoints,
      config: buildConfig(),
      gradeMin: gradeMin === "" ? null : Number(gradeMin),
      gradeMax: gradeMax === "" ? null : Number(gradeMax),
      schoolTypes: schoolTypes.length > 0 ? schoolTypes.join(", ") : undefined,
      source: source.trim() || null,
    };
    setBusy(true);
    try {
      const created = await api.createExercise(Number(subjectId), Number(chapterId), TYPE_ROUTE[type], payload);
      setOkMsg(`Übung „${payload.title}" angelegt.`);
      setJustCreated({ id: created.id, title: payload.title });
      setTitle("");
      setRows([emptyRow(type)]);
      setVocabKeys([]);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
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
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={createSubject}>+</button>
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
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={!subjectId} onClick={createChapter}>+</button>
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
          <div className="field"><label>Titel</label><input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="z. B. Vokabeln Unit 1" /></div>
          <div className="field"><label>Punkte</label><input type="number" min={0} value={rewardPoints} onChange={(e) => setRewardPoints(Number(e.target.value))} /></div>
          <div className="field"><label>Klasse von</label><input type="number" min={1} max={13} value={gradeMin} onChange={(e) => setGradeMin(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><label>Klasse bis</label><input type="number" min={1} max={13} value={gradeMax} onChange={(e) => setGradeMax(e.target.value === "" ? "" : Number(e.target.value))} /></div>
          <div className="field"><label>Quelle (Lehrbuch)</label><input value={source} onChange={(e) => setSource(e.target.value)} placeholder="z. B. Green Line 1, Unit 1" /></div>
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label>Schularten</label>
          <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
            {SCHOOL_TYPES.map((s) => (
              <label key={s} className="checkline"><input type="checkbox" checked={schoolTypes.includes(s)} onChange={() => toggleSchool(s)} /> {s}</label>
            ))}
          </div>
        </div>
      </section>

      {/* Typ-spezifischer Inhalts-Editor */}
      <section className="card">
        <h3 style={{ marginTop: 0 }}>Inhalt · {TYPE_LABEL[type]}</h3>
        {type === "Vocabulary"
          ? <VocabRefPicker selectedKeys={vocabKeys} setSelectedKeys={setVocabKeys} extra={extra} setExtra={setExtra} />
          : <ConfigEditor type={type} rows={rows} extra={extra} setExtra={setExtra}
              patchRow={patchRow} addRow={addRow} removeRow={removeRow} />}
      </section>

      {error && <div className="banner err">{error}</div>}
      {okMsg && (
        <div className="banner ok row" style={{ alignItems: "center", gap: 10 }}>
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
            <h3 style={{ margin: 0 }}>Übungen in diesem Kapitel <span className="muted">({chapterExercises.length})</span></h3>
            {/* Verwaltung = eigene Übungen; bei Bedarf die geteilte Bibliothek anderer Väter einblenden. */}
            <label className="row" style={{ marginLeft: "auto", gap: 6, alignItems: "center", fontSize: 13 }}>
              <input type="checkbox" checked={showShared} onChange={(e) => setShowShared(e.target.checked)} />
              geteilte Übungen anderer Väter anzeigen
            </label>
          </div>
          {chapterExercises.length === 0 ? <div className="muted">Noch keine Übungen.</div> : (
            <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
              {chapterExercises.map((e) => (
                <ExerciseManageRow key={e.id} exercise={e} subjectId={Number(subjectId)} onChanged={existing.reload}
                  onPreview={() => setPreview({ id: e.id, title: e.title })} />
              ))}
            </div>
          )}
        </section>
      )}

      {preview && <ExercisePreviewModal exerciseId={preview.id} title={preview.title} onClose={() => setPreview(null)} />}
    </form>
  );
}

interface EditorProps {
  type: ExerciseTypeKey;
  rows: Row[];
  extra: Row;
  setExtra: (updater: (e: Row) => Row) => void;
  patchRow: (i: number, patch: Row) => void;
  addRow: () => void;
  removeRow: (i: number) => void;
}

function ConfigEditor({ type, rows, extra, setExtra, patchRow, addRow, removeRow }: EditorProps) {
  const ex = (patch: Row) => setExtra((e) => ({ ...e, ...patch }));
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      {/* Extra-Felder je Typ */}
      {type === "Vocabulary" && (
        <div className="field" style={{ maxWidth: 260 }}>
          <label>Abfragerichtung</label>
          <select aria-label="Abfragerichtung" value={extra.direction ?? "front-to-back"} onChange={(e) => ex({ direction: e.target.value })}>
            <option value="front-to-back">vorne → hinten</option>
            <option value="back-to-front">hinten → vorne</option>
            <option value="both">beide</option>
          </select>
        </div>
      )}
      {type === "Cloze" && (
        <>
          <div className="field"><label>Text (Lücken als {"{{1}}"}, {"{{2}}"} …)</label>
            <input value={extra.text ?? ""} onChange={(e) => ex({ text: e.target.value })} placeholder="Je {{1}} du pain à la {{2}}." /></div>
          <div className="field"><label>Wortpool (optional, kommagetrennt)</label>
            <input value={extra.wordBank ?? ""} onChange={(e) => ex({ wordBank: e.target.value })} placeholder="mange, achète, boulangerie" /></div>
        </>
      )}
      {type === "Matching" && (
        <div className="field"><label>Anweisung (optional)</label>
          <input value={extra.instruction ?? ""} onChange={(e) => ex({ instruction: e.target.value })} placeholder="Ordne zu." /></div>
      )}
      {type === "List" && (
        <>
          <div className="field"><label>Anweisung (optional)</label>
            <input value={extra.instruction ?? ""} onChange={(e) => ex({ instruction: e.target.value })} placeholder="Nenne alle …" /></div>
          <label className="checkline"><input type="checkbox" checked={!!extra.ordered} onChange={(e) => ex({ ordered: e.target.checked })} /> Reihenfolge zählt</label>
        </>
      )}
      {type === "Birkenbihl" && (
        <div className="form-grid">
          <div className="field"><label>Lernsprache</label><input value={extra.learningLang ?? ""} onChange={(e) => ex({ learningLang: e.target.value })} placeholder="Englisch" /></div>
          <div className="field"><label>Muttersprache</label><input value={extra.nativeLang ?? ""} onChange={(e) => ex({ nativeLang: e.target.value })} placeholder="Deutsch" /></div>
        </div>
      )}

      {/* Zeilen */}
      {rows.map((r, i) => (
        <div key={i} className="row" style={{ gap: 6, alignItems: "flex-end", flexWrap: "wrap" }}>
          {type === "Vocabulary" && <>
            <RowField label="Vorderseite" value={r.front} onChange={(v) => patchRow(i, { front: v })} />
            <RowField label="Rückseite" value={r.back} onChange={(v) => patchRow(i, { back: v })} />
            <RowField label="Hinweis" value={r.hint} onChange={(v) => patchRow(i, { hint: v })} optional />
          </>}
          {type === "Arithmetic" && <>
            <RowField label="Aufgabe" value={r.prompt} onChange={(v) => patchRow(i, { prompt: v })} placeholder="7 × 6" />
            <RowField label="Lösung" value={r.answer} onChange={(v) => patchRow(i, { answer: v })} type="number" width={90} />
            <RowField label="Toleranz" value={r.tolerance} onChange={(v) => patchRow(i, { tolerance: v })} type="number" width={90} optional />
          </>}
          {type === "Cloze" && <>
            <RowField label="Lücke-Nr." value={r.index} onChange={(v) => patchRow(i, { index: v })} type="number" width={80} />
            <RowField label="Lösung" value={r.answer} onChange={(v) => patchRow(i, { answer: v })} />
            <RowField label="Alternativen (kommagetrennt)" value={r.alternatives} onChange={(v) => patchRow(i, { alternatives: v })} optional />
          </>}
          {type === "Matching" && <>
            <RowField label="Links" value={r.left} onChange={(v) => patchRow(i, { left: v })} />
            <RowField label="Rechts" value={r.right} onChange={(v) => patchRow(i, { right: v })} />
          </>}
          {type === "List" && <>
            <RowField label="Eintrag" value={r.value} onChange={(v) => patchRow(i, { value: v })} />
            <RowField label="Alternativen (kommagetrennt)" value={r.alternatives} onChange={(v) => patchRow(i, { alternatives: v })} optional />
          </>}
          {type === "Birkenbihl" && <>
            <RowField label="Satz (Lernsprache)" value={r.text} onChange={(v) => patchRow(i, { text: v })} />
            <RowField label="Dekodierung (Wort:wörtlich, …)" value={r.decoding} onChange={(v) => patchRow(i, { decoding: v })} placeholder="What:Was, is:ist" />
            <RowField label="Natürliche Übersetzung" value={r.naturalTranslation} onChange={(v) => patchRow(i, { naturalTranslation: v })} />
          </>}
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => removeRow(i)} aria-label="Zeile entfernen">×</button>
        </div>
      ))}
      <button type="button" className="btn ghost" style={{ width: "auto", alignSelf: "flex-start" }} onClick={addRow}>+ Zeile</button>
    </div>
  );
}

function RowField({ label, value, onChange, type = "text", placeholder, optional, width }: {
  label: string; value: unknown; onChange: (v: string) => void;
  type?: string; placeholder?: string; optional?: boolean; width?: number;
}) {
  return (
    <div className="field" style={{ flex: width ? "none" : 1, minWidth: width ?? 120, width }}>
      <label>{label}{optional && <span className="muted"> (optional)</span>}</label>
      <input type={type} value={String(value ?? "")} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

/** Vokabel-Inhalt: wählt Store-Vokabeln (Komplextyp) per Key statt inline-Wörter; erlaubt „einfach anlegen". */
function VocabRefPicker({ selectedKeys, setSelectedKeys, extra, setExtra }: {
  selectedKeys: string[];
  setSelectedKeys: (updater: (k: string[]) => string[]) => void;
  extra: Row;
  setExtra: (updater: (e: Row) => Row) => void;
}) {
  const [search, setSearch] = useState("");
  const store = useAsync<VocabularyResponse[]>(() => api.vocabulary(search.trim() || undefined), [search]);
  const [qWord, setQWord] = useState("");
  const [qTrans, setQTrans] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const toggle = (key: string) =>
    setSelectedKeys((cur) => (cur.includes(key) ? cur.filter((k) => k !== key) : [...cur, key]));

  async function quickAdd() {
    if (!qWord.trim() || !qTrans.trim()) return;
    setBusy(true); setErr(null);
    try {
      const v = await api.createVocabulary({ sourceLanguage: "en", targetLanguage: "de", word: qWord.trim(), translation: qTrans.trim() });
      setSelectedKeys((cur) => (cur.includes(v.key) ? cur : [...cur, v.key]));
      setQWord(""); setQTrans(""); store.reload();
    } catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }

  const byKey = new Map((store.data ?? []).map((v) => [v.key, v]));
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div className="field" style={{ maxWidth: 260 }}>
        <label>Abfragerichtung</label>
        <select aria-label="Abfragerichtung" value={extra.direction ?? "front-to-back"} onChange={(e) => setExtra((x) => ({ ...x, direction: e.target.value }))}>
          <option value="front-to-back">vorne → hinten</option>
          <option value="back-to-front">hinten → vorne</option>
          <option value="both">beide</option>
        </select>
      </div>

      {selectedKeys.length > 0 && (
        <div className="tokenlist">
          {selectedKeys.map((k) => {
            const v = byKey.get(k);
            return <span className="token" key={k}>{v ? `${v.word}→${v.translation}` : k}<button type="button" onClick={() => toggle(k)}>×</button></span>;
          })}
        </div>
      )}

      <input placeholder="Store durchsuchen…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Vokabel-Store durchsuchen" />
      {store.loading ? <div className="loading">Lade…</div> : (
        <div style={{ maxHeight: 240, overflowY: "auto", display: "grid", gridTemplateColumns: "repeat(auto-fill,minmax(200px,1fr))", gap: 6 }}>
          {(store.data ?? []).map((v) => (
            <label key={v.id} className="checkline" style={{ padding: 6, border: "1px solid var(--stroke)", borderRadius: 8 }}>
              <input type="checkbox" checked={selectedKeys.includes(v.key)} onChange={() => toggle(v.key)} />
              <span>{v.word} <span className="muted">→ {v.translation}</span></span>
            </label>
          ))}
          {(store.data?.length ?? 0) === 0 && <span className="muted">Keine Treffer.</span>}
        </div>
      )}

      <div className="row" style={{ gap: 6, alignItems: "flex-end" }}>
        <div className="field" style={{ flex: 1 }}><label>Neu: Wort</label><input value={qWord} onChange={(e) => setQWord(e.target.value)} /></div>
        <div className="field" style={{ flex: 1 }}><label>Übersetzung</label><input value={qTrans} onChange={(e) => setQTrans(e.target.value)} /></div>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={quickAdd}>+ anlegen &amp; wählen</button>
      </div>
      {err && <div className="banner err">{err}</div>}
      <p className="muted" style={{ margin: 0 }}>Vokabeln kommen aus dem Store (Komplextyp) und bleiben über Übungen hinweg verknüpft.</p>
    </div>
  );
}

/** Eine Zeile der Kapitel-Übungsliste mit Verwendungs-Anzeige, Testmodus und Löschen (409-bewusst). */
function ExerciseManageRow({ exercise, subjectId, onChanged, onPreview }: {
  exercise: ExerciseSummary; subjectId: number; onChanged: () => void; onPreview: () => void;
}) {
  const [usage, setUsage] = useState<ExerciseUsage | null>(null);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function toggleUsage() {
    if (open) { setOpen(false); return; }
    setBusy(true); setErr(null);
    try { setUsage(await api.exerciseUsage(exercise.id)); setOpen(true); }
    catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }
  async function remove() {
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
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={toggleUsage}>Verwendung</button>
        {/* Nur der Autor darf löschen – fremde Übungen sind übernehmbar, aber geschützt. */}
        {exercise.isOwn && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Löschen</button>}
      </div>
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
