import { useState, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { Mascot } from "../components/Mascot";
import { LetterBoxes } from "../components/LetterBoxes";
import { LetterTiles } from "../components/LetterTiles";
import { AudioButton } from "../components/AudioButton";
import { ClozePrompt } from "../components/ClozePrompt";
import { ListRule } from "../components/ListRule";
import { Passage } from "../components/Passage";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import { CueBox } from "../components/CueBox";
import { choicesClass } from "../lib/choices";
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
  // Die Testkarte nennt ihren Typ nur optional.
  & { type?: string | null };

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
      <ClozePrompt text={karte.prompt} gapIndex={karte.gapIndex} fitAsked />
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
 */
export function Antwortflaeche({ karte, kartenId, wert, onWert, gesperrt, knopf, feldName, platzhalter, onAbschicken }: {
  karte: LektionsKarte;
  kartenId: string | number;
  wert: string;
  onWert: (wert: string) => void;
  /** Eine Bewertung läuft – nichts annehmen. */
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
        <LetterTiles tiles={karte.letterTiles} value={wert} onChange={onWert} onSubmit={abschicken} />
      ) : karte.answerLength ? (
        <LetterBoxes length={karte.answerLength} value={wert} onChange={onWert} onSubmit={abschicken} pattern={karte.answerPattern ?? undefined} />
      ) : (
        <form onSubmit={(e) => { e.preventDefault(); abschicken(); }}>
          <input
            className="lektion-feld" aria-label={feldName} name={`answer-${karte.itemIndex}`} placeholder={platzhalter}
            autoComplete="off" autoCapitalize="off" autoCorrect="off" spellCheck={false}
            value={wert} onChange={(e) => onWert(e.target.value)}
          />
        </form>
      )}
      <div className="lektion-fuss">
        <button type="button" className="btn lime" disabled={gesperrt || !wert.trim()} onClick={abschicken}>{knopf}</button>
      </div>
    </>
  );
}
