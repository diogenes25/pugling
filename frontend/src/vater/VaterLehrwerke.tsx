import { Fragment, useRef, useState } from "react";
import { FieldLabel } from "../components/InfoHint";
import { StatusBanner } from "../components/StatusBanner";
import { FREETEXT_SUBJECT, seriesFormValues, seriesPatch, type SeriesFormValues } from "./seriesPatch";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { SCHOOL_TYPES } from "../lib/labels";
import { LANGUAGES } from "../lib/languages";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { PublisherAdmin } from "./PublisherAdmin";
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
                  key={s.id} series={s} subjects={subjects.data ?? []} publishers={publishers.data ?? []}
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
      <PublisherAdmin onChanged={() => { publishers.reload(); list.reload(); }} />
    </>
  );
}

function SeriesRow({ series, subjects, publishers, open, onToggle, onChanged }: {
  series: TextbookSeriesResponse;
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  open: boolean;
  onToggle: () => void;
  onChanged: () => void;
}) {
  const action = useAction();
  // Lokal, nicht im seiten-globalen `open`: das ist der Akkordeon-Platz der Units, und beide Bereiche
  // sollen sich nicht gegenseitig zuklappen (B-123, Entscheidung 3 – Muster `UnitPanel`).
  const [editing, setEditing] = useState(false);

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
          {/* Fremde Reihen bleiben lesbar – die Knöpfe fehlen, statt später mit 403 zu scheitern. */}
          {series.isOwn && (
            <>
              {/* „Reihe bearbeiten", nicht bloß „Bearbeiten": daneben steht „Units", und welcher der
                  beiden den Inhalt ändert, wäre sonst nicht zu erraten (B-123, Entscheidung 6). */}
              {/* `aria-label` mit dem Reihennamen, weil ein Screenreader sonst bei zehn Reihen zehnmal
                  dasselbe hört. **Sichtbarer Text zuerst, Kontext hinten** – WCAG 2.5.3 „Label in Name"
                  verlangt, dass der Sichttext im barrierefreien Namen VORKOMMT, sonst löst eine
                  Spracheingabe („Reihe bearbeiten") den Knopf nicht aus. Die Nachbarn taugen hier nicht
                  als Vorbild: `PublisherAdmin` macht es einmal so und einmal andersherum. */}
              <button type="button" className="btn ghost small" style={{ width: "auto" }}
                aria-label={editing
                  ? `Bearbeiten schließen: „${series.name}"`
                  : `Reihe bearbeiten: „${series.name}"`}
                aria-expanded={editing} onClick={() => setEditing(!editing)}>
                {editing ? "Bearbeiten schließen" : "Reihe bearbeiten"}
              </button>
              <button type="button" className="btn ghost small" style={{ width: "auto" }}
                aria-label={`Löschen: „${series.name}"`}
                disabled={action.busy} onClick={remove}>
                Löschen
              </button>
            </>
          )}
        </td>
      </tr>
      {action.message && <tr><td colSpan={6}><StatusBanner message={action.message} style={{ marginTop: 0 }} /></td></tr>}
      {editing && (
        <tr>
          <td colSpan={6}>
            <SeriesForm
              series={series} subjects={subjects} publishers={publishers} onSaved={onChanged}
            />
          </td>
        </tr>
      )}
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

/**
 * Die Metadaten einer Reihe ändern.
 *
 * Bewusst **nicht** dasselbe Formular wie `NewSeries` (B-123, Entscheidung 3): dort hängen
 * Abschnittsüberschrift, Idempotenz-Hinweis und das Verlag-Inline-Anlegen daran, die beim Ändern alle
 * nichts zu suchen haben – ein gemeinsames Formular wäre voller `series ? … : …`. Wer hier einen fehlenden
 * Verlag braucht, legt ihn unten im Anlegen-Abschnitt an; er erscheint sofort in dieser Auswahl, weil die
 * Verlagsliste auf Seitenebene liegt.
 */
/*
 * Exportiert **nur für den Test** (B-143): Die beiden deaktivierten `<option>`s sind der eigentliche
 * Defekt dieser Story, und `seriesPatch` erreicht sie nicht — dort war nie etwas kaputt. Ohne diesen
 * Zugang liefe ein späteres „Aufräumen" der Optionen grün durch. Die Komponente braucht dafür kein
 * gefälschtes `fetch`: alle `api`-Aufrufe hängen am Absenden, das Rendern ist reine Prop-Arbeit.
 */
export function SeriesForm({ series, subjects, publishers, onSaved }: {
  series: TextbookSeriesResponse;
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  onSaved: () => void;
}) {
  // Der Ladezustand ist der Bezugspunkt des Diffs (B-123, Entscheidung 2) – er darf sich NICHT mit dem
  // Formular mitbewegen, sonst entscheidet der Vergleich gegen sich selbst. Darum `useRef`.
  //
  // Er zieht an genau zwei Stellen nach: beim Speichern (aus der Server-Antwort) und bei jeder
  // Neumontage (das Formular hängt an `{editing && …}`, Zuklappen wirft es weg). Ändert jemand die
  // Reihe von außen, während das Formular offen ist, bleibt er stehen — das richtet keinen Schaden an,
  // weil der Diff FELDWEISE läuft: ein nicht angefasstes Feld geht nie mit, der fremde Wert bleibt also
  // stehen statt überschrieben zu werden. Geprüft für alle In-App-Auslöser (UnitPanel ändert nur
  // Zählwerte, ein gelöschter Verlag steht serverseitig schon auf `null`); übrig bleibt ein zweiter Tab,
  // und dort hilft Zuklappen und neu aufklappen.
  const geladen = useRef(seriesFormValues(series));
  const [form, setForm] = useState<SeriesFormValues>(geladen.current);
  const action = useAction();
  const id = `se${series.id}`;

  function up<K extends keyof SeriesFormValues>(k: K, v: SeriesFormValues[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { action.fail("Der Name der Reihe fehlt."); return; }

    const dto = seriesPatch(geladen.current, form);
    // Kein leerer PATCH: er wäre erfolgreich und folgenlos, und „Gespeichert." wäre dann eine Lüge.
    if (dto === null) { action.succeed("Nichts geändert."); return; }

    const aktualisiert = await action.runFor(() => api.updateTextbookSeries(series.id, dto));
    if (!aktualisiert) return;
    // Das Formular bleibt offen und der Ladezustand wird aus der ANTWORT nachgezogen. Beides gehört
    // zusammen: schlösse es sich, verschwände der StatusBanner mitsamt der Bestätigung (im Rollengang
    // aufgefallen – „Gespeichert." war nie zu sehen); bliebe es offen ohne diese Zeile, rechnete der
    // nächste Diff gegen einen veralteten Bezugspunkt und schickte Felder erneut oder gar nicht.
    geladen.current = seriesFormValues(aktualisiert);
    setForm(geladen.current);
    action.succeed("Gespeichert.");
    onSaved();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <FieldLabel htmlFor={`se-name-${id}`} topic="seriesName">Name der Reihe</FieldLabel>
          <input id={`se-name-${id}`} value={form.name} onChange={(e) => up("name", e.target.value)} />
          {/* Nur der WERT – die Erklärung dazu steht im `HelpTopic` und darf nicht zweimal dastehen
              (`frontend/CLAUDE.md`: der Text steht nie am Feld). Den Wert kann das Popover nicht
              liefern, es kennt die Reihe nicht. */}
          <span className="sub">Kurzname <code>{series.slug}</code></span>
        </div>
        <div className="field">
          <label htmlFor={`se-publisher-${id}`}>Verlag</label>
          <select id={`se-publisher-${id}`} value={form.publisherId} onChange={(e) => up("publisherId", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {publishers.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </div>
        <div className="field">
          <FieldLabel htmlFor={`se-subject-${id}`} topic="seriesSubject">Fach</FieldLabel>
          <select id={`se-subject-${id}`} value={form.subjectId} onChange={(e) => up("subjectId", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {/* Der Freitext-Zustand als eigene, nicht wählbare Option (B-143): so sagt das Feld dasselbe
                wie die Zeile daneben, statt zu schweigen. Und der Schutz vorm versehentlichen Löschen
                fällt gratis ab – `form` bleibt gleich `loaded`, also schickt `seriesPatch` nichts.

                Bedingung und Beschriftung kommen BEIDE aus `series`, dem geladenen Stand – nicht aus
                `form`. Zwei Gründe: die Option soll stehen bleiben, während der Nutzer „– keine Angabe –"
                probiert (sie zeigt ihm, was er gerade ersetzt, und ein Zurück kostet sonst das Zuklappen
                des Formulars mitsamt allen anderen Eingaben); und aus einer Quelle können Bedingung und
                Text nicht auseinanderlaufen. */}
            {series.subjectId == null && series.subjectName && (
              <option value={FREETEXT_SUBJECT} disabled>{series.subjectName} (Freitext)</option>
            )}
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <FieldLabel htmlFor={`se-school-${id}`} topic="seriesSchoolTypes">Schulart</FieldLabel>
          <select id={`se-school-${id}`} value={form.schoolTypes}
            onChange={(e) => up("schoolTypes", e.target.value as SchoolType)}>
            <option value="None">– für alle –</option>
            {/* Dieselbe Form für eine Kombination („Realschule, Gymnasium"): `SchoolTypes` ist ein
                [Flags]-Enum, das Feld kennt aber nur Einzelwerte. Ohne diese Option stünde der `<select>`
                leer, der Nutzer griffe zu „– für alle –" und `None` LÖSCHTE die Kombination.
                Aus `series` und nicht aus `form`, aus denselben zwei Gründen wie beim Fach. */}
            {series.schoolTypes !== "None" && !SCHOOL_TYPES.includes(series.schoolTypes) && (
              <option value={series.schoolTypes} disabled>{series.schoolTypes}</option>
            )}
            {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`se-src-${id}`}>Lernsprache</label>
          <select id={`se-src-${id}`} value={form.sourceLanguage} onChange={(e) => up("sourceLanguage", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`se-tgt-${id}`}>Muttersprache</label>
          <select id={`se-tgt-${id}`} value={form.targetLanguage} onChange={(e) => up("targetLanguage", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {LANGUAGES.map((l) => <option key={l.code} value={l.code}>{l.flag} {l.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`se-notes-${id}`}>Notiz zum Werk</label>
          <input id={`se-notes-${id}`} value={form.notes} onChange={(e) => up("notes", e.target.value)} />
        </div>
      </div>
      <div className="row" style={{ gap: 8 }}>
        {/* Der Name im Label ist der PERSISTIERTE – im Feld steht ggf. schon der neue. Gewollt: er
            benennt die Zeile, die bearbeitet wird, nicht den Entwurf darin. */}
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }}
          aria-label={`Speichern: „${series.name}"`} disabled={action.busy}>
          {action.busy ? "Speichere…" : "Speichern"}
        </button>
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
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
export function UnitForm({ seriesId, unit, onDone }: {
  seriesId: number;
  unit?: SeriesUnitResponse;
  onDone: () => void;
}) {
  const [form, setForm] = useState({
    label: unit?.label ?? "", grade: unit?.grade?.toString() ?? "",
    bookType: unit?.bookType ?? "Textbook" as BookType, topics: unit?.topics ?? [] as string[],
    topicInput: "", grammar: unit?.grammar ?? "", vocabularyNotes: unit?.vocabularyNotes ?? "",
  });
  const action = useAction();
  const id = unit ? `u${unit.id}` : `new${seriesId}`;

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  function addTopic() {
    const t = form.topicInput.trim();
    if (t.length === 0 || form.topics.includes(t)) { up("topicInput", ""); return; }
    setForm((f) => ({ ...f, topics: [...f.topics, t], topicInput: "" }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.label.trim()) { action.fail("Die Bezeichnung fehlt."); return; }
    const dto: CreateSeriesUnitDto = {
      label: form.label.trim(),
      grade: form.grade.trim() === "" ? null : Number(form.grade),
      bookType: form.bookType,
      topics: form.topics,
      grammar: form.grammar.trim() || null,
      vocabularyNotes: form.vocabularyNotes.trim() || null,
    };
    const ok = await action.run(() => (unit
      ? api.updateSeriesUnit(seriesId, unit.id, dto)
      : api.createSeriesUnit(seriesId, dto)), unit ? "Gespeichert." : "Unit hinzugefügt.");
    if (!ok) return;
    if (!unit) setForm((f) => ({ ...f, label: "", topics: [], grammar: "", vocabularyNotes: "" }));
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
        <div className="field">
          <label htmlFor={`unit-booktype-${id}`}>Buchtyp</label>
          <select id={`unit-booktype-${id}`} value={form.bookType} onChange={(e) => up("bookType", e.target.value as BookType)}>
            {BOOK_TYPES.map((t) => <option key={t} value={t}>{BOOK_TYPE_LABEL[t]}</option>)}
          </select>
        </div>
      </div>
      <div className="field">
        <label htmlFor={`unit-topics-${id}`}>Themen der Unit</label>
        <div className="row" style={{ gap: 4, flexWrap: "wrap", marginBottom: form.topics.length > 0 ? 4 : 0 }}>
          {form.topics.map((t) => (
            <span key={t} className="chip" style={{ fontSize: 12 }}>
              {t}
              <button type="button" aria-label={`Thema ${t} entfernen`}
                onClick={() => up("topics", form.topics.filter((x) => x !== t))}
                style={{ background: "none", border: "none", color: "inherit", cursor: "pointer", padding: 0, marginLeft: 4, fontSize: 14, lineHeight: 1 }}>×</button>
            </span>
          ))}
        </div>
        {/*
          `onBlur` legt an – das rettet die Eingabe dessen, der Enter vergisst. Damit nicht jede
          versehentliche Fokusbewegung zur Dateneingabe wird, gibt es die Gegenrichtung: Escape leert das
          Feld und entzieht dem `onBlur` seinen Inhalt. Der Platzhalter nennt beide Wege, sonst ist die
          Abbruchmöglichkeit unauffindbar (B-129).
        */}
        <input
          id={`unit-topics-${id}`} value={form.topicInput} onChange={(e) => up("topicInput", e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") { e.preventDefault(); addTopic(); }
            if (e.key === "Escape") { e.preventDefault(); up("topicInput", ""); }
          }}
          onBlur={addTopic}
          placeholder="Thema eintippen"
        />
        {/*
          Die Wege stehen als `.sub`-Zeile, nicht im Platzhalter: der Platzhalter ist nur im LEEREN Feld
          sichtbar, und beide angekündigten Verhaltensweisen wirken nur im nicht-leeren – er wäre genau
          dann weg, wenn er zählt. Dazu schnitt der lange Text auf Telefonbreite ausgerechnet den
          Esc-Hinweis ab (gemessen im Review, B-129).
        */}
        <span className="sub">
          <strong>Enter</strong> oder das Verlassen des Feldes fügt das Thema hinzu,
          <strong> Esc</strong> verwirft die Eingabe.
        </span>
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
    // `runFor`, weil die Meldung den Namen **des Servers** nennt: gleicher Name = gleiche Reihe, ein
    // zweites Anlegen liefert die bestehende zurück – und die kann anders geschrieben sein als das Feld.
    const created = await action.runFor(() => api.createTextbookSeries({
      name: form.name.trim(),
      publisherId: form.publisherId ? Number(form.publisherId) : null,
      subjectId: form.subjectId ? Number(form.subjectId) : null,
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
