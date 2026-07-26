import type { ItemReport } from "../lib/types";

/**
 * Die „sitzt"-Ampel eines einzelnen Inhalts: neu (grau) · unsicher (magenta) · in Arbeit (neutral) ·
 * sitzt sicher (lime).
 *
 * Geteilt zwischen dem positionsgebundenen Report und der plan-übergreifenden Lernstand-Sicht: der Vater
 * soll denselben Zustand nicht an zwei Stellen unterschiedlich eingefärbt sehen.
 */
export function MasteryPill({ it, maxBox }: { it: Pick<ItemReport, "introduced" | "box" | "masteryPercent">; maxBox: number }) {
  if (!it.introduced) return <span className="pill">neu</span>;
  if (it.box >= maxBox) return <span className="pill lime">sitzt · {it.masteryPercent}%</span>;
  if (it.masteryPercent < 50) return <span className="pill mag">{it.masteryPercent}% · Box {it.box}</span>;
  return <span className="pill">{it.masteryPercent}% · Box {it.box}</span>;
}
