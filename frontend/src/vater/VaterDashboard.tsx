import { useState } from "react";
import { Link } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, ChildrenDashboard, PlanResponse } from "../lib/types";

export function VaterDashboard() {
  const { session } = useAuth();
  const fatherId = session!.id;

  const children = useAsync<ChildResponse[]>(() => api.children(), [fatherId]);
  const plans = useAsync<PlanResponse[]>(() => api.plans(), []);
  const today = useAsync<ChildrenDashboard>(() => api.childrenDaily(), [fatherId]);

  const [name, setName] = useState("");
  const [grade, setGrade] = useState("");
  const [pin, setPin] = useState("");
  const [msg, setMsg] = useState<string | null>(null);

  async function addChild(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      await api.createChild({ name: name.trim(), pin, grade: grade ? Number(grade) : null });
      setName(""); setGrade(""); setPin("");
      setMsg("Kind angelegt.");
      children.reload();
    } catch (err) {
      setMsg(errorMessage(err));
    }
  }

  const childName = (id: number) => children.data?.find((c) => c.id === id)?.name ?? `#${id}`;

  return (
    <>
      <section>
        <h2 className="h-section">Heute</h2>
        {today.loading ? <div className="loading">Lade…</div> : today.error ? <div className="banner err">{today.error}</div> : (
          <table className="table">
            <thead><tr><th>Kind</th><th>Status</th><th className="num">Ziele</th><th className="num">Punkte heute</th></tr></thead>
            <tbody>
              {today.data?.children.map((c) => (
                <tr key={c.childId}>
                  <td>{c.name}</td>
                  <td>
                    {c.goalsTotal === 0 ? <span className="pill">kein Tagesziel</span>
                      : c.dutyDone ? <span className="pill lime">✓ geschafft</span>
                      : c.practiced ? <span className="pill">dran</span>
                      : <span className="pill mag">offen</span>}
                  </td>
                  <td className="num">{c.goalsMet} / {c.goalsTotal}</td>
                  <td className="num">{c.pointsToday}</td>
                </tr>
              ))}
              {today.data?.children.length === 0 && <tr><td colSpan={4} className="muted">Noch keine Kinder.</td></tr>}
            </tbody>
          </table>
        )}
      </section>

      <section>
        <h2 className="h-section">Kinder</h2>
        {children.loading ? <div className="loading">Lade…</div> : children.error ? <div className="banner err">{children.error}</div> : (
          <table className="table">
            <thead><tr><th>Id</th><th>Name</th><th>Klasse</th><th>Schulart</th><th className="num">🪙</th><th className="num">💎</th></tr></thead>
            <tbody>
              {children.data?.map((c) => (
                <tr key={c.id}>
                  <td className="num">{c.id}</td><td>{c.name}</td>
                  <td>{c.grade ? `${c.grade}.` : "–"}</td>
                  <td className="muted">{c.schoolType && c.schoolType !== "None" ? c.schoolType : "–"}</td>
                  <td className="num">{c.coins}</td>
                  <td className="num">{c.gems}</td>
                </tr>
              ))}
              {children.data?.length === 0 && <tr><td colSpan={6} className="muted">Noch keine Kinder.</td></tr>}
            </tbody>
          </table>
        )}

        <form className="form-grid" style={{ marginTop: 12, alignItems: "end" }} onSubmit={addChild}>
          <div className="field"><label htmlFor="new-child-name">Name</label><input id="new-child-name" name="name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Vorname" /></div>
          <div className="field"><label htmlFor="new-child-grade">Klasse</label><input id="new-child-grade" name="grade" type="number" min={1} max={13} value={grade} onChange={(e) => setGrade(e.target.value)} placeholder="z.B. 8" /></div>
          <div className="field"><label htmlFor="new-child-pin">PIN</label><input id="new-child-pin" name="pin" value={pin} onChange={(e) => setPin(e.target.value)} placeholder="z.B. 1111" /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }}>Kind anlegen</button>
        </form>
        <p className="sub" style={{ marginTop: 8 }}>
          Tipp: Der <strong>Lehrplan-Assistent</strong> führt Schritt für Schritt durch Kind, Problemfeld und passende Übungen.
        </p>
        {msg && <div className="banner ok" style={{ marginTop: 10 }} role="status" aria-live="polite">{msg}</div>}
      </section>

      <section>
        <div className="row">
          <h2 className="h-section">Lehrpläne</h2>
          <Link to="/vater/plan/new" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>+ Neuer Plan</Link>
        </div>
        {plans.loading ? <div className="loading">Lade…</div> : plans.error ? <div className="banner err">{plans.error}</div> : (
          <table className="table">
            <thead><tr><th>Titel</th><th>Kind</th><th className="num">Übungen</th><th>Zeitraum</th><th>Status</th></tr></thead>
            <tbody>
              {plans.data?.map((p) => (
                <tr key={p.id}>
                  <td><Link to={`/vater/plan/${p.id}`}>{p.title}</Link></td>
                  <td>{childName(p.childId)}</td>
                  <td className="num">{p.positionCount}</td>
                  <td className="muted">{p.startDate} – {p.endDate}</td>
                  <td>{p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                </tr>
              ))}
              {plans.data?.length === 0 && <tr><td colSpan={5} className="muted">Noch keine Pläne. Lege einen an.</td></tr>}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}
