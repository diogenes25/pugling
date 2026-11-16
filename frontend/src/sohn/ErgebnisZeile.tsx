import { useTranslation } from "react-i18next";
import { ClozePrompt } from "../components/ClozePrompt";
import { clozeSentence } from "../lib/cloze";

/**
 * Eine Zeile einer Auswertung (Abschlusstest, Bonus-Runde): was gefragt war, was das Kind geantwortet hat, was richtig ist
 * – untereinander, damit die drei nicht in einer Zeile ineinanderlaufen. Ein Lückentext zeigt nur den Satz um seine
 * Lücke: alle Lücken teilen denselben Text, und der ganze Absatz in jeder Zeile begrub die Antworten.
 *
 * `zeigeAntwort` ist aus, wo es keine getippte Antwort gibt (selbst eingeschätzt) – dort ist nur die Lösung die Lehre.
 * Eine richtige Antwort wird nicht wiederholt: sie steht ohnehin als Lösung da.
 */
export function ErgebnisZeile({ prompt, gapIndex, expected, givenAnswer, wasCorrect, misspelled = false, zeigeAntwort }: {
  prompt?: string | null; gapIndex?: number | null; expected: string; givenAnswer?: string | null; wasCorrect: boolean;
  misspelled?: boolean; zeigeAntwort: boolean;
}) {
  const { t } = useTranslation();
  return (
    <div className="ergebnis-zeile">
      {/* B-524: „falsch geschrieben" ist eine eigene Lehre – das Wort war da, die Schreibweise nicht. */}
      <span aria-hidden="true">{wasCorrect ? "✅" : misspelled ? "✏️" : "❌"}</span>
      <div>
        <span className="sr-only">{wasCorrect ? t("test.correct") : misspelled ? t("test.misspelled") : t("test.incorrect")}</span>
        <ClozePrompt text={prompt ? clozeSentence(prompt, gapIndex) : null} gapIndex={gapIndex} className="test-prompt" />
        {zeigeAntwort && !wasCorrect && (
          <div className="falsch">
            {givenAnswer?.trim() ? t("test.answerYou", { given: givenAnswer }) : t("test.answerNone")}
            {misspelled && <> · {t("test.misspelled")}</>}
          </div>
        )}
        <div className="richtig">{t("test.answerRight", { expected })}</div>
      </div>
    </div>
  );
}
