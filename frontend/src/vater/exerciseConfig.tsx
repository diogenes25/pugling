import { useId } from "react";
import type { ExerciseTypeKey } from "../lib/types";

/*
 * Die typ-spezifische Inhalts-Maschinerie einer Übung – geteilt zwischen Anlegen (VaterExercises) und
 * Bearbeiten (ExerciseEditModal). Sie liegt hier, weil beide Richtungen zueinander passen müssen:
 * `buildTypeConfig` schreibt die Config, `configToEditorState` liest sie zurück. Divergierten die zwei,
 * würde Bearbeiten Inhalte still verlieren.
 *
 * Vokabelübungen sind der Sonderfall: ihre Wortpaare stehen **nicht** in der Config, sondern als eigene
 * `ExerciseItem`-Ebene mit stabilen Ids (der Lernstand hängt daran). Die Config trägt nur Einstellungen.
 */

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type Row = Record<string, any>;

/** Übungstyp → Routen-Segment (Backend: .../chapters/{c}/<segment>). */
export const TYPE_ROUTE: Record<ExerciseTypeKey, string> = {
  Vocabulary: "vocabulary", Arithmetic: "arithmetic", Cloze: "cloze",
  Matching: "matching", List: "list", Birkenbihl: "birkenbihl",
};

/**
 * Kennt dieses UI den Typ überhaupt?
 *
 * Das Backend führt **mehr** Typen als dieses UI (Reading, Listening, Essay, Grammar, Translation,
 * Arithmetic-Drill – der KI-Creator legt Grammar und Translation tatsächlich an), und `ExerciseSummary.type`
 * ist zur Laufzeit ein beliebiger `string`. Ohne diese Prüfung liefen Routen-Aufbau und Zeilen-Editor auf
 * `undefined`, statt den Typ ehrlich als „hier nicht bearbeitbar" zu behandeln.
 */
export function isKnownType(type: string): type is ExerciseTypeKey {
  return Object.prototype.hasOwnProperty.call(TYPE_ROUTE, type);
}

/**
 * Typen, deren Inhalt dieses UI **verlustfrei** hin- und zurückschreiben kann.
 *
 * `Vocabulary` fehlt bewusst: seine Wortpaare sind eine eigene Item-Ebene und werden einzeln gepflegt.
 * `Birkenbihl` fehlt, weil seine Sätze und Wörter **identitätstragende Ids** haben (`sentenceId`/`wordId`
 * adressieren den Austausch-Endpunkt, `vocabularyId` die Store-Bindung). Der Zeilen-Editor kennt nur den
 * Text „Wort:wörtlich" und könnte diese Ids nach einer Änderung keinem Wort mehr zuordnen – ein
 * Vollersatz würde sie also neu vergeben und die Store-Bindung stillschweigend wegwerfen.
 */
const CONTENT_EDITABLE: ExerciseTypeKey[] = ["Arithmetic", "Cloze", "Matching", "List"];
export const isContentEditable = (type: ExerciseTypeKey): boolean => CONTENT_EDITABLE.includes(type);

export const TYPE_LABEL: Record<ExerciseTypeKey, string> = {
  Vocabulary: "Vokabeln", Arithmetic: "Rechnen (feste Aufgaben)", Cloze: "Lückentext",
  Matching: "Zuordnung (Paare)", List: "Liste (auswendig)", Birkenbihl: "Birkenbihl",
};

/** Standard-Abfrageform einer Vokabelübung (TestStage-Werte; "" = Verfahrens-Standard: Selbstcheck → Tippen). */
export const VOCAB_FORMS: { value: number | ""; label: string }[] = [
  { value: "", label: "Standard (Selbstcheck → Tippen)" },
  { value: 1, label: "Nur anzeigen" },
  { value: 2, label: "Selbsteinschätzung" },
  { value: 3, label: "Buchstabenkästchen" },
  { value: 4, label: "Freitext (tippen)" },
  { value: 6, label: "Multiple-Choice (Auswahl)" },
  { value: 5, label: "Hören → tippen" },
];

/** Kommaseparierten Text in eine getrimmte Liste (oder undefined) wandeln – für Alternativen/Wortpool. */
export function splitList(s: string): string[] | undefined {
  const list = s.split(",").map((x) => x.trim()).filter(Boolean);
  return list.length > 0 ? list : undefined;
}

const joinList = (l: unknown): string => (Array.isArray(l) ? l.join(", ") : "");

/** Leere Anfangszeile je Typ (die Felder, die der jeweilige Editor bearbeitet). */
export function emptyRow(type: ExerciseTypeKey): Row {
  switch (type) {
    case "Vocabulary": return { front: "", back: "", hint: "" };
    case "Arithmetic": return { prompt: "", answer: "", tolerance: "0" };
    case "Cloze": return { index: 1, answer: "", alternatives: "" };
    case "Matching": return { left: "", right: "" };
    case "List": return { value: "", alternatives: "" };
    case "Birkenbihl": return { text: "", decoding: "", naturalTranslation: "" };
  }
}

/** Anfangs-Extrafelder je Typ (Richtung/Reihenfolge …). */
export function emptyExtra(type: ExerciseTypeKey): Row {
  return type === "Vocabulary" ? { direction: "front-to-back", sourceLang: "en", targetLang: "de" }
    : type === "List" ? { ordered: false } : {};
}

/**
 * Baut die typ-spezifische Config aus Zeilen/Extrafeldern (Form entspricht den Backend-*Config-Klassen).
 *
 * Bei Vokabeln kommt `base` zum Tragen: beim **Bearbeiten** wird die geladene Config übernommen und nur die
 * Einstellung geändert. Entscheidend ist, dass dabei **keine** `items`/`refs` mitgehen – ein reiner
 * Einstellungs-PUT lässt die per `/items` gepflegte Wortmenge (und damit den Lernstand) unangetastet.
 */
export function buildTypeConfig(
  type: ExerciseTypeKey, rows: Row[], extra: Row,
  opts: { vocabRefs?: { vocabularyId: number }[]; base?: Record<string, unknown> } = {},
): unknown {
  switch (type) {
    case "Vocabulary": {
      const direction = extra.direction || "front-to-back";
      // Das Sprachpaar gehört in die Config, nicht nur in die Auswahl-Oberfläche: der Server braucht es,
      // um später inline ergänzte Wörter im Store anzulegen (und es hält fremdsprachige Treffer heraus).
      const langs = { sourceLang: extra.sourceLang || null, targetLang: extra.targetLang || null };
      return opts.vocabRefs
        ? { ...langs, direction, refs: opts.vocabRefs.map((r) => ({ vocabularyId: r.vocabularyId })) }
        : { ...(opts.base ?? {}), direction, items: [], refs: null };
    }
    case "Arithmetic":
      return { problems: rows.map((r) => ({ prompt: r.prompt, answer: Number(r.answer), tolerance: Number(r.tolerance) || 0 })) };
    case "Cloze":
      return { text: extra.text ?? "", wordBank: splitList(extra.wordBank ?? ""),
        // `vocabKey` unverändert durchreichen: ist er gesetzt, kommt die Lösung aus dem Vokabel-Store und
        // folgt dessen Pflege. Ein Weglassen machte aus der Store-Lücke stillschweigend eine Inline-Lücke.
        gaps: rows.map((r) => ({ index: Number(r.index), answer: r.answer,
          alternatives: splitList(r.alternatives ?? ""), vocabKey: r.vocabKey ?? null })) };
    case "Matching":
      return { instruction: extra.instruction?.trim() || null, pairs: rows.map((r) => ({ left: r.left, right: r.right })) };
    case "List":
      return { instruction: extra.instruction?.trim() || null, ordered: !!extra.ordered,
        items: rows.map((r) => ({ value: r.value, alternatives: splitList(r.alternatives ?? "") })) };
    case "Birkenbihl":
      // Feldnamen müssen zu BirkenbihlSentence/WordPair passen (learningSentence, decoding[{learningWord, gloss}]);
      // sentenceId/wordId lässt der Server beim Speichern vergeben (NormalizeConfig).
      return { learningLang: extra.learningLang ?? "", nativeLang: extra.nativeLang ?? "",
        sentences: rows.map((r) => ({ learningSentence: r.text, naturalTranslation: r.naturalTranslation,
          // Dekodierung als "Wort:wörtlich, Wort:wörtlich" eingegeben – hier in WordPair-Liste geparst.
          decoding: (r.decoding ?? "").split(",").map((p: string) => p.split(":"))
            .filter((kv: string[]) => kv[0]?.trim())
            .map((kv: string[]) => ({ learningWord: kv[0].trim(), gloss: (kv[1] ?? "").trim() || null })) })) };
  }
}

/**
 * Die Rückrichtung: eine geladene Config in den Editor-Zustand übersetzen. Die Gegenprobe zu
 * {@link buildTypeConfig} – jedes Feld, das dort geschrieben wird, muss hier zurückgelesen werden.
 *
 * Nur für Typen aus {@link isKnownType} aufrufen; für alles andere gibt es einen leeren Zustand statt
 * `undefined` (der Typ kommt als `string` von der API, das Compiler-Versprechen der Union gilt hier nicht).
 */
export function configToEditorState(type: ExerciseTypeKey, config: unknown): { rows: Row[]; extra: Row } {
  if (!isKnownType(type)) return { rows: [], extra: {} };
  const c = (config ?? {}) as Record<string, Row[] | Row | string | boolean | undefined>;
  const list = (v: unknown): Row[] => (Array.isArray(v) ? (v as Row[]) : []);
  const fallback = (rows: Row[], extra: Row) => ({ rows: rows.length > 0 ? rows : [emptyRow(type)], extra });

  switch (type) {
    case "Vocabulary":
      // Die Wortpaare liegen in der Item-Tabelle, nicht hier – der Editor zeigt nur die Einstellungen.
      return { rows: [], extra: {
        direction: (c.direction as string) ?? "front-to-back",
        sourceLang: (c.sourceLang as string) ?? "", targetLang: (c.targetLang as string) ?? "",
      } };
    case "Arithmetic":
      return fallback(
        list(c.problems).map((p) => ({ prompt: p.prompt ?? "", answer: String(p.answer ?? ""), tolerance: String(p.tolerance ?? 0) })),
        {});
    case "Cloze":
      return fallback(
        // `vocabKey` wird nicht bearbeitet, aber mitgeführt (siehe buildTypeConfig).
        list(c.gaps).map((g) => ({ index: g.index ?? 1, answer: g.answer ?? "",
          alternatives: joinList(g.alternatives), vocabKey: g.vocabKey ?? null })),
        { text: (c.text as string) ?? "", wordBank: joinList(c.wordBank) });
    case "Matching":
      return fallback(
        list(c.pairs).map((p) => ({ left: p.left ?? "", right: p.right ?? "" })),
        { instruction: (c.instruction as string) ?? "" });
    case "List":
      return fallback(
        list(c.items).map((i) => ({ value: i.value ?? "", alternatives: joinList(i.alternatives) })),
        { instruction: (c.instruction as string) ?? "", ordered: !!c.ordered });
    case "Birkenbihl":
      return fallback(
        list(c.sentences).map((s) => ({
          text: s.learningSentence ?? "",
          naturalTranslation: s.naturalTranslation ?? "",
          decoding: list(s.decoding).map((w) => `${w.learningWord}:${w.gloss ?? ""}`).join(", "),
        })),
        { learningLang: (c.learningLang as string) ?? "", nativeLang: (c.nativeLang as string) ?? "" });
  }
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

/** Zeilen-Editor für alle Typen außer Vokabeln (die haben ihre eigene Item-Ebene). */
export function ConfigEditor({ type, rows, extra, setExtra, patchRow, addRow, removeRow }: EditorProps) {
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

export function RowField({ label, value, onChange, type = "text", placeholder, optional, width }: {
  label: string; value: unknown; onChange: (v: string) => void;
  type?: string; placeholder?: string; optional?: boolean; width?: number;
}) {
  const uid = useId();
  return (
    <div className="field" style={{ flex: width ? "none" : 1, minWidth: width ?? 120, width }}>
      <label htmlFor={uid}>{label}{optional && <span className="muted"> (optional)</span>}</label>
      <input id={uid} type={type} value={String(value ?? "")} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

/** Grobe Pflichtprüfung je Typ: sind die Kernfelder der ersten Zeile gefüllt? */
export function firstRowIncomplete(type: ExerciseTypeKey, rows: Row[], extra: Row, vocabCount: number): boolean {
  const r = rows[0] ?? {};
  switch (type) {
    case "Vocabulary": return vocabCount === 0;
    case "Arithmetic": return !r.prompt || r.answer === "";
    case "Cloze": return !extra.text || !r.answer;
    case "Matching": return !r.left || !r.right;
    case "List": return !r.value;
    case "Birkenbihl": return !r.text || !r.naturalTranslation;
  }
}
