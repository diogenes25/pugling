import { useEffect, useRef, useState } from "react";
import { arcadeDatum } from "../lib/datum";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError, api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import { useOncePerKey } from "../lib/useOncePerKey";
import { useAction } from "../lib/useAction";
import { useSohn } from "./SohnApp";
import { LetterBoxes } from "../components/LetterBoxes";
import { LetterTiles } from "../components/LetterTiles";
import { AudioButton } from "../components/AudioButton";
import { ClozePrompt, TaskAnnouncer } from "../components/ClozePrompt";
import { choicesClass } from "../lib/choices";
import { ListRule } from "../components/ListRule";
import { Passage } from "../components/Passage";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import type { PracticeCard, PositionSession, ReviewOutcome } from "../lib/types";

// Kleine Anerkennung bei jedem Treffer – Variation sorgt für Abwechslung (Daumen, Stern, Feuer, Muskel).
const SMALL_EMOJI = ["👍", "⭐", "🔥", "💪", "✨"];

/** `wrong` (B-420): eine falsch getippte Antwort hält die Karte an, bis das Kind die Lösung gelesen hat und weiterdrückt. */
type Phase = "loading" | "front" | "back" | "wrong" | "done" | "empty" | "error";

/**
 * Wie eine Review-Antwort dargestellt wird – als reine Regel statt inline in `judge()`, damit sie ohne
 * Bildschirm und `fetch` prüfbar ist (Muster `SelfAssessAnswer`).
 * <p>
 * `displayOnly` (ShowBoth/B-96, Kennenlernen) gibt IMMER `"none"` zurück, unabhängig von
 * `outcome.wasCorrect`: der Server wertet diese Stufe nie (siehe `PositionPracticeController.Review`),
 * `wasCorrect` steht dort einfach auf `false`, weil kein `wasKnown` mitgeschickt wird – ohne diese
 * Sonderregel läse das Kind nach jeder betrachteten Karte ein „Leider nicht.", ein Urteil, das die Stufe
 * per Story-Vorgabe gerade NICHT fällen darf.
 */
export function reviewFeedback(outcome: ReviewOutcome | null | undefined, displayOnly: boolean):
  | { kind: "none" }
  | { kind: "correct"; awarded: number; combo: number; comboBonus: number; speedBonus: number; box: number }
  | { kind: "wrong"; expected: string | null } {
  if (!outcome || displayOnly) return { kind: "none" };
  if (outcome.wasCorrect) {
    return {
      kind: "correct", awarded: outcome.awarded, combo: outcome.combo,
      comboBonus: outcome.comboBonus, speedBonus: outcome.speedBonus, box: outcome.box,
    };
  }
  return { kind: "wrong", expected: outcome.expected ?? null };
}

export function SohnPractice() {
  const { planId, refreshWallet, setStreak, celebrate } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const nav = useNavigate();

  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const [phase, setPhase] = useState<Phase>("loading");
  const [error, setError] = useState<string | null>(null);
  const [cards, setCards] = useState<PracticeCard[]>([]);
  const [idx, setIdx] = useState(0);
  const [typedAnswer, setTypedAnswer] = useState("");
  const [combo, setCombo] = useState(0);
  const [earned, setEarned] = useState(0);
  const [toast, setToast] = useState<string | null>(null);
  const [lastOutcome, setLastOutcome] = useState<ReviewOutcome | null>(null);
  // B-420: was auf der angehaltenen Karte steht – die eigene Antwort und die Lösung (null in einer Menge).
  const [mistake, setMistake] = useState<{ given: string; expected: string | null } | null>(null);
  const continueRef = useRef<HTMLButtonElement>(null);
  // B-419: „Jetzt abfragen" startet eine neue Runde derselben Position – eine neue Sitzung, darum ein neuer Schlüssel
  // für den Start und ein neuer Lauf des Heartbeat-Effekts, der die alte Sitzung schließt.
  const [round, setRound] = useState(0);
  // Abwechslung: Tipp erst auf Wunsch aufdecken; Tempo-Modus (persistiert) blendet eine Countdown-Leiste ein.
  const [hintShown, setHintShown] = useState(false);
  const [tempo, setTempo] = useState(() => localStorage.getItem("pugling.tempo") === "1");

  function toggleTempo() {
    setTempo((t) => {
      const next = !t;
      localStorage.setItem("pugling.tempo", next ? "1" : "0");
      return next;
    });
  }

  // Geteilte Instanz für `judge` UND `reshuffleImage` (B-49, Entscheidung 3): eine laufende Bewertung
  // sperrt harmlos auch den Bildwechsel und umgekehrt - beides ändert dieselbe Karte.
  const action = useAction(errorMessage);
  const session = useRef<PositionSession | null>(null);
  const startedIso = useRef<number>(Date.now());

  // Ohne Ziel navigiert weg - nichts zu laden.
  useEffect(() => { if (!planId || !positionId) nav("/sohn"); }, [planId, positionId, nav]);

  // Sitzung starten + fällige Karten laden - wie `startedFor` in `SohnTest.tsx`, aber über `useOncePerKey`:
  // ein Remount (React-StrictMode im Dev, generell) legte vorher zwei `PracticeSession`-Zeilen an, die
  // zweite blieb bei Cursor 0 offen liegen (B-62). Ein reiner Boolean-Gate reicht dafür nicht - siehe
  // `useOncePerKey`s eigene Dokumentation für den Stolperstein, den diese Reparatur selbst erst aufdeckte.
  useOncePerKey(
    planId && positionId ? `${planId}:${positionId}:${round}` : null,
    async () => {
      const sess = await api.startSession(planId!, positionId);
      const due = await api.cards(planId!, positionId, sess.id);
      return { sess, due };
    },
    ({ sess, due }) => {
      session.current = sess;
      setCards(due);
      setPhase(due.length === 0 ? "empty" : "front");
    },
    // 404: Plan oder Position gibt es nicht mehr (Reload nach dem Löschen, B-326) – die Basis korrigiert die Wahl.
    (e) => {
      if (e instanceof ApiError && e.status === 404) { nav("/sohn", { replace: true }); return; }
      setError(errorMessage(e)); setPhase("error");
    },
  );

  // Heartbeat: alle 12s aktive Zeit melden (Server clamped ohnehin). Session am Ende schließen.
  useEffect(() => {
    if (!planId || !positionId) return;
    // Hier und nicht beim Klick auf „Jetzt abfragen": das Cleanup der vorigen Runde liest den Wert noch für ihre
    // Rest-Sekunden, und es läuft erst nach dem Klick (Review-Fund).
    startedIso.current = Date.now();
    const iv = setInterval(() => {
      if (session.current) api.heartbeat(planId, positionId, session.current.id, 12, true).catch(() => {});
    }, 12000);
    return () => {
      clearInterval(iv);
      // Die Sitzung gehört ab hier dem Schließen: eine neue Runde (B-419) setzt die nächste erst, wenn sie gestartet ist,
      // und bis dahin pingte der Heartbeat der neuen Runde sonst die alte an.
      const ending = session.current;
      session.current = null;
      if (ending) {
        const secs = Math.round((Date.now() - startedIso.current) / 1000) % 12;
        const sessionId = ending.id;
        // Erst die Rest-Sekunden, dann schließen: `end` wertet das Ziel sofort aus, und bei einer Übung
        // ohne einzelne Inhalte hängt die Pflicht an der Verweildauer. Kämen die Sekunden danach an,
        // stünde die Gutschrift bis zum Periodenschluss aus.
        api.heartbeat(planId, positionId, sessionId, secs, true)
          .catch(() => {})
          .then(() => api.endSession(planId, positionId, sessionId))
          .catch(() => {});
      }
    };
  }, [planId, positionId, round]);

  // B-420: auf der angehaltenen Karte gehört der Fokus dem „Weiter" – so trägt Enter das Kind weiter, und die Ansage der
  // Lösung (Live-Region) wird nicht von einem Feld übertönt, das nichts mehr annimmt.
  useEffect(() => { if (phase === "wrong") continueRef.current?.focus(); }, [phase]);
  // Und danach zurück in die Antwort der nächsten Karte: vor dem Halt blieb das Feld von Karte zu Karte stehen, jetzt
  // ersetzt der Knopf es, und ohne diesen Schritt fiele der Fokus auf `body` (Review-Fund).
  const answerRef = useRef<HTMLDivElement>(null);
  const prevPhase = useRef<Phase>(phase);
  // B-439: endet die Runde, verschwindet die Karte samt fokussiertem Feld bzw. „Weiter" – der Fokus geht an die
  // Überschrift des Rundenendes, sonst fiele er auf `body` und ein Screenreader verlöre die Stelle.
  const doneRef = useRef<HTMLDivElement>(null);
  // B-443: „Jetzt abfragen" nimmt sich selbst aus dem DOM (Rundenende → Laden → Karte) – der Fokus ginge auf `body`.
  // Nur nach dieser Taste, nicht beim ersten Laden der Seite: dort soll keine Tastatur ungefragt aufspringen.
  const focusAfterLoad = useRef(false);
  // Die erste Taste einer Karte ohne Eingabefeld (Umdrehen, Kennenlernen-„Weiter"): welche Stufe kommt, entscheidet der
  // Server – auch sie muss den Fokus bekommen können (Review-Fund).
  const cardActionRef = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    const toCard = phase === "front"
      && (prevPhase.current === "wrong" || (prevPhase.current === "loading" && focusAfterLoad.current));
    if (toCard) {
      focusAfterLoad.current = false;
      (answerRef.current?.querySelector<HTMLElement>("input, button:not([disabled])") ?? cardActionRef.current)?.focus();
    }
    // Der Effekt läuft nur bei einem Phasenwechsel, „done" → „done" gibt es hier nicht.
    if (phase === "done") doneRef.current?.focus();
    prevPhase.current = phase;
  }, [phase]);

  /** B-419: eine neue Runde derselben Position – nach dem Kennenlernen die Abfrage. */
  function startNextRound() {
    focusAfterLoad.current = true;
    setPhase("loading");
    setCards([]);
    setIdx(0);
    setEarned(0);
    setCombo(0);
    setLastOutcome(null);
    setRound((r) => r + 1);
  }

  /*
   * Eine Bewertung je Karte, seit B-49 über `useAction` gesperrt (Ref, nicht State - dieselbe Begründung
   * wie überall sonst: `busy` als State steht erst nach dem Re-Render am Knopf). Seit die Auswahl
   * ausgespielt wird (B-73), stehen hier drei Knöpfe nebeneinander statt einer – zwei schnelle Tipper
   * schickten sonst zwei `review` auf denselben `itemIndex` **und** zwei `next()`, der Zähler sprang von
   * 1/5 auf 3/5 und Karte 2 kam nie.
   */
  async function judge(card: PracticeCard, payload: { wasKnown?: boolean; givenAnswer?: string }) {
    if (!planId || !session.current) return;
    // `next()` läuft bewusst außerhalb von `action.run` und auch nach einem Fehlschlag: die Bewertung ist
    // idempotent genug, das innere `try/catch` schluckt den Fehler schon selbst, und `action.run` bekommt
    // darum nie eine Ablehnung zu sehen. `false` heißt hier also nur noch „von der Sperre verworfen" - dann
    // gehört das Weiterschalten dem laufenden Aufruf, sonst überspränge ein doppeltes Enter eine Karte (B-284).
    // Ausnahme `plan_inactive` (B-330): dann zählt keine Karte mehr, und die Runde endet mit dem Satz der Arcade.
    let planStopped = false;
    // B-420: eine falsch GETIPPTE Antwort hält an. Die Selbsteinschätzung nicht – dort stand die Lösung schon auf der
    // Rückseite, bevor das Kind „Nochmal" drückte.
    let halted = false;
    const ran = await action.run(async () => {
      try {
        const outcome = await api.review(planId, positionId, session.current!.id, { itemIndex: card.itemIndex, ...payload });
        setLastOutcome(outcome ?? null);
        setCombo(outcome?.combo ?? 0);
        const feedback = reviewFeedback(outcome, card.displayOnly ?? false);
        if (feedback.kind === "correct") {
          setEarned((e) => e + feedback.awarded + feedback.comboBonus + feedback.speedBonus);
          if (feedback.comboBonus > 0) {
            const tier = feedback.combo >= 10 ? "big" : "medium";
            celebrate(tier, tier === "big" ? "🥷" : "🎉", `COMBO ×${feedback.combo}`, t("practice.comboBonus", { bonus: feedback.comboBonus }));
          } else {
            celebrate("small", SMALL_EMOJI[feedback.combo % SMALL_EMOJI.length]);
          }
          if (feedback.awarded > 0) {
            setToast(`+${feedback.awarded} 🪙${feedback.box ? ` · ${t("practice.box", { box: feedback.box })}` : ""}`);
            setTimeout(() => setToast(null), 1100);
          }
          refreshWallet();
        } else if (feedback.kind === "wrong") {
          // Ohne getroffenen Eintrag gibt es keine Lösung zu nennen: bei einer Menge (ungeordnete Liste) wäre
          // „Lösung: Hessen" willkürlich, solange ein Dutzend Einträge offen ist – und verriete einen, der noch
          // gefragt wird. Der Server liefert dann `null`, und hier bleibt es bei der schlichten Absage.
          setToast(feedback.expected ? t("practice.solution", { expected: feedback.expected }) : t("practice.wrong"));
          if (payload.givenAnswer !== undefined) {
            halted = true;
            setMistake({ given: payload.givenAnswer, expected: feedback.expected });
            setPhase("wrong");
          } else {
            setTimeout(() => setToast(null), 1600);
          }
        }
        // feedback.kind === "none": kein Outcome ODER eine Anzeigenurstufe (ShowBoth) - kein Urteil, kein Toast.
      } catch (e) {
        // Sonst ist die Bewertung idempotent genug, und die Runde läuft weiter.
        if (e instanceof ApiError && e.code === "plan_inactive") {
          planStopped = true;
          setError(errorMessage(e)); setPhase("error");
        }
      }
    });
    if (ran && !planStopped && !halted) next();
  }

  function next() {
    setTypedAnswer("");
    setHintShown(false);
    setMistake(null);
    setToast(null);
    if (idx + 1 >= cards.length) {
      if (planId) api.overview(planId).then((o) => setStreak(o.currentStreak)).catch(() => {});
      setPhase("done");
    } else {
      setIdx((i) => i + 1);
      setPhase("front");
    }
  }

  /*
   * B-286: Die Ansage der Aufgabe steht ÜBER allen Ansichten (Laden, Fehler, Karte) und ist darum über die ganze
   * Sitzung DERSELBE Knoten. Nur so wächst jede Aufgabe in eine schon vorhandene Region hinein (B-134) – auch die erste
   * und die nach einem Fehler fortgesetzte; eine Region in der Karte entstünde dort zusammen mit ihrem Text und bliebe
   * stumm.
   */
  const asked = phase === "front" || phase === "back" || phase === "wrong" ? cards[idx] : null;
  return (
    <>
      <TaskAnnouncer text={asked?.prompt} gapIndex={asked?.gapIndex} />
      {view()}
    </>
  );

  function view() {
  if (phase === "loading") return <div className="sohn-body"><div className="loading">{t("practice.loading")}</div></div>;
  if (phase === "error") return <div className="sohn-body"><div className="error-box">{error}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button></div>;

  // B-117: eine Anzeigestufe (ShowBoth, B-96) kennt keinen Test - die Klausur antwortet dort mit
  // `stage_not_testable`. Ohne dieses Gate bot der Bildschirm den Knopf trotzdem an und schickte in die
  // rohe Fehlerseite, die exakt dieselbe Fehlerklasse ist, die B-114 an der Tageskarte schon behoben hat.
  const testable = session.current?.testable ?? true;

  if (phase === "empty") return (
    <div className="sohn-body">
      <div className="card" style={{ textAlign: "center" }}>
        <div className="screen-title">{t("practice.nothingDue")}</div>
        <p className="sub">{testable
          ? t("practice.nothingDueTest")
          : t("practice.nothingDueLater")}</p>
      </div>
      {testable && <button type="button" className="btn" onClick={() => nav(`/sohn/test/${positionId}`)}>{t("practice.toTest")}</button>}
      <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button>
    </div>
  );

  // B-419: wie viele Karten dieser Runde zum ersten Mal gezeigt wurden – das sagt der Server je Karte (`introduction`),
  // nicht die Stufe: eine Kennenlern-Stufe des Tages führt nichts ein und behält den heutigen Bildschirm.
  const introduced = cards.filter((c) => c.introduction).length;

  if (phase === "done") return (
    <div className="sohn-body">
      <div className="victory">
        <div className="vtitle win" role="heading" aria-level={1} ref={doneRef} tabIndex={-1}>{t("practice.roundDone")}</div>
        {introduced > 0 && <p className="sub" style={{ textAlign: "center" }}>{t("practice.introduced", { count: introduced })}</p>}
        {/* Eine Runde kann neue und schon bekannte Karten mischen (die Einführung gilt je Karte): was die
            abgefragten verdient haben, bleibt sichtbar. Nur eine reine Kennenlern-Runde hat nichts zu zeigen. */}
        {cards.length > introduced && (
          <div className="reward">
            <div className="card">🪙<span style={{ color: "var(--gold)" }}>+{earned}</span></div>
            <div className="card">🃏<span>{t("practice.cards", { count: cards.length - introduced })}</span></div>
          </div>
        )}
        {introduced > 0 && <button type="button" className="btn lime" onClick={startNextRound} style={{ marginTop: 10 }}>{t("practice.quizNow")}</button>}
        {testable && <button type="button" className="btn gold" onClick={() => nav(`/sohn/test/${positionId}`)} style={{ marginTop: 10 }}>{t("practice.onToTest")}</button>}
        <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button>
      </div>
    </div>
  );

  const card = cards[idx];
  const typed = card.reveal === null; // getippte Stufe → Eingabe; sonst Flip-Karte (Selbsteinschätzung) oder ShowBoth (sofort offen, kein Urteil)
  const submitTyped = () => { if (typedAnswer.trim()) judge(card, { givenAnswer: typedAnswer }); };

  /**
   * „Anderes Bild": das abgelehnte kommt nie wieder. Die neue Wahl wird lokal in die Karte gespiegelt,
   * damit der Wechsel sofort sichtbar ist – ein Neuladen der Sitzung würde den Fortschritt anfassen.
   * Ohne Alternative antwortet der Server 409; dann bleibt das Bild stehen und wir sagen es nur.
   */
  async function reshuffleImage() {
    if (!planId || !session.current) return;
    const at = idx;
    await action.run(async () => {
      try {
        // Nicht `next` nennen – so heißt die Karten-Weiterschaltung oben.
        const picked = await api.reshuffleCardImage(planId, positionId, session.current!.id, cards[at].itemIndex);
        setCards((prev) => prev.map((c, i) =>
          i === at ? { ...c, imageUrl: picked.imageUrl, imageAlt: picked.imageAlt } : c));
      } catch (e) {
        // `media_no_alternative` hat seinen Satz unter `problem.*` (B-415) – `errorMessage` ist die Arcade-Fassung.
        setToast(errorMessage(e));
      }
    });
  }
  return (
    <div className="sohn-body">
      <div className="row play-head">
        <span className="pill cyan">{t("practice.cardOf", { n: idx + 1, total: cards.length })}</span>
        <span className="row" style={{ marginLeft: "auto", gap: 8 }}>
          {combo >= 2 && <span className="pill mag">⚡ COMBO ×{combo}</span>}
          <button type="button" className={`pill toggle-pill ${tempo ? "lime" : ""}`} onClick={toggleTempo} aria-pressed={tempo}>
            {t("practice.tempo")}
          </button>
          {/*
            Sichtbarer Ausweg aus der laufenden Runde. Ohne Rückfrage, weil eine Rückfrage über eine
            Aktion, deren Wort schon die Wahrheit sagt, zum Wegklicken erzieht. Der Server-Aufruf (`end`)
            gehört bewusst NICHT hierher – das Cleanup des Heartbeat-Effekts oben schickt die Rest-Sekunden
            und beendet die Sitzung; ein zweiter Aufruf hier wäre eine Doppelquelle für dieselbe Sache.
            Folgenlos ist das Verlassen nur beim Leitner-Üben (jede gespielte Karte ist schon gebucht):
            bei einer Übung ohne automatische Prüfung misst die Pflicht EINE Sitzung gegen die Schwelle,
            und die nächste Runde beginnt wieder bei null.
          */}
          <button type="button" className="pill toggle-pill exit-pill" onClick={() => nav("/sohn")}>
            {t("practice.endRound")}
          </button>
        </span>
      </div>

      {/* Tempo-Modus: Countdown-Leiste je Karte (rein visueller Ansporn; der Schnell-Bonus zählt serverseitig). */}
      {tempo && typed && phase === "front" && <div className="tempo-bar" key={idx}><i /></div>}

      <div className="flash">
        {/* Nur die Selbsteinschätzung kommt auf „back" – ShowBoth ist von Anfang an offen und dreht nichts (B-35). */}
        <div className={phase === "back" ? "fcard flipped" : "fcard"}>
          <div className="lang">{t("practice.task")}</div>
          {/*
            Das Bild kommt nur auf Stufen, auf denen es die Lösung nicht verraten kann – der Server
            entscheidet das, das Frontend rendert nur, was da ist. Es steht über dem Wort, weil es beim
            Einprägen hilft (Bild + Wort zusammen), nicht als Dekoration darunter.
          */}
          {card.imageUrl && (
            <CardImage
              key={card.imageUrl}
              url={card.imageUrl}
              alt={card.imageAlt ?? ""}
              // Auf der angehaltenen Karte liefe die Meldung eines 409 in die stumme Region (sr-only) – dort kein Wechsel.
              busy={action.busy || phase === "wrong"}
              onReshuffle={reshuffleImage}
            />
          )}
          <Passage text={card.passage} />
          {/*
            Aufnahme und Frage nebeneinander, nicht entweder-oder: Beim Hörverstehen braucht das Kind
            beides. Dass die Vokabel-Hörstufe ihr Wort verschweigt, entscheidet der Server – er schickt
            dort keinen `prompt`. Eine Regel, die hier stünde, müsste jeder neue Audio-Typ nachtragen.

            Fehlt der Text, IST die Aufnahme die Aufgabe: kurz, also anspielen und der Knopf genügt. Steht
            eine Frage daneben, ist sie das Material und kann lang sein – dann die Bedienelemente, sonst
            könnte das Kind sie nicht anhalten. Die Unterscheidung liest sich aus den gelieferten Feldern,
            nicht aus dem Übungstyp.
          */}
          {card.audioUrl && (
            <AudioButton url={card.audioUrl} autoPlay={!card.prompt} withControls={!!card.prompt} />
          )}
          <ClozePrompt text={card.prompt} gapIndex={card.gapIndex} fitAsked />
          {/* Die Dekodierung steht auf der VORDERSEITE, direkt unter dem Satz: sie ist die Methode, nicht die
              Lösung – man liest den Satz und seine Entschlüsselung zusammen (B-78). */}
          <BirkenbihlDecoding decoding={card.decoding} />
          <ListRule type={card.type} anyOrder={card.anyOrder} itemIndex={card.itemIndex} />
          {card.hint && typed && (
            hintShown
              ? <div className="sub">💡 {card.hint}</div>
              : <button type="button" className="btn ghost small" style={{ marginTop: 6 }} onClick={() => setHintShown(true)}>{t("practice.hint")}</button>
          )}
          {/* ShowBoth (B-96): a free display stage shows both sides at once, no flip needed - the server
              flags this via `displayOnly`, distinct from self-assessment which reveals only after "Umdrehen". */}
          {(phase === "back" || card.displayOnly) && card.reveal && <div className="rev">→ {card.reveal}</div>}
          {(phase === "back" || card.displayOnly) && card.reveal && <RevealAlternatives alternatives={card.revealAlternatives} />}
        </div>

        {/* Gruppe statt loser Knöpfe: ein Screenreader liest sonst „Leeds, Schaltfläche" ohne Bezug zur
            Frage. Der Key trägt den Index mit – der Autor darf dieselbe Option zweimal eintragen. */}
        {phase === "wrong" && mistake ? (
          // B-420: die Karte bleibt stehen – Frage oben, darunter die eigene Antwort und die Lösung, bis „Weiter".
          <div className="card mistake">
            <div className="sub">{t("practice.yourAnswerWas")} <s>{mistake.given}</s></div>
            <div className="rev">{mistake.expected ? t("practice.solution", { expected: mistake.expected }) : t("practice.wrong")}</div>
            <button type="button" ref={continueRef} className="btn lime" style={{ marginTop: 10 }} onClick={next}>{t("practice.next")}</button>
          </div>
        ) : typed && card.choices ? (
          <div ref={answerRef} className={choicesClass(card.choices)} style={{ gap: 8, flexWrap: "wrap" }} role="group" aria-label={t("practice.choices")}>
            {card.choices.map((c, i) => (
              <button type="button" key={`${i}-${c}`} className="btn ghost" disabled={action.busy}
                onClick={() => judge(card, { givenAnswer: c })}>{c}</button>
            ))}
          </div>
        ) : typed ? (
          <div ref={answerRef}>
            {card.letterTiles ? (
              <LetterTiles tiles={card.letterTiles} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} />
            ) : card.answerLength ? (
              <LetterBoxes length={card.answerLength} value={typedAnswer} onChange={setTypedAnswer} onSubmit={submitTyped} pattern={card.answerPattern ?? undefined} />
            ) : (
              <form onSubmit={(e) => { e.preventDefault(); submitTyped(); }}>
                <input
                  aria-label={t("practice.yourAnswer")}
                  name="answer"
                  autoComplete="off"
                  autoCapitalize="off"
                  autoCorrect="off"
                  spellCheck={false}
                  style={{ width: "100%", background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 12, fontSize: 16 }}
                  placeholder={t("practice.answerPlaceholder")}
                  value={typedAnswer}
                  onChange={(e) => setTypedAnswer(e.target.value)}
                />
              </form>
            )}
            <button type="button" className="btn lime" style={{ marginTop: 10 }} disabled={action.busy || !typedAnswer.trim()} onClick={submitTyped}>{t("practice.check")}</button>
          </div>
        ) : card.displayOnly ? (
          // Kennenlernen: no judgment to make, so no "Gewusst?" buttons - just move on.
          <button type="button" ref={cardActionRef} className="btn lime" disabled={action.busy} onClick={() => judge(card, {})}>{t("practice.next")}</button>
        ) : phase === "front" ? (
          <button type="button" ref={cardActionRef} className="btn" onClick={() => setPhase("back")}>{t("practice.flip")}</button>
        ) : (
          <div className="judge">
            {/* B-49: vorher ohne `disabled` - ein Doppelklick konnte zwei `review` auf denselben `itemIndex`
                auslösen (dieselbe Lücke, die die Choice-Knöpfe oben schon durch `useAction` geschlossen
                hatten). */}
            <button type="button" className="btn red small" disabled={action.busy} onClick={() => judge(card, { wasKnown: false })}>{t("practice.again")}</button>
            <button type="button" className="btn lime small" disabled={action.busy} onClick={() => judge(card, { wasKnown: true })}>{t("practice.knewIt")}</button>
          </div>
        )}
      </div>

      {/* Auf der angehaltenen Karte (B-420) steht die Lösung schon sichtbar in der Karte – die Region sagt sie nur an. */}
      <div className={`live-region${toast ? (phase === "wrong" ? " sr-only" : " toast") : ""}`} role="status" aria-live="polite">{toast}</div>
      {lastOutcome?.dueOn && phase === "front" && (
        <p className="sub" style={{ textAlign: "center" }}>{t("practice.nextDue", { date: arcadeDatum(lastOutcome.dueOn, i18n.language) })}</p>
      )}
    </div>
  );
  }
}

/**
 * Das Bild zur Karte. Es ist Teil der Lernhilfe, nicht Deko – deshalb bekommt es Platz und einen
 * Alt-Text (der Server liefert ihn nur mit, wenn er die Lösung nicht verrät).
 *
 * Der „anderes Bild"-Knopf ist bewusst klein und unaufdringlich: Bildkonstanz ist beim Vokabellernen
 * der Merkeffekt, das Wechseln ist die Ausnahme. Genau deshalb ist es aber wichtig, dass es sie gibt –
 * ein Motiv, das ein Kind nicht mag, arbeitet gegen das Lernen.
 */
function CardImage({ url, alt, busy, onReshuffle }: { url: string; alt: string; busy: boolean; onReshuffle: () => void }) {
  const { t } = useTranslation();
  return (
    <figure style={{ margin: "0 0 10px", textAlign: "center" }}>
      <img
        src={url}
        alt={alt}
        style={{ maxWidth: "100%", maxHeight: 180, objectFit: "contain", borderRadius: 12 }}
      />
      <figcaption>
        <button
          type="button"
          className="btn ghost small"
          style={{ width: "auto", marginTop: 4 }}
          disabled={busy}
          onClick={onReshuffle}
        >
          {t("practice.otherImage")}
        </button>
      </figcaption>
    </figure>
  );
}
