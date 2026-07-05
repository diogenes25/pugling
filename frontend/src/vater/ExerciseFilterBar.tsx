import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { CategoryResponse, ChapterResponse, SchoolType, SubjectResponse } from "../lib/types";

/**
 * Faceted Filter für die Katalog-Übungssuche (Fach, Kapitel, Klasse, Schulart, Typ, Art, Freitext).
 * Ersetzt das unübersichtliche flache Pulldown beim Zusammenstellen eines Lehrplans. Die Komponente
 * ist zustandslos: der Aufrufer hält den {@link ExerciseFilter} und führt die eigentliche Suche aus.
 */
export interface ExerciseFilter {
  subjectId?: number;
  chapterId?: number;
  grade?: number;
  schoolType?: SchoolType;
  categoryId?: number;
  type?: string;
  search?: string;
}

const SCHOOL_TYPES: SchoolType[] = ["Grundschule", "Hauptschule", "Realschule", "Gymnasium", "Gesamtschule", "Berufsschule"];
const TYPES: { key: string; label: string }[] = [
  { key: "Vocabulary", label: "Vokabeln" }, { key: "Arithmetic", label: "Rechnen" },
  { key: "Cloze", label: "Lückentext" }, { key: "Matching", label: "Zuordnung" },
  { key: "List", label: "Liste" }, { key: "Birkenbihl", label: "Birkenbihl" },
];

export function ExerciseFilterBar({ value, onChange, subjects }: {
  value: ExerciseFilter;
  onChange: (next: ExerciseFilter) => void;
  subjects: SubjectResponse[];
}) {
  // Kapitel + Arten hängen am gewählten Fach und werden reaktiv nachgeladen.
  const chapters = useAsync<ChapterResponse[]>(
    () => (value.subjectId ? api.chapters(value.subjectId) : Promise.resolve([])), [value.subjectId]);
  const categories = useAsync<CategoryResponse[]>(
    () => (value.subjectId ? api.categories(value.subjectId) : Promise.resolve([])), [value.subjectId]);

  const set = (patch: Partial<ExerciseFilter>) => onChange({ ...value, ...patch });
  // Fachwechsel macht die fachabhängigen Facetten (Kapitel, Art) hinfällig → mit zurücksetzen.
  const setSubject = (subjectId?: number) => onChange({ ...value, subjectId, chapterId: undefined, categoryId: undefined });
  const hasFilter = value.subjectId != null || value.chapterId != null || value.grade != null
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
        <label>Kapitel</label>
        <select aria-label="Kapitel-Filter" value={value.chapterId ?? ""} disabled={!value.subjectId}
          onChange={(e) => set({ chapterId: e.target.value ? Number(e.target.value) : undefined })}>
          <option value="">– alle –</option>
          {chapters.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
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
          {TYPES.map((t) => <option key={t.key} value={t.key}>{t.label}</option>)}
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
