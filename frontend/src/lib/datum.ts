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
 * Bewusst: ein Zeitstempel zeigt den Kalendertag des **Geräts**, ein `DateOnly` des Servers den Lerntag (deutsche Zeit,
 * B-730). Auf einem Gerät in deutscher Zeit fallen beide zusammen; die Tagesgrenze der Pflicht bleibt eine Server-Regel,
 * keine Anzeige – darum rechnen die Fristen unten mit `LERNTAG_ZONE` und nicht mit der Zone des Geräts.
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
  return new Date(alsUtcText(value));
}

/**
 * Die Text-Regel hinter `augenblick`: ein Zeitstempel ohne Offset bekommt ein `Z`. Eigene Funktion, weil nur ein Vergleich
 * von **Text** zonenunabhängig ist – in einer UTC-Umgebung (der CI) ist „ohne `Z` als Ortszeit gelesen" derselbe Augenblick
 * wie „mit `Z`", ein Test über `Date` sähe den Fehler dort nicht (B-517).
 */
export function alsUtcText(value: string): string {
  return OHNE_OFFSET.test(value) ? `${value}Z` : value;
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
  // Ein `DateOnly` ist ein Kalendertag, kein Augenblick: in UTC formatiert (wie in der Arcade), sonst stünde östlich von
  // UTC+11 der Folgetag da (B-517).
  const zone = NUR_TAG.test(value) ? "UTC" : timeZone;
  return zone ? new Intl.DateTimeFormat("de-DE", { timeZone: zone, day: "2-digit", month: "2-digit", year: "numeric" }).format(d)
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

/**
 * Die Zone des Lerntags (B-730): Tag und Woche enden für den Server um Mitternacht **deutscher Zeit**
 * (`LearningDay:TimeZone` in `backend/Pugling.Api/appsettings.json`). Hier gespiegelt, weil der Browser die Zone des
 * Servers nicht kennt; ändert sie sich dort, ändert sie sich hier.
 */
export const LERNTAG_ZONE = "Europe/Berlin";

// Ein Formatierer je Zone: `teile` läuft je Position und Render mehrmals, das Anlegen ist der teure Teil.
const FORMATIERER = new Map<string, Intl.DateTimeFormat>();

/** Kalendertag, Wochentag und Uhrzeit eines Augenblicks in `zone`. */
function teile(jetzt: Date, zone: string) {
  let f = FORMATIERER.get(zone);
  if (!f) {
    f = new Intl.DateTimeFormat("en-US", {
      timeZone: zone, year: "numeric", month: "2-digit", day: "2-digit", weekday: "short",
      hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23",
    });
    FORMATIERER.set(zone, f);
  }
  const p = Object.fromEntries(f.formatToParts(jetzt).map((x) => [x.type, x.value]));
  return {
    jahr: Number(p.year), monat: Number(p.month), tag: Number(p.day), wochentag: p.weekday,
    stunde: Number(p.hour), minute: Number(p.minute), sekunde: Number(p.second),
  };
}

/** Wie weit `zone` im Augenblick `ms` der UTC voraus ist, in Millisekunden (Winter +1 h, Sommer +2 h). */
function versatz(ms: number, zone: string): number {
  const t = teile(new Date(ms), zone);
  return Date.UTC(t.jahr, t.monat - 1, t.tag, t.stunde, t.minute, t.sekunde) - Math.floor(ms / 1000) * 1000;
}

/**
 * Der heutige Lerntag als `DateOnly`-Text („2026-11-08") – der Tag, den der Server „heute" nennt. Nicht
 * `toISOString()`: das ist der UTC-Tag und belegte ein Formular um 00:30 Ortszeit mit gestern vor (B-730).
 */
export function heuteImLerntag(jetzt: Date = new Date(), zone: string = LERNTAG_ZONE): string {
  const t = teile(jetzt, zone);
  return `${t.jahr}-${String(t.monat).padStart(2, "0")}-${String(t.tag).padStart(2, "0")}`;
}

/** Mitternacht eines Wanduhr-Tags (`Date.UTC(…)` der Wanduhr) in `zone` als Augenblick (ms) – zweimal mit dem Versatz,
 * weil er um Mitternacht an einem Umstellungstag ein anderer sein kann als einen Tag davor. */
function mitternacht(wand: number, zone: string): number {
  return wand - versatz(wand - versatz(wand, zone), zone);
}

/**
 * Der Beginn eines Lerntags („2026-11-20") als UTC-Zeitpunkt für den Server („2026-11-19T23:00:00Z") – etwa für ein
 * einmaliges Auffüllen im Shop, das wie jedes andere um Mitternacht deutscher Zeit kommt (B-730), nicht um 1 bzw. 2 Uhr.
 */
export function beginnDesLerntags(tag: string, zone: string = LERNTAG_ZONE): string {
  const m = NUR_TAG.exec(tag);
  if (!m) return tag;
  const ms = mitternacht(Date.UTC(Number(m[1]), Number(m[2]) - 1, Number(m[3])), zone);
  return new Date(ms).toISOString().replace(".000Z", "Z");
}

/**
 * Die Server-Uhr für Fristen (B-525): die Arcade zeigt ihre Fristen nach dem Lerntag – eine Anzeige nach der Zone des
 * Geräts widerspräche der Buchung, wenn das Gerät woanders steht. Der Augenblick wird hier geholt und nicht in der
 * Arcade gebaut (B-447).
 */
export const jetztGerade = (): Date => new Date();

/** Angefangene Stunden bis zum Ende des Lerntags (Mitternacht deutscher Zeit), mindestens 1. */
export function stundenBisServerTagesende(jetzt: Date, zone: string = LERNTAG_ZONE): number {
  const t = teile(jetzt, zone);
  const ende = mitternacht(Date.UTC(t.jahr, t.monat - 1, t.tag + 1), zone);
  return Math.max(1, Math.ceil((ende - jetzt.getTime()) / 3_600_000));
}

/** Der letzte Tag der Lernwoche (Montag bis Sonntag, deutsche Zeit). */
export const istServerSonntag = (jetzt: Date, zone: string = LERNTAG_ZONE): boolean =>
  teile(jetzt, zone).wochentag === "Sun";
