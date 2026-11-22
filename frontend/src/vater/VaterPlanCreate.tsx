import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { FieldLabel } from "../components/InfoHint";
import { Pflicht } from "../components/Pflicht";
import { ListenAusfall } from "../components/ListenAusfall";
import { useAsync } from "../lib/useAsync";
import { heuteImLerntag } from "../lib/datum";
import { useAuth } from "../lib/auth";
import type { ChildResponse, CreatePlanDto, SubjectResponse } from "../lib/types";
import { PlanUnitSelect } from "./PlanUnitSelect";

// Der Lerntag des Servers, nicht der UTC-Tag: um 00:30 deutscher Zeit ist schon morgen (B-730).
function todayIso(): string {
  return heuteImLerntag();
}

/**
 * Lehrplan anlegen = reinen Container erstellen. Der Plan trägt Titel, Kind, Fach (Pflicht, B-539 – je Fach ist ein
 * Plan des Kindes aktiv), optional eine Unit, und die Laufzeit; welche
 * Übungen mit welchem Ziel/Punkten/Stufe gelernt werden, stellt der Vater danach auf der Plan-Seite als
 * Positionen zusammen (jede Position bringt ihre eigenen Werte mit). Kein plan-weites Verfahren mehr.
 */
export function VaterPlanCreate() {
  const { session } = useAuth();
  const nav = useNavigate();
  const [params] = useSearchParams();
  const children = useAsync<ChildResponse[]>(() => api.children(), [session!.id]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  // `?subjectId=` reist mit wie das Kind (Auswahl als Query).
  const [subjectId, setSubjectId] = useState<number | "">(Number(params.get("subjectId")) || "");
  const [seriesUnitId, setSeriesUnitId] = useState<number | "">("");

  const [title, setTitle] = useState("Englisch – Unit 1");
  const [description, setDescription] = useState("");
  // `?childId=` kommt aus der gefilterten Plan-Liste bzw. dem Kind-Hub. Ohne diese Übernahme stand hier
  // stets das *erste* Kind – wer für Kind 2 einen Plan anlegte, baute ihn stillschweigend für Kind 1.
  const [childId, setChildId] = useState<number | "">(Number(params.get("childId")) || "");
  const [durationDays, setDurationDays] = useState(10);
  const [startDate, setStartDate] = useState(todayIso());
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Erstes Kind vorwählen, sobald geladen – nur, wenn die Route keines mitgebracht hat.
  useEffect(() => {
    if (childId === "" && children.data && children.data.length > 0) setChildId(children.data[0].id);
  }, [children.data, childId]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!childId) { setError("Bitte ein Kind wählen."); return; }
    // Pflichtfeld: der Knopf ist ohne Fach gesperrt, die Zeile kehrt nur stumm zurück (B-233).
    if (subjectId === "") return;
    setBusy(true);
    const dto: CreatePlanDto = {
      childId: Number(childId), title: title.trim(), description: description.trim() || null, durationDays, startDate,
      subjectId, seriesUnitId: seriesUnitId === "" ? null : seriesUnitId,
    };
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
          <div className="field"><label htmlFor="plan-title">Titel</label><input id="plan-title" title="Titel" value={title} onChange={(e) => setTitle(e.target.value)} /></div>
          <div className="field"><label>Kind</label>
            <select aria-label="Kind" value={childId} onChange={(e) => setChildId(Number(e.target.value))}>
              {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
            </select>
          </div>
          <div className="field"><FieldLabel htmlFor="plan-subject" topic="planSubject">Fach <Pflicht /></FieldLabel>
            <select id="plan-subject" value={subjectId} onChange={(e) => {
              // Die Unit gehört zum alten Fach – stehen gelassen wiese der Server die Kombination ab.
              setSeriesUnitId("");
              setSubjectId(e.target.value === "" ? "" : Number(e.target.value));
            }}>
              <option value="">– Fach wählen –</option>
              {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
            <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />
          </div>
          <div className="field"><FieldLabel htmlFor="plan-unit" topic="planUnit">Unit <span className="muted">(optional)</span></FieldLabel>
            <PlanUnitSelect id="plan-unit" subjectId={subjectId} value={seriesUnitId} onChange={setSeriesUnitId} />
          </div>
          <div className="field"><label htmlFor="plan-start">Start</label><input id="plan-start" title="Startdatum" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
          <div className="field"><FieldLabel htmlFor="plan-duration" topic="planDuration">Dauer (Tage)</FieldLabel><input id="plan-duration" title="Dauer in Tagen" type="number" min={1} value={durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} /></div>
        </div>
        <div className="field" style={{ marginTop: 10 }}>
          <label htmlFor="plan-description">Beschreibung <span className="muted">(optional)</span></label>
          <textarea id="plan-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2}
            placeholder="Ziel/Umfang des Plans – hilft beim Wiederfinden." />
        </div>
      </section>

      <div className={`live-region${error ? " banner err" : ""}`} role="status" aria-live="polite">{error}</div>
      <button type="submit" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} disabled={busy || subjectId === ""}>
        {busy ? "…" : "Plan anlegen & Übungen hinzufügen"}
      </button>
    </form>
  );
}
