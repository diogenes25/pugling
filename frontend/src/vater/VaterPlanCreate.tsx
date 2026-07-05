import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, CreatePlanDto } from "../lib/types";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Lehrplan anlegen = reinen Container erstellen. Der Plan trägt nur Titel, Kind und Laufzeit; welche
 * Übungen mit welchem Ziel/Punkten/Stufe gelernt werden, stellt der Vater danach auf der Plan-Seite als
 * Positionen zusammen (jede Position bringt ihre eigenen Werte mit). Kein plan-weites Verfahren mehr.
 */
export function VaterPlanCreate() {
  const { session } = useAuth();
  const nav = useNavigate();
  const children = useAsync<ChildResponse[]>(() => api.children(), [session!.id]);

  const [title, setTitle] = useState("Englisch – Unit 1");
  const [childId, setChildId] = useState<number | "">("");
  const [durationDays, setDurationDays] = useState(10);
  const [startDate, setStartDate] = useState(todayIso());
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Erstes Kind vorwählen, sobald geladen.
  useEffect(() => {
    if (childId === "" && children.data && children.data.length > 0) setChildId(children.data[0].id);
  }, [children.data, childId]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!childId) { setError("Bitte ein Kind wählen."); return; }
    setBusy(true);
    const dto: CreatePlanDto = { childId: Number(childId), title: title.trim(), durationDays, startDate };
    try {
      const plan = await api.createPlan(dto);
      nav(`/vater/plan/${plan.id}`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <h2 className="h-section">Neuer Lehrplan</h2>
      <p className="muted" style={{ marginTop: -8 }}>
        Legt einen leeren Plan an. Danach fügst du auf der Plan-Seite beliebige Übungen als Positionen
        hinzu – Ziel (Tag/Woche), Punkte, Stufe und Leitner stellst du je Übung ein.
      </p>

      <section className="card">
        <div className="form-grid">
          <div className="field"><label>Titel</label><input title="Titel" value={title} onChange={(e) => setTitle(e.target.value)} /></div>
          <div className="field"><label>Kind</label>
            <select aria-label="Kind" value={childId} onChange={(e) => setChildId(Number(e.target.value))}>
              {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
            </select>
          </div>
          <div className="field"><label>Start</label><input title="Startdatum" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
          <div className="field"><label>Dauer (Tage)</label><input title="Dauer in Tagen" type="number" min={1} value={durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} /></div>
        </div>
      </section>

      {error && <div className="banner err">{error}</div>}
      <button type="submit" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} disabled={busy}>
        {busy ? "…" : "Plan anlegen & Übungen hinzufügen"}
      </button>
    </form>
  );
}
