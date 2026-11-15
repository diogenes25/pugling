import type { ExerciseSearchParams, SchoolType } from "../lib/types";

/**
 * Die Filter der Übungssuche als **reine Regel** (B-18) — anfangs nur für den Assistenten-Schritt
 * „Übungen", seit B-167 auch für den Planbau (`PlanPositions.tsx`), der dieselbe Frage stellt.
 *
 * Sie liegt hier statt im Bildschirm, weil sie an **zwei** Stellen gebraucht wird — bei jeder Eingabe für
 * die Trefferliste und noch einmal beim „Alle wählen"-Nachschlag mit höherem `take`. Zwei Kopien derselben
 * Abbildung wären genau die Konstellation, in der eine von beiden einen später ergänzten Filter vergisst:
 * Die Liste zeigte dann etwas anderes an, als der Knopf übernimmt, und niemand sähe es.
 *
 * Prüfbar ist sie nur hier: Welche Query der Server bekommt, sieht man am Bildschirm nicht.
 */

/** Der Formularzustand des Schritts, so wie die Eingabefelder ihn tragen. */
export type WizardFilter = {
  subjectId: number;
  grade?: number;
  schoolType?: SchoolType;
  search: string;
  categoryId: number | "";
  type: string;
  source: string;
  /** Grammatik-Thema (B-155) – die achte Facette, einwertig; hängt wie die Art am Fach. */
  grammarTopicId: number | "";
  /**
   * „Beste Bewertung zuerst" (B-408) – wie im Planbau (B-270). Eine Reihenfolge, **kein** Suchkriterium: sie reist in
   * die Abfrage, aber nicht in `wizardFilterKey`.
   */
  besteZuerst?: boolean;
};

/**
 * Nur gesetzte Filter reisen mit. Leer heißt **„alle"**, nicht „leerer Wert": Ein `categoryId=` oder
 * `type=` in der Query wäre für den Server ein Filter auf nichts, und die Trefferliste bliebe ohne
 * sichtbaren Grund leer.
 */
export function wizardSearchParams(f: WizardFilter, take?: number): ExerciseSearchParams {
  return {
    subjectId: f.subjectId,
    grade: f.grade,
    schoolType: f.schoolType,
    search: f.search.trim() || undefined,
    categoryId: f.categoryId === "" ? undefined : f.categoryId,
    type: f.type || undefined,
    source: f.source.trim() || undefined,
    grammarTopicId: f.grammarTopicId === "" ? undefined : f.grammarTopicId,
    // Ohne den Wunsch reist keines von beiden: dann gilt die Katalog-Reihenfolge des Servers.
    sort: f.besteZuerst ? "rating" : undefined,
    dir: f.besteZuerst ? "desc" : undefined,
    take,
  };
}

/**
 * Ein Vergleichsschlüssel über die **Suchkriterien** (B-161).
 *
 * Er existiert, weil die Auswahl beim Wechsel der Kriterien fallen muss und ein Objektvergleich per Referenz
 * bei *jedem* Rendern anders ausfiele. Bewusst über `wizardSearchParams` gebildet und nicht über die
 * Rohfelder: dann zählt genau das als Änderung, was der Server auch anders beantwortet — ein Leerschlag am
 * Ende der Inhaltssuche (`"abc "` → `"abc"`) verwirft die Auswahl also nicht.
 *
 * `take` gehört ausdrücklich **nicht** dazu: eine größere Seite ist dieselbe Suche. Ebenso wenig `sort`/`dir`
 * (B-408): umsortiert ist die Treffermenge dieselbe, und „Alle wählen" kann bis 500 gewählt haben – fiele die
 * Auswahl beim Umsortieren, verlöre der Vater stumm die meisten davon.
 */
export function wizardFilterKey(f: WizardFilter): string {
  const { take: _take, sort: _sort, dir: _dir, ...kriterien } = wizardSearchParams(f);
  return JSON.stringify(kriterien);
}

/**
 * Wie viele der gewählten Übungen stehen **nicht** in der gezeigten Liste (B-161).
 *
 * „Alle wählen" darf bis zu 500 Treffer wählen, gerendert wird aber nur die geladene Seite — die Differenz
 * ist für den Vater sonst unsichtbar, und `toggle` gibt es nur an einer gerenderten Zeile. Die Zahl macht
 * aus „(500 gewählt)" neben „5 passende Übungen" einen lesbaren Zustand statt eines Widerspruchs.
 */
export function unsichtbareAuswahl(selected: readonly number[], geladeneIds: readonly number[]): number {
  const geladen = new Set(geladeneIds);
  return selected.filter((id) => !geladen.has(id)).length;
}

/**
 * Was mit der Auswahl geschieht, wenn sich die Suchkriterien geändert haben (B-161).
 *
 * Die Regel liegt hier und nicht als `if` im Effekt, weil sie den **Defekt** dieser Story trägt: dass eine
 * Auswahl den Filterwechsel überlebte. Als Funktion ist sie rot zu bekommen; als Zeile im Bildschirm hing
 * sie nur am Rollengang.
 *
 * `null` heißt „nichts tun" — der Schlüssel ist derselbe. Ein Hinweis entsteht **nur**, wenn wirklich etwas
 * wegfiel: sonst begrüßte der Assistent den Vater mit „Auswahl zurückgesetzt", bevor er etwas getan hat
 * (und stumm zu leeren wäre die andere Falle, B-116).
 */
export function auswahlNachFilterwechsel(vorher: readonly number[], keyAlt: string, keyNeu: string):
{ selected: number[]; hinweis: string | null } | null {
  if (keyAlt === keyNeu) return null;
  return {
    selected: [],
    // Gebeugt, wie an den vergleichbaren Stellen des Hauses („Plan/Pläne", „Kind/Kindern"): „1 Übungen"
    // liest sich wie ein Platzhalter, den jemand vergessen hat.
    hinweis: vorher.length > 0
      ? `Auswahl zurückgesetzt (${vorher.length} ${vorher.length === 1 ? "Übung" : "Übungen"}),`
        + " weil sich die Suche geändert hat."
      : null,
  };
}

/**
 * Der Grund an einer gesperrten Trefferzeile (B-140, Entscheidung 3). `canAssign = false` hat heute genau einen Grund – die
 * Übung ist nicht öffentlich und der Anfragende hat keinen Grant –, darum ein fester Satz statt eines Codes im Vertrag.
 * Planbau, Assistent und Klassenarbeit teilen ihn, sonst wären es mehrere Formulierungen derselben Sperre.
 */
export const NICHT_ZUWEISBAR_GRUND =
  "Nicht zuweisbar – der Ersteller hat sie nicht für alle freigegeben; zuweisen darf nur, wem er sie freigibt.";

/**
 * „Alle wählen" im Assistenten übernimmt nur, was der Vater auch zuweisen darf (B-140, Entscheidung 5) – sonst scheiterte der
 * Abschluss am Ende an `exercise_not_executable`, mit Hunderten Positionen. Die Regel rechnet der Server (`canAssign`), hier
 * wird nur gefiltert und gezählt. Ein Hinweis entsteht nur, wenn wirklich etwas übersprungen wurde.
 */
export function nurZuweisbare(items: readonly { id: number; canAssign: boolean }[]):
{ ids: number[]; hinweis: string | null } {
  const ids = items.filter((e) => e.canAssign).map((e) => e.id);
  const weg = items.length - ids.length;
  return {
    ids,
    hinweis: weg > 0
      ? `${ids.length} ${ids.length === 1 ? "Übung" : "Übungen"} gewählt, ${weg} nicht zuweisbare übersprungen.`
      : null,
  };
}

/**
 * Was mit einer Auswahl geschieht, nachdem die Trefferliste **umsortiert** neu geladen wurde (B-408, Entscheidung 3).
 *
 * Für den Planbau (Einzelauswahl, Umbau in B-540): die Suche ist dieselbe, nur die Seite eine andere. Was in der neu
 * geladenen Seite steht, bleibt gewählt; was aus ihr gerutscht ist, fällt – die Auswahl darf nie auf eine Übung zeigen,
 * die der Vater nicht sieht (B-167). Ein Hinweis entsteht nur, wenn wirklich etwas wegfiel (wie `auswahlNachFilterwechsel`).
 * Der Assistent braucht die Regel nicht: dort zählt „davon N unten nicht sichtbar" die Unsichtbaren, statt sie zu verwerfen.
 */
export function auswahlNachUmsortieren(vorher: readonly number[], geladeneIds: readonly number[]):
{ selected: number[]; hinweis: string | null } {
  const geladen = new Set(geladeneIds);
  const selected = vorher.filter((id) => geladen.has(id));
  const weg = vorher.length - selected.length;
  return {
    selected,
    hinweis: weg > 0
      ? `Auswahl zurückgesetzt (${weg} ${weg === 1 ? "Übung" : "Übungen"}), weil sie nach dem Neuladen nicht mehr`
        + ` in der Liste ${weg === 1 ? "steht" : "stehen"}.`
      : null,
  };
}
