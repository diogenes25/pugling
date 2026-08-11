import { subjectFormValue, subjectPatch } from "./subjectField";
import type { CreatorProfileResponse, SchoolType, UpdateCreatorProfileDto } from "../lib/types";

/**
 * Der PATCH-Rumpf beim Bearbeiten eines Fachlehrer-Profils, als **reine Regel** (B-148) – dieselbe
 * Begründung wie bei `seriesPatch.ts` und `textbookPatch.ts`: „ein unverändertes Feld wird gar nicht
 * gesendet" ist am Bildschirm unsichtbar und nur am Rumpf prüfbar.
 *
 * Das Profil hat gegenüber dem Lehrbuch zwei Eigenheiten, und beide sind hier abgebildet: die Schulart
 * reist als Sentinel `"None"` statt über einen Schalter (das `[Flags]`-Enum hat den Wert selbst), und die
 * Sprachen kennen serverseitig gar keinen leeren Zustand – ein geleertes Feld käme nie an.
 */

/** Die Felder des Formulars, alle als Strings bzw. Listen – so wie die Eingaben sie tragen. */
export type ProfileFormValues = {
  name: string;
  subjectId: string;
  schoolTypes: SchoolType;
  gradeMin: string;
  gradeMax: string;
  seriesId: string;
  sourceLang: string;
  targetLang: string;
  persona: string;
  didactics: string;
  active: boolean;
  defaultTypes: string[];
};

/** Der Ladezustand: dieselbe Form, gefüllt aus der Antwort des Servers. */
export function profileFormValues(
  profile: CreatorProfileResponse,
  schoolTypes: SchoolType,
  sourceFallback: string,
  targetFallback: string,
): ProfileFormValues {
  return {
    name: profile.name,
    subjectId: subjectFormValue(profile),
    // Die Schulart geht durch dieselbe Normalisierung wie das Formular: eine gespeicherte KOMBINATION
    // ("Realschule, Gymnasium") ist im Pulldown nicht darstellbar und wird dort zu "None". Wer sie hier
    // roh übernähme, sähe beim ersten Speichern einen Unterschied, den der Nutzer nie gemacht hat.
    schoolTypes,
    gradeMin: profile.gradeMin == null ? "" : String(profile.gradeMin),
    gradeMax: profile.gradeMax == null ? "" : String(profile.gradeMax),
    seriesId: profile.seriesId == null ? "" : String(profile.seriesId),
    sourceLang: profile.sourceLang ?? sourceFallback,
    targetLang: profile.targetLang ?? targetFallback,
    persona: profile.persona ?? "",
    didactics: profile.didactics ?? "",
    active: profile.active,
    defaultTypes: profile.defaultTypes ?? [],
  };
}

/** Nur das Geänderte. `null` als Rückgabe heißt: nichts zu tun. */
export function profilePatch(
  loaded: ProfileFormValues,
  form: ProfileFormValues,
): UpdateCreatorProfileDto | null {
  const dto: UpdateCreatorProfileDto = {};

  if (form.name.trim() !== loaded.name) dto.name = form.name.trim();
  if (form.schoolTypes !== loaded.schoolTypes) dto.schoolTypes = form.schoolTypes;
  // Die Sprachen kennen keinen leeren Zustand (`seriesDerivation.ts`, FIELD_FALLBACKS): ein geleertes
  // Feld schickte `""`, der Server machte daraus nichts, und die Oberfläche meldete trotzdem Erfolg.
  // Darum nur ein nicht-leerer, geänderter Wert.
  if (form.sourceLang !== loaded.sourceLang && form.sourceLang.trim() !== "") dto.sourceLang = form.sourceLang;
  if (form.targetLang !== loaded.targetLang && form.targetLang.trim() !== "") dto.targetLang = form.targetLang;
  if (form.persona.trim() !== loaded.persona) dto.persona = form.persona.trim();
  if (form.didactics.trim() !== loaded.didactics) dto.didactics = form.didactics.trim();
  if (form.active !== loaded.active) dto.active = form.active;

  // Das Fach trägt seine eigene Regel, geteilt mit Reihe und Lehrbuch.
  Object.assign(dto, subjectPatch(loaded.subjectId, form.subjectId));

  if (form.seriesId !== loaded.seriesId) {
    if (form.seriesId === "") dto.clearSeries = true;
    else dto.seriesId = Number(form.seriesId);
  }
  if (form.gradeMin !== loaded.gradeMin) {
    if (form.gradeMin.trim() === "") dto.clearGradeMin = true;
    else dto.gradeMin = Number(form.gradeMin);
  }
  if (form.gradeMax !== loaded.gradeMax) {
    if (form.gradeMax.trim() === "") dto.clearGradeMax = true;
    else dto.gradeMax = Number(form.gradeMax);
  }

  // Die Typenliste wird als Ganzes ersetzt, nicht in place verändert – Reihenfolge zählt dabei mit,
  // weil sie im Formular sichtbar ist.
  if (form.defaultTypes.length !== loaded.defaultTypes.length
    || form.defaultTypes.some((t, i) => t !== loaded.defaultTypes[i])) {
    dto.defaultTypes = form.defaultTypes;
  }

  return Object.keys(dto).length === 0 ? null : dto;
}
