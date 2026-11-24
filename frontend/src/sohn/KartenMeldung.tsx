import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import type { FeedbackNotice, FeedbackReason } from "../lib/types";
import { useAction } from "../lib/useAction";

/**
 * Die Reihenfolge der Gründe, in der das Kind sie sieht (B-623, Entscheidung 4). Ein Abbild über **alle** Gründe des
 * Vertrags (`satisfies Record`): kommt dort ein neunter dazu, wird der Typecheck rot, statt dass der Grund im Vater-Web
 * erscheint, das Kind ihn aber nicht melden kann.
 */
const REIHENFOLGE = {
  TooHard: 0, TooEasy: 1, AheadOfClass: 2, WrongTranslation: 3, TooMuchAsked: 4, TooStrict: 5, SeveralMeanings: 6, Liked: 7,
} as const satisfies Record<FeedbackReason, number>;

/** Die acht festen Gründe in der Reihenfolge des Kindes. */
export const GRUENDE: readonly FeedbackReason[] = (Object.keys(REIHENFOLGE) as FeedbackReason[])
  .sort((a, b) => REIHENFOLGE[a] - REIHENFOLGE[b]);

/** Wohin der Fokus nach einer Handlung des Kindes geht – nie von selbst, nur nach Öffnen, Abbrechen oder Absenden. */
type FokusZiel = "formular" | "knopf" | "dank" | null;

/**
 * Der Knopf „Rückmeldung" an einer Karte und sein Formular (B-629): Gründe zur Mehrfachauswahl, optional ein Text,
 * Absenden erst mit einem Grund oder Text. Nach dem Absenden bleibt die Karte stehen, der Knopf weicht dem Dank.
 *
 * Der Knopf bleibt stehen, solange das Formular offen ist (`aria-expanded`), und der Fokus folgt der Handlung: ins
 * Formular beim Öffnen, zurück auf den Knopf beim Abbrechen, auf den Dank nach dem Absenden – sonst fiele er jedes Mal
 * auf die Seite, und wer mit der Tastatur spielt, müsste neu durch die Karte tabben.
 *
 * Der Dialog darf das Spiel nicht stören (Risiko 1): kein Tastendruck aus dem Formular verlässt es (`onKeyDown` endet
 * hier – ein Vertrag für künftige Tastenkürzel der Arcade, heute hört dort niemand), und er bewegt nichts an der Runde: die
 * Meldung ist ein eigener Aufruf, keine Antwort. Wer sie einhängt, gibt ihr je Karte einen eigenen `key`, damit Auswahl
 * und Dank nicht auf die nächste Karte wandern.
 */
export function KartenMeldung({ planId, positionId, itemIndex }: {
  planId: number; positionId: number;
  /** Die Karte innerhalb der Übung; `null` meldet die ganze Übung. */
  itemIndex: number | null;
}) {
  const { t } = useTranslation();
  const action = useAction(useErrorMessage());
  const uid = useId();
  const [offen, setOffen] = useState(false);
  const [gewaehlt, setGewaehlt] = useState<FeedbackReason[]>([]);
  const [text, setText] = useState("");
  const [gesendet, setGesendet] = useState(false);
  const leer = gewaehlt.length === 0 && !text.trim();
  const knopfRef = useRef<HTMLButtonElement>(null);
  const formularRef = useRef<HTMLFieldSetElement>(null);
  const dankRef = useRef<HTMLDivElement>(null);
  const [fokus, setFokus] = useState<FokusZiel>(null);

  useEffect(() => {
    if (fokus === null) return;
    ({ formular: formularRef, knopf: knopfRef, dank: dankRef })[fokus].current?.focus();
    setFokus(null);
  }, [fokus]);

  const umschalten = (g: FeedbackReason) =>
    setGewaehlt((cur) => (cur.includes(g) ? cur.filter((x) => x !== g) : [...cur, g]));

  function oeffnen(auf: boolean) {
    setOffen(auf);
    setFokus(auf ? "formular" : "knopf");
  }

  async function absenden() {
    if (leer) return;
    const ok = await action.run(() => api.reportFeedback(planId, positionId, {
      reasons: gewaehlt, text: text.trim() || null, itemIndex,
    }));
    if (ok) { setGesendet(true); setOffen(false); setFokus("dank"); }
  }

  const formularId = `${uid}-formular`;
  // Ein Wert, keine innere Komponente: eine Funktionskomponente im Rumpf wäre bei jedem Tastendruck ein neuer Typ, und das
  // Textfeld verlöre mit dem Neu-Einhängen seinen Fokus.
  const formular = (
    <div className="rueckmeldung" id={formularId} onKeyDown={(e) => e.stopPropagation()}>
      <fieldset ref={formularRef} tabIndex={-1}>
        <legend>{t("feedback.title")}</legend>
        <div className="rueckmeldung-gruende" role="group" aria-label={t("feedback.reasons")}>
          {GRUENDE.map((g) => (
            <button key={g} type="button" className={`pill toggle-pill ${gewaehlt.includes(g) ? "lime" : ""}`}
              aria-pressed={gewaehlt.includes(g)} onClick={() => umschalten(g)}>
              {t(`feedback.reason.${g}`)}
            </button>
          ))}
        </div>
        <label htmlFor={`${uid}-text`} className="sub">{t("feedback.text")}</label>
        {/* Kein Lernstoff, sondern die Stimme des Kindes: hier darf diktiert und eingefügt werden. */}
        <textarea id={`${uid}-text`} data-einfuegen-erlaubt rows={2} maxLength={500}
          placeholder={t("feedback.textPlaceholder")} value={text} onChange={(e) => setText(e.target.value)} />
      </fieldset>
      <div className="lektion-fuss">
        <button type="button" className="btn lime small" disabled={action.busy || leer} onClick={absenden}>
          {t("feedback.send")}
        </button>
        <button type="button" className="btn ghost small" disabled={action.busy} onClick={() => oeffnen(false)}>
          {t("feedback.cancel")}
        </button>
      </div>
      <StatusBanner message={action.message} />
    </div>
  );

  return (
    <>
      {/* Die Region steht vor ihrem Dank im DOM (B-134) – sonst sagt ein Screenreader „angekommen“ nicht an. */}
      <div className="sub rueckmeldung-dank" role="status" aria-live="polite" ref={dankRef} tabIndex={-1}>
        {gesendet && t("feedback.thanks")}
      </div>
      {!gesendet && (
        <div className="lektion-fuss">
          <button type="button" className="btn ghost small" ref={knopfRef} aria-expanded={offen} aria-controls={formularId}
            onClick={() => oeffnen(!offen)}>
            💬 {t("feedback.open")}
          </button>
        </div>
      )}
      {!gesendet && offen && formular}
    </>
  );
}

/**
 * Der Hinweis nach „erledigt“ (B-629, B-623 Entscheidung 8): hat ein Betreuer eine Meldung erledigt, sieht das Kind das
 * einmal – mit dem Wort der Karte, sonst dem Titel der Übung – und quittiert es. Ohne Hinweis steht nichts da. Ein
 * fehlgeschlagenes Laden bleibt stumm: der Hinweis ist eine Zugabe, kein Teil der Mission.
 */
export function MeldungsHinweis({ childId }: { childId: number }) {
  const { t } = useTranslation();
  const action = useAction(useErrorMessage());
  const [hinweise, setHinweise] = useState<FeedbackNotice[]>([]);

  useEffect(() => {
    let aktiv = true;
    api.feedbackNotices(childId).then((n) => { if (aktiv) setHinweise(n); }, () => {});
    return () => { aktiv = false; };
  }, [childId]);

  const erster = hinweise[0];
  async function gesehen() {
    if (!erster) return;
    if (await action.run(() => api.ackFeedbackNotice(childId, erster.id)))
      setHinweise((cur) => cur.filter((h) => h.id !== erster.id));
  }
  // Die Region steht immer da (B-134): ein Hinweis, der erst mit seiner Region entsteht, wird nicht angesagt.
  return (
    <div role="status" aria-live="polite">
      {erster && (
        <div className="card meldungs-hinweis">
          <p style={{ margin: 0 }}>
            {erster.cue ? t("feedback.notice", { cue: erster.cue }) : t("feedback.noticeExercise", { title: erster.exerciseTitle })}
          </p>
          {/* B-769 Entscheidung 2: nach „berechtigt“ steht da, was nachgerechnet wurde und was nicht (B-627 Entscheidung 7). */}
          {erster.outcome === "Justified" && <p className="sub" style={{ margin: "6px 0 0" }}>{t("feedback.noticeJustified")}</p>}
          <div className="lektion-fuss">
            <button type="button" className="btn lime small" disabled={action.busy} onClick={gesehen}>{t("feedback.noticeOk")}</button>
          </div>
          <StatusBanner message={action.message} />
        </div>
      )}
    </div>
  );
}
