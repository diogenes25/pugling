import { useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { LANGUAGES, languageByCode } from "../lib/languages";
import type {
  ChildResponse, ChildTagResponse, CreateVocabularyDto, PartOfSpeech, VocabTagResponse, VocabularyResponse,
} from "../lib/types";

const POS: PartOfSpeech[] = ["Noun", "Verb", "Adjective", "Adverb", "Other"];

interface PairRow { word: string; translation: string; }
const emptyPair = (): PairRow => ({ word: "", translation: "" });

/**
 * Vokabel-Verwaltung: Quell- und Zielsprache werden EINMAL oben gewählt (feste Liste, kein Freitext,
 * mit Flaggen) und gelten für alle darunter eingegebenen Wort-Paare sowie für den Store darunter –
 * dieser zeigt nur die gewählte Sprach-Kombination. Gespeichert wird zeilenweise als ein Batch.
 * Pro Vokabel lassen sich zwei Tag-Arten pflegen: globale (kindneutrale) Schlagworte und – für das oben
 * gewählte Kind – kind-skopierte Tags (z. B. „relevant für die nächste Klassenarbeit").
 */
export function VaterVocab() {
  // Eine Sprach-Auswahl steuert Eingabe UND Store-Filter (Punkte 1, 2, 4).
  const [src, setSrc] = useState("en");
  const [tgt, setTgt] = useState("de");

  const [rows, setRows] = useState<PairRow[]>([emptyPair()]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const [search, setSearch] = useState("");
  const list = useAsync<VocabularyResponse[]>(
    () => api.vocabulary(search.trim() || undefined, { sourceLanguage: src, targetLanguage: tgt }),
    [search, src, tgt],
  );

  // Kind-Auswahl für die kind-skopierten Tags (Muster wie VaterClassTests).
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | "">("");
  useEffect(() => {
    if (childId === "" && children.data && children.data.length > 0) setChildId(children.data[0].id);
  }, [children.data, childId]);

  // Globale Tags einmal laden: liefert Name→Id (zum Lösen) und Vorschläge; Kind-Tags analog fürs Kind.
  const globalTags = useAsync<VocabTagResponse[]>(() => api.vocabTags(), []);
  const childTagOpts = useAsync<ChildTagResponse[]>(
    () => (childId === "" ? Promise.resolve([]) : api.childTags(childId)),
    [childId],
  );

  const srcLang = languageByCode(src);
  const tgtLang = languageByCode(tgt);

  function patchRow(i: number, patch: Partial<PairRow>) {
    setRows((rs) => rs.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  }
  function addRow() { setRows((rs) => [...rs, emptyPair()]); }
  function removeRow(i: number) { setRows((rs) => (rs.length > 1 ? rs.filter((_, idx) => idx !== i) : rs)); }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    if (src === tgt) { setMsg({ ok: false, text: "Quell- und Zielsprache müssen sich unterscheiden." }); return; }

    // Nur vollständig ausgefüllte Zeilen senden – leere Rest-Zeilen ignorieren.
    const filled = rows.filter((r) => r.word.trim() && r.translation.trim());
    if (filled.length === 0) { setMsg({ ok: false, text: "Mindestens ein Wort-Paar (Wort + Übersetzung) angeben." }); return; }

    const items: CreateVocabularyDto[] = filled.map((r) => ({
      sourceLanguage: src, targetLanguage: tgt, word: r.word.trim(), translation: r.translation.trim(),
    }));

    setBusy(true);
    try {
      const results = await api.createVocabularyBatch(items);
      const created = results.filter((r) => r.status === "created").length;
      const existing = results.filter((r) => r.status === "existing").length;
      const errors = results.filter((r) => r.status === "error");
      const parts = [
        `${created} angelegt`,
        existing > 0 ? `${existing} existierten bereits` : null,
        errors.length > 0 ? `${errors.length} fehlgeschlagen` : null,
      ].filter(Boolean);
      setMsg({
        ok: errors.length === 0,
        text: errors.length === 0
          ? parts.join(" · ")
          : `${parts.join(" · ")}: ${errors.map((e) => e.error).filter(Boolean).join("; ")}`,
      });
      setRows([emptyPair()]);
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
        <h2 className="h-section" style={{ margin: 0 }}>Vokabeln hinzufügen</h2>
        <p className="muted" style={{ marginTop: 4 }}>
          Sprachen einmal oben wählen – alle Paare darunter werden in dieser Kombination gespeichert.
        </p>

        <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {/* Sprach-Konfiguration (gilt für alle Zeilen und den Store darunter) */}
          <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
            <LangSelect label="Quellsprache" value={src} onChange={setSrc} />
            <span style={{ fontSize: 22, alignSelf: "center", paddingBottom: 4 }} aria-hidden>→</span>
            <LangSelect label="Zielsprache" value={tgt} onChange={setTgt} />
          </div>

          {/* Zeilenweise Wort-Paare (Punkt 2) */}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {rows.map((r, i) => (
              <div key={i} className="row" style={{ gap: 8, alignItems: "flex-end" }}>
                <div className="field" style={{ flex: 1 }}>
                  {i === 0 && <label>{srcLang?.flag} Wort ({srcLang?.label ?? src})</label>}
                  <input aria-label={`Wort ${i + 1}`} value={r.word}
                    onChange={(e) => patchRow(i, { word: e.target.value })}
                    placeholder={src === "en" ? "house" : src === "fr" ? "maison" : "…"} />
                </div>
                <div className="field" style={{ flex: 1 }}>
                  {i === 0 && <label>{tgtLang?.flag} Übersetzung ({tgtLang?.label ?? tgt})</label>}
                  <input aria-label={`Übersetzung ${i + 1}`} value={r.translation}
                    onChange={(e) => patchRow(i, { translation: e.target.value })}
                    placeholder={tgt === "de" ? "Haus" : "…"} />
                </div>
                <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                  disabled={rows.length === 1} onClick={() => removeRow(i)} aria-label={`Zeile ${i + 1} entfernen`}>×</button>
              </div>
            ))}
          </div>

          <div className="row" style={{ gap: 8 }}>
            <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={addRow}>+ Zeile</button>
            <button type="submit" className="btn" style={{ width: "auto", marginLeft: "auto" }} disabled={busy}>
              {busy ? "…" : "Speichern"}
            </button>
          </div>
        </form>
        {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}
      </section>

      <section>
        <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <h2 className="h-section" style={{ margin: 0 }}>
            Vokabel-Store <span className="muted">{srcLang?.flag}→{tgtLang?.flag}</span> {list.data ? `(${list.data.length})` : ""}
          </h2>
          {/* Kind-Auswahl steuert, für welches Kind die kind-skopierten Tags gelten. */}
          <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
            <span className="muted">Kind-Tags für</span>
            <select aria-label="Kind für Tags" value={childId}
              onChange={(e) => setChildId(e.target.value === "" ? "" : Number(e.target.value))}
              disabled={!children.data || children.data.length === 0}>
              {(!children.data || children.data.length === 0) && <option value="">– kein Kind –</option>}
              {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
            </select>
          </label>
          <input style={{ marginLeft: "auto", maxWidth: 220 }} placeholder="Suchen…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Vokabel suchen" />
        </div>
        {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Key</th><th>Wort</th><th>Übersetzung</th><th>Wortart</th><th>Aktionen</th></tr></thead>
              <tbody>
                {list.data?.map((v) => (
                  <VocabRow key={v.id} v={v} onChanged={list.reload}
                    childId={childId === "" ? null : childId}
                    globalTags={globalTags.data ?? []} reloadGlobalTags={globalTags.reload}
                    childTagOptions={childTagOpts.data ?? []} reloadChildTags={childTagOpts.reload} />
                ))}
                {list.data?.length === 0 && <tr><td colSpan={5} className="muted">Keine Vokabeln in dieser Sprach-Kombination.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}

/** Sprach-Auswahl aus der festen Liste, mit Flagge im Eintrag (Punkte 1 & 3). */
function LangSelect({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  return (
    <div className="field" style={{ maxWidth: 200 }}>
      <label>{label}</label>
      <select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)}>
        {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
      </select>
    </div>
  );
}

interface VocabRowProps {
  v: VocabularyResponse;
  onChanged: () => void;
  childId: number | null;
  globalTags: VocabTagResponse[];
  reloadGlobalTags: () => void;
  childTagOptions: ChildTagResponse[];
  reloadChildTags: () => void;
}

/** Eine Store-Zeile mit Inline-Bearbeiten (PATCH), Löschen und aufklappbarem Tag-Editor. */
function VocabRow({ v, onChanged, childId, globalTags, reloadGlobalTags, childTagOptions, reloadChildTags }: VocabRowProps) {
  const [editing, setEditing] = useState(false);
  const [word, setWord] = useState(v.word);
  const [translation, setTranslation] = useState(v.translation);
  const [pos, setPos] = useState<PartOfSpeech>(v.partOfSpeech);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showTags, setShowTags] = useState(false);

  async function save() {
    setBusy(true); setErr(null);
    try {
      await api.updateVocabulary(v.id, { word, translation, partOfSpeech: pos });
      setEditing(false);
      onChanged();
    } catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }
  // Abbrechen: Änderungen verwerfen und wieder auf die gespeicherten Werte zurücksetzen (Punkt 5).
  function cancel() {
    setWord(v.word); setTranslation(v.translation); setPos(v.partOfSpeech);
    setErr(null); setEditing(false);
  }
  async function remove() {
    setBusy(true); setErr(null);
    try { await api.deleteVocabulary(v.id); onChanged(); }
    catch (e) { setErr(errorMessage(e)); setBusy(false); }
  }

  const tagCount = v.tags.length;

  return (
    <>
      <tr>
        <td className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{v.key}</td>
        {editing ? (
          <>
            <td><input aria-label="Wort" value={word} onChange={(e) => setWord(e.target.value)} /></td>
            <td><input aria-label="Übersetzung" value={translation} onChange={(e) => setTranslation(e.target.value)} /></td>
            <td>
              <select aria-label="Wortart" value={pos} onChange={(e) => setPos(e.target.value as PartOfSpeech)}>
                {POS.map((p) => <option key={p} value={p}>{p}</option>)}
              </select>
            </td>
            <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
              {err && <span className="muted" style={{ color: "var(--danger, #c00)", fontSize: 12 }}>{err}</span>}
              <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy} onClick={save}>OK</button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={cancel}>Abbrechen</button>
            </td>
          </>
        ) : (
          <>
            <td>{v.word}</td><td>{v.translation}</td><td>{v.partOfSpeech}</td>
            <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
              {err && <span className="muted" style={{ color: "var(--danger, #c00)", fontSize: 12 }}>{err}</span>}
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                aria-expanded={showTags} onClick={() => setShowTags((s) => !s)}>
                🏷️ Tags{tagCount > 0 ? ` (${tagCount})` : ""}
              </button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => setEditing(true)}>Bearbeiten</button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Löschen</button>
            </td>
          </>
        )}
      </tr>
      {showTags && !editing && (
        <tr>
          <td colSpan={5} style={{ background: "rgba(255,255,255,.02)" }}>
            <TagEditor v={v} onGlobalChanged={() => { onChanged(); reloadGlobalTags(); }}
              childId={childId} globalTags={globalTags} childTagOptions={childTagOptions}
              reloadChildTags={reloadChildTags} />
          </td>
        </tr>
      )}
    </>
  );
}

/** Zwei-teiliger Tag-Editor einer Vokabel: globale (kindneutrale) Tags + kind-skopierte Tags. */
function TagEditor({ v, onGlobalChanged, childId, globalTags, childTagOptions, reloadChildTags }: {
  v: VocabularyResponse;
  onGlobalChanged: () => void;
  childId: number | null;
  globalTags: VocabTagResponse[];
  childTagOptions: ChildTagResponse[];
  reloadChildTags: () => void;
}) {
  const [err, setErr] = useState<string | null>(null);

  // Kind-Tags dieser Vokabel werden lazy geladen (vermeidet N+1 über die ganze Store-Liste).
  const [childTags, setChildTags] = useState<ChildTagResponse[] | null>(null);
  const [ctLoading, setCtLoading] = useState(false);
  useEffect(() => {
    if (childId === null) { setChildTags(null); return; }
    let cancelled = false;
    setCtLoading(true);
    api.tagsForVocabulary(v.id, childId)
      .then((d) => { if (!cancelled) setChildTags(d); })
      .catch((e) => { if (!cancelled) setErr(errorMessage(e)); })
      .finally(() => { if (!cancelled) setCtLoading(false); });
    return () => { cancelled = true; };
  }, [v.id, childId]);

  async function addGlobal(name: string) {
    setErr(null);
    try { await api.attachVocabTags(v.id, [name]); onGlobalChanged(); }
    catch (e) { setErr(errorMessage(e)); }
  }
  async function removeGlobal(name: string) {
    setErr(null);
    const tag = globalTags.find((t) => t.name === name);
    if (!tag) { setErr(`Tag „${name}" nicht auffindbar – bitte Seite neu laden.`); return; }
    try { await api.detachVocabTag(v.id, tag.id); onGlobalChanged(); }
    catch (e) { setErr(errorMessage(e)); }
  }

  async function addChild(name: string) {
    if (childId === null) return;
    setErr(null);
    try {
      // Bestehenden Kind-Tag wiederverwenden, sonst neu anlegen (create-if-missing clientseitig).
      let tag = childTagOptions.find((t) => t.name === name) ?? childTags?.find((t) => t.name === name);
      if (!tag) tag = await api.createChildTag({ childId, name });
      await api.tagVocabulary(tag.id, [v.id]);
      setChildTags(await api.tagsForVocabulary(v.id, childId));
      reloadChildTags();
    } catch (e) { setErr(errorMessage(e)); }
  }
  async function removeChild(tag: ChildTagResponse) {
    if (childId === null) return;
    setErr(null);
    try {
      await api.untagVocabulary(tag.id, v.id);
      setChildTags(await api.tagsForVocabulary(v.id, childId));
      reloadChildTags();
    } catch (e) { setErr(errorMessage(e)); }
  }

  const childApplied = new Set((childTags ?? []).map((t) => t.name));
  const childSuggestions = childTagOptions.filter((t) => !childApplied.has(t.name)).map((t) => t.name);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10, padding: "8px 2px" }}>
      {err && <div className="banner err" style={{ margin: 0 }}>{err}</div>}

      <div className="row" style={{ gap: 10, alignItems: "center", flexWrap: "wrap" }}>
        <span className="muted" style={{ minWidth: 96, fontSize: 12 }}>Globale Tags</span>
        {v.tags.map((name) => (
          <TagChip key={name} label={name} onRemove={() => removeGlobal(name)} />
        ))}
        {v.tags.length === 0 && <span className="muted" style={{ fontSize: 12 }}>keine</span>}
        <TagAdder placeholder="+ globaler Tag" options={globalTags.map((t) => t.name)} onAdd={addGlobal} />
      </div>

      <div className="row" style={{ gap: 10, alignItems: "center", flexWrap: "wrap" }}>
        <span className="muted" style={{ minWidth: 96, fontSize: 12 }}>Kind-Tags</span>
        {childId === null ? (
          <span className="muted" style={{ fontSize: 12 }}>Oben ein Kind wählen, um kind-skopierte Tags zu pflegen.</span>
        ) : ctLoading ? (
          <span className="muted" style={{ fontSize: 12 }}>Lade…</span>
        ) : (
          <>
            {(childTags ?? []).map((t) => (
              <TagChip key={t.id} label={t.name} color={t.color} onRemove={() => removeChild(t)} />
            ))}
            {(childTags ?? []).length === 0 && <span className="muted" style={{ fontSize: 12 }}>keine</span>}
            <TagAdder placeholder="+ Kind-Tag" options={childSuggestions} onAdd={addChild} />
          </>
        )}
      </div>
    </div>
  );
}

/** Chip mit Entfernen-Knopf; optionale Farbe färbt Rand + Text. */
function TagChip({ label, color, onRemove }: { label: string; color?: string | null; onRemove: () => void }) {
  const style = color ? { borderColor: color, color } : undefined;
  return (
    <span className="chip" style={style}>
      {label}
      <button type="button" aria-label={`Tag ${label} entfernen`} onClick={onRemove}
        style={{ background: "none", border: "none", color: "inherit", cursor: "pointer", padding: 0, fontSize: 14, lineHeight: 1 }}>×</button>
    </span>
  );
}

/** Eingabe zum Hinzufügen eines Tags (mit Vorschlagsliste); Enter oder „+" fügt hinzu. */
function TagAdder({ placeholder, options, onAdd }: { placeholder: string; options: string[]; onAdd: (name: string) => Promise<void> }) {
  const [value, setValue] = useState("");
  const [busy, setBusy] = useState(false);
  const listId = `tags-${placeholder}-${options.length}`;

  async function submit() {
    const name = value.trim();
    if (!name || busy) return;
    setBusy(true);
    try { await onAdd(name); setValue(""); }
    finally { setBusy(false); }
  }

  return (
    <span className="row" style={{ gap: 4, alignItems: "center" }}>
      <input list={listId} value={value} placeholder={placeholder} aria-label={placeholder}
        style={{ maxWidth: 150, fontSize: 13 }} disabled={busy}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); submit(); } }} />
      <datalist id={listId}>{options.map((o) => <option key={o} value={o} />)}</datalist>
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy || !value.trim()} onClick={submit}>+</button>
    </span>
  );
}
