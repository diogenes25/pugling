import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { StatusBanner } from "../components/StatusBanner";
import { api, errorMessage } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { LANGUAGES, languageByCode } from "../lib/languages";
import type {
  ChildResponse, ChildTagResponse, CreateVocabularyDto, Genus, NounInfo, Paged, PartOfSpeech, SortDir,
  UpdateVocabularyDto, VerbInfo, VocabSortKey, VocabTagResponse, VocabularyResponse,
} from "../lib/types";
import { GENUS, GENUS_LABEL, POS, POS_LABEL } from "../lib/vocab";
import { confirmAction } from "../lib/ui";
import { PAGE_SIZE, Pager, SortableTh } from "../components/ListControls";
import { VocabMediaPanel } from "./VocabMediaPanel";

interface PairRow { word: string; translation: string; }
const emptyPair = (): PairRow => ({ word: "", translation: "" });

/**
 * Stil der Aktionsspalte einer Store-Zeile – geteilt von Ansicht und Bearbeiten-Modus.
 *
 * `flexWrap` ist keine Kosmetik: Ohne es steht die Spalte in einer einzigen Zeile, die Tabelle wird breiter
 * als ihr Container, und die **letzten** Knöpfe („Löschen") wandern in den horizontalen Überlauf – sichtbar
 * nur für den, der auf die Idee kommt, seitwärts zu scrollen. Umbrechen dürfen sie; unerreichbar sein nicht.
 *
 * `minWidth` gehört dazu: Eine Flex-Zelle ohne Untergrenze lässt die Tabelle die Spalte auf die Breite eines
 * einzelnen Knopfes zusammendrücken, und dann steht jeder Knopf auf einer eigenen Zeile (vierfache
 * Zeilenhöhe). 230px halten die gewollte Aufteilung 2×2.
 */
const actionCell: React.CSSProperties = {
  gap: 6, justifyContent: "flex-end", flexWrap: "wrap", minWidth: 230,
};

/**
 * Vokabel-Verwaltung: Quell- und Zielsprache werden EINMAL oben gewählt (feste Liste, kein Freitext,
 * mit Flaggen) und gelten für alle darunter eingegebenen Wort-Paare sowie für den Store darunter –
 * dieser zeigt nur die gewählte Sprach-Kombination. Gespeichert wird zeilenweise als ein Batch.
 * Pro Vokabel lassen sich zwei Tag-Arten pflegen: globale (kindneutrale) Schlagworte und – für das oben
 * gewählte Kind – kind-skopierte Tags (z. B. „relevant für die nächste Klassenarbeit").
 */
export function VaterVocab() {
  /*
   * Einsprung von außen: Ein Link aus dem Übungs-Editor („dieses Wort im Store ansehen") trägt Wort und
   * Sprachpaar als Query mit, damit man nach dem Wechsel nicht erneut sucht und einstellt. Bewusst nur als
   * **Startwert** gelesen – danach gehört die Auswahl dem Nutzer, und ein Nachziehen der Query würde seine
   * Eingaben beim Zurück-Navigieren überschreiben.
   */
  const [params] = useSearchParams();

  // Eine Sprach-Auswahl steuert Eingabe UND Store-Filter (Punkte 1, 2, 4).
  const [src, setSrc] = useState(params.get("src") || "en");
  const [tgt, setTgt] = useState(params.get("tgt") || "de");

  const [rows, setRows] = useState<PairRow[]>([emptyPair()]);
  const action = useAction();

  const [search, setSearch] = useState(params.get("search") ?? "");
  // Feste Suchparameter neben dem Freitext: Wortart + globale Tags (Punkt „Vokabel-Store durchsuchbar").
  const [posFilter, setPosFilter] = useState<PartOfSpeech | "">("");
  const [tagFilter, setTagFilter] = useState<string[]>([]);
  const [tagMatchAll, setTagMatchAll] = useState(false);
  // Server-seitige Sortierung (Whitelist key/word/translation/pos) + Paginierung.
  const [sort, setSort] = useState<VocabSortKey>("key");
  const [dir, setDir] = useState<SortDir>("asc");
  const [skip, setSkip] = useState(0);
  const onSort = (key: VocabSortKey, nextDir: SortDir) => { setSort(key); setDir(nextDir); };
  const list = useAsync<Paged<VocabularyResponse>>(
    () => api.vocabulary({
      search: search.trim() || undefined,
      sourceLanguage: src, targetLanguage: tgt,
      partOfSpeech: posFilter || undefined,
      tags: tagFilter.length > 0 ? tagFilter : undefined,
      matchAll: tagMatchAll,
      sort, dir, skip, take: PAGE_SIZE,
    }),
    [search, src, tgt, posFilter, tagFilter, tagMatchAll, sort, dir, skip],
  );
  // Jede Filter-/Sortier-Änderung springt auf Seite 1 zurück (sonst landet man jenseits des Bestands).
  // Reset in der Render-Phase, damit die Liste nicht erst noch einmal mit altem skip nachlädt.
  const filterKey = `${search}|${src}|${tgt}|${posFilter}|${tagFilter.join(",")}|${tagMatchAll}|${sort}|${dir}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (prevFilterKey !== filterKey) { setPrevFilterKey(filterKey); setSkip(0); }

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
    if (src === tgt) { action.fail("Quell- und Zielsprache müssen sich unterscheiden."); return; }

    // Nur vollständig ausgefüllte Zeilen senden – leere Rest-Zeilen ignorieren.
    const filled = rows.filter((r) => r.word.trim() && r.translation.trim());
    if (filled.length === 0) { action.fail("Mindestens ein Wort-Paar (Wort + Übersetzung) angeben."); return; }

    const items: CreateVocabularyDto[] = filled.map((r) => ({
      sourceLanguage: src, targetLanguage: tgt, word: r.word.trim(), translation: r.translation.trim(),
    }));

    // `runFor`, weil der Stapel **teilweise** scheitern kann: die Meldung ist erst aus den Einzelergebnissen
    // zu bilden, und ein einzelnes „existiert schon" ist kein Fehlschlag des Ganzen.
    const results = await action.runFor(() => api.createVocabularyBatch(items));
    if (!results) return;
    const created = results.filter((r) => r.status === "created").length;
    const existing = results.filter((r) => r.status === "existing").length;
    const errors = results.filter((r) => r.status === "error");
    const parts = [
      `${created} angelegt`,
      existing > 0 ? `${existing} existierten bereits` : null,
      errors.length > 0 ? `${errors.length} fehlgeschlagen` : null,
    ].filter(Boolean);
    if (errors.length === 0) action.succeed(parts.join(" · "));
    else action.fail(`${parts.join(" · ")}: ${errors.map((e) => e.error).filter(Boolean).join("; ")}`);
    setRows([emptyPair()]);
    list.reload();
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
            <button type="submit" className="btn" style={{ width: "auto", marginLeft: "auto" }} disabled={action.busy}>
              {action.busy ? "Speichere…" : "Speichern"}
            </button>
          </div>
        </form>
        <StatusBanner message={action.message} style={{ marginTop: 10 }} />
      </section>

      <section>
        <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <h2 className="h-section" style={{ margin: 0 }}>
            Vokabel-Store <span className="muted">{srcLang?.flag}→{tgtLang?.flag}</span> {list.data ? `(${list.data.total})` : ""}
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

        {/* Feste Suchparameter: Wortart + globale Tags (zusätzlich zur Freitextsuche). */}
        <div className="row" style={{ gap: 10, alignItems: "center", flexWrap: "wrap", marginTop: 8 }}>
          <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
            <span className="muted">Wortart</span>
            <select aria-label="Wortart-Filter" value={posFilter}
              onChange={(e) => setPosFilter(e.target.value as PartOfSpeech | "")}>
              <option value="">– alle –</option>
              {POS.map((p) => <option key={p} value={p}>{POS_LABEL[p]}</option>)}
            </select>
          </label>
          <span className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
            <span className="muted">Tags</span>
            {tagFilter.map((name) => (
              <TagChip key={name} label={name} onRemove={() => setTagFilter((cur) => cur.filter((t) => t !== name))} />
            ))}
            <TagAdder placeholder="+ Tag-Filter" options={(globalTags.data ?? []).map((t) => t.name)}
              onAdd={async (name) => { setTagFilter((cur) => (cur.includes(name) ? cur : [...cur, name])); }} />
            {tagFilter.length > 1 && (
              <label className="row" style={{ gap: 4, alignItems: "center" }}>
                <input type="checkbox" checked={tagMatchAll} onChange={(e) => setTagMatchAll(e.target.checked)} /> alle
              </label>
            )}
          </span>
        </div>
        {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr>
                <SortableTh label="Key" sortKey="key" active={sort === "key"} dir={dir} onSort={onSort} />
                <SortableTh label="Wort" sortKey="word" active={sort === "word"} dir={dir} onSort={onSort} />
                <SortableTh label="Übersetzung" sortKey="translation" active={sort === "translation"} dir={dir} onSort={onSort} />
                <SortableTh label="Wortart" sortKey="pos" active={sort === "pos"} dir={dir} onSort={onSort} />
                {/* Nicht sortierbar: der Server sortiert über key/word/translation/pos, Tags sind keine Spalte. */}
                <th>Tags</th>
                <th>Aktionen</th>
              </tr></thead>
              <tbody>
                {list.data?.items.map((v) => (
                  <VocabRow key={v.id} v={v} onChanged={list.reload}
                    childId={childId === "" ? null : childId}
                    globalTags={globalTags.data ?? []} reloadGlobalTags={globalTags.reload}
                    childTagOptions={childTagOpts.data ?? []} reloadChildTags={childTagOpts.reload} />
                ))}
                {list.data?.items.length === 0 && <tr><td colSpan={6} className="muted">Keine Vokabeln in dieser Sprach-Kombination.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        {list.data && <Pager skip={skip} take={PAGE_SIZE} total={list.data.total} onSkip={setSkip} />}
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
  // Komplexer Datensatz: Substantiv-/Verb-Details, Grundform-Verknüpfung und Aussprache-Audio.
  const [noun, setNoun] = useState<NounInfo>(v.noun ?? {});
  const [verb, setVerb] = useState<VerbInfo>(v.verb ?? { isBaseForm: false });
  const [baseFormKey, setBaseFormKey] = useState(v.baseFormKey ?? "");
  const [baseFormRelation, setBaseFormRelation] = useState(v.baseFormRelation ?? "");
  const [audioUrl, setAudioUrl] = useState(v.pronunciationAudioUrl ?? "");
  const action = useAction();
  const [showTags, setShowTags] = useState(false);
  const [showMedia, setShowMedia] = useState(false);

  async function save() {
    const patch: UpdateVocabularyDto = {
      word, translation, partOfSpeech: pos,
      // "" hebt eine Grundform-Verknüpfung auf; ein Key setzt sie (Server prüft Existenz).
      baseFormKey: baseFormKey.trim(),
      baseFormRelation: baseFormRelation.trim() || null,
      pronunciationAudioUrl: audioUrl.trim() || null,
    };
    // Nur die zur Wortart passenden Detail-Blöcke mitschicken (Server merged partiell).
    if (pos === "Noun")
      patch.noun = { article: noun.article?.trim() || null, genus: noun.genus ?? null, plural: noun.plural?.trim() || null };
    if (pos === "Verb")
      patch.verb = {
        isBaseForm: verb.isBaseForm, infinitive: verb.infinitive?.trim() || null,
        tense: verb.tense?.trim() || null, person: verb.person?.trim() || null, number: verb.number?.trim() || null,
      };
    if (!await action.run(() => api.updateVocabulary(v.id, patch))) return;
    setEditing(false);
    onChanged();
  }
  // Abbrechen: Änderungen verwerfen und wieder auf die gespeicherten Werte zurücksetzen (Punkt 5).
  function cancel() {
    setWord(v.word); setTranslation(v.translation); setPos(v.partOfSpeech);
    setNoun(v.noun ?? {}); setVerb(v.verb ?? { isBaseForm: false });
    setBaseFormKey(v.baseFormKey ?? ""); setBaseFormRelation(v.baseFormRelation ?? "");
    setAudioUrl(v.pronunciationAudioUrl ?? "");
    action.clear(); setEditing(false);
  }
  async function remove() {
    if (!confirmAction("Diese Vokabel wirklich löschen?")) return;
    if (await action.run(() => api.deleteVocabulary(v.id))) onChanged();
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
                {POS.map((p) => <option key={p} value={p}>{POS_LABEL[p]}</option>)}
              </select>
            </td>
            {/* Tags bleiben auch im Bearbeiten-Modus sichtbar – geändert werden sie im Tag-Editor, nicht hier. */}
            <TagsCell tags={v.tags} />
            <td className="row" style={actionCell}>
              {action.message && !action.message.ok && (
                <span className="muted" role="status" aria-live="polite"
                  style={{ color: "var(--danger, #c00)", fontSize: 12 }}>{action.message.text}</span>
              )}
              <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={save}>OK</button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={cancel}>Abbrechen</button>
            </td>
          </>
        ) : (
          <>
            <td>{v.word}</td><td>{v.translation}</td>
            <td>{POS_LABEL[v.partOfSpeech]}{detailSummary(v) && <div className="muted" style={{ fontSize: 11 }}>{detailSummary(v)}</div>}</td>
            <TagsCell tags={v.tags} />
            <td className="row" style={actionCell}>
              {action.message && !action.message.ok && (
                <span className="muted" role="status" aria-live="polite"
                  style={{ color: "var(--danger, #c00)", fontSize: 12 }}>{action.message.text}</span>
              )}
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                aria-expanded={showTags} onClick={() => setShowTags((s) => !s)}>
                🏷️ Tags{tagCount > 0 ? ` (${tagCount})` : ""}
              </button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                aria-expanded={showMedia} onClick={() => setShowMedia((s) => !s)}>
                🖼️ Bilder
              </button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={() => setEditing(true)}>Bearbeiten</button>
              <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={remove}>Löschen</button>
            </td>
          </>
        )}
      </tr>
      {editing && (
        <tr>
          <td colSpan={6} style={{ background: "rgba(255,255,255,.02)" }}>
            <VocabDetailsEditor pos={pos} noun={noun} setNoun={setNoun} verb={verb} setVerb={setVerb}
              baseFormKey={baseFormKey} setBaseFormKey={setBaseFormKey}
              baseFormRelation={baseFormRelation} setBaseFormRelation={setBaseFormRelation}
              audioUrl={audioUrl} setAudioUrl={setAudioUrl}
              selfId={v.id} sourceLanguage={v.sourceLanguage} targetLanguage={v.targetLanguage} />
          </td>
        </tr>
      )}
      {showTags && !editing && (
        <tr>
          <td colSpan={6} style={{ background: "rgba(255,255,255,.02)" }}>
            <TagEditor v={v} onGlobalChanged={() => { onChanged(); reloadGlobalTags(); }}
              childId={childId} globalTags={globalTags} childTagOptions={childTagOptions}
              reloadChildTags={reloadChildTags} />
          </td>
        </tr>
      )}
      {showMedia && !editing && (
        <tr>
          <td colSpan={6} style={{ background: "rgba(255,255,255,.02)" }}>
            <VocabMediaPanel vocabularyId={v.id} word={v.word} />
          </td>
        </tr>
      )}
    </>
  );
}

/** Kompakte Zusammenfassung der komplexen Vokabel-Details für die Store-Zeile (Ansicht). */
function detailSummary(v: VocabularyResponse): string {
  const parts: string[] = [];
  if (v.noun) {
    const n = [v.noun.article, v.noun.plural ? `Pl. ${v.noun.plural}` : null].filter(Boolean).join(" ");
    if (n) parts.push(n);
    if (v.noun.genus) parts.push(GENUS_LABEL[v.noun.genus]);
  }
  if (v.verb) {
    if (v.verb.infinitive) parts.push(`Inf. ${v.verb.infinitive}`);
    else if (v.verb.tense) parts.push(v.verb.tense);
  }
  if (v.baseFormKey) parts.push(`↳ ${v.baseFormKey}${v.baseFormRelation ? ` (${v.baseFormRelation})` : ""}`);
  if (v.pronunciationAudioUrl) parts.push("🔊");
  return parts.join(" · ");
}

/**
 * Die Tag-Namen einer Vokabel in der Store-Zeile. Sie stehen hier und nicht erst hinter dem Aufklapper: beim
 * Durchsehen des Stores ist „welche Schlagworte hängen dran" die gesuchte Information, und `v.tags` ist mit
 * der Liste ohnehin geladen – die Anzahl allein am Knopf nützt niemandem. Nur lesend; geändert wird im
 * Tag-Editor, wo auch die kind-skopierten Tags stehen.
 */
function TagsCell({ tags }: { tags: string[] }) {
  if (tags.length === 0) return <td className="muted" style={{ fontSize: 12 }}>–</td>;
  return (
    <td>
      {/* `nowrap` je Chip: ohne es bricht ein zweiteiliger Tag-Name („Englisch 101-1000") mitten im Wort um
          und die Zeile wird doppelt so hoch. Umbrechen darf die *Liste*, nicht der einzelne Name. */}
      <span className="row" style={{ gap: 4, flexWrap: "wrap", maxWidth: 240 }}>
        {tags.map((name) => (
          <span key={name} className="chip" style={{ fontSize: 12, whiteSpace: "nowrap" }}>{name}</span>
        ))}
      </span>
    </td>
  );
}

/** Editor für den komplexen Vokabel-Datensatz: Substantiv-/Verb-Details, Grundform-Kante, Aussprache-Audio. */
function VocabDetailsEditor({ pos, noun, setNoun, verb, setVerb, baseFormKey, setBaseFormKey,
  baseFormRelation, setBaseFormRelation, audioUrl, setAudioUrl, selfId, sourceLanguage, targetLanguage }: {
  pos: PartOfSpeech;
  noun: NounInfo; setNoun: (updater: (n: NounInfo) => NounInfo) => void;
  verb: VerbInfo; setVerb: (updater: (v: VerbInfo) => VerbInfo) => void;
  baseFormKey: string; setBaseFormKey: (v: string) => void;
  baseFormRelation: string; setBaseFormRelation: (v: string) => void;
  audioUrl: string; setAudioUrl: (v: string) => void;
  /** Eigene Id – sie fällt aus den Grundform-Treffern heraus (der Server lehnt den Selbstverweis ab). */
  selfId: number;
  sourceLanguage: string;
  targetLanguage: string;
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 10, padding: "8px 2px" }}>
      {pos === "Noun" && (
        <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
          <span className="muted" style={{ minWidth: 96, fontSize: 12 }}>Substantiv</span>
          <div className="field" style={{ maxWidth: 120 }}><label>Artikel</label>
            <input aria-label="Artikel" value={noun.article ?? ""} placeholder="der/die/das"
              onChange={(e) => setNoun((n) => ({ ...n, article: e.target.value }))} /></div>
          <div className="field" style={{ maxWidth: 140 }}><label>Genus</label>
            <select aria-label="Genus" value={noun.genus ?? ""}
              onChange={(e) => setNoun((n) => ({ ...n, genus: (e.target.value || null) as Genus | null }))}>
              <option value="">–</option>
              {GENUS.map((g) => <option key={g} value={g}>{GENUS_LABEL[g]}</option>)}
            </select></div>
          <div className="field" style={{ maxWidth: 160 }}><label>Plural</label>
            <input aria-label="Plural" value={noun.plural ?? ""}
              onChange={(e) => setNoun((n) => ({ ...n, plural: e.target.value }))} /></div>
        </div>
      )}
      {pos === "Verb" && (
        <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
          <span className="muted" style={{ minWidth: 96, fontSize: 12 }}>Verb</span>
          <label className="checkline"><input type="checkbox" checked={verb.isBaseForm}
            onChange={(e) => setVerb((x) => ({ ...x, isBaseForm: e.target.checked }))} /> Grundform (Infinitiv)</label>
          <div className="field" style={{ maxWidth: 150 }}><label>Infinitiv</label>
            <input aria-label="Infinitiv" value={verb.infinitive ?? ""}
              onChange={(e) => setVerb((x) => ({ ...x, infinitive: e.target.value }))} /></div>
          <div className="field" style={{ maxWidth: 110 }}><label>Tempus</label>
            <input aria-label="Tempus" value={verb.tense ?? ""} placeholder="present/past"
              onChange={(e) => setVerb((x) => ({ ...x, tense: e.target.value }))} /></div>
          <div className="field" style={{ maxWidth: 90 }}><label>Person</label>
            <input aria-label="Person" value={verb.person ?? ""} placeholder="1/2/3"
              onChange={(e) => setVerb((x) => ({ ...x, person: e.target.value }))} /></div>
          <div className="field" style={{ maxWidth: 120 }}><label>Numerus</label>
            <input aria-label="Numerus" value={verb.number ?? ""} placeholder="singular/plural"
              onChange={(e) => setVerb((x) => ({ ...x, number: e.target.value }))} /></div>
        </div>
      )}
      <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
        <span className="muted" style={{ minWidth: 96, fontSize: 12 }}>Grundform</span>
        <div className="field" style={{ maxWidth: 300 }}><label>Grundform <span className="muted">(leer = keine)</span></label>
          <BaseFormPicker value={baseFormKey} onChange={setBaseFormKey}
            selfId={selfId} sourceLanguage={sourceLanguage} targetLanguage={targetLanguage} /></div>
        <div className="field" style={{ maxWidth: 160 }}><label>Relation</label>
          <input aria-label="Grundform-Relation" value={baseFormRelation} placeholder="Partizip/Plural…"
            onChange={(e) => setBaseFormRelation(e.target.value)} /></div>
        <div className="field" style={{ flex: 1, minWidth: 200 }}><label>Aussprache-Audio (URL)</label>
          <input aria-label="Aussprache-Audio-URL" value={audioUrl}
            onChange={(e) => setAudioUrl(e.target.value)} /></div>
      </div>
    </div>
  );
}

/**
 * Auswahl der Grundform („went" → „go").
 *
 * Der `BaseFormKey` ist ein **Fremdschlüssel auf eine andere Store-Vokabel**, kein Wert, den man sich
 * ausdenkt: der Server nimmt nur einen existierenden Key an und weist den Selbstverweis ab
 * (`VocabularyStoreController`: „BaseFormKey not found" bzw. „cannot be its own base form"). Als Freitextfeld
 * war das die eine Eingabe, die man zwangsläufig falsch macht – man hätte den generierten Key eines anderen
 * Eintrags abtippen müssen. Darum wird hier gesucht und ausgewählt; getippt wird das *Wort*, gespeichert der Key.
 *
 * Der eigene Key der Vokabel ist davon unberührt – den erzeugt der Server (`VocabKey.Generate`), er wird nie
 * eingegeben.
 */
function BaseFormPicker({ value, onChange, selfId, sourceLanguage, targetLanguage }: {
  value: string;
  onChange: (key: string) => void;
  selfId: number;
  sourceLanguage: string;
  targetLanguage: string;
}) {
  const [query, setQuery] = useState("");
  const [hits, setHits] = useState<VocabularyResponse[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function search() {
    const q = query.trim();
    if (!q || busy) return;
    setBusy(true); setErr(null);
    try {
      // Sprachpaar der bearbeiteten Vokabel: eine Grundform in einer anderen Sprache wäre nie richtig.
      const page = await api.vocabulary({ search: q, sourceLanguage, targetLanguage, take: 8 });
      setHits(page.items.filter((h) => h.id !== selfId));
    } catch (e) { setErr(errorMessage(e)); }
    finally { setBusy(false); }
  }

  // Gesetzte Grundform: den Key zeigen (er ist der gespeicherte Wert), lösen über „×".
  if (value) {
    return (
      <span className="row" style={{ gap: 6, alignItems: "center" }}>
        <code style={{ fontSize: 12, wordBreak: "break-all" }}>{value}</code>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          aria-label="Grundform-Verknüpfung lösen" onClick={() => { onChange(""); setHits(null); setQuery(""); }}>×</button>
      </span>
    );
  }

  return (
    <span style={{ display: "flex", flexDirection: "column", gap: 4 }}>
      <span className="row" style={{ gap: 4, alignItems: "center" }}>
        <input aria-label="Grundform suchen" value={query} placeholder="Wort der Grundform…"
          disabled={busy} onChange={(e) => setQuery(e.target.value)}
          // Kein umgebendes Formular, aber Enter darf hier nichts anderes auslösen als die Suche.
          onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); void search(); } }} />
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={busy || !query.trim()} onClick={() => void search()}>Suchen</button>
      </span>
      {err && <span className="muted" style={{ fontSize: 12, color: "var(--danger, #c00)" }}>{err}</span>}
      {hits !== null && hits.length === 0 && (
        <span className="muted" style={{ fontSize: 12 }}>Kein Treffer – die Grundform muss selbst im Store liegen.</span>
      )}
      {hits !== null && hits.length > 0 && (
        <span style={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {hits.map((h) => (
            <button key={h.id} type="button" className="btn ghost inline-btn"
              style={{ width: "auto", textAlign: "left", fontSize: 12 }}
              onClick={() => { onChange(h.key); setHits(null); setQuery(""); }}>
              {h.word} → {h.translation} <span className="muted">({POS_LABEL[h.partOfSpeech]})</span>
            </button>
          ))}
        </span>
      )}
    </span>
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
      <button type="button" className="btn ghost inline-btn" aria-label="Tag hinzufügen" style={{ width: "auto" }} disabled={busy || !value.trim()} onClick={submit}>+</button>
    </span>
  );
}
