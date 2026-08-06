import { useId } from "react";
import { InfoHint } from "../components/InfoHint";
import { RepeatedTextFields, nonEmpty } from "../components/RepeatedTextFields";
import { RepeatedPairFields, nonEmptyPairs, type Pair } from "../components/RepeatedPairFields";
import type { HelpTopic } from "../lib/fieldHelp";
import { LANGUAGES } from "../lib/languages";
import type { ArithmeticOperation, ExerciseTypeKey } from "../lib/types";

/*
 * Die typ-spezifische Inhalts-Maschinerie einer Übung – geteilt zwischen Anlegen (VaterExercises) und
 * Bearbeiten (ExerciseEditModal). Sie liegt hier, weil beide Richtungen zueinander passen müssen:
 * `buildTypeConfig` schreibt die Config, `configToEditorState` liest sie zurück. Divergierten die zwei,
 * würde Bearbeiten Inhalte still verlieren.
 *
 * Anzeigename und Routen-Segment stehen NICHT hier, sondern im Typ-Manifest des Servers
 * (`lib/exerciseTypes.ts`) – sie sind seine Sache, nicht die des Editors.
 *
 * Zwei Typen fehlen bewusst in `CONTENT_EDITABLE`: Vokabeln (Wortpaare sind eine eigene Item-Ebene mit
 * stabilen Ids) und Birkenbihl (Sätze/Wörter tragen identitätsstiftende Ids).
 */

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type Row = Record<string, any>;

/*
 * B-74: `Row`/`Row[]` stay the public signature of every function below (and of `ConfigEditor`/
 * `RowField`/`RowRepeatedField`) - a full discriminated union would need `type`/`rows`/`extra` bundled
 * into one state object in the two callers (`VaterExerciseCreate.tsx`, `ExerciseEditModal.tsx`), which
 * today hold them as three separate `useState`s. Without that bundling a union is structurally
 * compatible with `Record<string, any>` and therefore wouldn't catch anything at the caller.
 *
 * What IS won here: inside `emptyRow`/`emptyExtra` each literal is checked with `satisfies` against the
 * real shape, and inside `buildTypeConfig`/`configToEditorState` each `case` casts once to the matching
 * interface below - from that point on, `tsc` catches a renamed or mistyped field in that branch. Not
 * every type needs its own `…Extra` interface: several fall through to `default: return {}` and stay
 * untyped, since there's nothing there to check against.
 */
interface VocabularyRow { front: string; back: string; hint: string }
interface VocabularyExtra { direction: string; sourceLang: string; targetLang: string }

interface ArithmeticRow { prompt: string; answer: string; tolerance: string }
interface ArithmeticDrillExtra {
  operations: ArithmeticOperation[]; minOperand: string; maxOperand: string; problemCount: string;
  allowNegativeResults: boolean; divisionMustBeWhole: boolean; seed: string;
}

interface ClozeRow { index: number; answer: string; alternatives: string[]; vocabKey: string | null }
interface ClozeExtra { text: string; wordBank: string[] }

interface MatchingRow { left: string; right: string }
interface MatchingExtra { instruction: string }

interface ListRow { value: string; alternatives: string[] }
interface ListExtra { instruction: string; ordered: boolean }

/** Shared by Reading and Listening - both ask a comprehension question the same way. */
interface QuestionRow { prompt: string; answer: string; choices: string[] }
interface ReadingExtra { text: string }
interface ListeningExtra { audioUrl: string; transcript: string }

interface EssayRow { criterion: string; maxScore: string }
interface EssayExtra { prompt: string; minWords: string; maxWords: string }

interface GrammarRow { prompt: string; answer: string; ruleHint: string }
interface GrammarExtra { instruction: string }

interface TranslationRow {
  source: string; target: string; alternatives: string[]; vocabularyId: number | null;
  /** Only present once loaded back from a saved config - see `configToEditorState`. */
  origSource?: string; origTarget?: string;
}
interface TranslationExtra { sourceLang: string; targetLang: string }

interface BirkenbihlRow { text: string; naturalTranslation: string; decoding: Pair[] }
interface BirkenbihlExtra { learningLang: string; nativeLang: string }

/** Reihenfolge im Typ-Pulldown: erst die alltäglichen, dann die selteneren. */
export const AUTHORABLE_TYPES: ExerciseTypeKey[] = [
  "Vocabulary", "Cloze", "Matching", "List", "Grammar", "Translation",
  "Reading", "Listening", "Essay", "Arithmetic", "ArithmeticDrill", "Birkenbihl",
];

/** Hat diese UI einen Editor für den Typ? `Exercise.type` ist zur Laufzeit ein beliebiger `string`. */
export function isKnownType(type: string): type is ExerciseTypeKey {
  return (AUTHORABLE_TYPES as string[]).includes(type);
}

/**
 * Typen, deren Inhalt diese UI **verlustfrei** hin- und zurückschreiben kann.
 *
 * `Vocabulary` fehlt, weil seine Wortpaare als eigene `ExerciseItem`-Ebene mit stabilen Ids liegen (der
 * Lernstand des Kindes hängt daran) – die werden einzeln gepflegt. `Birkenbihl` fehlt, weil `sentenceId`,
 * `wordId` und `vocabularyId` den Austausch-Endpunkt und die Store-Bindung adressieren: der Zeilen-Editor
 * kennt nur den Text „Wort:wörtlich" und könnte diese Ids nach einer Änderung keinem Wort mehr zuordnen.
 */
const CONTENT_EDITABLE: ExerciseTypeKey[] = [
  "Cloze", "Matching", "List", "Grammar", "Translation",
  "Reading", "Listening", "Essay", "Arithmetic", "ArithmeticDrill",
];
export const isContentEditable = (type: ExerciseTypeKey): boolean => CONTENT_EDITABLE.includes(type);

/** Typen ohne Zeilenliste: ihr Inhalt sind reine Einstellungen. */
const ROWLESS: ExerciseTypeKey[] = ["ArithmeticDrill"];

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

export const ARITHMETIC_OPERATIONS: { value: ArithmeticOperation; label: string }[] = [
  { value: "Addition", label: "+ Addition" },
  { value: "Subtraction", label: "− Subtraktion" },
  { value: "Multiplication", label: "× Multiplikation" },
  { value: "Division", label: "÷ Division" },
];

/**
 * Eine geladene Liste als Zeilen-Wert. Der Editor führt sie seit B-69 als **Liste**, nicht mehr als
 * kommagetrennten Text – ein Wert, der selbst ein Komma enthält, wurde sonst beim Senden zerrissen.
 */
const strList = (l: unknown): string[] => (Array.isArray(l) ? l.map((x) => String(x ?? "")) : []);

/** Wie der Server vergleicht (`StageMechanics.Normalize`): trimmen, klein, Mehrfach-Leerzeichen zusammen. */
const normalizeAnswer = (s: string) => s.trim().toLowerCase().split(/\s+/).filter(Boolean).join(" ");

/**
 * Auswahl gefüllt, aber die Lösung steht nicht darin. Seit B-73 spielt der Server die Möglichkeiten aus:
 * Das Kind bekommt dann **nur** diese Knöpfe und kein Eingabefeld – jede Antwort ist falsch, der
 * Leitner-Kasten kommt nie vorwärts und der Malus greift. Nur ein Hinweis, keine Sperre: eine halbfertige
 * Übung darf man speichern (die Auswahl selbst ist optional).
 */
const choicesMissAnswer = (r: Row): boolean => {
  const choices = strList(r.choices).map(normalizeAnswer).filter((c) => c !== "");
  const answer = normalizeAnswer(String(r.answer ?? ""));
  return choices.length > 0 && answer !== "" && !choices.includes(answer);
};

const numOr = (v: unknown, fallback: number): number => (v === "" || v == null ? fallback : Number(v));
const numOrNull = (v: unknown): number | null => (v === "" || v == null ? null : Number(v));

/** Leere Anfangszeile je Typ (die Felder, die der jeweilige Editor bearbeitet). */
export function emptyRow(type: ExerciseTypeKey): Row {
  switch (type) {
    case "Vocabulary": return { front: "", back: "", hint: "" } satisfies VocabularyRow;
    case "Arithmetic": return { prompt: "", answer: "", tolerance: "0" } satisfies ArithmeticRow;
    case "ArithmeticDrill": return {};
    case "Cloze": return { index: 1, answer: "", alternatives: [], vocabKey: null } satisfies ClozeRow;
    case "Matching": return { left: "", right: "" } satisfies MatchingRow;
    case "List": return { value: "", alternatives: [] } satisfies ListRow;
    case "Birkenbihl": return { text: "", decoding: [], naturalTranslation: "" } satisfies BirkenbihlRow;
    case "Reading":
    case "Listening": return { prompt: "", choices: [], answer: "" } satisfies QuestionRow;
    case "Essay": return { criterion: "", maxScore: "5" } satisfies EssayRow;
    case "Grammar": return { prompt: "", answer: "", ruleHint: "" } satisfies GrammarRow;
    case "Translation": return { source: "", target: "", alternatives: [], vocabularyId: null } satisfies TranslationRow;
  }
}

/** Anfangs-Extrafelder je Typ (Richtung, Trägertext, Regeln …). */
export function emptyExtra(type: ExerciseTypeKey): Row {
  switch (type) {
    case "Vocabulary": return { direction: "front-to-back", sourceLang: "en", targetLang: "de" } satisfies VocabularyExtra;
    // Partial on purpose: `instruction` has no meaningful initial value, it arrives once the Vater types it.
    case "List": return { ordered: false } satisfies Partial<ListExtra>;
    case "Translation": return { sourceLang: "en", targetLang: "de" } satisfies TranslationExtra;
    case "Essay": return { prompt: "", minWords: "", maxWords: "" } satisfies EssayExtra;
    case "Listening": return { audioUrl: "", transcript: "" } satisfies ListeningExtra;
    case "ArithmeticDrill": return {
      operations: ["Addition"], minOperand: "1", maxOperand: "10", problemCount: "10",
      allowNegativeResults: false, divisionMustBeWhole: true, seed: "",
    } satisfies ArithmeticDrillExtra;
    default: return {};
  }
}

/** Zeigt dieser Typ überhaupt eine Zeilenliste? */
export const hasRows = (type: ExerciseTypeKey): boolean => !ROWLESS.includes(type);

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
      const cfg = extra as VocabularyExtra;
      const direction = cfg.direction || "front-to-back";
      // Das Sprachpaar gehört in die Config, nicht nur in die Auswahl-Oberfläche: der Server braucht es,
      // um später inline ergänzte Wörter im Store anzulegen (und es hält fremdsprachige Treffer heraus).
      const langs = { sourceLang: cfg.sourceLang || null, targetLang: cfg.targetLang || null };
      return opts.vocabRefs
        ? { ...langs, direction, refs: opts.vocabRefs.map((r) => ({ vocabularyId: r.vocabularyId })) }
        : { ...(opts.base ?? {}), direction, items: [], refs: null };
    }
    case "Arithmetic": {
      const items = rows as ArithmeticRow[];
      return { problems: items.map((r) => ({ prompt: r.prompt, answer: Number(r.answer), tolerance: Number(r.tolerance) || 0 })) };
    }
    case "ArithmeticDrill": {
      const cfg = extra as ArithmeticDrillExtra;
      // Gespeichert werden nur die REGELN; die Aufgaben erzeugt der Server je Durchlauf.
      return {
        operations: cfg.operations ?? ["Addition"],
        minOperand: numOr(cfg.minOperand, 1),
        maxOperand: numOr(cfg.maxOperand, 10),
        problemCount: numOr(cfg.problemCount, 10),
        allowNegativeResults: !!cfg.allowNegativeResults,
        divisionMustBeWhole: !!cfg.divisionMustBeWhole,
        // Fester Seed = reproduzierbare Aufgaben; leer = echter Zufall je Durchlauf.
        seed: numOrNull(cfg.seed),
      };
    }
    case "Cloze": {
      const items = rows as ClozeRow[];
      const cfg = extra as ClozeExtra;
      return { text: cfg.text ?? "", wordBank: nonEmpty(strList(cfg.wordBank)),
        // `vocabKey` unverändert durchreichen: ist er gesetzt, kommt die Lösung aus dem Vokabel-Store und
        // folgt dessen Pflege. Ein Weglassen machte aus der Store-Lücke stillschweigend eine Inline-Lücke.
        gaps: items.map((r) => ({ index: Number(r.index), answer: r.answer,
          alternatives: nonEmpty(strList(r.alternatives)), vocabKey: r.vocabKey ?? null })) };
    }
    case "Matching": {
      const items = rows as MatchingRow[];
      const cfg = extra as MatchingExtra;
      return { instruction: cfg.instruction?.trim() || null, pairs: items.map((r) => ({ left: r.left, right: r.right })) };
    }
    case "List": {
      const items = rows as ListRow[];
      const cfg = extra as ListExtra;
      return { instruction: cfg.instruction?.trim() || null, ordered: !!cfg.ordered,
        items: items.map((r) => ({ value: r.value, alternatives: nonEmpty(strList(r.alternatives)) })) };
    }
    case "Reading": {
      const cfg = extra as ReadingExtra;
      return { text: cfg.text ?? "", questions: (rows as QuestionRow[]).map(toQuestion) };
    }
    case "Listening": {
      const cfg = extra as ListeningExtra;
      return {
        audioUrl: cfg.audioUrl ?? "",
        transcript: cfg.transcript?.trim() || null,
        questions: (rows as QuestionRow[]).map(toQuestion),
      };
    }
    case "Essay": {
      const items = rows as EssayRow[];
      const cfg = extra as EssayExtra;
      return {
        prompt: cfg.prompt ?? "",
        minWords: numOrNull(cfg.minWords),
        maxWords: numOrNull(cfg.maxWords),
        // Ohne Kriterien lieber `null` als eine leere Liste – so bleibt „keine Rubrik" erkennbar.
        rubric: items.some((r) => r.criterion?.trim())
          ? items.filter((r) => r.criterion?.trim())
            .map((r) => ({ criterion: r.criterion.trim(), maxScore: numOr(r.maxScore, 1) }))
          : null,
      };
    }
    case "Grammar": {
      const items = rows as GrammarRow[];
      const cfg = extra as GrammarExtra;
      return {
        instruction: cfg.instruction?.trim() || null,
        tasks: items.map((r) => ({ prompt: r.prompt, answer: r.answer, ruleHint: r.ruleHint?.trim() || null })),
      };
    }
    case "Translation": {
      const items = rows as TranslationRow[];
      const cfg = extra as TranslationExtra;
      return {
        sourceLang: cfg.sourceLang || "", targetLang: cfg.targetLang || "",
        items: items.map((r) => ({
          source: r.source, target: r.target, alternatives: nonEmpty(strList(r.alternatives)),
          /*
           * Die Store-Bindung nur behalten, solange der Text unverändert ist. Wurde er bearbeitet, meint
           * das Paar ein anderes Wort – dann muss der Server neu auflösen (er legt es an bzw. findet es),
           * sonst zeigte die Übung auf die Vokabel des alten Wortlauts.
           */
          vocabularyId: r.source === r.origSource && r.target === r.origTarget ? r.vocabularyId ?? null : null,
        })),
      };
    }
    case "Birkenbihl": {
      const items = rows as BirkenbihlRow[];
      const cfg = extra as BirkenbihlExtra;
      // Feldnamen müssen zu BirkenbihlSentence/WordPair passen (learningSentence, decoding[{learningWord, gloss}]);
      // sentenceId/wordId lässt der Server beim Speichern vergeben (NormalizeConfig).
      return { learningLang: cfg.learningLang ?? "", nativeLang: cfg.nativeLang ?? "",
        sentences: items.map((r) => ({ learningSentence: r.text, naturalTranslation: r.naturalTranslation,
          // Dekodierung als eigene Paar-Zeilen (Wort, Glosse) eingegeben - kein Trennzeichen-Split mehr,
          // ein Komma oder Doppelpunkt in Wort/Glosse zerriss den Eintrag vorher lautlos (B-72).
          decoding: nonEmptyPairs(r.decoding ?? []) })) };
    }
  }
}

/** Verständnisfrage: leere Auswahl heißt Freitext-Antwort, gefüllte heißt Multiple-Choice. */
const toQuestion = (r: QuestionRow) => ({
  prompt: r.prompt, answer: r.answer, choices: nonEmpty(strList(r.choices)) ?? null,
});

const fromQuestion = (q: Row): QuestionRow => ({
  prompt: q.prompt ?? "", answer: q.answer ?? "", choices: strList(q.choices),
});

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
  const str = (v: unknown): string => (typeof v === "string" ? v : "");
  const num = (v: unknown): string => (v == null ? "" : String(v));
  const fallback = (rows: Row[], extra: Row) => ({ rows: rows.length > 0 ? rows : [emptyRow(type)], extra });

  switch (type) {
    case "Vocabulary":
      // Die Wortpaare liegen in der Item-Tabelle, nicht hier – der Editor zeigt nur die Einstellungen.
      return { rows: [], extra: {
        direction: str(c.direction) || "front-to-back",
        sourceLang: str(c.sourceLang), targetLang: str(c.targetLang),
      } satisfies VocabularyExtra };
    case "Arithmetic":
      return fallback(
        list(c.problems).map((p): ArithmeticRow => ({ prompt: p.prompt ?? "", answer: num(p.answer), tolerance: num(p.tolerance ?? 0) })),
        {});
    case "ArithmeticDrill":
      return { rows: [], extra: {
        operations: (list(c.operations) as unknown as ArithmeticOperation[]).length > 0
          ? (c.operations as unknown as ArithmeticOperation[]) : ["Addition"],
        minOperand: num(c.minOperand ?? 1), maxOperand: num(c.maxOperand ?? 10),
        problemCount: num(c.problemCount ?? 10),
        allowNegativeResults: !!c.allowNegativeResults,
        divisionMustBeWhole: c.divisionMustBeWhole !== false,
        seed: num(c.seed),
      } satisfies ArithmeticDrillExtra };
    case "Cloze":
      return fallback(
        // `vocabKey` wird nicht bearbeitet, aber mitgeführt (siehe buildTypeConfig).
        list(c.gaps).map((g): ClozeRow => ({ index: g.index ?? 1, answer: g.answer ?? "",
          alternatives: strList(g.alternatives), vocabKey: g.vocabKey ?? null })),
        { text: str(c.text), wordBank: strList(c.wordBank) } satisfies ClozeExtra);
    case "Matching":
      return fallback(
        list(c.pairs).map((p): MatchingRow => ({ left: p.left ?? "", right: p.right ?? "" })),
        { instruction: str(c.instruction) } satisfies MatchingExtra);
    case "List":
      return fallback(
        list(c.items).map((i): ListRow => ({ value: i.value ?? "", alternatives: strList(i.alternatives) })),
        { instruction: str(c.instruction), ordered: !!c.ordered } satisfies ListExtra);
    case "Reading":
      return fallback(list(c.questions).map(fromQuestion), { text: str(c.text) } satisfies ReadingExtra);
    case "Listening":
      return fallback(list(c.questions).map(fromQuestion),
        { audioUrl: str(c.audioUrl), transcript: str(c.transcript) } satisfies ListeningExtra);
    case "Essay":
      return fallback(
        list(c.rubric).map((r): EssayRow => ({ criterion: r.criterion ?? "", maxScore: num(r.maxScore ?? 5) })),
        { prompt: str(c.prompt), minWords: num(c.minWords), maxWords: num(c.maxWords) } satisfies EssayExtra);
    case "Grammar":
      return fallback(
        list(c.tasks).map((t): GrammarRow => ({ prompt: t.prompt ?? "", answer: t.answer ?? "", ruleHint: t.ruleHint ?? "" })),
        { instruction: str(c.instruction) } satisfies GrammarExtra);
    case "Translation":
      return fallback(
        // origSource/origTarget merken sich den geladenen Wortlaut: nur solange er steht, darf die
        // Store-Bindung (vocabularyId) mitwandern.
        list(c.items).map((i): TranslationRow => ({
          source: i.source ?? "", target: i.target ?? "", alternatives: strList(i.alternatives),
          vocabularyId: i.vocabularyId ?? null, origSource: i.source ?? "", origTarget: i.target ?? "",
        })),
        { sourceLang: str(c.sourceLang) || "en", targetLang: str(c.targetLang) || "de" } satisfies TranslationExtra);
    case "Birkenbihl":
      return fallback(
        list(c.sentences).map((s): BirkenbihlRow => ({
          text: s.learningSentence ?? "",
          naturalTranslation: s.naturalTranslation ?? "",
          decoding: list(s.decoding).map((w) => ({ word: w.learningWord ?? "", gloss: w.gloss ?? "" })),
        })),
        { learningLang: str(c.learningLang), nativeLang: str(c.nativeLang) } satisfies BirkenbihlExtra);
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

/** Beschriftung des „+ Zeile"-Knopfs – „+ Zeile" sagt bei einer Verständnisfrage zu wenig. */
const ROW_LABEL: Partial<Record<ExerciseTypeKey, string>> = {
  Reading: "+ Frage", Listening: "+ Frage", Essay: "+ Kriterium",
  Grammar: "+ Aufgabe", Translation: "+ Satz", Cloze: "+ Lücke",
  Matching: "+ Paar", List: "+ Eintrag", Arithmetic: "+ Aufgabe", Birkenbihl: "+ Satz",
};

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
          <div className="field"><span className="label-row">Wortpool <span className="muted">(optional)</span></span>
            <RepeatedTextFields label="Wort" placeholder="achète"
              values={Array.isArray(extra.wordBank) ? extra.wordBank : []}
              onChange={(v) => ex({ wordBank: v })} /></div>
        </>
      )}
      {(type === "Matching" || type === "List" || type === "Grammar") && (
        // Bei der Liste ist die Anweisung Pflicht, und das steht am Feld: sie ist der einzige Text, den die Karte
        // trägt – alle Einträge teilen sich einen Prompt (B-77/E7). Der Server weist es ohnehin ab; ohne das Wort
        // hier liefe der Vater erst nach sechzehn getippten Einträgen in eine englische Serverantwort.
        <div className="field"><label>Anweisung{type === "List" ? "" : " (optional)"}</label>
          <input aria-label="Anweisung" value={extra.instruction ?? ""} onChange={(e) => ex({ instruction: e.target.value })}
            placeholder={type === "Grammar" ? "Setze ins Passiv." : type === "List" ? "Nenne alle …" : "Ordne zu."} /></div>
      )}
      {type === "List" && (
        <label className="checkline"><input type="checkbox" checked={!!extra.ordered} onChange={(e) => ex({ ordered: e.target.checked })} /> Reihenfolge zählt</label>
      )}
      {type === "Reading" && (
        <div className="field">
          <label htmlFor="cfg-reading-text">Lesetext</label>
          <textarea id="cfg-reading-text" rows={6} value={extra.text ?? ""} onChange={(e) => ex({ text: e.target.value })}
            placeholder="Der Text, den das Kind liest. Die Fragen darunter beziehen sich darauf." />
        </div>
      )}
      {type === "Listening" && (
        <>
          <div className="field">
            <label htmlFor="cfg-audio">Audio-URL</label>
            <input id="cfg-audio" value={extra.audioUrl ?? ""} onChange={(e) => ex({ audioUrl: e.target.value })}
              placeholder="https://… (mp3/ogg)" />
            <span className="sub">Die Datei wird direkt abgespielt – sie muss öffentlich erreichbar sein.</span>
          </div>
          <div className="field">
            <label htmlFor="cfg-transcript">Transkript <span className="muted">(optional)</span></label>
            <textarea id="cfg-transcript" rows={4} value={extra.transcript ?? ""} onChange={(e) => ex({ transcript: e.target.value })}
              placeholder="Wortlaut des Hörtexts – hilft dir beim Prüfen, das Kind sieht ihn nicht." />
          </div>
        </>
      )}
      {type === "Essay" && (
        <>
          <div className="field">
            <label htmlFor="cfg-prompt">Schreibauftrag</label>
            <textarea id="cfg-prompt" rows={4} value={extra.prompt ?? ""} onChange={(e) => ex({ prompt: e.target.value })}
              placeholder="Schreibe einen Brief an deinen Austauschpartner über deine Hobbys." />
          </div>
          <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ maxWidth: 150 }}>
              <label htmlFor="cfg-minwords">Mindestens Wörter</label>
              <input id="cfg-minwords" type="number" min={0} placeholder="frei" value={extra.minWords ?? ""}
                onChange={(e) => ex({ minWords: e.target.value })} />
            </div>
            <div className="field" style={{ maxWidth: 150 }}>
              <label htmlFor="cfg-maxwords">Höchstens Wörter</label>
              <input id="cfg-maxwords" type="number" min={0} placeholder="frei" value={extra.maxWords ?? ""}
                onChange={(e) => ex({ maxWords: e.target.value })} />
            </div>
          </div>
          <p className="sub" style={{ margin: 0 }}>
            Ein Aufsatz wird <strong>nicht automatisch bewertet</strong> – er lässt sich darum auch nicht
            durchspielen. Die Kriterien unten sind deine Notizen zum Korrigieren (optional).
          </p>
        </>
      )}
      {type === "Translation" && (
        <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
          <div className="field" style={{ maxWidth: 180 }}>
            <label htmlFor="cfg-tr-src">Ausgangssprache</label>
            <select id="cfg-tr-src" value={extra.sourceLang ?? "en"} onChange={(e) => ex({ sourceLang: e.target.value })}>
              {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
            </select>
          </div>
          <div className="field" style={{ maxWidth: 180 }}>
            <label htmlFor="cfg-tr-tgt">Zielsprache</label>
            <select id="cfg-tr-tgt" value={extra.targetLang ?? "de"} onChange={(e) => ex({ targetLang: e.target.value })}>
              {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
            </select>
          </div>
          <p className="sub" style={{ flex: 1, minWidth: 240, margin: 0 }}>
            Jedes Satzpaar landet im <strong>Vokabel-Store</strong> und bleibt dort verknüpft – deshalb sind
            die Sprachcodes Pflicht.
          </p>
        </div>
      )}
      {type === "ArithmeticDrill" && (
        <>
          <div className="field">
            <label>Rechenarten</label>
            <div className="row" style={{ gap: 14, flexWrap: "wrap" }}>
              {ARITHMETIC_OPERATIONS.map((o) => {
                const on = ((extra.operations as ArithmeticOperation[] | undefined) ?? []).includes(o.value);
                return (
                  <label key={o.value} className="checkline">
                    <input type="checkbox" checked={on} onChange={() => {
                      const cur = (extra.operations as ArithmeticOperation[] | undefined) ?? [];
                      ex({ operations: on ? cur.filter((x) => x !== o.value) : [...cur, o.value] });
                    }} /> {o.label}
                  </label>
                );
              })}
            </div>
            <span className="sub">Je Aufgabe wird zufällig eine der erlaubten Arten gewählt.</span>
          </div>
          <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ maxWidth: 130 }}>
              <label htmlFor="cfg-min">Zahlen von</label>
              <input id="cfg-min" type="number" value={extra.minOperand ?? "1"} onChange={(e) => ex({ minOperand: e.target.value })} />
            </div>
            <div className="field" style={{ maxWidth: 130 }}>
              <label htmlFor="cfg-max">bis</label>
              <input id="cfg-max" type="number" value={extra.maxOperand ?? "10"} onChange={(e) => ex({ maxOperand: e.target.value })} />
            </div>
            <div className="field" style={{ maxWidth: 150 }}>
              <label htmlFor="cfg-count">Aufgaben je Durchlauf</label>
              <input id="cfg-count" type="number" min={1} value={extra.problemCount ?? "10"} onChange={(e) => ex({ problemCount: e.target.value })} />
            </div>
            <div className="field" style={{ maxWidth: 150 }}>
              <label htmlFor="cfg-seed">Fester Seed <span className="muted">(optional)</span></label>
              <input id="cfg-seed" type="number" placeholder="Zufall" value={extra.seed ?? ""} onChange={(e) => ex({ seed: e.target.value })} />
            </div>
          </div>
          <div className="row" style={{ gap: 16, flexWrap: "wrap" }}>
            <label className="checkline">
              <input type="checkbox" checked={!!extra.allowNegativeResults}
                onChange={(e) => ex({ allowNegativeResults: e.target.checked })} /> negative Ergebnisse erlauben
            </label>
            <label className="checkline">
              <input type="checkbox" checked={extra.divisionMustBeWhole !== false}
                onChange={(e) => ex({ divisionMustBeWhole: e.target.checked })} /> Division muss aufgehen
            </label>
          </div>
          <p className="sub" style={{ margin: 0 }}>
            Gespeichert werden nur diese <strong>Regeln</strong> – die Aufgaben erzeugt der Server bei jedem
            Durchlauf neu. Ein fester Seed macht sie reproduzierbar (gleiche Aufgaben für Geschwister).
          </p>
        </>
      )}
      {type === "Birkenbihl" && (
        <div className="form-grid">
          <div className="field"><label>Lernsprache</label><input value={extra.learningLang ?? ""} onChange={(e) => ex({ learningLang: e.target.value })} placeholder="Englisch" /></div>
          <div className="field"><label>Muttersprache</label><input value={extra.nativeLang ?? ""} onChange={(e) => ex({ nativeLang: e.target.value })} placeholder="Deutsch" /></div>
        </div>
      )}

      {/* Zeilen */}
      {hasRows(type) && rows.map((r, i) => (
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
            <RowRepeatedField label="Auch richtig" values={r.alternatives} scope={`Zeile ${i + 1}`}
              placeholder="zählt auch als richtig" optional topic={i === 0 ? "alsoCorrect" : undefined}
              onChange={(v) => patchRow(i, { alternatives: v })} />
          </>}
          {type === "Matching" && <>
            <RowField label="Links" value={r.left} onChange={(v) => patchRow(i, { left: v })} />
            <RowField label="Rechts" value={r.right} onChange={(v) => patchRow(i, { right: v })} />
          </>}
          {type === "List" && <>
            <RowField label="Eintrag" value={r.value} onChange={(v) => patchRow(i, { value: v })} />
            <RowRepeatedField label="Auch richtig" values={r.alternatives} scope={`Zeile ${i + 1}`}
              placeholder="zählt auch als richtig" optional topic={i === 0 ? "alsoCorrect" : undefined}
              onChange={(v) => patchRow(i, { alternatives: v })} />
          </>}
          {(type === "Reading" || type === "Listening") && <>
            <RowField label="Frage" value={r.prompt} onChange={(v) => patchRow(i, { prompt: v })} placeholder="Wohin fährt Tom?" />
            <RowField label="Antwort" value={r.answer} onChange={(v) => patchRow(i, { answer: v })} />
            <RowRepeatedField label="Auswahl" values={r.choices} scope={`Zeile ${i + 1}`}
              placeholder="eine Antwortmöglichkeit" optional topic={i === 0 ? "questionChoices" : undefined}
              onChange={(v) => patchRow(i, { choices: v })} />
            {choicesMissAnswer(r) && (
              <span className="error-box" role="status">
                Die Antwort steht nicht in der Auswahl – das Kind bekommt nur diese Knöpfe und kann nicht richtig antworten.
              </span>
            )}
          </>}
          {type === "Essay" && <>
            <RowField label="Kriterium" value={r.criterion} onChange={(v) => patchRow(i, { criterion: v })} placeholder="Aufbau" />
            <RowField label="Punkte" value={r.maxScore} onChange={(v) => patchRow(i, { maxScore: v })} type="number" width={90} />
          </>}
          {type === "Grammar" && <>
            <RowField label="Aufgabe" value={r.prompt} onChange={(v) => patchRow(i, { prompt: v })} placeholder="He ___ (go) to school." />
            <RowField label="Lösung" value={r.answer} onChange={(v) => patchRow(i, { answer: v })} placeholder="goes" />
            <RowField label="Regel-Hinweis" value={r.ruleHint} onChange={(v) => patchRow(i, { ruleHint: v })} optional />
          </>}
          {type === "Translation" && <>
            <RowField label="Satz (Ausgangssprache)" value={r.source} onChange={(v) => patchRow(i, { source: v })} placeholder="Where do you live?" />
            <RowField label="Übersetzung" value={r.target} onChange={(v) => patchRow(i, { target: v })} placeholder="Wo wohnst du?" />
            <RowRepeatedField label="Auch richtig" values={r.alternatives} scope={`Zeile ${i + 1}`}
              placeholder="zählt auch als richtig" optional topic={i === 0 ? "alsoCorrect" : undefined}
              onChange={(v) => patchRow(i, { alternatives: v })} />
          </>}
          {type === "Birkenbihl" && <>
            <RowField label="Satz (Lernsprache)" value={r.text} onChange={(v) => patchRow(i, { text: v })} />
            <RowRepeatedPairField label="Dekodierung" wordLabel="Wort" glossLabel="wörtlich"
              scope={`Satz ${i + 1}`} pairs={r.decoding} onChange={(v) => patchRow(i, { decoding: v })} />
            <RowField label="Natürliche Übersetzung" value={r.naturalTranslation} onChange={(v) => patchRow(i, { naturalTranslation: v })} />
          </>}
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => removeRow(i)} aria-label="Zeile entfernen">×</button>
        </div>
      ))}
      {hasRows(type) && (
        <button type="button" className="btn ghost" style={{ width: "auto", alignSelf: "flex-start" }} onClick={addRow}>
          {ROW_LABEL[type] ?? "+ Zeile"}
        </button>
      )}
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

/**
 * Wie {@link RowField}, aber für eine **Liste** gleichartiger Werte – ein Eingabefeld je Wert.
 *
 * Das abgelöste kommagetrennte Sammelfeld zerriss jeden Wert, der selbst ein Komma enthält (B-69); bei
 * einer Übersetzung („Wo wohnst du, Tom?") ist das der Normalfall, nicht der Rand.
 *
 * Die Überschrift ist ein `span` und **kein** `label` – auch mit Erklärung: Es gibt kein einzelnes Feld,
 * auf das sie zeigen könnte, und ein `<label>` ohne Ziel tut beim Anklicken nichts. Die Namen tragen die
 * Felder selbst, unterschieden über `scope`.
 */
export function RowRepeatedField({ label, values, onChange, scope, placeholder, optional, topic }: {
  label: string; values: unknown; onChange: (v: string[]) => void;
  /** Unterscheidet die Zeilen voneinander, z. B. „Zeile 2" – sonst heißen alle Felder „Auch richtig 1". */
  scope: string;
  placeholder?: string; optional?: boolean;
  /**
   * Feld-Erklärung. Bewusst **nur an der ersten Zeile** setzen: Der Hinweis-Knopf heißt nach dem Feld,
   * und acht Zeilen ergäben acht gleichnamige Knöpfe (Muster wie im Vokabel-Store).
   */
  topic?: HelpTopic;
}) {
  return (
    <div className="field" style={{ flex: 1, minWidth: 160 }}>
      <span className="label-row">
        {label}{optional && <span className="muted"> (optional)</span>}
        {topic && <InfoHint topic={topic} />}
      </span>
      <RepeatedTextFields label={label} scope={scope} placeholder={placeholder}
        values={Array.isArray(values) ? values : []} onChange={onChange} />
    </div>
  );
}

/**
 * Wie {@link RowRepeatedField}, aber für Paare (Muster {@link RepeatedPairFields}) - bislang nur für die
 * Birkenbihl-Dekodierung gebraucht (Wort der Lernsprache, wörtliche Glosse).
 */
export function RowRepeatedPairField({ label, wordLabel, glossLabel, pairs, onChange, scope }: {
  /** Überschrift über beiden Feldern (z. B. „Dekodierung"). */
  label: string;
  wordLabel: string;
  glossLabel: string;
  pairs: unknown;
  onChange: (pairs: Pair[]) => void;
  scope: string;
}) {
  return (
    <div className="field" style={{ flex: 1, minWidth: 220 }}>
      <span className="label-row">{label}</span>
      <RepeatedPairFields wordLabel={wordLabel} glossLabel={glossLabel} scope={scope}
        pairs={Array.isArray(pairs) ? pairs : []} onChange={onChange} />
    </div>
  );
}

/**
 * Grobe Pflichtprüfung je Typ: fehlt der Kern-Inhalt? Liefert die Meldung, die dem Vater sagt, was fehlt –
 * `null`, wenn alles Nötige da ist.
 */
export function contentProblem(type: ExerciseTypeKey, rows: Row[], extra: Row, vocabCount: number): string | null {
  const r = rows[0] ?? {};
  switch (type) {
    case "Vocabulary": return vocabCount === 0 ? "Bitte mindestens eine Vokabel wählen." : null;
    case "Arithmetic": return !r.prompt || r.answer === "" ? "Bitte mindestens eine Aufgabe mit Lösung angeben." : null;
    case "ArithmeticDrill": {
      const ops = (extra.operations as string[] | undefined) ?? [];
      if (ops.length === 0) return "Bitte mindestens eine Rechenart wählen.";
      if (Number(extra.minOperand) > Number(extra.maxOperand)) return "Die untere Zahlengrenze darf nicht über der oberen liegen.";
      return Number(extra.problemCount) < 1 ? "Es muss mindestens eine Aufgabe je Durchlauf geben." : null;
    }
    case "Cloze": return !extra.text ? "Bitte den Trägertext angeben." : !r.answer ? "Bitte mindestens eine Lücke mit Lösung angeben." : null;
    case "Matching": return !r.left || !r.right ? "Bitte mindestens ein Paar angeben." : null;
    // Die Anweisung zuerst: sie ist der einzige Text auf der Karte, weil alle Einträge sich einen Prompt teilen
    // (B-77/E7). Der Server weist sie auf POST und PUT ab – hier steht sie deutsch und vor dem Abschicken.
    case "List":
      if (!extra.instruction) return "Bitte die Anweisung angeben – sie ist der einzige Text auf der Karte.";
      return !r.value ? "Bitte mindestens einen Eintrag angeben." : null;
    case "Reading": return !extra.text ? "Bitte den Lesetext angeben." : !r.prompt || !r.answer ? "Bitte mindestens eine Frage mit Antwort angeben." : null;
    case "Listening": return !extra.audioUrl ? "Bitte die Audio-URL angeben." : !r.prompt || !r.answer ? "Bitte mindestens eine Frage mit Antwort angeben." : null;
    case "Essay": return !extra.prompt ? "Bitte den Schreibauftrag angeben." : null;
    case "Grammar": return !r.prompt || !r.answer ? "Bitte mindestens eine Aufgabe mit Lösung angeben." : null;
    case "Translation":
      if (!extra.sourceLang || !extra.targetLang) return "Bitte Ausgangs- und Zielsprache wählen.";
      return !r.source || !r.target ? "Bitte mindestens einen Satz mit Übersetzung angeben." : null;
    case "Birkenbihl": return !r.text || !r.naturalTranslation ? "Bitte mindestens einen Satz mit Übersetzung angeben." : null;
  }
}
