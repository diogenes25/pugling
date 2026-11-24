import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { Pflicht } from "../components/Pflicht";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useChildSelection } from "../lib/useChildSelection";
import { confirmAction } from "../lib/ui";
import type {
  ChildResponse, ClassTestReadiness, CreateKlassenarbeitDto, ExerciseSummary, KlassenarbeitDetail,
  KlassenarbeitPractice, KlassenarbeitRepeat, KlassenarbeitResponse, KlassenarbeitStatus, Paged, SubjectResponse,
} from "../lib/types";
import { PAGE_SIZE, Pager, TruncationHint } from "../components/ListControls";
import { formatDate } from "../lib/datum";
import { NICHT_ZUWEISBAR_GRUND } from "./wizardSearch";

const STATUS_LABEL: Record<KlassenarbeitStatus, string> = {
  Planned: "geplant", Written: "geschrieben", Cancelled: "entfällt",
};

export function VaterClassTests() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  // Vorauswahl aus `?childId=` (die Links vom Kind-Hub tragen sie), sonst das erste Kind.
  const { activeChild, select } = useChildSelection(children.data);

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
              <label htmlFor="ct-child">Kind</label>
              <select id="ct-child" value={activeChild ?? ""} onChange={(e) => select(Number(e.target.value))}>
                {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (unter „Kinder").</div>}
      </section>

      {activeChild !== null && <ClassTestManager key={activeChild} childId={activeChild} />}
    </>
  );
}

/** Exportiert für den Test (B-380). */
export function ClassTestManager({ childId }: { childId: number }) {
  // Server-paginiert (feste Termin-Ordnung, daher keine Sortier-UI). childId ist je Instanz fix
  // (key={activeChild} remountet bei Kindwechsel) → skip startet sauber bei 0.
  const [skip, setSkip] = useState(0);
  const list = useAsync<Paged<KlassenarbeitResponse>>(() => api.classTests(childId, { skip, take: PAGE_SIZE }), [childId, skip]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const repeat = useAsync<KlassenarbeitRepeat>(() => api.classTestRepeat(childId), [childId]);
  const [openId, setOpenId] = useState<number | null>(null);
  const action = useAction();

  const [form, setForm] = useState<CreateKlassenarbeitDto>({
    childId, title: "", topic: "", subjectId: null, scheduledDate: "",
  });
  // Leer = der Standard des Servers (B-757); als Text gehalten, damit „leer“ und „0“ (aus) unterscheidbar bleiben.
  const [fenster, setFenster] = useState("");
  function up<K extends keyof CreateKlassenarbeitDto>(k: K, v: CreateKlassenarbeitDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  // Die Liste hängt am Wiederholungs-Vorschlag: eine benotete Arbeit ändert, was zu üben ist.
  function reloadAll() { list.reload(); repeat.reload(); }

  async function create(e: React.FormEvent) {
    e.preventDefault();
    const title = form.title.trim();
    // Stumm (B-380, Entscheidung 4): der Knopf ist leer gesperrt, nur Enter im Feld kommt hier an.
    if (!title || !form.scheduledDate) return;
    const ok = await action.run(() => api.createClassTest({
      ...form, childId, title, topic: form.topic?.trim() || null, subjectId: form.subjectId || null,
      prepWindowDays: fensterWert(fenster),
    }), `Klassenarbeit „${title}" geplant.`);
    if (!ok) return;
    setForm((f) => ({ ...f, title: "", topic: "" }));
    setFenster("");
    reloadAll();
  }

  async function saveGrade(k: KlassenarbeitResponse, grade: number | null) {
    const ok = await action.run(
      () => api.updateClassTest(k.id, grade == null ? { clearGrade: true } : { grade, status: "Written" }),
      grade == null ? "Note entfernt." : `Note ${grade.toFixed(1)} eingetragen.`);
    if (ok) reloadAll();
  }

  async function remove(k: KlassenarbeitResponse) {
    if (!confirmAction("Diese Klassenarbeit wirklich löschen?")) return;
    if (!await action.run(() => api.deleteClassTest(k.id), "Klassenarbeit gelöscht.")) return;
    if (openId === k.id) setOpenId(null);
    reloadAll();
  }

  return (
    <>
      <section>
        <h3 className="h-section">Neue Klassenarbeit</h3>
        <form className="form-grid" onSubmit={create} style={{ alignItems: "end" }}>
          <div className="field" style={{ minWidth: 200 }}><label htmlFor="ct-title">Titel <Pflicht /></label>
            <input id="ct-title" value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Vokabeltest Unité 3" /></div>
          <div className="field" style={{ minWidth: 160 }}><label htmlFor="ct-topic">Thema (optional)</label>
            <input id="ct-topic" value={form.topic ?? ""} onChange={(e) => up("topic", e.target.value)} placeholder="Passé composé" /></div>
          <div className="field"><label>Fach (optional)</label>
            <select title="Fach" value={form.subjectId ?? ""} onChange={(e) => up("subjectId", e.target.value ? Number(e.target.value) : null)}>
              <option value="">–</option>
              {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select></div>
          <div className="field"><label htmlFor="ct-date">Termin <Pflicht /></label>
            <input id="ct-date" title="Termin" type="date" value={form.scheduledDate} onChange={(e) => up("scheduledDate", e.target.value)} /></div>
          <div className="field" style={{ maxWidth: 170 }}><label htmlFor="ct-prep">Vorbereitung (Tage)</label>
            <input id="ct-prep" type="number" min={0} max={MAX_FENSTER} value={fenster} onChange={(e) => setFenster(e.target.value)}
              placeholder="Standard" /></div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }}
            disabled={action.busy || !form.title.trim() || !form.scheduledDate}>{action.busy ? "Plane…" : "Planen"}</button>
        </form>
        <StatusBanner message={action.message} style={{ marginTop: 10 }} />
      </section>

      <section>
        <h3 className="h-section">Geplant & geschrieben {list.data ? `(${list.data.total})` : ""}</h3>
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Titel</th><th>Termin</th><th>Status</th><th>Übungen</th><th>Vorbereitung</th><th>Note</th><th>Prognose</th><th>Aktion</th></tr></thead>
              <tbody>
                {list.data?.items.map((k) => (
                  <tr key={k.id}>
                    <td>{k.title}{k.topic ? <span className="muted"> · {k.topic}</span> : ""}
                      {k.subjectName ? <span className="muted"> · {k.subjectName}</span> : ""}</td>
                    <td className="muted">{formatDate(k.scheduledDate)}</td>
                    <td>{k.status === "Written"
                      ? <span className="pill lime">{STATUS_LABEL[k.status]}</span>
                      : <span className="pill">{STATUS_LABEL[k.status]}</span>}</td>
                    <td className="num">{k.directExerciseCount}{k.tags.length > 0 ? ` +${k.tags.length}🏷️` : ""}</td>
                    <td><VorbereitungsZelle k={k} /><BereitschaftZeile k={k} /></td>
                    <td><GradeCell key={`${k.id}-${k.grade ?? ""}`} current={k.grade ?? null} busy={action.busy} onSave={(g) => saveGrade(k, g)} /></td>
                    <td><PrognoseZelle k={k} /></td>
                    <td style={{ whiteSpace: "nowrap" }}>
                      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                        onClick={() => setOpenId(openId === k.id ? null : k.id)}>{openId === k.id ? "Schließen" : "Übungen & Vorbereiten"}</button>{" "}
                      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={() => remove(k)}>Löschen</button>
                    </td>
                  </tr>
                ))}
                {list.data?.total === 0 && <tr><td colSpan={8} className="muted">Noch keine Klassenarbeiten.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        {list.data && <Pager skip={skip} take={PAGE_SIZE} total={list.data.total} onSkip={setSkip} busy={list.loading} />}
        {openId !== null && <ClassTestDetail key={openId} id={openId}
          subjectId={list.data?.items.find((k) => k.id === openId)?.subjectId ?? null} onChanged={reloadAll} />}
      </section>

      <RepeatPanel repeat={repeat} />
    </>
  );
}

/**
 * Abstand der Note zur Prognose (B-746): Note minus prognostizierte Stufe, roh – 2,5 gegen Stufe 3 ist −0,5, also besser
 * als erwartet (kleinere Note = besser). Gerundet wird erst in der Anzeige. Ohne Prognose oder ohne Note gibt es keinen
 * Abstand. Die Stufe ist am Klausurtag festgeschrieben; gespeichert wird der Abstand nirgends.
 */
export function prognoseAbstand(grade: number | null | undefined, predicted: number | null | undefined): number | null {
  if (grade == null || predicted == null) return null;
  return grade - predicted;
}

/**
 * Der Abstand in Worten statt mit Vorzeichen – ein „−“ liest sich leicht als „schlechter“, ist bei Noten aber besser.
 * Auf Zehntel gerundet (symmetrisch, `toLocaleString`); was auf 0 rundet, ist „wie erwartet“.
 */
export function abstandText(abstand: number): string {
  const betrag = Math.abs(abstand).toLocaleString("de-DE", { maximumFractionDigits: 1 });
  if (betrag === "0") return "wie erwartet";
  return `${betrag} ${abstand < 0 ? "besser" : "schlechter"}`;
}

/**
 * Die Prognose-Spalte (B-746): die Stufe, die die App am Klausurtag für das Fach festgehalten hat, und – sobald die Note
 * steht – wie weit die Note davon abweicht. Ohne Schnappschuss (Tag noch nicht da, an ihm keine Nutzung, kein Fach, keine
 * Stufe) steht „keine Prognose“, nie eine Zahl.
 */
export function PrognoseZelle({ k }: { k: KlassenarbeitResponse }) {
  if (k.predictedLevel == null) return <span className="muted">keine Prognose</span>;
  const abstand = prognoseAbstand(k.grade, k.predictedLevel);
  return (
    <span>
      Stufe {k.predictedLevel}
      {abstand !== null && <span className="muted">{" · "}{abstandText(abstand)}</span>}
    </span>
  );
}

/** Obergrenze des Vorbereitungs-Fensters – dieselbe wie auf dem Server (`ClassTestPrepOptions.MaxWindowDays`). */
export const MAX_FENSTER = 60;

/** Das Fenster aus dem Feld: leer = Standard (`null`), sonst die ganze Zahl. */
export const fensterWert = (text: string): number | null => (text.trim() === "" ? null : Math.trunc(Number(text)));

const GRUND: Record<string, string> = {
  no_subject: "keine Pflicht: die Arbeit hat kein Fach",
  no_active_plan: "keine Pflicht: kein aktiver Plan im Fach",
};

/**
 * Was die Vorbereitung dem Kind abverlangt (B-757): „etwa N Karten je Tag“ mit der Zahl des Servers, oder warum keine
 * Pflicht entsteht. Gerechnet wird hier nichts – die Dosisregel steht an einer Stelle, auf dem Server.
 */
export function VorbereitungsZelle({ k }: { k: KlassenarbeitResponse }) {
  if (k.status !== "Planned") return <span className="muted">–</span>;
  const tage = k.prepWindowDays ?? k.prepWindowDefault;
  if (tage === 0) return <span className="muted">aus</span>;
  if (k.prepBlockedReason) return <span className="muted">{GRUND[k.prepBlockedReason] ?? "keine Pflicht"}</span>;
  return (
    <span>
      {k.prepCardsPerDay != null ? `etwa ${k.prepCardsPerDay} Karten je Tag` : "–"}
      <span className="muted"> · {tage} Tage{k.prepWindowDays == null ? " (Standard)" : ""}</span>
      {/* B-941: zurückgezogenes Material wird keine Pflicht – der Vater soll sehen, warum die Pflicht kürzer ist. */}
      {k.prepNotExecutableCount > 0 && <span className="muted">
        {" · "}{k.prepNotExecutableCount} {k.prepNotExecutableCount === 1 ? "Übung" : "Übungen"} ohne Ausführ-Recht ausgelassen</span>}
    </span>
  );
}

/**
 * Wie weit das Kind für eine geplante Arbeit ist (B-949): „x von y Wörtern sicher“ – dieselbe Zahl vom selben Endpunkt wie auf
 * der Karte des Kindes, kein zweites Rechenmodell. Ohne Vokabeln, bei geschriebenen oder abgesagten Arbeiten und nach einem
 * Fehler steht nichts; die Liste bleibt davon unberührt.
 */
export function BereitschaftZeile({ k }: { k: KlassenarbeitResponse }) {
  const geplant = k.status === "Planned";
  const stand = useAsync<ClassTestReadiness | null>(
    () => (geplant ? api.classTestReadiness(k.childId, k.id) : Promise.resolve(null)),
    // Das Bündel gehört zu den Abhängigkeiten: weist der Vater Übungen zu, lädt die Liste neu, und die Zahl muss mitziehen.
    [k.childId, k.id, geplant, k.directExerciseCount, k.tags.map((t) => t.id).join("|")]);
  const s = stand.data;
  // Ein Fehler lässt `data` leer, die Zeile entfällt über `!s`; ein veralteter Stand gehört zum Bündel vor der Änderung.
  if (!geplant || stand.stale || !s || s.totalItems == null || s.secureItems == null) return null;
  return <div className="muted">{s.secureItems} von {s.totalItems} Wörtern sicher</div>;
}

/**
 * Das Vorbereitungs-Fenster einer bestehenden Arbeit ändern (B-757): eine Zahl (0 = aus) oder leer für den Standard. Leer
 * speichert über `clearPrepWindow` – ein fehlendes Feld ließe den alten Wert stehen (PATCH-Semantik).
 */
export function FensterEditor({ k, onSaved }: { k: KlassenarbeitResponse; onSaved: () => void }) {
  const [text, setText] = useState(k.prepWindowDays != null ? String(k.prepWindowDays) : "");
  const action = useAction();
  const wert = fensterWert(text);
  const gueltig = wert === null || (Number.isFinite(wert) && wert >= 0 && wert <= MAX_FENSTER);
  async function speichern(e: React.FormEvent) {
    e.preventDefault();
    if (!gueltig) return;
    const ok = await action.run(
      () => api.updateClassTest(k.id, wert === null ? { clearPrepWindow: true } : { prepWindowDays: wert }),
      wert === null ? "Vorbereitung zurück auf den Standard." : wert === 0 ? "Vorbereitung abgeschaltet." : `Vorbereitung: ${wert} Tage.`);
    if (ok) onSaved();
  }
  return (
    <form className="row" onSubmit={speichern} style={{ marginTop: 6, alignItems: "end" }}>
      <div className="field" style={{ maxWidth: 220 }}><label htmlFor={`ct-prep-${k.id}`}>Vorbereitung (Tage, leer = Standard {k.prepWindowDefault})</label>
        <input id={`ct-prep-${k.id}`} type="number" min={0} max={MAX_FENSTER} value={text} onChange={(e) => setText(e.target.value)}
          placeholder="Standard" /></div>
      <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy || !gueltig}>
        Fenster speichern</button>
      <StatusBanner message={action.message} />
    </form>
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
  // Gesamtzahl mitführen: der Server liefert nur eine Seite, sonst wirkt die Liste vollständig.
  const found = useAsync<Paged<ExerciseSummary>>(
    () => api.searchExercises({ subjectId: subjectId ?? undefined, search: search || undefined }), [id, subjectId]);
  const action = useAction();

  const assignedIds = new Set(detail.data?.assignedExercises.map((e) => e.id));

  async function act(fn: () => Promise<unknown>) {
    if (!await action.run(fn)) return;
    detail.reload();
    practice.reload();
    onChanged();
  }

  return (
    <div className="card" style={{ marginTop: 10 }}>
      {detail.loading && detail.data === null ? <div className="loading">Lade…</div> : detail.error ? <div className="banner err">{detail.error}</div> : detail.data && (
        <>
          <div className="row">
            <b>{detail.data.klassenarbeit.title}</b>
            {practice.data && <span className="pill" style={{ marginLeft: "auto" }}>
              {practice.data.daysUntil >= 0 ? `noch ${practice.data.daysUntil} Tage` : "Termin vorbei"}</span>}
          </div>
          {/* Schlüssel nur die Arbeit (B-942): mit dem gespeicherten Wert darin montierte das Neuladen den Editor neu und
              nahm ihm die Erfolgsmeldung. */}
          <FensterEditor key={id} k={detail.data.klassenarbeit}
            onSaved={() => { detail.reload(); onChanged(); }} />

          <h4 className="h-section" style={{ marginTop: 10 }}>Zugewiesene Übungen ({detail.data.assignedExercises.length})</h4>
          {detail.data.assignedExercises.length === 0 ? <p className="muted">Noch keine – unten aus dem Katalog zuweisen.</p> : (
            <ul>
              {detail.data.assignedExercises.map((e) => (
                <li key={e.id}>{e.title} <span className="muted">({e.type} · {e.subjectName})</span>{" "}
                  <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy}
                    onClick={() => act(() => api.unassignClassTestExercise(id, e.id))}>entfernen</button></li>
              ))}
            </ul>
          )}

          <h4 className="h-section" style={{ marginTop: 10 }}>Übungen aus dem Katalog zuweisen</h4>
          <form className="row" onSubmit={(e) => { e.preventDefault(); found.reload(); }} style={{ marginBottom: 6 }}>
            <input aria-label="Suche im Katalog" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Suche im Katalog…" style={{ maxWidth: 260 }} />
            <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }}>Suchen</button>
          </form>
          {found.loading && found.data === null ? <div className="loading">Lade…</div> : found.data && (
            found.data.total === 0 ? <p className="muted">Keine passenden Übungen im Katalog.</p> : (
              <ul>
                {found.data.items.map((e) => (
                  <li key={e.id}>{e.title} <span className="muted">({e.type})</span>{" "}
                    {assignedIds.has(e.id)
                      ? <span className="pill lime">zugewiesen</span>
                      // B-140: eine zurückgezogene Übung ohne Freigabe weist der Server ab (403) – der Knopf fehlt, der
                      // Grund steht da, statt dass erst das Zuweisen scheitert.
                      : !e.canAssign
                        ? <span className="muted">{NICHT_ZUWEISBAR_GRUND}</span>
                        : <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy}
                            onClick={() => act(() => api.assignClassTestExercises(id, [e.id]))}>zuweisen</button>}</li>
                ))}
              </ul>
            )
          )}
          {/* Die Region steht, sobald die Arbeit geladen ist; eine neue Suche ändert danach nur ihren Inhalt
              (B-134). Das allererste Laden kann stumm bleiben – dieselbe Abwägung wie bei der Seitenanzeige. */}
          <TruncationHint shown={found.data?.items.length ?? 0} total={found.data?.total ?? 0} />

          {practice.data && (
            <>
              <h4 className="h-section" style={{ marginTop: 10 }}>Zum Vorbereiten relevant ({practice.data.exercises.length})</h4>
              {practice.data.exercises.length === 0 ? <p className="muted">Noch nichts zugewiesen.</p> : (
                <ul>{practice.data.exercises.map((e) => <li key={e.id}>{e.title} <span className="muted">({e.type})</span></li>)}</ul>
              )}
            </>
          )}

          <StatusBanner message={action.message} />
        </>
      )}
    </div>
  );
}

/** Übungen aus schwach benoteten Arbeiten zum Wiederholen. */
export function RepeatPanel({ repeat }: { repeat: ReturnType<typeof useAsync<KlassenarbeitRepeat>> }) {
  return (
    <section>
      <h3 className="h-section">Wiederholen: schwach benotete Arbeiten</h3>
      {repeat.loading && repeat.data === null ? <div className="loading">Lade…</div> : repeat.error ? <div className="banner err">{repeat.error}</div> : repeat.data && (
        repeat.data.sources.length === 0
          // Kein Erfolg: dasselbe gilt für ein Kind ganz ohne bewertete Arbeit (B-496).
          ? <p className="muted">Keine Arbeit mit Note ≥ {repeat.data.minBadGrade.toFixed(1)} – aus Klassenarbeiten ist nichts zu wiederholen.</p>
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
