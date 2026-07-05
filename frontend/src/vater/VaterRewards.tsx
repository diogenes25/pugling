import { useId, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { confirmAction } from "../lib/ui";
import { offerPeriodLabel } from "../lib/labels";
import type {
  AchievementDef, ChildResponse, CreateAchievementDto, CreateMissionDto,
  MissionDef, MissionPeriod, OfferPeriod, PlanResponse, PositionResponse, ProgressMetric, RewardDef,
} from "../lib/types";

/** Wiederkehr-Optionen für Angebote (deutsche Labels). */
const OFFER_PERIODS: OfferPeriod[] = ["OneOff", "Daily", "Weekly", "Monthly"];

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
  const [childId, setChildId] = useState<number | null>(null);
  // Beim ersten Laden das erste Kind vorwählen.
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

  return (
    <>
      <section>
        <h2 className="h-section">Belohnungen</h2>
        <p className="muted">Setze Ziele, für die dein Kind beim Lernen Münzen verdient – die es dann
          (echt) für Charaktere ausgeben kann. <b>Missionen</b> sind zeitgebundene Ziele (täglich/wöchentlich),
          <b> Auszeichnungen</b> sind dauerhafte Meilensteine.</p>
        {children.loading ? <div className="loading">Lade…</div>
          : children.error ? <div className="banner err">{children.error}</div>
          : children.data && children.data.length > 0 ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label>Kind</label>
              <select title="Kind" value={activeChild ?? ""} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data.map((c) => (
                  <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>
                ))}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}
      </section>

      {activeChild !== null && <MissionManager key={`m${activeChild}`} childId={activeChild} />}
      {activeChild !== null && <AchievementManager key={`a${activeChild}`} childId={activeChild} />}
      {activeChild !== null && <RewardOfferManager key={`r${activeChild}`} childId={activeChild} />}
    </>
  );
}

interface RewardForm {
  title: string; cost: number; period: OfferPeriod; quantity: number;
  studyPlanId: number | ""; exerciseId: number | "";
}

function RewardOfferManager({ childId }: { childId: number }) {
  const uid = useId();
  const list = useAsync<RewardDef[]>(() => api.rewardsFor(childId), [childId]);
  const plans = useAsync<PlanResponse[]>(() => api.plans(childId), [childId]);
  const [form, setForm] = useState<RewardForm>({ title: "", cost: 200, period: "Weekly", quantity: 1, studyPlanId: "", exerciseId: "" });
  // Übungen zur Auswahl kommen aus den Positionen des gewählten Plans (nur dann ist ein Übungs-Bezug sinnvoll).
  const positions = useAsync<PositionResponse[]>(
    () => (form.studyPlanId === "" ? Promise.resolve([]) : api.positions(form.studyPlanId)),
    [form.studyPlanId]);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.title.trim()) { setMsg({ ok: false, text: "Titel nötig." }); return; }
    if (form.cost <= 0) { setMsg({ ok: false, text: "Preis muss positiv sein." }); return; }
    if (form.quantity < 1) { setMsg({ ok: false, text: "Anzahl muss mindestens 1 sein." }); return; }
    setBusy(true);
    try {
      await api.createReward(childId, {
        title: form.title.trim(), cost: form.cost, period: form.period, quantity: form.quantity,
        studyPlanId: form.studyPlanId === "" ? null : form.studyPlanId,
        exerciseId: form.exerciseId === "" ? null : form.exerciseId,
      });
      setMsg({ ok: true, text: `Angebot „${form.title.trim()}" angelegt.` });
      setForm((f) => ({ ...f, title: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  async function toggle(r: RewardDef) {
    setBusy(true);
    try { await api.updateReward(childId, r.id, { active: !r.active }); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
    finally { setBusy(false); }
  }
  async function remove(r: RewardDef) {
    if (!confirmAction("Dieses Angebot wirklich löschen?")) return;
    setBusy(true);
    try { await api.deleteReward(childId, r.id); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <h2 className="h-section">Angebote zum Kaufen {list.data ? `(${list.data.length})` : ""}</h2>
      <p className="muted">Reale Belohnungen, die dein Kind mit verdienten 🪙 Münzen kauft (z.B. 1 h Spielzeit).
        Das Kind kauft <b>sofort</b> (Münzen sind gleich weg); du erfüllst den Kauf im <b>Konto</b>. Anzahl =
        Kontingent pro Periode (füllt sich jede Periode neu auf).</p>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} placeholder="1 Stunde Zocken" /></div>
        <div className="field" style={{ maxWidth: 120 }}><label>Preis 🪙</label>
          <input title="Preis in Münzen" type="number" min={1} value={form.cost}
            onChange={(e) => setForm((f) => ({ ...f, cost: Number(e.target.value) }))} /></div>
        <div className="field" style={{ maxWidth: 150 }}><label>Wiederkehr</label>
          <select title="Wiederkehr" value={form.period} onChange={(e) => setForm((f) => ({ ...f, period: e.target.value as OfferPeriod }))}>
            {OFFER_PERIODS.map((p) => <option key={p} value={p}>{offerPeriodLabel(p)}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 110 }}><label>Anzahl</label>
          <input title="Kontingent pro Periode" type="number" min={1} value={form.quantity}
            onChange={(e) => setForm((f) => ({ ...f, quantity: Number(e.target.value) }))} /></div>
        <div className="field" style={{ minWidth: 160 }}><label>Plan <span className="muted">(optional)</span></label>
          <select title="Plan-Bezug" value={form.studyPlanId}
            onChange={(e) => setForm((f) => ({ ...f, studyPlanId: e.target.value ? Number(e.target.value) : "", exerciseId: "" }))}>
            <option value="">– kindweit –</option>
            {plans.data?.map((p) => <option key={p.id} value={p.id}>{p.title}</option>)}
          </select></div>
        <div className="field" style={{ minWidth: 160 }}><label>Übung <span className="muted">(optional)</span></label>
          <select title="Übungs-Bezug" value={form.exerciseId} disabled={form.studyPlanId === ""}
            onChange={(e) => setForm((f) => ({ ...f, exerciseId: e.target.value ? Number(e.target.value) : "" }))}>
            <option value="">– ganzer Plan –</option>
            {positions.data?.map((p) => <option key={p.id} value={p.exerciseId}>{p.exerciseTitle}</option>)}
          </select></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Anlegen"}</button>
      </form>
      {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Angebot</th><th>Kontext</th><th>Preis</th><th>Wiederkehr</th><th className="num">Anzahl</th><th>Status</th><th>Aktion</th></tr></thead>
            <tbody>
              {list.data?.map((r) => (
                <tr key={r.id} style={{ opacity: r.active ? 1 : 0.55 }}>
                  <td>{r.title}</td>
                  <td className="muted">{r.exerciseTitle ? `${r.planTitle} · ${r.exerciseTitle}` : r.planTitle ?? "kindweit"}</td>
                  <td>🪙 {r.cost}</td>
                  <td className="muted">{offerPeriodLabel(r.period)}</td>
                  <td className="num">{r.quantity}</td>
                  <td>{r.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => toggle(r)}>
                      {r.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => remove(r)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Angebote.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function MissionManager({ childId }: { childId: number }) {
  const uid = useId();
  const list = useAsync<MissionDef[]>(() => api.missionsFor(childId), [childId]);
  const [form, setForm] = useState<CreateMissionDto>({
    title: "", metric: "CorrectReviews", target: 10, period: "Daily", rewardPoints: 15,
  });
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  function up<K extends keyof CreateMissionDto>(k: K, v: CreateMissionDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.title.trim()) { setMsg({ ok: false, text: "Titel nötig." }); return; }
    if (form.target <= 0) { setMsg({ ok: false, text: "Ziel muss positiv sein." }); return; }
    setBusy(true);
    try {
      await api.createMission(childId, { ...form, title: form.title.trim() });
      setMsg({ ok: true, text: `Mission „${form.title.trim()}" angelegt.` });
      setForm((f) => ({ ...f, title: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  async function toggle(m: MissionDef) {
    try { await api.updateMission(childId, m.id, { active: !m.active }); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }
  async function remove(m: MissionDef) {
    if (!confirmAction("Diese Mission wirklich löschen?")) return;
    try { await api.deleteMission(childId, m.id); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <section>
      <h2 className="h-section">Missionen {list.data ? `(${list.data.length})` : ""}</h2>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Tagesziel: 10 richtig" /></div>
        <div className="field"><label htmlFor={`${uid}-metric`}>Ziel-Metrik</label>
          <select id={`${uid}-metric`} value={form.metric} onChange={(e) => up("metric", e.target.value as ProgressMetric)}>
            {METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 100 }}><label htmlFor={`${uid}-target`}>Zielwert</label>
          <input id={`${uid}-target`} type="number" min={1} value={form.target} onChange={(e) => up("target", Number(e.target.value))} /></div>
        <div className="field"><label htmlFor={`${uid}-period`}>Zeitraum</label>
          <select id={`${uid}-period`} value={form.period} onChange={(e) => up("period", e.target.value as MissionPeriod)}>
            {PERIODS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 120 }}><label htmlFor={`${uid}-reward`}>Belohnung 🪙</label>
          <input id={`${uid}-reward`} type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Anlegen"}</button>
      </form>
      {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}

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
                  <td>🪙 {m.rewardPoints}</td>
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
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  function up<K extends keyof CreateAchievementDto>(k: K, v: CreateAchievementDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.title.trim()) { setMsg({ ok: false, text: "Titel nötig." }); return; }
    if (form.threshold <= 0) { setMsg({ ok: false, text: "Schwelle muss positiv sein." }); return; }
    setBusy(true);
    try {
      await api.createAchievement(childId, { ...form, title: form.title.trim(), icon: form.icon?.trim() || null });
      setMsg({ ok: true, text: `Auszeichnung „${form.title.trim()}" angelegt.` });
      setForm((f) => ({ ...f, title: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  async function toggle(a: AchievementDef) {
    try { await api.updateAchievement(childId, a.id, { active: !a.active }); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }
  async function remove(a: AchievementDef) {
    if (!confirmAction("Diese Auszeichnung wirklich löschen?")) return;
    try { await api.deleteAchievement(childId, a.id); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <section>
      <h2 className="h-section">Auszeichnungen {list.data ? `(${list.data.length})` : ""}</h2>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ maxWidth: 80 }}><label htmlFor={`${uid}-icon`}>Icon</label>
          <input id={`${uid}-icon`} value={form.icon ?? ""} onChange={(e) => up("icon", e.target.value)} placeholder="🏆" /></div>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Test-Ass" /></div>
        <div className="field"><label htmlFor={`${uid}-metric`}>Ziel-Metrik</label>
          <select id={`${uid}-metric`} value={form.metric} onChange={(e) => up("metric", e.target.value as ProgressMetric)}>
            {METRICS.map((m) => <option key={m.value} value={m.value}>{m.label}</option>)}
          </select></div>
        <div className="field" style={{ maxWidth: 100 }}><label htmlFor={`${uid}-threshold`}>Schwelle</label>
          <input id={`${uid}-threshold`} type="number" min={1} value={form.threshold} onChange={(e) => up("threshold", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 120 }}><label htmlFor={`${uid}-reward`}>Belohnung 🪙</label>
          <input id={`${uid}-reward`} type="number" min={0} value={form.rewardPoints} onChange={(e) => up("rewardPoints", Number(e.target.value))} /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Anlegen"}</button>
      </form>
      {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}

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
                  <td>🪙 {a.rewardPoints}</td>
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
