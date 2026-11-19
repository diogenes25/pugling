import { useRef, useState, type ReactNode, type Ref } from "react";
import { useTranslation } from "react-i18next";
import { Mascot } from "../components/Mascot";
import { LetterBoxes } from "../components/LetterBoxes";
import { LetterTiles } from "../components/LetterTiles";
import { AudioButton } from "../components/AudioButton";
import { SpeakButton } from "../components/SpeakButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { ListRule } from "../components/ListRule";
import { Passage } from "../components/Passage";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import { CueBox } from "../components/CueBox";
import { SonderzeichenLeiste } from "../components/Sonderzeichen";
import { choicesClass } from "../lib/choices";
import { arcadeDatum } from "../lib/datum";
import { markiereSchreibfehler } from "../lib/schreibfehler";
import { einsetzen, leistenZeichen } from "../lib/sonderzeichen";
import type { PracticeCard } from "../lib/types";
import type { Skin } from "../lib/skins";
import type { Aufgabenform } from "./aufgabenform";

/*
 * Der Aufbau einer Aufgabe in der Arcade (B-526): derselbe Rahmen für Üben und Test – Kopf, Arbeitsanweisung,
 * Frage, Antwortfläche, ein Hauptknopf am unteren Rand. Die Bausteine darin (Lückensatz, Kästchen, Wortkasten …)
 * bleiben, was sie sind, und gehören auch der Vater-Vorschau; dieser Rahmen gehört nur der Arcade.
 */

/** Was Übungs- und Testkarte gemeinsam tragen und der Rahmen anzeigt. */
export type LektionsKarte = Pick<PracticeCard, "itemIndex" | "prompt" | "gapIndex" | "passage" | "audioUrl"
  | "decoding" | "anyOrder" | "cueBox" | "choices" | "letterTiles" | "answerLength" | "answerPattern">
  // Die Testkarte nennt ihren Typ nur optional – und trägt keine Vorlese-Sprache (B-252: nur die Übungskarte).
  & { type?: string | null } & Partial<Pick<PracticeCard, "speechLang" | "dictationSpokenAnswer" | "dictationLang" | "keyboardLang"
    | "solvedGaps">>;

/**
 * Die in der Nacharbeit schon gelösten Lücken, ins Format von `ClozePrompt.filled`. Ohne sie stehen sie als „…“ da, und
 * eine Runde, die bei der ersten offenen Lücke weitermacht, sieht aus, als hätte sie die davor übersprungen.
 */
export function geloesteLuecken(solved: PracticeCard["solvedGaps"]): Record<number, string> | undefined {
  if (!solved?.length) return undefined;
  return Object.fromEntries(solved.map((g) => [g.gapIndex, g.text]));
}

/**
 * Kopf und Arbeitsanweisung. Das ✕ trägt kein Wort, aber einen Vorlesenamen, der sagt, was es hier tut: im Üben
 * endet die Runde, im Test bleibt der Versuch stehen – das sieht man dem Zeichen nicht an.
 */
export function LektionsRahmen({ test = false, schliessenName, onSchliessen, zaehler, anteil, unterKopf, anweisung, hinweis, children }: {
  test?: boolean;
  schliessenName: string;
  onSchliessen: () => void;
  /** „Karte 3 / 8“ – bleibt als Text am Balken: das Kind, der Screenreader und die Specs lesen ihn. */
  zaehler: string;
  /** Fortschritt 0…1 für den Balken. */
  anteil: number;
  unterKopf?: ReactNode;
  anweisung: string;
  hinweis?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className={`sohn-body lektion${test ? " test" : ""}`}>
      <div className="lektion-kopf play-head">
        <button type="button" className="lektion-x" aria-label={schliessenName} title={schliessenName} onClick={onSchliessen}>
          <span aria-hidden="true">✕</span>
        </button>
        <div className="bar gold lektion-balken" aria-hidden="true">
          <i style={{ width: `${Math.round(Math.min(1, Math.max(0, anteil)) * 100)}%` }} />
        </div>
        <span className="pill cyan">{zaehler}</span>
      </div>
      {unterKopf}
      <h1 className="screen-title lektion-anweisung">{anweisung}</h1>
      {hinweis && <p className="sub" style={{ margin: 0 }}>{hinweis}</p>}
      {children}
    </div>
  );
}

/**
 * Die Frage mit ihrem Material. Ein Wort oder Satz steht in der Sprechblase am Maskottchen; sobald Lesetext, Bild,
 * Wortkasten oder Dekodierung dabei sind, als volle Karte (`form.darstellung`). In beiden Fällen ist es dieselbe
 * `.fcard`, die sich beim Aufdecken dreht (B-35) – die Blase ist nur ihr Gewand.
 */
export function Aufgabe({ karte, form, skin, gedreht = false, bild, hinweis, aufloesung }: {
  karte: LektionsKarte;
  form: Aufgabenform;
  skin: Skin;
  gedreht?: boolean;
  /** Bild zur Karte (nur Üben) – steht über dem Wort, weil es beim Einprägen hilft. */
  bild?: ReactNode;
  hinweis?: ReactNode;
  /** Die Lösung, wo sie schon gezeigt werden darf (Kennenlernen, aufgedeckte Selbsteinschätzung im Üben). */
  aufloesung?: ReactNode;
}) {
  const { t } = useTranslation();
  const blase = form.darstellung === "blase";
  const karteEl = (
    <div className={`fcard${blase ? " blase" : ""}${gedreht ? " flipped" : ""}`}>
      {bild}
      <Passage text={karte.passage} />
      {/*
        Aufnahme und Frage nebeneinander, nicht entweder-oder: Beim Hörverstehen braucht das Kind beides. Fehlt der
        Text, IST die Aufnahme die Aufgabe: kurz, also anspielen und der Knopf genügt. Steht eine Frage daneben, ist
        sie das Material und kann lang sein – dann die Bedienelemente. Gelesen aus den Feldern, nicht aus dem Typ.
      */}
      {karte.audioUrl && <AudioButton url={karte.audioUrl} autoPlay={!karte.prompt} withControls={!!karte.prompt} />}
      {/* B-252: vorgelesen wird nur der Text, der ohnehin auf der Karte steht – ob und in welcher Sprache, sagt der Server. */}
      {karte.speechLang && karte.prompt && <SpeakButton text={karte.prompt} lang={karte.speechLang} />}
      {/*
        B-597: Beim Diktat liest das Gerät das Fremdwort vor, und die Karte zeigt keinen Text – auch das deutsche Wort
        nicht, sonst wäre es eine Übersetzung mit Hörhilfe. Ohne Stimme (form.diktat aus) steht das deutsche Wort da.
      */}
      {form.diktat && karte.dictationSpokenAnswer && karte.dictationLang
        ? <SpeakButton text={karte.dictationSpokenAnswer} lang={karte.dictationLang} label={t("lektion.dictatePlay")} />
        : <ClozePrompt text={karte.prompt} gapIndex={karte.gapIndex} filled={geloesteLuecken(karte.solvedGaps)} fitAsked />}
      {/* Die Dekodierung steht direkt unter dem Satz: sie ist die Methode, nicht die Lösung (B-78, B-93). */}
      <BirkenbihlDecoding decoding={karte.decoding} />
      <ListRule type={karte.type} anyOrder={karte.anyOrder} itemIndex={karte.itemIndex} />
      {hinweis}
      {/* B-524: Der Wortkasten ist die Aufgabe, keine Hilfe – darum offen, nicht hinter einem Tipp-Knopf. */}
      <CueBox words={karte.cueBox} label={t("test.cueBox")} />
      {aufloesung}
    </div>
  );
  if (!blase) return karteEl;
  return (
    <div className="lektion-frage">
      <div className="lektion-begleiter" aria-hidden="true"><Mascot skin={skin} mood="happy" size={64} /></div>
      {karteEl}
    </div>
  );
}

/**
 * Die Antwortfläche der getippten Formen – Auswahl, Buchstaben ordnen, Kästchen, Freitext – samt ihrem Hauptknopf,
 * geteilt von Üben und Test. Sie weiß nichts von richtig und falsch: gewertet wird beim Server, und im Test darf
 * hier nie etwas darüber stehen.
 *
 * Eine Auswahl wird durch Antippen nur **markiert** und bleibt änderbar; gewertet wird erst mit dem Hauptknopf
 * (Entscheidung 2). Gemerkt wird der Platz, nicht der Text: der Autor darf dieselbe Option zweimal eintragen.
 *
 * `kartenId` nennt die Karte. Wechselt sie, fällt die Markierung – aber die Fläche wird NICHT neu eingehängt: ein
 * `key` von außen risse das Eingabefeld aus dem DOM, der Fokus fiele auf `body`, und am Handy klappte nach jeder
 * Antwort die Tastatur zu (Review B-526; dieselbe Warnung steht an `LetterTiles`).
 *
 * B-661: unter Freitext und Kästchen steht die Sonderzeichen-Leiste der getippten Sprache (`keyboardLang`) – nur die
 * feste Menge der Sprache, nie die Zeichen der Lösung: hier wird aus dem Kopf geschrieben, auch im Test.
 */
export function Antwortflaeche({ karte, kartenId, wert, onWert, gesperrt, knopf, feldName, platzhalter, onAbschicken }: {
  karte: LektionsKarte;
  kartenId: string | number;
  wert: string;
  onWert: (wert: string) => void;
  /**
   * Nichts annehmen: eine Bewertung läuft, oder sie ist gescheitert und wartet auf „Noch einmal senden" (B-532). Das
   * Feld wird `readOnly`, nicht `disabled` – ein gesperrtes Feld gäbe den Fokus an `body` ab, und Enter erreichte das
   * Formular nicht mehr (die Sperre gegen ein doppeltes Absenden sitzt in `useAction`).
   */
  gesperrt: boolean;
  /** Beschriftung des Hauptknopfs: „Prüfen“ im Üben, „Weiter“ im Test. */
  knopf: string;
  feldName: string;
  platzhalter: string;
  onAbschicken: () => void;
}) {
  const { t } = useTranslation();
  const [markiert, setMarkiert] = useState<number | null>(null);
  // Zustand beim Rendern nachziehen statt in einem Effekt: sonst leuchtete die alte Markierung einen Render lang
  // an der neuen Karte.
  const [gesehen, setGesehen] = useState(kartenId);
  if (gesehen !== kartenId) { setGesehen(kartenId); setMarkiert(null); }
  const abschicken = () => { if (wert.trim()) onAbschicken(); };
  const feld = useRef<HTMLInputElement | null>(null);
  const zeichen = leistenZeichen(karte.keyboardLang);
  return (
    <>
      {karte.choices ? (
        <div className={choicesClass(karte.choices)} style={{ gap: 8, flexWrap: "wrap" }} role="group" aria-label={t("practice.choices")}>
          {karte.choices.map((c, i) => (
            <button
              type="button" key={`${i}-${c}`} className={`btn ghost lektion-option${markiert === i ? " gewaehlt" : ""}`}
              aria-pressed={markiert === i} disabled={gesperrt}
              onClick={() => { const an = markiert !== i; setMarkiert(an ? i : null); onWert(an ? c : ""); }}
            >{c}</button>
          ))}
        </div>
      ) : karte.letterTiles ? (
        <LetterTiles tiles={karte.letterTiles} value={wert} onChange={onWert} onSubmit={abschicken} gesperrt={gesperrt}
          pattern={karte.answerPattern} />
      ) : karte.answerLength ? (
        <LetterBoxes length={karte.answerLength} value={wert} onChange={onWert} onSubmit={abschicken} pattern={karte.answerPattern ?? undefined} gesperrt={gesperrt}
          sonderzeichen={zeichen} />
      ) : (
        <form onSubmit={(e) => { e.preventDefault(); abschicken(); }}>
          <input
            ref={feld}
            className="lektion-feld" aria-label={feldName} name={`answer-${karte.itemIndex}`} placeholder={platzhalter}
            autoComplete="off" autoCapitalize="off" autoCorrect="off" spellCheck={false}
            value={wert} onChange={(e) => onWert(e.target.value)} readOnly={gesperrt}
          />
          <SonderzeichenLeiste zeichen={zeichen} gesperrt={gesperrt}
            onZeichen={(z) => einsetzen(feld.current, wert, z, onWert)} />
        </form>
      )}
      <div className="lektion-fuss">
        <button type="button" className="btn lime" disabled={gesperrt || !wert.trim()} onClick={abschicken}>{knopf}</button>
      </div>
    </>
  );
}

/** Was das Blatt nach einer vom Server geprüften Antwort zeigt. */
export interface Rueckmeldung {
  richtig: boolean;
  /** Die eigene Antwort – bei falsch neben der Lösung, außer sie ist nur falsch geschrieben (dann markiert die Lösung den Fehler). */
  gegeben: string;
  /** `null`, wenn der Server keine nennt: in einer Menge wäre sie willkürlich und verriete einen offenen Eintrag. */
  loesung: string | null;
  muenzen: number;
  comboBonus: number;
  tempoBonus: number;
  /** Nächste Fälligkeit, wie der Server sie meldet (ISO-Tag); `null`, wo der Karteikasten nichts meldet. */
  faelligAm: string | null;
  /** Wählt das Lobwort; der Combo-Stand, damit es wechselt, ohne zu würfeln. */
  lob: number;
  /**
   * B-422: falsch, aber das richtige Wort falsch geschrieben (der Server meldet `misspelled`). Nur ein Etikett – die
   * Wertung bleibt falsch; das Kind soll sehen, dass es das Wort wusste und nur die Schreibweise fehlte.
   */
  falschGeschrieben?: boolean;
  /**
   * B-529: warum die Antwort richtig oder falsch war (heute der Regel-Hinweis einer Regelaufgabe). Der Server schickt
   * sie erst mit der Bewertung; `null`, wo der Inhalt keine kennt – dann steht auch keine leere Zeile da.
   */
  erklaerung?: string | null;
  /** B-598: eine Lernkarte, aufgegeben oder falsch erinnert, kommt am Ende der Runde noch einmal (der Server meldet es). */
  nochmal?: boolean;
  /**
   * B-635: eine Nacharbeits-Aufgabe, die diese Antwort nicht erledigt hat – falsch oder mit Hilfe gelöst. Sie kommt in der
   * nächsten Runde wieder; das steht bei richtig UND falsch da, denn „richtig“ mit Hilfe ist noch nicht erledigt.
   */
  naechsteRunde?: boolean;
}

/** Die Lösung; bei einem einfachen Verschreiben sind die Buchstaben hervorgehoben, an denen es lag. */
function Loesung({ r, loesung }: { r: Rueckmeldung; loesung: string }) {
  const stuecke = r.falschGeschrieben ? markiereSchreibfehler(r.gegeben, loesung) : null;
  if (!stuecke) return <>{loesung}</>;
  return <>{stuecke.map((s, k) => (s.falsch ? <mark key={k} className="lektion-fehlerstelle">{s.text}</mark> : s.text))}</>;
}

const LOB = ["lektion.praise1", "lektion.praise2", "lektion.praise3", "lektion.praise4"] as const;

/**
 * Das Blatt (B-526, Entscheidungen 3 und 8): die Rückmeldung des Übens an **einer** Stelle, grün oder rot, bis
 * „Weiter". Es ersetzt den Toast und die angehaltene Karte aus B-420, deren Regel es erbt: die Karte bleibt stehen,
 * bis das Kind gelesen hat. Der Test zeigt es nie.
 *
 * Das Blatt ist bewusst **keine** Live-Region: eine Region, die zusammen mit ihrem Text entsteht, bleibt stumm
 * (B-134). Die Ansage läuft über die Region des Bildschirms, die schon vorher dasteht.
 *
 * Verdiente Münzen stehen nur da, wenn es welche gab – eine nicht wertbare Karte (nicht fällig, heute schon
 * gewertet) darf keinen Gewinn vortäuschen.
 */
export function RueckmeldungsBlatt({ r, skin, weiterRef, onWeiter }: {
  r: Rueckmeldung;
  skin: Skin;
  weiterRef?: Ref<HTMLButtonElement>;
  onWeiter: () => void;
}) {
  const { t, i18n } = useTranslation();
  const boni = [
    r.comboBonus > 0 ? t("lektion.comboPlus", { count: r.comboBonus }) : null,
    r.tempoBonus > 0 ? t("lektion.speedPlus", { count: r.tempoBonus }) : null,
  ].filter(Boolean).join(" · ");
  return (
    <div className={`lektion-blatt ${r.richtig ? "richtig" : "falsch"}`}>
      <div className="row">
        <div className="lektion-begleiter" aria-hidden="true"><Mascot skin={skin} mood={r.richtig ? "hyped" : "happy"} size={48} /></div>
        <b className="lektion-blatt-titel">{r.richtig ? t(LOB[Math.abs(r.lob) % LOB.length]) : r.falschGeschrieben ? t("test.misspelled") : t("lektion.almost")}</b>
        {r.richtig && r.muenzen > 0 && <span className="pill gold" style={{ marginLeft: "auto" }}>+{r.muenzen} 🪙</span>}
      </div>
      {r.richtig ? (
        <>
          {boni && <div className="sub">{boni}</div>}
          {r.faelligAm && <div className="sub">{t("practice.nextDue", { date: arcadeDatum(r.faelligAm, i18n.language) })}</div>}
          {/* B-643: mit Hilfe richtig ist noch nicht eingeprägt – das Wort kommt als ganze Lernkarte wieder. */}
          {r.nochmal && <div className="sub">{t("lektion.learnAgain")}</div>}
        </>
      ) : (
        <>
          {/* Nicht durchgestrichen: das machte das Wort schwer lesbar, und „falsch“ sagt schon das rote Blatt. Bei einem
              Schreibfehler fällt die Zeile ganz weg – die Lösung markiert dann die Stelle. Aufgegeben (B-598) gibt es keine. */}
          {r.gegeben && !r.falschGeschrieben && <div className="sub">{t("practice.yourAnswerWas")} {r.gegeben}</div>}
          <div className="rev">{r.loesung ? <>{t("practice.solutionLabel")} <Loesung r={r} loesung={r.loesung} /></> : t("practice.wrong")}</div>
          {r.nochmal && <div className="sub">{t("lektion.learnAgain")}</div>}
        </>
      )}
      {/* Bei richtig UND falsch (B-529, Entscheidung 5): richtig bestätigt sie, warum. Der Inhalt ist Nutzerdaten und
          reist als Platzhalter. */}
      {r.naechsteRunde && <div className="sub">{t("lektion.nextRound")}</div>}
      {r.erklaerung && <div className="sub lektion-erklaerung">{t("lektion.rule", { text: r.erklaerung })}</div>}
      <button type="button" ref={weiterRef} className={`btn ${r.richtig ? "lime" : "red"}`} onClick={onWeiter}>{t("practice.next")}</button>
    </div>
  );
}
