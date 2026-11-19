import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { AudioButton } from "../components/AudioButton";
import { SpeakButton } from "../components/SpeakButton";
import { SonderzeichenLeiste } from "../components/Sonderzeichen";
import { normalizeAnswer } from "../lib/answerNormalize";
import { einsetzen, leistenZeichen, ohneDiakritika } from "../lib/sonderzeichen";
import type { PracticeCard } from "../lib/types";
import { Antwortflaeche } from "./Lektion";

/**
 * Was die Lernkarte am Ende an den Server schickt: das Erinnern (B-598, Entscheidung 6). Die Frage „Kanntest du das
 * Wort schon?“ ist gestrichen (B-686) – sie wurde aus Reflex mit „ja“ beantwortet und von nichts gelesen.
 */
export type LernkartenErgebnis =
  /** Die Eingabe aus dem Gedächtnis. */
  | { givenAnswer: string }
  /**
   * Aufgegeben (Entscheidung 8): keine Antwort, aber ausdrücklich gesagt – ohne `wasKnown` wäre eine leere Antwort für
   * den Server sonst „kein Urteil“ (B-433), und das Wort käme nicht wieder.
   */
  | { givenAnswer: null; gaveUp: true };

type Schritt = "aufdecken" | "abschreiben" | "erinnern";

/**
 * Darf „OK“ beim Abschreiben gedrückt werden? Nach den Vergleichsregeln des Servers (Entscheidung 5): „Walk“ und
 * „don’t“ gelten für „walk“ und „don't“ – die Regel liegt in `normalizeAnswer`, die die gemeinsame Fallliste hält.
 *
 * Klammerteile sind wie beim Server (`AnswerVariants.OptionalParts`, B-462) einzeln weglassbar: „folk“ genügt für
 * „folk (pl) (infml)“ – das Kürzel ist Wörterbuch-Angabe, kein Teil des Worts, und der Server wertet „folk“ in
 * im Erinnern ohnehin als richtig.
 */
export function abgeschrieben(eingabe: string, wort: string): boolean {
  if (eingabe.trim() === "") return false;
  const gegeben = normalizeAnswer(eingabe);
  return klammerFormen(wort).some((form) => normalizeAnswer(form) === gegeben);
}

/**
 * B-661, Entscheidung 2: die Abschrift stimmt bis auf die Zeichen auf und unter den Buchstaben. Das gibt „OK“ NICHT
 * frei – es steuert nur den Hinweis „Fast – achte auf die Akzente“.
 */
export function nurAkzenteFalsch(eingabe: string, wort: string): boolean {
  return !abgeschrieben(eingabe, wort) && abgeschrieben(ohneDiakritika(eingabe), ohneDiakritika(wort));
}

// Der ganze Eintrag, dazu jede Klammer weggelassen oder ihr Inhalt behalten; ab 16 Formen nur „alle behalten“ und
// „alle weg“ – wie MaxForms am Server.
function klammerFormen(wort: string): string[] {
  const klammer = /\(([^()]*)\)/g;
  const anzahl = wort.match(klammer)?.length ?? 0;
  if (anzahl === 0) return [wort];
  if (2 ** anzahl > 16) return [wort, wort.replace(klammer, "$1"), wort.replace(klammer, " ")];
  return [wort, ...Array.from({ length: 2 ** anzahl }, (_, maske) => {
    let k = 0;
    return wort.replace(klammer, (_t, inhalt: string) => ((maske >> k++) & 1 ? inhalt : " "));
  })];
}

/**
 * Die Lernkarte (B-598): ein neues Wort in drei Schritten. Das deutsche Wort steht schon in der Karte darüber.
 * 1. aufdecken; 2. das Fremdwort sehen, hören und abschreiben – „OK“ erst, wenn es stimmt; 3. das Wort ist weg, aus
 * dem Kopf schreiben oder aufgeben. Gewertet wird erst der letzte Schritt, beim Server. Die Zwischenfrage „Kanntest du
 * das Wort schon?“ ist seit B-686 weg: sie kostete bei jedem Wort einen Klick und trug nichts.
 *
 * Vorgelesen wird nur, solange das Wort zu sehen ist (Entscheidung 10): im letzten Schritt wäre der Ton die Lösung.
 * `gesperrt` gilt, solange die Antwort unterwegs ist – derselbe Wartepunkt wie bei jedem anderen Knopf der Runde.
 *
 * B-643: Im letzten Schritt gibt es „Mach's mir leichter“, solange der Server `canEase` meldet. Er schaltet die Karte eine Stufe
 * tiefer (Kästchen, Buchstabensalat, Auswahl) und schickt sie neu; die Karte bleibt dabei eingehängt und im Erinnern,
 * nur die Antwortfläche folgt der neuen Form (`eased`). „Später nochmal“ (vorher „Weiß ich nicht“) gibt ohne Hilfe auf.
 */
export function Lernkarte({ karte, gesperrt, zuFrueh = () => false, onFertig, onLeichter }: {
  karte: PracticeCard;
  gesperrt: boolean;
  /**
   * Die Sperre der Runde nach einem Blatt (NACH_BLATT_MS): ein gehaltenes Enter darf nicht durch „Aufdecken“ laufen
   * und ein Wort aufdecken, das das Kind nie gesehen hat.
   */
  zuFrueh?: () => boolean;
  onFertig: (ergebnis: LernkartenErgebnis) => void;
  /** B-643: die Karte eine Stufe leichter – der Aufrufer fragt den Server und reicht die neue Karte herein. */
  onLeichter?: () => void;
}) {
  const { t } = useTranslation();
  const [schritt, setSchritt] = useState<Schritt>("aufdecken");
  const [kopie, setKopie] = useState("");
  const [erinnert, setErinnert] = useState("");
  const fokus = useRef<HTMLElement | null>(null);
  const kopieFeld = useRef<HTMLInputElement | null>(null);
  const erinnertFeld = useRef<HTMLInputElement | null>(null);
  const ersterSchritt = useRef(true);
  const wortId = useId();
  const wort = karte.reveal ?? "";
  // B-661, Entscheidung 3: beim Abschreiben steht das Wort da – die Leiste darf seine Zeichen anbieten, und so ist jedes
  // Wort abschreibbar. Aus dem Kopf nur die feste Menge der Sprache: dort verriete die Wort-Ergänzung die Akzente.
  const abschreibZeichen = leistenZeichen(karte.keyboardLang, wort);
  const erinnerZeichen = leistenZeichen(karte.keyboardLang);

  // Jeder weitere Schritt bringt den Fokus mit – sonst stünde er auf einem Knopf, den es nicht mehr gibt (B-231). Den
  // ersten nicht: beim Laden der Seite setzt die Runde bewusst keinen Fokus (B-443), beim Kartenwechsel tut sie es selbst.
  useEffect(() => {
    if (ersterSchritt.current) { ersterSchritt.current = false; return; }
    fokus.current?.focus();
  }, [schritt]);

  // Steht nur im Abschreib-Schritt, also nur, solange das Wort zu sehen ist.
  const vorlesenKnopf = (karte.revealAudioUrl
    ? <AudioButton url={karte.revealAudioUrl} label={t("lernkarte.listen")} autoPlay={false} />
    : karte.revealSpeechLang && <SpeakButton text={wort} lang={karte.revealSpeechLang} label={t("lernkarte.listen")} />);

  return (
    <div className="lernkarte">
      {schritt === "aufdecken" && (
        <div className="lektion-fuss">
          <button type="button" className="btn lime" ref={(el) => { fokus.current = el; }} disabled={gesperrt}
            onClick={() => { if (!zuFrueh()) setSchritt("abschreiben"); }}>{t("lernkarte.uncover")}</button>
        </div>
      )}

      {schritt === "abschreiben" && (
        <div className="lernkarte-wort">
          <div className="rev" id={wortId}>→ {wort}</div>
          {vorlesenKnopf}
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
            spellCheck={false} aria-describedby={wortId} ref={(el) => { fokus.current = el; kopieFeld.current = el; }} value={kopie}
            readOnly={gesperrt} onChange={(e) => setKopie(e.target.value)} />
          <SonderzeichenLeiste zeichen={abschreibZeichen} gesperrt={gesperrt}
            onZeichen={(z) => einsetzen(kopieFeld.current, kopie, z, setKopie)} />
          {/* Die Region steht immer, nur ihr Text wechselt: eine erst eingehängte Live-Region liest mancher Screenreader nicht vor (B-134). */}
          <p className="sub lernkarte-akzent" role="status">{nurAkzenteFalsch(kopie, wort) ? t("lernkarte.accentHint") : ""}</p>
          <button type="submit" className="btn lime" disabled={gesperrt || !abgeschrieben(kopie, wort)}>{t("lernkarte.ok")}</button>
        </form>
      )}

      {schritt === "erinnern" && karte.eased && (
        <>
          <p className="sub" style={{ margin: 0 }}>{t("lernkarte.recall")}</p>
          <Antwortflaeche kartenId={`lernkarte-${karte.itemIndex}-${karte.stage}`} karte={karte} wert={erinnert}
            onWert={setErinnert} gesperrt={gesperrt} knopf={t("lernkarte.ok")} feldName={t("lernkarte.recall")}
            platzhalter="" onAbschicken={() => onFertig({ givenAnswer: erinnert })} />
          <div className="lektion-fuss">
            {karte.canEase && onLeichter && (
              <button type="button" className="btn ghost" disabled={gesperrt} onClick={() => { setErinnert(""); onLeichter(); }}>
                {t("lernkarte.easier")}
              </button>
            )}
            <button type="button" className="btn ghost" disabled={gesperrt}
              onClick={() => onFertig({ givenAnswer: null, gaveUp: true })}>{t("lernkarte.giveUp")}</button>
          </div>
        </>
      )}

      {schritt === "erinnern" && !karte.eased && (
        <form className="lektion-fuss" onSubmit={(e) => {
          e.preventDefault();
          if (erinnert.trim()) onFertig({ givenAnswer: erinnert });
        }}>
          <label className="sub" htmlFor="lernkarte-erinnern">{t("lernkarte.recall")}</label>
          <input id="lernkarte-erinnern" className="lektion-feld" autoComplete="off" autoCapitalize="off" autoCorrect="off"
            spellCheck={false} ref={(el) => { fokus.current = el; erinnertFeld.current = el; }} value={erinnert}
            readOnly={gesperrt} onChange={(e) => setErinnert(e.target.value)} />
          <SonderzeichenLeiste zeichen={erinnerZeichen} gesperrt={gesperrt}
            onZeichen={(z) => einsetzen(erinnertFeld.current, erinnert, z, setErinnert)} />
          <button type="submit" className="btn lime" disabled={gesperrt || !erinnert.trim()}>{t("lernkarte.ok")}</button>
          {karte.canEase && onLeichter && (
            <button type="button" className="btn ghost" disabled={gesperrt} onClick={() => { setErinnert(""); onLeichter(); }}>
              {t("lernkarte.easier")}
            </button>
          )}
          <button type="button" className="btn ghost" disabled={gesperrt}
            onClick={() => onFertig({ givenAnswer: null, gaveUp: true })}>{t("lernkarte.giveUp")}</button>
        </form>
      )}
    </div>
  );
}
