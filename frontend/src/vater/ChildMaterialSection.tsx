import { Fragment, useEffect, useId, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { FieldLabel } from "../components/InfoHint";
import { ReferenceSelect } from "../components/ReferenceSelect";
import { StatusBanner } from "../components/StatusBanner";
import { FREETEXT_SUBJECT } from "./subjectField";
import { EMPTY_TEXTBOOK_FORM, textbookFormValues, textbookPatch, type TextbookFormValues } from "./textbookPatch";
import { api } from "../lib/api";
import { seriesOptions, subjectOptions, unitOptions } from "../lib/referenceOptions";
import { useAction } from "../lib/useAction";
import type { ActionMessage } from "../lib/useAction";
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
  /*
   * Der Meldungsplatz der Seite (B-151). Das Bearbeiten-Formular trägt seinen eigenen `StatusBanner` –
   * und schließt sich beim Speichern, nimmt ihn also mitsamt der Bestätigung aus dem DOM. Die Meldung
   * reist darum beim Schließen mit `onDone` hier herauf.
   *
   * Offen bleiben lassen wäre der Reflex und ist hier VERBOTEN: der `useRef`-Bezugspunkt des PATCH-Diffs
   * zieht nicht aus der Antwort nach, gerade WEIL das Formular schließt (siehe `TextbookForm`).
   */
  const [meldung, setMeldung] = useState<ActionMessage | null>(null);

  return (
    <section>
      <h3 className="h-section">Unterrichtsmaterial</h3>
      <p className="sub">
        Woraus lernt {childName} gerade? Mit hinterlegter <strong>Reihe und Unit</strong> kennt der
        KI-Creator den Stoff; ohne sie muss er ihn erfinden. Reihen und ihre Units pflegst du unter{" "}
        <Link to="/vater/lehrwerke">Lehrwerke</Link>.
      </p>

      {books.error && <div className="banner err">{books.error}</div>}
      {/* Die Reihen speisen das Auswahlfeld der Formulare (B-172) – scheitert der Abruf, fehlt dort die
          Auswahl, und ohne diese Zeile war das nicht zu sehen. */}
      {series.error && <div className="banner err">Reihen: {series.error}</div>}
      {books.showPlaceholder ? <div className="loading">Lade…</div> : books.data === null ? null : books.data.length === 0 ? (
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
                      onClick={() => { setMeldung(null); setEditing(editing === b.id ? null : b.id); }}
                    >{editing === b.id ? "Schließen" : "Bearbeiten"}</button>
                  </td>
                </tr>
                {editing === b.id && (
                  <tr>
                    <td colSpan={5}>
                      <TextbookForm
                        childId={childId} book={b} series={series.data ?? []} subjects={subjects}
                        onDone={(m) => { setEditing(null); books.reload(); if (m) setMeldung(m); }}
                        onMeldung={setMeldung} onSeriesCreated={series.reload}
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
        {/* Auch das Anlege-Formular räumt (B-197): Es schließt sich NICHT, kann eine alte Meldung
            also überleben lassen – „Gespeichert." von einer Bearbeitung stünde dann neben dem roten
            Fehler eines gescheiterten Anlegens. */}
        <TextbookForm
          childId={childId} series={series.data ?? []} subjects={subjects}
          onDone={books.reload} onMeldung={setMeldung} onSeriesCreated={series.reload}
        />
      </div>

      <StatusBanner message={meldung} />

      {/* Erst mit geladenen Büchern: Das Panel leitet sein Fach AUS ihnen ab. Vorher (`?? []`) fragte es
          erst ohne Fach und gleich darauf noch einmal mit – eine überflüssige Runde und dazwischen eine
          Trefferliste, die schon wieder falsch war. */}
      {books.data
        ? <MatchPanel childId={childId} childName={childName} books={books.data} />
        : books.showPlaceholder ? <div className="loading">Lade…</div> : null}
    </section>
  );
}

/**
 * Ein Formular für Anlegen und Ändern. Die Unit-Auswahl hängt an der gewählten Reihe – der Server
 * weist eine Unit aus einem fremden Werk ab, und das zu Recht: sonst stünde am Kind der Stoff eines
 * Buchs, das es nicht benutzt.
 *
 * Anlegen und Bearbeiten bleiben **ein** Formular (B-148, Entscheidung 3) – anders als bei der Reihe,
 * wo `NewSeries` und `SeriesForm` getrennt sind. Dort war der Grund die Feldsichtbarkeit: beim Anlegen
 * einer Reihe gibt es Felder, die es nicht geben soll. Hier ist das Formular in beiden Modi dasselbe,
 * und eine Trennung verdoppelte nur das Rendern.
 */
/*
 * Exportiert **nur für den Test** (B-148), wie `SeriesForm`: Die gesperrte Freitext-Option ist am
 * Formular zu sehen, nicht an der Regel – und `textbookPatch` erreicht sie nicht. Die Komponente
 * braucht dafür kein gefälschtes `fetch`, solange das Buch keine Reihe trägt: nur die Unit-Liste lädt
 * beim Rendern, alles andere hängt am Absenden.
 */
export function TextbookForm({ childId, book, series, subjects, onDone, onMeldung, onSeriesCreated }: {
  childId: number;
  book?: TextbookResponse;
  series: TextbookSeriesResponse[];
  subjects: SubjectResponse[];
  /**
   * Schließt bzw. lädt neu. Das optionale Argument ist die Bestätigung, die MITREISEN muss: Der eigene
   * `StatusBanner` unten hängt am Formular, und das Formular ist nach diesem Aufruf weg (B-151).
   */
  onDone: (message?: ActionMessage) => void;
  /**
   * Räumt bzw. setzt den Meldungsplatz der Seite. Gerufen zu **Beginn** jedes Absendens mit `null`
   * (B-197): Der Platz gilt ab dann nicht mehr, egal wie der Versuch ausgeht.
   *
   * **Pflicht, nicht optional** — und das ist der Punkt: Ein `?`-Prop kann man ganz weglassen, und genau
   * so entsteht die nächste Aufrufstelle ohne Räumen. Hier verzweigt nichts auf seine Abwesenheit, also
   * gibt es keinen Grund für die Lücke. (Seit B-212 gilt dasselbe für `SeriesForm`: auch dort ist der
   * Prop Pflicht, der Zweig „lokal bleiben" existiert nicht mehr.)
   *
   * Zwei Rückkanäle neben `onDone` sind trotzdem nicht schön und hier ausgesprochen: `onDone` gehört zum
   * Schließen, `onMeldung` zum Platz — und der Anlege-Zwilling schließt nie.
   */
  onMeldung: (m: ActionMessage | null) => void;
  /**
   * Eine Reihe wurde aus diesem Formular heraus angelegt (B-64): die Liste der Seite lädt neu, damit auch
   * das zweite Formular sie anbietet. Pflicht aus demselben Grund wie `onMeldung`.
   */
  onSeriesCreated: () => void;
}) {
  // Der Ladezustand ist der Bezugspunkt des Diffs (B-148) – er darf sich NICHT mit dem Formular
  // mitbewegen, sonst entscheidet der Vergleich gegen sich selbst und der Schalter entstünde wieder aus
  // dem Momentanwert. Darum `useRef`. Beim Anlegen gibt es keinen Bezugspunkt: dort ist er `null`.
  //
  // Er wird bewusst NICHT nachgezogen, und das ist an eine Bedingung geknüpft: Das Bearbeiten-Formular
  // hängt an `{editing === b.id && …}` und `onDone` schließt es beim Speichern – es kann also gar nicht
  // veralten. Wer es (wie `SeriesForm`) offen stehen lässt, muss den Bezugspunkt aus der ANTWORT
  // nachziehen, sonst rechnet das zweite Speichern derselben Sitzung gegen einen alten Stand.
  const geladen = useRef(book ? textbookFormValues(book) : null);
  const [form, setForm] = useState<TextbookFormValues>(geladen.current ?? EMPTY_TEXTBOOK_FORM);
  const action = useAction();
  const id = book ? `tb${book.id}` : "tbnew";
  const gruppe = useId();

  /*
   * Reihen, die dieses Formular selbst angelegt hat (B-64, Entscheidung 2). Sie stehen sofort in der
   * Auswahl, nicht erst nach dem Neuladen der Seitenliste – sonst zeigte das Feld für die Dauer des Abrufs
   * eine gewählte Id ohne Namen.
   */
  const [angelegt, setAngelegt] = useState<TextbookSeriesResponse[]>([]);
  const alleReihen = [...series, ...angelegt.filter((a) => !series.some((s) => s.id === a.id))];
  const [neueReiheOffen, setNeueReiheOffen] = useState(false);
  const [neueReihe, setNeueReihe] = useState("");
  /*
   * Wohin der Fokus nach dem Öffnen bzw. Schließen des Anlege-Streifens geht (B-268). Schließt der Streifen,
   * verschwindet mit ihm der gerade fokussierte Knopf, und der Fokus fiele auf `body` – eine Tastaturnutzerin
   * verlöre ihren Platz. Gesetzt wird er nach dem Rendern, weil das Ziel (Namensfeld) erst dann existiert.
   *
   * Ein Ref, kein Zustand: jeder Handler, der ein Ziel setzt, ändert ohnehin den Streifen und löst damit ein
   * Rendern aus. Als Zustand wäre dasselbe Ziel zweimal hintereinander („name“ – per Knopf zu – wieder „name“) kein
   * Wechsel, und der Effekt liefe beim zweiten Öffnen nicht (Review 2026-10-26).
   */
  const fokusZiel = useRef<"name" | "reihe" | "knopf" | null>(null);
  const setFokusZiel = (ziel: "name" | "reihe" | "knopf") => { fokusZiel.current = ziel; };
  const neueReiheKnopf = useRef<HTMLButtonElement | null>(null);
  useEffect(() => {
    const ziel = fokusZiel.current;
    if (ziel === null) return;
    fokusZiel.current = null;
    (ziel === "knopf" ? neueReiheKnopf.current
      : document.getElementById(ziel === "name" ? `${id}-neue-reihe` : `${id}-series`))?.focus();
  });

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

  /*
   * Verlag und Klasse kommen aus dem Katalog, sobald er sie kennt (B-64, Entscheidung 3) – berechnet aus der
   * AKTUELLEN Auswahl, nicht aus dem Ladezustand, damit ein Reihenwechsel sofort den neuen Verlag zeigt.
   * Das Freitextfeld verschwindet dann, sein Wert bleibt aber im Formular und am Server stehen: löst der
   * Vater die Verknüpfung, taucht er wieder auf. Kennt der Katalog den Wert nicht (Reihe ohne Verlag, Unit
   * ohne Band), bleibt das Feld zum Tippen da.
   */
  const gewaehlteReihe = alleReihen.find((s) => s.id === seriesId);
  const katalogVerlag = gewaehlteReihe?.publisherName ?? null;
  const gewaehlteUnit = (units.data ?? []).find((u) => String(u.id) === form.currentUnitId && u.seriesId === seriesId);
  const katalogKlasse = gewaehlteUnit?.grade ?? null;

  async function reiheAnlegen() {
    const name = neueReihe.trim();
    if (!name) { action.fail("Der Name der Reihe fehlt."); return; }
    onMeldung(null);
    // Das Fach des Buchs wandert mit, solange es ein Katalog-Fach ist; ein Freitext-Fach hat keine Id.
    const subjectId = /^\d+$/.test(form.subjectId) ? Number(form.subjectId) : null;
    let neu: TextbookSeriesResponse | null = null;
    // Der Server legt über den Slug idempotent an: gibt es die Reihe schon, kommt sie zurück statt einer
    // Dublette. Darum „steht im Katalog“ und nicht „angelegt“ – beides ist danach wahr.
    const ok = await action.run(async () => {
      neu = await api.createTextbookSeries({
        name, publisherId: null, subjectName: null, subjectId,
        schoolTypes: null, sourceLanguage: null, targetLanguage: null, notes: null,
      });
    }, `Reihe „${name}“ steht im Katalog und ist gewählt.`);
    if (!ok || neu === null) {
      // Der Knopf war während des Abrufs gesperrt und hat dabei im Browser den Fokus an `body` abgegeben. Zurück ins
      // Namensfeld, wo der Vater korrigiert oder es noch einmal versucht. Direkt, nicht über `fokusZiel`: gerendert
      // ist schon, der Streifen bleibt offen, ein Ziel bliebe bis zum nächsten fremden Rendern liegen.
      document.getElementById(`${id}-neue-reihe`)?.focus();
      return;
    }
    const reihe: TextbookSeriesResponse = neu;
    setAngelegt((cur) => [...cur.filter((a) => a.id !== reihe.id), reihe]);
    chooseSeries(String(reihe.id));
    setNeueReihe("");
    setNeueReiheOffen(false);
    // Auf die Auswahl, die eben gesetzt wurde: dort prüft der Vater, ob die richtige Reihe gewählt ist.
    setFokusZiel("reihe");
    onSeriesCreated();
  }

  // Gehört die gespeicherte Unit noch zur gewählten Reihe? Entscheidet, ob sie als Rückfall-Option
  // angeboten werden darf (B-172). Beim Anlegen ist `geladen.current` null, der Vergleich also falsch.
  const eigeneUnit = geladen.current?.seriesId === form.seriesId;

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    // Der Meldungsplatz der Seite gilt ab jetzt nicht mehr – egal wie dieser Versuch ausgeht (B-197).
    onMeldung(null);
    if (!form.title.trim()) { action.fail("Der Titel fehlt."); return; }

    if (book && geladen.current) {
      // Beim Ändern nur das Geänderte, und „leer" ausdrücklich über die Schalter (der Server überliest
      // `null` als „nicht angegeben"). Welche Felder das sind, entscheidet der Vergleich – nicht der
      // Momentanwert: sonst kostete jedes Speichern den Fachnamen eines gelöschten Fachs (B-148).
      const patch = textbookPatch(geladen.current, form);
      // Kein leerer PATCH: er wäre erfolgreich und folgenlos, und „Gespeichert." wäre dann eine Lüge.
      if (patch === null) { action.succeed("Nichts geändert."); return; }

      // Ein Text, zwei Anzeigeorte: der Banner im Formular (das gleich zugeht) und der Platz auf
      // Seitenebene. Zwei Literale nebeneinander divergieren beim ersten Textpflege-Durchgang lautlos.
      const bestaetigung = "Gespeichert.";
      if (!await action.run(() => api.updateChildTextbook(childId, book.id, patch), bestaetigung)) return;
      onDone({ ok: true, text: bestaetigung });
      return;
    }

    // Anlegen: kein Bezugspunkt, also auch kein Schalter – ein neues Buch hat nichts zu leeren.
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
    if (!await action.run(() => api.createChildTextbook(childId, dto), "Buch hinterlegt.")) return;
    setForm({ ...form, title: "", currentChapter: "" });
    onDone();
  }

  async function remove() {
    if (!book) return;
    if (!confirmAction(`Buch „${book.title}" entfernen?`)) return;
    // Ein Text, zwei Anzeigeorte – wie beim Speichern daneben. Der Name MUSS mit (B-196): Die Zeile,
    // auf die sich die Meldung bezieht, ist gleich verschwunden. Vorher war dieser Weg vollkommen stumm,
    // und der Vater las am Verschwinden der Zeile ab, ob der Server zugestimmt hatte (B-200).
    // Der Meldungsplatz der Seite gilt ab jetzt nicht mehr – dieselbe Reihenfolge wie beim Absenden, und
    // nach dem Dialog: Ein Abbruch darf ihn nicht anrühren.
    onMeldung(null);
    const bestaetigung = `Buch „${book.title}" entfernt.`;
    if (await action.run(() => api.deleteChildTextbook(childId, book.id), bestaetigung)) {
      onDone({ ok: true, text: bestaetigung });
    }
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      {/* B-64, Entscheidung 1: der Katalog ist der Vorschlagsweg und steht darum zuerst; der Freitext ist
          die ausdrücklich benannte Ausweiche dahinter, nicht ein gleichrangiges Feld im selben Raster. */}
      <div role="group" aria-labelledby={`${gruppe}-katalog`} style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        <span id={`${gruppe}-katalog`} className="label-row group-label">
          Aus dem Katalog <span className="muted">(empfohlen – damit kennt der KI-Creator den Stoff)</span>
        </span>
        <div className="form-grid" style={{ alignItems: "end" }}>
          <div className="field">
            <label htmlFor={`${id}-series`}>Reihe im Katalog</label>
            <ReferenceSelect
              id={`${id}-series`} value={form.seriesId} onChange={chooseSeries}
              loadedId={book?.seriesId} loadedName={book?.seriesName}
              options={seriesOptions(alleReihen)} emptyLabel="– nicht katalogisiert –"
            />
            <button
              type="button" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start" }}
              aria-expanded={neueReiheOffen} ref={neueReiheKnopf}
              onClick={() => { const auf = !neueReiheOffen; setNeueReiheOffen(auf); if (auf) setFokusZiel("name"); }}
            >+ Neue Reihe</button>
          </div>
          <div className="field">
            <label htmlFor={`${id}-unit`}>Aktuelle Unit</label>
            {/* Die gespeicherte Unit nur anbieten, solange die Reihe noch ihre eigene ist: nach einem
                Reihenwechsel gehörte sie zur alten und wäre als Auswahl falsch – `chooseSeries` räumt sie
                aus demselben Grund. Ohne diese Bedingung könnte der Nutzer eine fremde Unit-Id abschicken.

                Id UND Name hängen an derselben Bedingung, nicht nur die Id: `loadedId == null` bei
                gesetztem Namen ist die Signatur, an der das Bauteil den FREITEXT-Zustand erkennt. Heute
                fiele das nicht auf (die Unit übergibt keinen Sentinel), aber es wäre eine Falle für den,
                der ihr je einen gibt.

                Und die Optionsliste bei einem Fehler leeren: `useAsync` lässt `data` über den Dep-Wechsel
                stehen, sonst bötte das Feld nach einem gescheiterten Wechsel weiter die Units der ALTEN
                Reihe an – während das Banner darunter den Ausfall meldet. */}
            <ReferenceSelect
              id={`${id}-unit`} value={form.currentUnitId} onChange={(v) => up("currentUnitId", v)}
              loadedId={eigeneUnit ? book?.currentUnitId : null}
              loadedName={eigeneUnit ? book?.currentUnitLabel : null}
              // Die Liste lädt HIER, nicht auf Seitenebene – ohne `laedt` stünde beim Öffnen für die Dauer des
              // Abrufs die Beschriftung des Ausfalls (B-235, dieselbe Lage wie B-229 am Kind-Feld). `loading`
              // und nicht `showPlaceholder`: die Liste hängt am Reihen-Schlüssel und lädt bei jedem Wechsel neu,
              // `showPlaceholder` ist aber nach dem ersten Abschluss dauerhaft falsch – zurück zur eigenen
              // Reihe hieße es sonst wieder „(nicht geladen)". Nach einem Fehlschlag ist `loading` ebenso
              // falsch, also bleibt dort die Ausfall-Beschriftung mit dem Banner darunter.
              laedt={units.loading}
              // Zwei Bedingungen, jede für einen eigenen Fall: `units.error` deckt den gescheiterten
              // Abruf (B-172), der Reihen-Schlüssel das Ladefenster eines Reihenwechsels (B-195).
              // Die eine ersetzt die andere nicht.
              options={unitOptions(units.error ? [] : units.data ?? [], seriesId)}
              emptyLabel={seriesId == null ? "– erst Reihe wählen –"
                // Ohne dieses Zwischenwort liest sich die (zu Recht) geleerte Liste im Ladefenster als
                // „diese Reihe hat keine Units" – stumm zu leeren ist die andere Falle (B-116).
                : units.loading ? "– lädt… –" : "– keine –"}
              disabled={seriesId == null}
            />
            {/* `units.data` ist hier bewusst **roh** gelesen und nicht gefiltert: gefiltert ist die Liste im
                Ladefenster immer leer, und der Satz erschiene bei JEDEM Reihenwechsel kurz. Stattdessen
                schweigt er, solange die Antwort nicht da ist — sonst schickt er den Vater zum Anlegen,
                während die Units der neuen Reihe gerade unterwegs sind. */}
            {seriesId != null && !units.loading && !units.error && units.data?.length === 0 && (
              <span className="sub">Diese Reihe hat noch keine Units – unter „Lehrwerke" anlegen.</span>
            )}
            {/* Der Hinweis oben prüft `length === 0` und schweigt darum bei `null`, also genau im
                Fehlerfall (B-172). Ohne diese Zeile sah ein gescheiterter Abruf aus wie „lädt noch". */}
            {units.error && <div className="banner err">Units: {units.error}</div>}
          </div>
        </div>
        {neueReiheOffen && (
          <div className="row" style={{ gap: 6, alignItems: "flex-end", flexWrap: "wrap", marginTop: 6 }}>
            <div className="field" style={{ flex: 1, minWidth: 200 }}>
              <label htmlFor={`${id}-neue-reihe`}>Name der neuen Reihe</label>
              <input
                id={`${id}-neue-reihe`} value={neueReihe} onChange={(e) => setNeueReihe(e.target.value)} placeholder="Access"
                // Kein eigenes <form> (Formulare schachteln nicht): Enter legt die Reihe an, statt das Buch abzuschicken.
                onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); void reiheAnlegen(); } }}
              />
            </div>
            <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={() => void reiheAnlegen()}>
              Reihe anlegen &amp; wählen
            </button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              onClick={() => { setNeueReiheOffen(false); setNeueReihe(""); setFokusZiel("knopf"); }}>Abbrechen</button>
            <span className="sub" style={{ flexBasis: "100%" }}>
              Angelegt wird nur der Name{/^\d+$/.test(form.subjectId) ? " mit dem gewählten Fach" : ""}; Verlag und Units
              pflegst du unter „Lehrwerke“ nach.
            </span>
          </div>
        )}
      </div>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor={`${id}-title`}>Titel</label>
          <input id={`${id}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Access 8" />
        </div>
        <div className="field">
          <FieldLabel htmlFor={`${id}-subject`} topic="textbookSubject">Fach</FieldLabel>
          {/* Fach, Reihe und Unit über `ReferenceSelect` (B-172): drei Referenzen, drei eigene Abrufe, drei
              `clear…`-Schalter. Die Sonderoptionen (Freitext bzw. „nicht geladen") leitet das Bauteil aus
              `book` ab, dem geladenen Stand – nicht aus `form`, damit sie stehen bleiben, während der
              Nutzer etwas anderes probiert. */}
          <ReferenceSelect
            id={`${id}-subject`} value={form.subjectId} onChange={(v) => up("subjectId", v)}
            loadedId={book?.subjectId} loadedName={book?.subjectName}
            options={subjectOptions(subjects)} emptyLabel="– keine Angabe –"
            freetextValue={FREETEXT_SUBJECT}
          />
        </div>
      </div>
      <div role="group" aria-labelledby={`${gruppe}-freitext`} style={{ display: "flex", flexDirection: "column", gap: 4 }}>
        <span id={`${gruppe}-freitext`} className="label-row group-label">
          Ohne Katalog <span className="muted">(Freitext – nur, was der Katalog nicht weiß)</span>
        </span>
        <div className="form-grid" style={{ alignItems: "end" }}>
          <div className="field">
            {/* Kennt der Katalog den Verlag, gibt es hier nichts zu tippen – ein zweites Feld daneben wäre genau
                die doppelte Wahrheit, die B-64 schließt. */}
            {katalogVerlag !== null ? (
              <>
                <span className="label-row">Verlag</span>
                <span>{katalogVerlag} <span className="muted">· aus dem Katalog</span></span>
              </>
            ) : (
              <>
                <label htmlFor={`${id}-publisher`}>Verlag</label>
                <input id={`${id}-publisher`} value={form.publisher} onChange={(e) => up("publisher", e.target.value)} placeholder="z. B. Cornelsen" />
              </>
            )}
          </div>
          <div className="field">
            {katalogKlasse !== null ? (
              <>
                <span className="label-row">Für Klasse</span>
                <span>{katalogKlasse} <span className="muted">· aus der Unit</span></span>
              </>
            ) : (
              <>
                <label htmlFor={`${id}-grade`}>Für Klasse</label>
                <input
                  id={`${id}-grade`} type="number" min={1} max={13} value={form.grade}
                  // „z. B.“ vorweg: nach dem Entkoppeln ist das Feld leer, und ein nacktes Beispiel läse sich als Wert (B-278).
                  onChange={(e) => up("grade", e.target.value)} placeholder="z. B. 8"
                />
              </>
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
