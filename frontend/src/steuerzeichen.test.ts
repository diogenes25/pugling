import { describe, expect, it } from "vitest";

/**
 * Wächter (Retro des Tageslaufs, Sprint 56–58): keine Quelle trägt ein unsichtbares Steuerzeichen. Ein `\b` aus einem
 * Python-Skript wurde als Backspace (U+0008) in ein Muster geschrieben (`datum.test.ts`, Sprint 54) – das Muster sah im
 * Editor richtig aus, zwei seiner vier Formen griffen nie, und weder Test noch Review sahen es. Erlaubt sind Tab,
 * Zeilenvorschub und Wagenrücklauf. Rot, wenn eine Quelle ein anderes Zeichen unter U+0020 (oder DEL) enthält.
 */
// Auch die E2E-Specs und die Konfiguration: ein Steuerzeichen in einem Muster einer verneinten Zusicherung
// (`toHaveCount(0)`) ließe eine Spec still grün (Review).
const QUELLEN = import.meta.glob(["./**/*.ts", "./**/*.tsx", "./**/*.css", "../e2e/**/*.ts", "../*.ts"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

// Die verbotenen Zeichen als Codes, nicht als Literal: sonst stünde genau das Zeichen, das der Wächter sucht, in ihm.
const VERBOTEN = new Set([...Array(32).keys()].filter((c) => c !== 9 && c !== 10 && c !== 13).concat(127));

function funde(datei: string, text: string): string[] {
  const out: string[] = [];
  for (let i = 0; i < text.length; i++)
    if (VERBOTEN.has(text.charCodeAt(i)))
      out.push(`${datei}:${text.slice(0, i).split("\n").length}: U+${text.charCodeAt(i).toString(16).padStart(4, "0")}`);
  return out;
}

describe("Keine Quelle trägt ein Steuerzeichen", () => {
  it("findet in keiner Quelle eines", () => {
    expect(Object.keys(QUELLEN).length).toBeGreaterThan(50);
    expect(Object.entries(QUELLEN).flatMap(([d, t]) => funde(d, t))).toEqual([]);
  });

  it("erkennt ein Backspace und lässt Tab und Zeilenende durch", () => {
    expect(funde("x.ts", `a${String.fromCharCode(8)}b`)).toHaveLength(1);
    expect(funde("x.ts", "a\tb\r\nc")).toEqual([]);
  });
});
