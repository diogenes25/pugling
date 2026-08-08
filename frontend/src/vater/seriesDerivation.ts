/**
 * Ableitung „Fachlehrer-Feld aus der Lehrwerk-Reihe" (B-67), als reine Regel (B-126).
 *
 * Sie liegt hier statt im Bildschirm, weil sie inzwischen drei Zustände unterscheidet – unberührt,
 * vom Werk übernommen, selbst gesetzt – und das im Formular nur prüfbar wäre, indem man einen
 * Bildschirm mit gefälschtem `fetch` nachbaut. Genau das schließt `frontend/CLAUDE.md` aus; das
 * Muster ist `reviewFeedback` (B-96) und `runWizardFinish` (B-53).
 */

export type DerivableField = "subjectId" | "sourceLang" | "targetLang";

export const DERIVABLE_FIELDS: readonly DerivableField[] = ["subjectId", "sourceLang", "targetLang"];

/** Nur die Felder einer Reihe, die eine Ableitung speisen – strukturell, damit die Regel testbar bleibt. */
export type DerivableSource = {
  subjectId?: number | null;
  sourceLanguage?: string | null;
  targetLanguage?: string | null;
};

/** Die Werte, die das Formular trägt (als Strings, wie im Formular selbst). */
export type DerivableValues = Record<DerivableField, string>;

/**
 * Worauf ein Feld zurückfällt, wenn die neue Reihe dazu schweigt.
 *
 * Für die Sprachen ist das **nicht** der leere String: der Server kennt gar keinen leeren Zustand
 * (`CreatorProfile.SourceLang` ist non-null, Vorgabe `en`/`de`) und überliest ein `null` im PATCH als
 * „nicht angegeben". Ein geleertes Feld käme also nie in der Datenbank an – die Oberfläche meldete
 * „Gespeichert." und zeigte beim nächsten Öffnen wieder den alten Wert. Beim Fach ist `""` dagegen ein
 * echter Zustand, den `clearSubject` auch übertragen kann.
 */
export const FIELD_FALLBACKS: DerivableValues = { subjectId: "", sourceLang: "en", targetLang: "de" };

/**
 * Was die Reihe zu jedem Feld beisteuert. `""` heißt **die Reihe sagt dazu nichts** – bewusst nicht
 * `null`/`undefined`, damit der Vergleich mit dem Formularwert (immer ein String) direkt aufgeht.
 */
export function derivableValues(series: DerivableSource | undefined): DerivableValues {
  return {
    subjectId: series?.subjectId != null ? String(series.subjectId) : "",
    sourceLang: series?.sourceLanguage ?? "",
    targetLang: series?.targetLanguage ?? "",
  };
}

/**
 * Trägt das Feld gerade einen Wert, der aus der Reihe stammt? Der Vergleich des **angezeigten Wertes**
 * ist der Punkt: „unberührt" allein genügt nicht – beim Bearbeiten eines gespeicherten Profils ist
 * nichts berührt, und der Hinweis behauptete sonst eine Herkunft für einen Wert, den der Creator
 * selbst gesetzt hat.
 */
export function isDerived(
  field: DerivableField,
  form: DerivableValues,
  series: DerivableSource | undefined,
  touched: ReadonlySet<DerivableField>,
): boolean {
  const value = derivableValues(series)[field];
  return value !== "" && !touched.has(field) && form[field] === value;
}

/**
 * Der Reihenwechsel. Die Regel in einem Satz: **ein abgeleitetes Feld folgt der Reihe, ein selbst
 * gesetztes nie** – und ein noch leeres bzw. auf der Vorgabe stehendes Feld wird gefüllt, aber nicht
 * geleert.
 *
 * Die vier Fälle je Feld:
 * 1. berührt → unverändert; der Creator hat in dieser Sitzung entschieden.
 * 2. der aktuelle Wert ist der der **vorigen** Reihe → er folgt der neuen; schweigt die neue, fällt er
 *    auf {@link FIELD_FALLBACKS} zurück (sonst bliebe eine Sprache stehen, die aus einem nicht mehr
 *    referenzierten Werk stammt).
 * 3. der aktuelle Wert ist der **beim Öffnen geladene** des Profils → unverändert. Der Creator hat ihn
 *    in einer früheren Sitzung gesetzt; ein Reihenwechsel darf ihn so wenig verwerfen wie `touched`.
 *    Nur beim Bearbeiten gesetzt – bei einem neuen Profil gibt es nichts Geladenes, und die Vorgabe
 *    `en`/`de` soll ja gerade überschrieben werden (B-67).
 * 4. sonst → nur füllen, wenn die neue Reihe etwas sagt.
 */
export function applySeriesChange(
  form: DerivableValues,
  touched: ReadonlySet<DerivableField>,
  previous: DerivableSource | undefined,
  next: DerivableSource | undefined,
  loaded?: DerivableValues,
): DerivableValues {
  const before = derivableValues(previous);
  const after = derivableValues(next);
  // Genau die drei Felder aufbauen, nicht `form` kopieren: der Aufrufer spreizt das Ergebnis über
  // seinen Formularzustand, und ein mitkopiertes Fremdfeld (etwa die alte `seriesId`) überschriebe
  // dabei still den frisch gesetzten Wert.
  const result: DerivableValues = { subjectId: form.subjectId, sourceLang: form.sourceLang, targetLang: form.targetLang };
  for (const field of DERIVABLE_FIELDS) {
    if (touched.has(field)) continue;
    const cameFromPrevious = before[field] !== "" && form[field] === before[field];
    if (cameFromPrevious) result[field] = after[field] !== "" ? after[field] : FIELD_FALLBACKS[field];
    else if (loaded && form[field] === loaded[field]) continue;
    else if (after[field] !== "") result[field] = after[field];
  }
  return result;
}
