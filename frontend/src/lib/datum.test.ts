import { describe, expect, it } from "vitest";
import { alsUtcText, arcadeDatum, augenblick, formatDate, formatDateIn, formatDateRange, formatDateTime } from "./datum";

/**
 * B-427. Ein Datum hat im Vater-Web eine deutsche Schreibweise, egal welche Sprache der Browser meldet.
 */
describe("formatDate", () => {
  // AK 3. Rot, wenn ein DateOnly über `new Date()` läuft: in einer Zone westlich von UTC stünde dann der Vortag da. Die Zone
  // ist Parameter (B-449) – die Probe greift damit auf jeder Maschine, nicht nur unter TZ=America/New_York.
  it("zeigt einen DateOnly als deutschen Kalendertag, ohne Zeitzonen-Umweg", () => {
    expect(formatDate("2026-11-08")).toBe("08.11.2026");
    expect(formatDateIn("2026-11-08", "America/New_York")).toBe("08.11.2026");
    expect(formatDateIn("2026-01-01", "America/New_York")).toBe("01.01.2026");
    // B-517: auch östlich von UTC+11 derselbe Tag – rot, wenn ein DateOnly in der Gerätezone formatiert wird.
    expect(formatDateIn("2026-11-08", "Pacific/Kiritimati")).toBe("08.11.2026");
  });

  // Rot, wenn ein Zeitstempel roh oder im Browser-Format erscheint.
  it("zeigt einen Zeitstempel als Kalendertag", () => {
    // 06:00 UTC: derselbe Tag in jeder bewohnten Zone (−12 bis +14) – 12:00 war östlich von UTC+12 schon der Folgetag (B-520).
    expect(formatDate("2026-11-08T06:00:00Z")).toBe("08.11.2026");
  });

  // Rot, wenn ein unlesbarer Wert „Invalid Date" wird.
  it("lässt Unlesbares stehen", () => { expect(formatDate("kein Datum")).toBe("kein Datum"); });

  // Review-Fund (Sprint 27): der Server liefert gelesene Zeitstempel ohne Offset, oft mit sieben Nachkommastellen. Rot,
  // wenn sie als Ortszeit gelesen werden. In einer UTC-Umgebung (der CI) ist „als Ortszeit" derselbe Augenblick – darum
  // läuft dieser Fall in New York (B-517, Review): die Zone wird für ihn gesetzt, Node liest `TZ` zur Laufzeit.
  it("liest einen Zeitstempel ohne Offset als UTC", () => {
    inZone("America/New_York", () =>
      expect(augenblick("2026-11-08T23:30:00").toISOString()).toBe("2026-11-08T23:30:00.000Z"));
    expect(augenblick("2026-11-08T23:30:00").toISOString()).toBe("2026-11-08T23:30:00.000Z");
    expect(augenblick("2026-11-08T23:30:00.1234567").toISOString()).toBe("2026-11-08T23:30:00.123Z");
    expect(augenblick("2026-11-08T23:30").toISOString()).toBe("2026-11-08T23:30:00.000Z");
    expect(augenblick("2026-11-08T23:30:00+01:00").toISOString()).toBe("2026-11-08T22:30:00.000Z");
    expect(formatDateIn("2026-11-08T23:30:00", "Europe/Berlin")).toBe("09.11.2026");
  });

  // B-517: die Text-Regel, als Text geprüft – auf jeder Maschine, auch in der UTC-CI. Rot, wenn kein `Z` angehängt wird,
  // wenn der Bruchteil (bis zu sieben Stellen vom Server) nicht als „ohne Offset" zählt, oder wenn ein Offset doppelt wird.
  it("hängt einem Zeitstempel ohne Offset ein Z an, nur dort", () => {
    expect(alsUtcText("2026-11-08T23:30:00")).toBe("2026-11-08T23:30:00Z");
    expect(alsUtcText("2026-11-08T23:30:00.1234567")).toBe("2026-11-08T23:30:00.1234567Z");
    expect(alsUtcText("2026-11-08T23:30")).toBe("2026-11-08T23:30Z");
    expect(alsUtcText("2026-11-08T23:30:00Z")).toBe("2026-11-08T23:30:00Z");
    expect(alsUtcText("2026-11-08T23:30:00+01:00")).toBe("2026-11-08T23:30:00+01:00");
  });

  it("zeigt einen Zeitpunkt mit Uhrzeit", () => {
    expect(formatDateTime("2026-11-08T06:00:00Z")).toMatch(/^08\.11\.2026, \d{2}:\d{2}$/);
  });

  // AK 2. Rot, wenn der Zeitraum ISO bleibt.
  it("zeigt einen Zeitraum", () => {
    expect(formatDateRange("2026-11-08", "2026-11-21")).toBe("08.11.2026 – 21.11.2026");
  });
});

/**
 * AK 1, Wächter: kein `toLocaleDateString()`/`toLocaleString()`/`toLocaleTimeString()` **ohne** Locale in `src` – dort entschiede
 * der Browser die Schreibweise. Wer formatiert, nimmt `formatDate`/`formatDateTime`; eine Zahl mit fester Locale
 * (`toLocaleString("de-DE", …)`) ist erlaubt. Rot, wenn ein solcher Aufruf neu entsteht.
 */
const QUELLEN = import.meta.glob(["../**/*.tsx", "../**/*.ts", "!../**/*.test.tsx", "!../**/*.test.ts"], {
  query: "?raw", import: "default", eager: true,
}) as Record<string, string>;

const OHNE_LOCALE = /\.toLocale(?:Date|Time)?String\(\s*(?:\)|undefined\b|\[\s*\])|\bIntl\.DateTimeFormat\(\s*(?:\)|undefined\b)/g;

function funde(datei: string, text: string): string[] {
  return [...text.matchAll(OHNE_LOCALE)].map((m) =>
    `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[0]} ohne Locale – im Vater-Web formatDate/formatDateTime aus lib/datum.ts, `
    + `in der Arcade mit i18n.language formatieren`);
}

describe("Kein Datum ohne Locale (B-427)", () => {
  it("keine Quelle formatiert ohne Locale", () => {
    expect(Object.keys(QUELLEN).length).toBeGreaterThan(50);
    expect(Object.entries(QUELLEN).flatMap(([d, t]) => funde(d, t))).toEqual([]);
  });

  it.each([
    ["Datum ohne Locale", "new Date(x).toLocaleDateString()", 1],
    ["Zeitpunkt ohne Locale", "new Date(x).toLocaleString()", 1],
    ["Uhrzeit ohne Locale", "d.toLocaleTimeString( )", 1],
    ["Locale ausdrücklich offen", "d.toLocaleDateString(undefined, { weekday: \"short\" })", 1],
    ["leere Locale-Liste", "d.toLocaleDateString([])", 1],
    ["Intl ohne Locale", "new Intl.DateTimeFormat()", 1],
    ["Intl mit fester Locale", "new Intl.DateTimeFormat(\"de-DE\", { day: \"2-digit\" })", 0],
    ["Zahl mit fester Locale", `n.toLocaleString("de-DE", { minimumFractionDigits: 1 })`, 0],
    ["Datum mit Sprache", "new Date(iso).toLocaleDateString(i18n.language)", 0],
  ])("%s", (_name, quelle, erwartet) => {
    expect(funde("x.tsx", quelle)).toHaveLength(erwartet as number);
  });
});

/**
 * B-447: die Arcade zeigt Daten in ihrer Sprache – nie als ISO-Text, nie als deutscher Tag in der englischen Arcade, und mit
 * derselben Lese-Hälfte wie das Vater-Web.
 */
describe("arcadeDatum", () => {
  // Rot, wenn ein DateOnly über die Ortszeit läuft (westlich von Greenwich stünde der Vortag) oder roh bleibt.
  it("zeigt einen DateOnly als genau diesen Tag in der Sprache der Arcade", () => {
    expect(arcadeDatum("2026-11-08", "de")).toBe("8.11.2026");
    expect(arcadeDatum("2026-11-08", "en-US")).toBe("11/8/2026");
    // B-517 (Review): auch östlich von UTC+11 derselbe Tag – rot, wenn ein DateOnly in der übergebenen Zone formatiert wird.
    expect(arcadeDatum("2026-11-08", "de", "Pacific/Kiritimati")).toBe("8.11.2026");
    // … und auf einem Gerät dort (Zone zur Laufzeit gesetzt): rot, wenn ein DateOnly in der Gerätezone formatiert wird.
    // (`formatDate` gehört nicht hierher: sein Formatierer ist beim Import gebaut und hält dessen Zone – B-520; die Regel
    // für das Vater-Web hält `formatDateIn(…, "Pacific/Kiritimati")` oben.)
    inZone("Pacific/Kiritimati", () => expect(arcadeDatum("2026-11-08", "de")).toBe("8.11.2026"));
  });

  // Die Lese-Hälfte teilt sich die Arcade mit dem Vater-Web (`lies` → `augenblick`): ob ein Zeitstempel ohne Offset als
  // UTC gelesen wird, prüft zonenunabhängig der `augenblick`-Test oben. Hier nur, dass die Zone ankommt.
  it("zeigt einen Zeitstempel in der Zone, die man ihm gibt", () => {
    expect(arcadeDatum("2026-11-08T23:30:00Z", "de", "Europe/Berlin")).toBe("9.11.2026");
    expect(arcadeDatum("2026-11-08T23:30:00Z", "de", "UTC")).toBe("8.11.2026");
  });

  it("lässt Unlesbares stehen", () => { expect(arcadeDatum("kein Datum", "de")).toBe("kein Datum"); });
});

/**
 * B-447, Wächter: die Arcade formatiert Daten nur über `arcadeDatum` – ein eigenes `new Date(iso)` liest einen Zeitstempel
 * ohne Offset als Ortszeit, ein `toLocaleDateString` daneben zeigt ISO-Text oder die falsche Sprache. Rot, wenn eine
 * Arcade-Quelle wieder selbst ein Datum baut oder formatiert.
 */
describe("Die Arcade formatiert Daten nur über arcadeDatum (B-447)", () => {
  const ARCADE = Object.entries(QUELLEN).filter(([datei]) => datei.startsWith("../sohn/"));
  // `new Date(` auch ohne Argument und `toLocaleString` auch für Zahlen sind bewusst Funde: in der Arcade gibt es heute
  // keins, und wer eins braucht, schreibt es in lib/ – dort, wo die Lese-Regel steht.
  const SELBST = /new Date\(|\bDate\.parse\(|\bIntl\.DateTimeFormat\(|\.toLocale(?:Date|Time)?String\(/g;

  // B-517: auch kein rohes Datum – ein Wert, der als `date` in einen Satz oder als Titel ins Bild geht, läuft durch
  // `arcadeDatum`. Rot, wenn `{ date: o.dueDate }` oder `title={d.day}` zurückkehrt (die drei Stellen von B-447).
  it("keine Arcade-Quelle gibt ein Datum roh in Satz oder Titel", () => {
    const ROH = /\bdate:\s*(?!arcadeDatum\(|\()[A-Za-z_$][\w$.?]*|title=\{\s*[A-Za-z_$][\w$.?]*\.(?:day|dueDate|dueOn|date)\s*\}/g;
    const funde = ARCADE.flatMap(([datei, text]) => [...text.matchAll(ROH)]
      .map((m) => `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[0]} – arcadeDatum aus lib/datum.ts nehmen`));
    expect(funde).toEqual([]);
    // Selbsttest: die alten Formen sind Funde, die neuen nicht.
    expect("{ date: o.dueDate }".match(ROH)).not.toBeNull();
    expect("title={d.day}".match(ROH)).not.toBeNull();
    expect("{ date: arcadeDatum(o.dueDate, l) }".match(ROH)).toBeNull();
    expect("date: (iso: string) =>".match(ROH)).toBeNull();
  });

  it("keine Arcade-Quelle baut oder formatiert ein Datum selbst", () => {
    // Selbsttest (B-517): jede genannte Form ist ein Fund – ein Steuerzeichen im Muster ließ zwei davon nie greifen.
    for (const form of ["new Date(x)", "Date.parse(x)", "new Intl.DateTimeFormat(l)", "d.toLocaleDateString(l)"])
      expect(form.match(SELBST), form).not.toBeNull();
    expect(ARCADE.length).toBeGreaterThan(5);
    const funde = ARCADE.flatMap(([datei, text]) => [...text.matchAll(SELBST)]
      .map((m) => `${datei}:${text.slice(0, m.index).split("\n").length}: ${m[0]} – arcadeDatum aus lib/datum.ts nehmen`));
    expect(funde).toEqual([]);
  });
});

/**
 * Führt `fn` in einer Zone aus und stellt die vorige wieder her (B-520): war `TZ` vorher nicht gesetzt, wird es gelöscht –
 * eine Zuweisung von `undefined` schriebe den Text „undefined", und Node rechnete den Rest der Datei in `Etc/Unknown`.
 */
function inZone(zone: string, fn: () => void): void {
  const vorher = process.env.TZ;
  process.env.TZ = zone;
  try {
    fn();
  } finally {
    if (vorher === undefined) delete process.env.TZ;
    else process.env.TZ = vorher;
  }
}

