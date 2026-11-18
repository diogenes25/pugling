import { ListenAusfall } from "../components/ListenAusfall";
import { api } from "../lib/api";
import { feedbackReasonLabel } from "../lib/labels";
import { useAsync } from "../lib/useAsync";
import type { ExerciseFeedbackSummary, FeedbackReasonCount } from "../lib/types";

/** „Falsch übersetzt ×2 · Zu schwer ×1“ – die Gründe einer Zeile, häufigster zuerst (so liefert sie der Server). */
export function reasonCounts(reasons: readonly FeedbackReasonCount[]): string {
  return reasons.map((r) => `${feedbackReasonLabel(r.reason)} ×${r.count}`).join(" · ");
}

/**
 * Was Kinder an dieser Übung gemeldet haben, für den, der sie reparieren kann (B-629, B-623 Entscheidung 7): gezählte
 * Gründe je Karte und gesamt – ohne Kind und ohne Text; das liefert der Server schon so. Nur mit Schreibrecht sichtbar.
 */
export function ExerciseFeedbackPanel({ exerciseId }: { exerciseId: number }) {
  const summary = useAsync<ExerciseFeedbackSummary>(() => api.exerciseFeedbackSummary(exerciseId), [exerciseId]);
  if (summary.loading && summary.data === null) return <div className="loading">Lade…</div>;
  if (summary.error)
    return <ListenAusfall was="Die Rückmeldungen" fehler={summary.error} laedt={summary.loading} onRetry={summary.reload} />;
  const s = summary.data;
  if (!s || s.total === 0) return <p className="muted" style={{ margin: "6px 0" }}>Keine Rückmeldungen von Kindern.</p>;
  return (
    <div style={{ margin: "6px 0" }}>
      <div><b>Rückmeldungen von Kindern ({s.total}):</b> {reasonCounts(s.byReason)}</div>
      <ul className="muted" style={{ margin: "4px 0 0", paddingLeft: 18 }}>
        {s.byCard.map((c) => (
          <li key={c.itemIndex ?? "ganz"}>
            {c.itemIndex == null ? "ganze Übung" : `Karte ${c.itemIndex + 1}`}: {reasonCounts(c.reasons)}
          </li>
        ))}
      </ul>
    </div>
  );
}
