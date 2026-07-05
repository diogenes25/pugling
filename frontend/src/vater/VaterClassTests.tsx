import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { confirmAction } from "../lib/ui";
import type {
  ChildResponse, CreateKlassenarbeitDto, ExerciseSummary, KlassenarbeitDetail,
  KlassenarbeitPractice, KlassenarbeitRepeat, KlassenarbeitResponse, KlassenarbeitStatus, Paged, SubjectResponse,
} from "../lib/types";
import { PAGE_SIZE, Pager } from "../components/ListControls";

const STATUS_LABEL: Record<KlassenarbeitStatus, string> = {
  Planned: "geplant", Written: "geschrieben", Cancelled: "entfällt",
};

export function VaterClassTests() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | null>(null);
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

  return (
    <>
      <section>
        <h2 className="h-section">Klassenarbeiten</h2>
        <p className="muted">Plane den Ernstfall: Arbeit anlegen, relevante Übungen zuweisen, gezielt
          vorbereiten – nach dem Schreiben die Note nachtragen und schwach benotete Arbeiten wiederholen.</p>
        {children.loading ? <div className="loading">Lade…</div>
          : children.error ? <div className="banner err">{children.error}</div>
          : children.data && children.data.length > 0 ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label>Kind</label>
              <select title="Kind" value={activeChild ?? ""} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}
      </section>

      {activeChild !== null && <ClassTestManager key={activeChild} childId={activeChild} />}
    </>
  );
}

function ClassTestManager({ childId }: { childId: number }) {
  // Server-paginiert (feste Termin-Ordnung, daher keine Sortier-UI). childId ist je Instanz fix
  // (key={activeChild} remountet bei Kindwechsel) → skip startet sauber bei 0.
  const [skip, setSkip] = useState(0);
  const list = useAsync<Paged<KlassenarbeitResponse>>(() => api.classTests(childId, { skip, take: PAGE_SIZE }), [childId, skip]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const repeat = useAsync<KlassenarbeitRepeat>(() => api.classTestRepeat(childId), [childId]);
  const [openId, setOpenId] = useState<number | null>(null);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const [form, setForm] = useState<CreateKlassenarbeitDto>({
    childId, title: "", topic: "", subjectId: null, scheduledDate: "",
  });
  function up<K extends keyof CreateKlassenarbeitDto>(k: K, v: CreateKlassenarbeitDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }
  function flash(ok: boolean, text: string) { setMsg({ ok, text }); setTimeout(() => setMsg(null), 2500); }

  function reloadAll() { list.reload(); repeat.reload(); }

  async function create(e: React.FormEvent) {
    e.preventDefault();
    if (!form.title.trim()) { flash(false, "Titel nötig."); return; }
    if (!form.scheduledDate) { flash(false, "Termin nötig."); return; }
    setBusy(true);
    try {
      await api.createClassTest({
        ...form, childId, title: form.title.trim(), topic: form.topic?.trim() || null,
        subjectId: form.subjectId || null,
      });
      flash(true, `Klassenarbeit „${form.title.trim()}" geplant.`);
      setForm((f) => ({ ...f, title: "", topic: "" }));
      reloadAll();
    } catch (err) { flash(false, errorMessage(err)); }
    finally { setBusy(false); }
  }

  async function saveGrade(k: KlassenarbeitResponse, grade: number | null) {
    setBusy(true);
    try {
      await api.updateClassTest(k.id, grade == null
        ? { clearGrade: true }
        : { grade, status: "Written" });
      flash(true, grade == null ? "Note entfernt." : `Note ${grade.toFixed(1)} eingetragen.`);
      reloadAll();
    } catch (err) { flash(false, errorMessage(err)); }
    finally { setBusy(false); }
  }

  async function remove(k: KlassenarbeitResponse) {
    if (!confirmAction("Diese Klassenarbeit wirklich löschen?")) return;
    setBusy(true);
    try { await api.deleteClassTest(k.id); if (openId === k.id) setOpenId(null); flash(true, "Klassenarbeit gelöscht."); reloadAll(); }
    catch (err) { flash(false, errorMessage(err)); }
    finally { setBusy(false); }
  }

  return (
    <>
      <section>
        <h3 className="h-section">Neue Klassenarbeit</h3>
        <form className="form-grid" onSubmit={create} style={{ alignItems: "end" }}>
          <div className="field" style={{ minWidth: 200 }}><label htmlFor="ct-title">Titel</label>
            <input id="ct-title" value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Vokabeltest Unité 3" /></div>
          <div className="field" style={{ minWidth: 160 }}><label htmlFor="ct-topic">Thema (optional)</label>
            <input id="ct-topic" value={form.topic ?? ""} onChange={(e) => up("topic", e.target.value)} placeholder="Passé composé" /></div>
          <div className="field"><label>Fach (optional)</label>
            <select title="Fach" value={form.subjectId ?? ""} onChange={(e) => up("subjectId", e.target.value ? Number(e.target.value) : null)}>
              <option value="">–</option>
              {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select></div>
          <div className="field"><label>Termin</label>
            <input title="Termin" type="date" value={form.scheduledDate} onChange={(e) => up("scheduledDate", e.target.value)} /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Planen"}</button>
        </form>
        {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}
      </section>

      <section>
        <h3 className="h-section">Geplant & geschrieben {list.data ? `(${list.data.total})` : ""}</h3>
        {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Titel</th><th>Termin</th><th>Status</th><th>Übungen</th><th>Note</th><th>Aktion</th></tr></thead>
              <tbody>
                {list.data?.items.map((k) => (
                  <tr key={k.id}>
                    <td>{k.title}{k.topic ? <span className="muted"> · {k.topic}</span> : ""}
                      {k.subjectName ? <span className="muted"> · {k.subjectName}</span> : ""}</td>
                    <td className="muted">{k.scheduledDate}</td>
                    <td>{k.status === "Written"
                      ? <span className="pill lime">{STATUS_LABEL[k.status]}</span>
                      : <span className="pill">{STATUS_LABEL[k.status]}</span>}</td>
                    <td className="num">{k.directExerciseCount}{k.tags.length > 0 ? ` +${k.tags.length}🏷️` : ""}</td>
                    <td><GradeCell key={`${k.id}-${k.grade ?? ""}`} current={k.grade} busy={busy} onSave={(g) => saveGrade(k, g)} /></td>
                    <td style={{ whiteSpace: "nowrap" }}>
                      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                        onClick={() => setOpenId(openId === k.id ? null : k.id)}>{openId === k.id ? "Schließen" : "Übungen & Vorbereiten"}</button>{" "}
                      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={() => remove(k)}>Löschen</button>
                    </td>
                  </tr>
                ))}
                {list.data?.items.length === 0 && <tr><td colSpan={6} className="muted">Noch keine Klassenarbeiten.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        {list.data && <Pager skip={skip} take={PAGE_SIZE} total={list.data.total} onSkip={setSkip} />}
        {openId !== null && <ClassTestDetail key={openId} id={openId}
          subjectId={list.data?.items.find((k) => k.id === openId)?.subjectId ?? null} onChanged={reloadAll} />}
      </section>

      <RepeatPanel repeat={repeat} />
    </>
  );
}

/** Zelle zum Nachtragen/Entfernen der Note (deutsche Skala 1,0–6,0). */
function GradeCell({ current, busy, onSave }: { current: number | null; busy: boolean; onSave: (g: number | null) => void }) {
  const [val, setVal] = useState(current != null ? String(current) : "");
  // Nur eine gültige Note (1,0–6,0) darf gespeichert werden – sonst würde NaN als grade:null gesendet,
  // die Arbeit aber trotzdem auf "geschrieben" gesetzt. Ungültige/leere Eingaben deaktivieren OK.
  const num = Number(val);
  const valid = val.trim() !== "" && Number.isFinite(num) && num >= 1 && num <= 6;
  return (
    <span className="row" style={{ gap: 4 }}>
      <input title="Note" type="number" min={1} max={6} step={0.1} value={val} onChange={(e) => setVal(e.target.value)}
        placeholder="–" style={{ width: 64 }} />
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy || !valid}
        onClick={() => onSave(num)}>OK</button>
      {current != null && <button type="button" aria-label="Note entfernen" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
        onClick={() => { setVal(""); onSave(null); }}>×</button>}
    </span>
  );
}

/** Detailbereich: zugewiesene Übungen verwalten + gezielt vorbereiten. */
function ClassTestDetail({ id, subjectId, onChanged }:
  { id: number; subjectId: number | null; onChanged: () => void }) {
  const detail = useAsync<KlassenarbeitDetail>(() => api.classTest(id), [id]);
  const practice = useAsync<KlassenarbeitPractice>(() => api.classTestPractice(id), [id]);
  const [search, setSearch] = useState("");
  const found = useAsync<ExerciseSummary[]>(
    () => api.searchExercises({ subjectId: subjectId ?? undefined, search: search || undefined }).then((r) => r.items), [id, subjectId]);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  const assignedIds = new Set(detail.data?.assignedExercises.map((e) => e.id));

  async function act(fn: () => Promise<unknown>) {
    setBusy(true); setMsg(null);
    try { await fn(); detail.reload(); practice.reload(); onChanged(); }
    catch (err) { setMsg(errorMessage(err)); }
    finally { setBusy(false); }
  }

  return (
    <div className="card" style={{ marginTop: 10 }}>
      {detail.loading ? <div className="loading">Lade…</div> : detail.error ? <div className="banner err">{detail.error}</div> : detail.data && (
        <>
          <div className="row">
            <b>{detail.data.klassenarbeit.title}</b>
            {practice.data && <span className="pill" style={{ marginLeft: "auto" }}>
              {practice.data.daysUntil >= 0 ? `noch ${practice.data.daysUntil} Tage` : "Termin vorbei"}</span>}
          </div>

          <h4 className="h-section" style={{ marginTop: 10 }}>Zugewiesene Übungen ({detail.data.assignedExercises.length})</h4>
          {detail.data.assignedExercises.length === 0 ? <p className="muted">Noch keine – unten aus dem Katalog zuweisen.</p> : (
            <ul>
              {detail.data.assignedExercises.map((e) => (
                <li key={e.id}>{e.title} <span className="muted">({e.type} · {e.subjectName})</span>{" "}
                  <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
                    onClick={() => act(() => api.unassignClassTestExercise(id, e.id))}>entfernen</button></li>
              ))}
            </ul>
          )}

          <h4 className="h-section" style={{ marginTop: 10 }}>Übungen aus dem Katalog zuweisen</h4>
          <form className="row" onSubmit={(e) => { e.preventDefault(); found.reload(); }} style={{ marginBottom: 6 }}>
            <input aria-label="Suche im Katalog" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Suche im Katalog…" style={{ maxWidth: 260 }} />
            <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }}>Suchen</button>
          </form>
          {found.loading ? <div className="loading">Lade…</div> : found.data && (
            found.data.length === 0 ? <p className="muted">Keine passenden Übungen im Katalog.</p> : (
              <ul>
                {found.data.map((e) => (
                  <li key={e.id}>{e.title} <span className="muted">({e.type})</span>{" "}
                    {assignedIds.has(e.id)
                      ? <span className="pill lime">zugewiesen</span>
                      : <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
                          onClick={() => act(() => api.assignClassTestExercises(id, [e.id]))}>zuweisen</button>}</li>
                ))}
              </ul>
            )
          )}

          {practice.data && (
            <>
              <h4 className="h-section" style={{ marginTop: 10 }}>Zum Vorbereiten relevant ({practice.data.exercises.length})</h4>
              {practice.data.exercises.length === 0 ? <p className="muted">Noch nichts zugewiesen.</p> : (
                <ul>{practice.data.exercises.map((e) => <li key={e.id}>{e.title} <span className="muted">({e.type})</span></li>)}</ul>
              )}
            </>
          )}

          {msg && <div className="banner err">{msg}</div>}
        </>
      )}
    </div>
  );
}

/** Übungen aus schwach benoteten Arbeiten zum Wiederholen. */
function RepeatPanel({ repeat }: { repeat: ReturnType<typeof useAsync<KlassenarbeitRepeat>> }) {
  return (
    <section>
      <h3 className="h-section">Wiederholen: schwach benotete Arbeiten</h3>
      {repeat.loading ? <div className="loading">Lade…</div> : repeat.error ? <div className="banner err">{repeat.error}</div> : repeat.data && (
        repeat.data.sources.length === 0
          ? <p className="muted">Keine Arbeiten mit Note ≥ {repeat.data.minBadGrade.toFixed(1)} – nichts nachzuholen. 👍</p>
          : (
            <>
              <p className="muted">{repeat.data.sources.length} Arbeit(en) ≥ {repeat.data.minBadGrade.toFixed(1)}:
                {" "}{repeat.data.sources.map((s) => `${s.title} (${s.grade?.toFixed(1)})`).join(", ")}</p>
              {repeat.data.exercises.length === 0 ? <p className="muted">Keine zugewiesenen Übungen zum Wiederholen.</p> : (
                <ul>{repeat.data.exercises.map((e) => <li key={e.id}>{e.title} <span className="muted">({e.type} · {e.subjectName})</span></li>)}</ul>
              )}
            </>
          )
      )}
    </section>
  );
}
