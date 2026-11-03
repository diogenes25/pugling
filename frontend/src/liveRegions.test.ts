import { describe, expect, it } from "vitest";

/*
 * Wächter für B-134: eine Live-Region (`role="status"` oder `aria-live`) muss im DOM stehen, BEVOR ihr Text
 * hineinkommt. Viele Screenreader sagen nur an, was in eine schon vorhandene Region hineinwächst; entsteht
 * die Region zusammen mit ihrem Text (`{error && <div role="status">…}`), bleibt die Meldung stumm. Die Regel
 * und ihre Begründung stehen an `StatusBanner.tsx`; dieser Test hält sie dort, wo niemand den Kommentar liest.
 *
 * Gemeldet wird ein öffnendes Tag mit `role="status"` oder `aria-live` – oder einer der Bausteine, die selbst
 * eine solche Region rendern (`REGION_BAUSTEINE`) –, das
 *   (a) direkt hinter `&&`, `?` oder `:` steht (auch über eine Klammer und Zeilenumbrüche hinweg), oder
 *   (b) in einer Funktion steht, die davor schon `return null` liefern kann.
 * Richtig ist die Region, die immer gerendert wird und nur ihren INHALT bedingt trägt
 * (`<div role="status" aria-live="polite">{error && …}</div>`, Vorbild `StatusBanner`).
 *
 * Bewusst NICHT gemeldet: `role="alert"`. Ein eingefügtes `alert` wird zuverlässig angesagt; das Repo nutzt
 * es absichtlich für Fehler, die laut sein sollen (B-188, `ListenAusfall`).
 *
 * Ein halber Parser: (b) erkennt die umgebende Funktion an ihrer Kopfzeile in Spalte 0, nicht am Syntaxbaum – eine
 * eingerückte innere Funktion (`function save()` im Komponentenrumpf) beendet die Suche nach oben nicht. Ein
 * Fehlalarm gehört mit Grund in `AUSNAHMEN`, nicht in einen Umbau der Erkennung. Bekannte Lücke: eine Region in
 * einem bedingten Wrapper (`{x && <div><p role="status">…}`) sieht er nicht – vor ihr steht `>`, keine Bedingung.
 * Ebenso hält er den Block eines `if` mit Lambda in der Bedingung (`if (xs.some((x) => x.done)) {`) für einen
 * Funktionsrumpf und übersieht ein `return null` darin (gemessen: heute nirgends mit Region dahinter).
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
  {
    datei: "./components/RemarkWidget.tsx",
    tag: "<StatusBanner message={action.message} />",
    grund: "Das `return null` davor schaltet das ganze Widget ab (E2E, `pugling.remarks.off`) und hängt nicht an "
      + "einer Meldung: Ist das Widget da, steht auch die Region, bevor eine Meldung hineinkommt.",
  },
];

const LIVE = /\b(role="status"|aria-live=)/;
/**
 * Bausteine, die selbst eine Live-Region rendern. Wer sie bedingt einhängt (`{x && <StatusBanner …/>}`), baut die
 * Region zusammen mit ihrem Text – genau der Fall, an dem B-134 gefunden wurde (`VaterLehrwerke.tsx`).
 */
const REGION_BAUSTEINE = ["StatusBanner", "TruncationHint", "SavedNotice", "TaskAnnouncer", "DerivedHint", "DuplicateHint"];
const BAUSTEIN = new RegExp(`^<(${REGION_BAUSTEINE.join("|")})\\b`);
const FUNKTIONSKOPF = /^(export\s+)?(default\s+)?function\s|^(export\s+)?const\s+\w+\s*=\s*(\([^)]*\)|\w+)\s*=>/;

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
    if (!LIVE.test(m[0]) && !BAUSTEIN.test(m[0])) continue;
    const zeile = zeileVon(m.index);
    const davor = text.slice(Math.max(0, m.index - 200), m.index).replace(/\{?\/\*[\s\S]*?\*\/\}?|\/\/[^\n]*/g, "").trimEnd();
    if (/(&&|\?|:)\s*\(?\s*$/.test(davor)) {
      funde.push(`${datei}:${zeile + 1} steht hinter einer Bedingung (${davor.slice(-30).replace(/\s+/g, " ").trim()}) ${m[0]}`);
      continue;
    }
    let kopf = zeile;
    while (kopf > 0 && !FUNKTIONSKOPF.test(zeilen[kopf])) kopf--;
    const frueh = fruehesReturnNull(zeilen.slice(kopf, zeile));
    if (frueh !== null) {
      funde.push(`${datei}:${zeile + 1} steht nach einem \`return null\` (Zeile ${kopf + frueh + 1}) ${m[0]}`);
    }
  }
  return funde;
}

/**
 * Komponenten einer Quelle, deren eigenes `return` (zwei Leerzeichen eingerückt, also das der Funktion selbst) als
 * erstes Element eine Live-Region oder einen Region-Baustein liefert – die Kandidaten für `REGION_BAUSTEINE`.
 */
function wurzelRegionen(quelle: string): string[] {
  const namen: string[] = [];
  const zeilen = quelle.split(/\r?\n/);
  zeilen.forEach((zeile, i) => {
    const kopf = /^(?:export\s+)?function\s+([A-Z]\w*)\s*\(/.exec(zeile);
    if (!kopf) return;
    const ende = zeilen.findIndex((z, j) => j > i && /^}\s*$/.test(z));
    const rumpf = zeilen.slice(i + 1, ende < 0 ? undefined : ende);
    const ret = rumpf.findIndex((z) => /^ {2}return\b/.test(z));
    if (ret < 0) return;
    const rest = rumpf.slice(ret).join("\n").replace(/^\s*return\s*\(?/, "")
      .replace(/^(\s*(\/\/[^\n]*|\{?\/\*[\s\S]*?\*\/\}?))*/, "").trimStart();
    const tag = /^<([A-Za-z][\w.]*)\b(?:=>|[^<>])*?>/s.exec(rest);
    if (tag && (LIVE.test(tag[0]) || BAUSTEIN.test(tag[0]))) namen.push(kopf[1]);
  });
  return namen;
}

/**
 * Erste Zeile (ab dem Funktionskopf gezählt), in der die Funktion selbst `return null` liefern kann – `null`, wenn
 * es keine gibt. Ein `return null` in einer inneren Funktion oder einem Lambda zählt nicht: dort beendet es nur
 * diese (`function canAdvance(): string | null` im Assistenten). Erkannt an der Klammertiefe: jede `{`, deren
 * Zeile davor `function` oder `=>` trägt, öffnet einen Funktionsrumpf; die äußerste ist die Funktion selbst.
 */
function fruehesReturnNull(rumpf: string[]): number | null {
  const rahmen: boolean[] = [];
  for (let i = 0; i < rumpf.length; i++) {
    const zeile = rumpf[i];
    for (let c = 0; c < zeile.length; c++) {
      if (zeile[c] === "{") rahmen.push(/\bfunction\b|=>/.test(zeile.slice(0, c)));
      else if (zeile[c] === "}") rahmen.pop();
      else if (zeile.startsWith("return null", c) && rahmen.slice(1).every((fn) => !fn)) return i;
    }
  }
  return null;
}

describe("Live-Regionen stehen vor ihrem Text im DOM (B-134)", () => {
  it("keine Datei rendert eine Live-Region erst zusammen mit ihrer Meldung", () => {
    const funde = Object.entries(QUELLEN)
      .flatMap(([datei, quelle]) => findeBedingteLiveRegionen(datei, quelle))
      .filter((f) => !AUSNAHMEN.some((a) => f.startsWith(`${a.datei}:`) && f.includes(a.tag)));
    expect(funde, "Region immer rendern und nur den Inhalt bedingt tragen – Vorbild StatusBanner").toEqual([]);
  });

  // Rot, wenn eine Ausnahme überholt ist: das Tag steht noch, meldet aber nichts mehr – der Eintrag deckte dann einen
  // künftigen echten Fund mit (B-272).
  it("die Ausnahmeliste nennt nur Funde, die es gibt, und jeden mit Grund", () => {
    for (const { datei, tag, grund } of AUSNAHMEN) {
      expect(QUELLEN[datei]?.includes(tag), `${datei}: Tag ${tag} gibt es nicht mehr – Eintrag streichen`).toBe(true);
      expect(findeBedingteLiveRegionen(datei, QUELLEN[datei]).some((f) => f.includes(tag)),
        `${datei}: ${tag} erzeugt keinen Fund mehr – Eintrag streichen`).toBe(true);
      expect(grund.trim().length, `${datei} ohne Grund`).toBeGreaterThan(10);
    }
  });

  // Rot, wenn ein Name aus der Liste fällt oder ein neuer Baustein mit Region entsteht, ohne hineinzukommen: dann dürfte
  // genau er wieder bedingt hängen (B-272). Die Liste wird darum gegen die Quellen geprüft, nicht gegen sich selbst.
  it("REGION_BAUSTEINE nennt jede Komponente, deren Wurzel eine Region oder ein solcher Baustein ist", () => {
    const gefunden = Object.values(QUELLEN).flatMap(wurzelRegionen).sort();
    expect(gefunden.length).toBeGreaterThanOrEqual(5);
    expect(gefunden).toEqual([...REGION_BAUSTEINE].sort());
  });

  it.each(REGION_BAUSTEINE)("Baustein %s hinter && wird gemeldet", (name) => {
    expect(findeBedingteLiveRegionen("x.tsx", `{x && <${name} a={1} />}`)).toHaveLength(1);
  });

  // Die Erkennung selbst: jeder Fall benennt die Form, die ihn rot macht.
  it.each([
    ["&& direkt", `{error && <div role="status" aria-live="polite">{error}</div>}`, 1],
    ["&& mit Klammer und Umbruch", `{ok && (\n  <div className="banner ok" role="status">x</div>\n)}`, 1],
    ["Ternär", `{busy ? <span aria-live="polite">…</span> : null}`, 1],
    // B-272: rot, wenn `:` aus der Bedingungs-Regex fällt.
    ["Ternär, Region im Else-Zweig", `{busy ? null : <p role="status">fertig</p>}`, 1],
    // B-359: rot, wenn Kommentare vor dem Tag nicht entfernt werden – dann steht `warum` bzw. `*/` davor statt `&&`. Nur
    // diese zwei Formen kompilieren zwischen `&&` und dem Tag (B-272 maß an `{/* … */}`, das dort kein gültiges TSX ist).
    ["&& mit Zeilenkommentar davor", `{ok && ( // warum\n  <div role="status">x</div>\n)}`, 1],
    ["&& mit Blockkommentar davor", `{ok && /* warum */ <div role="status">x</div>}`, 1],
    // B-272: rot, wenn die Pfeil-Form aus dem Funktionskopf fällt – dann sucht die Erkennung bis in die Funktion davor.
    ["return null in der Funktion vor einer Pfeil-Komponente",
      `function A() { return null; }
const B = () => {
  return <p role="status">x</p>;
};`, 0],
    ["return null davor", `function H({ n }: P) {\n  if (n === 0) return null;\n  return <p role="status">{n}</p>;\n}`, 1],
    ["dauerhafte Region", `<div role="status" aria-live="polite">{error && <div>{error}</div>}</div>`, 0],
    ["return null in einer anderen Funktion", `function A() { return null; }\nfunction B() {\n  return <p role="status">x</p>;\n}`, 0],
    ["role=alert ist erlaubt", `{error && <div role="alert">{error}</div>}`, 0],
    ["Baustein hinter &&", `{saved && <StatusBanner message={m} />}`, 1],
    ["Baustein dauerhaft", `<TruncationHint shown={n} total={stale ? 0 : t} />`, 0],
    ["Baustein-Name nur als Präfix", `{x && <StatusBannerRow a={1} />}`, 0],
    ["return null nur in einer inneren Funktion",
      `export function W() {\n  function can(): string | null {\n    return null;\n  }\n  return <p role="status">x</p>;\n}`, 0],
    ["return null nur in einem Lambda",
      `export function W() {\n  const v = useMemo(() => { if (!d) return null; return 1; }, [d]);\n  return <p role="status">x</p>;\n}`, 0],
    ["return null in einem if-Block der Funktion selbst",
      `export function W() {\n  if (!d) {\n    return null;\n  }\n  return <p role="status">x</p>;\n}`, 1],
    ["return null vor einer inneren Funktion",
      `export function W() {\n  if (!d) return null;\n  function save() {\n    go();\n  }\n  return <p role="status">x</p>;\n}`, 1],
  ])("%s", (_name, quelle, erwartet) => {
    expect(findeBedingteLiveRegionen("x.tsx", quelle)).toHaveLength(erwartet as number);
  });
});
