import { describe, expect, it } from "vitest";

/**
 * B-352, Wächter: eine Radiogruppe aus Knöpfen (`role="radio"` an einem `<button>`) sagt Pfeiltasten und einen Tab-Stopp
 * an. Beides liefert nur `radioGroupKeys` am Container plus ein wandernder `tabIndex` an jeder Option. Native
 * `<input type="radio">` (PlanPositions) bringen das selbst mit und fallen nicht unter die Regel.
 *
 * Gezählt wird je Datei, die eine `radiogroup` ohne native Radios trägt: so viele `role="radiogroup"` wie
 * `onKeyDown={radioGroupKeys}` – auch dort, wo die Optionen aus einem Baustein einer anderen Datei kommen –, und an jedem
 * `role="radio"` ein `tabIndex={… ? 0 : -1}` in derselben Zeile. Rot, wenn eine Gruppe aus Knöpfen ohne den Helfer
 * entsteht oder jede Option ein Tab-Stopp bleibt.
 */
const QUELLEN = import.meta.glob(["./**/*.tsx", "!./**/*.test.tsx"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

const ROVING = /tabIndex=\{[^}]*\?\s*0\s*:\s*-1\}/;

/** Die Funde einer Quelle (auch vom Selbsttest unten benutzt). */
function funde(datei: string, text: string): string[] {
  if (!/role="radio(group)?"/.test(text) || /type="radio"/.test(text)) return [];
  const out: string[] = [];
  const gruppen = (text.match(/role="radiogroup"/g) ?? []).length;
  const helfer = (text.match(/onKeyDown=\{radioGroupKeys\}/g) ?? []).length;
  if (gruppen !== helfer) out.push(`${datei}: ${gruppen} radiogroup, ${helfer} with onKeyDown={radioGroupKeys}`);
  text.split("\n").forEach((zeile, i) => {
    if (/role="radio"/.test(zeile) && !ROVING.test(zeile)) out.push(`${datei}:${i + 1}: role="radio" without a roving tabIndex`);
  });
  return out;
}

describe("Radiogruppen aus Knöpfen", () => {
  it("tragen alle das Tastaturmodell", () => {
    expect(Object.keys(QUELLEN).length).toBeGreaterThan(50);
    expect(Object.entries(QUELLEN).flatMap(([d, t]) => funde(d, t))).toEqual([]);
  });

  // Selbsttest: der Wächter muss jede Lücke einzeln sehen, sonst prüft er den Ausgangszustand und kann nie fallen.
  it("meldet eine Gruppe ohne Helfer, eine Option ohne wandernden tabIndex und eine Gruppe mit fremden Bausteinen", () => {
    expect(funde("a.tsx", '<div role="radiogroup">\n<button role="radio" tabIndex={on ? 0 : -1} />')).toHaveLength(1);
    expect(funde("b.tsx", '<div role="radiogroup" onKeyDown={radioGroupKeys}>\n<button role="radio" />')).toHaveLength(1);
    // Das Attribut steht da, wandert aber nicht: jede Option bliebe ein Tab-Stopp.
    expect(funde("c.tsx", '<div role="radiogroup" onKeyDown={radioGroupKeys}>\n<button role="radio" tabIndex={0} />')).toHaveLength(1);
    // Die Optionen kommen aus einem Baustein anderswo: die Gruppe allein muss den Helfer tragen.
    expect(funde("d.tsx", '<div role="radiogroup">\n<OptionCard active />')).toHaveLength(1);
    expect(funde("e.tsx", '<div role="radiogroup" onKeyDown={radioGroupKeys}>\n<button role="radio" tabIndex={on ? 0 : -1} />')).toEqual([]);
  });

  it("lässt native Radios in Ruhe", () => {
    expect(funde("f.tsx", '<div role="radiogroup">\n<input type="radio" />')).toEqual([]);
  });
});
