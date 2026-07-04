import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { ChildResponse, CreatePlanDto, VocabularyResponse } from "../lib/types";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function VaterPlanCreate() {
  const { session } = useAuth();
  const nav = useNavigate();
  const children = useAsync<ChildResponse[]>(() => api.children(session!.id), [session!.id]);
  const vocab = useAsync<VocabularyResponse[]>(() => api.vocabulary(), []);

  const [title, setTitle] = useState("Englisch – Unit 1");
  const [childId, setChildId] = useState<number | "">("");
  const [durationDays, setDurationDays] = useState(10);
  const [startDate, setStartDate] = useState(todayIso());
  const [newItemsPerLesson, setNewItemsPerLesson] = useState(5);
  const [dailyMinutes, setDailyMinutes] = useState(15);
  const [passPercent, setPassPercent] = useState(80);
  const [useLeitner, setUseLeitner] = useState(true);
  const [requireTyped, setRequireTyped] = useState(false);
  const [defaultStage, setDefaultStage] = useState(2); // SelfAssess
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Erstes Kind vorwählen, sobald geladen.
  useEffect(() => {
    if (childId === "" && children.data && children.data.length > 0) setChildId(children.data[0].id);
  }, [children.data, childId]);

  const filtered = useMemo(() => {
    const v = vocab.data ?? [];
    if (!search.trim()) return v;
    const s = search.toLowerCase();
    return v.filter((x) => x.word.toLowerCase().includes(s) || x.translation.toLowerCase().includes(s) || x.key.toLowerCase().includes(s));
  }, [vocab.data, search]);

  function toggle(key: string) {
    setSelected((sel) => (sel.includes(key) ? sel.filter((k) => k !== key) : [...sel, key]));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!childId) { setError("Bitte ein Kind wählen."); return; }
    if (selected.length === 0) { setError("Bitte mindestens eine Vokabel wählen."); return; }
    setBusy(true);
    const dto: CreatePlanDto = {
      childId: Number(childId), title: title.trim(), method: "Vocabulary",
      durationDays, startDate, newItemsPerLesson, contentKeys: selected,
      dailyMinutesRequired: dailyMinutes, dailyTestPassPercent: passPercent,
      defaultStage, requireTypedTest: requireTyped, useLeitner,
    };
    try {
      const plan = await api.createPlan(dto);
      nav(`/vater/plan/${plan.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Plan konnte nicht erstellt werden.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 18 }}>
      <h2 className="h-section">Neuer Lehrplan (Vokabeln)</h2>

      <section className="card">
        <div className="form-grid">
          <div className="field"><label>Titel</label><input value={title} onChange={(e) => setTitle(e.target.value)} /></div>
          <div className="field"><label>Kind</label>
            <select aria-label="Kind" value={childId} onChange={(e) => setChildId(Number(e.target.value))}>
              {children.data?.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
            </select>
          </div>
          <div className="field"><label>Start</label><input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></div>
          <div className="field"><label>Dauer (Tage)</label><input type="number" min={1} value={durationDays} onChange={(e) => setDurationDays(Number(e.target.value))} /></div>
          <div className="field"><label>Neue Wörter/Tag</label><input type="number" min={1} value={newItemsPerLesson} onChange={(e) => setNewItemsPerLesson(Number(e.target.value))} /></div>
          <div className="field"><label>Minuten/Tag</label><input type="number" min={1} value={dailyMinutes} onChange={(e) => setDailyMinutes(Number(e.target.value))} /></div>
          <div className="field"><label>Bestehen ab %</label><input type="number" min={1} max={100} value={passPercent} onChange={(e) => setPassPercent(Number(e.target.value))} /></div>
          <div className="field"><label>Test-Stufe</label>
            <select aria-label="Test-Stufe" value={defaultStage} onChange={(e) => setDefaultStage(Number(e.target.value))}>
              <option value={1}>1 · Zeigen</option>
              <option value={2}>2 · Selbstcheck</option>
              <option value={3}>3 · Buchstaben</option>
              <option value={4}>4 · Tippen</option>
              <option value={5}>5 · Hören</option>
            </select>
          </div>
        </div>
        <div className="row" style={{ marginTop: 12, gap: 20, flexWrap: "wrap" }}>
          <label className="checkline"><input type="checkbox" checked={useLeitner} onChange={(e) => setUseLeitner(e.target.checked)} /> Leitner-Kasten (Übungspunkte)</label>
          <label className="checkline"><input type="checkbox" checked={requireTyped} onChange={(e) => setRequireTyped(e.target.checked)} /> Nur getippte Tests zählen</label>
        </div>
      </section>

      <section className="card">
        <div className="row">
          <h3 style={{ margin: 0 }}>Vokabeln wählen <span className="muted">({selected.length} gewählt)</span></h3>
          <input style={{ marginLeft: "auto", maxWidth: 220 }} placeholder="Suchen…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Vokabel suchen" />
        </div>

        {selected.length > 0 && (
          <div className="tokenlist" style={{ margin: "10px 0" }}>
            {selected.map((k) => {
              const v = vocab.data?.find((x) => x.key === k);
              return <span className="token" key={k}>{v ? `${v.word}→${v.translation}` : k}<button type="button" onClick={() => toggle(k)}>×</button></span>;
            })}
          </div>
        )}

        {vocab.loading ? <div className="loading">Lade Vokabeln…</div> : (vocab.data?.length ?? 0) === 0 ? (
          <div className="banner err">Der Vokabel-Store ist leer. Lege zuerst unter „Vokabeln" Einträge an.</div>
        ) : (
          <div style={{ maxHeight: 320, overflowY: "auto", display: "grid", gridTemplateColumns: "repeat(auto-fill,minmax(200px,1fr))", gap: 6 }}>
            {filtered.map((v) => (
              <label key={v.id} className="checkline" style={{ padding: 6, border: "1px solid var(--stroke)", borderRadius: 8 }}>
                <input type="checkbox" checked={selected.includes(v.key)} onChange={() => toggle(v.key)} />
                <span>{v.word} <span className="muted">→ {v.translation}</span></span>
              </label>
            ))}
          </div>
        )}
      </section>

      {error && <div className="banner err">{error}</div>}
      <button type="submit" className="btn" style={{ width: "auto", alignSelf: "flex-start" }} disabled={busy}>
        {busy ? "…" : "Lehrplan erstellen"}
      </button>
    </form>
  );
}
