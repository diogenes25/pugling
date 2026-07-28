import { useId, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel } from "../components/InfoHint";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useChildSelection } from "../lib/useChildSelection";
import { confirmAction } from "../lib/ui";
import type {
  AchievementDef, ChildResponse, CreateAchievementDto, CreateMissionDto,
  MissionDef, MissionPeriod, ProgressMetric,
} from "../lib/types";

/** Metriken mit deutschen Labels – dieselben, die der Server (ProgressMetric) auswertet. */
const METRICS: { value: ProgressMetric; label: string }[] = [
  { value: "NewWords", label: "Neue Wörter" },
  { value: "CorrectReviews", label: "Richtige Wiederholungen" },
  { value: "TestsPassed", label: "Bestandene Tests" },
  { value: "MinutesPracticed", label: "Übungsminuten" },
  { value: "DaysComplete", label: "Komplette Tage" },
  { value: "StreakDays", label: "Streak-Tage" },
];
const metricLabel = (m: ProgressMetric) => METRICS.find((x) => x.value === m)?.label ?? m;

const PERIODS: { value: MissionPeriod; label: string }[] = [
  { value: "Daily", label: "Täglich" },
  { value: "Weekly", label: "Wöchentlich" },
  { value: "OneOff", label: "Einmalig" },
];
const periodLabel = (p: MissionPeriod) => PERIODS.find((x) => x.value === p)?.label ?? p;

export function VaterRewards() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  // Vorauswahl aus `?childId=` (die Links vom Kind-Hub tragen sie), sonst das erste Kind.
  const { activeChild, select } = useChildSelection(children.data);

  return (
    <>
      <section>
        <h2 className="h-section">Belohnungen</h2>
        <p className="muted">Setze Ziele, für die dein Kind beim Lernen 💎 Gems verdient – die es dann im
          Skins-Shop für Charaktere ausgibt. <b>Missionen</b> sind zeitgebundene Ziele (täglich/wöchentlich),
          <b> Auszeichnungen</b> sind dauerhafte Meilensteine. (Echte Belohnungen laufen über den 🛒 Familien-Shop.)</p>
        {children.loading ? <div className="loading">Lade…</div>
          : children.error ? <div className="banner err">{children.error}</div>
          : children.data && children.data.length > 0 ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label htmlFor="rewards-child">Kind</label>
              <select id="rewards-child" value={activeChild ?? ""} onChange={(e) => select(Number(e.target.value))}>
                {children.data.map((c) => (
                  <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>
                ))}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}
      </section>

      {activeChild !== null && <MissionManager key={`m${activeChild}`} childId={activeChild} />}
      {activeChild !== null && <AchievementManager key={`a${activeChild}`} childId={activeChild} />}
    </>
  );
}

function MissionManager({ childId }: { childId: number }) {
  const uid = useId();
  const list = useAsync<MissionDef[]>(() => api.missionsFor(childId), [childId]);
  const [form, setForm] = useState<CreateMissionDto>({
    title: "", metric: "CorrectReviews", target: 10, period: "Daily", rewardPoints: 15,
  });
  const action = useAction();

  function up<K extends keyof CreateMissionDto>(k: K, v: CreateMissionDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const title = form.title.trim();
    if (!title) { action.fail("Titel nötig."); return; }
    if (form.target <= 0) { action.fail("Ziel muss positiv sein."); return; }
    if (!await action.run(() => api.createMission(childId, { ...form, title }), `Mission „${title}" angelegt.`)) return;
    setForm((f) => ({ ...f, title: "" }));
    list.reload();
  }

  async function toggle(m: MissionDef) {
    if (await action.run(() => api.updateMission(childId, m.id, { active: !m.active }))) list.reload();
  }
  async function remove(m: MissionDef) {
    if (!confirmAction("Diese Mission wirklich löschen?")) return;
    if (await action.run(() => api.deleteMission(childId, m.id))) list.reload();
  }

  return (
    <section>
      <h2 className="h-section">Missionen {list.data ? `(${list.data.length})` : ""}</h2>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Tagesziel: 10 richtig" /></div>
        <div className="field"><FieldLabel htmlFor={`${uid}-metric`} topic="missionMetric">Ziel-Metrik</FieldLabel>
          <select id={`${uid}-metric`} value={form.metric} onChange={(e) => up("metric", e.target.value as ProgressMetric)}>
            {METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-target`} topic="missionTarget">Zielwert</FieldLabel>
          <input id={`${uid}-target`} type="number" min={1} value={form.target} onChange={(e) => up("target", Number(e.target.value))} /></div>
        <div className="field"><FieldLabel htmlFor={`${uid}-period`} topic="missionPeriod">Zeitraum</FieldLabel>
          <select id={`${uid}-period`} value={form.period} onChange={(e) => up("period", e.target.value as MissionPeriod)}>
            {PERIODS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 120 }}><FieldLabel htmlFor={`${uid}-reward`} topic="missionReward">Belohnung 💎</FieldLabel>
          <input id={`${uid}-reward`} type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Lege an…" : "Anlegen"}
        </button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Titel</th><th>Ziel</th><th>Zeitraum</th><th>Belohnung</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((m) => (
                <tr key={m.id} style={{ opacity: m.active ? 1 : 0.55 }}>
                  <td>{m.title}</td>
                  <td className="muted">{m.target}× {metricLabel(m.metric)}</td>
                  <td>{periodLabel(m.period)}</td>
                  <td>💎 {m.rewardPoints}</td>
                  <td>{m.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => toggle(m)}>
                      {m.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => remove(m)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.length === 0 && <tr><td colSpan={6} className="muted">Noch keine Missionen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function AchievementManager({ childId }: { childId: number }) {
  const uid = useId();
  const list = useAsync<AchievementDef[]>(() => api.achievementsFor(childId), [childId]);
  const [form, setForm] = useState<CreateAchievementDto>({
    title: "", icon: "🏆", metric: "TestsPassed", threshold: 5, rewardPoints: 40,
  });
  const action = useAction();

  function up<K extends keyof CreateAchievementDto>(k: K, v: CreateAchievementDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const title = form.title.trim();
    if (!title) { action.fail("Titel nötig."); return; }
    if (form.threshold <= 0) { action.fail("Schwelle muss positiv sein."); return; }
    const ok = await action.run(
      () => api.createAchievement(childId, { ...form, title, icon: form.icon?.trim() || null }),
      `Auszeichnung „${title}" angelegt.`);
    if (!ok) return;
    setForm((f) => ({ ...f, title: "" }));
    list.reload();
  }

  async function toggle(a: AchievementDef) {
    if (await action.run(() => api.updateAchievement(childId, a.id, { active: !a.active }))) list.reload();
  }
  async function remove(a: AchievementDef) {
    if (!confirmAction("Diese Auszeichnung wirklich löschen?")) return;
    if (await action.run(() => api.deleteAchievement(childId, a.id))) list.reload();
  }

  return (
    <section>
      <h2 className="h-section">Auszeichnungen {list.data ? `(${list.data.length})` : ""}</h2>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ maxWidth: 80 }}><label htmlFor={`${uid}-icon`}>Icon</label>
          <input id={`${uid}-icon`} value={form.icon ?? ""} onChange={(e) => up("icon", e.target.value)} placeholder="🏆" /></div>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Test-Ass" /></div>
        <div className="field"><FieldLabel htmlFor={`${uid}-metric`} topic="missionMetric">Ziel-Metrik</FieldLabel>
          <select id={`${uid}-metric`} value={form.metric} onChange={(e) => up("metric", e.target.value as ProgressMetric)}>
            {METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-threshold`} topic="achievementThreshold">Schwelle</FieldLabel>
          <input id={`${uid}-threshold`} type="number" min={1} value={form.threshold} onChange={(e) => up("threshold", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 120 }}><FieldLabel htmlFor={`${uid}-reward`} topic="missionReward">Belohnung 💎</FieldLabel>
          <input id={`${uid}-reward`} type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Lege an…" : "Anlegen"}
        </button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th></th><th>Titel</th><th>Schwelle</th><th>Belohnung</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((a) => (
                <tr key={a.id} style={{ opacity: a.active ? 1 : 0.55 }}>
                  <td style={{ fontSize: 20 }}>{a.icon ?? "🎖️"}</td>
                  <td>{a.title}</td>
                  <td className="muted">{a.threshold}× {metricLabel(a.metric)}</td>
                  <td>💎 {a.rewardPoints}</td>
                  <td>{a.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => toggle(a)}>
                      {a.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => remove(a)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.length === 0 && <tr><td colSpan={6} className="muted">Noch keine Auszeichnungen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
