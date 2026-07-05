import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { PartOfSpeech, VocabularyResponse } from "../lib/types";

const POS: PartOfSpeech[] = ["Noun", "Verb", "Adjective", "Adverb", "Other"];
type Mode = "simple" | "complex";

export function VaterVocab() {
  const [search, setSearch] = useState("");
  const list = useAsync<VocabularyResponse[]>(() => api.vocabulary(search.trim() || undefined), [search]);

  const [mode, setMode] = useState<Mode>("simple");
  const [word, setWord] = useState("");
  const [translation, setTranslation] = useState("");
  const [src, setSrc] = useState("en");
  const [tgt, setTgt] = useState("de");
  const [pos, setPos] = useState<PartOfSpeech>("Noun");
  const [key, setKey] = useState("");
  const [audio, setAudio] = useState("");
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!word.trim() || !translation.trim()) { setMsg({ ok: false, text: "Wort und Übersetzung nötig." }); return; }
    setBusy(true);
    // Einfach: nur Wort/Übersetzung/Sprachen – Key + Wortart überlässt man dem Server.
    // Komplex: zusätzlich Wortart, optionaler eigener Key, Aussprache.
    const dto = mode === "simple"
      ? { sourceLanguage: src, targetLanguage: tgt, word: word.trim(), translation: translation.trim() }
      : {
          sourceLanguage: src, targetLanguage: tgt, word: word.trim(), translation: translation.trim(),
          partOfSpeech: pos, key: key.trim() || undefined, pronunciationAudioUrl: audio.trim() || null,
        };
    try {
      const v = await api.createVocabulary(dto);
      setMsg({ ok: true, text: `„${v.word}" gespeichert (Key: ${v.key}).` });
      setWord(""); setTranslation(""); setKey(""); setAudio("");
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <section>
        <div className="row" style={{ alignItems: "center", gap: 12 }}>
          <h2 className="h-section" style={{ margin: 0 }}>Vokabel hinzufügen</h2>
          <div className="row" style={{ marginLeft: "auto", gap: 6 }}>
            <button type="button" className={`btn ${mode === "simple" ? "" : "ghost"} inline-btn`} style={{ width: "auto" }} onClick={() => setMode("simple")}>Einfach</button>
            <button type="button" className={`btn ${mode === "complex" ? "" : "ghost"} inline-btn`} style={{ width: "auto" }} onClick={() => setMode("complex")}>Komplex</button>
          </div>
        </div>
        <p className="muted" style={{ marginTop: 4 }}>
          {mode === "simple"
            ? "Nur Wort ↔ Übersetzung. Key & Wortart ergänzt der Server (später komplex nachlieferbar)."
            : "Vollständiger Vokabel-Typ inkl. Wortart, eigenem Key und Aussprache."}
        </p>
        <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
          <div className="field"><label>Wort (Quelle)</label><input value={word} onChange={(e) => setWord(e.target.value)} placeholder="house" /></div>
          <div className="field"><label>Übersetzung</label><input value={translation} onChange={(e) => setTranslation(e.target.value)} placeholder="Haus" /></div>
          <div className="field"><label>Quellsprache</label><input value={src} onChange={(e) => setSrc(e.target.value)} /></div>
          <div className="field"><label>Zielsprache</label><input value={tgt} onChange={(e) => setTgt(e.target.value)} /></div>
          {mode === "complex" && <>
            <div className="field"><label>Wortart</label>
              <select aria-label="Wortart" value={pos} onChange={(e) => setPos(e.target.value as PartOfSpeech)}>
                {POS.map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </div>
            <div className="field"><label>Key (optional)</label><input value={key} onChange={(e) => setKey(e.target.value)} placeholder="auto" /></div>
            <div className="field"><label>Aussprache-URL (optional)</label><input value={audio} onChange={(e) => setAudio(e.target.value)} /></div>
          </>}
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Speichern"}</button>
        </form>
        {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}
      </section>

      <section>
        <div className="row" style={{ alignItems: "center" }}>
          <h2 className="h-section" style={{ margin: 0 }}>Vokabel-Store {list.data ? `(${list.data.length})` : ""}</h2>
          <input style={{ marginLeft: "auto", maxWidth: 220 }} placeholder="Suchen…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Vokabel suchen" />
        </div>
        {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Key</th><th>Wort</th><th>Übersetzung</th><th>Wortart</th><th>Sprachen</th><th>Aktionen</th></tr></thead>
              <tbody>
                {list.data?.map((v) => <VocabRow key={v.id} v={v} onChanged={list.reload} />)}
                {list.data?.length === 0 && <tr><td colSpan={6} className="muted">Keine Treffer.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

/** Eine Store-Zeile mit Inline-Bearbeiten (PATCH von Wort/Übersetzung/Wortart) und Löschen. */
function VocabRow({ v, onChanged }: { v: VocabularyResponse; onChanged: () => void }) {
  const [editing, setEditing] = useState(false);
  const [word, setWord] = useState(v.word);
  const [translation, setTranslation] = useState(v.translation);
  const [pos, setPos] = useState<PartOfSpeech>(v.partOfSpeech);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function save() {
    setBusy(true); setErr(null);
    try {
      await api.updateVocabulary(v.id, { word, translation, partOfSpeech: pos });
      setEditing(false);
      onChanged();
    } catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }
  async function remove() {
    setBusy(true); setErr(null);
    try { await api.deleteVocabulary(v.id); onChanged(); }
    catch (e) { setErr(errorMessage(e)); setBusy(false); }
  }

  if (!editing) {
    return (
      <tr>
        <td className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{v.key}</td>
        <td>{v.word}</td><td>{v.translation}</td><td>{v.partOfSpeech}</td>
        <td className="muted">{v.sourceLanguage}→{v.targetLanguage}</td>
        <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
          {err && <span className="muted" style={{ color: "var(--danger, #c00)", fontSize: 12 }}>{err}</span>}
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => setEditing(true)}>Bearbeiten</button>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Löschen</button>
        </td>
      </tr>
    );
  }
  return (
    <tr>
      <td className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{v.key}</td>
      <td><input aria-label="Wort" value={word} onChange={(e) => setWord(e.target.value)} /></td>
      <td><input aria-label="Übersetzung" value={translation} onChange={(e) => setTranslation(e.target.value)} /></td>
      <td>
        <select aria-label="Wortart" value={pos} onChange={(e) => setPos(e.target.value as PartOfSpeech)}>
          {POS.map((p) => <option key={p} value={p}>{p}</option>)}
        </select>
      </td>
      <td className="muted">{v.sourceLanguage}→{v.targetLanguage}</td>
      <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
        <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy} onClick={save}>OK</button>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => setEditing(false)}>×</button>
      </td>
    </tr>
  );
}
