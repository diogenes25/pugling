import { useEffect, useRef, useState, type Ref } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError, api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import { useAction } from "../lib/useAction";
import { useSohn } from "./SohnApp";
import { LearningDad } from "../components/LearningDad";
import { TaskAnnouncer } from "../components/ClozePrompt";
import { ErgebnisZeile } from "./ErgebnisZeile";
import { KartenMeldung } from "./KartenMeldung";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { aufgabenform, istDiktat } from "./aufgabenform";
import { useVoiceFor } from "../components/SpeakButton";
import { Antwortflaeche, Aufgabe, LektionsRahmen } from "./Lektion";
import { useBuchbezug } from "./buchbezug";
import type { AnswerDto, TestItem, TestSubmitResponse } from "../lib/types";

// Vokabel-Teststufen (numerisch, serverseitig erzwungen): 1 Zeigen … 7 Ordnen.
const STAGE_KEY = {
  1: "test.stage1", 2: "test.stage2", 3: "test.stage3", 4: "test.stage4", 5: "test.stage5", 6: "test.stage6", 7: "test.stage7",
} as const;
const isKnownStage = (s: number): s is keyof typeof STAGE_KEY => s in STAGE_KEY;

/**
 * Name der Leitner-Stufe in der Arcade-Sprache; eine unbekannte Stufe zeigt ihre Nummer. Die Tabelle gilt nur für
 * Vokabeln: Bei anderen Typen bedeutet dieselbe Zahl etwas anderes (B-524: Lückentext-Stufe 5 ist der Wortkasten,
 * nicht „Hören“). Mit Wortkasten heißt die Stufe darum nach ihm, sonst trägt sie nur ihre Nummer.
 */
function useStageLabel() {
  const { t } = useTranslation();
  return (s: number, type?: string | null, hasCueBox?: boolean) => {
    if (hasCueBox) return t("test.cueBox");
    if (type && type !== "Vocabulary") return t("test.stageN", { n: s });
    return isKnownStage(s) ? t(STAGE_KEY[s]) : t("test.stageN", { n: s });
  };
}

/**
 * Abschlusstest = Klausur: strikt server-getrieben. Der Client holt jede Frage einzeln (nextTest),
 * schickt die Antwort (answerTest, ohne Korrektheit zurück) und kann NICHT zurück. Erst der Abschluss
 * (submitTest) liefert die Auswertung – wie eine echte Klassenarbeit.
 */
export function SohnTest() {
  const { planId, refreshWallet, setStreak, skin, celebrate } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const buchbezug = useBuchbezug(planId, positionId);
  const nav = useNavigate();
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const stageLabel = useStageLabel();

  const [attemptId, setAttemptId] = useState<number | null>(null);
  const [total, setTotal] = useState(0);
  const [cursor, setCursor] = useState(0);
  const [item, setItem] = useState<TestItem | null>(null);
  // B-597: Ob das Gerät die aktuelle Karte diktieren kann – hier oben, weil die Form erst nach den frühen Ausstiegen entsteht.
  const diktatStimme = useVoiceFor(item?.dictationLang ?? "");
  // Der Name der Stufe, wie die letzte Karte ihn trug: das Ergebnis selbst kennt weder Typ noch Wortkasten (B-524).
  const [stageName, setStageName] = useState<string | null>(null);
  const [typedAnswer, setTypedAnswer] = useState("");
  const [revealed, setRevealed] = useState(false);
  const [result, setResult] = useState<TestSubmitResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  // Ein Plan, der nicht mehr dran ist (B-330), lässt sich nicht fortsetzen – „Weiter versuchen“ holte dieselbe 403.
  const [planStopped, setPlanStopped] = useState(false);
  // Ref-Gate gegen den Doppelklick (B-49). Der Fehler speist die `error`-Vollbild-Anzeige; `action.message`
  // wird hier nicht gerendert.
  const action = useAction(errorMessage);
  // Hat das Kind die Klausur verlassen (seit B-350 auch aus dem Wartebild), darf ein später antwortender Aufruf es nicht
  // zurückholen und nicht anderswo feiern. Beim Einhängen zurückgesetzt, weil StrictMode im Dev einmal aus- und wieder
  // einhängt.
  const gone = useRef(false);
  useEffect(() => { gone.current = false; return () => { gone.current = true; }; }, []);
  // Den Namen der Stufe mitschreiben, solange eine Karte da ist – das Ergebnis danach kennt nur die Nummer.
  useEffect(() => {
    if (item) setStageName(stageLabel(item.stage, item.type, !!item.cueBox));
  }, [item]); // nur die Karte: stageLabel ist je Render eine neue Funktion, aber immer dieselbe Regel

  /*
   * B-541 (Regel aus B-531): nimmt eine Handlung des Kindes den fokussierten Knopf mit – Aufdecken, Antwort, „Weiter
   * versuchen", „Nochmal versuchen" –, landet der Fokus dort, wo es als Nächstes handelt: erstes Bedienbares der
   * Antwortzone, sonst die Überschrift des Ergebnisses oder die Fehlermeldung. Die Absicht wird erst verbraucht, wenn
   * ein Ziel da ist: die neue Frage steht schon, während `action.busy` ihre Optionen noch sperrt – ein Lauf in diesem
   * Augenblick fände nichts und ließe den Fokus auf `body`. Der erste Aufbau setzt die Absicht nicht (B-443).
   */
  const focusIntent = useRef(false);
  const answerRef = useRef<HTMLDivElement>(null);
  const errorRef = useRef<HTMLDivElement>(null);
  const resultTitleRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!focusIntent.current) return;
    const target = error ? errorRef.current
      : result ? resultTitleRef.current
      : item && !action.busy ? answerRef.current?.querySelector<HTMLElement>("input, button:not([disabled])")
      : null;
    if (!target) return;
    target.focus();
    focusIntent.current = false;
  }, [item, cursor, revealed, error, result, action.busy]);

  async function finish(id: number) {
    if (!planId) return;
    const res = await api.submitTest(planId, positionId, id);
    // Schon verlassen: kein Ergebnisbild und keine Feier anderswo, aber die Kopfzeile lebt weiter und zeigt, was die
    // Abgabe eingebracht hat (Münzen und Serie, B-356).
    if (gone.current) { refreshWallet(); setStreak((await api.overview(planId)).currentStreak); return; }
    setResult(res);
    setItem(null);
    celebrate(res.passed ? "big" : "small", res.passed ? "🎉" : "💪",
      res.passed ? t("test.victory") : undefined, res.passed ? `${res.scorePercent}%` : undefined);
    setStreak((await api.overview(planId)).currentStreak);
    refreshWallet();
  }

  async function start() {
    if (!planId) return;
    setError(null); setResult(null); setItem(null); setTypedAnswer(""); setRevealed(false); setCursor(0);
    // Ein Neustart aus dem Ergebnis trägt sonst noch den abgegebenen Versuch – scheitert er, böte „Weiter versuchen“ diesen an.
    setAttemptId(null); setPlanStopped(false);
    try {
      const a = await api.startTest(planId, positionId);
      setAttemptId(a.attemptId); setTotal(a.totalItems);
      const first = await api.nextTest(planId, positionId, a.attemptId);
      if (first.done) await finish(a.attemptId);
      // `?? null`: ein nullable Feld darf im Vertrag auch fehlen, der Zustand kennt nur „keine Aufgabe".
      else { setItem(first.item ?? null); setCursor(first.cursor); }
    } catch (e) {
      // 404 beim Start: Plan oder Position gibt es nicht mehr (Reload nach dem Löschen, B-326). Die Basis korrigiert die
      // Wahl; die Serverzeile („Study plan not found.“) sagte dem Kind nur, dass etwas kaputt ist.
      if (e instanceof ApiError && e.status === 404) { if (!gone.current) nav("/sohn", { replace: true }); return; }
      // Scheitert erst die erste Frage, steht die Versuchs-Id schon – „Weiter versuchen“ darf dann nur kommen, wenn der Plan
      // noch dran ist (B-335), sonst holte es nur dieselbe 403.
      setError(errorMessage(e)); setPlanStopped(isPlanInactive(e));
    }
  }

  /**
   * Merkt, für welche Position bereits ein Versuch gestartet wurde. Nötig, weil ein Effekt
   * <b>zweimal laufen kann</b> (React-StrictMode im Dev, generell bei einem Remount) – ein
   * `alive`-Flag verhindert dann zwar das Setzen von State, aber nicht den bereits abgeschickten
   * POST. Ergebnis wären zwei Klausur-Versuche: der zweite gewinnt die Anzeige, die erste Antwort
   * landet aber noch auf dem ersten – der Test bliebe eine Frage vor dem Ende hängen.
   */
  const startedFor = useRef<string | null>(null);

  useEffect(() => {
    if (!planId || !positionId) { nav("/sohn"); return; }
    const key = `${planId}:${positionId}`;
    if (startedFor.current === key) return;
    startedFor.current = key;
    void start();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [planId, positionId, nav]);

  // Laufenden Versuch nach einem transienten Fehler fortsetzen: aktuelle Cursor-Frage erneut holen.
  async function resume() {
    focusIntent.current = true;
    if (!planId || attemptId === null) { await start(); return; }
    // Die alte Frage weg, bevor die neue geholt wird: meist ist sie schon beantwortet (nur `nextTest` scheiterte), und
    // stünde sie bis zur Antwort wieder da, sagte die Region sie als neue Aufgabe an und die Karte wäre anklickbar (B-339).
    setError(null); setItem(null);
    try {
      const nx = await api.nextTest(planId, positionId, attemptId);
      if (nx.done) { await finish(attemptId); return; }
      setItem(nx.item ?? null); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
    } catch (e) { setError(errorMessage(e)); setPlanStopped(isPlanInactive(e)); }
  }

  // Antwort abgeben (server-geführt: der Server adressiert stets die aktuelle Cursor-Frage) und weiterrücken.
  async function answerAndAdvance(dto: AnswerDto) {
    if (!planId || attemptId === null) return;
    focusIntent.current = true;
    // Der Fehler wird im `catch` gesetzt, nicht danach aus `action.message` gelesen: das ist der Zustand des Renders,
    // in dem der Klick entstand, und war beim ersten Fehlschlag `null` (B-320). Ein von der Sperre verworfener
    // Doppelklick ruft `fn` gar nicht auf und zeigt darum zu Recht nichts.
    await action.run(async () => {
      try {
        const ack = await api.answerTest(planId, positionId, attemptId, dto);
        if (ack.done) { await finish(attemptId); return; }
        const nx = await api.nextTest(planId, positionId, attemptId);
        if (nx.done) { await finish(attemptId); return; }
        setItem(nx.item ?? null); setCursor(nx.cursor); setTypedAnswer(""); setRevealed(false);
      } catch (e) {
        setError(errorMessage(e));
        setPlanStopped(isPlanInactive(e));
        throw e;
      }
    });
  }

  /*
   * B-286: Die Ansage der Aufgabe steht ÜBER allen Ansichten (Laden, Fehler, Karte) und ist darum über die ganze
   * Sitzung DERSELBE Knoten. Nur so wächst jede Aufgabe in eine schon vorhandene Region hinein (B-134) – auch die erste
   * und die nach einem Fehler fortgesetzte; eine Region in der Karte entstünde dort zusammen mit ihrem Text und bliebe
   * stumm.
   */
  const asked = !error && !result ? item : null;
  return (
    <>
      {/* B-597: beim Diktat sagt die Ansage die Aufgabe, nicht das deutsche Wort, das die Karte verbirgt. */}
      <TaskAnnouncer text={asked && istDiktat(asked, diktatStimme !== null) ? t("lektion.listen") : asked?.prompt}
        gapIndex={asked?.gapIndex} />
      {view()}
    </>
  );

  function view() {
  // Bei einem transienten Fehler den LAUFENDEN Versuch fortsetzen können (nicht verwerfen): resume() holt
  // die aktuelle Frage des bestehenden Attempts erneut, statt über start() einen neuen Versuch anzulegen.
  if (error) return <div className="sohn-body"><div className="error-box" ref={errorRef} tabIndex={-1} role="alert">{error}</div>
    {attemptId !== null && !planStopped && <button type="button" className="btn lime" onClick={resume}>{t("test.resume")}</button>}
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button></div>;

  if (result) return (
    <TestResult result={result} stageName={stageName} titleRef={resultTitleRef} onHome={() => nav("/sohn")}
      onRetry={() => { focusIntent.current = true; void start(); }}
      // B-522: die Herkunft reist mit – nach einer Niederlage feiert der Leerbildschirm des Übens nicht.
      onPractice={() => nav(`/sohn/practice/${positionId}`, { state: { nachTest: true } })}
      rueckmeldung={planId ? { planId, positionId } : undefined} />
  );

  // B-350: hängt der Start oder die nächste Frage, ist das Bild sonst eine Sackgasse – nur der Browser führte zurück.
  if (!item) return <div className="sohn-body"><div className="loading">{t("test.preparing")}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button></div>;

  // Getippte Stufe: Server liefert keine aufgedeckte Lösung (reveal === null) → Eingabefeld.
  const form = aufgabenform(item, diktatStimme !== null);
  const typed = form.antwort !== "selbst" && form.antwort !== "kennenlernen";
  const submitTyped = () => { if (typedAnswer.trim()) answerAndAdvance({ itemIndex: item.itemIndex, givenAnswer: typedAnswer }); };

  return (
    <LektionsRahmen
      test
      /*
        „Später weiter" statt „Abbrechen": der Versuch bleibt am Cursor stehen und wird beim nächsten Start
        fortgesetzt (der Server gibt den laufenden Versuch zurück, statt einen neuen anzulegen). Deshalb kein
        Server-Aufruf und keine Rückfrage – es geht nichts verloren, und ein versehentliches Verlassen verbraucht
        keinen der begrenzten Versuche.
      */
      schliessenName={t("test.later")} onSchliessen={() => nav("/sohn")}
      zaehler={t("test.questionOf", { n: Math.min(cursor + 1, total), total })} anteil={total > 0 ? cursor / total : 0}
      // B-686: führt der Knoten direkt hierher, ist das die einzige Stelle, an der das Kind die Buchseite sieht.
      unterKopf={buchbezug && <div className="row lektion-unterkopf"><span className="pill pfad-buchbezug">{buchbezug}</span></div>}
      // Das Wort „Test" steht in der Anweisung: der Aufbau ist derselbe wie beim Üben, die Klausur muss erkennbar bleiben.
      anweisung={t("lektion.inTest", { task: t(form.anweisung) })}
      hinweis={typed ? t("test.typedHint") : t("test.selfHint")}
    >
      {/* Aufdecken dreht die Karte um wie beim Üben (B-35); getippte Stufen decken nichts auf. */}
      <Aufgabe
        karte={item} form={form} skin={skin} gedreht={!typed && revealed}
        /* Kein Tipp im Test (B-544): er misst. Der Server schickt keinen mehr – hier steht auch kein Platz dafür. */
        /* Die Lösung erscheint IN der Karte, die sich dreht – sonst drehte sich eine leere Geste (Review B-526).
           `revealed` bleibt die einzige Bedingung: vor dem Aufdecken steht sie nirgends. */
        aufloesung={!typed && revealed && (
          <>
            <div className="rev">→ {item.reveal ?? t("test.revealedFallback")}</div>
            <RevealAlternatives alternatives={item.revealAlternatives} />
          </>
        )}
      />
      <div className="lektion-antwort" ref={answerRef}>
        {typed ? (
          // Dieselbe Antwortfläche wie beim Üben – sie weiß nichts von richtig und falsch, und hier bleibt das so.
          <Antwortflaeche
            kartenId={item.itemIndex} karte={item} wert={typedAnswer} onWert={setTypedAnswer} gesperrt={action.busy}
            knopf={t("practice.next")} feldName={t("test.solution")} platzhalter={t("test.solutionPlaceholder")}
            onAbschicken={submitTyped}
          />
        ) : (
          <div className="lektion-fuss">
            <SelfAssessAnswer
              reveal={item.reveal}
              alternatives={item.revealAlternatives}
              revealed={revealed}
              loesungInDerKarte
              busy={action.busy}
              onReveal={() => { focusIntent.current = true; setRevealed(true); }}
              onJudge={(wasKnown) => answerAndAdvance({ itemIndex: item.itemIndex, wasKnown })}
            />
          </div>
        )}
      </div>
    </LektionsRahmen>
  );
  }
}

/**
 * Die Selbsteinschätzung – in der Klausur und, seit B-526, auch im Üben: erst denken, dann aufdecken, dann ehrlich
 * urteilen.
 *
 * **Die Reihenfolge IST die Prüfung.** Stünde die Lösung sofort neben „Gewusst / Nicht gewusst", läse das Kind
 * sie und trüge sich „Gewusst" ein – der Test wäre ein Formular, keine Prüfung. Der Zustand `revealed` ist
 * darum die einzige Bedingung: `item.reveal` ist auf einer nicht-getippten Stufe **immer** gesetzt (der Server
 * schickt die Lösung mit der Frage), ein zusätzliches `|| reveal !== null` machte das Aufdecken zu totem Code.
 *
 * **Exportiert, damit die Reihenfolge prüfbar ist** (Muster `TestResult`): eine reine Props-Komponente, kein
 * Laden, kein `fetch` – der Fehler war unsichtbar, weil ihn niemand ohne den Server ansehen konnte.
 */
export function SelfAssessAnswer({ reveal, alternatives, revealed, busy, loesungInDerKarte = false, texte, onReveal, onJudge }: {
  // Wie `alternatives` optional: ein nullable Feld darf im Vertrag auch fehlen (siehe `?? null` in `start`).
  reveal?: string | null;
  alternatives?: readonly string[] | null;
  revealed: boolean;
  busy: boolean;
  /** Der Aufrufer zeigt die Lösung selbst in der Karte darüber (B-526) – hier bleiben nur die Urteils-Knöpfe. */
  loesungInDerKarte?: boolean;
  /** Beschriftungen des Übens („Nochmal" / „Gewusst!"); ohne Angabe die der Klausur. */
  texte?: { aufdecken: string; nein: string; ja: string };
  onReveal: () => void;
  onJudge: (wasKnown: boolean) => void;
}) {
  const { t } = useTranslation();
  if (!revealed) return (
    <div style={{ marginTop: 10 }}>
      {/* Der Hauptknopf der Karte – derselbe im Üben und im Test (B-526). */}
      <button type="button" className="btn" onClick={onReveal}>{texte?.aufdecken ?? t("test.reveal")}</button>
    </div>
  );
  return (
    <div style={{ marginTop: 10 }}>
      {!loesungInDerKarte && (
        <>
          <div className="rev" style={{ color: "var(--cyan)", fontWeight: 800, marginBottom: 8 }}>→ {reveal ?? t("test.revealedFallback")}</div>
          <RevealAlternatives alternatives={alternatives} />
        </>
      )}
      <div className="judge" style={{ marginTop: 8 }}>
        <button type="button" className="btn red small" disabled={busy} onClick={() => onJudge(false)}>{texte?.nein ?? t("test.notKnown")}</button>
        <button type="button" className="btn lime small" disabled={busy} onClick={() => onJudge(true)}>{texte?.ja ?? t("test.known")}</button>
      </div>
    </div>
  );
}

/**
 * Der Auswertungsschirm der Klausur. **Exportiert, damit er prüfbar ist**: er ist eine reine
 * Props-Komponente (kein Laden, kein `fetch`), und genau die Regeln, die hier hängen – teilen alle Zeilen
 * ihren Aufgabentext? gibt es Fehlnennungen? – sind die, die man sonst nur von Hand ansehen könnte.
 */
export function TestResult({ result, stageName, titleRef, onHome, onRetry, onPractice, rueckmeldung }: {
  result: TestSubmitResponse; stageName?: string | null; onHome: () => void; onRetry: () => void; onPractice: () => void;
  /**
   * B-629: im Test meldet das Kind erst hier, je Karte neben der Lösung (B-623, Entscheidung 5) – während des Tests gibt
   * es den Knopf nicht. Ohne Plan/Position (Vorschau) bleibt das Blatt ohne Knopf.
   */
  rueckmeldung?: { planId: number; positionId: number };
  /**
   * Landeplatz für den Fokus (B-541). Fokussiert wird er vom Aufrufer, nur nach einer Handlung des Kindes – ein Ergebnis,
   * das ohne Handlung erscheint (der Start trifft einen fertigen Versuch), bekommt keinen Fokus.
   */
  titleRef?: Ref<HTMLDivElement>;
}) {
  const { t } = useTranslation();
  const stageLabel = useStageLabel();
  const pct = result.scorePercent;
  const ring = { background: `conic-gradient(${result.passed ? "var(--lime)" : "var(--red)"} 0 ${pct}%, #0c0e2c ${pct}% 100%)` };
  // Teilen alle Zeilen ihren Aufgabentext (eine Menge – jede Zeile trägt dieselbe Anweisung), steht er EINMAL
  // oben, und groß gesetzt wird die Lösung. Sonst schreit 16-mal derselbe Auftrag und die Antwort, die das Kind
  // lernen soll, flüstert daneben. Lückentexte sind ausgenommen: dort unterscheidet `gapIndex` die Zeilen, und
  // ihr Text gehört in jede (B-76).
  const sharedPrompt = result.items.length > 1
    && result.items.every((o) => o.prompt === result.items[0].prompt && o.gapIndex == null)
    ? result.items[0].prompt
    : null;
  return (
    <div className="sohn-body">
      <div className="victory">
        <div style={{ fontFamily: "var(--font-display)", letterSpacing: ".2em", color: "var(--muted)", fontSize: 12 }}>
          {t("test.header", { stage: (stageName ?? stageLabel(result.stage)).toUpperCase() })}
        </div>
        <div className={`vtitle ${result.passed ? "win" : "lose"}`} ref={titleRef} tabIndex={-1}>{result.passed ? t("test.victory") : t("test.almost")}</div>
        <div className="ring" style={ring}><b>{pct}%<small>{result.correctItems} / {result.totalItems}</small></b></div>
        <div className="dad-test-reaction" style={{ margin: "10px auto 4px" }}>
          <LearningDad
            action={result.passed ? "thumbs-up" : "facepalm"}
            size="md"
            caption={result.passed ? "Klasse Leistung!" : "Kein Problem – beim nächsten Mal!"}
          />
        </div>
        <p className="sub">{t("test.passMark", { pct: result.passPercent })}</p>

        <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
          {sharedPrompt && <p className="sub" style={{ marginTop: 0 }}>{sharedPrompt}</p>}
          {!sharedPrompt && result.items.map((o) => (
            // Selbst eingeschätzte Karten tragen keine getippte Antwort (`givenAnswer` null) – dort nur die Lösung.
            <div key={o.itemIndex}>
              <ErgebnisZeile prompt={o.prompt} gapIndex={o.gapIndex} expected={o.expected}
                givenAnswer={o.givenAnswer} wasCorrect={o.wasCorrect} misspelled={o.misspelled}
                zeigeAntwort={o.givenAnswer != null} />
              {rueckmeldung && <KartenMeldung {...rueckmeldung} itemIndex={o.itemIndex} />}
            </div>
          ))}
          {sharedPrompt && result.items.map((o) => (
            // Das Zeichen ist die Optik, das Wort die Bedeutung: allein gelesen hieße „❌ Berlin" für das Kind „Berlin war
            // falsch", während der Server „Berlin hast du nicht genannt" meint (B-77/E2). Die Lösung steht groß, weil der
            // Aufgabentext schon oben steht.
            <div key={o.itemIndex}>
              <div className="row" style={{ padding: "4px 0" }}>
                <span aria-hidden="true">{o.wasCorrect ? "✅" : "❌"}</span>
                <span className="test-prompt">{o.expected}</span>
                <span className="sub" style={{ marginLeft: "auto" }}>{o.wasCorrect ? t("test.named") : t("test.notNamed")}</span>
              </div>
              {rueckmeldung && <KartenMeldung {...rueckmeldung} itemIndex={o.itemIndex} />}
            </div>
          ))}
        </div>

        {/* Bei einer Menge zählt die Zeile darüber, was das Kind VERGESSEN hat – ohne diese Liste
            verschwände lautlos, was es tatsächlich getippt hat (B-77/E2). */}
        {result.wrongMentions && result.wrongMentions.length > 0 && (
          <div className="card" style={{ width: "100%", marginTop: 4, textAlign: "left" }}>
            <p className="sub" style={{ marginTop: 0 }}>{t("test.didNotCount")}</p>
            {result.wrongMentions.map((m, i) => (
              // Bewusst leiser als die Lösung oben: was das Kind fälschlich getippt hat, ist die Nebensache –
              // die Lehre ist der Eintrag, den es vergessen hat.
              <div className="row" key={`${m}-${i}`} style={{ padding: "4px 0" }}>
                <span aria-hidden="true">❌</span>
                <span className="sub">{m}</span>
              </div>
            ))}
          </div>
        )}

        {/* B-430 (c): Der Knopf verspricht Üben, nicht „diese Wörter" – der Test macht seine Fehler in der Position
            nicht fällig, die Runde kann also andere Karten bringen oder keine (dann bietet sie den Test wieder an). */}
        {!result.passed &&
          <button type="button" className="btn" onClick={onPractice}>{t("test.practice")}</button>}
        {/* Ohne attemptsRemaining bot der Knopf einen Versuch an, den der Server längst abgewiesen hätte
            (ApiErrors.TestAttemptsExhausted, B-62) - ein Klick danach landete nur in der Fehlerbox. */}
        {!result.passed && result.attemptsRemaining > 0 &&
          <button type="button" className="btn gold" onClick={onRetry}>{t("test.tryAgain")}</button>}
        <button type="button" className="btn ghost" onClick={onHome}>{t("progress.toHome")}</button>
      </div>
    </div>
  );
}

const isPlanInactive = (e: unknown) => e instanceof ApiError && e.code === "plan_inactive";
