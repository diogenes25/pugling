import { istServerSonntag, stundenBisServerTagesende } from "../lib/datum";
import type { PositionStatus } from "../lib/types";

/*
 * Die Regeln des Lernpfads (B-525) als reine Funktionen: was ein Abschnitt ist, welcher Knoten der nächste ist,
 * welche Frist er zeigt und wohin der Tipp auf den Knoten führt. Sie stehen hier und nicht in der Komponente, weil jede davon eine
 * Entscheidung der Story trägt und einzeln prüfbar sein soll. Der Server bleibt die Wahrheit – hier wird nur
 * gelesen, was die Tagesübersicht meldet.
 */

/** Aufeinanderfolgende Positionen derselben Unit. Dieselbe Unit kann mehrfach vorkommen, wenn ein Plan sie mischt. */
export interface PfadAbschnitt {
  unitId: number;
  title: string;
  positions: PositionStatus[];
}

/**
 * Schneidet die Positionen in der Reihenfolge des Servers in Abschnitte. Bewusst kein Gruppieren über die ganze
 * Liste: ein Plan, der Units mischt, soll das als viele kurze Abschnitte zeigen, statt umsortiert zu werden.
 */
export function abschnitte(positions: PositionStatus[]): PfadAbschnitt[] {
  const out: PfadAbschnitt[] = [];
  for (const pos of positions) {
    const last = out[out.length - 1];
    if (last && last.unitId === pos.seriesUnitId) last.positions.push(pos);
    else out.push({ unitId: pos.seriesUnitId, title: pos.seriesUnitLabel, positions: [pos] });
  }
  return out;
}

/**
 * Eine Pflicht, die jeden Tag fällig ist: das Tagesziel und die Nacharbeit „täglich, bis erledigt“ (B-636) – dieselbe
 * Auswahl wie `PositionProgressService.IsDailyDuty` am Server.
 */
export const istTagespflicht = (cadence: PositionStatus["cadence"]) => cadence === "Daily" || cadence === "UntilDone";

/** Welche Ziele die Truhe freischalten: die Tagesziele, ohne sie alle (Wochen-)Ziele, ohne Pflicht gar keine (B-535). */
export type TruhenZiele = "tag" | "woche" | "keine";

/**
 * Die Art der Ziele, an denen Truhe und Bonus-Training hängen, für ihren Wortlaut (B-612). Gewählt wird sie am Server
 * (`rewardCadence`, aus `PositionProgressService.RewardObligations` über alle Pläne) – die Seite rechnet sie nicht nach.
 * Die Nacharbeit „bis erledigt“ ist eine Tagespflicht, wie am Server.
 */
export function truhenZiele(cadence: PositionStatus["cadence"]): TruhenZiele {
  return istTagespflicht(cadence) ? "tag" : cadence === "Weekly" ? "woche" : "keine";
}

/** Eine freie Position hat keine Pflicht – der Server meldet sie als erfüllt, auch ungespielt (B-467). */
export const hatPflicht = (pos: PositionStatus) => pos.cadence !== "None";
export const pflichtErfuellt = (pos: PositionStatus) => hatPflicht(pos) && pos.goalMet;
export const pflichtOffen = (pos: PositionStatus) => hatPflicht(pos) && !pos.goalMet;

/**
 * Der eine hervorgehobene Knoten: das erste offene Tagesziel, sonst das erste offene Wochenziel, sonst keiner.
 * Kein freiwilliger Ersatz, wenn die Pflicht erfüllt ist – die Grenze zwischen Pflicht und Kür bleibt sichtbar.
 */
export function naechstePositionId(positions: PositionStatus[]): number | null {
  const offen = positions.filter(pflichtOffen);
  return (offen.find((p) => istTagespflicht(p.cadence)) ?? offen.find((p) => p.cadence === "Weekly"))?.positionId ?? null;
}

/**
 * Anteil eingeprägter Inhalte (0…1) für den goldenen Ring; `null`, wenn die Position keinen Pool hat (kein Ring). B-643:
 * eingeprägt heißt einmal frei aus dem Gedächtnis richtig geschrieben – bei einer Übung ohne Lernkarte füllt der Server
 * die Zahl mit den eingeführten Wörtern, die Regel kennt nur er.
 */
export function kennenlernAnteil(pos: PositionStatus): number | null {
  if (pos.poolSize <= 0) return null;
  return Math.min(1, Math.max(0, (pos.imprintedCount ?? 0) / pos.poolSize));
}

/**
 * Stand der laufenden Pflicht (0…1) für den inneren Ring; `null` für eine freie Position – sie hat kein Ziel, das
 * sich füllen könnte. Voll ist der Ring genau dann, wenn der Haken steht: `goalMet` entscheidet, `goalProgress` füllt
 * nur den Weg dorthin (bei einem Test-Ziel bleibt er bis zum Bestehen leer – ein gescheiterter Test ist kein Teil davon).
 */
export function zielAnteil(pos: PositionStatus): number | null {
  if (!hatPflicht(pos)) return null;
  if (pos.goalMet) return 1;
  return Math.min(0.99, Math.max(0, pos.goalProgress));
}

export type Frist = { art: "stunden"; stunden: number } | { art: "sonntag" };

/**
 * Die Frist eines offenen Pflichtziels nach **Server-Uhr** (siehe `lib/datum.ts`): die Anzeige soll zur Buchung
 * passen, nicht zur Ortszeit. Am letzten Tag der Woche zeigt auch ein Wochenziel Stunden. `null` für Positionen
 * ohne offene Pflicht – und für eine Periode, die gar nichts schuldet (`owesDuty`, B-1022: Sonntag, Pausentag,
 * eine von einer Pause berührte Woche): dort bucht der Server keinen Malus, also droht die Anzeige auch keinen an.
 */
export function frist(pos: PositionStatus, jetzt: Date): Frist | null {
  if (!pflichtOffen(pos) || !pos.owesDuty) return null;
  if (pos.cadence === "Weekly" && !istServerSonntag(jetzt)) return { art: "sonntag" };
  return { art: "stunden", stunden: stundenBisServerTagesende(jetzt) };
}

export type Spielweg = "practice" | "test";

/** Üben steht offen, sobald es etwas zu üben gibt – dieselbe Regel wie vor dem Pfad (B-454, B-96). */
const kannUeben = (pos: PositionStatus) => pos.useLeitner || !pos.testable || pos.poolSize > 0;

/**
 * Im Karteikasten ist heute nichts zu tun. `dueCount` zählt schon mit, was neu dazukäme – der Server rechnet nie
 * Gezeigtes als fällig und kappt es an der Tagesdosis. „Noch nicht alles kennengelernt" ist darum kein Grund zu
 * üben: mit einer Dosis von 4 aus 40 Wörtern führte der Knoten nach der ersten Runde sonst in eine leere.
 */
const kastenLeer = (pos: PositionStatus) => pos.useLeitner && pos.dueCount === 0;

/**
 * Wohin der Tipp auf den Knoten führt: ins Üben (an dessen Ende der Test angeboten wird), und nur dann direkt in den
 * Test, wenn es heute einen gibt und nichts zu üben ist. Eine heute nicht prüfbare Position bleibt immer über das Üben
 * spielbar. Einen zweiten Weg am Start gibt es nicht mehr (B-686): der Test soll das Üben abschließen, nicht ersetzen.
 */
export function hauptweg(pos: PositionStatus): Spielweg {
  if (pos.testable && (!kannUeben(pos) || kastenLeer(pos))) return "test";
  return "practice";
}

export const spielPfad = (weg: Spielweg, positionId: number) => `/sohn/${weg}/${positionId}`;
