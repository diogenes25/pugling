/**
 * Die eine deutsche Schreibweise für Datum und Zeitpunkt im Vater-Web (B-427): „08.11.2026" bzw. „08.11.2026, 14:03".
 *
 * Vorher entschieden zwölf Stellen mit `toLocaleDateString()` ohne Locale selbst – ein Browser auf en-US zeigte „11/8/2026"
 * neben ISO-Strings wie „2026-11-08". Die Locale steht darum hier fest, nicht beim Browser; das Tor `datum.test.ts` meldet
 * jeden neuen Aufruf ohne Locale.
 *
 * Zwei Sorten Wert kommen vom Server, und sie werden verschieden gelesen:
 * - ein `DateOnly` („2026-11-08") ist ein Kalendertag ohne Uhrzeit. Er wird als **Text** zerlegt und als Mittag UTC
 *   gelesen, nie über `new Date()`: das läse ihn als Mitternacht UTC, und westlich von Greenwich stünde der Vortag da.
 * - ein Zeitstempel (ISO-8601 mit Uhrzeit) ist ein Augenblick und wird in der Zeitzone des Geräts gezeigt. Der Server schreibt
 *   alle Zeitstempel in UTC (`DateTime.UtcNow`), liefert gelesene aber ohne Offset (EF/SQLite: `Kind=Unspecified`, kein
 *   UTC-Konverter) – `new Date("…T23:30:00")` läse das als Ortszeit. Ein Zeitstempel ohne Offset bekommt darum ein `Z`.
 *
 * Bewusst: ein Zeitstempel zeigt den Kalendertag des **Geräts**, ein `DateOnly` des Servers den UTC-Tag. Eine Buchung um 23:30 UTC
 * steht damit auf einem Gerät in MEZ am Folgetag, im Tagesverlauf (Server-Tag) am Vortag – Menschen lesen Zeitpunkte in Ortszeit,
 * und die Tagesgrenze der Pflicht ist eine Server-Regel (`CLAUDE.md`, Zeit/UTC), keine Anzeige.
 *
 * Die Sohn-Arcade spricht über `t()` und formatiert mit der gewählten Sprache – dafür `arcadeDatum` (B-447), mit derselben
 * Lese-Hälfte (Textzerlegung, `augenblick`), sonst stünde in der englischen Arcade ein deutsches Datum.
 */

const NUR_TAG = /^(\d{4})-(\d{2})-(\d{2})$/;
// Ein Zeitstempel ohne `Z` und ohne `±hh:mm` am Ende – so liefert der Server gelesene Werte.
const OHNE_OFFSET = /T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?$/;

/**
 * Ein Server-Zeitstempel als Augenblick: ohne Offset ist er UTC (siehe oben), sonst gilt sein Offset. Exportiert, damit
 * der Test ihn zonenunabhängig über `toISOString()` prüfen kann (B-449).
 */
export function augenblick(value: string): Date {
  return new Date(OHNE_OFFSET.test(value) ? `${value}Z` : value);
}
const TAG = new Intl.DateTimeFormat("de-DE", { day: "2-digit", month: "2-digit", year: "numeric" });
const ZEITPUNKT = new Intl.DateTimeFormat("de-DE", {
  day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
});

/** Ein Datum als „08.11.2026" – ein `DateOnly` als genau dieser Tag, ein Zeitstempel als Kalendertag des Geräts. */
export function formatDate(value: string): string {
  return formatDateIn(value);
}

/**
 * `formatDate` mit einer Zeitzone statt der des Geräts. Die Zone ist Parameter, nicht Umgebung, damit ein Test die
 * Regel auf jeder Maschine prüft – lokal Berlin, in der CI UTC (B-449).
 */
export function formatDateIn(value: string, timeZone?: string): string {
  const d = lies(value);
  // Unlesbares lieber roh zeigen als „Invalid Date" – der Wert ist dann wenigstens erkennbar.
  if (Number.isNaN(d.getTime())) return value;
  return timeZone ? new Intl.DateTimeFormat("de-DE", { timeZone, day: "2-digit", month: "2-digit", year: "numeric" }).format(d)
    : TAG.format(d);
}

/**
 * Ein Datum in der Sprache der Arcade (B-447): ein `DateOnly` als genau dieser Kalendertag (nie über die Ortszeit), ein
 * Zeitstempel als Kalendertag des Geräts. `timeZone` nur für Tests, wie bei `formatDateIn`.
 */
export function arcadeDatum(value: string, language: string, timeZone?: string): string {
  const d = lies(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString(language, NUR_TAG.test(value) ? { timeZone: "UTC" } : timeZone ? { timeZone } : undefined);
}

/**
 * Die eine Lese-Hälfte für Vater-Web und Arcade (Review B-449/B-447): ein `DateOnly` als **Mittag** UTC dieses Tages – so
 * verschiebt auch eine Formatierung in Ortszeit den Tag zwischen UTC−12 und UTC+11 nicht –, ein Zeitstempel als
 * `augenblick`. Weil beide Formatierer hier lesen, prüft der zonenunabhängige `augenblick`-Test die Arcade mit.
 */
function lies(value: string): Date {
  const tag = NUR_TAG.exec(value);
  return tag ? new Date(Date.UTC(Number(tag[1]), Number(tag[2]) - 1, Number(tag[3]), 12)) : augenblick(value);
}

/** Ein Zeitpunkt als „08.11.2026, 14:03" (Zeitzone des Geräts). */
export function formatDateTime(value: string): string {
  const d = augenblick(value);
  return Number.isNaN(d.getTime()) ? value : ZEITPUNKT.format(d);
}

/** Ein Zeitraum als „08.11.2026 – 21.11.2026". */
export function formatDateRange(from: string, to: string): string {
  return `${formatDate(from)} – ${formatDate(to)}`;
}
