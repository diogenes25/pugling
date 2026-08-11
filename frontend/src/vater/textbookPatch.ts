import { subjectFormValue, subjectPatch } from "./subjectField";
import type { TextbookResponse, UpdateTextbookDto } from "../lib/types";

/**
 * Der PATCH-Rumpf beim Bearbeiten eines Lehrbuchs am Kind, als **reine Regel** (B-148) – Vorbild und
 * Begründung wie bei `seriesPatch.ts`: Die Zusicherung „ein unverändertes Feld wird gar nicht gesendet"
 * ist am Bildschirm unsichtbar, prüfbar ist sie nur am Rumpf.
 *
 * Warum überhaupt ein Vergleich: Die PATCH-Semantik des Projekts liest `null` als „nicht angegeben", nie
 * als „leeren". Wer leeren will, braucht den `clear…`-Schalter – und der darf nur mitgehen, wenn der
 * Nutzer den Wert *aktiv* weggenommen hat. Vorher entstand er aus dem Momentanwert, und weil das Formular
 * ein Freitext-Fach nicht darstellen konnte, hieß „das Feld zeigt nichts" fälschlich „der Nutzer hat
 * geleert" – jedes Speichern eines beliebigen anderen Feldes zerstörte den Fachnamen.
 */

/** Die sieben Felder des Formulars, alle als Strings – so wie ein `<input>`/`<select>` sie trägt. */
export type TextbookFormValues = {
  title: string;
  subjectId: string;
  grade: string;
  publisher: string;
  seriesId: string;
  currentUnitId: string;
  currentChapter: string;
};

/** Der leere Bogen: der Zustand des Anlege-Formulars, das keinen Ladezustand hat. */
export const EMPTY_TEXTBOOK_FORM: TextbookFormValues = {
  title: "", subjectId: "", grade: "", publisher: "", seriesId: "", currentUnitId: "", currentChapter: "",
};

/** Der Ladezustand: dieselbe Form, gefüllt aus der Antwort des Servers. */
export function textbookFormValues(book: TextbookResponse): TextbookFormValues {
  return {
    title: book.title,
    subjectId: subjectFormValue(book),
    grade: book.grade == null ? "" : String(book.grade),
    publisher: book.publisher ?? "",
    seriesId: book.seriesId == null ? "" : String(book.seriesId),
    currentUnitId: book.currentUnitId == null ? "" : String(book.currentUnitId),
    currentChapter: book.currentChapter ?? "",
  };
}

/**
 * Nur das Geänderte. `null` als Rückgabe heißt: nichts zu tun.
 *
 * Die Textfelder sind über den leeren String leerbar (der Server schreibt den getrimmten Wert), die drei
 * Referenzen und die Klassenstufe brauchen ihren Schalter, weil `null` dort „unverändert" bedeutet.
 */
export function textbookPatch(
  loaded: TextbookFormValues,
  form: TextbookFormValues,
): UpdateTextbookDto | null {
  const dto: UpdateTextbookDto = {};

  if (form.title.trim() !== loaded.title) dto.title = form.title.trim();
  if (form.publisher.trim() !== loaded.publisher) dto.publisher = form.publisher.trim();
  if (form.currentChapter.trim() !== loaded.currentChapter) dto.currentChapter = form.currentChapter.trim();

  // Das Fach trägt seine eigene Regel, geteilt mit Reihe und Fachlehrer-Profil.
  Object.assign(dto, subjectPatch(loaded.subjectId, form.subjectId));

  if (form.grade !== loaded.grade) {
    if (form.grade.trim() === "") dto.clearGrade = true;
    else dto.grade = Number(form.grade);
  }
  if (form.seriesId !== loaded.seriesId) {
    if (form.seriesId === "") dto.clearSeries = true;
    else dto.seriesId = Number(form.seriesId);
  }
  // Die Unit nur, wenn die Reihe bleibt: `clearSeries` räumt sie serverseitig ohnehin mit, und beim
  // Reihenwechsel verwirft der Server die alte Unit von sich aus (sie gehört zur alten Reihe).
  if (form.currentUnitId !== loaded.currentUnitId && dto.clearSeries !== true) {
    if (form.currentUnitId === "") dto.clearUnit = true;
    else dto.currentUnitId = Number(form.currentUnitId);
  }

  return Object.keys(dto).length === 0 ? null : dto;
}
