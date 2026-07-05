import { useCallback, useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import type { ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult } from "../lib/types";

/**
 * Testmodus („Ausprobieren"): Der Vater spielt eine einzelne Übung selbst durch – genau wie das Kind, aber
 * nebenwirkungsfrei (keine Punkte, kein Fortschritt). Bewertet wird server-autoritativ über den Preview-Endpunkt,
 * d. h. mit derselben Prüf-Logik wie im echten Test. So kann er die Übung verifizieren, bevor er sie zuweist.
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

  const load = useCallback(() => {
    setData(null); setAnswers({}); setRevealed(new Set()); setResult(null); setError(null);
    api.previewExercise(exerciseId)
      .then(setData)
      .catch((e) => setError(errorMessage(e)));
  }, [exerciseId]);

  useEffect(load, [load]);

  // Schließen per Escape (kleines Komfort-Detail wie bei einem echten Dialog).
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

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
      setResult(await api.checkPreviewExercise(exerciseId, payload));
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  // Lückentext: alle Items teilen denselben Trägertext – einmal oben zeigen, dann pro Lücke ein Feld.
  const isCloze = !!data && data.items.some((it) => it.gapIndex != null);

  return (
    <div style={backdrop} role="dialog" aria-modal="true" aria-label={`Testmodus: ${title}`} onMouseDown={onClose}>
      <div className="card" style={sheet} onMouseDown={(e) => e.stopPropagation()}>
        <div className="row" style={{ alignItems: "center", gap: 8 }}>
          <h3 style={{ margin: 0 }}>🧪 Ausprobieren · {title}</h3>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }} onClick={onClose} aria-label="Schließen">×</button>
        </div>
        <div className="banner" style={testBanner}>
          Testmodus – rein zum Ausprobieren. Keine Punkte, kein Fortschritt, das Kind bekommt davon nichts mit.
        </div>

        {error && <div className="banner err">{error}</div>}

        {!data && !error && <div className="loading">Lade…</div>}

        {data && !result && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {isCloze && <div className="card" style={{ background: "var(--surface-2, transparent)" }}><b>{data.items[0].prompt}</b></div>}
            <p className="muted" style={{ margin: 0 }}>
              {data.typed ? "Tippe deine Antwort – bewertet wird wie beim Kind." : "Überlege, dann aufdecken und ehrlich bewerten."}
            </p>

            {data.items.map((it) => {
              const a = answers[it.itemIndex];
              return (
                <div className="card" key={it.itemIndex}>
                  <div className="row" style={{ alignItems: "center", gap: 8 }}>
                    <b>{isCloze ? `Lücke ${it.gapIndex}` : it.prompt}</b>
                    {it.hint && <span className="muted" style={{ fontSize: 13 }}>💡 {it.hint}</span>}
                  </div>

                  {data.typed ? (
                    <input
                      style={{ marginTop: 8, width: "100%" }}
                      placeholder={it.answerLength ? `${it.answerLength} Buchstaben` : "Antwort…"}
                      value={a?.givenAnswer ?? ""}
                      onChange={(e) => setText(it.itemIndex, e.target.value)}
                    />
                  ) : (
                    <div style={{ marginTop: 8 }}>
                      {revealed.has(it.itemIndex)
                        ? <div style={{ color: "var(--accent, #2563eb)", fontWeight: 700, marginBottom: 6 }}>→ {it.reveal ?? "(aufgedeckt)"}</div>
                        : <button type="button" className="btn ghost small" style={{ width: "auto" }} onClick={() => setRevealed((r) => new Set(r).add(it.itemIndex))}>Aufdecken</button>}
                      {revealed.has(it.itemIndex) && (
                        <div className="row" style={{ gap: 6 }}>
                          <button type="button" className={`btn ghost small ${a?.wasKnown === false ? "err" : ""}`} style={{ width: "auto" }} onClick={() => setKnown(it.itemIndex, false)}>Nicht gewusst</button>
                          <button type="button" className={`btn ghost small ${a?.wasKnown === true ? "ok" : ""}`} style={{ width: "auto" }} onClick={() => setKnown(it.itemIndex, true)}>Gewusst</button>
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
      </div>
    </div>
  );
}

const backdrop: React.CSSProperties = {
  position: "fixed", inset: 0, background: "rgba(0,0,0,.45)", zIndex: 1000,
  display: "flex", alignItems: "flex-start", justifyContent: "center", padding: "5vh 16px", overflowY: "auto",
};
const sheet: React.CSSProperties = {
  width: "100%", maxWidth: 620, display: "flex", flexDirection: "column", gap: 14,
};
const testBanner: React.CSSProperties = {
  background: "rgba(37,99,235,.10)", border: "1px solid rgba(37,99,235,.35)",
};
