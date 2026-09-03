import type {
  PublisherResponse, SeriesUnitResponse, SubjectResponse, TextbookSeriesResponse,
} from "./types";
import type { ReferenceOption } from "../components/ReferenceSelect";

/*
 * Die Optionslisten für `ReferenceSelect` (B-172). Hier, weil das Bauteil keine Fachbegriffe kennen soll
 * und zwei dieser Beschriftungen an mehr als einer Stelle gebraucht werden – die Reihe stand vorher
 * wörtlich gleich im Lehrbuch am Kind und im Fachlehrer-Profil, und zwei Formulierungen desselben Begriffs
 * werden zwei Bedeutungen (`frontend/CLAUDE.md`).
 */

export const subjectOptions = (subjects: readonly SubjectResponse[]): ReferenceOption[] =>
  subjects.map((s) => ({ value: String(s.id), label: s.name }));

export const publisherOptions = (publishers: readonly PublisherResponse[]): ReferenceOption[] =>
  publishers.map((p) => ({ value: String(p.id), label: p.name }));

/** Der Verlag gehört zur Reihe: „Access (Cornelsen)" trennt zwei Reihen gleichen Namens. */
export const seriesOptions = (series: readonly TextbookSeriesResponse[]): ReferenceOption[] =>
  series.map((s) => ({
    value: String(s.id),
    label: `${s.name}${s.publisherName ? ` (${s.publisherName})` : ""}`,
  }));

/**
 * Die Units **einer** Reihe. Die Klassenstufe steht vorn, weil eine Reihe dieselbe Unit-Nummer über
 * mehrere Bände trägt.
 *
 * `seriesId` ist Pflicht und nicht bequemlichkeitshalber optional (B-195): `useAsync` behält seine `data`
 * über einen Wechsel der Abhängigkeit, also stehen im Ladefenster nach einem Reihenwechsel noch die Units
 * der **alten** Reihe da — wählbar, und ihr Abschicken endet am Server mit `400`. Die Option trägt ihre
 * Reihe selbst (`SeriesUnitResponse.seriesId`), also gilt hier die Hausregel „die Zeile bringt ihr
 * Elternteil mit und rendert bei Nichtpassung nicht" (`frontend/CLAUDE.md`) — kein Schlüsselvergleich
 * nötig, anders als bei einer Trefferliste, deren Zeilen ihren Filter nicht kennen (B-167).
 *
 * Als **Parameter** und nicht als Filter beim Aufrufer, damit die zweite Aufrufstelle ihn nicht vergessen
 * kann: genau so verlor B-172 seinen achten Fall (B-194).
 *
 * `null` heißt „keine Reihe gewählt" und ergibt darum nichts zur Auswahl.
 */
export const unitOptions = (
  units: readonly SeriesUnitResponse[], seriesId: number | null,
): ReferenceOption[] =>
  seriesId == null ? [] : units.filter((u) => u.seriesId === seriesId).map((u) => ({
    value: String(u.id),
    label: `${u.grade != null ? `Kl. ${u.grade}: ` : ""}${u.label}`,
  }));
