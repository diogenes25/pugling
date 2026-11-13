import { describe, expect, it } from "vitest";

/**
 * B-505: eine geblätterte Liste sagt „noch keine …" nur, wenn der **Bestand** leer ist (`total === 0`), nicht die Seite.
 * Eine leere Seite hinter dem Bestand korrigiert der `Pager` (B-505); bis dahin – und in jeder Liste, die ihn mitlaufen
 * lässt – behauptete eine Frage nach der Länge der Seite „nichts da" bei vollem Bestand. Gelesen wird jede Quelle, die
 * einen `<Pager` rendert.
 *
 * B-512: das Muster ist **umgedreht** – nicht die bekannten Formen der Seitenfrage werden gesucht (es gibt zu viele:
 * Ternäre, `> 0`, `== 0`, `!`), sondern jede Länge einer geladenen Seite (`x.data?.items.length`, auch mit `!` oder
 * ohne `?`) ist ein Fund, außer an der einen erlaubten Stelle: `TruncationHint shown=` zählt die gezeigten Zeilen und
 * soll das. **Bekannte Lücke:** eine Zwischenvariable (`const rows = x.data?.items ?? []; rows.length`) sieht er nicht.
 * Rot, wenn eine geblätterte Liste die Länge ihrer Seite liest, oder wenn sie ihren Leerfall am Bestand ganz verliert.
 */
const QUELLEN = import.meta.glob(["./**/*.tsx", "!./**/*.test.tsx"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

const SEITENLAENGE = /\b([A-Za-z_$][\w$]*)\.data[?!]?\.items\??\.length\b/g;
const ERLAUBT = /<TruncationHint\s+shown=\{[^}]*$/;
const BESTAND_LEER = /\.total\s*(?:\?\?\s*0\s*\)\s*)?===\s*0/;

function funde(datei: string, text: string): string[] {
  if (!text.includes("<Pager")) return [];
  const seitenfragen = [...text.matchAll(SEITENLAENGE)]
    .filter((m) => !ERLAUBT.test(text.slice(text.lastIndexOf("\n", m.index) + 1, m.index)))
    .map((m) => `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[1]}.data?.items.length – den Bestand lesen (total === 0)`);
  const ohneLeerfall = BESTAND_LEER.test(text) ? [] : [`${datei}: kein Leerfall am Bestand (total === 0)`];
  return [...seitenfragen, ...ohneLeerfall];
}

describe("Der Leerfall einer geblätterten Liste liest den Bestand (B-505, B-512)", () => {
  it("keine geblätterte Liste liest die Länge ihrer Seite, und jede hat einen Leerfall am Bestand", () => {
    const geblaettert = Object.entries(QUELLEN).filter(([, text]) => text.includes("<Pager"));
    expect(geblaettert.length).toBeGreaterThan(0);
    expect(geblaettert.flatMap(([datei, text]) => funde(datei, text))).toEqual([]);
  });

  // Selbsttest: jede Form der Seitenfrage ist ein Fund, die erlaubte Stelle und die Bestandsfrage nicht – sonst wäre der
  // Wächter oben leer.
  it("erkennt jede Form der Seitenfrage und lässt die Bestandsfrage durch", () => {
    const leer = "{list.data?.total === 0 && <p/>}";
    const seitenfragen = [
      "{list.data?.items.length === 0 && <p/>}",
      "{(list.data?.items.length ?? 0) === 0 ? <p/> : null}",
      "{!list.data?.items.length && <p/>}",
      "{list.data.items.length < 1 && <p/>}",
      "{list.data?.items.length ? <table/> : <p/>}",
      "{list.data!.items.length > 0 ? rows : <p/>}",
      "{list.data?.items?.length == 0 && <p/>}",
    ];
    for (const form of seitenfragen) expect(funde("x.tsx", `<Pager />\n${leer}\n${form}`), form).toHaveLength(1);
    expect(funde("x.tsx", `<Pager />\n${leer}\n<TruncationHint shown={list.data?.items.length ?? 0} total={1} />`)).toEqual([]);
    expect(funde("x.tsx", `<Pager />\n${leer}`)).toEqual([]);
    expect(funde("x.tsx", "<Pager />\n{list.data?.items.length === 0 && <p/>}")).toHaveLength(2);
    expect(funde("x.tsx", "{list.data?.items.length === 0 && <p/>}")).toEqual([]);
  });
});
