import { ClozeTexts } from "./ClozeTexts";

/**
 * Der Lückentext-Store als eigener Bereich.
 *
 * Trägertexte sind **Lerngrundlage**, kein Teil einer einzelnen Übung – dasselbe Verhältnis wie beim
 * Vokabel-Store, der seine Route (`/vater/vocab`) längst hat. Darum liegt er nicht mehr eingeklappt auf
 * der Übungen-Seite.
 */
export function VaterLueckentexte() {
  return (
    <>
      <h2 className="h-section">Lückentexte</h2>
      <p className="sub">
        Ein Trägertext wird einmal gepflegt und dann von <strong>mehreren</strong> Übungen genutzt. Die
        Lücken hängen über die Platzhalter am Text.
      </p>
      <ClozeTexts />
    </>
  );
}
