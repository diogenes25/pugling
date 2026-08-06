import { Fragment, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { SCHOOL_TYPES } from "../lib/labels";
import { LANGUAGES } from "../lib/languages";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  BookType, CreateSeriesUnitDto, PublisherResponse, SchoolType, SeriesUnitResponse, SubjectResponse,
  TextbookSeriesResponse,
} from "../lib/types";

/** Anzeigename je Buchtyp; Lehrbuch ist der Regelfall und bleibt darum ohne Chip in der Unit-Zeile. */
const BOOK_TYPE_LABEL: Record<BookType, string> = {
  Textbook: "Lehrbuch", Workbook: "Arbeitsheft", TeacherGuide: "Lehrerhandreichung",
};

/**
 * Die Lehrwerke: welcher Stoff im Unterricht überhaupt dran ist.
 *
 * Drei Dinge machen diese Seite mehr als eine Bücherliste:
 *
 * 1. **Die Reihe ist geteilt.** „Access" wird einmal gepflegt; das Lehrbuch eines Kindes und ein
 *    Creator-Profil zeigen auf denselben Eintrag. Nur dadurch lässt sich der Fachlehrer zu einem Kind
 *    *finden* statt raten. Lesen darf jeder, ändern nur wer sie angelegt hat.
 * 2. **Der Verlag ist ein eigenes, geteiltes Vokabular** – wie die Reihe selbst, damit „Cornelsen" und
 *    „cornelsen " nicht als zwei Schreibweisen auseinanderlaufen (B-63).
 * 3. **Der Stoff steht in der Unit.** Themen, Grammatik und Wortschatz einer Unit sind das, was der
 *    KI-Creator liest. Bleiben sie leer, erfindet er den Inhalt – fachlich plausibel, aber am
 *    Unterricht vorbei.
 */
export function VaterLehrwerke() {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [publisherId, setPublisherId] = useState("");
  const [schoolTypes, setSchoolTypes] = useState<SchoolType | "">("");
  const [grade, setGrade] = useState("");
  const list = useAsync<TextbookSeriesResponse[]>(() => api.textbookSeries({
    search: applied || undefined,
    publisherId: publisherId ? Number(publisherId) : undefined,
    schoolTypes: schoolTypes || undefined,
    grade: grade ? Number(grade) : undefined,
  }), [applied, publisherId, schoolTypes, grade]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const publishers = useAsync<PublisherResponse[]>(() => api.publishers(), []);
  const [open, setOpen] = useState<number | null>(null);

  return (
    <>
      <section>
        <h2 className="h-section">Lehrwerke</h2>
        <p className="sub">
          Die Buchreihe verbindet <strong>Kind</strong> und <strong>Fachlehrer</strong>: am Kind hinterlegt
          (Reiter <em>Kind → Unterrichtsmaterial</em>) und am Profil ausgewählt, weiß die App, wessen Stoff
          gerade dran ist. Der Inhalt einer Unit gehört in die Felder Themen/Grammatik/Wortschatz.
        </p>

        <form
          className="row" style={{ gap: 8, marginBottom: 10, flexWrap: "wrap" }}
          onSubmit={(e) => { e.preventDefault(); setApplied(search.trim()); }}
        >
          <input
            aria-label="Lehrwerke durchsuchen" value={search} onChange={(e) => setSearch(e.target.value)}
            placeholder="Suche in Name oder Verlag" style={{ maxWidth: 260 }}
          />
          <select aria-label="Verlag-Filter" value={publisherId} onChange={(e) => setPublisherId(e.target.value)}>
            <option value="">– alle Verlage –</option>
            {publishers.data?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
          <select aria-label="Schulart-Filter" value={schoolTypes}
            onChange={(e) => setSchoolTypes(e.target.value as SchoolType | "")}>
            <option value="">– alle Schularten –</option>
            {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <input
            aria-label="Klassenstufe-Filter" type="number" min={1} max={13} value={grade}
            onChange={(e) => setGrade(e.target.value)} placeholder="Klasse" style={{ maxWidth: 100 }}
          />
          <button type="submit" className="btn ghost small" style={{ width: "auto" }}>Suchen</button>
        </form>

        {list.error && <div className="banner err">{list.error}</div>}
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
          <table className="table">
            <thead><tr><th>Reihe</th><th>Fach</th><th>Schulart</th><th>Band</th><th>Units</th><th /></tr></thead>
            <tbody>
              {list.data?.map((s) => (
                <SeriesRow
                  key={s.id} series={s} subjects={subjects.data ?? []}
                  open={open === s.id} onToggle={() => setOpen(open === s.id ? null : s.id)}
                  onChanged={list.reload}
                />
              ))}
              {list.data?.length === 0 && (
                <tr><td colSpan={6} className="muted">Noch kein Lehrwerk. Lege unten eines an.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      <NewSeries
        subjects={subjects.data ?? []} publishers={publishers.data ?? []}
        onPublisherCreated={publishers.reload} onCreated={list.reload}
      />
    </>
  );
}

function SeriesRow({ series, subjects, open, onToggle, onChanged }: {
  series: TextbookSeriesResponse;
  subjects: SubjectResponse[];
  open: boolean;
  onToggle: () => void;
  onChanged: () => void;
}) {
  const action = useAction();

  async function remove() {
    if (!confirmAction(
      `Reihe „${series.name}" wirklich löschen? Die ${series.unitCount} Unit(s) gehen mit. `
      + "Lehrbücher der Kinder und Profile verlieren nur die Zuordnung und bleiben nutzbar.")) return;
    if (await action.run(() => api.deleteTextbookSeries(series.id))) onChanged();
  }

  const subjectLabel = series.subjectId != null
    ? subjects.find((s) => s.id === series.subjectId)?.name ?? series.subjectName ?? `#${series.subjectId}`
    : series.subjectName;

  // Aggregiert aus den vorhandenen Units – berechnet vom Server (Min/Max von SeriesUnit.Grade).
  const gradeLabel = series.gradeMin == null && series.gradeMax == null
    ? null
    : series.gradeMin === series.gradeMax ? `Kl. ${series.gradeMin}` : `Kl. ${series.gradeMin ?? "?"}–${series.gradeMax ?? "?"}`;

  return (
    <>
      <tr>
        <td>
          {series.name}
          {series.publisherName && <span className="muted"> · {series.publisherName}</span>}
          <div className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{series.slug}</div>
        </td>
        <td>{subjectLabel ?? <span className="muted">–</span>}</td>
        <td>{series.schoolTypes === "None" ? <span className="muted">alle</span> : series.schoolTypes}</td>
        <td>{gradeLabel ?? <span className="muted">–</span>}</td>
        <td>{series.unitCount === 0 ? <span className="pill mag">keine</span> : series.unitCount}</td>
        <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
          <button type="button" className="btn ghost small" style={{ width: "auto" }} onClick={onToggle}
            aria-expanded={open}>
            {open ? "Units zu" : "Units"}
          </button>
          {/* Fremde Reihen bleiben lesbar – der Knopf fehlt, statt später mit 403 zu scheitern. */}
          {series.isOwn && (
            <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy} onClick={remove}>
              Löschen
            </button>
          )}
        </td>
      </tr>
      {action.message && <tr><td colSpan={6}><StatusBanner message={action.message} style={{ marginTop: 0 }} /></td></tr>}
      {open && (
        <tr>
          <td colSpan={6}>
            {!series.isOwn && (
              <p className="muted">
                Diese Reihe hat jemand anderes angelegt – du kannst sie verwenden, aber nicht ändern.
              </p>
            )}
            <UnitPanel series={series} onChanged={onChanged} />
          </td>
        </tr>
      )}
    </>
  );
}

/** Die Units einer Reihe: Band, Buchtyp, Bezeichnung und – der Kern – der Stoff. */
function UnitPanel({ series, onChanged }: { series: TextbookSeriesResponse; onChanged: () => void }) {
  const units = useAsync<SeriesUnitResponse[]>(() => api.seriesUnits(series.id), [series.id]);
  const [editing, setEditing] = useState<number | null>(null);

  return (
    <>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>Units in „{series.name}"</h4>
      {units.error && <div className="banner err">{units.error}</div>}
      {units.data === null ? <div className="loading">Lade…</div> : units.data.length === 0 ? (
        <p className="muted">Noch keine Unit. Ohne Unit kennt der Creator nur den Reihennamen, nicht den Stoff.</p>
      ) : (
        <table className="table">
          <thead><tr><th>Band</th><th>Unit</th><th>Stoff</th><th /></tr></thead>
          <tbody>
            {units.data.map((u) => (
              // Der Key gehört auf das Fragment: die Zeile und ihr Editor sind zusammen EIN Eintrag.
              <Fragment key={u.id}>
                <tr>
                  <td>{u.grade != null ? `Klasse ${u.grade}` : <span className="muted">–</span>}</td>
                  <td>
                    {u.label}
                    {u.bookType !== "Textbook" && <span className="chip" style={{ marginLeft: 6, fontSize: 11 }}>{BOOK_TYPE_LABEL[u.bookType]}</span>}
                  </td>
                  <td>
                    {u.topics.length === 0 && !u.grammar && !u.vocabularyNotes
                      ? <span className="pill mag">kein Stoff hinterlegt</span>
                      : (
                        <div className="muted" style={{ fontSize: 13 }}>
                          {u.topics.length > 0 && (
                            <div className="row" style={{ gap: 4, flexWrap: "wrap", marginBottom: 2 }}>
                              {u.topics.map((t) => <span key={t} className="chip" style={{ fontSize: 12 }}>{t}</span>)}
                            </div>
                          )}
                          {u.grammar && <div>Grammatik: {u.grammar}</div>}
                          {u.vocabularyNotes && <div>Wortschatz: {u.vocabularyNotes}</div>}
                        </div>
                      )}
                  </td>
                  <td style={{ textAlign: "right" }}>
                    {series.isOwn && (
                      <button
                        type="button" className="btn ghost small" style={{ width: "auto" }}
                        aria-label={`${u.label} bearbeiten`}
                        onClick={() => setEditing(editing === u.id ? null : u.id)}
                      >{editing === u.id ? "Schließen" : "Bearbeiten"}</button>
                    )}
                  </td>
                </tr>
                {editing === u.id && (
                  <tr>
                    <td colSpan={4}>
                      <UnitForm
                        seriesId={series.id} unit={u}
                        onDone={() => { setEditing(null); units.reload(); onChanged(); }}
                      />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      )}

      {series.isOwn && (
        <div style={{ marginTop: 10 }}>
          <h4 className="h-section" style={{ fontSize: "1rem" }}>Unit hinzufügen</h4>
          <UnitForm seriesId={series.id} onDone={() => { units.reload(); onChanged(); }} />
        </div>
      )}
    </>
  );
}

const BOOK_TYPES: BookType[] = ["Textbook", "Workbook", "TeacherGuide"];

/**
 * Ein Formular für Anlegen und Ändern. Bewusst eines: die Felder sind identisch, und beim Ändern zählt
 * genau derselbe fachliche Hinweis – der Stoff der Unit ist der Grund, warum es diese Ebene gibt.
 */
function UnitForm({ seriesId, unit, onDone }: {
  seriesId: number;
  unit?: SeriesUnitResponse;
  onDone: () => void;
}) {
  const [label, setLabel] = useState(unit?.label ?? "");
  const [grade, setGrade] = useState(unit?.grade?.toString() ?? "");
  const [bookType, setBookType] = useState<BookType>(unit?.bookType ?? "Textbook");
  const [topics, setTopics] = useState<string[]>(unit?.topics ?? []);
  const [topicInput, setTopicInput] = useState("");
  const [grammar, setGrammar] = useState(unit?.grammar ?? "");
  const [vocabularyNotes, setVocabularyNotes] = useState(unit?.vocabularyNotes ?? "");
  const action = useAction();
  const id = unit ? `u${unit.id}` : `new${seriesId}`;

  function addTopic() {
    const t = topicInput.trim();
    if (t.length === 0 || topics.includes(t)) { setTopicInput(""); return; }
    setTopics((cur) => [...cur, t]);
    setTopicInput("");
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!label.trim()) { action.fail("Die Bezeichnung fehlt."); return; }
    const dto: CreateSeriesUnitDto = {
      label: label.trim(),
      grade: grade.trim() === "" ? null : Number(grade),
      bookType,
      topics,
      grammar: grammar.trim() || null,
      vocabularyNotes: vocabularyNotes.trim() || null,
    };
    const ok = await action.run(() => (unit
      ? api.updateSeriesUnit(seriesId, unit.id, dto)
      : api.createSeriesUnit(seriesId, dto)), unit ? "Gespeichert." : "Unit hinzugefügt.");
    if (!ok) return;
    if (!unit) { setLabel(""); setTopics([]); setGrammar(""); setVocabularyNotes(""); }
    onDone();
  }

  async function remove() {
    if (!unit) return;
    if (!confirmAction(`Unit „${unit.label}" löschen? Lehrbücher, die darauf zeigen, verlieren nur die Angabe.`)) return;
    if (await action.run(() => api.deleteSeriesUnit(seriesId, unit.id))) onDone();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor={`unit-label-${id}`}>Bezeichnung</label>
          <input
            id={`unit-label-${id}`} value={label} onChange={(e) => setLabel(e.target.value)}
            placeholder="Unit 3 – Growing up"
          />
        </div>
        <div className="field">
          <label htmlFor={`unit-grade-${id}`}>Band <span className="muted">(als Klasse)</span></label>
          <input
            id={`unit-grade-${id}`} type="number" min={1} max={13} value={grade}
            onChange={(e) => setGrade(e.target.value)} placeholder="8"
          />
        </div>
        <div className="field">
          <label htmlFor={`unit-booktype-${id}`}>Buchtyp</label>
          <select id={`unit-booktype-${id}`} value={bookType} onChange={(e) => setBookType(e.target.value as BookType)}>
            {BOOK_TYPES.map((t) => <option key={t} value={t}>{BOOK_TYPE_LABEL[t]}</option>)}
          </select>
        </div>
      </div>
      <div className="field">
        <label htmlFor={`unit-topics-${id}`}>Themen der Unit</label>
        <div className="row" style={{ gap: 4, flexWrap: "wrap", marginBottom: topics.length > 0 ? 4 : 0 }}>
          {topics.map((t) => (
            <span key={t} className="chip" style={{ fontSize: 12 }}>
              {t}
              <button type="button" aria-label={`Thema ${t} entfernen`}
                onClick={() => setTopics((cur) => cur.filter((x) => x !== t))}
                style={{ background: "none", border: "none", color: "inherit", cursor: "pointer", padding: 0, marginLeft: 4, fontSize: 14, lineHeight: 1 }}>×</button>
            </span>
          ))}
        </div>
        <input
          id={`unit-topics-${id}`} value={topicInput} onChange={(e) => setTopicInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); addTopic(); } }}
          onBlur={addTopic}
          placeholder="Thema eintippen, Enter fügt hinzu"
        />
      </div>
      <div className="field">
        <label htmlFor={`unit-grammar-${id}`}>Grammatik der Unit</label>
        <input
          id={`unit-grammar-${id}`} value={grammar} onChange={(e) => setGrammar(e.target.value)}
          placeholder="Present perfect vs. simple past"
        />
      </div>
      <div className="field">
        <label htmlFor={`unit-vocab-${id}`}>Wortschatz der Unit</label>
        <textarea
          id={`unit-vocab-${id}`} rows={2} value={vocabularyNotes}
          onChange={(e) => setVocabularyNotes(e.target.value)}
          placeholder="to grow up, responsibility, to argue"
        />
        <span className="sub">
          Wortfelder oder konkrete Wörter. Genau das nimmt der KI-Creator als gesetzten Stoff – er darf ihn
          einkleiden, aber nicht austauschen.
        </span>
      </div>
      <div className="row" style={{ gap: 8 }}>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Speichere…" : unit ? "Speichern" : "Unit hinzufügen"}
        </button>
        {unit && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
            disabled={action.busy} onClick={remove}>Unit löschen</button>
        )}
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}

/** Neue Reihe. Der Name trägt den Slug – derselbe Name liefert die bestehende Reihe zurück. */
function NewSeries({ subjects, publishers, onPublisherCreated, onCreated }: {
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  onPublisherCreated: () => void;
  onCreated: () => void;
}) {
  const [form, setForm] = useState({
    name: "", publisherId: "", newPublisher: "", subjectId: "", schoolTypes: "None" as SchoolType,
    sourceLanguage: "", targetLanguage: "", notes: "",
  });
  const action = useAction();
  const publisherAction = useAction();

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  /** Verlag inline anlegen (idempotent über den Slug) und gleich auswählen – kein Seitenwechsel nötig. */
  async function addPublisher() {
    const name = form.newPublisher.trim();
    if (!name) return;
    const created = await publisherAction.runFor(() => api.createPublisher({ name }));
    if (!created) return;
    onPublisherCreated();
    setForm((f) => ({ ...f, publisherId: String(created.id), newPublisher: "" }));
    // Neutral formuliert wie bei der Reihe: der Slug macht das idempotent, ein zweiter Versuch
    // mit demselben Namen legt nichts an, sondern liefert den bestehenden Verlag zurück.
    publisherAction.succeed(`„${created.name}" steht im Vokabular.`);
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) return;
    const subject = subjects.find((s) => String(s.id) === form.subjectId);
    // `runFor`, weil die Meldung den Namen **des Servers** nennt: gleicher Name = gleiche Reihe, ein
    // zweites Anlegen liefert die bestehende zurück – und die kann anders geschrieben sein als das Feld.
    const created = await action.runFor(() => api.createTextbookSeries({
      name: form.name.trim(),
      publisherId: form.publisherId ? Number(form.publisherId) : null,
      subjectId: form.subjectId ? Number(form.subjectId) : null,
      // Den Fachnamen mitschicken: er trägt die Reihe auch dort, wo kein Katalog-Fach gewählt ist.
      subjectName: subject?.name ?? null,
      schoolTypes: form.schoolTypes === "None" ? null : form.schoolTypes,
      sourceLanguage: form.sourceLanguage || null,
      targetLanguage: form.targetLanguage || null,
      notes: form.notes.trim() || null,
    }));
    if (!created) return;
    setForm({ ...form, name: "", notes: "" });
    action.succeed(`„${created.name}" steht im Katalog. Jetzt die Units mit ihrem Stoff anlegen.`);
    onCreated();
  }

  return (
    <section>
      <h3 className="h-section">Lehrwerk hinzufügen</h3>
      <form className="form-grid" style={{ alignItems: "end" }} onSubmit={submit}>
        <div className="field">
          <label htmlFor="ns-name">Reihe</label>
          <input id="ns-name" value={form.name} onChange={(e) => up("name", e.target.value)} placeholder="Access" />
        </div>
        <div className="field">
          <label htmlFor="ns-publisher">Verlag</label>
          <select id="ns-publisher" value={form.publisherId} onChange={(e) => up("publisherId", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {publishers.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ns-subject">Fach</label>
          <select id="ns-subject" value={form.subjectId} onChange={(e) => up("subjectId", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ns-school">Schulart</label>
          <select id="ns-school" value={form.schoolTypes} onChange={(e) => up("schoolTypes", e.target.value as SchoolType)}>
            <option value="None">– für alle –</option>
            {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ns-src">Lernsprache <span className="muted">(Sprachreihen)</span></label>
          <select id="ns-src" value={form.sourceLanguage} onChange={(e) => up("sourceLanguage", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ns-tgt">Muttersprache</label>
          <select id="ns-tgt" value={form.targetLanguage} onChange={(e) => up("targetLanguage", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="ns-notes">Notiz zum Werk</label>
          <input id="ns-notes" value={form.notes} onChange={(e) => up("notes", e.target.value)} placeholder="Aufbau, Besonderheiten" />
        </div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Lege an…" : "Reihe anlegen"}
        </button>
      </form>
      <p className="sub" style={{ marginTop: 8 }}>
        Gleicher Name = gleiche Reihe: ein zweites Anlegen liefert die bestehende zurück, statt eine
        Dublette in den geteilten Katalog zu schreiben.
      </p>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {/* Verlag fehlt in der Liste? Direkt hier anlegen statt die Seite zu verlassen (idempotent über den Slug). */}
      <form
        className="row" style={{ gap: 8, marginTop: 10, alignItems: "end" }}
        onSubmit={(e) => { e.preventDefault(); addPublisher(); }}
      >
        <div className="field" style={{ maxWidth: 240 }}>
          <label htmlFor="ns-new-publisher">Neuer Verlag <span className="muted">(fehlt er oben?)</span></label>
          <input id="ns-new-publisher" value={form.newPublisher} onChange={(e) => up("newPublisher", e.target.value)}
            placeholder="Westermann" />
        </div>
        <button type="submit" className="btn ghost small" style={{ width: "auto" }}
          disabled={publisherAction.busy || !form.newPublisher.trim()}>
          {publisherAction.busy ? "Lege an…" : "Verlag anlegen"}
        </button>
      </form>
      <StatusBanner message={publisherAction.message} style={{ marginTop: 6 }} />
    </section>
  );
}
