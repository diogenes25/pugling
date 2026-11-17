import { afterEach, describe, expect, it } from "vitest";
import { kopierSperre } from "./kopierSperre";

/*
 * Die Kopier- und Einfügesperre der Arcade. Je Fall, welche Änderung ihn rot macht:
 * - „copy/cut leert die Zwischenablage“: rot ohne den copy-/cut-Hörer oder ohne das leere `setData`.
 * - „paste/drop/dragstart“: rot ohne den jeweiligen Hörer.
 * - „insertFrom…“: rot ohne die `startsWith("insertFrom")`-Zeile (`paste` allein fängt sie nicht, es ist ein anderes Ereignis).
 * - „mehrere Zeichen auf einmal“: rot ohne die Mehrzeichen-Regel.
 * - „ein getipptes Zeichen bleibt“: rot, wenn die Regel jedes insertText sperrt oder UTF-16-Einheiten statt Codepunkte zählt
 *   (das Emoji ist zwei Einheiten lang).
 * - „Komposition bleibt“: rot, wenn die Regel auch insertCompositionText sperrt (Wischtippen, Akzente am Handy).
 * - „Safaris Kompositions-Abschluss bleibt“: rot ohne die Ausnahme `insertFromComposition` (sonst kein „é“ per Tottaste).
 * - „meldet das gesperrte Wort“: rot, wenn die Mehrzeichen-Regel `beiMehrzeichen` nicht ruft – oder es auch bei einem
 *   einzelnen Zeichen bzw. beim Einfügen ruft (dort bleibt die Sperre stumm, ein Hinweis lüde zum Ausprobieren ein).
 * - „ausgenommen“: rot ohne die `data-einfuegen-erlaubt`-Prüfung.
 * - „Aufräumen“: rot, wenn die Aufräumfunktion einen Hörer stehen lässt.
 */

let aufraeumen: (() => void) | null = null;
afterEach(() => { aufraeumen?.(); aufraeumen = null; document.body.innerHTML = ""; });

function aufbau() {
  document.body.innerHTML = `
    <div id="wurzel">
      <div class="rev">station</div>
      <input id="feld" />
      <section data-einfuegen-erlaubt><textarea id="notiz"></textarea></section>
    </div>`;
  const gemeldet: string[] = [];
  aufraeumen = kopierSperre(document.getElementById("wurzel")!, () => gemeldet.push("mehrzeichen"));
  return {
    gemeldet, wort: document.querySelector(".rev")!, feld: document.getElementById("feld")!, notiz: document.getElementById("notiz")! };
}

// happy-dom kennt kein `clipboardData` am Ereignis; die Ablage wird darum beigelegt.
function zwischenablageEreignis(typ: "copy" | "cut") {
  const ablage = new Map<string, string>([["text/plain", "vorher"]]);
  const e = new Event(typ, { bubbles: true, cancelable: true });
  Object.defineProperty(e, "clipboardData", { value: { setData: (f: string, v: string) => ablage.set(f, v) } });
  return { e, ablage };
}

const ereignis = (typ: string) => new Event(typ, { bubbles: true, cancelable: true });
const eingabe = (inputType: string, data: string | null) =>
  new InputEvent("beforeinput", { bubbles: true, cancelable: true, inputType, data });

describe("kopierSperre", () => {
  it.each(["copy", "cut"] as const)("%s aus der Arcade leert die Zwischenablage", (typ) => {
    const { wort } = aufbau();
    const { e, ablage } = zwischenablageEreignis(typ);
    wort.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(true);
    expect(ablage.get("text/plain")).toBe("");
  });

  it.each(["paste", "drop", "dragstart"])("%s ist gesperrt", (typ) => {
    const { feld } = aufbau();
    const e = ereignis(typ);
    feld.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(true);
  });

  it.each(["insertFromPaste", "insertFromDrop", "insertFromYank", "insertFromPasteAsQuotation"])(
    "beforeinput %s ist gesperrt", (inputType) => {
      const { feld } = aufbau();
      const e = eingabe(inputType, null);
      feld.dispatchEvent(e);
      expect(e.defaultPrevented).toBe(true);
    });

  it.each(["insertText", "insertReplacementText"])("%s mit mehreren Zeichen auf einmal ist gesperrt", (inputType) => {
    const { feld } = aufbau();
    const e = eingabe(inputType, "station");
    feld.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(true);
  });

  it.each(["s", "é", "😀"])("ein getipptes Zeichen (%s) bleibt", (zeichen) => {
    const { feld } = aufbau();
    const e = eingabe("insertText", zeichen);
    feld.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(false);
  });

  it("eine laufende Komposition bleibt, auch mit mehreren Zeichen", () => {
    const { feld } = aufbau();
    const e = eingabe("insertCompositionText", "station");
    feld.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(false);
  });

  it("Safaris Kompositions-Abschluss (Tottaste) bleibt", () => {
    const { feld } = aufbau();
    const e = eingabe("insertFromComposition", "é");
    feld.dispatchEvent(e);
    expect(e.defaultPrevented).toBe(false);
  });

  it("meldet ein gesperrtes ganzes Wort, aber kein Zeichen und kein Einfügen", () => {
    const { feld, gemeldet } = aufbau();
    feld.dispatchEvent(eingabe("insertText", "s"));
    feld.dispatchEvent(eingabe("insertFromPaste", null));
    feld.dispatchEvent(ereignis("paste"));
    expect(gemeldet).toEqual([]);
    feld.dispatchEvent(eingabe("insertText", "station"));
    expect(gemeldet).toEqual(["mehrzeichen"]);
  });

  it("im ausgenommenen Bereich ist Kopieren und Einfügen erlaubt", () => {
    const { notiz } = aufbau();
    const { e: kopie, ablage } = zwischenablageEreignis("copy");
    const einfuegen = ereignis("paste");
    const einfuegenTyp = eingabe("insertFromPaste", null);
    for (const e of [kopie, einfuegen, einfuegenTyp]) notiz.dispatchEvent(e);
    expect([kopie.defaultPrevented, einfuegen.defaultPrevented, einfuegenTyp.defaultPrevented]).toEqual([false, false, false]);
    expect(ablage.get("text/plain")).toBe("vorher");
  });

  it("nach dem Aufräumen ist nichts mehr gesperrt", () => {
    const { feld } = aufbau();
    aufraeumen!();
    aufraeumen = null;
    const proben = [ereignis("paste"), ereignis("drop"), ereignis("dragstart"), eingabe("insertFromPaste", null),
      eingabe("insertText", "station"), zwischenablageEreignis("copy").e, zwischenablageEreignis("cut").e];
    for (const e of proben) feld.dispatchEvent(e);
    expect(proben.map((e) => e.defaultPrevented)).toEqual(proben.map(() => false));
  });
});
