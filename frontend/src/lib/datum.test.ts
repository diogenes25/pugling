import { describe, expect, it } from "vitest";
import { formatDate, formatDateRange, formatDateTime } from "./datum";

/**
 * B-427. Ein Datum hat im Vater-Web eine deutsche Schreibweise, egal welche Sprache der Browser meldet.
 */
describe("formatDate", () => {
  // AK 3. Rot, wenn ein DateOnly über `new Date()` läuft: in einer Zone westlich von UTC stünde dann der Vortag da. Die
  // Probe dafür läuft mit TZ=America/New_York (Mutante „Textzerlegung weg“ im Verlauf von B-427).
  it("zeigt einen DateOnly als deutschen Kalendertag, ohne Zeitzonen-Umweg", () => {
    expect(formatDate("2026-11-08")).toBe("08.11.2026");
    expect(formatDate("2026-01-01")).toBe("01.01.2026");
  });

  // Rot, wenn ein Zeitstempel roh oder im Browser-Format erscheint.
  it("zeigt einen Zeitstempel als Kalendertag", () => {
    expect(formatDate("2026-11-08T12:00:00Z")).toBe("08.11.2026");
  });

  // Rot, wenn ein unlesbarer Wert „Invalid Date" wird.
  it("lässt Unlesbares stehen", () => { expect(formatDate("kein Datum")).toBe("kein Datum"); });

  // Review-Fund (Sprint 27): der Server liefert gelesene Zeitstempel ohne Offset. Unter TZ=Europe/Berlin rot, wenn sie als
  // Ortszeit gelesen werden (dann stünde der 08.11. statt des 09.11. da, 23:30 statt 00:30).
  it("liest einen Zeitstempel ohne Offset als UTC", () => {
    const berlin = new Intl.DateTimeFormat("de-DE", { timeZone: "Europe/Berlin", day: "2-digit", month: "2-digit", year: "numeric" });
    expect(formatDate("2026-11-08T23:30:00")).toBe(formatDate("2026-11-08T23:30:00Z"));
    expect(berlin.format(new Date("2026-11-08T23:30:00Z"))).toBe("09.11.2026");
  });

  it("zeigt einen Zeitpunkt mit Uhrzeit", () => {
    expect(formatDateTime("2026-11-08T12:00:00Z")).toMatch(/^08\.11\.2026, \d{2}:\d{2}$/);
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
