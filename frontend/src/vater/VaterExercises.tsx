import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { ExerciseAttribution } from "./ExerciseAttribution";
import { ExerciseEditModal } from "./ExerciseEditModal";
import { ExercisePreviewModal } from "./ExercisePreviewModal";
import { ExerciseRatingPanel, ratingSummary } from "./ExerciseRatingPanel";
import { PAGE_SIZE, Pager, SortControl } from "../components/ListControls";
import type {
  ExerciseSortKey, ExerciseSummary, ExerciseUsage, Paged, SeriesUnitResponse, SortDir, SubjectResponse,
  TextbookSeriesResponse,
} from "../lib/types";
import { isKnownType } from "./exerciseConfig";
import { useExerciseTypes } from "../lib/exerciseTypes";
import { ListenAusfall } from "../components/ListenAusfall";

/**
 * Übungen **verwalten**: suchen, ausprobieren, bearbeiten, löschen.
 *
 * Diese Route trug früher vier Anliegen zugleich – Katalog, Lückentext-Store, Anlege-Formular und diese
 * Liste, alles in *einem* `<form>` (Anmerkung 11). Geblieben ist die Daueraufgabe; das Anlegen ist ein
 * abgeschlossener Vorgang und liegt auf `/vater/exercises/neu`. Siehe
 * docs/vater-informationsarchitektur-plan.md.
 *
 * Fach und Lehrwerk-Unit sind hier **Filter**, nicht Pflicht: die Liste erscheint, sobald ein Fach gewählt
 * ist – die Unit schränkt weiter ein. (Vorher blieb die Seite leer, bis auch ein Kapitel gewählt war, weil
 * die Auswahl zum Anlegen gehörte, nicht zum Suchen.) Seit B-106 hängt jede Übung an einer Lehrwerk-Unit
 * statt an einem Kapitel; die Reihe ist hier nur die Zwischenstufe der Auswahl (Reihe → Unit).
 */
/**
 * Die Frage vor „Löschen". Der Server löscht nur eine unbenutzte Übung (sonst `exercise_in_use`, dessen Text das sagt) –
 * Lehrpläne, Klassenarbeiten und Ziele berührt das Löschen also nie (B-499, wie B-418). Den Lernstand der Kinder an
 * ihren Vokabeln aber schon: `ExerciseItem` → `ItemProgress` ist Cascade – das sagt die Frage.
 */
export const DELETE_EXERCISE_QUESTION = "Diese Übung wirklich löschen? Sie verschwindet samt Inhalten aus dem Katalog – "
  + "was Kinder darin gelernt haben, geht im Lernstand mit verloren.";

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
  const seriesId: number | "" = Number(params.get("seriesId")) || "";
  const seriesUnitId: number | "" = Number(params.get("seriesUnitId")) || "";
  // Der Schalter „geteilte Übungen" lebt ebenfalls in der Adresse (Review-Fund zu B-428): die Übungszahl einer Unit zählt
  // ALLE Übungen, und ihr Link muss eine Liste öffnen können, die dieselben zeigt – nicht nur die eigenen.
  //
  // Zusätzlich ein eigener Zustand (B-450): der Router rendert eine Adress-Änderung in einer Transition
  // (`v7_startTransition`, main.tsx), und bis dahin setzte React den kontrollierten Haken auf den alten Wert zurück – der
  // Klick schien nichts zu tun. Der Zustand folgt dem Klick sofort und gleicht sich mit der Adresse ab (Link, Zurück-Taste) –
  // im Render, wie `prevFilterKey` unten, damit kein Commit den alten Haken zeigt. Die Pulldowns flackern in derselben
  // Transition kurz zurück, aber niemand liest ihren Wert direkt nach dem Ereignis; ein eigener Zustand je Pulldown lohnt nicht.
  const sharedInUrl = params.get("shared") === "1";
  const [showShared, setShowShared] = useState(sharedInUrl);
  const [prevSharedInUrl, setPrevSharedInUrl] = useState(sharedInUrl);
  if (prevSharedInUrl !== sharedInUrl) { setPrevSharedInUrl(sharedInUrl); setShowShared(sharedInUrl); }
  /**
   * Filter setzen = Adresse setzen. `replace`, damit Filtern keine Historie aus Zwischenständen baut.
   *
   * Nur die übergebenen Schlüssel ändern sich, der Rest kommt aus der **aktuellen** Adresse (`window.location.search`), nicht aus
   * den Werten dieses Renders (B-450, Vorsorge): solange die Transition der ersten Änderung läuft, trügen die Render-Werte noch
   * den alten Stand – eine zweite Änderung kurz danach schriebe ihn zurück. `navigate` schreibt die History sofort, gerendert
   * wird später; `setParams(prev => …)` hilft nicht, react-router gibt `prev` aus dem Render. Reproduziert ist dieser Weg nicht
   * (sechs Wiederholungen grün); der belegte Fehler war das Zurückspringen des Hakens oben.
   */
  function setFilter(patch: { subjectId?: number | ""; seriesId?: number | ""; seriesUnitId?: number | ""; shared?: boolean }) {
    const q = new URLSearchParams(window.location.search);
    for (const key of ["subjectId", "seriesId", "seriesUnitId"] as const) {
      if (!(key in patch)) continue;
      const value = patch[key];
      if (value === "" || value === undefined) q.delete(key);
      else q.set(key, String(value));
    }
    if (patch.shared !== undefined) {
      if (patch.shared) q.set("shared", "1");
      else q.delete("shared");
    }
    setParams(q, { replace: true });
  }

  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const series = useAsync<TextbookSeriesResponse[]>(
    () => api.textbookSeries(subjectId ? { subjectId: Number(subjectId) } : {}), [subjectId]);
  const units = useAsync<SeriesUnitResponse[]>(
    () => (seriesId ? api.seriesUnits(Number(seriesId)) : Promise.resolve([])), [seriesId]);

  // Routen-Segment und Anzeigename der Typen kommen vom Server (Typ-Manifest), nicht aus einer Tabelle hier.
  const types = useExerciseTypes();
  const typeLabel = types.label;

  // Verwaltung zeigt standardmäßig nur eigene Übungen (mineOnly); optional auch die geteilte Bibliothek (`?shared=1`, oben).
  // Bearbeiten-Dialog bzw. Testmodus: die aktuell offene Übung.
  const [editing, setEditing] = useState<ExerciseSummary | null>(null);
  const [preview, setPreview] = useState<{ id: number; title: string } | null>(null);

  // Sortierung (Whitelist title/type/grade/source/created) + Paginierung; Fach und Kapitel werden
  // server-seitig gefiltert, damit die Seitenzählung stimmt – kein In-Memory-Filter.
  const [sort, setSort] = useState<ExerciseSortKey>("title");
  const [dir, setDir] = useState<SortDir>("asc");
  const [skip, setSkip] = useState(0);
  // B-431: ohne Fach lädt die Seite trotzdem – die eigenen Übungen aller Fächer (mit dem Schalter: alle). Der Server
  // braucht kein Fach; die Werkstatt zählt mit derselben Abfrage (`mineOnly`), darum stimmt ihre Zahl mit der Liste überein.
  const existing = useAsync<Paged<ExerciseSummary>>(
    () => api.searchExercises({
      subjectId: subjectId !== "" ? Number(subjectId) : undefined,
      seriesUnitId: seriesUnitId !== "" ? Number(seriesUnitId) : undefined,
      mineOnly: !showShared, sort, dir, skip, take: PAGE_SIZE,
    }),
    [subjectId, seriesUnitId, showShared, sort, dir, skip]);
  /** Ohne Fach-Filter stehen Übungen aus mehreren Fächern in einer Liste – dann nennt jede Zeile ihr Fach. */
  const subjectNameOf = (id: number | null | undefined) => fachDerZeile(subjectId, id, subjects.data);
  // Filter-/Sortier-Wechsel springt auf Seite 1 zurück (sonst leere Seite jenseits des Bestands). Der Reset
  // geschieht in der Render-Phase (nicht per Effekt), damit die Liste nicht erst mit altem skip nachlädt.
  const filterKey = `${subjectId}|${seriesUnitId}|${showShared}|${sort}|${dir}`;
  const [prevFilterKey, setPrevFilterKey] = useState(filterKey);
  if (prevFilterKey !== filterKey) { setPrevFilterKey(filterKey); setSkip(0); }

  /** Die Auswahl wandert als Query mit – die Anlege-Seite startet damit in derselben Reihe/Unit. */
  const createHref = `/vater/exercises/neu${params.size > 0 ? `?${params}` : ""}`;

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
      <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />
      <div className="form-grid">
        <div className="field">
          <label htmlFor="ex-subject">Fach</label>
          <select id="ex-subject" aria-label="Fach" value={subjectId}
            onChange={(e) => setFilter({ subjectId: e.target.value ? Number(e.target.value) : "", seriesId: "", seriesUnitId: "" })}>
            <option value="">– alle Fächer –</option>
            {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ex-series">Reihe <span className="muted">(optional)</span></label>
          <select id="ex-series" aria-label="Reihe" value={seriesId} disabled={!subjectId}
            onChange={(e) => setFilter({ seriesId: e.target.value ? Number(e.target.value) : "", seriesUnitId: "" })}>
            <option value="">– alle –</option>
            {series.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ex-unit">Unit <span className="muted">(optional)</span></label>
          <select id="ex-unit" aria-label="Unit" value={seriesUnitId} disabled={!seriesId}
            onChange={(e) => setFilter({ seriesUnitId: e.target.value ? Number(e.target.value) : "" })}>
            <option value="">– alle –</option>
            {units.data?.map((u) => <option key={u.id} value={u.id}>{u.label}</option>)}
          </select>
        </div>
      </div>
      {/* Verwaltung = eigene Übungen; bei Bedarf die geteilte Bibliothek anderer Väter einblenden. */}
      <label className="row" style={{ gap: 6, alignItems: "center", fontSize: 13, marginTop: 10 }}>
        <input type="checkbox" checked={showShared}
          onChange={(e) => { setShowShared(e.target.checked); setFilter({ shared: e.target.checked }); }} />
        geteilte Übungen anderer Väter anzeigen
      </label>
    </section>

      <section className="card">
        <div className="row" style={{ alignItems: "center", gap: 8, marginBottom: 4 }}>
          <h3 style={{ margin: 0 }}>
            {subjectId !== "" ? "Übungen" : showShared ? "Alle Übungen" : "Meine Übungen"}{" "}
            <span className="muted">({existing.data?.total ?? 0})</span>
          </h3>
        </div>
        <div className="row" style={{ marginBottom: 8 }}>
          <SortControl<ExerciseSortKey>
            options={[
              { key: "title", label: "Titel" }, { key: "type", label: "Typ" }, { key: "grade", label: "Klasse" },
              { key: "source", label: "Quelle" }, { key: "created", label: "Erstellt" },
              // B-12: unbewertete Übungen stehen in beiden Richtungen hinten (Server-Regel, entschieden 2026-10-26).
              { key: "rating", label: "Bewertung" },
            ]}
            value={sort} dir={dir} onChange={(k, d) => { setSort(k); setDir(d); }} />
        </div>
        {/*
          Der Platzhalter gilt nur fürs **erste** Laden. Jede Änderung an einer Zeile frischt die Liste auf,
          und ein „Lade…" an dieser Stelle hängt dabei sämtliche Zeilen aus: die aufgeklappte
          Verwendungs-Anzeige verschwand mitten in der Arbeit, samt ihres Zurückziehen-Schalters. Dieselbe
          Falle wie in VaterKatalog – `useAsync` behält `data` über ein `reload`, also darf der Platzhalter
          nur greifen, solange es noch keine gibt.
        */}
        {/* Ohne Manifest keine Route, ohne Route sind Bearbeiten und Löschen je Zeile gesperrt – der Grund
            gehört über die Zeilen, nicht als `title` an die grauen Knöpfe (B-160, B-215). */}
        <ListenAusfall was="Die Übungstypen" fehler={types.error} laedt={types.loading} onRetry={types.reload} />
        {/* Seit B-431 lädt die Liste beim ersten Aufruf: ein Ausfall darf nicht als „Noch keine Übungen." erscheinen. */}
        <ListenAusfall was="Die Übungen" fehler={existing.error} laedt={existing.loading} onRetry={existing.reload} schluessel={filterKey} />
        {existing.loading && existing.data === null ? <div className="loading">Lade…</div>
          : (existing.data?.total ?? 0) === 0 ? (existing.error ? null : <div className="muted">Noch keine Übungen.</div>) : (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {existing.data?.items.map((e) => (
              <ExerciseManageRow key={e.id} exercise={e} subjectName={subjectNameOf(e.subjectId)}
                route={types.route(e.type)} label={typeLabel(e.type)} onChanged={existing.reload}
                onPreview={() => setPreview({ id: e.id, title: e.title })} onEdit={() => setEditing(e)} />
            ))}
          </div>
        )}
        {existing.data && <Pager skip={skip} take={PAGE_SIZE} total={existing.data.total} onSkip={setSkip} busy={existing.loading} />}
      </section>

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

/**
 * Wo eine Übung verwendet wird – aus **zwei** Blickwinkeln, weil es zwei Sorten Nutzer gibt.
 *
 * Ein Vater sieht die Pläne seiner Kinder und will wissen, was er anfasst, bevor er ändert. Ein Creator
 * ohne eigene Kinder – ein Lehrer oder eine KI-Creator-App – sieht in diesen Listen **nie** etwas: er
 * betreut niemanden. Drei „—"-Zeilen (Pläne, Klassenarbeiten, Ziele) lesen sich für ihn wie „wird nicht benutzt", und genau das ist die
 * Falschaussage, um die es in Anmerkung 14 ging. Für ihn ist die Zahl der fremden Kinder die Hauptaussage,
 * nicht der Nachsatz – darum tritt sie an die Stelle der leeren Listen.
 *
 * Namen fremder Kinder tauchen in **keinem** der beiden Fälle auf: sie gehören ihrem Betreuer.
 */
export function UsagePanel({ usage, exercise, busy, onToggleSharing }: {
  usage: ExerciseUsage;
  exercise: ExerciseSummary;
  busy: boolean;
  onToggleSharing: () => void;
}) {
  const others = usage.otherLearnersCount;
  const childWord = others === 1 ? "Kind" : "Kindern";
  // Ziel-Etappen sperren das Löschen wie eine Position (B-409). Ohne sie hier stand bei einer Übung, die nur an einem
  // Ziel hängt, „Löschen ist möglich" – und der Server antwortete 409.
  const hasOwn = usage.plans.length > 0 || usage.classTests.length > 0 || usage.goals.length > 0;
  // Der Ausweg, wenn Löschen gesperrt ist – aber nur für den Owner, denn nur er darf zurückziehen.
  const wayOut = exercise.isOwner && exercise.executePublic
    ? " Zurückziehen kannst du sie trotzdem: das stoppt neue Zuweisungen, ohne laufende Pläne zu brechen."
    : "";

  return (
    <div className="muted" style={{ marginTop: 6, fontSize: 13 }}>
      {hasOwn ? (
        <>
          <div>Lehrpläne: {usage.plans.length === 0 ? "—" : usage.plans.map((p) => `${p.planTitle} (${p.childName})`).join(", ")}</div>
          <div>Klassenarbeiten: {usage.classTests.length === 0 ? "—" : usage.classTests.map((c) => `${c.title} (${c.childName})`).join(", ")}</div>
          <div>Ziele: {usage.goals.length === 0 ? "—" : usage.goals.map((g) =>
            `${g.objectiveTitle}${g.keyResultTitle ? ` – ${g.keyResultTitle}` : ""} (${g.childName})`).join(", ")}</div>
          {others > 0 && (
            <div style={{ marginTop: 2 }}>
              Außerdem bei <strong>{others}</strong> {childWord} anderer Betreuer im Einsatz.
            </div>
          )}
        </>
      ) : (
        <div>
          {others === 0
            ? "Wird derzeit nirgends eingesetzt – weder bei deinen Kindern noch bei anderen. Löschen ist möglich."
            : <>Im Einsatz bei <strong>{others}</strong> {childWord} – nicht von dir betreut, darum ohne Namen
              und ohne Plan-Titel. Löschen ist deshalb gesperrt.{wayOut}</>}
        </div>
      )}

      {/*
        Die Rücknahme steht **hier** und nicht in der Zeile: sie ist eine seltene Verwaltungs-Entscheidung,
        und als fünfter Knopf neben „Ausprobieren"/„Bearbeiten" brach die Zeile um – dieselbe Überfüllung,
        gegen die Anmerkung 11 ging. Hier steht sie neben der Auskunft, die sie begründet.
      */}
      {exercise.isOwner && (
        <div className="row" style={{ marginTop: 8, gap: 8, alignItems: "center", flexWrap: "wrap" }}>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
            onClick={onToggleSharing}>
            {exercise.executePublic ? "⬇️ Zurückziehen" : "⬆️ Wieder freigeben"}
          </button>
          <span style={{ fontSize: 12 }}>
            {exercise.executePublic
              ? "Nimmt sie aus dem Verkehr: niemand kann sie mehr neu zuweisen. Laufende Lehrpläne bleiben unberührt."
              : "Derzeit zurückgezogen – nur wer ein ausdrückliches Recht hält, kann sie noch zuweisen."}
          </span>
        </div>
      )}
    </div>
  );
}

/** Eine Zeile der Übungsliste mit Verwendungs-Anzeige, Testmodus und Löschen (409-bewusst). */
/**
 * B-431: das Fach einer Zeile, wenn die Liste mehrere Fächer mischt (kein Fach-Filter); sonst `null` – dann steht es im Filter.
 * „ohne Fach" nur, wenn die Übung wirklich keins hat – nicht, solange die Fächer laden oder ihr Abruf scheiterte (Review-Fund):
 * „lädt", „gescheitert" und „hat keins" sind drei Aussagen, nicht eine.
 */
export function fachDerZeile(filterSubjectId: number | "", id: number | null | undefined, subjects: SubjectResponse[] | null): string | null {
  if (filterSubjectId !== "") return null;
  if (id == null) return "ohne Fach";
  if (subjects === null) return null;
  return subjects.find((x) => x.id === id)?.name ?? `Fach #${id}`;
}

function ExerciseManageRow({ exercise, subjectName = null, route, label, onChanged, onPreview, onEdit }: {
  exercise: ExerciseSummary;
  /** Das Fach der Übung – nur gesetzt, wenn die Liste mehrere Fächer mischt (B-431); sonst steht es schon im Filter. */
  subjectName?: string | null;
  /** Routen-Segment des Typs aus dem Manifest; `null` = der Server kennt den Typ nicht. */
  route: string | null;
  label: string;
  onChanged: () => void; onPreview: () => void; onEdit: () => void;
}) {
  const [usage, setUsage] = useState<ExerciseUsage | null>(null);
  const [open, setOpen] = useState(false);
  const [ratingsOpen, setRatingsOpen] = useState(false);
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
    if (!confirmAction(DELETE_EXERCISE_QUESTION)) return;
    setBusy(true); setErr(null);
    try { await api.deleteExercise(exercise.seriesId, exercise.seriesUnitId, route!, exercise.id); onChanged(); }
    catch (e) { setErr(errorMessage(e)); setBusy(false); }
  }
  /*
   * Freigeben / Zurückziehen – die Gegenbewegung zum Veröffentlichen und der **einzige** Weg, Material aus
   * dem Verkehr zu nehmen: Löschen verweigert eine benutzte Übung, und das zu Recht, denn laufende Pflichten
   * dürfen nicht unter dem Kind wegbrechen.
   *
   * Nur das Zurückziehen fragt nach. Freigeben ist harmlos und jederzeit umkehrbar; Zurückziehen wirkt auf
   * fremde Familien, die die Übung ab dann nicht mehr zuweisen können.
   */
  async function toggleSharing() {
    const withdraw = exercise.executePublic;
    if (withdraw && !confirmAction(
      "Übung zurückziehen? Andere Väter können sie dann nicht mehr neu zuweisen. "
      + "Laufende Lehrpläne bleiben unberührt – wer sie heute lernt, lernt sie weiter.")) return;
    setBusy(true); setErr(null);
    try { await api.setExerciseSharing(exercise.id, !withdraw); onChanged(); }
    catch (e) { setErr(errorMessage(e)); }
    finally { setBusy(false); }
  }

  return (
    // Eine benannte Gruppe je Übung: Knöpfe wie „★ Bewertungen" stehen in jeder Zeile gleich da, erst der
    // Titel der Gruppe sagt dem Screenreader, zu welcher Übung sie gehören.
    <div role="group" aria-label={exercise.title} style={{ border: "1px solid var(--stroke)", borderRadius: 8, padding: "6px 10px" }}>
      <div className="row" style={{ alignItems: "center", gap: 8 }}>
        <span>{exercise.title}</span>
        <span className="muted">· {label}</span>
        {subjectName && <span className="muted">· Fach: {subjectName}</span>}
        {/* Attribution der geteilten Bibliothek: eigene vs. von anderen Vätern erstellt vs. System. */}
        <ExerciseAttribution e={exercise} />
        {/*
          Der Freigabe-Stand steht sichtbar in der Zeile, nicht nur im Dialog: „zurückgezogen" ist der
          Zustand, in dem sich eine Übung anders verhält als alle anderen (niemand kann sie neu zuweisen),
          und ein unsichtbarer Sonderzustand wird später als Fehler gemeldet. Für **jeden** sichtbar, nicht
          nur für den Owner – wer sie nicht zuweisen kann, soll den Grund sehen.
        */}
        {/* B-12: das Urteil der anderen Creator, damit Bewährtes schon beim Stöbern auffällt. */}
        <span className="muted" style={{ fontSize: 12 }}>{ratingSummary(exercise.averageRating, exercise.ratingCount)}</span>
        {!exercise.executePublic && (
          <span className="pill mag" title="Neue Zuweisungen nur mit ausdrücklichem Recht. Laufende Lehrpläne bleiben unberührt.">
            zurückgezogen
          </span>
        )}
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
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} aria-expanded={ratingsOpen}
          onClick={() => setRatingsOpen((v) => !v)}>★ Bewertungen</button>
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
      {open && usage && <UsagePanel usage={usage} exercise={exercise} busy={busy} onToggleSharing={toggleSharing} />}
      {ratingsOpen && <ExerciseRatingPanel exercise={exercise} onChanged={onChanged} />}
    </div>
  );
}
