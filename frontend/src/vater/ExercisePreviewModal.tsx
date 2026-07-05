import { useCallback, useEffect, useRef, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { LetterBoxes } from "../components/LetterBoxes";
import { AudioButton } from "../components/AudioButton";
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
  // Container des Dialogs – für Fokus-Verwaltung (Fokus-Falle, Wiederherstellen beim Schließen).
  const dialogRef = useRef<HTMLDivElement | null>(null);
  // Neueste onClose-Referenz im Ref halten: Der Aufrufer übergibt onClose inline (neue Funktion pro Render);
  // ohne das Ref würde die Fokus-Falle bei jedem Eltern-Re-Render neu aufgesetzt und der Fokus zurückgerissen.
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  const load = useCallback(() => {
    setData(null); setAnswers({}); setRevealed(new Set()); setResult(null); setError(null);
    api.previewExercise(exerciseId, stage)
      .then(setData)
      .catch((e) => setError(errorMessage(e)));
  }, [exerciseId, stage]);

  useEffect(load, [load]);

  // Fokus-Verwaltung wie bei einem echten Dialog: Fokus beim Öffnen in den Dialog holen, Tab darin
  // gefangen halten (Fokus-Falle), Escape schließt, und beim Schließen den vorherigen Fokus wiederherstellen.
  useEffect(() => {
    const dialog = dialogRef.current;
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const focusables = () =>
      dialog
        ? Array.from(
            dialog.querySelectorAll<HTMLElement>(
              'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
            ),
          )
        : [];

    // Fokus initial in den Dialog holen (erstes fokussierbares Element, sonst der Container selbst).
    (focusables()[0] ?? dialog)?.focus();

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") { onCloseRef.current(); return; }
      if (e.key !== "Tab" || !dialog) return;
      const items = focusables();
      if (items.length === 0) { e.preventDefault(); dialog.focus(); return; }
      const first = items[0];
      const last = items[items.length - 1];
      const active = document.activeElement;
      if (e.shiftKey && (active === first || active === dialog)) {
        e.preventDefault(); last.focus();
      } else if (!e.shiftKey && active === last) {
        e.preventDefault(); first.focus();
      }
    };

    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      previouslyFocused?.focus?.();
    };
    // Nur beim Öffnen/Schließen – nicht bei jedem Eltern-Re-Render (siehe onCloseRef).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
    <div ref={dialogRef} tabIndex={-1} style={backdrop} role="dialog" aria-modal="true" aria-label={`Testmodus: ${title}`} onMouseDown={onClose}>
      <div className="card" style={sheet} onMouseDown={(e) => e.stopPropagation()}>
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
            {isCloze && <div className="card" style={{ background: "var(--surface-2, transparent)" }}><b>{data.items[0].prompt}</b></div>}
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
      </div>
    </div>
  );
}

const backdrop: React.CSSProperties = {
  position: "fixed", inset: 0, background: "rgba(0,0,0,.45)", zIndex: 1000,
  display: "flex", alignItems: "flex-start", justifyContent: "center", padding: "5vh 16px", overflowY: "auto",
  overscrollBehavior: "contain",
};
const sheet: React.CSSProperties = {
  width: "100%", maxWidth: 620, display: "flex", flexDirection: "column", gap: 14,
};
const testBanner: React.CSSProperties = {
  background: "rgba(37,99,235,.10)", border: "1px solid rgba(37,99,235,.35)",
};
