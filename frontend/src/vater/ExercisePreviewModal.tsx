import { useCallback, useEffect, useState } from "react";
import { ApiError, api, errorMessage } from "../lib/api";
import { StatusBanner } from "../components/StatusBanner";
import { LetterBoxes } from "../components/LetterBoxes";
import { LetterTiles } from "../components/LetterTiles";
import { AudioButton } from "../components/AudioButton";
import { SpeakButton } from "../components/SpeakButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { Passage } from "../components/Passage";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import { Modal } from "../components/Modal";
import { CueBox } from "../components/CueBox";
import type { ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult } from "../lib/types";

/**
 * Testmodus („Ausprobieren"): Der Vater spielt eine einzelne Übung selbst durch – genau wie das Kind, aber
 * nebenwirkungsfrei (keine Punkte, kein Fortschritt). Bewertet wird server-autoritativ über den Preview-Endpunkt,
 * d. h. mit derselben Prüf-Logik wie im echten Test. Über den Stufen-Umschalter lässt sich jede Abfrageform
 * (Selbsteinschätzung, Multiple-Choice, Buchstabenkästchen, Freitext, Diktat) durchprobieren, exakt so
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
  // Lückentext: welche Aufgabe (Index in `data.items`) gerade dran ist – eine Lücke nach der anderen (B-486).
  const [cursor, setCursor] = useState(0);

  const load = useCallback(() => {
    setData(null); setAnswers({}); setRevealed(new Set()); setResult(null); setError(null); setCursor(0);
    api.previewExercise(exerciseId, stage)
      .then(setData)
      // Manche Übungen haben keine einzeln prüfbaren Aufgaben (ein Lesetext ohne Fragen). Der Server sagt
      // das mit `no_checkable_content`; die technische englische Meldung würde hier wie ein Defekt aussehen.
      // Der Satz ist *hier* länger als die zentrale Fassung, weil er die Folge nennt (durchspielen nein) – und
      // keinen Ausweg: Zuweisen hat eigene Sperren (B-496). Ein Aufsatz kommt nicht mehr hierher: er liefert eine
      // Ansicht (B-15, unten).
      //
      // Der verwandte Fall „Übung noch nicht gefüllt" (`exercise_empty`) braucht keinen Zweig: er ist
      // nicht vorschau-spezifisch und kommt deutsch aus `errorMessage` (GERMAN_PROBLEM_TEXT in lib/api).
      .catch((e) => setError(e instanceof ApiError && e.code === "no_checkable_content"
        ? "Diese Übung hat keine einzeln prüfbaren Aufgaben – durchspielen lässt sie sich darum nicht."
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

  /*
   * Lückentext: alle Items teilen denselben Trägertext. Er steht einmal oben, darunter aber nur die EINE
   * gefragte Lücke – wie beim Kind, das je Karte eine Lücke mit „?" sieht (B-486). Vorher stand je Lücke eine
   * Karte mit der ganzen Wortbank untereinander: auf dem Handy hieß das Scrollen zwischen Text und Auswahl und
   * Abzählen, welche der „…" die „Lücke 7" ist.
   */
  const isCloze = !!data && data.items.some((it) => it.gapIndex != null);
  const at = data ? Math.min(cursor, data.items.length - 1) : 0;
  const current = data?.items[at];
  const shownItems = !data ? [] : isCloze && current ? [current] : data.items;
  // Die Antworten liegen je Aufgabe; der Text adressiert die Lücke. Hier die Brücke dazwischen.
  const filledGaps: Record<number, string | null | undefined> = {};
  for (const it of data?.items ?? []) if (it.gapIndex != null) filledGaps[it.gapIndex] = answers[it.itemIndex]?.givenAnswer;
  function goToGap(gap: number) {
    const i = data?.items.findIndex((it) => it.gapIndex === gap) ?? -1;
    if (i >= 0) setCursor(i);
  }
  // Nach einer Wahl in der Wortbank weiter zur nächsten Lücke – ein Fehlgriff ist ein Tipp auf die Lücke im Text.
  function chooseCloze(i: number, val: string) {
    setText(i, val);
    if (data && at < data.items.length - 1) setCursor(at + 1);
  }
  // Mengen-Bewertung: alle Aufgaben teilen ihre Anweisung, und die Reihenfolge der Eingaben bedeutet nichts.
  const isSet = !!data && data.items.some((it) => it.anyOrder);
  // Die GEORDNETE Liste teilt ihre Anweisung genauso – nur zählt dort der Platz. Zwei Fälle, zwei Gründe, wie
  // bei `ListRule` in der Sohn-App: die Menge hängt am Feld (für jeden mengenweise bewerteten Typ richtig), der
  // geordnete Fall am Typ (nur bei einer Liste teilen sich die Zeilen ihren Text).
  const isOrderedList = !!data && !isSet && data.type === "List";
  // Beide Listenfälle: Anweisung einmal oben, die Zeilen tragen ihre Nummer.
  const listShared = isSet || isOrderedList;

  /*
   * Die Aufnahme einer Hörverstehen-Übung gehört der Übung: Sie hängt an jedem Item, ist aber überall
   * dieselbe. Erkennbar genau daran – eine Vokabel-Hörstufe trägt je Item eine *eigene* Aussprache, und
   * dort bleibt der Knopf an der Zeile. Kein Verzweigen auf den Übungstyp, nur auf die Daten.
   */
  const audioOfExercise = data?.items.length
    && data.items.every((it) => it.audioUrl && it.audioUrl === data.items[0].audioUrl)
    && data.items.some((it) => it.prompt)
    ? data.items[0].audioUrl : null;

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

        {/* Live-Region, die schon beim Öffnen steht: der Fokus sitzt dann auf „×", ein nachgeladener Fehler wird sonst nicht angesagt (B-444). */}
        <StatusBanner message={error ? { ok: false, text: error } : null} />

        {!data && !error && <div className="loading">Lade…</div>}

        {/* Nur ansehen, nicht bewerten (B-15): Die Ansicht hängt am Feld `gradable`, nicht am Typ. Heute liefert
            nur der Aufsatz eine (Schreibauftrag, Länge, Bewertungskriterien), der Text darunter nennt darum keinen. */}
        {data && data.gradable === false && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {data.items.map((it) => (
              <div className="card" key={it.itemIndex}>
                <div className="test-prompt">{it.prompt}</div>
                {it.hint && <p className="muted" style={{ margin: "6px 0 0" }}>{it.hint}</p>}
              </div>
            ))}
            <p className="muted" style={{ margin: 0 }}>
              Diese Übung lässt sich nicht automatisch bewerten – durchspielen lässt sie sich darum nicht.
            </p>
            <button type="button" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} onClick={onClose}>Fertig</button>
          </div>
        )}

        {data && data.gradable !== false && !result && (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {/* Lesetext bzw. Anweisung der Übung – dasselbe Material, das die Karte des Kindes trägt.
                Eine Vorschau, die weniger zeigt, ist eine Beruhigung statt einer Prüfung. Steht oben, wie
                beim Kind. */}
            <Passage text={data.items[0]?.passage} />
            {/* Der Trägertext einmal oben – ohne die rohe Vorlagensyntax, der Vater soll sehen, was das
                Kind sieht: die gefragte Lücke mit „?". Gewählte Antworten stehen in ihrer Lücke, ein Tipp
                darauf springt zurück. */}
            {isCloze && current && (
              <div className="card" style={{ background: "var(--surface-2, transparent)" }}>
                <ClozePrompt text={data.items[0]?.prompt} gapIndex={current.gapIndex} className="test-prompt"
                  filled={filledGaps} onGap={goToGap} fitAsked />
              </div>
            )}
            {/* Die Aufnahme gehört der Übung, nicht der einzelnen Frage: einmal oben, nicht je Zeile –
                sonst mounten fünf Fragen fünf <audio> auf dieselbe Quelle und spielen beim Öffnen des
                Dialogs alle zugleich los. Nicht automatisch: der Vater öffnet den Dialog zum Prüfen. */}
            {audioOfExercise && (
              <AudioButton url={audioOfExercise} autoPlay={false} withControls label="Aufnahme der Übung" />
            )}
            {/* Eine Liste teilt ihre Anweisung über alle Einträge: sie steht einmal oben, weil die Vorschau alle
                Aufgaben zugleich zeigt – sonst stünde 16-mal derselbe fette Satz, also genau die
                Ununterscheidbarkeit, die es beim Kind nicht mehr gibt. Die Regel darunter nennt beide Fälle
                (B-77/R5, E6): ungeordnet bewertet der Server als Menge, geordnet positionsgenau. Fehlte der
                geordnete Zweig, sagte die Vorschau dem Vater etwas anderes als die Klausur dem Kind. */}
            {listShared && (
              <div className="card" style={{ background: "var(--surface-2, transparent)" }}>
                <div className="test-prompt">{data.items[0]?.prompt}</div>
                <p className="muted" style={{ margin: "4px 0 0" }}>
                  {isSet
                    ? "Reihenfolge egal – jede Antwort zählt einmal, eine Wiederholung zählt nicht."
                    : "Die Reihenfolge zählt – jedes Feld fragt seinen Eintrag."}
                </p>
              </div>
            )}
            <p className="muted" style={{ margin: 0 }}>
              {data.typed ? "Tippe deine Antwort – bewertet wird wie beim Kind." : "Überlege, dann aufdecken und ehrlich bewerten."}
            </p>

            {/* B-524: Der Wortkasten ist auf jeder Karte derselbe – hier einmal über allen Lücken statt je Zeile. */}
            <CueBox words={shownItems[0]?.cueBox} label="Wortkasten" />

            {shownItems.map((it) => {
              const a = answers[it.itemIndex];
              return (
                <div className="card" key={it.itemIndex}>
                  <div className="row" style={{ alignItems: "center", gap: 8 }}>
                    {/* Die Frage steht immer da, wo der Server eine schickt. Sie war hier eine Zeit lang
                        unsichtbar, weil das Audio sie im Entweder-oder verdrängte – bei einem Hörverstehen
                        trägt jedes Item die Aufnahme, also lief der Frage-Zweig nie. Verschwiegen wird die
                        Frage nur, wo der Server sie weglässt (Vokabel-Hörstufe). */}
                    {it.audioUrl && !it.prompt && <AudioButton url={it.audioUrl} label="🔊 Anhören" />}
                    {/* B-597: das Diktat ohne Aufnahme liest das Gerät vor – der Vater hört, was das Kind hört. Das deutsche
                        Wort daneben sieht nur er, zur Orientierung; das Kind mit Stimme sieht es nicht. */}
                    {it.dictationSpokenAnswer && it.dictationLang && (
                      <SpeakButton text={it.dictationSpokenAnswer} lang={it.dictationLang} label="🔊 Diktat anhören" />
                    )}
                    {/* Bei einer Liste steht die Anweisung oben, hier bleibt die Nummer: bei der Menge die des
                        Antwortfeldes (die Reihenfolge bedeutet nichts), bei der geordneten Liste die des
                        Eintrags – dieselbe Adressierung, die das Kind auf der Karte liest. */}
                    {isSet ? <b>Antwort {it.itemIndex + 1}</b>
                      : isOrderedList ? <b>Eintrag {it.itemIndex + 1}</b>
                      : (it.prompt || isCloze) && <b>{isCloze ? `Lücke ${it.gapIndex}` : it.prompt}</b>}
                    {it.hint && <span className="muted" style={{ fontSize: 13 }}>💡 {it.hint}</span>}
                  </div>
                  {/* Dieselbe Wort-für-Wort-Ansicht wie beim Kind: wer die Dekodierung pflegt, muss sie so
                      prüfen können, wie sie ankommt (B-78). */}
                  <BirkenbihlDecoding decoding={it.decoding} />

                  {data.typed && it.choices ? (
                    // Multiple-Choice: gewählte Option deutlich gefüllt (nicht nur Rahmen), damit die Auswahl sichtbar ist.
                    <div className="row" style={{ marginTop: 8, gap: 6, flexWrap: "wrap" }}
                      role="group" aria-label="Antwortmöglichkeiten">
                      {/* Index im Key: derselbe Text darf zweimal in der Auswahl stehen, und `aria-pressed`
                          markierte sonst beide Knöpfe als gewählt. */}
                      {it.choices.map((c, i) => (
                        <button type="button" key={`${i}-${c}`}
                          className={`btn ${a?.givenAnswer === c ? "" : "ghost"} small`} style={{ width: "auto" }}
                          aria-pressed={a?.givenAnswer === c}
                          onClick={() => (isCloze ? chooseCloze(it.itemIndex, c) : setText(it.itemIndex, c))}>{a?.givenAnswer === c ? "✓ " : ""}{c}</button>
                      ))}
                    </div>
                  ) : data.typed && it.letterTiles ? (
                    // Buchstaben ordnen (B-05): dieselbe Komponente wie in der Sohn-App.
                    <div style={{ marginTop: 8 }}>
                      <LetterTiles tiles={it.letterTiles} value={a?.givenAnswer ?? ""}
                        onChange={(v) => setText(it.itemIndex, v)} onSubmit={submit} pattern={it.answerPattern} />
                    </div>
                  ) : data.typed && it.answerLength ? (
                    // Buchstabenkästchen: dieselbe Komponente wie in der Sohn-App.
                    <div style={{ marginTop: 8 }}>
                      <LetterBoxes length={it.answerLength} value={a?.givenAnswer ?? ""}
                        onChange={(v) => setText(it.itemIndex, v)} onSubmit={submit} pattern={it.answerPattern ?? undefined} />
                    </div>
                  ) : data.typed ? (
                    <input
                      style={{ marginTop: 8, width: "100%" }}
                      // Bei einer Liste ist die Nummer die einzige Unterscheidung der Felder – sie steht sichtbar
                      // darüber und muss darum auch im Namen des Feldes stehen, sonst heißen alle sechzehn gleich.
                      aria-label={isSet ? `Antwort ${it.itemIndex + 1}`
                        : isOrderedList ? `Eintrag ${it.itemIndex + 1}` : "Antwort"}
                      placeholder="Antwort…"
                      value={a?.givenAnswer ?? ""}
                      onChange={(e) => setText(it.itemIndex, e.target.value)}
                    />
                  ) : (
                    <div style={{ marginTop: 8 }}>
                      {revealed.has(it.itemIndex)
                        ? <>
                            <div style={{ color: "var(--accent, #2563eb)", fontWeight: 700, marginBottom: 6 }}>→ {it.reveal ?? "(aufgedeckt)"}</div>
                            {/* Der Autor sieht hier, was beim Kind als „auch richtig" ankommt – sonst prüft
                                er eine Karte, die es so nie zu sehen bekommt. */}
                            <RevealAlternatives alternatives={it.revealAlternatives} />
                          </>
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

            {isCloze && data.items.length > 1 && (
              <div className="row" style={{ alignItems: "center", gap: 8 }}>
                <button type="button" className="btn ghost small" style={{ width: "auto" }}
                  disabled={at === 0} onClick={() => setCursor(at - 1)}>← Zurück</button>
                <span className="muted" aria-live="polite">Lücke {at + 1} von {data.items.length}</span>
                <button type="button" className="btn ghost small" style={{ width: "auto", marginLeft: "auto" }}
                  disabled={at === data.items.length - 1} onClick={() => setCursor(at + 1)}>Weiter →</button>
              </div>
            )}

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
              {/* Auch im ERGEBNIS teilt die Liste ihre Anweisung – ohne diese Zeile stünde sie sechzehnmal, also
                  wieder das Bild, das B-77 als Defekt beschreibt. Anders als in der Sohn-App wird hier nicht
                  geraten: die Vorschau hält die Aufgaben noch und weiß, dass es eine Liste ist. */}
              {listShared && <p className="muted" style={{ margin: "0 0 2px" }}>{data.items[0]?.prompt}</p>}
              {result.items.map((o) => (
                <div className="row" key={o.itemIndex} style={{ alignItems: "center", gap: 8, padding: "3px 0" }}>
                  {/* B-524: dieselbe Kategorie wie im Testergebnis des Kindes – das Wort war da, die Schreibweise nicht. */}
                  <span aria-hidden="true">{o.wasCorrect ? "✅" : o.misspelled && !isSet ? "✏️" : "❌"}</span>
                  {/* Bei der Menge trägt das Zeichen eine andere Bedeutung als sonst: nicht „falsch geantwortet",
                      sondern „nicht genannt" – und das ist die Lehre der Übung (B-77/E2). */}
                  <span className="sr-only">
                    {isSet ? (o.wasCorrect ? "Genannt" : "Nicht genannt") : (o.wasCorrect ? "Richtig" : o.misspelled ? "Falsch geschrieben" : "Falsch")}
                  </span>
                  {!isSet && <span>{isOrderedList ? `Eintrag ${o.itemIndex + 1}` : o.prompt}</span>}
                  <span className="muted" style={{ marginLeft: "auto" }}>
                    {o.misspelled && !isSet && "falsch geschrieben · "}{o.givenAnswer ? `„${o.givenAnswer}" → ` : ""}<b>{o.expected}</b>
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
