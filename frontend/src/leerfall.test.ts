import { describe, expect, it } from "vitest";

/**
 * B-505: eine geblätterte Liste sagt „noch keine …" nur, wenn der **Bestand** leer ist (`total === 0`), nicht die Seite.
 * Eine leere Seite hinter dem Bestand korrigiert der `Pager` (B-505); bis dahin – und in jeder Liste, die ihn mitlaufen
 * lässt – behauptete `items.length === 0` „nichts da" bei vollem Bestand. Gelesen wird jede Quelle, die einen `<Pager`
 * rendert. Rot, wenn eine geblätterte Liste ihren Leerfall wieder an die Länge der Seite hängt.
 */
const QUELLEN = import.meta.glob(["./**/*.tsx", "!./**/*.test.tsx"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

// Die Formen, in denen eine Liste „Seite leer" fragt: `=== 0`, `< 1`, `<= 0` (auch hinter `?? 0)`), und `!x.data?.items.length`.
const LEERE_SEITE =
  /!\s*([A-Za-z_$][\w$]*)\.data\??\.items\.length\b|\b([A-Za-z_$][\w$]*)\.data\??\.items\.length\s*(?:\?\?\s*0\s*\)\s*)?(?:===\s*0|<\s*1|<=\s*0)/g;

function funde(datei: string, text: string): string[] {
  if (!text.includes("<Pager")) return [];
  return [...text.matchAll(LEERE_SEITE)].map((m) =>
    `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[1] ?? m[2]}.data?.items.length – den Bestand lesen (total === 0)`);
}

describe("Der Leerfall einer geblätterten Liste liest den Bestand (B-505)", () => {
  it("keine geblätterte Liste hängt ihren Leerfall an die Länge der Seite", () => {
    const geblaettert = Object.entries(QUELLEN).filter(([, text]) => text.includes("<Pager"));
    // Gezählt beim Bau: acht Quellen rendern einen Pager (sieben Listen mit Leerfall plus der Lernstand).
    expect(geblaettert.length).toBeGreaterThanOrEqual(8);
    expect(geblaettert.flatMap(([datei, text]) => funde(datei, text))).toEqual([]);
  });

  // Selbsttest: das Muster trifft jede Form der Seitenfrage und lässt die Bestandsfrage durch – sonst wäre der Wächter oben leer.
  it("erkennt die Formen der Seitenfrage und lässt die Bestandsfrage durch", () => {
    const seitenfragen = [
      "{list.data?.items.length === 0 && <p/>}",
      "{(list.data?.items.length ?? 0) === 0 ? <p/> : null}",
      "{!list.data?.items.length && <p/>}",
      "{list.data.items.length < 1 && <p/>}",
      "{list.data?.items.length <= 0 && <p/>}",
    ];
    for (const form of seitenfragen) expect(funde("x.tsx", `<Pager />\n${form}`), form).toHaveLength(1);
    expect(funde("x.tsx", "<Pager />\n{list.data?.total === 0 && <p/>}")).toEqual([]);
    expect(funde("x.tsx", "{list.data?.items.length === 0 && <p/>}")).toEqual([]);
  });
});
