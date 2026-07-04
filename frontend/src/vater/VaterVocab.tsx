import { useState } from "react";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { CreateVocabularyDto, PartOfSpeech, VocabularyResponse } from "../lib/types";

const POS: PartOfSpeech[] = ["Noun", "Verb", "Adjective", "Adverb", "Other"];

/** Erzeugt einen stabilen Key aus Sprachen + Wörtern (Slug), falls der Vater keinen eigenen vergibt. */
function makeKey(src: string, tgt: string, word: string, translation: string): string {
  // NFD + Property-Escape entfernt Akzente; danach bleibt nur [a-z0-9], Rest wird zu "_".
  const slug = (s: string) =>
    s.toLowerCase().normalize("NFD").replace(/\p{Diacritic}/gu, "")
      .replace(/[^a-z0-9]+/g, "_").replace(/^_|_$/g, "");
  return `${src}_${slug(word)}_${tgt}_${slug(translation)}`;
}

export function VaterVocab() {
  const list = useAsync<VocabularyResponse[]>(() => api.vocabulary(), []);
  const [form, setForm] = useState<CreateVocabularyDto>({
    key: "", sourceLanguage: "en", targetLanguage: "de", word: "", translation: "", partOfSpeech: "Noun",
  });
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  function up<K extends keyof CreateVocabularyDto>(k: K, v: CreateVocabularyDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.word.trim() || !form.translation.trim()) { setMsg({ ok: false, text: "Wort und Übersetzung nötig." }); return; }
    setBusy(true);
    const dto: CreateVocabularyDto = {
      ...form,
      key: form.key.trim() || makeKey(form.sourceLanguage, form.targetLanguage, form.word, form.translation),
    };
    try {
      await api.createVocabulary(dto);
      setMsg({ ok: true, text: `„${dto.word}" gespeichert (Key: ${dto.key}).` });
      setForm((f) => ({ ...f, key: "", word: "", translation: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: err instanceof Error ? err.message : "Fehler" });
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <section>
        <h2 className="h-section">Vokabel hinzufügen</h2>
        <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
          <div className="field"><label>Wort (Quelle)</label><input value={form.word} onChange={(e) => up("word", e.target.value)} placeholder="house" /></div>
          <div className="field"><label>Übersetzung</label><input value={form.translation} onChange={(e) => up("translation", e.target.value)} placeholder="Haus" /></div>
          <div className="field"><label>Quellsprache</label><input value={form.sourceLanguage} onChange={(e) => up("sourceLanguage", e.target.value)} /></div>
          <div className="field"><label>Zielsprache</label><input value={form.targetLanguage} onChange={(e) => up("targetLanguage", e.target.value)} /></div>
          <div className="field"><label>Wortart</label>
            <select value={form.partOfSpeech} onChange={(e) => up("partOfSpeech", e.target.value as PartOfSpeech)}>
              {POS.map((p) => <option key={p} value={p}>{p}</option>)}
            </select>
          </div>
          <div className="field"><label>Key (optional)</label><input value={form.key} onChange={(e) => up("key", e.target.value)} placeholder="auto" /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Speichern"}</button>
        </form>
        {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}
      </section>

      <section>
        <h2 className="h-section">Vokabel-Store {list.data ? `(${list.data.length})` : ""}</h2>
        {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Key</th><th>Wort</th><th>Übersetzung</th><th>Wortart</th><th>Sprachen</th></tr></thead>
              <tbody>
                {list.data?.map((v) => (
                  <tr key={v.id}>
                    <td className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{v.key}</td>
                    <td>{v.word}</td><td>{v.translation}</td><td>{v.partOfSpeech}</td>
                    <td className="muted">{v.sourceLanguage}→{v.targetLanguage}</td>
                  </tr>
                ))}
                {list.data?.length === 0 && <tr><td colSpan={5} className="muted">Store ist leer.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
