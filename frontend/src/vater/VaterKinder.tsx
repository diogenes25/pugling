import { useState } from "react";
import { Link } from "react-router-dom";
import { StatusBanner } from "../components/StatusBanner";
import { Pflicht } from "../components/Pflicht";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { pinProblem } from "../lib/pin";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, PlanResponse } from "../lib/types";

/**
 * Die Verwaltung der Kinder (B-749): Stammdaten-Tabelle, Kind anlegen und der Weg zu den Lehrplänen. Stand bis dahin auf
 * der Startseite und schob den täglichen Blick (geübt? Tagesziel? einlösen?) unter Formulare – am Handy außer Sicht.
 */
export function VaterKinder() {
  const { session } = useAuth();
  const adultId = session!.id;

  const children = useAsync<ChildResponse[]>(() => api.children(), [adultId]);
  // Nur für die Standortbestimmung „gibt es überhaupt einen Plan?" – die Liste selbst liegt beim Zuweisen.
  const plans = useAsync<PlanResponse[]>(() => api.plans(), [adultId]);

  const [name, setName] = useState("");
  const [grade, setGrade] = useState("");
  const [pin, setPin] = useState("");
  /*
   * Der erste Bildschirm eines neuen Vaters – und bis B-54 der einzige mit einem eigenen `try/catch`. Zwei
   * Dinge hingen daran: zwei Klicks im selben Tick legten **zwei Kinder** an (kein Ref-Gate, kein
   * `disabled`), und Erfolg wie Fehler schrieben in *dieselbe* Variable, die fest als grüner Kasten
   * gerendert wurde – „Kind angelegt." und „Name schon vergeben" sahen identisch aus.
   */
  const action = useAction();

  async function addChild(e: React.FormEvent) {
    e.preventDefault();
    // Stumm (B-380, Entscheidung 4): der Knopf ist leer gesperrt, nur Enter im Feld kommt hier an.
    if (!name.trim()) return;
    const pinError = pinProblem(pin.trim());
    if (pinError) { action.fail(pinError); return; }
    // Ohne PIN legt der Server das Kind mit leerer PIN an – es kann sich dann nicht anmelden, bis der
    // Vater sie auf der Kind-Seite nachträgt. Darauf weist der Hinweis unter dem Formular hin.
    const ok = await action.run(() => api.createChild({
      name: name.trim(), pin: pin.trim() || undefined, grade: grade ? Number(grade) : null,
    }), "Kind angelegt.");
    if (!ok) return;
    setName(""); setGrade(""); setPin("");
    children.reload();
  }

  return (
    <>
      <section>
        <h2 className="h-section">Kinder</h2>
        {children.loading && children.data === null ? <div className="loading">Lade…</div> : children.error ? <div className="banner err">{children.error}</div> : (
          <div style={{ overflowX: "auto" }}>
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
                        : <Link to={`/vater/kind/${c.id}?bereich=interessen`}>Interessen pflegen →</Link>}
                    </td>
                    <td className="num">{c.coins}</td>
                    <td className="num">{c.gems}</td>
                  </tr>
                ))}
                {children.data?.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Kinder.</td></tr>}
              </tbody>
            </table>
          </div>
        )}

        <form className="form-grid" style={{ marginTop: 12, alignItems: "end" }} onSubmit={addChild}>
          <div className="field"><label htmlFor="new-child-name">Name <Pflicht /></label><input id="new-child-name" name="name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Vorname" /></div>
          <div className="field"><label htmlFor="new-child-grade">Klasse</label><input id="new-child-grade" name="grade" type="number" min={1} max={13} value={grade} onChange={(e) => setGrade(e.target.value)} placeholder="z.B. 8" /></div>
          <div className="field"><label htmlFor="new-child-pin">PIN</label><input id="new-child-pin" name="pin" value={pin} onChange={(e) => setPin(e.target.value)} placeholder="z.B. 1111" /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy || !name.trim()}>
            {action.busy ? "Lege an…" : "Kind anlegen"}
          </button>
        </form>
        <p className="sub" style={{ marginTop: 8 }}>
          Die <strong>PIN ist der Login deines Kindes</strong> – ohne sie kommt es nicht in seine App
          (nachtragen kannst du sie auf der Kind-Seite). Der <strong>Lehrplan-Assistent</strong> führt
          danach Schritt für Schritt durch Problemfeld und passende Übungen.
        </p>
        <StatusBanner message={action.message} style={{ marginTop: 10 }} />
      </section>

      {/*
        Die Lehrpläne liegen beim Zuweisen (docs/vater-perspektiven-plan.md). Was hier bleibt, ist der Weg dorthin – mit
        der Zahl, damit die Seite nicht verschweigt, ob überhaupt ein Plan existiert.
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
