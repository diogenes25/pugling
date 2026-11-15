/**
 * Die Marke „(Pflicht)" im Label eines Pflichtfelds (B-233, B-380) – eine Fassung des Wortlauts statt einer je Formular.
 *
 * Sie steht **im** Label, damit der zugängliche Name sie trägt („Name der Reihe (Pflicht)"): ein Screenreader hört, was
 * das Auge sieht. Die Sperre gehört nicht hierher, sondern bleibt als `disabled={busy || !wert.trim()}` am Knopf – dort
 * muss der Leser die Bedingung sehen, unter der gespeichert wird.
 */
export function Pflicht() {
  return <span className="muted">(Pflicht)</span>;
}
