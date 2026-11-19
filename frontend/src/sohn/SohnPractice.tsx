import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError, api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import { useOncePerKey } from "../lib/useOncePerKey";
import { useAction } from "../lib/useAction";
import { useSohn } from "./SohnApp";
import { TaskAnnouncer } from "../components/ClozePrompt";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { aufgabenform, istDiktat } from "./aufgabenform";
import { Lernkarte } from "./Lernkarte";
import { useVoiceFor } from "../components/SpeakButton";
import { Antwortflaeche, Aufgabe, LektionsRahmen, RueckmeldungsBlatt, type Rueckmeldung } from "./Lektion";
import { SelfAssessAnswer } from "./SohnTest";
import { Mascot } from "../components/Mascot";
import type { PracticeCard, PositionSession, ReviewInput, ReviewOutcome } from "../lib/types";
import { KartenMeldung } from "./KartenMeldung";

/** So lange nimmt die nächste Karte nach „Weiter" auf dem Blatt keinen Knopfdruck an (Doppeltipp, gehaltenes Enter). */
export const NACH_BLATT_MS = 350;

// Kleine Anerkennung bei jedem Treffer – Variation sorgt für Abwechslung (Daumen, Stern, Feuer, Muskel).
const SMALL_EMOJI = ["👍", "⭐", "🔥", "💪", "✨"];

/**
 * `blatt` (B-420, B-526): nach einer vom Server geprüften Antwort bleibt die Karte stehen und zeigt das Blatt, bis das
 * Kind „Weiter" drückt – bei falsch, um die Lösung zu lesen, bei richtig, weil die Rückmeldung an einer Stelle sitzt.
 */
type Phase = "loading" | "front" | "back" | "blatt" | "done" | "empty" | "error";
/** Was eine Bewertung schickt: die Antwort ohne den Index, den `judge` aus der Karte nimmt (Vertragstyp, nicht von Hand). */
type ReviewAntwort = Omit<ReviewInput, "itemIndex">;

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
  | { kind: "wrong"; expected: string | null; misspelled: boolean } {
  if (!outcome || displayOnly) return { kind: "none" };
  if (outcome.wasCorrect) {
    return {
      kind: "correct", awarded: outcome.awarded, combo: outcome.combo,
      comboBonus: outcome.comboBonus, speedBonus: outcome.speedBonus, box: outcome.box,
    };
  }
  // B-422: nur ein Etikett – die Antwort bleibt falsch, der Kasten fällt wie sonst.
  return { kind: "wrong", expected: outcome.expected ?? null, misspelled: outcome.misspelled ?? false };
}

export function SohnPractice() {
  const { planId, refreshWallet, setStreak, celebrate, skin } = useSohn();
  const { positionId: positionIdRaw } = useParams();
  const positionId = Number(positionIdRaw);
  const nav = useNavigate();
  // B-522: kommt das Kind vom verpatzten Test, feiert der Leerbildschirm nicht. Ein Neuladen verliert den State – hinnehmbar.
  // Einmal gelesen und danach aus dem Verlaufseintrag entfernt (Code-Review): sonst unterdrückte ein späteres „Zurück"
  // auf diese Seite die Feier erneut, obwohl das Kind gar nicht vom Test kommt.
  const location = useLocation();
  const [nachTest] = useState(() => (location.state as { nachTest?: boolean } | null)?.nachTest === true);
  useEffect(() => {
    if ((location.state as { nachTest?: boolean } | null)?.nachTest)
      nav({ pathname: location.pathname, search: location.search }, { replace: true, state: null });
  }, [location, nav]);

  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [phase, setPhase] = useState<Phase>("loading");
  const [error, setError] = useState<string | null>(null);
  const [cards, setCards] = useState<PracticeCard[]>([]);
  const [idx, setIdx] = useState(0);
  // B-597: Ob das Gerät die aktuelle Karte diktieren kann – hier oben, weil die Form erst nach den frühen Ausstiegen entsteht.
  const diktatStimme = useVoiceFor(cards[idx]?.dictationLang ?? "");
  const [typedAnswer, setTypedAnswer] = useState("");
  const [combo, setCombo] = useState(0);
  const [earned, setEarned] = useState(0);
  const [toast, setToast] = useState<string | null>(null);
  // Was das Blatt zeigt (B-526); `null`, solange keine geprüfte Antwort ansteht.
  const [blatt, setBlatt] = useState<Rueckmeldung | null>(null);
  // Genauigkeit des Rundenendes: nur Antworten, die der Server geprüft hat – ein selbst vergebenes „Gewusst" ist
  // keine Genauigkeit (Entscheidung 9).
  const [geprueft, setGeprueft] = useState({ richtig: 0, gesamt: 0 });
  const continueRef = useRef<HTMLButtonElement>(null);
  // B-419: „Jetzt abfragen" startet eine neue Runde derselben Position – eine neue Sitzung, darum ein neuer Schlüssel
  // für den Start und ein neuer Lauf des Heartbeat-Effekts, der die alte Sitzung schließt.
  const [round, setRound] = useState(0);
  // B-643: die Karte je Platz der Runde, bevor sie leichter geschaltet wurde – ihre Wiederholung beginnt wieder vorn.
  const vorDemSchalten = useRef(new Map<number, PracticeCard>());
  // B-635: der Stand einer Nacharbeit (Runde, erledigt, gesamt) – vom Server beim Start, „erledigt“ wächst je Antwort.
  const [nacharbeit, setNacharbeit] = useState<{ round: number; done: number; total: number } | null>(null);
  // B-686: der Buchbezug (B-588) stand auf dem Blatt vor dem Start; das Blatt ist weg, also steht er im Kopf der Runde.
  // Beiwerk: scheitert das Lesen, fehlt nur die Pille.
  const [buchbezug, setBuchbezug] = useState<string | null>(null);
  useEffect(() => {
    setBuchbezug(null);
    if (!planId || !positionId) return;
    let aktiv = true;
    api.overview(planId).then((o) => {
      const pos = o.today.positions.find((p) => p.positionId === positionId);
      if (aktiv && pos?.bookPages)
        setBuchbezug(t("pfad.bookRef", { series: pos.seriesName ?? "", unit: pos.seriesUnitLabel, pages: pos.bookPages }));
    }).catch(() => {});
    return () => { aktiv = false; };
  }, [planId, positionId, t]);
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
      setNacharbeit(sess.remediation ?? null);
      setCards(due);
      // B-635: eine Nacharbeits-Runde des Tages wird fortgesetzt, nicht neu begonnen – an der Karte, an der sie stand.
      if (sess.remediation && sess.cursor > 0 && sess.cursor < due.length) setIdx(sess.cursor);
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

  // B-420: solange das Blatt steht, gehört der Fokus dem „Weiter" – so trägt Enter das Kind weiter, und die Ansage der
  // Lösung (Live-Region) wird nicht von einem Feld übertönt, das nichts mehr annimmt.
  useEffect(() => { if (phase === "blatt") continueRef.current?.focus(); }, [phase]);
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
  const landingRef = useRef<HTMLDivElement>(null);
  const prevIdx = useRef(idx);
  useEffect(() => {
    // B-531: jeder Wechsel, den das Kind auf einer Karte auslöst – Aufdecken (`front → back`), Urteil (`back → front`),
    // „Weiter" nach dem Blatt und das „Weiter" einer Kennenlernkarte (`front → front`, nur `idx` wechselt) –, nimmt den
    // fokussierten Knopf mit. Der erste Aufbau nach dem Laden zählt nicht dazu (B-443), außer nach „Jetzt abfragen".
    const fromCard = prevPhase.current === "front" || prevPhase.current === "back" || prevPhase.current === "blatt";
    const changed = phase !== prevPhase.current || idx !== prevIdx.current;
    const toCard = (phase === "front" || phase === "back")
      && ((fromCard && changed) || (prevPhase.current === "loading" && focusAfterLoad.current));
    if (toCard)
      // Das erste Bedienbare der Antwortzone – Feld, erste Option oder, bei einer Karte ohne Eingabe, „Aufdecken" bzw.
      // „Weiter": welche Stufe kommt, entscheidet der Server.
      answerRef.current?.querySelector<HTMLElement>("input, button:not([disabled])")?.focus();
    // Endet die neue Runde leer oder im Fehler, verschwindet „Jetzt abfragen" ebenso – der Fokus geht an die Überschrift
    // bzw. die Meldung (B-515). Jedes Ende des Ladens verbraucht die Absicht, nicht nur der Weg zur Karte.
    if (prevPhase.current === "loading" && focusAfterLoad.current && (phase === "empty" || phase === "error"))
      landingRef.current?.focus();
    // B-541: endet der Plan mitten in der Runde (`plan_inactive`), verschwindet die Karte samt fokussiertem Knopf – der
    // Fokus geht an die Meldung, nicht nur nach dem Laden.
    if (fromCard && phase === "error") landingRef.current?.focus();
    if (prevPhase.current === "loading" && phase !== "loading") focusAfterLoad.current = false;
    // „done" → „done" gibt es hier nicht: im Rundenende wechselt weder Phase noch Karte.
    if (phase === "done") doneRef.current?.focus();
    prevPhase.current = phase;
    prevIdx.current = idx;
  }, [phase, idx]);

  /*
   * B-532: scheitert die Bewertung (außer `plan_inactive`), hält die Karte an, statt still weiterzurücken – das Kind soll
   * merken, dass nichts gezählt hat. `payload` ist genau das, was nicht angekommen ist: „Noch einmal senden" schickt es
   * unverändert, auch wenn das Feld inzwischen anders aussähe.
   */
  const [failed, setFailed] = useState<{ payload: ReviewAntwort; text: string } | null>(null);
  const resendRef = useRef<HTMLButtonElement>(null);
  // Der Knopf, der die Bewertung schickte, ist gesperrt oder verschwunden – der Fokus geht an „Noch einmal senden" (B-531).
  useEffect(() => { if (failed) resendRef.current?.focus(); }, [failed]);

  /** B-419: eine neue Runde derselben Position – nach dem Kennenlernen die Abfrage. */
  function startNextRound() {
    focusAfterLoad.current = true;
    setPhase("loading");
    setCards([]);
    vorDemSchalten.current = new Map();
    setIdx(0);
    setEarned(0);
    setCombo(0);
    setGeprueft({ richtig: 0, gesamt: 0 });
    // Die Sperre nach dem letzten Blatt gilt der alten Runde: sonst schluckte sie den ersten Tipp der neuen (B-598 – seit
    // die Lernkarte ein Blatt zeigt, endet eine Kennenlern-Runde auf einem).
    ruheBis.current = 0;
    setRound((r) => r + 1);
  }

  /*
   * Eine Bewertung je Karte, seit B-49 über `useAction` gesperrt (Ref, nicht State - dieselbe Begründung
   * wie überall sonst: `busy` als State steht erst nach dem Re-Render am Knopf). Seit die Auswahl
   * ausgespielt wird (B-73), stehen hier drei Knöpfe nebeneinander statt einer – zwei schnelle Tipper
   * schickten sonst zwei `review` auf denselben `itemIndex` **und** zwei `next()`, der Zähler sprang von
   * 1/5 auf 3/5 und Karte 2 kam nie.
   */
  async function judge(card: PracticeCard, payload: ReviewAntwort) {
    if (!planId || !session.current) return;
    // `next()` läuft bewusst außerhalb von `action.run`: das innere `try/catch` fängt jeden Fehler selbst, und
    // `action.run` bekommt darum nie eine Ablehnung zu sehen. `false` heißt hier also nur „von der Sperre verworfen" –
    // dann gehört das Weiterschalten dem laufenden Aufruf, sonst überspränge ein doppeltes Enter eine Karte (B-284).
    // Ein Fehlschlag hält an (B-532); `plan_inactive` (B-330) beendet die Runde mit dem Satz der Arcade.
    let planStopped = false;
    // B-420/B-526: eine vom Server GEPRÜFTE Antwort hält an und zeigt das Blatt. Die Selbsteinschätzung nicht – dort stand
    // die Lösung schon auf der Rückseite, bevor das Kind urteilte, und das Blatt wiederholte nur seinen eigenen Tipp.
    let halted = false;
    const ran = await action.run(async () => {
      try {
        const outcome = await api.review(planId, positionId, session.current!.id, { itemIndex: card.itemIndex, ...payload });
        setFailed(null);
        // B-598: eine aufgegebene oder falsch erinnerte Lernkarte kommt einmal ans Rundenende. Der Server hängt sie an seine
        // Folge; die Runde hier hat ihre Karten schon geladen und hängt dieselbe Karte an – als Wiederholung, nicht als neu.
        // B-643: eine leichter geschaltete Lernkarte kommt als GANZE Lernkarte wieder – also in ihrer Form vor dem Schalten.
        const urKarte = vorDemSchalten.current.get(idx) ?? card;
        if (outcome?.repeatsAtEnd) setCards((prev) => [...prev, { ...urKarte, introduction: false }]);
        // B-635: frei getippt richtig – die Aufgabe ist erledigt. Alles andere kommt in der nächsten Runde wieder.
        if (outcome?.taskDone) setNacharbeit((n) => (n ? { ...n, done: n.done + 1 } : n));
        // B-636: die letzte Aufgabe ist erledigt – die Nacharbeit endet mit ihrem Abschlussbonus. Er steht dauerhaft im Blatt,
        // in der Ansage und in der Rundensumme: der Jubel allein ist aria-hidden und fehlt bei reduzierter Bewegung.
        const abschluss = outcome?.completionBonus ?? 0;
        const naechsteRunde = nacharbeit !== null && !outcome?.taskDone;
        setCombo(outcome?.combo ?? 0);
        const feedback = reviewFeedback(outcome, card.displayOnly ?? false);
        const getippt = payload.givenAnswer !== undefined;
        if (feedback.kind === "correct") {
          setEarned((e) => e + feedback.awarded + feedback.comboBonus + feedback.speedBonus + abschluss);
          if (abschluss > 0) {
            celebrate("big", "🏆", t("practice.allTasksDone"), t("practice.completionBonus", { bonus: abschluss }));
          } else if (feedback.comboBonus > 0) {
            const tier = feedback.combo >= 10 ? "big" : "medium";
            celebrate(tier, tier === "big" ? "🥷" : "🎉", `COMBO ×${feedback.combo}`, t("practice.comboBonus", { bonus: feedback.comboBonus }));
          } else {
            celebrate("small", SMALL_EMOJI[feedback.combo % SMALL_EMOJI.length]);
          }
          refreshWallet();
          if (getippt) {
            halted = true;
            // Die Lernkarte wertet nichts (B-598, Entscheidung 2) – sie zählt nicht in die Genauigkeit der Runde.
            if (!card.learnCard) setGeprueft((g) => ({ richtig: g.richtig + 1, gesamt: g.gesamt + 1 }));
            setBlatt({
              richtig: true, gegeben: payload.givenAnswer ?? "", loesung: null, muenzen: feedback.awarded + abschluss,
              comboBonus: feedback.comboBonus, tempoBonus: feedback.speedBonus, lob: feedback.combo,
              // Eine Lernkarte bewegt keinen Kasten: ein Fälligkeitsdatum sähe wie ein Leitner-Termin aus, den es nicht gibt.
              faelligAm: card.learnCard ? null : outcome?.dueOn ?? null, erklaerung: outcome?.explanation ?? null,
              naechsteRunde, nochmal: outcome?.repeatsAtEnd ?? false,
            });
            // Die Ansage für die Region, die schon dasteht – das Blatt selbst ist keine (B-134).
            setToast(abschluss > 0
              ? `${t("lektion.right")} ${t("practice.allTasksDone")} ${t("practice.completionBonus", { bonus: abschluss })}`
              : outcome?.taskDone ? `${t("lektion.right")} ${t("practice.taskDone")}`
              : feedback.awarded > 0 ? `${t("lektion.right")} +${feedback.awarded} 🪙` : t("lektion.right"));
            setPhase("blatt");
          }
        } else if (feedback.kind === "wrong" && getippt) {
          // Ohne getroffenen Eintrag gibt es keine Lösung zu nennen: bei einer Menge (ungeordnete Liste) wäre
          // „Lösung: Hessen" willkürlich, solange ein Dutzend Einträge offen ist – und verriete einen, der noch
          // gefragt wird. Der Server liefert dann `null`, und es bleibt bei der schlichten Absage.
          halted = true;
          if (!card.learnCard) setGeprueft((g) => ({ ...g, gesamt: g.gesamt + 1 }));
          setBlatt({
            richtig: false, gegeben: payload.givenAnswer ?? "", loesung: feedback.expected, muenzen: 0, comboBonus: 0,
            tempoBonus: 0, lob: 0, faelligAm: null, falschGeschrieben: feedback.misspelled,
            erklaerung: outcome?.explanation ?? null, nochmal: outcome?.repeatsAtEnd ?? false, naechsteRunde,
          });
          const absage = feedback.expected ? t("practice.solution", { expected: feedback.expected }) : t("practice.wrong");
          setToast(feedback.misspelled ? `${t("test.misspelled")}. ${absage}` : absage);
          setPhase("blatt");
        }
        // feedback.kind === "none": kein Outcome ODER eine Anzeigenurstufe (ShowBoth) - kein Urteil, kein Blatt.
      } catch (e) {
        if (e instanceof ApiError && e.code === "plan_inactive") {
          planStopped = true;
          setError(errorMessage(e)); setPhase("error");
        } else {
          // B-532: die Runde endet nicht (B-330/5), aber sie rückt auch nicht still weiter – das Kind entscheidet.
          // Ein zweites Senden zahlt nicht doppelt: der Server wertet je Karte und Tag höchstens einmal.
          halted = true;
          setFailed({ payload, text: errorMessage(e) });
        }
      }
    });
    if (ran && !planStopped && !halted) next();
  }

  /*
   * B-635: „Mach's mir leichter“ – der Server schaltet die aktuelle Karte eine Stufe tiefer und schickt sie in der neuen
   * Form; sie ersetzt die Karte an ihrer Stelle. Das Getippte gehört zur alten Form und wird verworfen. Dieselbe Sperre wie
   * die Bewertung: beides ändert dieselbe Karte.
   */
  async function easier(card: PracticeCard) {
    if (!planId || !session.current) return;
    await action.run(async () => {
      // Die Arcade zeigt `action.message` nicht – ein Fehlschlag (`cannot_ease`, Netz) geht darum in die Ansage-Region,
      // wie beim Bildwechsel; sonst täte der Knopf sichtbar nichts.
      try {
        const eased = await api.easier(planId, positionId, session.current!.id, card.itemIndex);
        if (!vorDemSchalten.current.has(idx)) vorDemSchalten.current.set(idx, card);
        fokusNachLeichter.current = true;
        setCards((prev) => prev.map((c, i) => (i === idx ? eased : c)));
        setTypedAnswer("");
      } catch (e) {
        setToast(errorMessage(e));
      }
    });
  }

  /*
   * Nach dem Leichterschalten wechseln weder Phase noch Karte, also greift die Fokus-Regel oben nicht: der Knopf bliebe
   * fokussiert (getippte Buchstaben gingen ins Leere) oder verschwände samt Fokus (letzte Stufe). Der Fokus geht in die
   * neue Antwortform – Feld, erstes Kästchen, erste Kachel oder erste Option.
   */
  const fokusNachLeichter = useRef(false);
  useEffect(() => {
    if (!fokusNachLeichter.current) return;
    fokusNachLeichter.current = false;
    answerRef.current?.querySelector<HTMLElement>("input, .lettertiles-pool button, .lektion-option")?.focus();
  }, [cards]);

  /*
   * Nach dem Blatt steht der erste Knopf der nächsten Karte dort, wo eben „Weiter" stand. Ein Doppeltipp (oder ein
   * gehaltenes Enter) träfe ihn: eine Kennenlern-Karte wäre gebucht, ohne gesehen zu sein, eine Selbsteinschätzung
   * aufgedeckt, bevor die Frage gelesen ist (Review B-526). Für einen Augenblick nehmen diese beiden Knöpfe darum
   * nichts an. Getippte Karten brauchen das nicht – ihr Hauptknopf ist gesperrt, solange nichts eingegeben ist.
   */
  const ruheBis = useRef(0);
  const zuFrueh = () => performance.now() < ruheBis.current;

  function next() {
    if (phase === "blatt") ruheBis.current = performance.now() + NACH_BLATT_MS;
    setTypedAnswer("");
    setHintShown(false);
    setBlatt(null);
    setToast(null);
    setFailed(null);
    if (idx + 1 >= cards.length) {
      if (planId) api.overview(planId).then((o) => setStreak(o.currentStreak)).catch(() => {});
      setPhase("done");
    } else {
      setIdx((i) => i + 1);
      setPhase("front");
      // B-530: ab jetzt ist die nächste Karte sichtbar – die Lesezeit auf dem Blatt zählt nicht in ihre Antwortzeit. Der
      // Server stempelt mit seiner Uhr. Fire-and-forget: ein Fehlschlag kostet höchstens den Tempo-Bonus, nie die Runde.
      // Die Meldung nennt ihre Karte: überholt die Antwort sie, verwirft der Server den Stempel, statt ihn der
      // übernächsten Karte als Startzeit unterzuschieben (Code-Review B-530).
      const sichtbar = cards[idx + 1];
      if (planId && session.current) api.cardShown(planId, positionId, session.current.id, sichtbar.itemIndex).catch(() => {});
    }
  }

  /*
   * B-286: Die Ansage der Aufgabe steht ÜBER allen Ansichten (Laden, Fehler, Karte) und ist darum über die ganze
   * Sitzung DERSELBE Knoten. Nur so wächst jede Aufgabe in eine schon vorhandene Region hinein (B-134) – auch die erste
   * und die nach einem Fehler fortgesetzte; eine Region in der Karte entstünde dort zusammen mit ihrem Text und bliebe
   * stumm.
   */
  const asked = phase === "front" || phase === "back" || phase === "blatt" ? cards[idx] : null;
  return (
    <>
      {/* B-597: beim Diktat sagt die Ansage die Aufgabe, nicht das deutsche Wort, das die Karte verbirgt. */}
      <TaskAnnouncer text={asked && istDiktat(asked, diktatStimme !== null) ? t("lektion.listen") : asked?.prompt}
        gapIndex={asked?.gapIndex} />
      {view()}
    </>
  );

  function view() {
  if (phase === "loading") return <div className="sohn-body"><div className="loading">{t("practice.loading")}</div></div>;
  if (phase === "error") return <div className="sohn-body"><div className="error-box" ref={landingRef} tabIndex={-1} role="alert">{error}</div>
    <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button></div>;

  // B-117: eine Anzeigestufe (ShowBoth, B-96) kennt keinen Test - die Klausur antwortet dort mit
  // `stage_not_testable`. Ohne dieses Gate bot der Bildschirm den Knopf trotzdem an und schickte in die
  // rohe Fehlerseite, die exakt dieselbe Fehlerklasse ist, die B-114 an der Tageskarte schon behoben hat.
  // B-522: `testable` heißt Tag/Stufe, nicht Restversuche – den Test bietet der Knopf nur an, wenn heute noch einer startbar
  // ist (dieselbe Zählung, mit der der Server den Start sperrt). Ein älterer Server ohne das Feld sperrt nichts.
  const versucheUebrig = (session.current?.testAttemptsLeft ?? 1) > 0;
  const testable = (session.current?.testable ?? true) && versucheUebrig;
  const ohneFeier = nachTest || !versucheUebrig;

  if (phase === "empty") return (
    <div className="sohn-body">
      <div className="card" style={{ textAlign: "center" }}>
        <div className="screen-title" ref={landingRef} tabIndex={-1}>{t(ohneFeier ? "practice.nothingDueAfterTest" : "practice.nothingDue")}</div>
        <p className="sub">{!versucheUebrig
          ? t("practice.attemptsUsedUp")
          : testable
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
        <div aria-hidden="true"><Mascot skin={skin} mood="hyped" size={96} /></div>
        <div className="vtitle win" role="heading" aria-level={1} ref={doneRef} tabIndex={-1}>{t("practice.roundDone")}</div>
        {introduced > 0 && <p className="sub" style={{ textAlign: "center" }}>{t("practice.introduced", { count: introduced })}</p>}
        {/* Eine Runde kann neue und schon bekannte Karten mischen (die Einführung gilt je Karte): was die
            abgefragten verdient haben, bleibt sichtbar. Nur eine reine Kennenlern-Runde hat nichts zu zeigen. */}
        {/* Lernkarten (B-598) zahlen nichts und sind keine Abfrage – auch ihre Wiederholung am Rundenende nicht. */}
        {cards.some((c) => !c.introduction && !c.learnCard) && (
          <div className="reward">
            <div className="card">🪙<span style={{ color: "var(--gold)" }}>+{earned}</span></div>
            {/* Genauigkeit nur aus Antworten, die der Server geprüft hat; gab es keine, fehlt die Kachel. Keine Zeit:
                eine Stoppuhr am Ende lädt zum Hetzen ein (Entscheidung 9). */}
            {geprueft.gesamt > 0 && (
              <div className="card" role="img" aria-label={t("practice.accuracy", { pct: Math.round((geprueft.richtig / geprueft.gesamt) * 100) })}>
                <span aria-hidden="true">🎯</span><span aria-hidden="true">{Math.round((geprueft.richtig / geprueft.gesamt) * 100)} %</span>
              </div>
            )}
          </div>
        )}
        {introduced > 0 && <button type="button" className="btn lime" onClick={startNextRound} style={{ marginTop: 10 }}>{t("practice.quizNow")}</button>}
        {testable && <button type="button" className="btn gold" onClick={() => nav(`/sohn/test/${positionId}`)} style={{ marginTop: 10 }}>{t("practice.onToTest")}</button>}
        <button type="button" className="btn ghost" onClick={() => nav("/sohn")}>{t("progress.toHome")}</button>
      </div>
    </div>
  );

  const card = cards[idx];
  // Eine Quelle für „getippt“: dieselbe Form, die Anweisung und Darstellung wählt. Sonst Flip-Karte
  // (Selbsteinschätzung) oder ShowBoth (sofort offen, kein Urteil).
  const form = aufgabenform(card, diktatStimme !== null);
  // Die Lernkarte (B-598) schreibt zwar, bringt ihre Lösung aber mit – sie hat ihre eigene Fläche, keine getippte.
  const typed = form.antwort !== "selbst" && form.antwort !== "kennenlernen" && form.antwort !== "lernkarte";
  // Steht die Meldung (B-532), schickt Enter im Feld nichts: was noch einmal geht, entscheiden die beiden Knöpfe darunter.
  const submitTyped = () => { if (typedAnswer.trim() && !failed) judge(card, { givenAnswer: typedAnswer }); };

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
    <LektionsRahmen
      /*
        Sichtbarer Ausweg aus der laufenden Runde. Ohne Rückfrage, weil eine Rückfrage über eine Aktion, deren
        Wort schon die Wahrheit sagt, zum Wegklicken erzieht. Der Server-Aufruf (`end`) gehört bewusst NICHT
        hierher – das Cleanup des Heartbeat-Effekts oben schickt die Rest-Sekunden und beendet die Sitzung; ein
        zweiter Aufruf hier wäre eine Doppelquelle für dieselbe Sache. Folgenlos ist das Verlassen nur beim
        Leitner-Üben (jede gespielte Karte ist schon gebucht): bei einer Übung ohne automatische Prüfung misst
        die Pflicht EINE Sitzung gegen die Schwelle, und die nächste Runde beginnt wieder bei null.
      */
      schliessenName={t("practice.endRound")} onSchliessen={() => nav("/sohn")}
      zaehler={t("practice.cardOf", { n: idx + 1, total: cards.length })} anteil={idx / cards.length}
      unterKopf={
        <>
          {/* Combo und Tempo in einer eigenen Zeile: im Kopf neben Balken und Zähler stünde er bei 360 Pixeln über. */}
          <div className="row lektion-unterkopf">
            {buchbezug && <span className="pill pfad-buchbezug">{buchbezug}</span>}
            {nacharbeit && <span className="pill cyan">{t("practice.remediation", nacharbeit)}</span>}
            {combo >= 2 && <span className="pill mag">⚡ COMBO ×{combo}</span>}
            <button type="button" className={`pill toggle-pill ${tempo ? "lime" : ""}`} style={{ marginLeft: "auto" }} onClick={toggleTempo} aria-pressed={tempo}>
              {t("practice.tempo")}
            </button>
          </div>
          {/* Tempo-Modus: Countdown-Leiste je Karte (rein visueller Ansporn; der Schnell-Bonus zählt serverseitig). */}
          {tempo && typed && phase === "front" && <div className="tempo-bar" key={idx}><i /></div>}
        </>
      }
      anweisung={t(form.anweisung)}
    >
      {/* Nur die Selbsteinschätzung kommt auf „back" – ShowBoth ist von Anfang an offen und dreht nichts (B-35). */}
      <Aufgabe
        karte={card} form={form} skin={skin} gedreht={phase === "back"}
        /*
          Das Bild kommt nur auf Stufen, auf denen es die Lösung nicht verraten kann – der Server entscheidet das,
          das Frontend rendert nur, was da ist.
        */
        bild={card.imageUrl && (
          <CardImage
            key={card.imageUrl}
            url={card.imageUrl}
            alt={card.imageAlt ?? ""}
            // Solange das Blatt steht, liefe die Meldung eines 409 in die stumme Region (sr-only) – dort kein Wechsel.
            busy={action.busy || phase === "blatt"}
            onReshuffle={reshuffleImage}
          />
        )}
        /* Der Tipp gehört VOR die Antwort (Entscheidung 8): unter dem Blatt gibt es den Knopf nicht mehr – ein schon
           aufgedeckter Tipp bleibt als Text stehen. */
        hinweis={card.hint && typed && (
          hintShown
            ? <div className="sub">💡 {card.hint}</div>
            : phase !== "blatt" && <button type="button" className="btn ghost small" style={{ marginTop: 6 }} onClick={() => setHintShown(true)}>{t("practice.hint")}</button>
        )}
        /* ShowBoth (B-96): eine freie Anzeigestufe zeigt beide Seiten sofort – der Server meldet das über
           `displayOnly`, anders als die Selbsteinschätzung, die erst nach „Aufdecken" auflöst. */
        aufloesung={(phase === "back" || card.displayOnly) && card.reveal && (
          <>
            <div className="rev">→ {card.reveal}</div>
            <RevealAlternatives alternatives={card.revealAlternatives} />
          </>
        )}
      />

      <div className="lektion-antwort" ref={answerRef}>
        {phase === "blatt" && blatt ? (
          // B-420/B-526: die Karte bleibt stehen – Frage oben, darunter das Blatt mit der Rückmeldung, bis „Weiter".
          <RueckmeldungsBlatt r={blatt} skin={skin} weiterRef={continueRef} onWeiter={next} />
        ) : (
          <>
            {form.antwort === "lernkarte" ? (
              // Je Karte frisch eingehängt: die Schritte beginnen bei jeder Lernkarte (auch der Wiederholung) vorn.
              !failed && <Lernkarte key={`${round}-${idx}`} karte={card} gesperrt={action.busy} zuFrueh={zuFrueh}
                onFertig={(ergebnis) => judge(card, ergebnis)} onLeichter={() => easier(card)} />
            ) : typed ? (
              // Die Kennung trägt Runde und Karte: die Markierung einer Auswahl darf nicht auf die nächste Karte wandern.
              // Dieselbe Stelle im Baum mit und ohne Meldung (B-532): sonst baute React die Fläche neu auf, und eine
              // gewählte Option verlöre ihre Markierung. Steht die Meldung, ist das Feld nur noch lesbar – „Noch einmal
              // senden" schickt, was nicht ankam, nicht, was danach im Feld stünde.
              <>
                <Antwortflaeche
                  kartenId={`${round}-${idx}-${card.stage}`} karte={card} wert={typedAnswer} onWert={setTypedAnswer} gesperrt={action.busy || !!failed}
                  knopf={t("practice.check")} feldName={t("practice.yourAnswer")} platzhalter={t("practice.answerPlaceholder")}
                  onAbschicken={submitTyped}
                />
                {/* B-635: nur wo der Server es anbietet (Nacharbeit, noch nicht die leichteste Form) – die Stufe wählt er.
                    Hinter der Antwortfläche, damit der Fokus nach „Weiter" im Feld landet und nicht hier; dieselbe Ruhe
                    nach dem Blatt wie bei „Aufdecken": ein gehaltenes Enter schaltet keine ungelesene Karte leichter. */}
                {card.canEase && !failed && (
                  <div className="lektion-fuss">
                    <button type="button" className="btn ghost small" disabled={action.busy}
                      onClick={() => { if (!zuFrueh()) easier(card); }}>
                      {t("practice.easier")}
                    </button>
                  </div>
                )}
              </>
            ) : !failed && (
              <div className="lektion-fuss">
                {card.displayOnly ? (
                  // Kennenlernen: kein Urteil zu fällen, also kein „Gewusst?" – nur weiter.
                  <button type="button" className="btn lime" disabled={action.busy} onClick={() => { if (!zuFrueh()) judge(card, {}); }}>{t("practice.next")}</button>
                ) : (
                  // Dieselbe Selbsteinschätzung wie im Test: erst aufdecken, dann urteilen. Die Lösung steht in der Karte
                  // darüber. B-49: die Urteils-Knöpfe sind während einer Bewertung gesperrt.
                  <SelfAssessAnswer
                    reveal={card.reveal} alternatives={card.revealAlternatives} revealed={phase === "back"} loesungInDerKarte
                    busy={action.busy} texte={{ aufdecken: t("practice.flip"), nein: t("practice.again"), ja: t("practice.knewIt") }}
                    onReveal={() => { if (!zuFrueh()) setPhase("back"); }} onJudge={(wasKnown) => judge(card, { wasKnown })}
                  />
                )}
              </div>
            )}
            {failed && (
              // B-532: nicht angekommen – das Kind entscheidet, ob es noch einmal sendet oder weitergeht.
              <>
                <div className="error-box" role="alert">{failed.text}</div>
                <div className="lektion-fuss">
                  <button type="button" className="btn lime" ref={resendRef} disabled={action.busy} onClick={() => judge(card, failed.payload)}>{t("practice.resend")}</button>
                  <button type="button" className="btn ghost" disabled={action.busy} onClick={next}>{t("practice.skip")}</button>
                </div>
              </>
            )}
          </>
        )}
      </div>

      {/* B-629: an jeder Karte im Üben; je Karte frisch, damit Auswahl und Dank nicht auf die nächste wandern. */}
      {planId && <KartenMeldung key={`${round}-${idx}`} planId={planId} positionId={positionId} itemIndex={card.itemIndex} />}

      {/* Solange das Blatt steht, sagt die Region nur an, was dort schon zu lesen ist; sichtbar wird sie allein für
          die Meldung des Bildwechsels. Sie steht immer da – eine Region, die erst mit ihrem Text entsteht, bleibt stumm. */}
      <div className={`live-region${toast ? (phase === "blatt" ? " sr-only" : " toast") : ""}`} role="status" aria-live="polite">{toast}</div>
    </LektionsRahmen>
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
