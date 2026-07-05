import { useId } from "react";
import type { SortDir } from "../lib/types";

/**
 * Gemeinsame Blätter-/Sortier-Bausteine für die Vater-Listen. Das Backend paginiert server-seitig
 * (skip/take, Gesamtzahl im X-Total-Count-Header) und sortiert bei zwei Endpunkten per sort/dir –
 * diese Komponenten machen das im UI bedienbar. Bewusst präsentational: der State (skip/sort/dir)
 * liegt im jeweiligen Screen, damit er in die useAsync-Deps einfließen kann.
 */

/** Einheitliche Seitengröße der Vater-Tabellen (klein fürs UI; der Server-Default wäre 100). */
export const PAGE_SIZE = 25;

/** „‹ Zurück · 26–50 von 312 · Weiter ›". Rendert nichts, wenn alles auf eine Seite passt. */
export function Pager({ skip, take, total, onSkip }: {
  skip: number;
  take: number;
  total: number;
  onSkip: (skip: number) => void;
}) {
  if (total <= take) return null;
  const from = total === 0 ? 0 : skip + 1;
  const to = Math.min(skip + take, total);
  const canPrev = skip > 0;
  const canNext = skip + take < total;
  return (
    <div className="pager">
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
        disabled={!canPrev} onClick={() => onSkip(Math.max(0, skip - take))}>‹ Zurück</button>
      <span className="muted tabnum" aria-live="polite">{from}–{to} von {total}</span>
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
        disabled={!canNext} onClick={() => onSkip(skip + take)}>Weiter ›</button>
    </div>
  );
}

/**
 * Sortierbarer Tabellen-Spaltenkopf (klickbar, zeigt ▲/▼ bei aktiver Spalte, setzt `aria-sort`).
 * Klick auf die aktive Spalte dreht die Richtung, sonst wird nach dieser Spalte sortiert.
 */
export function SortableTh<K extends string>({ label, sortKey, active, dir, numeric, onSort }: {
  label: string;
  sortKey: K;
  active: boolean;
  dir: SortDir;
  numeric?: boolean;
  onSort: (key: K, dir: SortDir) => void;
}) {
  const next: SortDir = active && dir === "asc" ? "desc" : "asc";
  return (
    <th className={`sortable${numeric ? " num" : ""}`} aria-sort={active ? (dir === "asc" ? "ascending" : "descending") : "none"}>
      <button type="button" className="th-sort" onClick={() => onSort(sortKey, next)}>
        {label}<span className="sort-ind" aria-hidden="true">{active ? (dir === "asc" ? "▲" : "▼") : ""}</span>
      </button>
    </th>
  );
}

/** Sortier-Auswahl für Listen ohne Tabellenkopf (z.B. Karten): Feld-Dropdown + Richtungs-Umschalter. */
export function SortControl<K extends string>({ options, value, dir, onChange }: {
  options: { key: K; label: string }[];
  value: K;
  dir: SortDir;
  onChange: (key: K, dir: SortDir) => void;
}) {
  const id = useId();
  return (
    <div className="row" style={{ gap: 6, alignItems: "center" }}>
      <label className="muted" htmlFor={id} style={{ fontSize: 12, textTransform: "uppercase", letterSpacing: ".06em" }}>Sortieren</label>
      <select id={id} value={value} onChange={(e) => onChange(e.target.value as K, dir)}>
        {options.map((o) => <option key={o.key} value={o.key}>{o.label}</option>)}
      </select>
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
        aria-label={dir === "asc" ? "Aufsteigend – zu absteigend wechseln" : "Absteigend – zu aufsteigend wechseln"}
        onClick={() => onChange(value, dir === "asc" ? "desc" : "asc")}>
        {dir === "asc" ? "▲" : "▼"}
      </button>
    </div>
  );
}
