import { Fragment, useState } from "react";
import { Link } from "react-router-dom";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { matchReasonLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  CreateTextbookDto, CreatorProfileMatch, SeriesUnitResponse, SubjectResponse, TextbookResponse,
  TextbookSeriesResponse,
} from "../lib/types";

/**
 * Das Unterrichtsmaterial eines Kindes – und wer es kennt.
 *
 * Hier wird die Kette geschlossen, auf der die ganze Individualisierung des KI-Creators ruht: das Buch
 * des Kindes zeigt auf eine <b>katalogisierte Reihe</b> und auf die <b>aktuelle Unit</b>. Erst dadurch
 * kann der Server sagen, welcher Fachlehrer den Stoff kennt (Reihen-Treffer wiegt am schwersten) – und
 * der Creator weiß, welche Themen, Grammatik und Wörter gerade dran sind.
 *
 * Titel und Kapitel bleiben als Freitext-Rückfallebene: ein Buch ohne katalogisierte Reihe ist erlaubt,
 * es liefert dem Creator aber nur seinen Namen.
 */
export function ChildMaterialSection({ childId, childName, subjects }: {
  childId: number;
  childName: string;
  /* Von der Kind-Seite durchgereicht: Der Stundenplan daneben braucht dieselbe Liste, und zweimal
     dieselbe Fächer-Abfrage je Seitenaufruf ist eine Runde zu viel. */
  subjects: SubjectResponse[];
}) {
  const books = useAsync<TextbookResponse[]>(() => api.childTextbooks(childId), [childId]);
  const series = useAsync<TextbookSeriesResponse[]>(() => api.textbookSeries(), []);
  const [editing, setEditing] = useState<number | null>(null);

  return (
    <section>
      <h3 className="h-section">Unterrichtsmaterial</h3>
      <p className="sub">
        Woraus lernt {childName} gerade? Mit hinterlegter <strong>Reihe und Unit</strong> kennt der
        KI-Creator den Stoff; ohne sie muss er ihn erfinden. Reihen und ihre Units pflegst du unter{" "}
        <Link to="/vater/lehrwerke">Lehrwerke</Link>.
      </p>

      {books.error && <div className="banner err">{books.error}</div>}
      {books.data === null ? <div className="loading">Lade…</div> : books.data.length === 0 ? (
        <p className="muted">Noch kein Buch hinterlegt.</p>
      ) : (
        <table className="table">
          <thead><tr><th>Buch</th><th>Fach</th><th>Reihe</th><th>Aktuelle Unit</th><th /></tr></thead>
          <tbody>
            {books.data.map((b) => (
              <Fragment key={b.id}>
                <tr>
                  <td>
                    {b.title}
                    {b.publisher && <span className="muted"> · {b.publisher}</span>}
                  </td>
                  <td>{b.subjectName ?? <span className="muted">–</span>}</td>
                  <td>
                    {b.seriesName
                      ? b.seriesName
                      : <span className="pill mag">nicht katalogisiert</span>}
                  </td>
                  <td>
                    {b.currentUnitLabel
                      ?? (b.currentChapter
                        ? <span className="muted">{b.currentChapter} (Freitext)</span>
                        : <span className="pill mag">keine</span>)}
                  </td>
                  <td style={{ textAlign: "right" }}>
                    <button
                      type="button" className="btn ghost small" style={{ width: "auto" }}
                      aria-label={`${b.title} bearbeiten`}
                      onClick={() => setEditing(editing === b.id ? null : b.id)}
                    >{editing === b.id ? "Schließen" : "Bearbeiten"}</button>
                  </td>
                </tr>
                {editing === b.id && (
                  <tr>
                    <td colSpan={5}>
                      <TextbookForm
                        childId={childId} book={b} series={series.data ?? []} subjects={subjects}
                        onDone={() => { setEditing(null); books.reload(); }}
                      />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      )}

      <div style={{ marginTop: 12 }}>
        <h4 className="h-section" style={{ fontSize: "1rem" }}>Buch hinzufügen</h4>
        <TextbookForm
          childId={childId} series={series.data ?? []} subjects={subjects}
          onDone={books.reload}
        />
      </div>

      {/* Erst mit geladenen Büchern: Das Panel leitet sein Fach AUS ihnen ab. Vorher (`?? []`) fragte es
          erst ohne Fach und gleich darauf noch einmal mit – eine überflüssige Runde und dazwischen eine
          Trefferliste, die schon wieder falsch war. */}
      {books.data
        ? <MatchPanel childId={childId} childName={childName} books={books.data} />
        : <div className="loading">Lade…</div>}
    </section>
  );
}

/**
 * Ein Formular für Anlegen und Ändern. Die Unit-Auswahl hängt an der gewählten Reihe – der Server
 * weist eine Unit aus einem fremden Werk ab, und das zu Recht: sonst stünde am Kind der Stoff eines
 * Buchs, das es nicht benutzt.
 */
function TextbookForm({ childId, book, series, subjects, onDone }: {
  childId: number;
  book?: TextbookResponse;
  series: TextbookSeriesResponse[];
  subjects: SubjectResponse[];
  onDone: () => void;
}) {
  const [form, setForm] = useState({
    title: book?.title ?? "",
    subjectId: book?.subjectId?.toString() ?? "",
    grade: book?.grade?.toString() ?? "",
    publisher: book?.publisher ?? "",
    seriesId: book?.seriesId?.toString() ?? "",
    currentUnitId: book?.currentUnitId?.toString() ?? "",
    currentChapter: book?.currentChapter ?? "",
  });
  const action = useAction();
  const id = book ? `tb${book.id}` : "tbnew";

  // Die Units der gewählten Reihe. Ohne Reihe gibt es nichts zu wählen – dann bleibt das Freitext-Kapitel.
  const seriesId = form.seriesId ? Number(form.seriesId) : null;
  const units = useAsync<SeriesUnitResponse[]>(
    () => seriesId == null ? Promise.resolve([]) : api.seriesUnits(seriesId), [seriesId]);

  function up<K extends keyof typeof form>(k: K, v: string) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  /** Reihe gewechselt: die Unit-Wahl der alten Reihe wäre danach falsch – also fällt sie weg. */
  function chooseSeries(value: string) {
    setForm((f) => ({ ...f, seriesId: value, currentUnitId: "" }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.title.trim()) { action.fail("Der Titel fehlt."); return; }
    const subject = subjects.find((s) => String(s.id) === form.subjectId);
    const dto: CreateTextbookDto = {
      title: form.title.trim(),
      subjectId: form.subjectId ? Number(form.subjectId) : null,
      subjectName: subject?.name ?? null,
      grade: form.grade.trim() === "" ? null : Number(form.grade),
      publisher: form.publisher.trim() || null,
      seriesId,
      currentUnitId: form.currentUnitId ? Number(form.currentUnitId) : null,
      currentChapter: form.currentChapter.trim() || null,
    };
    const ok = await action.run(() => (book
      // Beim Ändern muss „leer" ausdrücklich gesagt werden (der Server überliest `null` als „nicht
      // angegeben"). Ohne die Schalter wäre „nicht katalogisiert" ein stiller Klick ins Nichts.
      ? api.updateChildTextbook(childId, book.id, {
          ...dto,
          clearSubject: dto.subjectId == null,
          clearGrade: dto.grade == null,
          clearSeries: seriesId == null,
          clearUnit: form.currentUnitId === "",
        })
      : api.createChildTextbook(childId, dto)), book ? "Gespeichert." : "Buch hinterlegt.");
    if (!ok) return;
    if (!book) setForm({ ...form, title: "", currentChapter: "" });
    onDone();
  }

  async function remove() {
    if (!book) return;
    if (!confirmAction(`Buch „${book.title}" entfernen?`)) return;
    if (await action.run(() => api.deleteChildTextbook(childId, book.id))) onDone();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor={`${id}-title`}>Titel</label>
          <input id={`${id}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Access 8" />
        </div>
        <div className="field">
          <label htmlFor={`${id}-subject`}>Fach</label>
          <select id={`${id}-subject`} value={form.subjectId} onChange={(e) => up("subjectId", e.target.value)}>
            <option value="">– keine Angabe –</option>
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`${id}-grade`}>Für Klasse</label>
          <input
            id={`${id}-grade`} type="number" min={1} max={13} value={form.grade}
            onChange={(e) => up("grade", e.target.value)} placeholder="8"
          />
        </div>
        <div className="field">
          <label htmlFor={`${id}-publisher`}>Verlag</label>
          <input id={`${id}-publisher`} value={form.publisher} onChange={(e) => up("publisher", e.target.value)} placeholder="Cornelsen" />
        </div>
        <div className="field">
          <label htmlFor={`${id}-series`}>Reihe im Katalog</label>
          <select id={`${id}-series`} value={form.seriesId} onChange={(e) => chooseSeries(e.target.value)}>
            <option value="">– nicht katalogisiert –</option>
            {series.map((s) => (
              <option key={s.id} value={s.id}>{s.name}{s.publisherName ? ` (${s.publisherName})` : ""}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`${id}-unit`}>Aktuelle Unit</label>
          <select
            id={`${id}-unit`} value={form.currentUnitId} disabled={seriesId == null}
            onChange={(e) => up("currentUnitId", e.target.value)}
          >
            <option value="">{seriesId == null ? "– erst Reihe wählen –" : "– keine –"}</option>
            {units.data?.map((u) => (
              <option key={u.id} value={u.id}>
                {u.grade != null ? `Kl. ${u.grade}: ` : ""}{u.label}
              </option>
            ))}
          </select>
          {seriesId != null && units.data?.length === 0 && (
            <span className="sub">Diese Reihe hat noch keine Units – unter „Lehrwerke" anlegen.</span>
          )}
        </div>
        <div className="field">
          <label htmlFor={`${id}-chapter`}>Kapitel als Freitext <span className="muted">(Rückfallebene)</span></label>
          <input
            id={`${id}-chapter`} value={form.currentChapter} onChange={(e) => up("currentChapter", e.target.value)}
            placeholder="Unit 4 – Past Tense"
          />
        </div>
      </div>
      <div className="row" style={{ gap: 8 }}>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Speichere…" : book ? "Speichern" : "Buch hinterlegen"}
        </button>
        {book && (
          <button
            type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
            disabled={action.busy} onClick={remove}
          >Entfernen</button>
        )}
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}

/**
 * Wer kennt den Stoff dieses Kindes? Die Antwort kommt vom Server, deterministisch bewertet – hier steht
 * sie mit ihrer <b>Begründung</b>, weil eine nackte Punktzahl niemandem hilft. Ein Fach-Filter ist
 * absichtlich wählbar: bei mehreren Fächern gibt es nicht *einen* passenden Lehrer, sondern einen pro Fach.
 */
function MatchPanel({ childId, childName, books }: {
  childId: number;
  childName: string;
  books: TextbookResponse[];
}) {
  // Die Fächer, für die dieses Kind Bücher hat – mehr Auswahl wäre hier nur Rauschen.
  const bookSubjects = books
    .filter((b) => b.subjectId != null)
    .map((b) => ({ id: b.subjectId!, name: b.subjectName ?? `Fach #${b.subjectId}` }))
    .filter((s, i, all) => all.findIndex((x) => x.id === s.id) === i);

  // `null` = noch nichts gewählt. Bei genau einem Fach ist die Vorgabe genau dieses – sonst blieben die
  // Fach-Punkte ungenutzt, und die Frage „wer unterrichtet das hier?" wäre gar nicht gestellt. Als
  // abgeleiteter Wert statt via Effekt, weil die Bücher asynchron nachkommen.
  const [chosen, setChosen] = useState<string | null>(null);
  const subjectId = chosen ?? (bookSubjects.length === 1 ? String(bookSubjects[0].id) : "");
  const filter = subjectId ? Number(subjectId) : undefined;

  /*
   * Die Treffer hängen am Material, nicht nur am Kind: eine neu hinterlegte Reihe ändert das Ergebnis.
   * Die Signatur der Bücher als Abhängigkeit sorgt dafür, dass genau dann neu gefragt wird, wenn sich
   * Reihe oder Unit wirklich geändert haben – vorher blieb hier ein Stand von vor dem Speichern stehen.
   */
  const signature = books.map((b) => `${b.id}:${b.seriesId ?? "-"}:${b.currentUnitId ?? "-"}`).join("|");
  const matches = useAsync<CreatorProfileMatch[]>(
    () => api.matchCreatorProfiles(childId, filter), [childId, filter, signature]);

  return (
    <div style={{ marginTop: 16 }}>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>Passender Fachlehrer</h4>

      {bookSubjects.length > 0 && (
        <div className="field" style={{ maxWidth: 260 }}>
          <label htmlFor="match-subject">Für welches Fach?</label>
          <select id="match-subject" value={subjectId} onChange={(e) => setChosen(e.target.value)}>
            <option value="">alle Fächer</option>
            {bookSubjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <span className="sub">
            Ein Fach-Filter lässt fachfremde Profile weg (fachneutrale bleiben) und zählt den Fach-Treffer mit.
          </span>
        </div>
      )}

      {matches.error && <div className="banner err">{matches.error}</div>}
      {matches.loading && matches.data === null ? <div className="loading">Lade…</div>
        : matches.data?.length === 0 ? (
          <p className="muted">
            Kein Profil passt zu {childName} – prüfe Klasse und Schulart am Kind, oder lege unter{" "}
            <Link to="/vater/fachlehrer">Fachlehrer</Link> ein Profil an. Ohne Profil arbeitet der
            KI-Creator als Generalist.
          </p>
        ) : (
          <table className="table">
            <thead><tr><th>Profil</th><th>Punkte</th><th>Warum</th></tr></thead>
            <tbody>
              {matches.data?.map((m, i) => (
                <tr key={m.profile.id}>
                  <td>
                    {i === 0 && <span className="pill lime" style={{ marginRight: 6 }}>beste Wahl</span>}
                    {m.profile.name}
                    {m.profile.seriesName && <div className="muted" style={{ fontSize: 13 }}>{m.profile.seriesName}</div>}
                  </td>
                  <td>{m.score}</td>
                  <td className="muted">
                    {m.reasons.length === 0
                      ? "passt nur allgemein"
                      : m.reasons.map(matchReasonLabel).join(", ")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      <p className="sub">
        Gewichtung: gleiche Buchreihe (8) &gt; gleiches Fach (4) &gt; Klassenstufe (2) &gt; Schulart (1).
        Der KI-Creator nimmt ohne ausdrückliche Angabe genau die obere Zeile.
      </p>
    </div>
  );
}
