import { useId, useRef } from "react";
import type { SortDir } from "../lib/types";

/**
 * Gemeinsame Blätter-/Sortier-Bausteine für die Vater-Listen. Das Backend paginiert server-seitig
 * (skip/take, Gesamtzahl im X-Total-Count-Header) und sortiert bei zwei Endpunkten per sort/dir –
 * diese Komponenten machen das im UI bedienbar. Bewusst präsentational: der State (skip/sort/dir)
 * liegt im jeweiligen Screen, damit er in die useAsync-Deps einfließen kann.
 */

/** Einheitliche Seitengröße der Vater-Tabellen (klein fürs UI; der Server-Default wäre 100). */
export const PAGE_SIZE = 25;

/**
 * „‹ Zurück · 26–50 von 312 · Weiter ›". Rendert nichts, wenn alles auf eine Seite passt.
 *
 * `busy` (B-116): während die nächste Seite lädt, sperrt der Pager beide Knöpfe und hält die
 * zuletzt bekannte Spanne fest, statt sie aus dem neuen `skip` zu berechnen – sonst behauptet die
 * `aria-live`-Zeile schon die neue Seite, während die Tabelle noch die alte zeigt, und ein zweiter
 * Klick könnte eine Seite überspringen.
 */
export function Pager({ skip, take, total, onSkip, busy }: {
  skip: number;
  take: number;
  total: number;
  onSkip: (skip: number) => void;
  busy?: boolean;
}) {
  // While busy, the caller's `skip` already points at the requested page but `total`/the row count
  // still describe the OLD one (that is exactly what "busy" means here) - showing the new skip against
  // the old total would announce a page that has not arrived yet. So the shown span freezes on the
  // last known-good values and only catches up once busy clears.
  const shown = useRef({ skip, take, total });
  if (!busy) shown.current = { skip, take, total };
  const { skip: shownSkip, take: shownTake, total: shownTotal } = shown.current;

  if (shownTotal <= shownTake) return null;
  const from = shownTotal === 0 ? 0 : shownSkip + 1;
  const to = Math.min(shownSkip + shownTake, shownTotal);
  const canPrev = shownSkip > 0;
  const canNext = shownSkip + shownTake < shownTotal;
  return (
    <div className="pager">
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
        disabled={busy || !canPrev} onClick={() => onSkip(Math.max(0, skip - take))}>‹ Zurück</button>
      <span className="muted tabnum" aria-live="polite">{from}–{to} von {shownTotal}</span>
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
        disabled={busy || !canNext} onClick={() => onSkip(skip + take)}>Weiter ›</button>
    </div>
  );
}

/**
 * Sagt an, wenn eine Auswahlliste nur einen Teil der Treffer zeigt. Gedacht für Listen ohne Blätterung
 * (Übungs-Auswahl im Assistenten, beim Positions-Anlegen, beim Zuweisen zur Klassenarbeit): der Server
 * liefert höchstens eine Seite, und eine still gekappte Liste liest sich wie „mehr gibt es nicht".
 */
export function TruncationHint({ shown, total }: { shown: number; total: number }) {
  if (total <= shown) return null;
  return (
    <p className="banner" style={{ margin: "6px 0", fontSize: 13 }} role="status">
      Zeigt {shown} von {total} Treffern – grenze die Filter weiter ein, um die übrigen zu sehen.
    </p>
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
