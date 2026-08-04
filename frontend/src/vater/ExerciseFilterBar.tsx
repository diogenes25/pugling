import { useState } from "react";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { SCHOOL_TYPES } from "../lib/labels";
import { useExerciseTypes } from "../lib/exerciseTypes";
import type { CategoryResponse, SchoolType, SeriesUnitResponse, SubjectResponse, TextbookSeriesResponse } from "../lib/types";

/**
 * Faceted Filter für die Katalog-Übungssuche (Fach, Lehrwerk-Unit, Klasse, Schulart, Typ, Art, Freitext).
 * Ersetzt das unübersichtliche flache Pulldown beim Zusammenstellen eines Lehrplans. Die Komponente
 * ist zustandslos: der Aufrufer hält den {@link ExerciseFilter} und führt die eigentliche Suche aus.
 *
 * Seit B-106 hängt jede Übung an einer Lehrwerk-Unit statt an einem Kapitel; die Reihe (`series`) ist
 * nur eine Zwischenstufe der Auswahl hier im Browser (Reihe → Unit), sie geht nicht in den Filter selbst.
 * Die Reihen-Auswahl lebt bewusst nur lokal (nicht aus `value.seriesUnitId` rückableitbar) – die Komponente
 * darf darum nur mit einem **leeren** `value.seriesUnitId` gestartet werden; ein vorbelegter Aufruf ließe
 * „Unit" einen Wert zeigen, während „Reihe" auf „– alle –" stünde und das Feld gesperrt bliebe.
 */
export interface ExerciseFilter {
  subjectId?: number;
  seriesUnitId?: number;
  grade?: number;
  schoolType?: SchoolType;
  categoryId?: number;
  type?: string;
  search?: string;
}



export function ExerciseFilterBar({ value, onChange, subjects }: {
  value: ExerciseFilter;
  onChange: (next: ExerciseFilter) => void;
  subjects: SubjectResponse[];
}) {
  // Reihe ist nur eine lokale Zwischenauswahl (Reihe → Unit); Arten hängen am gewählten Fach.
  const [seriesId, setSeriesId] = useState<number | "">("");
  const types = useExerciseTypes();
  const series = useAsync<TextbookSeriesResponse[]>(
    () => api.textbookSeries(value.subjectId ? { subjectId: value.subjectId } : {}), [value.subjectId]);
  const units = useAsync<SeriesUnitResponse[]>(
    () => (seriesId ? api.seriesUnits(seriesId) : Promise.resolve([])), [seriesId]);
  const categories = useAsync<CategoryResponse[]>(
    () => (value.subjectId ? api.categories(value.subjectId) : Promise.resolve([])), [value.subjectId]);

  const set = (patch: Partial<ExerciseFilter>) => onChange({ ...value, ...patch });
  // Fachwechsel macht die fachabhängigen Facetten (Reihe/Unit, Art) hinfällig → mit zurücksetzen.
  const setSubject = (subjectId?: number) => {
    setSeriesId("");
    onChange({ ...value, subjectId, seriesUnitId: undefined, categoryId: undefined });
  };
  const hasFilter = value.subjectId != null || value.seriesUnitId != null || value.grade != null
    || value.schoolType != null || value.categoryId != null || value.type != null || (value.search ?? "") !== "";

  return (
    <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
      <div className="field" style={{ minWidth: 150 }}>
        <label>Fach</label>
        <select aria-label="Fach-Filter" value={value.subjectId ?? ""}
          onChange={(e) => setSubject(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">– alle –</option>
          {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
      <div className="field" style={{ minWidth: 150 }}>
        <label>Reihe</label>
        <select aria-label="Reihe-Filter" value={seriesId}
          onChange={(e) => { const v = e.target.value ? Number(e.target.value) : ""; setSeriesId(v); set({ seriesUnitId: undefined }); }}>
          <option value="">– alle –</option>
          {series.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
      <div className="field" style={{ minWidth: 150 }}>
        <label>Unit</label>
        <select aria-label="Unit-Filter" value={value.seriesUnitId ?? ""} disabled={!seriesId}
          onChange={(e) => set({ seriesUnitId: e.target.value ? Number(e.target.value) : undefined })}>
          <option value="">– alle –</option>
          {units.data?.map((u) => <option key={u.id} value={u.id}>{u.label}</option>)}
        </select>
      </div>
      <div className="field" style={{ maxWidth: 110 }}>
        <label>Klasse</label>
        <input type="number" min={1} max={13} aria-label="Klassenstufe-Filter" value={value.grade ?? ""}
          onChange={(e) => set({ grade: e.target.value ? Number(e.target.value) : undefined })} />
      </div>
      <div className="field" style={{ minWidth: 140 }}>
        <label>Schulart</label>
        <select aria-label="Schulart-Filter" value={value.schoolType ?? ""}
          onChange={(e) => set({ schoolType: (e.target.value || undefined) as SchoolType | undefined })}>
          <option value="">– alle –</option>
          {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>
      <div className="field" style={{ minWidth: 140 }}>
        <label>Typ</label>
        <select aria-label="Typ-Filter" value={value.type ?? ""}
          onChange={(e) => set({ type: e.target.value || undefined })}>
          <option value="">– alle –</option>
          {(types?.all ?? []).map((t) => <option key={t.type} value={t.type}>{t.label}</option>)}
        </select>
      </div>
      <div className="field" style={{ minWidth: 140 }}>
        <label>Art</label>
        <select aria-label="Art-Filter" value={value.categoryId ?? ""} disabled={!value.subjectId}
          onChange={(e) => set({ categoryId: e.target.value ? Number(e.target.value) : undefined })}>
          <option value="">– alle –</option>
          {categories.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </div>
      <div className="field" style={{ flex: "1 1 160px" }}>
        <label>Suche (Titel/Beschreibung)</label>
        <input aria-label="Freitext-Filter" value={value.search ?? ""}
          onChange={(e) => set({ search: e.target.value || undefined })} placeholder="Stichwort…" />
      </div>
      {hasFilter && (
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          onClick={() => onChange({})}>Filter zurücksetzen</button>
      )}
    </div>
  );
}
