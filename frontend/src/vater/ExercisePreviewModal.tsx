import { useCallback, useEffect, useState } from "react";
import { ApiError, api, errorMessage } from "../lib/api";
import { LetterBoxes } from "../components/LetterBoxes";
import { AudioButton } from "../components/AudioButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { Modal } from "../components/Modal";
import type { ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult } from "../lib/types";

/**
 * Testmodus („Ausprobieren"): Der Vater spielt eine einzelne Übung selbst durch – genau wie das Kind, aber
 * nebenwirkungsfrei (keine Punkte, kein Fortschritt). Bewertet wird server-autoritativ über den Preview-Endpunkt,
 * d. h. mit derselben Prüf-Logik wie im echten Test. Über den Stufen-Umschalter lässt sich jede Abfrageform
 * (Selbsteinschätzung, Multiple-Choice, Buchstabenkästchen, Freitext, Hören → tippen) durchprobieren, exakt so
 * gerendert wie in der Sohn-App. So kann er die Übung verifizieren, bevor er sie zuweist.
 */
export function ExercisePreviewModal({ exerciseId, title, onClose }: {
  exerciseId: number; title: string; onClose: () => void;
}) {
  const [data, setData] = useState<ExercisePreviewData | null>(null);
  const [answers, setAnswers] = useState<Record<number, ExercisePreviewAnswer>>({});
  const [revealed, setRevealed] = useState<Set<number>>(new Set());
  const [result, setResult] = useState<ExercisePreviewResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // Vom Vater gewählte Abfrageform (undefined = Übungs-Standard); steuert Neuladen der Vorschau.
  const [stage, setStage] = useState<number | undefined>(undefined);

  const load = useCallback(() => {
    setData(null); setAnswers({}); setRevealed(new Set()); setResult(null); setError(null);
    api.previewExercise(exerciseId, stage)
      .then(setData)
      // Manche Typen haben keine einzeln prüfbaren Aufgaben (ein Aufsatz ist ein Schreibauftrag, kein
      // Frage-Antwort-Paar). Der Server sagt das mit `no_checkable_content`; die technische englische
      // Meldung würde hier wie ein Defekt aussehen, obwohl es die Natur des Typs ist. Der Satz ist
      // *hier* länger als die zentrale Fassung, weil er die Folge nennt (zuweisen ja, durchspielen nein).
      //
      // Der verwandte Fall „Übung noch nicht gefüllt" (`exercise_empty`) braucht keinen Zweig: er ist
      // nicht vorschau-spezifisch und kommt deutsch aus `errorMessage` (GERMAN_PROBLEM_TEXT in lib/api).
      .catch((e) => setError(e instanceof ApiError && e.code === "no_checkable_content"
        ? "Diese Übung hat keine einzeln prüfbaren Aufgaben – ein Schreibauftrag lässt sich nicht "
          + "automatisch bewerten. Du kannst sie zuweisen, aber nicht durchspielen."
        : errorMessage(e)));
  }, [exerciseId, stage]);

  useEffect(load, [load]);

  function setText(i: number, val: string) {
    setAnswers((a) => ({ ...a, [i]: { itemIndex: i, givenAnswer: val } }));
  }
  function setKnown(i: number, known: boolean) {
    setAnswers((a) => ({ ...a, [i]: { itemIndex: i, wasKnown: known } }));
    setRevealed((r) => new Set(r).add(i));
  }

  async function submit() {
    if (!data) return;
    setBusy(true); setError(null);
    try {
      const payload: ExercisePreviewAnswer[] = data.items.map(
        (it) => answers[it.itemIndex] ?? { itemIndex: it.itemIndex, givenAnswer: data.typed ? "" : null, wasKnown: data.typed ? null : false },
      );
      // Dieselbe Stufe wie beim Laden mitschicken, damit „getippt" server- und clientseitig übereinstimmt.
      setResult(await api.checkPreviewExercise(exerciseId, payload, data.stage));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  // Lückentext: alle Items teilen denselben Trägertext – einmal oben zeigen, dann pro Lücke ein Feld.
  const isCloze = !!data && data.items.some((it) => it.gapIndex != null);

  return (
    <Modal label={`Testmodus: ${title}`} onClose={onClose}>
        <div className="row" style={{ alignItems: "center", gap: 8 }}>
          <h3 style={{ margin: 0 }}>🧪 Ausprobieren · {title}</h3>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }} onClick={onClose} aria-label="Schließen">×</button>
        </div>
        <div className="banner" style={testBanner}>
          Testmodus – rein zum Ausprobieren. Keine Punkte, kein Fortschritt, das Kind bekommt davon nichts mit.
        </div>

        {/* Stufen-Umschalter: jede Abfrageform durchprobieren (nur bei Typen mit mehreren Stufen). */}
        {data && data.stages.length > 1 && (
          <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13 }}>
            <span className="muted">Abfrageform</span>
            <select aria-label="Abfrageform" value={data.stage}
              onChange={(e) => setStage(Number(e.target.value))}>
              {data.stages.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
            </select>
          </label>
        )}

        {error && <div className="banner err">{error}</div>}

        {!data && !error && <div className="loading">Lade…</div>}

        {data && !result && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {/* Der Trägertext einmal oben – ohne die rohe Vorlagensyntax, der Vater soll sehen, was das
                Kind sieht. Ohne `gapIndex` bleiben alle Lücken neutral; welche gefragt ist, sagt die Zeile
                darunter je Aufgabe. */}
            {isCloze && (
              <div className="card" style={{ background: "var(--surface-2, transparent)" }}>
                <ClozePrompt text={data.items[0].prompt} className="test-prompt" />
              </div>
            )}
            <p className="muted" style={{ margin: 0 }}>
              {data.typed ? "Tippe deine Antwort – bewertet wird wie beim Kind." : "Überlege, dann aufdecken und ehrlich bewerten."}
            </p>

            {data.items.map((it) => {
              const a = answers[it.itemIndex];
              return (
                <div className="card" key={it.itemIndex}>
                  <div className="row" style={{ alignItems: "center", gap: 8 }}>
                    {/* Hör-Stufe: Wort vorlesen statt zeigen; sonst Prompt (bzw. Lücken-Nr. beim Lückentext). */}
                    {it.audioUrl
                      ? <AudioButton url={it.audioUrl} label="🔊 Vokabel anhören" />
                      : <b>{isCloze ? `Lücke ${it.gapIndex}` : it.prompt}</b>}
                    {it.hint && <span className="muted" style={{ fontSize: 13 }}>💡 {it.hint}</span>}
                  </div>

                  {data.typed && it.choices ? (
                    // Multiple-Choice: gewählte Option deutlich gefüllt (nicht nur Rahmen), damit die Auswahl sichtbar ist.
                    <div className="row" style={{ marginTop: 8, gap: 6, flexWrap: "wrap" }}>
                      {it.choices.map((c) => (
                        <button type="button" key={c}
                          className={`btn ${a?.givenAnswer === c ? "" : "ghost"} small`} style={{ width: "auto" }}
                          aria-pressed={a?.givenAnswer === c}
                          onClick={() => setText(it.itemIndex, c)}>{a?.givenAnswer === c ? "✓ " : ""}{c}</button>
                      ))}
                    </div>
                  ) : data.typed && it.answerLength ? (
                    // Buchstabenkästchen: dieselbe Komponente wie in der Sohn-App.
                    <div style={{ marginTop: 8 }}>
                      <LetterBoxes length={it.answerLength} value={a?.givenAnswer ?? ""}
                        onChange={(v) => setText(it.itemIndex, v)} onSubmit={submit} />
                    </div>
                  ) : data.typed ? (
                    <input
                      style={{ marginTop: 8, width: "100%" }}
                      aria-label="Antwort"
                      placeholder="Antwort…"
                      value={a?.givenAnswer ?? ""}
                      onChange={(e) => setText(it.itemIndex, e.target.value)}
                    />
                  ) : (
                    <div style={{ marginTop: 8 }}>
                      {revealed.has(it.itemIndex)
                        ? <div style={{ color: "var(--accent, #2563eb)", fontWeight: 700, marginBottom: 6 }}>→ {it.reveal ?? "(aufgedeckt)"}</div>
                        : <button type="button" className="btn ghost small" style={{ width: "auto" }} onClick={() => setRevealed((r) => new Set(r).add(it.itemIndex))}>Aufdecken</button>}
                      {revealed.has(it.itemIndex) && (
                        // Selbsteinschätzung: die geklickte Bewertung gefüllt hervorheben (bisher blieb sie unsichtbar).
                        <div className="row" style={{ gap: 6 }}>
                          <button type="button" className={`btn ${a?.wasKnown === false ? "" : "ghost"} small`} style={{ width: "auto" }}
                            aria-pressed={a?.wasKnown === false} onClick={() => setKnown(it.itemIndex, false)}>{a?.wasKnown === false ? "✓ " : ""}Nicht gewusst</button>
                          <button type="button" className={`btn ${a?.wasKnown === true ? "" : "ghost"} small`} style={{ width: "auto" }}
                            aria-pressed={a?.wasKnown === true} onClick={() => setKnown(it.itemIndex, true)}>{a?.wasKnown === true ? "✓ " : ""}Gewusst</button>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}

            <button type="button" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} onClick={submit} disabled={busy}>
              {busy ? "…" : "Auswerten"}
            </button>
          </div>
        )}

        {result && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            <div className="banner ok" style={{ fontSize: 16 }}>
              Ergebnis: <b>{result.scorePercent}%</b> · {result.correct} / {result.total} richtig
            </div>
            <div className="card" style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              {result.items.map((o) => (
                <div className="row" key={o.itemIndex} style={{ alignItems: "center", gap: 8, padding: "3px 0" }}>
                  <span>{o.wasCorrect ? "✅" : "❌"}</span>
                  <span>{o.prompt}</span>
                  <span className="muted" style={{ marginLeft: "auto" }}>
                    {o.givenAnswer ? `„${o.givenAnswer}" → ` : ""}<b>{o.expected}</b>
                  </span>
                </div>
              ))}
            </div>
            <div className="row" style={{ gap: 8 }}>
              <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={load}>Nochmal</button>
              <button type="button" className="btn" style={{ width: "auto" }} onClick={onClose}>Fertig</button>
            </div>
          </div>
        )}
    </Modal>
  );
}

const testBanner: React.CSSProperties = {
  background: "rgba(37,99,235,.10)", border: "1px solid rgba(37,99,235,.35)",
};
