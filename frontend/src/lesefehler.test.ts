import { describe, expect, it } from "vitest";

/**
 * B-183, Wächter: ein gescheitertes **Lesen** bietet einen erneuten Versuch. Ein Banner, das nur `x.error` zeigt, lässt den
 * Nutzer mit F5 stehen – und F5 verliert jede Eingabe daneben. Die Hausregel ist `ListenAusfall` (B-188): Satzanfang, Fehler,
 * „Erneut laden" an `x.reload`. Gemeldet wird jedes `{x.error && <… className="banner err">}`; `useAction`-Fehler heißen
 * `message` und fallen nicht darunter.
 *
 * Ein halber Parser: erkannt wird die `&&`-Form (gezählt beim Bau: 29 Stellen, keine mehrzeilige). **Bekannte Lücke:** die
 * Ternär-Form `x.loading … ? „Lade…" : x.error ? <banner> : …` (mehrzeilig gezählt 22 im Vater-Web, 26 in ganz `src`), frühe
 * Rückgaben mit Banner, die Arcade (`banner err` wie `error-box`) und Fehler unter einem nackten Namen (`error`,
 * auch für Anmeldefehler benutzt) sieht er nicht – sie trägt [B-369](../../docs/backlog/B-369-lesefehler-in-ternaer-form.md),
 * weil dort der Platzhalter vor dem Ausfall steht und „Erneut laden" ihn sonst gegen „Lade…" tauschte (B-216).
 * Rot, wenn ein Lesefehler in der `&&`-Form wieder als nacktes Banner entsteht.
 */
const QUELLEN = import.meta.glob(["./**/*.tsx", "!./**/*.test.tsx"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

const NACKTES_BANNER = /\{\s*([A-Za-z_$][\w$.]*)\.error\s*&&\s*\(?\s*<(div|p)\b[^>]*\bbanner err\b/g;

/** Die Funde einer Quelle: Zeile und Name der Abfrage (auch vom Selbsttest unten benutzt). */
function funde(datei: string, text: string): string[] {
  return [...text.matchAll(NACKTES_BANNER)].map((m) =>
    `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[1]}.error ohne erneuten Versuch – ListenAusfall nehmen`);
}

describe("Ein gescheitertes Lesen bietet einen erneuten Versuch (B-183)", () => {
  it("kein Lesefehler steht als nacktes Banner da", () => {
    expect(Object.keys(QUELLEN).length).toBeGreaterThan(50);
    expect(Object.entries(QUELLEN).flatMap(([d, t]) => funde(d, t))).toEqual([]);
  });

  // Selbsttest: jeder Fall nennt eine Form; rot wird er, wenn die Regex diese Form nicht mehr (bzw. fälschlich) erkennt.
  it.each([
    ["einfach", `{links.error && <div className="banner err">{links.error}</div>}`, 1],
    ["mit Zusatz-Attributen", `{known.error && <div className="banner err" style={{ marginBottom: 6 }}>{known.error}</div>}`, 1],
    ["mit Pfad", `{state.list.error && <p className="muted banner err">x</p>}`, 1],
    ["in Klammern", `{list.error && (<div className="banner err">x</div>)}`, 1],
    ["Aktions-Meldung ist kein Lesefehler", `{action.message && <div className="banner err">x</div>}`, 0],
    // Absichtserklärung, kein Mutanten-Fall: der Baustein selbst ist das Erwünschte und darf nie gemeldet werden.
    ["ListenAusfall", `<ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />`, 0],
    ["anderes Banner", `{list.error && <div className="banner ok">x</div>}`, 0],
  ])("%s", (_name, quelle, erwartet) => {
    expect(funde("x.tsx", quelle)).toHaveLength(erwartet as number);
  });
});
