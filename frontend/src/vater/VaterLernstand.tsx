import { Fragment, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { MasteryPill } from "../components/MasteryPill";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import type {
  SeriesUnitProgress, ChildResponse, ExerciseProgress, ItemHistoryEntry, ItemProgressResponse,
  MasteryRollup, Paged, SubjectProgress, WordMastery,
} from "../lib/types";

/**
 * Der Lernstand eines Kindes **über alle Lehrpläne hinweg** – die Sicht, die der Positions-Report nicht
 * geben kann: er endet an der Position, hier zählt das Wort.
 *
 * Zwei Zugänge, weil zwei Fragen dahinterstehen:
 *
 * * **„Was sitzt nicht?"** → das Wort-Rollup. Ein Wort steckt oft in mehreren Übungen; der Durchschnitt
 *   über alle Vorkommen sagt mehr als eine einzelne Übungszeile. Standard ist der Filter auf die schwachen
 *   Wörter, denn das ist die Arbeitsliste.
 * * **„Wo im Stoff stehen wir?"** → der Katalog-Drilldown (Fach → Lehrwerk-Unit → Übung → Wort). Das `active`-Flag
 *   trennt aktuell zugewiesene Übungen von nur noch historischen; ohne es sähe abgehängter Stoff wie
 *   laufender aus.
 */
/** Höchste Leitner-Box des plan-übergreifenden Item-Lernstands (Server-Konstante `ItemProgress.MaxBox`). */
const ITEM_MAX_BOX = 5;

export function VaterLernstand() {
  const childId = Number(useParams().childId);
  const child = useAsync<ChildResponse>(() => api.child(childId), [childId]);
  const [tab, setTab] = useState<"weak" | "catalog">("weak");

  return (
    <>
      <div className="row" style={{ marginBottom: 8 }}>
        <h2 className="h-section">Lernstand{child.data ? ` · ${child.data.name}` : ""}</h2>
        <Link to={`/vater/kind/${childId}`} className="btn ghost small"
          style={{ marginLeft: "auto", textDecoration: "none" }}>← Kind</Link>
      </div>

      <div className="row" style={{ gap: 8, marginBottom: 12 }} role="radiogroup" aria-label="Sicht">
        {([["weak", "🩹 Schlecht gelernte Wörter"], ["catalog", "📚 Nach Katalog"]] as const).map(([value, label]) => (
          <button key={value} type="button" className={`pill toggle-pill ${tab === value ? "lime" : ""}`}
            role="radio" aria-checked={tab === value} onClick={() => setTab(value)}>{label}</button>
        ))}
      </div>

      {tab === "weak" ? <WeakWords childId={childId} /> : <CatalogDrilldown childId={childId} />}
    </>
  );
}

// ─── „Was sitzt nicht?" – Rollup je Wort ─────────────────────────────────────

function WeakWords({ childId }: { childId: number }) {
  const [onlyWeak, setOnlyWeak] = useState(true);
  const [skip, setSkip] = useState(0);
  const words = useAsync<Paged<WordMastery>>(
    () => api.childWordMastery(childId, { onlyWeak, skip, take: PAGE_SIZE }), [childId, onlyWeak, skip]);
  const [openWord, setOpenWord] = useState<number | null>(null);

  // Filterwechsel auf Seite 1 zurücksetzen – sonst zeigt der Pager ein Fenster jenseits des Bestands.
  const [prevOnlyWeak, setPrevOnlyWeak] = useState(onlyWeak);
  if (prevOnlyWeak !== onlyWeak) { setPrevOnlyWeak(onlyWeak); setSkip(0); }

  return (
    <section>
      <div className="row" style={{ alignItems: "center", gap: 10 }}>
        <h3 className="h-section" style={{ margin: 0 }}>
          Wörter {words.data ? `(${words.data.total})` : ""}
        </h3>
        <label className="checkline" style={{ marginLeft: "auto" }}>
          <input type="checkbox" checked={onlyWeak} onChange={(e) => setOnlyWeak(e.target.checked)} />
          nur schwache (unter 50 %)
        </label>
      </div>
      <p className="muted" style={{ marginTop: 0 }}>
        Schwächste zuerst. Der Wert ist der Durchschnitt über <em>alle</em> Übungen, in denen das Wort steckt –
        Kandidaten für eine gezielte Wiederholungs-Übung.
      </p>

      {words.error && <div className="banner err">{words.error}</div>}
      {words.loading ? <div className="loading">Lade…</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr>
              <th>Wort</th><th>Übersetzung</th><th>Beherrschung</th>
              <th className="num">kleinste Box</th><th className="num">richtig</th><th />
            </tr></thead>
            <tbody>
              {words.data?.items.map((w) => (
                <WordRow key={w.vocabularyId} word={w} childId={childId}
                  open={openWord === w.vocabularyId}
                  onToggle={() => setOpenWord(openWord === w.vocabularyId ? null : w.vocabularyId)} />
              ))}
              {words.data?.items.length === 0 && (
                <tr><td colSpan={6} className="muted">
                  {onlyWeak
                    ? "Kein Wort unter 50 % – es sitzt alles. 👍"
                    : "Noch kein Lernstand: das Kind hat noch nichts geübt."}
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      {words.data && <Pager skip={skip} take={PAGE_SIZE} total={words.data.total} onSkip={setSkip} />}
    </section>
  );
}

function WordRow({ word, childId, open, onToggle }: {
  word: WordMastery; childId: number; open: boolean; onToggle: () => void;
}) {
  return (
    <>
      <tr>
        <td>{word.word}</td>
        <td className="muted">{word.translation}</td>
        <td>
          {/*
            Über die Ampel gelesen: „eingeführt" ist ein Wort mit Rollup immer, die Box ist die kleinste
            über alle Vorkommen. `maxBox` ist hier die Konstante `ItemProgress.MaxBox` des Servers – das
            Wort-Rollup führt sie nicht mit, die Item-Sicht bekommt denselben Wert je Zeile geliefert.
          */}
          <MasteryPill it={{ introduced: true, box: word.minBox, masteryPercent: word.avgMasteryPercent }} maxBox={ITEM_MAX_BOX} />
          {word.itemCount > 1 && <span className="muted" style={{ marginLeft: 6 }}>in {word.itemCount} Übungen</span>}
        </td>
        <td className="num">{word.minBox}</td>
        <td className="num">{word.seenCount === 0 ? "—" : `${word.correctCount}/${word.seenCount}`}</td>
        <td style={{ textAlign: "right" }}>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
            aria-expanded={open} onClick={onToggle}>{open ? "Schließen" : "Einzelheiten"}</button>
        </td>
      </tr>
      {open && (
        <tr>
          <td colSpan={6} style={{ background: "rgba(255,255,255,.02)" }}>
            <WordDetail childId={childId} vocabularyId={word.vocabularyId} />
          </td>
        </tr>
      )}
    </>
  );
}

/**
 * Die Vorkommen eines Wortes und – auf Wunsch – die Antwort-Historie eines Vorkommens. Die Historie ist der
 * eigentliche Erkenntnisgewinn: sie zeigt, *wie* das Kind geantwortet hat (Tippfehler oder Ratloses).
 *
 * Gefiltert wird hier im Client, weil der Endpunkt nach Übung filtert, nicht nach Wort – für die paar
 * Vorkommen eines Wortes ist das die schlankere Variante als ein Aufruf je Übung.
 */
function WordDetail({ childId, vocabularyId }: { childId: number; vocabularyId: number }) {
  // `take` bis an die Server-Obergrenze (MaxTake = 500): der Endpunkt filtert nach Übung, nicht nach Wort,
  // also wird hier im Client gefiltert. Bei mehr Items als einer Seite kann ein Vorkommen außerhalb des
  // Fensters liegen – das sagt die Meldung unten dann auch, statt „gibt es nicht" zu behaupten.
  const items = useAsync<Paged<ItemProgressResponse>>(
    () => api.childItemProgress(childId, { take: 500 }), [childId]);
  const [openItem, setOpenItem] = useState<number | null>(null);

  if (items.loading) return <div className="loading">Lade Vorkommen…</div>;
  if (items.error) return <div className="banner err">{items.error}</div>;
  const loaded = items.data?.items ?? [];
  const truncated = (items.data?.total ?? 0) > loaded.length;
  const mine = loaded.filter((i) => i.vocabularyId === vocabularyId);

  return (
    <div style={{ padding: "6px 2px" }}>
      <table className="table">
        <thead><tr><th>Übung</th><th>Beherrschung</th><th className="num">richtig</th><th>letzte Antwort</th><th /></tr></thead>
        <tbody>
          {mine.map((i) => (
            // Fragment mit key: das ist das äußerste Element der Map, die Zeilen darin sind Geschwister.
            <Fragment key={i.itemId}>
              <tr>
                <td className="muted">#{i.exerciseId}</td>
                <td><MasteryPill it={{ introduced: i.introducedAt != null, box: i.box, masteryPercent: i.masteryPercent }} maxBox={i.maxBox} /></td>
                <td className="num">{i.seenCount === 0 ? "—" : `${i.correctCount}/${i.seenCount}`}</td>
                <td className="muted">
                  {i.lastAnswerAt ? new Date(i.lastAnswerAt).toLocaleDateString() : "—"}
                  {i.lastCorrect === false && <span className="pill mag" style={{ marginLeft: 6 }}>falsch</span>}
                </td>
                <td style={{ textAlign: "right" }}>
                  <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                    aria-expanded={openItem === i.itemId}
                    onClick={() => setOpenItem(openItem === i.itemId ? null : i.itemId)}>Verlauf</button>
                </td>
              </tr>
              {openItem === i.itemId && (
                <tr>
                  <td colSpan={5}><ItemHistory childId={childId} itemId={i.itemId} /></td>
                </tr>
              )}
            </Fragment>
          ))}
          {mine.length === 0 && (
            <tr><td colSpan={5} className="muted">
              {truncated
                ? `Vorkommen nicht in den geladenen ${loaded.length} von ${items.data?.total} Items – nutze „Nach Katalog".`
                : "Keine Einzel-Vorkommen."}
            </td></tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function ItemHistory({ childId, itemId }: { childId: number; itemId: number }) {
  const history = useAsync<Paged<ItemHistoryEntry>>(() => api.childItemHistory(childId, itemId), [childId, itemId]);

  if (history.loading) return <div className="loading">Lade Verlauf…</div>;
  if (history.error) return <div className="banner err">{history.error}</div>;
  const rows = history.data?.items ?? [];
  if (rows.length === 0) return <p className="muted">Noch keine Antworten protokolliert.</p>;

  return (
    <ul className="muted" style={{ margin: "4px 0" }}>
      {rows.map((h, idx) => (
        <li key={`${h.at}-${idx}`}>
          {new Date(h.at).toLocaleString()} · {h.source === "Test" ? "Test" : "Übung"} ·{" "}
          {h.wasCorrect ? "✓ richtig" : "✗ falsch"}
          {h.givenAnswer ? ` · geantwortet „${h.givenAnswer}"` : ""}
        </li>
      ))}
    </ul>
  );
}

// ─── „Wo im Stoff stehen wir?" – Katalog-Drilldown ────────────────────────────

function CatalogDrilldown({ childId }: { childId: number }) {
  const subjects = useAsync<SubjectProgress[]>(() => api.childLearnSubjects(childId), [childId]);
  const [openSubject, setOpenSubject] = useState<number | null>(null);

  return (
    <section>
      <h3 className="h-section">Fächer</h3>
      <p className="muted" style={{ marginTop: 0 }}>
        Abgeleitet aus den Lehrplänen des Kindes. <span className="pill lime">aktiv</span> = über einen
        laufenden Plan zugewiesen; ohne Marke ist der Stoff nur noch Historie.
      </p>
      {subjects.error && <div className="banner err">{subjects.error}</div>}
      {subjects.loading ? <div className="loading">Lade…</div> : (
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {subjects.data?.map((s) => (
            <div key={s.subjectId} className="card" style={{ padding: "8px 12px" }}>
              <div className="row" style={{ alignItems: "center", gap: 8 }}>
                <b>{s.name}</b>
                {s.active && <span className="pill lime">aktiv</span>}
                <span className="muted">{s.seriesUnitCount} Units · {s.exerciseCount} Übungen</span>
                <span style={{ marginLeft: "auto" }} />
                <RollupSummary r={s.progress} />
                <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                  aria-expanded={openSubject === s.subjectId}
                  onClick={() => setOpenSubject(openSubject === s.subjectId ? null : s.subjectId)}>
                  {openSubject === s.subjectId ? "Schließen" : "Units"}
                </button>
              </div>
              {openSubject === s.subjectId && <SeriesUnits childId={childId} subjectId={s.subjectId} />}
            </div>
          ))}
          {subjects.data?.length === 0 && (
            <p className="muted">Noch kein Stoff zugewiesen – lege einen Lehrplan mit Positionen an.</p>
          )}
        </div>
      )}
    </section>
  );
}

function SeriesUnits({ childId, subjectId }: { childId: number; subjectId: number }) {
  const units = useAsync<SeriesUnitProgress[]>(() => api.childLearnSeriesUnits(childId, subjectId), [childId, subjectId]);
  const [open, setOpen] = useState<number | null>(null);

  if (units.loading) return <div className="loading">Lade Units…</div>;
  if (units.error) return <div className="banner err">{units.error}</div>;

  return (
    <div style={{ marginTop: 8, paddingLeft: 12, borderLeft: "2px solid var(--stroke)" }}>
      {units.data?.map((u) => (
        <div key={u.seriesUnitId} style={{ marginTop: 6 }}>
          <div className="row" style={{ alignItems: "center", gap: 8 }}>
            <span>{u.name}</span>
            {u.active && <span className="pill lime">aktiv</span>}
            <span className="muted">{u.exerciseCount} Übungen</span>
            <span style={{ marginLeft: "auto" }} />
            <RollupSummary r={u.progress} />
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-expanded={open === u.seriesUnitId}
              onClick={() => setOpen(open === u.seriesUnitId ? null : u.seriesUnitId)}>
              {open === u.seriesUnitId ? "Schließen" : "Übungen"}
            </button>
          </div>
          {open === u.seriesUnitId && <Exercises childId={childId} subjectId={subjectId} seriesUnitId={u.seriesUnitId} />}
        </div>
      ))}
      {units.data?.length === 0 && <p className="muted">Keine Units mit Stoff.</p>}
    </div>
  );
}

function Exercises({ childId, subjectId, seriesUnitId }: { childId: number; subjectId: number; seriesUnitId: number }) {
  const exercises = useAsync<ExerciseProgress[]>(
    () => api.childLearnExercises(childId, subjectId, seriesUnitId), [childId, subjectId, seriesUnitId]);
  const [open, setOpen] = useState<number | null>(null);

  if (exercises.loading) return <div className="loading">Lade Übungen…</div>;
  if (exercises.error) return <div className="banner err">{exercises.error}</div>;

  return (
    <div style={{ marginTop: 6, paddingLeft: 12, borderLeft: "2px solid var(--stroke)" }}>
      {exercises.data?.map((e) => (
        <div key={e.exerciseId} style={{ marginTop: 4 }}>
          <div className="row" style={{ alignItems: "center", gap: 8 }}>
            <span className="muted">{e.title}</span>
            {e.active && <span className="pill lime">aktiv</span>}
            <span style={{ marginLeft: "auto" }} />
            <RollupSummary r={e.progress} />
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-expanded={open === e.exerciseId}
              onClick={() => setOpen(open === e.exerciseId ? null : e.exerciseId)}>
              {open === e.exerciseId ? "Schließen" : "Wörter"}
            </button>
          </div>
          {open === e.exerciseId && (
            <ExerciseItems childId={childId} subjectId={subjectId} seriesUnitId={seriesUnitId} exerciseId={e.exerciseId} />
          )}
        </div>
      ))}
      {exercises.data?.length === 0 && <p className="muted">Keine Vokabelübungen in dieser Unit.</p>}
    </div>
  );
}

function ExerciseItems({ childId, subjectId, seriesUnitId, exerciseId }: {
  childId: number; subjectId: number; seriesUnitId: number; exerciseId: number;
}) {
  const items = useAsync<ItemProgressResponse[]>(
    () => api.childLearnItems(childId, subjectId, seriesUnitId, exerciseId), [childId, subjectId, seriesUnitId, exerciseId]);

  if (items.loading) return <div className="loading">Lade Wörter…</div>;
  if (items.error) return <div className="banner err">{items.error}</div>;

  return (
    <div style={{ overflowX: "auto", marginTop: 4 }}>
      <table className="table">
        <thead><tr><th>Wort</th><th>Übersetzung</th><th>Beherrschung</th><th className="num">richtig</th><th>zuletzt</th></tr></thead>
        <tbody>
          {items.data?.map((i) => (
            <tr key={i.itemId}>
              <td>{i.front}</td>
              <td className="muted">{i.back}</td>
              <td><MasteryPill it={{ introduced: i.introducedAt != null, box: i.box, masteryPercent: i.masteryPercent }} maxBox={i.maxBox} /></td>
              <td className="num">{i.seenCount === 0 ? "—" : `${i.correctCount}/${i.seenCount}`}</td>
              <td className="muted">{i.lastAnswerAt ? new Date(i.lastAnswerAt).toLocaleDateString() : "—"}</td>
            </tr>
          ))}
          {items.data?.length === 0 && <tr><td colSpan={5} className="muted">Keine Wörter.</td></tr>}
        </tbody>
      </table>
    </div>
  );
}

/** Der Rollup in einer Zeile: was ist eingeführt, was sitzt, was wackelt. */
function RollupSummary({ r }: { r: MasteryRollup }) {
  if (r.totalItems === 0) return <span className="muted">keine Wörter</span>;
  return (
    <span className="row" style={{ gap: 6, alignItems: "center" }}>
      <span className="muted">{r.introducedItems}/{r.totalItems} begonnen</span>
      <span className="pill lime">{r.masteredItems} sitzen</span>
      {r.weakItems > 0 && <span className="pill mag">{r.weakItems} schwach</span>}
      <span className="muted">Ø {r.avgMasteryPercent}%</span>
    </span>
  );
}
