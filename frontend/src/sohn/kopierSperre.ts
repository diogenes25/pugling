import { useEffect, useRef, type RefObject } from "react";

/**
 * Die Kopier- und Einfügesperre der Arcade (Anti-Cheat): Ein Kind markiert das Fremdwort, solange es zu sehen ist
 * (Lernkarte Schritt 2/3, Lösung auf dem Rückmeldungsblatt), und fügt es danach im Gedächtnis-Schritt ein. Drei
 * Schichten, alle an der Wurzel der Arcade statt am einzelnen Feld – das Wort steht an mehreren Stellen, und jede neue
 * Antwortfläche wäre sonst eine offene Tür:
 *
 * 1. **Nichts verlässt die Arcade**: `copy`/`cut` schreiben eine leere Zwischenablage, `dragstart` ist gesperrt. Die CSS
 *    (`.app-sohn { user-select: none }`) nimmt dazu am Handy das Markier-Menü weg.
 * 2. **Nichts kommt herein**: `paste`, `drop` und jedes `beforeinput` vom Typ `insertFrom…` (Einfügen, Ziehen, Yank) –
 *    das fängt auch Text von außerhalb, etwa aus der Texterkennung eines Screenshots.
 * 3. **Auffangnetz**: ein `insertText`/`insertReplacementText` mit mehr als einem Zeichen. So fügt mancher Handy-
 *    Tastatur-Vorschlag die Zwischenablage ein, ohne ein `paste` auszulösen. Ein Tastendruck bringt genau ein Zeichen.
 *    Unberührt bleibt jede Komposition: `insertCompositionText` (Android-Tastaturen, Wischtippen dort) ist nicht
 *    abbrechbar, und `insertFromComposition` ist Safaris Abschluss eines Akzents über eine Tottaste („´“ + „e“) – den zu
 *    sperren hieße, in Safari kein „é“ mehr tippen zu können. Eine Lücke bleibt damit: ein Vorschlag, den Android
 *    mitten in einer Komposition einsetzt, kommt durch. Was diese Schicht sperrt, aber ehrlich gemeint war – Wischtippen
 *    und Diktat am iPhone kommen als ganzes Wort –, meldet `beiMehrzeichen`, damit das Wort nicht stumm verschwindet.
 *
 * Ausgenommen ist, was `data-einfuegen-erlaubt` trägt (das Anmerkungs-Widget: dort ist Einfügen Werkzeug, kein Betrug).
 * Gibt die Aufräumfunktion zurück.
 */
export function kopierSperre(wurzel: HTMLElement, beiMehrzeichen: () => void = () => {}): () => void {
  const erlaubt = (e: Event) => e.target instanceof Element && e.target.closest("[data-einfuegen-erlaubt]") !== null;

  const kopieren = (e: Event) => {
    if (erlaubt(e)) return;
    e.preventDefault();
    // Leer überschreiben statt nur abbrechen: ein abgebrochenes `copy` ließe die alte Zwischenablage stehen.
    (e as ClipboardEvent).clipboardData?.setData("text/plain", "");
  };
  const sperren = (e: Event) => { if (!erlaubt(e)) e.preventDefault(); };
  const eingabe = (e: Event) => {
    if (erlaubt(e)) return;
    const { inputType, data } = e as InputEvent;
    if (inputType.startsWith("insertFrom") && inputType !== "insertFromComposition") { e.preventDefault(); return; }
    // Codepunkte statt UTF-16-Einheiten: ein Emoji ist ein getipptes Zeichen, aber zwei Einheiten lang.
    if ((inputType === "insertText" || inputType === "insertReplacementText") && data !== null && [...data].length > 1) {
      e.preventDefault();
      beiMehrzeichen();
    }
  };

  const hoerer: [string, (e: Event) => void][] = [
    ["copy", kopieren], ["cut", kopieren], ["dragstart", sperren], ["paste", sperren], ["drop", sperren],
    ["beforeinput", eingabe],
  ];
  for (const [typ, h] of hoerer) wurzel.addEventListener(typ, h, true);
  return () => { for (const [typ, h] of hoerer) wurzel.removeEventListener(typ, h, true); };
}

/** Hängt {@link kopierSperre} an das Element hinter `ref`, solange die Komponente steht. */
export function useKopierSperre(ref: RefObject<HTMLElement | null>, beiMehrzeichen: () => void): void {
  // Über eine Ref, damit ein neuer Rückruf je Render die Hörer nicht ab- und wieder anhängt.
  const rueckruf = useRef(beiMehrzeichen);
  useEffect(() => { rueckruf.current = beiMehrzeichen; });
  useEffect(() => (ref.current ? kopierSperre(ref.current, () => rueckruf.current()) : undefined), [ref]);
}
