import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { ExerciseAttribution } from "./ExerciseAttribution";
import { ExerciseEditModal } from "./ExerciseEditModal";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { PAGE_SIZE, Pager, SortControl } from "../components/ListControls";
import type {
  ChapterResponse, ExerciseSortKey, ExerciseSummary, ExerciseUsage, Paged, SortDir, SubjectResponse,
} from "../lib/types";
import { isKnownType } from "./exerciseConfig";
import { useExerciseTypes } from "../lib/exerciseTypes";

/**
 * Übungen **verwalten**: suchen, ausprobieren, bearbeiten, löschen.
 *
 * Diese Route trug früher vier Anliegen zugleich – Katalog, Lückentext-Store, Anlege-Formular und diese
 * Liste, alles in *einem* `<form>` (Anmerkung 11). Geblieben ist die Daueraufgabe; das Anlegen ist ein
 * abgeschlossener Vorgang und liegt auf `/vater/exercises/neu`. Siehe
 * docs/vater-informationsarchitektur-plan.md.
 *
 * Fach und Kapitel sind hier **Filter**, nicht Pflicht: die Liste erscheint, sobald ein Fach gewählt ist –
 * das Kapitel schränkt weiter ein. (Vorher blieb die Seite leer, bis auch ein Kapitel gewählt war, weil
 * die Auswahl zum Anlegen gehörte, nicht zum Suchen.)
 */
export function VaterExercises() {
  /*
   * Der Filter lebt in der **URL**, nicht in `useState`. Ein Startwert aus der Query hätte gereicht, um von
   * der Anlege-Seite zurückzukommen – aber dann laufen Adresse und Ansicht auseinander: der Nav-Eintrag
   * „📚 Übungen" führt auf `/vater/exercises` *ohne* Query, ohne die Route neu zu montieren. Der Filter
   * hätte still weitergegolten, während die Adresse „ungefiltert" behauptet – und ein Neuladen genau dieser
   * Adresse hätte dann etwas anderes gezeigt als der Klick davor. So ist die Adresse teilbar und ehrlich.
   */
  const [params, setParams] = useSearchParams();
  const subjectId: number | "" = Number(params.get("subjectId")) || "";
  const chapterId: number | "" = Number(params.get("chapterId")) || "";
  /** Filter setzen = Adresse setzen. `replace`, damit Filtern keine Historie aus Zwischenständen baut. */
  function setFilter(next: { subjectId: number | ""; chapterId: number | "" }) {
    const q = new URLSearchParams();
    if (next.subjectId !== "") q.set("subjectId", String(next.subjectId));
    if (next.chapterId !== "") q.set("chapterId", String(next.chapterId));
    setParams(q, { replace: true });
  }

  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const chapters = useAsync<ChapterResponse[]>(
    () => (subjectId ? api.chapters(Number(subjectId)) : Promise.resolve([])), [subjectId]);

  // Routen-Segment und Anzeigename der Typen kommen vom Server (Typ-Manifest), nicht aus einer Tabelle hier.
  const types = useExerciseTypes();
  const typeLabel = (t: string) => types?.label(t) ?? t;

  // Verwaltung zeigt standardmäßig nur eigene Übungen (mineOnly); optional auch die geteilte Bibliothek.
  const [showShared, setShowShared] = useState(false);
  // Bearbeiten-Dialog bzw. Testmodus: die aktuell offene Übung.
  const [editing, setEditing] = useState<ExerciseSummary | null>(null);
  const [preview, setPreview] = useState<{ id: number; title: string } | null>(null);

  // Sortierung (Whitelist title/type/grade/source/created) + Paginierung; Fach und Kapitel werden
  // server-seitig gefiltert, damit die Seitenzählung stimmt – kein In-Memory-Filter.
  const [sort, setSort] = useState<ExerciseSortKey>("title");
  const [dir, setDir] = useState<SortDir>("asc");
  const [skip, setSkip] = useState(0);
  const existing = useAsync<Paged<ExerciseSummary>>(
    () => (subjectId
      ? api.searchExercises({
        subjectId: Number(subjectId),
        chapterId: chapterId !== "" ? Number(chapterId) : undefined,
        mineOnly: !showShared, sort, dir, skip, take: PAGE_SIZE,
      })
      : Promise.resolve({ items: [], total: 0 })),
    [subjectId, chapterId, showShared, sort, dir, skip]);
  // Filter-/Sortier-Wechsel springt auf Seite 1 zurück (sonst leere Seite jenseits des Bestands). Der Reset
  // geschieht in der Render-Phase (nicht per Effekt), damit die Liste nicht erst mit altem skip nachlädt.
  const filterKey = `${subjectId}|${chapterId}|${showShared}|${sort}|${dir}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (prevFilterKey !== filterKey) { setPrevFilterKey(filterKey); setSkip(0); }

  /** Die Auswahl wandert als Query mit – die Anlege-Seite startet damit im richtigen Kapitel. */
  const createHref = `/vater/exercises/neu${subjectId ? `?subjectId=${subjectId}${chapterId ? `&chapterId=${chapterId}` : ""}` : ""}`;

  return (
    <>
    <div className="row" style={{ alignItems: "center", gap: 8 }}>
      <h2 className="h-section">Übungen verwalten</h2>
      <Link to={createHref} className="btn inline-btn"
        style={{ width: "auto", marginLeft: "auto", textDecoration: "none", textAlign: "center" }}>
        + Neue Übung
      </Link>
    </div>

    {/*
      Katalog und Lückentext-Store sind eigene Bereiche (Anmerkung 12) – sie tragen mehrere Übungen. Die
      Wege dorthin bleiben hier, damit man sie nicht in der Navigation suchen muss.
    */}
    <div className="row" style={{ gap: 8, flexWrap: "wrap" }}>
      <Link to="/vater/katalog" className="btn ghost inline-btn"
        style={{ width: "auto", textDecoration: "none", textAlign: "center" }}>🗂️ Katalog verwalten</Link>
      <Link to="/vater/lueckentexte" className="btn ghost inline-btn"
        style={{ width: "auto", textDecoration: "none", textAlign: "center" }}>📄 Lückentexte verwalten</Link>
    </div>

    <section className="card">
      <h3 style={{ marginTop: 0 }}>Suchen</h3>
      <div className="form-grid">
        <div className="field">
          <label htmlFor="ex-subject">Fach</label>
          <select id="ex-subject" aria-label="Fach" value={subjectId}
            onChange={(e) => setFilter({ subjectId: e.target.value ? Number(e.target.value) : "", chapterId: "" })}>
            <option value="">– wählen –</option>
            {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ex-chapter">Kapitel <span className="muted">(optional)</span></label>
          <select id="ex-chapter" aria-label="Kapitel" value={chapterId} disabled={!subjectId}
            onChange={(e) => setFilter({ subjectId, chapterId: e.target.value ? Number(e.target.value) : "" })}>
            <option value="">– alle –</option>
            {chapters.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
      </div>
      {/* Verwaltung = eigene Übungen; bei Bedarf die geteilte Bibliothek anderer Väter einblenden. */}
      <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13, marginTop: 10 }}>
        <input type="checkbox" checked={showShared} onChange={(e) => setShowShared(e.target.checked)} />
        geteilte Übungen anderer Väter anzeigen
      </label>
    </section>

    {subjectId === "" ? <div className="banner">Wähle ein Fach, um seine Übungen zu sehen.</div> : (
      <section className="card">
        <div className="row" style={{ alignItems: "center", gap: 8, marginBottom: 4 }}>
          <h3 style={{ margin: 0 }}>Übungen <span className="muted">({existing.data?.total ?? 0})</span></h3>
        </div>
        <div className="row" style={{ marginBottom: 8 }}>
          <SortControl<ExerciseSortKey>
            options={[
              { key: "title", label: "Titel" }, { key: "type", label: "Typ" }, { key: "grade", label: "Klasse" },
              { key: "source", label: "Quelle" }, { key: "created", label: "Erstellt" },
            ]}
            value={sort} dir={dir} onChange={(k, d) => { setSort(k); setDir(d); }} />
        </div>
        {existing.loading ? <div className="loading">Lade…</div>
          : (existing.data?.items.length ?? 0) === 0 ? <div className="muted">Noch keine Übungen.</div> : (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {existing.data?.items.map((e) => (
              <ExerciseManageRow key={e.id} exercise={e} subjectId={Number(subjectId)}
                route={types?.route(e.type) ?? null} label={typeLabel(e.type)} onChanged={existing.reload}
                onPreview={() => setPreview({ id: e.id, title: e.title })} onEdit={() => setEditing(e)} />
            ))}
          </div>
        )}
        {existing.data && <Pager skip={skip} take={PAGE_SIZE} total={existing.data.total} onSkip={setSkip} />}
      </section>
    )}

    {preview && <ExercisePreviewModal exerciseId={preview.id} title={preview.title} onClose={() => setPreview(null)} />}
    {editing && (
      <ExerciseEditModal
        exercise={editing}
        onClose={() => setEditing(null)}
        // Nur die Liste auffrischen, den Dialog offen lassen: Beschreibung und Inhalt sind getrennte
        // Speicher-Schritte, und ein zuklappender Dialog nähme die Bestätigung gleich wieder mit.
        onSaved={existing.reload}
      />
    )}
    </>
  );
}

/** Eine Zeile der Übungsliste mit Verwendungs-Anzeige, Testmodus und Löschen (409-bewusst). */
function ExerciseManageRow({ exercise, subjectId, route, label, onChanged, onPreview, onEdit }: {
  exercise: ExerciseSummary; subjectId: number;
  /** Routen-Segment des Typs aus dem Manifest; `null` = der Server kennt den Typ nicht. */
  route: string | null;
  label: string;
  onChanged: () => void; onPreview: () => void; onEdit: () => void;
}) {
  const [usage, setUsage] = useState<ExerciseUsage | null>(null);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  // Bearbeitbar nur, wenn diese UI einen Editor hat UND der Server eine Route dafür nennt.
  const known = isKnownType(exercise.type) && route !== null;

  async function toggleUsage() {
    if (open) { setOpen(false); return; }
    setBusy(true); setErr(null);
    try { setUsage(await api.exerciseUsage(exercise.id)); setOpen(true); }
    catch (e) { setErr(errorMessage(e)); } finally { setBusy(false); }
  }
  async function remove() {
    if (!confirmAction("Diese Übung wirklich löschen? Zuordnungen in Lehrplänen können betroffen sein.")) return;
    setBusy(true); setErr(null);
    try { await api.deleteExercise(subjectId, exercise.chapterId, route!, exercise.id); onChanged(); }
    catch (e) { setErr(errorMessage(e)); setBusy(false); }
  }

  return (
    <div style={{ border: "1px solid var(--stroke)", borderRadius: 8, padding: "6px 10px" }}>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <span>{exercise.title}</span>
        <span className="muted">· {label}</span>
        {/* Attribution der geteilten Bibliothek: eigene vs. von anderen Vätern erstellt vs. System. */}
        <ExerciseAttribution e={exercise} />
        <span style={{ marginLeft: "auto" }} />
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onPreview}>🧪 Ausprobieren</button>
        {/*
          Bearbeiten und Löschen brauchen Schreibrecht (isOwn = Owner oder Write-Grant) UND einen Typ, den
          diese UI mit einem Editor bedienen kann. Das Routen-Segment kommt aus dem Typ-Manifest; fehlt es,
          liefen die Aufrufe ins Leere.
        */}
        {exercise.isOwn && known && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onEdit}>✏️ Bearbeiten</button>
        )}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={toggleUsage}>Verwendung</button>
        {exercise.isOwn && known && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={remove}>Löschen</button>
        )}
        {exercise.isOwn && !known && (
          <span className="muted" style={{ fontSize: 12 }}>Typ hier nicht bearbeitbar</span>
        )}
        {/*
          Ohne diesen Hinweis war das Fehlen von „Bearbeiten" nicht von einem Fehler zu unterscheiden: Die
          Attribution nennt zwar den Autor, sagt aber nicht, dass daran das Schreibrecht hängt. Die Übung
          bleibt nutzbar – ausprobieren und einem Lehrplan zuweisen geht mit Leserecht.
        */}
        {!exercise.isOwn && (
          <span className="muted" style={{ fontSize: 12 }} title="Bearbeiten und Löschen brauchen Schreibrecht an dieser Übung.">
            kein Schreibrecht – nur ausprobieren &amp; zuweisen
          </span>
        )}
      </div>
      {exercise.description && <div className="muted" style={{ marginTop: 2, fontSize: 13 }}>{exercise.description}</div>}
      {err && <div className="banner err" style={{ marginTop: 6 }}>{err}</div>}
      {open && usage && (
        <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
          <div>Lehrpläne: {usage.plans.length === 0 ? "—" : usage.plans.map((p) => `${p.planTitle} (${p.childName})`).join(", ")}</div>
          <div>Klassenarbeiten: {usage.classTests.length === 0 ? "—" : usage.classTests.map((c) => `${c.title} (${c.childName})`).join(", ")}</div>
          {/*
            Die Zahl ohne Namen: fremde Kinder gehören einem anderen Betreuer und dürfen hier nicht stehen.
            Sie muss aber sichtbar sein – sonst las diese Anzeige „nirgends", während das Löschen mit 409
            scheiterte, und niemand konnte den Widerspruch auflösen (Anmerkung 14).
          */}
          {usage.otherCarersCount > 0 && (
            <div style={{ marginTop: 2 }}>
              Außerdem <strong>{usage.otherCarersCount}×</strong> bei Kindern, die du nicht betreust – darum
              lässt sich diese Übung nicht löschen.
            </div>
          )}
        </div>
      )}
    </div>
  );
}
