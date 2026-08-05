import { Link, useSearchParams } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { ChildResponse, PlanResponse } from "../lib/types";

/**
 * Startseite der Perspektive **Zuweisen**: welches Kind lernt welchen Stoff.
 *
 * Der Abschnitt stand vorher unten auf dem Dashboard, zwischen „Heute" und dem Kind-Formular. Damit lag
 * die einzige Stelle, an der Creator- und Supervisor-Arbeit sich treffen, als Anhängsel unter zwei
 * Betreuungs-Tabellen – siehe docs/vater-perspektiven-plan.md.
 *
 * `?childId=` schränkt auf ein Kind ein (der Kind-Hub verlinkt so hierher).
 */
export function VaterPlaene() {
  const [params] = useSearchParams();
  const filterChildId = Number(params.get("childId")) || null;

  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const plans = useAsync<PlanResponse[]>(() => api.plans(filterChildId ?? undefined), [filterChildId]);

  const childName = (id: number) => children.data?.find((c) => c.id === id)?.name ?? `#${id}`;
  // Genau ein aktiver, laufender Plan je Kind ist spielbar (Anti-Cheat, server-seitig erzwungen). Darum
  // ist „zwei aktive Pläne für ein Kind" ein Zustand, den der Vater sehen muss, statt ihn beim Sohn zu
  // entdecken – die Liste hebt ihn hervor.
  const activeCountByChild = new Map<number, number>();
  for (const p of plans.data ?? []) {
    if (p.active) activeCountByChild.set(p.childId, (activeCountByChild.get(p.childId) ?? 0) + 1);
  }
  const hasPlans = (plans.data?.length ?? 0) > 0;

  return (
    <>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <h2 className="h-section">Lehrpläne{filterChildId ? ` · ${childName(filterChildId)}` : ""}</h2>
        {filterChildId && (
          <Link to="/vater/plaene" className="btn ghost small" style={{ width: "auto", textDecoration: "none" }}>
            alle Kinder
          </Link>
        )}
        {/* Das gefilterte Kind reist mit: sonst stand im Formular das *erste* Kind, und wer für Kind 2
            einen Plan anlegte, baute ihn stillschweigend für Kind 1. */}
        <Link to={`/vater/plan/new${filterChildId ? `?childId=${filterChildId}` : ""}`} className="btn inline-btn"
          style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>
          + Neuer Plan
        </Link>
      </div>
      <p className="sub">
        Ein Lehrplan verbindet <strong>fertige Übungen</strong> mit einem <strong>Kind</strong> und macht
        daraus eine Pflicht: Rhythmus, Bestehensgrenze, Punkte – und optional den Münz-Malus, wenn die
        Pflicht reißt.
      </p>

      {/*
        Der geführte Weg: er stellt die Fragen in der Reihenfolge, in der man sie beantworten kann.
        Die Überschrift richtet sich nach dem Bestand – „Zum ersten Mal hier?" über einer Liste mit neun
        Plänen wäre schlicht falsch und macht den Rest der Seite unglaubwürdig.
      */}
      <section className="card">
        <div className="row" style={{ alignItems: "center", gap: 10 }}>
          <div>
            <h3 style={{ margin: 0 }}>🧭 {hasPlans ? "Geführt statt von Hand" : "Zum ersten Mal hier?"}</h3>
            <p className="muted" style={{ margin: "4px 0 0", fontSize: 13 }}>
              Der Assistent führt von „woran hakt es" über passende Übungen bis zum fertigen Plan mit Zielen.
            </p>
          </div>
          <Link to="/vater/wizard" className="btn inline-btn"
            style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>
            Assistent starten
          </Link>
        </div>
      </section>

      {plans.loading && plans.data === null ? <div className="loading">Lade…</div> : plans.error ? <div className="banner err">{plans.error}</div> : (
        <table className="table">
          <thead><tr><th>Titel</th><th>Kind</th><th className="num">Übungen</th><th>Zeitraum</th><th>Status</th></tr></thead>
          <tbody>
            {plans.data?.map((p) => (
              <tr key={p.id}>
                <td><Link to={`/vater/plan/${p.id}`}>{p.title}</Link></td>
                <td>{childName(p.childId)}</td>
                <td className="num">{p.positionCount}</td>
                <td className="muted">{p.startDate} – {p.endDate}</td>
                <td>
                  {p.active
                    ? <span className="pill lime">aktiv</span>
                    : <span className="pill">inaktiv</span>}
                  {p.active && (activeCountByChild.get(p.childId) ?? 0) > 1 && (
                    <span className="pill mag" style={{ marginLeft: 6 }}
                      title="Nur ein aktiver, laufender Plan je Kind ist spielbar – die anderen bleiben liegen.">
                      mehrfach aktiv
                    </span>
                  )}
                </td>
              </tr>
            ))}
            {plans.data?.length === 0 && (
              <tr><td colSpan={5} className="muted">
                Noch keine Pläne. Starte den Assistenten oder lege einen leeren Plan an.
              </td></tr>
            )}
          </tbody>
        </table>
      )}

      {children.data?.length === 0 && (
        <div className="banner">
          Ein Plan braucht ein Kind. Lege zuerst eines unter <Link to="/vater">Betreuen</Link> an.
        </div>
      )}
    </>
  );
}
