import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { AudioButton } from "../components/AudioButton";
import { SpeakButton } from "../components/SpeakButton";
import { normalizeAnswer } from "../lib/answerNormalize";
import type { PracticeCard } from "../lib/types";

/** Was die Lernkarte am Ende an den Server schickt: beide Ergebnisse in einem Aufruf (B-598, Entscheidung 6). */
export interface LernkartenErgebnis {
  wasKnown: boolean;
  /** Die Eingabe aus dem Gedächtnis; `null` heißt „aufgegeben“ (Entscheidung 8). */
  givenAnswer: string | null;
}

type Schritt = "aufdecken" | "gewusst" | "abschreiben" | "erinnern";

/**
 * Darf „OK“ beim Abschreiben gedrückt werden? Nach den Vergleichsregeln des Servers (Entscheidung 5): „Walk“ und
 * „don’t“ gelten für „walk“ und „don't“ – die Regel liegt in `normalizeAnswer`, die die gemeinsame Fallliste hält.
 */
export function abgeschrieben(eingabe: string, wort: string): boolean {
  return eingabe.trim() !== "" && normalizeAnswer(eingabe) === normalizeAnswer(wort);
}

/**
 * Die Lernkarte (B-598): ein neues Wort in vier Schritten. Das deutsche Wort steht schon in der Karte darüber.
 * 1. aufdecken; 2. das Fremdwort sehen und hören, „kannte ich“ oder „neu“; 3. abschreiben – „OK“ erst, wenn es stimmt;
 * 4. das Wort ist weg, aus dem Kopf schreiben oder aufgeben. Gewertet wird erst der letzte Schritt, beim Server.
 *
 * Vorgelesen wird nur, solange das Wort zu sehen ist (Entscheidung 10): in Schritt 4 wäre der Ton die Lösung.
 * `gesperrt` gilt, solange die Antwort unterwegs ist – derselbe Wartepunkt wie bei jedem anderen Knopf der Runde.
 */
export function Lernkarte({ karte, gesperrt, zuFrueh = () => false, onFertig }: {
  karte: PracticeCard;
  gesperrt: boolean;
  /**
   * Die Sperre der Runde nach einem Blatt (NACH_BLATT_MS): ein gehaltenes Enter darf nicht durch „Aufdecken“ und
   * „Kannte ich?“ laufen und ein „neu“ buchen, das das Kind nie gesehen hat.
   */
  zuFrueh?: () => boolean;
  onFertig: (ergebnis: LernkartenErgebnis) => void;
}) {
  const { t } = useTranslation();
  const [schritt, setSchritt] = useState<Schritt>("aufdecken");
  const [kannte, setKannte] = useState(false);
  const [kopie, setKopie] = useState("");
  const [erinnert, setErinnert] = useState("");
  const fokus = useRef<HTMLElement | null>(null);
  const ersterSchritt = useRef(true);
  const wortId = useId();
  const frageId = useId();
  const wort = karte.reveal ?? "";

  // Jeder weitere Schritt bringt den Fokus mit – sonst stünde er auf einem Knopf, den es nicht mehr gibt (B-231). Den
  // ersten nicht: beim Laden der Seite setzt die Runde bewusst keinen Fokus (B-443), beim Kartenwechsel tut sie es selbst.
  useEffect(() => {
    if (ersterSchritt.current) { ersterSchritt.current = false; return; }
    fokus.current?.focus();
  }, [schritt]);

  // Steht nur im Block der Schritte 2 und 3, also nur, solange das Wort zu sehen ist.
  const vorlesenKnopf = (karte.revealAudioUrl
    ? <AudioButton url={karte.revealAudioUrl} label={t("lernkarte.listen")} autoPlay={false} />
    : karte.revealSpeechLang && <SpeakButton text={wort} lang={karte.revealSpeechLang} label={t("lernkarte.listen")} />);

  return (
    <div className="lernkarte">
      {schritt === "aufdecken" && (
        <div className="lektion-fuss">
          <button type="button" className="btn lime" ref={(el) => { fokus.current = el; }} disabled={gesperrt}
            onClick={() => { if (!zuFrueh()) setSchritt("gewusst"); }}>{t("lernkarte.uncover")}</button>
        </div>
      )}

      {(schritt === "gewusst" || schritt === "abschreiben") && (
        <div className="lernkarte-wort">
          <div className="rev" id={wortId}>→ {wort}</div>
          {vorlesenKnopf}
        </div>
      )}

      {schritt === "gewusst" && (
        // Die Gruppe heißt wie ihre sichtbare Frage und beschreibt sich mit dem Wort: ein Screenreader hört, was er
        // beurteilen soll, ohne es zu suchen.
        <div className="lektion-fuss" role="group" aria-labelledby={frageId} aria-describedby={wortId}>
          <p className="sub" id={frageId} style={{ margin: 0 }}>{t("lernkarte.knewIt")}</p>
          <button type="button" className="btn ghost" ref={(el) => { fokus.current = el; }} disabled={gesperrt}
            onClick={() => { if (zuFrueh()) return; setKannte(false); setSchritt("abschreiben"); }}>{t("lernkarte.new")}</button>
          <button type="button" className="btn lime" disabled={gesperrt}
            onClick={() => { if (zuFrueh()) return; setKannte(true); setSchritt("abschreiben"); }}>{t("lernkarte.known")}</button>
        </div>
      )}

      {schritt === "abschreiben" && (
        <form className="lektion-fuss" onSubmit={(e) => {
          e.preventDefault();
          if (abgeschrieben(kopie, wort)) setSchritt("erinnern");
        }}>
          <label className="sub" htmlFor="lernkarte-kopie">{t("lernkarte.copy")}</label>
          {/* `readOnly` statt `disabled`, und keine Autokorrektur – wie in der Antwortfläche (Lektion.tsx): ein gesperrtes
              Feld gäbe den Fokus ab und klappte am Handy die Tastatur zu, und iOS „korrigierte“ das Fremdwort. */}
          <input id="lernkarte-kopie" className="lektion-feld" autoComplete="off" autoCapitalize="off" autoCorrect="off"
            spellCheck={false} aria-describedby={wortId} ref={(el) => { fokus.current = el; }} value={kopie}
            readOnly={gesperrt} onChange={(e) => setKopie(e.target.value)} />
          <button type="submit" className="btn lime" disabled={gesperrt || !abgeschrieben(kopie, wort)}>{t("lernkarte.ok")}</button>
        </form>
      )}

      {schritt === "erinnern" && (
        <form className="lektion-fuss" onSubmit={(e) => {
          e.preventDefault();
          if (erinnert.trim()) onFertig({ wasKnown: kannte, givenAnswer: erinnert });
        }}>
          <label className="sub" htmlFor="lernkarte-erinnern">{t("lernkarte.recall")}</label>
          <input id="lernkarte-erinnern" className="lektion-feld" autoComplete="off" autoCapitalize="off" autoCorrect="off"
            spellCheck={false} ref={(el) => { fokus.current = el; }} value={erinnert}
            readOnly={gesperrt} onChange={(e) => setErinnert(e.target.value)} />
          <button type="submit" className="btn lime" disabled={gesperrt || !erinnert.trim()}>{t("lernkarte.ok")}</button>
          <button type="button" className="btn ghost" disabled={gesperrt}
            onClick={() => onFertig({ wasKnown: kannte, givenAnswer: null })}>{t("lernkarte.giveUp")}</button>
        </form>
      )}
    </div>
  );
}
