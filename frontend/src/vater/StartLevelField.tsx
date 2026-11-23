import { useId } from "react";
import { api } from "../lib/api";
import { FieldLabel } from "../components/InfoHint";
import { ListenAusfall } from "../components/ListenAusfall";
import { useAsync } from "../lib/useAsync";
import type { BuildPath, ExerciseSummary, Paged } from "../lib/types";
import { BUILD_LEVELS } from "./BuildPathField";

/** Die Stufe der Ziel-Übung selbst (`Exercise.BuildLevelTarget` des Servers). */
export const BUILD_LEVEL_TARGET = 3;

/**
 * Die Start-Stufen, die eine Position auf dieser Übung haben kann (B-841, B-704 Entscheidung 1): leer, wenn die Übung
 * keine Vorstufen hat – dann gibt es kein Feld. Sonst „ohne Aufbau-Weg“ (die Position spielt das Ziel wie bisher), jede
 * Stufe, auf der eine Vorstufe liegt, und die Ziel-Stufe. Der Server nimmt genau diese an (eine Stufe ohne Vorstufe lehnt
 * er mit `validation_error` ab). Eigene Funktion, damit die Regel ohne nachgebautes Formular prüfbar ist.
 */
export function startLevelOptions(unitExercises: readonly ExerciseSummary[], targetId: number) {
  const levels = [...new Set(unitExercises
    .filter((e) => e.buildTargetExerciseId === targetId && e.buildLevel != null && e.buildLevel < BUILD_LEVEL_TARGET)
    .map((e) => e.buildLevel!))].sort((a, b) => a - b);
  if (levels.length === 0) return [];
  // Beschriftet nach dem Platz auf dem Weg, wie Zeile, Report und das Band des Kindes ihn zeigen („Stufe 1 von 2“) – der
  // Wert bleibt die Stufe des Katalogs (1, 2, Ziel 3), die der Server erwartet. Zwei Skalen unter einem Wort lasen sich
  // sonst als Widerspruch: „3 · Ziel-Übung“ im Feld, „Stufe 2 von 2“ in der Zeile.
  const of = levels.length + 1;
  const art = (l: number) => BUILD_LEVELS.find((b) => b.value === l)?.label.replace(/^\d+ · /, "") ?? "";
  return [
    { value: "" as const, label: "ohne Aufbau-Weg (Ziel-Übung direkt)" },
    ...levels.map((l, i) => ({ value: l, label: `Stufe ${i + 1} von ${of} · ${art(l)}` })),
    { value: BUILD_LEVEL_TARGET, label: `Stufe ${of} von ${of} · Ziel-Übung` },
  ];
}

/**
 * Das Feld „Start-Stufe“ im Positionsformular (B-841): erscheint nur, wenn die gewählte Übung Vorstufen hat. Die
 * Vorstufen liegen in derselben Unit (B-700), darum genügt deren Liste.
 */
export function StartLevelField({ exercise, value, onChange, disabled }: {
  exercise: ExerciseSummary;
  value: number | "";
  onChange: (next: number | "") => void;
  disabled: boolean;
}) {
  const uid = useId();
  // Eine Unit trägt selten mehr als ein paar Dutzend Übungen; 500 ist die Obergrenze des Servers.
  const unit = useAsync<Paged<ExerciseSummary>>(
    () => api.searchExercises({ seriesUnitId: exercise.seriesUnitId, take: 500 }), [exercise.seriesUnitId]);
  // Die Zeilen müssen zur Unit der gewählten Übung gehören – im Ladefenster eines Wechsels stünden sonst die der vorigen.
  const rows = unit.data?.items.filter((e) => e.seriesUnitId === exercise.seriesUnitId) ?? [];
  const options = startLevelOptions(rows, exercise.id);

  if (unit.error) {
    return <ListenAusfall was="Die Vorstufen dieser Übung" fehler={unit.error} laedt={unit.loading} onRetry={unit.reload}
      schluessel={exercise.seriesUnitId} />;
  }
  if (options.length === 0) return null;
  return (
    <div className="field" style={{ maxWidth: 260 }}>
      <FieldLabel htmlFor={`${uid}-start`} topic="startBuildLevel">Start-Stufe</FieldLabel>
      <select id={`${uid}-start`} value={value} disabled={disabled}
        onChange={(e) => onChange(e.target.value === "" ? "" : Number(e.target.value))}>
        {options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  );
}

/**
 * Die Stufe einer gestuften Position für den Vater (B-841, B-704 Entscheidung 4): „Stufe 1 von 3 · gerade: Wörter
 * erkennen“, auf der Ziel-Stufe ohne den Zusatz – dort spielt das Kind die Übung der Position selbst.
 */
export function aufbauStufeText(b: BuildPath): string {
  const stufe = `Stufe ${b.level} von ${b.of}`;
  return b.level < b.of ? `${stufe} · gerade: ${b.playedTitle}` : stufe;
}
