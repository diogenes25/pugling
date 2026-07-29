import { useState } from "react";
import { Link } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, ChildrenDashboard, PlanResponse } from "../lib/types";

export function VaterDashboard() {
  const { session } = useAuth();
  const adultId = session!.id;

  const children = useAsync<ChildResponse[]>(() => api.children(), [adultId]);
  // Nur für die Standortbestimmung „gibt es überhaupt einen Plan?" – die Liste selbst liegt beim Zuweisen.
  const plans = useAsync<PlanResponse[]>(() => api.plans(), [adultId]);
  const today = useAsync<ChildrenDashboard>(() => api.childrenDaily(), [adultId]);

  const [name, setName] = useState("");
  const [grade, setGrade] = useState("");
  const [pin, setPin] = useState("");
  const [msg, setMsg] = useState<string | null>(null);

  async function addChild(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    try {
      // Ohne PIN legt der Server das Kind mit leerer PIN an – es kann sich dann nicht anmelden, bis der
      // Vater sie auf der Kind-Seite nachträgt. Darauf weist der Hinweis unter dem Formular hin.
      await api.createChild({ name: name.trim(), pin: pin.trim() || undefined, grade: grade ? Number(grade) : null });
      setName(""); setGrade(""); setPin("");
      setMsg("Kind angelegt.");
      children.reload();
    } catch (err) {
      setMsg(errorMessage(err));
    }
  }


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
            <thead><tr><th>Id</th><th>Name</th><th>Klasse</th><th>Schulart</th><th>Interessen</th><th className="num">🪙</th><th className="num">💎</th></tr></thead>
            <tbody>
              {children.data?.map((c) => (
                <tr key={c.id}>
                  <td className="num">{c.id}</td>
                  {/* Das Profil steuert, welches Bild das Kind zu einer Vokabel sieht – deshalb hier verlinkt. */}
                  <td><Link to={`/vater/kind/${c.id}`}>{c.name}</Link></td>
                  <td>{c.grade ? `${c.grade}.` : "–"}</td>
                  <td className="muted">{c.schoolType && c.schoolType !== "None" ? c.schoolType : "–"}</td>
                  <td className="muted">
                    {c.interests.length > 0
                      ? c.interests.join(", ")
                      : <Link to={`/vater/kind/${c.id}`}>Interessen pflegen →</Link>}
                  </td>
                  <td className="num">{c.coins}</td>
                  <td className="num">{c.gems}</td>
                </tr>
              ))}
              {children.data?.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Kinder.</td></tr>}
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
          Die <strong>PIN ist der Login deines Kindes</strong> – ohne sie kommt es nicht in seine App
          (nachtragen kannst du sie auf der Kind-Seite). Der <strong>Lehrplan-Assistent</strong> führt
          danach Schritt für Schritt durch Problemfeld und passende Übungen.
        </p>
        {msg && <div className="banner ok" style={{ marginTop: 10 }} role="status" aria-live="polite">{msg}</div>}
      </section>

      {/*
        Die Lehrpläne sind hier bewusst **weg**: das Zuweisen ist eine eigene Perspektive und stand vorher
        als Anhängsel unter zwei Betreuungs-Tabellen (docs/vater-perspektiven-plan.md). Was bleibt, ist der
        Weg dorthin – mit der Zahl, damit die Seite nicht verschweigt, ob überhaupt ein Plan existiert.
      */}
      <section className="card">
        <div className="row" style={{ alignItems: "center", gap: 10 }}>
          <div>
            <h3 style={{ margin: 0 }}>🎯 Lehrpläne</h3>
            <p className="muted" style={{ margin: "4px 0 0", fontSize: 13 }}>
              {plans.loading ? "Lade…"
                : plans.data?.length === 0 ? "Noch kein Plan angelegt – ohne Plan hat dein Kind keine Pflicht."
                : `${plans.data?.length} ${plans.data?.length === 1 ? "Plan" : "Pläne"}, davon ${plans.data?.filter((p) => p.active).length} aktiv.`}
            </p>
          </div>
          <Link to="/vater/plaene" className="btn inline-btn"
            style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>
            Zum Zuweisen
          </Link>
        </div>
      </section>
    </>
  );
}
