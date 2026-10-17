import { useRef } from "react";
import { useGemerkterFehler } from "../lib/useGemerkterFehler";

/**
 * Ein gescheiterter Listen-Abruf, beim Namen genannt und mit dem Weg zurück (B-188).
 *
 * „konnte nicht geladen werden" allein sagt nicht, WAS fehlt – eine Seite lädt oft mehrere Listen. Darum
 * trägt der Aufrufer den Satzanfang bei („Deine Kinder", „Die Übungstypen"). Ohne den Knopf wäre die
 * wahre Meldung eine Verschlechterung: Oft ändert keine Bewegung des Nutzers eine `useAsync`-Abhängigkeit,
 * es fliegt also keine neue Abfrage, und der einzige Ausweg wäre F5.
 *
 * Lag bis B-215 in `VaterWizard.tsx`; seit auch Übungsliste und Übung-anlegen ihn brauchen, ist eine
 * Seite die falsche Heimat. Die Regel „der letzte Fehler überlebt den Start der Wiederholung" steht in
 * `useGemerkterFehler` – geteilt mit den drei Stellen aus B-216.
 *
 * **Fokus beim Wiederholen** (B-216, im Browser gemessen): Ein Knopf, der `disabled` wird, verliert in
 * Chromium den Fokus – er fiele auf `<body>`, obwohl der Knopf stehen bleibt. Darum wandert der Fokus beim
 * Klick auf den Kasten selbst (`tabIndex={-1}`): Er liegt damit bei der Meldung, die gerade sagt, was
 * passiert, und scheitert die Wiederholung erneut, ist der Knopf von dort mit Tab erreichbar. Gelingt sie,
 * verschwindet der Kasten – und mit ihm ein Fehlerzustand, in dem nichts mehr zu fokussieren ist.
 */
export function ListenAusfall({ was, fehler, laedt, onRetry }: {
  /** Was nicht geladen werden konnte, als Satzanfang: „Deine Kinder", „Die Fächer". */
  was: string;
  fehler: string | null;
  laedt: boolean;
  onRetry: () => void;
}) {
  const gescheitert = useGemerkterFehler(fehler, laedt);
  const kasten = useRef<HTMLDivElement>(null);
  if (!gescheitert) return null;
  return (
    <div className="banner err" role="alert" tabIndex={-1} ref={kasten}>
      {was} konnten nicht geladen werden: {gescheitert}
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: 8 }}
        disabled={laedt} onClick={() => { kasten.current?.focus(); onRetry(); }}>
        {laedt ? "Lädt…" : "Erneut laden"}
      </button>
    </div>
  );
}
