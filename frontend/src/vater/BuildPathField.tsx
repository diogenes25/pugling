import { useId } from "react";
import { api } from "../lib/api";
import { FieldLabel } from "../components/InfoHint";
import { ListenAusfall } from "../components/ListenAusfall";
import { useAsync } from "../lib/useAsync";
import type { ExerciseSummary, Paged } from "../lib/types";

/** Stufen einer Vorstufe (B-700); die Ziel-Übung selbst ist die oberste Stufe und trägt keine. */
export const BUILD_LEVELS = [
  { value: 1, label: "1 · erkennen" },
  { value: 2, label: "2 · einsetzen mit Auswahl" },
] as const;

/**
 * Welche Übungen der Unit als Ziel taugen: nicht die Übung selbst und keine, die selbst Vorstufe ist – der Weg ist
 * eine Ebene tief (B-700, Entscheidung 2). Dieselbe Regel lehnt der Server mit `validation_error` ab; die Auswahl
 * bietet nur an, was er annimmt. Eigene Funktion, damit die Regel ohne nachgebauten Dialog prüfbar ist.
 */
export function buildTargetOptions(unitExercises: readonly ExerciseSummary[], selfId: number, seriesUnitId: number) {
  return unitExercises.filter((e) => e.id !== selfId && e.seriesUnitId === seriesUnitId && e.buildTargetExerciseId == null);
}

/**
 * Der Abschnitt „Aufbau-Weg" im Übungseditor (B-704, Entscheidung 2): „Vorstufe von" (Ziel-Übungen derselben Unit) und
 * „Stufe". Ist die Übung selbst Ziel von Vorstufen, gibt es nichts zu wählen – sie kann dann keine Vorstufe werden,
 * und der Abschnitt sagt das, statt eine Auswahl anzubieten, die der Server ablehnt.
 *
 * Die Werte reisen im Vollersatz-PUT mit (`payloadFrom`); ohne sie löschte jedes Speichern die Stufe (B-700 AK 3).
 */
export function BuildPathField({ selfId, seriesUnitId, level, targetId, onChange, disabled }: {
  selfId: number;
  seriesUnitId: number;
  level: number | "";
  targetId: number | null;
  onChange: (next: { level: number | ""; targetId: number | null }) => void;
  disabled: boolean;
}) {
  const uid = useId();
  // Eine Unit trägt selten mehr als ein paar Dutzend Übungen; 500 ist die Obergrenze des Servers.
  const unit = useAsync<Paged<ExerciseSummary>>(() => api.searchExercises({ seriesUnitId, take: 500 }), [seriesUnitId]);
  const rows = unit.data?.items.filter((e) => e.seriesUnitId === seriesUnitId) ?? [];
  const prerequisites = rows.filter((e) => e.buildTargetExerciseId === selfId);
  const options = buildTargetOptions(rows, selfId, seriesUnitId);

  return (
    <div className="field" role="group" aria-labelledby={`${uid}-head`}>
      <FieldLabel id={`${uid}-head`} topic="exerciseBuildPath">Aufbau-Weg</FieldLabel>
      <ListenAusfall was="Die Übungen dieser Unit" fehler={unit.error} laedt={unit.loading} onRetry={unit.reload} schluessel={seriesUnitId} />
      {unit.loading && unit.data === null && <div className="loading">Lade…</div>}
      {/* Mehr Übungen als geladen: dann kann eine Vorstufe fehlen, die diese Übung zum Ziel macht – lieber sagen als
          eine Auswahl anbieten, die der Server ablehnt. */}
      {unit.data && unit.data.total > unit.data.items.length && (
        <p className="sub" style={{ margin: 0 }}>
          Diese Unit hat mehr als {unit.data.items.length} Übungen; die Auswahl zeigt nur die ersten.
        </p>
      )}
      {unit.data && prerequisites.length > 0 && (
        <p className="sub" style={{ margin: 0 }}>
          Diese Übung ist Ziel von {prerequisites.length === 1 ? "einer Vorstufe" : `${prerequisites.length} Vorstufen`}
          {" "}({prerequisites.map((p) => p.title).join(", ")}) und kann darum selbst keine Vorstufe sein.
        </p>
      )}
      {unit.data && prerequisites.length === 0 && (
        <div className="form-grid">
          <div className="field">
            <label htmlFor={`${uid}-target`}>Vorstufe von</label>
            <select id={`${uid}-target`} value={targetId ?? ""} disabled={disabled}
              onChange={(e) => {
                const next = e.target.value === "" ? null : Number(e.target.value);
                // Ohne Ziel keine Stufe; mit Ziel beginnt die Auswahl bei 1 statt leer (beides oder keins, B-700).
                onChange({ targetId: next, level: next === null ? "" : (level === "" ? 1 : level) });
              }}>
              <option value="">– keine Vorstufe –</option>
              {options.map((e) => <option key={e.id} value={e.id}>{e.title}</option>)}
              {/* Ein gespeichertes Ziel, das die Liste nicht (mehr) anbietet, bleibt sichtbar statt still zu verschwinden. */}
              {targetId != null && !options.some((e) => e.id === targetId) && (
                <option value={targetId}>Übung #{targetId}</option>
              )}
            </select>
          </div>
          <div className="field">
            <label htmlFor={`${uid}-level`}>Stufe</label>
            <select id={`${uid}-level`} value={level} disabled={disabled || targetId === null}
              onChange={(e) => onChange({ targetId, level: e.target.value === "" ? "" : Number(e.target.value) })}>
              {targetId === null && <option value="">– erst Ziel wählen –</option>}
              {BUILD_LEVELS.map((l) => <option key={l.value} value={l.value}>{l.label}</option>)}
            </select>
          </div>
        </div>
      )}
    </div>
  );
}
