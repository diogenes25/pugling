import { describe, expect, it } from "vitest";

/*
 * Wächter für B-134: eine Live-Region (`role="status"` oder `aria-live`) muss im DOM stehen, BEVOR ihr Text
 * hineinkommt. Viele Screenreader sagen nur an, was in eine schon vorhandene Region hineinwächst; entsteht
 * die Region zusammen mit ihrem Text (`{error && <div role="status">…}`), bleibt die Meldung stumm. Die Regel
 * und ihre Begründung stehen an `StatusBanner.tsx`; dieser Test hält sie dort, wo niemand den Kommentar liest.
 *
 * Gemeldet wird ein öffnendes Tag mit `role="status"` oder `aria-live`, das
 *   (a) direkt hinter `&&`, `?` oder `:` steht (auch über eine Klammer und Zeilenumbrüche hinweg), oder
 *   (b) in einer Funktion steht, die davor schon `return null` liefern kann.
 * Richtig ist die Region, die immer gerendert wird und nur ihren INHALT bedingt trägt
 * (`<div role="status" aria-live="polite">{error && …}</div>`, Vorbild `StatusBanner`).
 *
 * Bewusst NICHT gemeldet: `role="alert"`. Ein eingefügtes `alert` wird zuverlässig angesagt; das Repo nutzt
 * es absichtlich für Fehler, die laut sein sollen (B-188, `ListenAusfall`).
 *
 * Ein halber Parser: (b) erkennt die umgebende Funktion an ihrer Kopfzeile, nicht am Syntaxbaum. Ein Fehlalarm
 * gehört mit Grund in `AUSNAHMEN`, nicht in einen Umbau der Erkennung.
 */

const QUELLEN = import.meta.glob(["./**/*.tsx", "!./**/*.test.tsx"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

/**
 * Bewusst bedingte Live-Regionen: Datei, ein Stück des öffnenden Tags (damit der Eintrag Zeilenverschiebungen
 * überlebt) und der Grund. Jeder Eintrag braucht einen Grund.
 */
const AUSNAHMEN: { datei: string; tag: string; grund: string }[] = [
  {
    datei: "./components/ListControls.tsx",
    tag: '<span className="muted tabnum" aria-live="polite">',
    grund: "Seitenanzeige des Blätterns: sie soll den Seitenwechsel ansagen, und dabei bleibt sie stehen. "
      + "Dass ihr erstes Erscheinen (die Liste wächst über eine Seite) stumm ist, ist gewollt – angesagt "
      + "würde sonst bei jedem Laden „1–20 von 25\", ohne dass der Vater etwas getan hat.",
  },
];

const LIVE = /\b(role="status"|aria-live=)/;
const FUNKTIONSKOPF = /^\s*(export\s+)?(default\s+)?function\s|^\s*(export\s+)?const\s+\w+\s*=\s*(\([^)]*\)|\w+)\s*=>/;

/** Alle öffnenden Tags mit Live-Attribut: Zeile des `<` und Text davor. */
export function findeBedingteLiveRegionen(datei: string, quelle: string): string[] {
  const funde: string[] = [];
  const zeilen = quelle.split(/\r?\n/);
  // Startoffsets der Zeilen, um aus einem Index die Zeilennummer zu machen.
  const starts: number[] = [];
  let pos = 0;
  for (const z of zeilen) { starts.push(pos); pos += z.length + 1; }
  const zeileVon = (i: number) => {
    let lo = 0, hi = starts.length - 1;
    while (lo < hi) { const mid = (lo + hi + 1) >> 1; if (starts[mid] <= i) lo = mid; else hi = mid - 1; }
    return lo;
  };
  const text = zeilen.join("\n");
  // `=>` in einem Attribut (`onClick={() => …}`) beendet das Tag nicht.
  const tag = /<([A-Za-z][\w.]*)\b(?:=>|[^<>])*?>/gs;
  for (let m = tag.exec(text); m; m = tag.exec(text)) {
    if (!LIVE.test(m[0])) continue;
    const zeile = zeileVon(m.index);
    const davor = text.slice(Math.max(0, m.index - 200), m.index).replace(/\{\/\*[\s\S]*?\*\/\}/g, "").trimEnd();
    if (/(&&|\?|:)\s*\(?\s*$/.test(davor)) {
      funde.push(`${datei}:${zeile + 1} steht hinter einer Bedingung (${davor.slice(-30).replace(/\s+/g, " ").trim()}) ${m[0]}`);
      continue;
    }
    for (let z = zeile; z >= 0; z--) {
      if (/\breturn null\b/.test(zeilen[z])) {
        funde.push(`${datei}:${zeile + 1} steht nach einem \`return null\` (Zeile ${z + 1}) ${m[0]}`);
        break;
      }
      if (FUNKTIONSKOPF.test(zeilen[z])) break;
    }
  }
  return funde;
}

describe("Live-Regionen stehen vor ihrem Text im DOM (B-134)", () => {
  it("keine Datei rendert eine Live-Region erst zusammen mit ihrer Meldung", () => {
    const funde = Object.entries(QUELLEN)
      .flatMap(([datei, quelle]) => findeBedingteLiveRegionen(datei, quelle))
      .filter((f) => !AUSNAHMEN.some((a) => f.startsWith(`${a.datei}:`) && f.includes(a.tag)));
    expect(funde, "Region immer rendern und nur den Inhalt bedingt tragen – Vorbild StatusBanner").toEqual([]);
  });

  it("die Ausnahmeliste nennt nur Dateien, die es gibt, und jede mit Grund", () => {
    for (const { datei, tag, grund } of AUSNAHMEN) {
      expect(QUELLEN[datei]?.includes(tag), `${datei}: Tag ${tag} gibt es nicht mehr – Eintrag streichen`).toBe(true);
      expect(grund.trim().length, `${datei} ohne Grund`).toBeGreaterThan(10);
    }
  });

  // Die Erkennung selbst: jeder Fall benennt die Form, die ihn rot macht.
  it.each([
    ["&& direkt", `{error && <div role="status" aria-live="polite">{error}</div>}`, 1],
    ["&& mit Klammer und Umbruch", `{ok && (\n  <div className="banner ok" role="status">x</div>\n)}`, 1],
    ["Ternär", `{busy ? <span aria-live="polite">…</span> : null}`, 1],
    ["return null davor", `function H({ n }: P) {\n  if (n === 0) return null;\n  return <p role="status">{n}</p>;\n}`, 1],
    ["dauerhafte Region", `<div role="status" aria-live="polite">{error && <div>{error}</div>}</div>`, 0],
    ["return null in einer anderen Funktion", `function A() { return null; }\nfunction B() {\n  return <p role="status">x</p>;\n}`, 0],
    ["role=alert ist erlaubt", `{error && <div role="alert">{error}</div>}`, 0],
  ])("%s", (_name, quelle, erwartet) => {
    expect(findeBedingteLiveRegionen("x.tsx", quelle)).toHaveLength(erwartet as number);
  });
});
