import { describe, expect, it } from "vitest";

/**
 * Sprint 97, Retro (Review-Fund in B-749): eine Rückfrage (`confirmAction`, `window.confirm`, `bestaetige…`) gehört **vor**
 * `action.run`/`runFor`, nie hinein. Drinnen endet ein „Abbrechen" als gelungener Lauf: `run` setzt den `okText`, und die
 * Seite meldete „freigegeben", obwohl nichts geschah. Kein Test sah es, weil jeder `confirm` auf `true` stellt.
 *
 * Ein halber Parser: gelesen wird das Argument jedes `.run(`/`.runFor(` bis zur schließenden Klammer. Rufe einer Hilfsfunktion,
 * die selbst fragt, sieht er nicht – darum heißen solche Helfer `bestaetige…` und stehen im Muster. Gemessen beim Bau:
 * 0 Funde im Bestand, 1 mit der Fassung vor dem Review-Fix. Rot, wenn eine Rückfrage wieder in einen Lauf wandert.
 */
const QUELLEN = import.meta.glob(["./**/*.tsx", "./**/*.ts", "!./**/*.test.tsx", "!./**/*.test.ts"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

const RUECKFRAGE = /\b(?:confirmAction|confirm|bestaetige\w*)\s*\(/;

/** Die Funde einer Quelle: Zeile des Laufs, in dessen Argument eine Rückfrage steht (auch vom Selbsttest benutzt). */
function funde(datei: string, text: string): string[] {
  const treffer: string[] = [];
  for (const m of text.matchAll(/\.(?:run|runFor)\(/g)) {
    let tiefe = 1;
    let i = m.index! + m[0].length;
    const start = i;
    while (i < text.length && tiefe > 0) {
      if (text[i] === "(") tiefe++;
      else if (text[i] === ")") tiefe--;
      i++;
    }
    if (RUECKFRAGE.test(text.slice(start, i)))
      treffer.push(`${datei}:${text.slice(0, m.index).split("\n").length}: Rückfrage in einem Lauf – vor action.run stellen`);
  }
  return treffer;
}

describe("Eine Rückfrage steht vor dem Lauf, nicht darin (Sprint 97)", () => {
  it("kein action.run/runFor fragt in seinem Argument nach", () => {
    expect(Object.keys(QUELLEN).length).toBeGreaterThan(50);
    expect(Object.entries(QUELLEN).flatMap(([d, t]) => funde(d, t))).toEqual([]);
  });

  // Selbsttest: jeder Fall nennt eine Form; rot, wenn der Scan sie nicht mehr (bzw. fälschlich) erkennt.
  it.each([
    ["Helfer im Lauf (der Fund aus B-749)", "await action.run(async () => { ok = await bestaetigeFreigabe(r); });", 1],
    ["confirmAction im Lauf", "action.run(async () => { if (!confirmAction('x')) return; await api.x(); })", 1],
    ["window.confirm im Lauf", "action.runFor(() => window.confirm('x') ? api.x() : Promise.resolve(null))", 1],
    ["Rückfrage davor", "if (!confirmAction('x')) return;\nawait action.run(() => api.x(), 'ok');", 0],
    ["verschachtelte Klammern ohne Rückfrage", "action.run(() => api.save(f(a, (b)), c), `ok ${n(1)}`)", 0],
  ])("%s", (_name, code, n) => {
    expect(funde("probe.tsx", code)).toHaveLength(n as number);
  });
});
