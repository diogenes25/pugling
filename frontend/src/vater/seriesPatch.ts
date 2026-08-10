import type { SchoolType, TextbookSeriesResponse, UpdateTextbookSeriesDto } from "../lib/types";

/**
 * Der PATCH-Rumpf beim Bearbeiten einer Lehrwerk-Reihe, als **reine Regel** (B-123).
 *
 * Sie liegt hier statt im Formular, weil die eigentliche Zusicherung sonst unprüfbar wäre: „ein
 * unverändertes Feld wird gar nicht gesendet". Das ist kein sichtbares Verhalten – man sieht es nur am
 * Rumpf – und im Bildschirm wäre es nur über einen nachgebauten Screen mit gefälschtem `fetch` zu
 * prüfen, was `frontend/CLAUDE.md` ausschließt. Vorbild: `seriesDerivation.ts` (B-126).
 *
 * Warum überhaupt ein Vergleich und nicht „alles senden": Die PATCH-Semantik des Projekts liest `null`
 * als „nicht angegeben", nie als „leeren". Wer leeren will, braucht den `clear…`-Schalter – und der darf
 * nur mitgehen, wenn der Nutzer den Wert *aktiv* weggenommen hat. Der Ladezustand ist der einzige
 * Bezugspunkt, an dem sich das entscheiden lässt.
 *
 * **Was diese Funktion und ihr Test NICHT sind: ein Vertragstest.** Sie pinnen den Rumpf, den das
 * Frontend *baut*, nicht den, den der Server *annimmt*. Als der Schalter serverseitig von
 * `clearSubjectId` auf `clearSubject` umbenannt wurde, blieben alle Fälle grün – gegriffen haben der
 * generierte Typ (`tsc -b`) und die E2E. Wer hier einen Feldnamen ändert, muss dort nachsehen.
 */

/** Die sieben Felder des Formulars, alle als Strings – so wie ein `<input>`/`<select>` sie trägt. */
export type SeriesFormValues = {
  name: string;
  publisherId: string;
  subjectId: string;
  schoolTypes: SchoolType;
  sourceLanguage: string;
  targetLanguage: string;
  notes: string;
};

/** Der Ladezustand: dieselbe Form, gefüllt aus der Antwort des Servers. */
export function seriesFormValues(series: TextbookSeriesResponse): SeriesFormValues {
  return {
    name: series.name,
    publisherId: series.publisherId == null ? "" : String(series.publisherId),
    subjectId: series.subjectId == null ? "" : String(series.subjectId),
    schoolTypes: series.schoolTypes,
    sourceLanguage: series.sourceLanguage ?? "",
    targetLanguage: series.targetLanguage ?? "",
    notes: series.notes ?? "",
  };
}

/**
 * Nur das Geänderte. `null` als Rückgabe heißt: nichts zu tun.
 *
 * Das Fach hängt an **zwei** Feldern: `subjectId` zeigt in den Katalog, `subjectName` trägt die Reihe
 * auch ohne Katalog-Fach (und ist eine gespeicherte Spalte, kein Join – anders als `publisherName`).
 * Beim **Leeren** räumt der Server beide über `clearSubject` ab; beim **Wechsel** genügt die Id, weil
 * der Server den Namen seit B-142 selbst daraus ableitet. Diese Stelle schickte den Namen früher mit
 * und war damit die einzige Kompensation einer Regel, die der Server nicht durchsetzte – jetzt tut er
 * es, und der mitgeschickte Name würde ohnehin ignoriert.
 */
export function seriesPatch(
  loaded: SeriesFormValues,
  form: SeriesFormValues,
): UpdateTextbookSeriesDto | null {
  const dto: UpdateTextbookSeriesDto = {};

  if (form.name.trim() !== loaded.name) dto.name = form.name.trim();
  // Die Schulart ist der dritte Mechanismus neben Schalter und leerem String: „für alle" reist als
  // Sentinel `"None"` – das `[Flags]`-Enum hat den Wert selbst (`None = 0`), es gibt also nichts zu
  // leeren. Beim **Anlegen** schickt `NewSeries` dafür `null`; beides kommt an, aber es sind zwei Wege
  // für dieselbe Aussage, und hier gilt der Sentinel.
  if (form.schoolTypes !== loaded.schoolTypes) dto.schoolTypes = form.schoolTypes;
  // Die Textfelder sind über den leeren String leerbar: der Server macht `""` zu `null`.
  if (form.sourceLanguage !== loaded.sourceLanguage) dto.sourceLanguage = form.sourceLanguage;
  if (form.targetLanguage !== loaded.targetLanguage) dto.targetLanguage = form.targetLanguage;
  if (form.notes.trim() !== loaded.notes) dto.notes = form.notes.trim();

  // Die beiden Referenzen brauchen ihren Schalter, weil `null` „unverändert" bedeutet.
  if (form.publisherId !== loaded.publisherId) {
    if (form.publisherId === "") dto.clearPublisherId = true;
    else dto.publisherId = Number(form.publisherId);
  }
  if (form.subjectId !== loaded.subjectId) {
    if (form.subjectId === "") {
      // Nur der Schalter: der Controller räumt Id UND Namen: TextbookSeriesController, dieselbe Zeile
      // wie in CreatorProfilesController und TextbooksController.
      dto.clearSubject = true;
    } else {
      // Nur die Id: den Anzeigenamen holt der Server aus dem Katalog (B-142). Ihn hier mitzuschicken
      // wäre totes Feld — und schlimmer, es sähe aus wie eine Regel, die es nicht mehr gibt.
      dto.subjectId = Number(form.subjectId);
    }
  }

  return Object.keys(dto).length === 0 ? null : dto;
}
