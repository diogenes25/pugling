import { Fragment, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  CreateSeriesUnitDto, SchoolType, SeriesUnitResponse, SubjectResponse, TextbookSeriesResponse,
} from "../lib/types";

/**
 * Die Lehrwerke: welcher Stoff im Unterricht überhaupt dran ist.
 *
 * Zwei Dinge machen diese Seite mehr als eine Bücherliste:
 *
 * 1. **Die Reihe ist geteilt.** „Access" wird einmal gepflegt; das Lehrbuch eines Kindes und ein
 *    Creator-Profil zeigen auf denselben Eintrag. Nur dadurch lässt sich der Fachlehrer zu einem Kind
 *    *finden* statt raten. Lesen darf jeder, ändern nur wer sie angelegt hat.
 * 2. **Der Stoff steht in der Unit.** Themen, Grammatik und Wortschatz einer Unit sind das, was der
 *    KI-Creator liest. Bleiben sie leer, erfindet er den Inhalt – fachlich plausibel, aber am
 *    Unterricht vorbei.
 */
export function VaterLehrwerke() {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const list = useAsync<TextbookSeriesResponse[]>(
    () => api.textbookSeries({ search: applied || undefined }), [applied]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
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
          className="row" style={{ gap: 8, marginBottom: 10 }}
          onSubmit={(e) => { e.preventDefault(); setApplied(search.trim()); }}
        >
          <input
            aria-label="Lehrwerke durchsuchen" value={search} onChange={(e) => setSearch(e.target.value)}
            placeholder="Suche in Name oder Verlag" style={{ maxWidth: 320 }}
          />
          <button type="submit" className="btn ghost small" style={{ width: "auto" }}>Suchen</button>
        </form>

        {list.error && <div className="banner err">{list.error}</div>}
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
          <table className="table">
            <thead><tr><th>Reihe</th><th>Fach</th><th>Schulart</th><th>Units</th><th /></tr></thead>
            <tbody>
              {list.data?.map((s) => (
                <SeriesRow
                  key={s.id} series={s} subjects={subjects.data ?? []}
                  open={open === s.id} onToggle={() => setOpen(open === s.id ? null : s.id)}
                  onChanged={list.reload}
                />
              ))}
              {list.data?.length === 0 && (
                <tr><td colSpan={5} className="muted">Noch kein Lehrwerk. Lege unten eines an.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      <NewSeries subjects={subjects.data ?? []} onCreated={list.reload} />
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

  return (
    <>
      <tr>
        <td>
          {series.name}
          {series.publisher && <span className="muted"> · {series.publisher}</span>}
          <div className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{series.slug}</div>
        </td>
        <td>{subjectLabel ?? <span className="muted">–</span>}</td>
        <td>{series.schoolTypes === "None" ? <span className="muted">alle</span> : series.schoolTypes}</td>
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
      {action.message && <tr><td colSpan={5}><StatusBanner message={action.message} style={{ marginTop: 0 }} /></td></tr>}
      {open && (
        <tr>
          <td colSpan={5}>
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

/** Die Units einer Reihe: Band, Bezeichnung und – der Kern – der Stoff. */
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
                  <td>{u.label}</td>
                  <td>
                    {[u.topics, u.grammar, u.vocabularyNotes].every((f) => !f)
                      ? <span className="pill mag">kein Stoff hinterlegt</span>
                      : (
                        <div className="muted" style={{ fontSize: 13 }}>
                          {u.topics && <div>Themen: {u.topics}</div>}
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

/**
 * Ein Formular für Anlegen und Ändern. Bewusst eines: die Felder sind identisch, und beim Ändern zählt
 * genau derselbe fachliche Hinweis – der Stoff der Unit ist der Grund, warum es diese Ebene gibt.
 */
function UnitForm({ seriesId, unit, onDone }: {
  seriesId: number;
  unit?: SeriesUnitResponse;
  onDone: () => void;
}) {
  const [form, setForm] = useState({
    label: unit?.label ?? "",
    grade: unit?.grade?.toString() ?? "",
    topics: unit?.topics ?? "",
    grammar: unit?.grammar ?? "",
    vocabularyNotes: unit?.vocabularyNotes ?? "",
  });
  const action = useAction();
  const id = unit ? `u${unit.id}` : `new${seriesId}`;

  function up<K extends keyof typeof form>(k: K, v: string) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.label.trim()) { action.fail("Die Bezeichnung fehlt."); return; }
    const dto: CreateSeriesUnitDto = {
      label: form.label.trim(),
      grade: form.grade.trim() === "" ? null : Number(form.grade),
      topics: form.topics.trim() || null,
      grammar: form.grammar.trim() || null,
      vocabularyNotes: form.vocabularyNotes.trim() || null,
    };
    const ok = await action.run(() => (unit
      ? api.updateSeriesUnit(seriesId, unit.id, dto)
      : api.createSeriesUnit(seriesId, dto)), unit ? "Gespeichert." : "Unit hinzugefügt.");
    if (!ok) return;
    if (!unit) setForm({ label: "", grade: form.grade, topics: "", grammar: "", vocabularyNotes: "" });
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
            id={`unit-label-${id}`} value={form.label} onChange={(e) => up("label", e.target.value)}
            placeholder="Unit 3 – Growing up"
          />
        </div>
        <div className="field">
          <label htmlFor={`unit-grade-${id}`}>Band <span className="muted">(als Klasse)</span></label>
          <input
            id={`unit-grade-${id}`} type="number" min={1} max={13} value={form.grade}
            onChange={(e) => up("grade", e.target.value)} placeholder="8"
          />
        </div>
      </div>
      <div className="field">
        <label htmlFor={`unit-topics-${id}`}>Themen der Unit</label>
        <input
          id={`unit-topics-${id}`} value={form.topics} onChange={(e) => up("topics", e.target.value)}
          placeholder="Familie, Freundschaft, Erwachsenwerden"
        />
      </div>
      <div className="field">
        <label htmlFor={`unit-grammar-${id}`}>Grammatik der Unit</label>
        <input
          id={`unit-grammar-${id}`} value={form.grammar} onChange={(e) => up("grammar", e.target.value)}
          placeholder="Present perfect vs. simple past"
        />
      </div>
      <div className="field">
        <label htmlFor={`unit-vocab-${id}`}>Wortschatz der Unit</label>
        <textarea
          id={`unit-vocab-${id}`} rows={2} value={form.vocabularyNotes}
          onChange={(e) => up("vocabularyNotes", e.target.value)}
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
function NewSeries({ subjects, onCreated }: { subjects: SubjectResponse[]; onCreated: () => void }) {
  const [form, setForm] = useState({
    name: "", publisher: "", subjectId: "", schoolTypes: "None" as SchoolType,
    sourceLanguage: "", targetLanguage: "", notes: "",
  });
  const action = useAction();

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) return;
    const subject = subjects.find((s) => String(s.id) === form.subjectId);
    // `runFor`, weil die Meldung den Namen **des Servers** nennt: gleicher Name = gleiche Reihe, ein
    // zweites Anlegen liefert die bestehende zurück – und die kann anders geschrieben sein als das Feld.
    const created = await action.runFor(() => api.createTextbookSeries({
      name: form.name.trim(),
      publisher: form.publisher.trim() || null,
      subjectId: form.subjectId ? Number(form.subjectId) : null,
      // Den Fachnamen mitschicken: er trägt die Reihe auch dort, wo kein Katalog-Fach gewählt ist.
      subjectName: subject?.name ?? null,
      schoolTypes: form.schoolTypes === "None" ? null : form.schoolTypes,
      sourceLanguage: form.sourceLanguage.trim() || null,
      targetLanguage: form.targetLanguage.trim() || null,
      notes: form.notes.trim() || null,
    }));
    if (!created) return;
    setForm({ ...form, name: "", publisher: "", notes: "" });
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
          <input id="ns-publisher" value={form.publisher} onChange={(e) => up("publisher", e.target.value)} placeholder="Cornelsen" />
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
          <input id="ns-src" value={form.sourceLanguage} onChange={(e) => up("sourceLanguage", e.target.value)} placeholder="en" />
        </div>
        <div className="field">
          <label htmlFor="ns-tgt">Muttersprache</label>
          <input id="ns-tgt" value={form.targetLanguage} onChange={(e) => up("targetLanguage", e.target.value)} placeholder="de" />
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
    </section>
  );
}
