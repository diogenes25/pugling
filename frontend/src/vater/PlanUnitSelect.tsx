import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { ListenAusfall } from "../components/ListenAusfall";
import type { SeriesUnitResponse, TextbookSeriesResponse } from "../lib/types";

/** Die Units eines Fachs, je Reihe gebündelt – mit dem Fach, für das sie geladen wurden (B-170). */
interface UnitsOfSubject {
  subjectId: number;
  groups: { series: TextbookSeriesResponse; units: SeriesUnitResponse[] }[];
}

/**
 * Die **optionale Unit** eines Lehrplans (B-539): eine Auswahl über die Units aller Reihen des gewählten Fachs, je Reihe
 * eine Gruppe. Die Unit legt beim Server das Fach fest – darum bietet die Auswahl nur Units des gewählten Fachs an;
 * „– keine Unit –" entfernt sie (der Aufrufer schickt dann `clearSeriesUnit`).
 *
 * Die geladene Liste trägt ihr Fach mit: im Ladefenster nach einem Fachwechsel stünden sonst die Units des alten Fachs
 * zur Wahl, und der Server wiese die Kombination als Widerspruch ab.
 */
export function PlanUnitSelect({ id, subjectId, value, onChange, currentLabel, disabled }: {
  id: string;
  subjectId: number | "";
  value: number | "";
  onChange: (seriesUnitId: number | "") => void;
  /** Label der gespeicherten Unit, falls sie (noch) nicht in der Liste steht – etwa während des Ladens. */
  currentLabel?: string | null;
  disabled?: boolean;
}) {
  const units = useAsync<UnitsOfSubject | null>(async () => {
    if (subjectId === "") return null;
    const series = await api.textbookSeries({ subjectId });
    const groups = await Promise.all(series.map(async (s) => ({ series: s, units: await api.seriesUnits(s.id) })));
    return { subjectId, groups: groups.filter((g) => g.units.length > 0) };
  }, [subjectId]);

  const current = units.data && units.data.subjectId === subjectId ? units.data : null;
  const listed = current?.groups.some((g) => g.units.some((u) => u.id === value)) ?? false;

  return (
    <>
      <select id={id} value={value} disabled={disabled || subjectId === ""}
        onChange={(e) => onChange(e.target.value === "" ? "" : Number(e.target.value))}>
        <option value="">– keine Unit –</option>
        {value !== "" && !listed && <option value={value}>{currentLabel ?? `Unit #${value}`}</option>}
        {current?.groups.map((g) => (
          <optgroup key={g.series.id} label={g.series.name}>
            {g.units.map((u) => <option key={u.id} value={u.id}>{u.label}</option>)}
          </optgroup>
        ))}
      </select>
      <ListenAusfall was="Die Units des Fachs" fehler={units.error} laedt={units.loading} onRetry={units.reload}
        schluessel={subjectId} />
    </>
  );
}
