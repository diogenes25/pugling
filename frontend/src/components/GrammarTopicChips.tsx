import { useState } from "react";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAction } from "../lib/useAction";
import { FieldLabel } from "./InfoHint";
import { ListenAusfall } from "./ListenAusfall";
import { StatusBanner } from "./StatusBanner";
import type { GrammarTopicResponse } from "../lib/types";

/**
 * Grammatik-Themen einer Übung oder Unit (B-155): Chips aus dem geteilten Vokabular des Fachs, dazu ein Feld, das
 * ein fehlendes Thema **nebenbei** anlegt – der Server ist idempotent über den Slug, ein zweites „Present perfect“
 * liefert das bestehende zurück. Es gibt bewusst keine eigene Verwaltungsseite: das Vokabular wächst dort, wo es
 * gebraucht wird, und bleibt durch den Slug trotzdem eines.
 *
 * `GrammarTopicPicker` lädt die Themen des Fachs selbst und gibt die Auswahl **nach oben** (sie gehört in die
 * Nutzlast); `GrammarTopicChips` ist der reine, props-getriebene Teil – darum getrennt, damit er ohne gefälschtes
 * `fetch` prüfbar ist (`frontend/CLAUDE.md`). Muster: `InterestChips` im Übungs-Editor.
 */
export function GrammarTopicPicker({ subjectId, topicIds, onChange, disabled, id = "grammar-topics" }: {
  /** Das Fach, dessen Themen zur Wahl stehen; ohne Fach gibt es nichts zu wählen. */
  subjectId: number | null;
  topicIds: number[];
  onChange: (ids: number[]) => void;
  disabled: boolean;
  /** Id des Auswahlfelds – zwei Picker auf einer Seite brauchen zwei. */
  id?: string;
}) {
  // Ohne Fach gibt es nichts zu laden – und keinen Hook, der eine Sitzung braucht: so bleibt ein Formular ohne
  // Fach (eine Reihe ohne Fach, ein Komponententest) ohne `AuthProvider` renderbar.
  if (subjectId === null) {
    return (
      <div className="field">
        <FieldLabel htmlFor={id} topic="grammarTopics">Grammatik-Themen</FieldLabel>
        <GrammarTopicChips id={id} topicIds={topicIds} known={[]} onChange={onChange} disabled={disabled} />
      </div>
    );
  }
  return <LoadedPicker subjectId={subjectId} topicIds={topicIds} onChange={onChange} disabled={disabled} id={id} />;
}

function LoadedPicker({ subjectId, topicIds, onChange, disabled, id }: {
  subjectId: number;
  topicIds: number[];
  onChange: (ids: number[]) => void;
  disabled: boolean;
  id: string;
}) {
  const known = useAsync<GrammarTopicResponse[]>(() => api.grammarTopics(subjectId), [subjectId]);
  const create = useAction();
  // Frisch angelegte Themen bleiben lokal bekannt, bis die Liste nachgeladen ist: sonst hieße der Chip für
  // einen Moment „Thema #123“ – auch im Namen seines Entfernen-Knopfs, den ein Screenreader ansagt.
  const [angelegt, setAngelegt] = useState<GrammarTopicResponse[]>([]);
  const bekannt = [...(known.data ?? []), ...angelegt.filter((a) => !known.data?.some((k) => k.id === a.id))];

  async function createTopic(label: string) {
    // Neutral formuliert wie beim Verlag: idempotent über den Slug, der Name kommt aus der ANTWORT.
    const created = await create.runFor(() => api.createGrammarTopic({ label, subjectId }));
    if (!created) return;
    create.succeed(`„${created.label}“ steht im Vokabular.`);
    setAngelegt((prev) => (prev.some((a) => a.id === created.id) ? prev : [...prev, created]));
    known.reload();
    if (!topicIds.includes(created.id)) onChange([...topicIds, created.id]);
  }

  return (
    <div className="field">
      <FieldLabel htmlFor={id} topic="grammarTopics">Grammatik-Themen</FieldLabel>
      {/* Ein Ausfall der Liste darf die Zuordnung nicht verstecken: die Chips unten stehen auch ohne sie (dann
          mit Id statt Name), und der Ausfall hat einen Weg zurück statt nur F5 (B-188). */}
      <ListenAusfall was="Die Grammatik-Themen" fehler={known.error} laedt={known.loading} onRetry={known.reload} />
      <GrammarTopicChips
        id={id}
        topicIds={topicIds}
        known={bekannt}
        loading={known.data === null && !known.error}
        onChange={onChange}
        onCreate={createTopic}
        disabled={disabled || known.loading || create.busy}
      />
      <StatusBanner message={create.message} style={{ marginTop: 6 }} />
    </div>
  );
}

export function GrammarTopicChips({ topicIds, known, onChange, onCreate, disabled, loading = false, id = "grammar-topics" }: {
  topicIds: number[];
  known: GrammarTopicResponse[];
  onChange: (ids: number[]) => void;
  /** Legt ein Thema mit diesem Namen an (idempotent) und wählt es; fehlt der Rückruf, gibt es kein Anlegen (kein Fach). */
  onCreate?: (label: string) => void;
  disabled: boolean;
  /** Die Liste ist noch unterwegs – dann ist „Name nicht dabei“ noch keine Aussage. */
  loading?: boolean;
  id?: string;
}) {
  const [pick, setPick] = useState("");
  const [neu, setNeu] = useState("");
  const available = known.filter((t) => !topicIds.includes(t.id));

  function submitNew() {
    const label = neu.trim();
    if (!label || !onCreate) return;
    onCreate(label);
    setNeu("");
  }

  return (
    <>
      <div className="row" style={{ gap: 6, flexWrap: "wrap", marginBottom: 8 }}>
        {topicIds.length === 0
          // Ausdrücklich, nicht leer: „kein Thema“ darf nicht aussehen wie „Feld gibt es nicht“.
          ? <span className="muted">Kein Grammatik-Thema – über den Filter ist das hier nicht zu finden.</span>
          : topicIds.map((tid) => {
            const topic = known.find((t) => t.id === tid);
            const name = topic ? topic.label : loading ? "…" : `Thema #${tid}`;
            return (
              <span key={tid} className="chip">
                {name}
                <button
                  type="button" className="btn ghost small" style={{ width: "auto", padding: "2px 8px" }}
                  aria-label={`Thema „${name}“ entfernen`}
                  disabled={disabled}
                  onClick={() => onChange(topicIds.filter((x) => x !== tid))}
                >×</button>
              </span>
            );
          })}
      </div>
      <div className="row" style={{ gap: 8, flexWrap: "wrap" }}>
        <select
          id={id} value={pick} disabled={disabled || available.length === 0}
          onChange={(e) => setPick(e.target.value)} style={{ maxWidth: 260 }}
        >
          <option value="">
            {loading ? "– Liste lädt –"
              : available.length === 0 ? "– nichts mehr zur Auswahl –"
              : "– Thema wählen –"}
          </option>
          {available.map((t) => <option key={t.id} value={t.id}>{t.label}</option>)}
        </select>
        <button
          type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={disabled || pick === ""}
          onClick={() => { onChange([...topicIds, Number(pick)]); setPick(""); }}
        >Thema hinzufügen</button>
      </div>
      {onCreate && (
        <div className="row" style={{ gap: 8, marginTop: 6, flexWrap: "wrap" }}>
          <input
            aria-label="Neues Grammatik-Thema" value={neu} placeholder="Neues Thema, z. B. Present perfect"
            disabled={disabled} style={{ maxWidth: 260 }}
            onChange={(e) => setNeu(e.target.value)}
            onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); submitNew(); } }}
          />
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            disabled={disabled || neu.trim() === ""} onClick={submitNew}>Thema anlegen</button>
        </div>
      )}
    </>
  );
}
