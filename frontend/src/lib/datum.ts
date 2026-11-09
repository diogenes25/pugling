/**
 * Die eine deutsche Schreibweise für Datum und Zeitpunkt im Vater-Web (B-427): „08.11.2026" bzw. „08.11.2026, 14:03".
 *
 * Vorher entschieden zwölf Stellen mit `toLocaleDateString()` ohne Locale selbst – ein Browser auf en-US zeigte „11/8/2026"
 * neben ISO-Strings wie „2026-11-08". Die Locale steht darum hier fest, nicht beim Browser; das Tor `datum.test.ts` meldet
 * jeden neuen Aufruf ohne Locale.
 *
 * Zwei Sorten Wert kommen vom Server, und sie werden verschieden gelesen:
 * - ein `DateOnly` („2026-11-08") ist ein Kalendertag ohne Uhrzeit. Er wird als **Text** zerlegt, nie über `new Date()`:
 *   das läse ihn als Mitternacht UTC, und westlich von Greenwich stünde der Vortag da.
 * - ein Zeitstempel (ISO-8601 mit Uhrzeit) ist ein Augenblick und wird in der Zeitzone des Geräts gezeigt. Der Server schreibt
 *   alle Zeitstempel in UTC (`DateTime.UtcNow`), liefert gelesene aber ohne Offset (EF/SQLite: `Kind=Unspecified`, kein
 *   UTC-Konverter) – `new Date("…T23:30:00")` läse das als Ortszeit. Ein Zeitstempel ohne Offset bekommt darum ein `Z`.
 *
 * Bewusst: ein Zeitstempel zeigt den Kalendertag des **Geräts**, ein `DateOnly` des Servers den UTC-Tag. Eine Buchung um 23:30 UTC
 * steht damit auf einem Gerät in MEZ am Folgetag, im Tagesverlauf (Server-Tag) am Vortag – Menschen lesen Zeitpunkte in Ortszeit,
 * und die Tagesgrenze der Pflicht ist eine Server-Regel (`CLAUDE.md`, Zeit/UTC), keine Anzeige.
 *
 * Die Sohn-Arcade bleibt außen vor: sie spricht über `t()` und formatiert mit der gewählten Sprache (`SohnShop`).
 */

const NUR_TAG = /^(\d{4})-(\d{2})-(\d{2})$/;
// Ein Zeitstempel ohne `Z` und ohne `±hh:mm` am Ende – so liefert der Server gelesene Werte.
const OHNE_OFFSET = /T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?$/;

/** Ein Server-Zeitstempel als Augenblick: ohne Offset ist er UTC (siehe oben), sonst gilt sein Offset. */
function augenblick(value: string): Date {
  return new Date(OHNE_OFFSET.test(value) ? `${value}Z` : value);
}
const TAG = new Intl.DateTimeFormat("de-DE", { day: "2-digit", month: "2-digit", year: "numeric" });
const ZEITPUNKT = new Intl.DateTimeFormat("de-DE", {
  day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit",
});

/** Ein Datum als „08.11.2026" – ein `DateOnly` per Textzerlegung, ein Zeitstempel als Kalendertag des Geräts. */
export function formatDate(value: string): string {
  const tag = NUR_TAG.exec(value);
  if (tag) return `${tag[3]}.${tag[2]}.${tag[1]}`;
  const d = augenblick(value);
  // Unlesbares lieber roh zeigen als „Invalid Date" – der Wert ist dann wenigstens erkennbar.
  return Number.isNaN(d.getTime()) ? value : TAG.format(d);
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
