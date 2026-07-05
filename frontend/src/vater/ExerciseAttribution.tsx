import type { ExerciseSummary } from "../lib/types";

/** Die relevanten Autorschafts-Felder – funktioniert für ExerciseSummary wie ExerciseDetail. */
type Authored = Pick<ExerciseSummary, "authorName" | "isOwn">;

/**
 * Kurztext-Attribution für kompakte Zeilen (z. B. Lehrplan-Assistent): „von X" bzw. „von X (du)".
 * Gibt <c>null</c> zurück, wenn keine Attribution anzuzeigen ist (geseedete System-Übung).
 */
export function authorText(e: Authored): string | null {
  if (!e.authorName) return null;
  return `von ${e.authorName}${e.isOwn ? " (du)" : ""}`;
}

/**
 * Attribution als Pill (geteilte Bibliothek): eigene vs. von anderem Vater vs. System-Übung.
 * Die eine Stelle für diese Dreiteilung – Wizard und Verwaltung nutzen sie gemeinsam.
 */
export function ExerciseAttribution({ e }: { e: Authored }) {
  if (e.isOwn) return <span className="pill lime" style={{ fontSize: 11 }}>deine Übung</span>;
  if (e.authorName) return <span className="pill" style={{ fontSize: 11 }}>🤝 von {e.authorName}</span>;
  return <span className="pill muted" style={{ fontSize: 11 }}>System-Übung</span>;
}
