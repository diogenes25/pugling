import { useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { ListenAusfall } from "../components/ListenAusfall";
import { formatDate, heuteImLerntag } from "../lib/datum";
import type { ChildPeriod, ChildrenOverview, OverviewGranularity } from "../lib/types";

const GRANULARITIES: { value: OverviewGranularity; label: string }[] = [
  { value: "Day", label: "Tag" }, { value: "Week", label: "Woche" }, { value: "Month", label: "Monat" },
];

const iso = (d: Date) => d.toISOString().slice(0, 10);

/*
 * Der Zeitraum je Ansicht – in Kalendertagen des Lerntags (der Aufrufer reicht ihn als UTC-Mittag herein), und nie über die 92 Tage, die er annimmt (B-39,
 * Entscheidung 9): zwei Wochen Tage, zwölf ganze Wochen ab einem Montag (sonst wäre der erste Balken eine halbe Woche),
 * drei Kalendermonate bis heute.
 */
export function rangeFor(granularity: OverviewGranularity, today: Date): { from: string; to: string } {
  const to = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate()));
  const from = new Date(to);
  if (granularity === "Day") from.setUTCDate(from.getUTCDate() - 13);
  else if (granularity === "Week") from.setUTCDate(from.getUTCDate() - ((from.getUTCDay() + 6) % 7) - 77);
  else from.setUTCMonth(from.getUTCMonth() - 2, 1);
  return { from: iso(from), to: iso(to) };
}

const MONAT = new Intl.DateTimeFormat("de-DE", { month: "long", year: "numeric", timeZone: "UTC" });

/** Der Name eines Balkens – Status in Worten, nicht nur in der Farbe (B-39, AK 5). */
export function periodLabel(p: ChildPeriod, granularity: OverviewGranularity): string {
  const wann = granularity === "Day" ? formatDate(p.start)
    : granularity === "Week" ? `Woche ab ${formatDate(p.start)}`
    : MONAT.format(new Date(`${p.start}T00:00:00Z`));
  const muenzen = `${p.coins} ${p.coins === 1 ? "Münze" : "Münzen"}`;
  // B-954: Münzen kommen aus dem Konto, also auch an einem Tag ohne Ziel (Kür, Truhe) – dort nur, wenn es welche gab.
  if (p.goalsTotal === 0) return p.coins > 0 ? `${wann}: kein Ziel, ${muenzen}` : `${wann}: kein Ziel`;
  return `${wann}: ${p.goalsMet} von ${p.goalsTotal} Zielen${p.dutyDone ? " – Pflicht geschafft ✓" : ""}, ${muenzen}`;
}

/**
 * Die Kinder über die Zeit (B-39): je Kind eine Reihe von Balken, einer je Tag, Woche oder Monat – die Höhe ist der Anteil
 * erfüllter Ziele. Gerechnet wird am Server (`children/overview`), hier wird nur gezeichnet; ohne Diagramm-Bibliothek und
 * ohne Animation (darum braucht es keine Rücksicht auf `prefersReducedMotion`). Jede Kindzeile führt in den Lernstand.
 */
export function ChildrenTimeline({ reloadToken = 0 }: { reloadToken?: number }) {
  const [granularity, setGranularity] = useState<OverviewGranularity>("Week");
  // Der Lerntag als UTC-Mittag: `rangeFor` rechnet in UTC-Feldern, und um 00:30 deutscher Zeit ist schon morgen (B-730).
  const range = rangeFor(granularity, new Date(`${heuteImLerntag()}T12:00:00Z`));
  const overview = useAsync<ChildrenOverview>(
    () => api.childrenOverview({ ...range, granularity }), [granularity, range.from, range.to, reloadToken]);

  return (
    <section>
      <div className="row" style={{ alignItems: "center", gap: 10 }}>
        <h2 className="h-section" style={{ margin: 0 }}>Über die Zeit</h2>
        <div className="row" role="group" aria-label="Zeitraster" style={{ gap: 6, marginLeft: "auto" }}>
          {GRANULARITIES.map((g) => (
            <button key={g.value} type="button" className={`btn small ${granularity === g.value ? "" : "ghost"}`}
              style={{ width: "auto" }} aria-pressed={granularity === g.value} onClick={() => setGranularity(g.value)}>
              {g.label}
            </button>
          ))}
        </div>
      </div>
      <ListenAusfall was="Die Zeitachse" fehler={overview.error} laedt={overview.loading} onRetry={overview.reload}
        schluessel={granularity} />
      {/* Ein Wechsel des Rasters lässt die alten Balken fallen – ohne diese Zeile stünde die Fläche bis zur Antwort leer (B-116). */}
      {overview.loading && (overview.data === null || overview.stale) ? <div className="loading">Lade…</div> : (
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {!overview.stale && overview.data?.children.map((c) => (
            <div key={c.childId} className="card">
              <div className="row" style={{ alignItems: "center" }}>
                <b>{c.name}</b>
                <Link to={`/vater/kind/${c.childId}/lernstand`} style={{ marginLeft: "auto" }}>Lernstand von {c.name}</Link>
              </div>
              <ol className="zeitachse" aria-label={`Zeitachse von ${c.name}`}
                style={{ display: "flex", alignItems: "flex-end", gap: 4, listStyle: "none", padding: 0, margin: "8px 0 0", height: 64 }}>
                {c.periods.map((p) => {
                  const anteil = p.goalsTotal === 0 ? 0 : p.goalsMet / p.goalsTotal;
                  const label = periodLabel(p, granularity);
                  return (
                    <li key={p.start} title={label}
                      style={{ flex: 1, display: "flex", flexDirection: "column", justifyContent: "flex-end", alignItems: "center", height: "100%" }}>
                      {/* Der Status als Text im Element, nicht nur als `aria-label` an einem nicht bedienbaren Listeneintrag. */}
                      <span className="sr-only">{label}</span>
                      <span aria-hidden="true" style={{
                        display: "block", width: "100%", minHeight: 2, height: `${Math.round(anteil * 48)}px`,
                        background: p.dutyDone ? "var(--lime)" : p.goalsTotal === 0 ? "var(--stroke)" : "var(--magenta)",
                      }} />
                      <span aria-hidden="true" className="muted" style={{ fontSize: 10 }}>
                        {p.goalsTotal === 0 ? "–" : p.dutyDone ? "✓" : p.goalsMet}
                      </span>
                    </li>
                  );
                })}
              </ol>
              {c.periods.length > 0 && (
                <div className="row muted" aria-hidden="true" style={{ fontSize: 11 }}>
                  <span>{formatDate(c.periods[0].start)}</span>
                  <span style={{ marginLeft: "auto" }}>{formatDate(c.periods[c.periods.length - 1].end)}</span>
                </div>
              )}
            </div>
          ))}
          {overview.data?.children.length === 0 && !overview.stale && <p className="muted">Noch keine Kinder.</p>}
        </div>
      )}
    </section>
  );
}
