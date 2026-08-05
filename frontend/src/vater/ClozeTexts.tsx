import { useState } from "react";
import { InfoHint } from "../components/InfoHint";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import { RepeatedTextFields, nonEmpty } from "../components/RepeatedTextFields";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { placeholderIndices } from "../lib/cloze";
import { LANGUAGES } from "../lib/languages";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { ClozeResponse, Gap, Paged } from "../lib/types";

/*
 * Der Lückentext-Store: Trägertexte als **Lerngrundlage**, analog zum Vokabel-Store und unabhängig von
 * einer einzelnen Übung. Er war bisher nur über die API erreichbar – ein Vater konnte Lückentexte also
 * nicht sammeln, sondern musste den Text in jeder Übung neu tippen.
 *
 * Er lag danach eingeklappt auf der Übungen-Seite; seit er eine eigene Route hat (`/vater/lueckentexte`,
 * siehe docs/vater-informationsarchitektur-plan.md) ist der Einklapper entfallen – ein Store ist ein
 * Baustein neben dem Anlegen, kein Teil davon.
 *
 * Die Lücken sind über den Platzhalter an den Text gebunden: `{{1}}` im Text gehört zur Lücke mit
 * `index` 1. Deshalb liest diese Oberfläche die Platzhalter aus dem Text und führt die Lücken-Zeilen
 * daran nach – eine Lücke ohne Platzhalter würde beim Spielen nie erscheinen. Der Parser dafür liegt in
 * `lib/cloze`, weil die Sohn-Ansicht dieselbe Syntax lesen muss.
 */

/**
 * Die beiden Listen für den Sendeweg: getrimmt, ohne Leerfelder, und „keine" als `null` statt als leere
 * Liste – sonst gäbe es zwei Schreibweisen für denselben Zustand.
 *
 * Eigene Funktion, weil daran der `clearWordBank`-Schalter hängt: `null` heißt serverseitig „nicht
 * angegeben", ein geräumtes Feld löschte also nichts, und „Gespeichert." wäre eine Lüge. Hier ist die
 * Regel prüfbar, ohne einen Netzaufruf zu fälschen.
 */
export function listsForSave(gaps: Gap[], wordBank: string[]) {
  return {
    gaps: gaps.map((g) => ({ ...g, alternatives: nonEmpty(g.alternatives ?? []) ?? null })),
    wordBank: nonEmpty(wordBank) ?? null,
    clearWordBank: nonEmpty(wordBank) === undefined,
  };
}

/** Sagt, was am Verhältnis Text ↔ Lücken nicht stimmt – oder `null`, wenn es passt. */
function gapProblem(text: string, gaps: Gap[]): string | null {
  const inText = placeholderIndices(text);
  if (inText.length === 0) return "Der Text braucht mindestens einen Platzhalter, z. B. {{1}}.";
  const missing = inText.filter((i) => !gaps.some((g) => g.index === i && g.answer.trim()));
  if (missing.length > 0) return `Ohne Lösung: ${missing.map((i) => `{{${i}}}`).join(", ")}.`;
  const orphan = gaps.filter((g) => g.answer.trim() && !inText.includes(g.index));
  if (orphan.length > 0) return `Kein Platzhalter im Text: ${orphan.map((g) => `{{${g.index}}}`).join(", ")}.`;
  return null;
}

export function ClozeTexts() {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [skip, setSkip] = useState(0);
  const list = useAsync<Paged<ClozeResponse>>(
    () => api.clozeTexts({ search: applied || undefined, skip, take: PAGE_SIZE }), [applied, skip]);
  const action = useAction();
  const [editing, setEditing] = useState<ClozeResponse | null>(null);

  async function remove(c: ClozeResponse) {
    if (!confirmAction(`Trägertext „${c.title}" löschen? Bereits angelegte Übungen behalten ihren Inhalt.`)) return;
    if (await action.run(() => api.deleteClozeText(c.id), "Trägertext gelöscht.")) {
      if (editing?.id === c.id) setEditing(null);
      list.reload();
    }
  }

  return (
    <section className="card">
      <h3 style={{ marginTop: 0 }}>Alle Trägertexte {list.data ? `(${list.data.total})` : ""}</h3>
      <p className="muted" style={{ fontSize: 13 }}>
        Trägertexte sind <strong>Lerngrundlage</strong>, keine Übung: einmal gepflegt, in mehreren Übungen
        nutzbar. Lücken markierst du im Text mit <code>{"{{1}}"}</code>, <code>{"{{2}}"}</code> …
      </p>

      <form className="row" style={{ gap: 8, marginTop: 8 }}
        onSubmit={(e) => { e.preventDefault(); setSkip(0); setApplied(search.trim()); }}>
        <input aria-label="Lückentext suchen" value={search} onChange={(e) => setSearch(e.target.value)}
          placeholder="Titel, Text oder Key…" style={{ maxWidth: 260 }} />
        <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }}>Suchen</button>
      </form>

      {list.error && <div className="banner err">{list.error}</div>}
      {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
        <div style={{ overflowX: "auto", marginTop: 8 }}>
          <table className="table">
            <thead><tr><th>Titel</th><th>Sprachen</th><th>Text</th><th className="num">Lücken</th><th /></tr></thead>
            <tbody>
              {list.data?.items.map((c) => (
                <tr key={c.id}>
                  <td>{c.title}<div className="muted" style={{ fontSize: 11 }}><code>{c.key}</code></div></td>
                  <td className="muted">{c.sourceLanguage} → {c.targetLanguage}</td>
                  <td className="muted" style={{ maxWidth: 280, overflowWrap: "anywhere" }}>{c.text}</td>
                  <td className="num">{c.gaps.length}</td>
                  <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      onClick={() => setEditing(editing?.id === c.id ? null : c)}>
                      {editing?.id === c.id ? "Schließen" : "Bearbeiten"}
                    </button>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      disabled={action.busy} aria-label={`Löschen: ${c.title}`} onClick={() => remove(c)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.items.length === 0 && (
                <tr><td colSpan={5} className="muted">Noch keine Trägertexte – unten den ersten anlegen.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {list.data && <Pager skip={skip} take={PAGE_SIZE} total={list.data.total} onSkip={setSkip} busy={list.loading} />}

      {editing && (
        <ClozeForm key={editing.id} existing={editing}
          onDone={() => { setEditing(null); list.reload(); }} />
      )}
      {!editing && <ClozeForm onDone={() => { setSkip(0); list.reload(); }} />}

      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}

/**
 * Anlegen und Bearbeiten in **einem** Formular – die Felder sind dieselben, und zwei Kopien liefen
 * auseinander. Einziger Unterschied: der `key` ist beim Bearbeiten fest (stabile Referenz).
 */
export function ClozeForm({ existing, onDone }: { existing?: ClozeResponse; onDone: () => void }) {
  const [key, setKey] = useState(existing?.key ?? "");
  const [title, setTitle] = useState(existing?.title ?? "");
  const [sourceLanguage, setSourceLanguage] = useState(existing?.sourceLanguage ?? "en");
  const [targetLanguage, setTargetLanguage] = useState(existing?.targetLanguage ?? "de");
  const [text, setText] = useState(existing?.text ?? "");
  const [translation, setTranslation] = useState(existing?.translation ?? "");
  const [wordBank, setWordBank] = useState<string[]>([...(existing?.wordBank ?? [])]);
  const [gaps, setGaps] = useState<Gap[]>(existing?.gaps ?? []);
  const action = useAction();

  const indices = placeholderIndices(text);
  const problem = gapProblem(text, gaps);

  /** Für jeden Platzhalter im Text eine Zeile – bestehende Lösungen bleiben erhalten. */
  function syncGaps() {
    setGaps(indices.map((i) => gaps.find((g) => g.index === i) ?? { index: i, answer: "" }));
  }

  function setAnswer(index: number, answer: string) {
    setGaps((gs) => gs.map((g) => (g.index === index ? { ...g, answer } : g)));
  }
  // Die getippten Werte bleiben stehen, wie sie sind – auch leere und ungetrimmte. Aussortiert wird erst
  // beim Absenden (`nonEmpty`); wer währenddessen zusammenzieht, nimmt dem Tippenden das Zeichen weg.
  function setAlternatives(index: number, alternatives: string[]) {
    setGaps((gs) => gs.map((g) => (g.index === index ? { ...g, alternatives } : g)));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!title.trim()) { action.fail("Der Titel fehlt."); return; }
    if (!existing && !key.trim()) { action.fail("Der Key fehlt – er ist die stabile Referenz."); return; }
    if (problem) { action.fail(problem); return; }

    const { gaps: cleanedGaps, wordBank: bank, clearWordBank } = listsForSave(gaps, wordBank);
    const ok = await action.run(() => (existing
      // Der Key fehlt im PATCH bewusst: er bleibt, was er ist. Die beiden `clear`-Schalter sind Pflicht,
      // weil `null` serverseitig „nicht angegeben" heißt – ein geräumtes Feld allein löschte nichts,
      // und „Gespeichert." wäre eine Lüge.
      ? api.updateClozeText(existing.id, {
          title: title.trim(), text: text.trim(), translation: translation.trim() || null,
          gaps: cleanedGaps, wordBank: bank,
          clearTranslation: translation.trim() === "", clearWordBank,
        })
      : api.createClozeText({
          key: key.trim(), title: title.trim(), sourceLanguage, targetLanguage,
          text: text.trim(), gaps: cleanedGaps, translation: translation.trim() || null,
          wordBank: bank,
        })), existing ? "Gespeichert." : `Trägertext „${title.trim()}" angelegt.`);
    if (!ok) return;
    if (!existing) { setKey(""); setTitle(""); setText(""); setTranslation(""); setWordBank([]); setGaps([]); }
    onDone();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 14 }}>
      <h4 className="h-section" style={{ fontSize: 15, margin: 0 }}>
        {existing ? `„${existing.title}" bearbeiten` : "Neuer Trägertext"}
      </h4>

      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor="cz-key">Key {existing && <span className="muted">(unveränderlich)</span>}</label>
          <input id="cz-key" value={key} disabled={!!existing} onChange={(e) => setKey(e.target.value)}
            placeholder="cz_greetings_1" />
        </div>
        <div className="field">
          <label htmlFor="cz-title">Titel</label>
          <input id="cz-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Begrüßungen" />
        </div>
        {/* Die Sprachen liegen am Trägertext und bleiben beim Bearbeiten fest – ein Sprachwechsel
            wäre ein anderer Text, kein geänderter. */}
        <div className="field">
          <label htmlFor="cz-src">Quellsprache</label>
          <select id="cz-src" value={sourceLanguage} disabled={!!existing}
            onChange={(e) => setSourceLanguage(e.target.value)}>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="cz-tgt">Zielsprache</label>
          <select id="cz-tgt" value={targetLanguage} disabled={!!existing}
            onChange={(e) => setTargetLanguage(e.target.value)}>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
      </div>

      <div className="field">
        <label htmlFor="cz-text">Text mit Lücken</label>
        <textarea id="cz-text" rows={2} value={text} onChange={(e) => setText(e.target.value)}
          onBlur={syncGaps} placeholder="Good {{1}}, how {{2}} you?" />
        <span className="sub">
          Jede Lücke ist ein Platzhalter <code>{"{{n}}"}</code>. Beim Verlassen des Feldes entstehen die
          Lösungs-Zeilen dazu.
        </span>
      </div>

      <div className="field">
        <label htmlFor="cz-translation">Übersetzung <span className="muted">(optional, Hilfe auf Stufe 2/3)</span></label>
        <input id="cz-translation" value={translation} onChange={(e) => setTranslation(e.target.value)}
          placeholder="Guten Morgen, wie geht es dir?" />
      </div>

      {gaps.length > 0 && (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            {/* Spaltenkopf, kein `label`: Er gehört zu N Feldern, nicht zu einem – ein `<label>` ohne
                Ziel täte beim Anklicken nichts. */}
            <thead><tr><th>Lücke</th><th>Lösung</th>
              <th><span className="label-row">Auch richtig <span className="muted">(optional)</span>
                <InfoHint topic="alsoCorrect" /></span></th>
            </tr></thead>
            <tbody>
              {gaps.map((g) => (
                <tr key={g.index}>
                  <td><code>{`{{${g.index}}}`}</code></td>
                  <td>
                    <input aria-label={`Lösung für Lücke ${g.index}`} value={g.answer}
                      onChange={(e) => setAnswer(g.index, e.target.value)} style={{ maxWidth: 180 }} />
                  </td>
                  {/* `scope`: mehrere Lücken tragen dieselbe Komponente – sonst hießen alle Felder
                      „Auch richtig 1". */}
                  {/* Breiter als die Lösungs-Spalte: Hier stehen typischerweise die längeren Werte –
                      eine Umschreibung oder ein ganzer Satzteil, nicht ein einzelnes Wort. */}
                  <td style={{ minWidth: 320 }}>
                    <RepeatedTextFields label="Auch richtig" scope={`Lücke ${g.index}`}
                      placeholder="zählt auch als richtig"
                      values={g.alternatives ?? []} onChange={(v) => setAlternatives(g.index, v)} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="field">
        <span className="label-row">Wortpool <span className="muted">(optional, Auswahl auf Stufe 1/2)</span></span>
        {/* Ohne `scope`: Den Wortpool gibt es je Formular genau einmal, „Wort 1" kollidiert mit nichts. */}
        <RepeatedTextFields label="Wort" placeholder="morning" values={wordBank} onChange={setWordBank} />
      </div>

      {/* Der Hinweis steht vor dem Knopf, nicht als Fehler danach: so sieht der Vater beim Tippen,
          was noch fehlt, statt es beim Absenden zu erfahren. */}
      {problem && <p className="sub" style={{ color: "var(--danger, #c00)" }}>{problem}</p>}

      <div className="row" style={{ gap: 8 }}>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Speichere…" : existing ? "Speichern" : "Trägertext anlegen"}
        </button>
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}
