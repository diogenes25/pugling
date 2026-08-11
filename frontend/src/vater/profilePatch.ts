import { FIELD_FALLBACKS } from "./seriesDerivation";
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
export function profileFormValues(profile: CreatorProfileResponse): ProfileFormValues {
  return {
    name: profile.name,
    subjectId: subjectFormValue(profile),
    // Die Schulart **roh**, wie bei der Reihe (`seriesFormValues`). Eine gespeicherte Kombination
    // („Realschule, Gymnasium") ist im Pulldown nicht auszuwählen, aber sie muss der Bezugspunkt sein:
    // Normalisierte man sie hier auf `None`, stünden beide Seiten des Vergleichs auf `None` – die
    // Kombination überlebte zwar, aber „– für alle –" wäre nicht mehr herstellbar. Das ist derselbe
    // Tausch (Zerstörung gegen Unerreichbarkeit), den Entscheidung 1 fürs Fach verworfen hat. Sichtbar
    // wird der Zustand über die gesperrte Option im `<select>`.
    schoolTypes: profile.schoolTypes as SchoolType,
    gradeMin: profile.gradeMin == null ? "" : String(profile.gradeMin),
    gradeMax: profile.gradeMax == null ? "" : String(profile.gradeMax),
    seriesId: profile.seriesId == null ? "" : String(profile.seriesId),
    // Die Vorgaben liegen bei der Ableitungsregel, die sie begründet – nicht als Parameter, den ein
    // Aufrufer auch anders belegen könnte.
    sourceLang: profile.sourceLang ?? FIELD_FALLBACKS.sourceLang,
    targetLang: profile.targetLang ?? FIELD_FALLBACKS.targetLang,
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

  // Die Typenliste wird als Ganzes ersetzt, nicht in place verändert. Verglichen wird als **Menge**:
  // Sichtbar ist die feste Reihenfolge der Pillen, gespeichert die Klick-Reihenfolge – wer einen Typ
  // ab- und wieder anwählt, schickte sonst eine „Änderung", die niemand sehen kann.
  if (form.defaultTypes.length !== loaded.defaultTypes.length
    || form.defaultTypes.some((t) => !loaded.defaultTypes.includes(t))) {
    dto.defaultTypes = form.defaultTypes;
  }

  return Object.keys(dto).length === 0 ? null : dto;
}
