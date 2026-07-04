import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, PlanResponse } from "../lib/types";

export function VaterDashboard() {
  const { session } = useAuth();
  const fatherId = session!.id;
  const nav = useNavigate();

  const children = useAsync<ChildResponse[]>(() => api.children(fatherId), [fatherId]);
  const plans = useAsync<PlanResponse[]>(() => api.plans(), []);

  const [name, setName] = useState("");
  const [pin, setPin] = useState("");
  const [msg, setMsg] = useState<string | null>(null);

  async function addChild(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      await api.createChild(fatherId, name.trim(), pin);
      setName(""); setPin("");
      setMsg("Kind angelegt.");
      children.reload();
    } catch (err) {
      setMsg(err instanceof Error ? err.message : "Fehler");
    }
  }

  const childName = (id: number) => children.data?.find((c) => c.id === id)?.name ?? `#${id}`;

  return (
    <>
      <section>
        <h2 className="h-section">Kinder</h2>
        {children.loading ? <div className="loading">Lade…</div> : children.error ? <div className="banner err">{children.error}</div> : (
          <table className="table">
            <thead><tr><th>Id</th><th>Name</th><th>Geb.-Jahr</th><th className="num">Punkte</th></tr></thead>
            <tbody>
              {children.data?.map((c) => (
                <tr key={c.id}>
                  <td className="num">{c.id}</td><td>{c.name}</td><td>{c.birthYear ?? "–"}</td>
                  <td className="num">{c.pointsBalance}</td>
                </tr>
              ))}
              {children.data?.length === 0 && <tr><td colSpan={4} className="muted">Noch keine Kinder.</td></tr>}
            </tbody>
          </table>
        )}

        <form className="form-grid" style={{ marginTop: 12, alignItems: "end" }} onSubmit={addChild}>
          <div className="field"><label>Name</label><input value={name} onChange={(e) => setName(e.target.value)} placeholder="Vorname" /></div>
          <div className="field"><label>PIN</label><input value={pin} onChange={(e) => setPin(e.target.value)} placeholder="z.B. 1111" /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }}>Kind anlegen</button>
        </form>
        {msg && <div className="banner ok" style={{ marginTop: 10 }}>{msg}</div>}
      </section>

      <section>
        <div className="row">
          <h2 className="h-section">Lehrpläne</h2>
          <button type="button" className="btn inline-btn" style={{ width: "auto", marginLeft: "auto" }} onClick={() => nav("/vater/plan/new")}>+ Neuer Plan</button>
        </div>
        {plans.loading ? <div className="loading">Lade…</div> : plans.error ? <div className="banner err">{plans.error}</div> : (
          <table className="table">
            <thead><tr><th>Titel</th><th>Kind</th><th>Verfahren</th><th className="num">Inhalte</th><th>Zeitraum</th><th>Status</th></tr></thead>
            <tbody>
              {plans.data?.map((p) => (
                <tr key={p.id} className="clickable" onClick={() => nav(`/vater/plan/${p.id}`)}>
                  <td><Link to={`/vater/plan/${p.id}`}>{p.title}</Link></td>
                  <td>{childName(p.childId)}</td>
                  <td>{p.method}{p.useLeitner ? " · Leitner" : ""}</td>
                  <td className="num">{p.items.length}</td>
                  <td className="muted">{p.startDate} – {p.endDate}</td>
                  <td>{p.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                </tr>
              ))}
              {plans.data?.length === 0 && <tr><td colSpan={6} className="muted">Noch keine Pläne. Lege einen an.</td></tr>}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}
