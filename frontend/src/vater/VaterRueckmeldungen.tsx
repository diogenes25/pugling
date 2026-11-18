import { useState } from "react";
import { Link } from "react-router-dom";
import { ListenAusfall } from "../components/ListenAusfall";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { formatDate } from "../lib/datum";
import { feedbackReasonLabel } from "../lib/labels";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { ChildResponse, FeedbackEntry, FeedbackStatus } from "../lib/types";

/** Der Status-Filter der Seite: offen (Vorgabe – das ist die Arbeit), erledigt oder alle. */
type StatusFilter = FeedbackStatus | "alle";

/**
 * Die Rückmeldungen der eigenen Kinder zu Karten (B-629, B-623 Entscheidung 7): je Meldung Kind, Übung, Karte samt
 * Lösung, die letzte gespeicherte Antwort des Kindes und sein Text. „Erledigt“ gilt für alle Betreuer des Kindes, und das
 * Kind bekommt einmal einen Hinweis. Ein Fehler in der Karte wird an der Übung korrigiert – der Sprung führt zum Plan,
 * in dem das Kind sie spielt.
 */
export function VaterRueckmeldungen() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | "">("");
  const [status, setStatus] = useState<StatusFilter>("Open");
  const list = useAsync<FeedbackEntry[]>(
    () => api.listFeedback(childId === "" ? undefined : childId, status === "alle" ? undefined : status),
    [childId, status]);
  // Eine Instanz für die ganze Liste: eine laufende Änderung sperrt die übrigen Knöpfe sichtbar (`disabled={busy}`).
  const action = useAction();

  async function setze(entry: FeedbackEntry, next: FeedbackStatus) {
    if (await action.run(() => api.updateFeedback(entry.id, next),
      next === "Done" ? "Als erledigt markiert – das Kind bekommt einen Hinweis." : "Wieder geöffnet."))
      list.reload();
  }

  return (
    <section>
      <h2 className="h-section">Rückmeldungen</h2>
      <p className="muted">Was deine Kinder an einzelnen Karten gemeldet haben. Eine Meldung ändert nichts an Punkten oder
        Lernstand – sie ist ein Hinweis an dich, die Übung zu prüfen.</p>
      <div className="row" style={{ gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
        <div className="field" style={{ maxWidth: 240 }}>
          <label htmlFor="rm-kind">Kind</label>
          <select id="rm-kind" value={childId} onChange={(e) => setChildId(e.target.value === "" ? "" : Number(e.target.value))}>
            <option value="">alle Kinder</option>
            {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className="field" style={{ maxWidth: 200 }}>
          <label htmlFor="rm-status">Status</label>
          <select id="rm-status" value={status} onChange={(e) => setStatus(e.target.value as StatusFilter)}>
            <option value="Open">offen</option>
            <option value="Done">erledigt</option>
            <option value="alle">alle</option>
          </select>
        </div>
      </div>
      <StatusBanner message={action.message} />
      {list.loading && list.data === null ? <div className="loading">Lade…</div>
        : list.error ? <ListenAusfall was="Die Rückmeldungen" fehler={list.error} laedt={list.loading}
          onRetry={list.reload} schluessel={`${childId}|${status}`} />
        : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead>
                <tr><th>Kind</th><th>Übung · Karte</th><th>Grund</th><th>Antwort des Kindes</th><th>Text</th><th>Zuletzt</th><th /></tr>
              </thead>
              <tbody>
                {list.data?.map((f) => (
                  <tr key={f.id}>
                    <td>{f.childName}</td>
                    <td>
                      {f.studyPlanId != null
                        ? <Link to={`/vater/plan/${f.studyPlanId}`}>{f.exerciseTitle}</Link>
                        : f.exerciseTitle}
                      {f.cue != null && <div className="muted">{f.cue} → {f.solution}</div>}
                      {f.itemIndex == null && <div className="muted">ganze Übung</div>}
                    </td>
                    <td>{feedbackReasonLabel(f.reason)}{f.count > 1 && <span className="muted"> ×{f.count}</span>}</td>
                    <td className="muted">{f.childAnswer ?? "—"}</td>
                    <td style={{ whiteSpace: "pre-line" }}>{f.text ?? <span className="muted">—</span>}</td>
                    <td className="muted">{formatDate(f.lastReportedAt)}</td>
                    <td>
                      {f.status === "Open"
                        ? <button type="button" className="btn small" disabled={action.busy} onClick={() => setze(f, "Done")}>Erledigt</button>
                        : <button type="button" className="btn ghost small" disabled={action.busy} onClick={() => setze(f, "Open")}>Wieder öffnen</button>}
                    </td>
                  </tr>
                ))}
                {list.data?.length === 0 && (
                  <tr><td colSpan={7} className="muted">{status === "Open" ? "Keine offenen Rückmeldungen." : "Keine Rückmeldungen."}</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
    </section>
  );
}
