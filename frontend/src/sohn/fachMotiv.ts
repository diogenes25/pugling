/*
 * Das Motiv hinter dem Kopf eines Fach-Reiters: Sprachen tragen ihre Flagge (flach, nach rechts in die Fachfarbe
 * ausgeblendet), die übrigen Fächer ein Themenmuster. Rein dekorativ – der Fachname bleibt die Information.
 * Erkannt wird am Fachnamen, wie der Server ihn meldet; ein unbekanntes Fach bekommt kein eigenes Motiv und behält die
 * Reihenfarbe seines Platzes, damit zwei unbekannte Fächer trotzdem verschieden aussehen.
 */

export interface FachMotiv {
  /** Stabiler Schlüssel (für Tests und `data-motiv`). */
  key: string;
  art: "flagge" | "muster";
  /** Hauptfarbe und dunklere Verlaufsfarbe des Reiter-Kopfs. */
  farbe: readonly [string, string];
  /** Das Bild als SVG-Quelltext; die Flagge wird auf den Kopf gestreckt, das Muster gekachelt. */
  svg: string;
  /** Kantenlänge der Musterkachel in px (Standard 64) – Muster mit mehreren Figuren brauchen mehr Platz. */
  kachel?: number;
}

const flagge = (viewBox: string, inhalt: string) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}" preserveAspectRatio="none">${inhalt}</svg>`;

/** Quadratische Musterkachel, weiße Linien – die Deckkraft regelt das CSS. */
const kachel = (inhalt: string, groesse = 64) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${groesse} ${groesse}" fill="none" stroke="#fff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${inhalt}</svg>`;

const streifenV = (farben: string[]) =>
  flagge(`0 0 ${farben.length} 2`, farben.map((f, i) => `<rect x="${i}" width="1" height="2" fill="${f}"/>`).join(""));
const streifenH = (farben: string[]) =>
  flagge(`0 0 5 ${farben.length}`, farben.map((f, i) => `<rect y="${i}" width="5" height="1" fill="${f}"/>`).join(""));

const UNION_JACK = flagge("0 0 60 30",
  `<clipPath id="t"><path d="M30,15h30v15zv15h-30zh-30v-15zv-15h30z"/></clipPath>`
  + `<rect width="60" height="30" fill="#012169"/>`
  + `<path d="M0,0L60,30M60,0L0,30" stroke="#fff" stroke-width="6"/>`
  + `<path d="M0,0L60,30M60,0L0,30" clip-path="url(#t)" stroke="#C8102E" stroke-width="4"/>`
  + `<path d="M30,0v30M0,15h60" stroke="#fff" stroke-width="10"/>`
  + `<path d="M30,0v30M0,15h60" stroke="#C8102E" stroke-width="6"/>`);

const GRIECHENLAND = flagge("0 0 27 18",
  Array.from({ length: 9 }, (_, i) => `<rect y="${i * 2}" width="27" height="2" fill="${i % 2 ? "#fff" : "#0D5EAF"}"/>`).join("")
  + `<rect width="10" height="10" fill="#0D5EAF"/><path d="M5,0v10M0,5h10" stroke="#fff" stroke-width="2"/>`);

/** Das römische Feldzeichen (Vexillum): Purpurrot mit goldenem SPQR – die „Flagge“ einer Sprache ohne Staat. */
const VEXILLUM = flagge("0 0 60 30",
  `<rect width="60" height="30" fill="#8e1622"/>`
  + `<rect x="2.5" y="2.5" width="55" height="25" fill="none" stroke="#f2c14e" stroke-width="1.5"/>`
  + `<text x="30" y="20.5" text-anchor="middle" font-family="Georgia,'Times New Roman',serif" font-weight="700" font-size="13" fill="#f2c14e" letter-spacing="1">SPQR</text>`);

const MOTIVE: { muster: RegExp; motiv: FachMotiv }[] = [
  { muster: /\b(englisch|english)/, motiv: { key: "en", art: "flagge", farbe: ["#ff4d6d", "#c92a4a"], svg: UNION_JACK } },
  { muster: /\b(franzosisch|french|francais)/, motiv: { key: "fr", art: "flagge", farbe: ["#2f6bff", "#1d47c4"], svg: streifenV(["#002654", "#fff", "#ED2939"]) } },
  { muster: /\b(latein|latin)/, motiv: { key: "la", art: "flagge", farbe: ["#9b3bd9", "#6a1fa3"], svg: VEXILLUM } },
  { muster: /\b(spanisch|spanish|espanol)/, motiv: { key: "es", art: "flagge", farbe: ["#ff9e2c", "#c46a10"],
    svg: flagge("0 0 3 4", `<rect width="3" height="4" fill="#AA151B"/><rect y="1" width="3" height="2" fill="#F1BF00"/>`) } },
  { muster: /\b(italienisch|italian|italiano)/, motiv: { key: "it", art: "flagge", farbe: ["#23b84a", "#168234"], svg: streifenV(["#009246", "#fff", "#CE2B37"]) } },
  { muster: /\b(deutsch|german)/, motiv: { key: "de", art: "flagge", farbe: ["#e0a800", "#9c7400"], svg: streifenH(["#000", "#DD0000", "#FFCE00"]) } },
  { muster: /\b(niederlandisch|hollandisch|dutch)/, motiv: { key: "nl", art: "flagge", farbe: ["#ff7a1a", "#c45208"], svg: streifenH(["#AE1C28", "#fff", "#21468B"]) } },
  { muster: /\b(griechisch|greek|altgriechisch)/, motiv: { key: "gr", art: "flagge", farbe: ["#3a8dde", "#1f5fa8"], svg: GRIECHENLAND } },
  { muster: /\b(japanisch|japanese)/, motiv: { key: "jp", art: "flagge", farbe: ["#ff5c8a", "#c22f5c"],
    svg: flagge("0 0 3 2", `<rect width="3" height="2" fill="#fff"/><circle cx="1.5" cy="1" r=".6" fill="#BC002D"/>`) } },

  // Mathe: Zahlen und geometrische Figuren – Dreieck, Kreis mit Radius, Quadrat mit rechtem Winkel.
  { muster: /\b(mathe|math)/, motiv: { key: "mathe", art: "muster", farbe: ["#14b8c4", "#0b7f88"], kachel: 96, svg: kachel(
    `<path d="M8,32L26,6L40,32Z"/>`
    + `<circle cx="72" cy="20" r="12"/><path d="M72,20h12"/><circle cx="72" cy="20" r="1.5" fill="#fff"/>`
    + `<rect x="54" y="56" width="26" height="26"/><path d="M54,74h8v8"/>`
    + `<g fill="#fff" stroke="none" font-family="Verdana,sans-serif" font-weight="700">`
    + `<text x="10" y="64" font-size="20">7</text><text x="28" y="56" font-size="13">3</text>`
    + `<text x="6" y="90" font-size="13">2+5</text><text x="84" y="54" font-size="15">π</text></g>`, 96) } },
  { muster: /\b(physik|physics)/, motiv: { key: "physik", art: "muster", farbe: ["#5865f2", "#3640b8"], svg: kachel(
    `<ellipse cx="32" cy="32" rx="22" ry="8"/><ellipse cx="32" cy="32" rx="22" ry="8" transform="rotate(60 32 32)"/>`
    + `<ellipse cx="32" cy="32" rx="22" ry="8" transform="rotate(120 32 32)"/><circle cx="32" cy="32" r="3" fill="#fff"/>`) } },
  { muster: /\b(chemie|chemistry)/, motiv: { key: "chemie", art: "muster", farbe: ["#23b84a", "#168234"], svg: kachel(
    `<path d="M32,12l17,10v20l-17,10l-17-10v-20z"/><circle cx="32" cy="32" r="9"/><path d="M49,22l13-8M15,42l-13,8"/>`) } },
  // Bio: Knochen, Herz, Fisch und Pfote – Körper und Tiere.
  { muster: /\b(bio)/, motiv: { key: "bio", art: "muster", farbe: ["#6cc24a", "#3f8a26"], kachel: 96, svg: kachel(
    `<path d="M15,15L35,33" stroke-width="5"/>`
    + `<g fill="#fff" stroke="none"><circle cx="10" cy="16" r="4"/><circle cx="16" cy="10" r="4"/>`
    + `<circle cx="34" cy="38" r="4"/><circle cx="40" cy="32" r="4"/></g>`
    + `<path d="M70,30c-3-2-14-9-14-17c0-5 4-8 8-8c3,0 5,2 6,4c1-2 3-4 6-4c4,0 8,3 8,8c0,8-11,15-14,17z"/>`
    + `<path d="M10,66c8-10 22-10 30,0c-8,10-22,10-30,0zM40,66l9-8v16z"/><circle cx="18" cy="64" r="1.6" fill="#fff" stroke="none"/>`
    + `<g fill="#fff" stroke="none"><ellipse cx="72" cy="80" rx="8" ry="6.5"/><circle cx="61" cy="70" r="3.2"/>`
    + `<circle cx="68" cy="64" r="3.2"/><circle cx="77" cy="64" r="3.2"/><circle cx="84" cy="70" r="3.2"/></g>`, 96) } },
  { muster: /\b(geschichte|history)/, motiv: { key: "geschichte", art: "muster", farbe: ["#c68a3e", "#8a5a1f"], svg: kachel(
    `<path d="M12,14h24M14,18h20M16,18v30M22,18v30M28,18v30M32,18v30M12,52h24"/>`
    + `<path d="M44,10h12M44,34h12M46,10c0,8 8,8 8,12s-8,4-8,12M54,10c0,8-8,8-8,12s8,4 8,12"/>`) } },
  { muster: /\b(erdkunde|geograph|geo\b)/, motiv: { key: "erdkunde", art: "muster", farbe: ["#1fa88c", "#11705d"], svg: kachel(
    `<circle cx="32" cy="32" r="22"/><ellipse cx="32" cy="32" rx="10" ry="22"/><path d="M32,10v44M11,24h42M11,40h42"/>`) } },
  { muster: /\b(musik|music)/, motiv: { key: "musik", art: "muster", farbe: ["#e04fbf", "#9e2a84"], svg: kachel(
    `<g fill="#fff" stroke="none" font-size="22"><text x="6" y="26">♪</text><text x="34" y="52">♫</text></g>`) } },
  { muster: /\b(kunst|art)\b/, motiv: { key: "kunst", art: "muster", farbe: ["#ff6b3d", "#c4421c"], svg: kachel(
    `<path d="M32,10c14,0 24,9 24,20c0,8-8,8-12,8c-5,0-6,6-2,10c3,3 0,6-8,6c-14,0-24-10-24-22s10-22 22-22z"/>`
    + `<g stroke="none"><circle cx="22" cy="24" r="4" fill="#ffd23f"/><circle cx="34" cy="18" r="4" fill="#3ec5ff"/>`
    + `<circle cx="44" cy="26" r="4" fill="#7bff6b"/><circle cx="20" cy="38" r="4" fill="#ff5ca8"/></g>`) } },
  { muster: /\b(informatik|computer|programm)/, motiv: { key: "informatik", art: "muster", farbe: ["#19a974", "#0e6e4b"], svg: kachel(
    `<g fill="#fff" stroke="none" font-family="Consolas,monospace" font-size="11" font-weight="700"><text x="4" y="14">01</text><text x="36" y="40">10</text></g>`
    + `<path d="M22,10h10v20h16M8,48h20v8M48,52h10"/><circle cx="48" cy="30" r="2.5" fill="#fff"/><circle cx="28" cy="56" r="2.5" fill="#fff"/>`) } },
  { muster: /\b(religion|ethik|philosoph)/, motiv: { key: "religion", art: "muster", farbe: ["#f2b33d", "#b07a12"], svg: kachel(
    `<circle cx="32" cy="32" r="7"/><path d="M32,12v8M32,44v8M12,32h8M44,32h8M18,18l5,5M41,41l5,5M46,18l-5,5M23,41l-5,5"/>`) } },
  { muster: /\b(sport)/, motiv: { key: "sport", art: "muster", farbe: ["#ff5a36", "#c23a1a"], svg: kachel(
    `<circle cx="32" cy="32" r="18"/><path d="M14,32c8,-6 28,-6 36,0M32,14c-6,8-6,28 0,36"/>`) } },
];

/** Für ein Fach ohne eigenes Motiv: Funkelsterne – das Muster sagt „Lernen“, die Farbe kommt vom Platz in der Reihe. */
export const STERNE: string = kachel(
  `<path d="M16,8v12M10,14h12M46,34v16M38,42h16M24,46v6M21,49h6" /><circle cx="52" cy="12" r="1.5" fill="#fff"/>`);

/** Großbuchstaben, Umlaute und Akzente spielen keine Rolle: „Französisch“, „franzoesisch“ und „French“ treffen gleich. */
function normalisiere(name: string): string {
  return name.toLowerCase().replace(/ä/g, "a").replace(/ö/g, "o").replace(/ü/g, "u").replace(/oe/g, "o").replace(/ae/g, "a")
    .normalize("NFD").replace(/[̀-ͯ]/g, "");
}

/** Das Motiv für einen Fachnamen, oder `null`, wenn das Fach keins hat. */
export function fachMotiv(fachName: string): FachMotiv | null {
  const n = normalisiere(fachName);
  return MOTIVE.find((m) => m.muster.test(n))?.motiv ?? null;
}

/** Ein SVG-Quelltext als CSS-`url(…)`. */
export const svgUrl = (svg: string) => `url("data:image/svg+xml,${encodeURIComponent(svg)}")`;
