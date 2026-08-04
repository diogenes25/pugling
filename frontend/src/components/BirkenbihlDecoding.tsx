import type { WordPair } from "../lib/types";

/**
 * Die Wort-für-Wort-Dekodierung eines Satzes (Birkenbihl): über jedem Wort der Lernsprache steht seine
 * wörtliche Bedeutung, positionsgenau und grammatikunabhängig. **Das ist die Methode** – ohne sie ist die
 * Karte eine gewöhnliche Übersetzungskarte, und genau so kam sie beim Kind an (B-78).
 *
 * Eigenes Bauteil und keine Erweiterung von `Passage`: dort ist es ein zusammenhängender Fließtext, hier eine
 * Folge von Paaren, die als **Einheit** umbrechen müssen (Lernwort und Gloss dürfen nie auf zwei Zeilen
 * auseinanderfallen, sonst zeigt die Ausrichtung auf das falsche Wort). Darum je Paar eine Spalte statt zwei
 * Zeilen Text – Wörter der beiden Sprachen sind unterschiedlich lang, eine Ausrichtung über Leerzeichen
 * verrutscht bei jeder Fenstergröße neu.
 *
 * Die Spalte trägt die Zuordnung aber **nur visuell**. Für ein Ohr wäre „How Wie are bist" ein Durchlauf, in dem
 * Wort und Bedeutung nicht zu unterscheiden sind – darum je Paar ein vorgelesener Gedankenstrich und nach dem
 * Paar ein Komma (`.sr-only`, dieselbe Sorgfalt wie bei `Passage` und der Antwort-Gruppe in `SohnPractice`).
 */
export function BirkenbihlDecoding({ decoding }: { decoding?: readonly WordPair[] | null }) {
  if (!decoding?.length) return null;

  return (
    <div className="decoding" role="group" aria-label="Wort-für-Wort-Dekodierung">
      {decoding.map((w) => (
        // Schlüssel ist die übungsweit eindeutige `wordId` – dasselbe Wort darf im Satz zweimal vorkommen.
        <span className="decoding-pair" key={w.wordId}>
          <span className="decoding-word">{w.learningWord}</span>
          {/* Ohne Gloss bleibt die Spalte stehen, aber leer: das Wort steht nicht im Vokabelspeicher, und ein
              Platzhalter würde eine Bedeutung behaupten, die niemand hinterlegt hat. Dann entfällt auch der
              vorgelesene Trenner – „How –" verspräche eine Bedeutung, die nicht kommt. */}
          {w.gloss ? <span className="sr-only"> – </span> : null}
          <span className="decoding-gloss">{w.gloss ?? ""}</span>
          <span className="sr-only">, </span>
        </span>
      ))}
    </div>
  );
}
