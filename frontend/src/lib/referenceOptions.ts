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

/** Die Klassenstufe vorn, weil eine Reihe dieselbe Unit-Nummer über mehrere Bände trägt. */
export const unitOptions = (units: readonly SeriesUnitResponse[]): ReferenceOption[] =>
  units.map((u) => ({
    value: String(u.id),
    label: `${u.grade != null ? `Kl. ${u.grade}: ` : ""}${u.label}`,
  }));
