import { Fragment, useCallback, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { FieldLabel } from "../components/InfoHint";
import { ReferenceSelect } from "../components/ReferenceSelect";
import { StatusBanner } from "../components/StatusBanner";
import { GrammarTopicPicker } from "../components/GrammarTopicChips";
import { FREETEXT_SUBJECT, seriesFormValues, seriesPatch, type SeriesFormValues } from "./seriesPatch";
import { api } from "../lib/api";
import { publisherOptions, subjectOptions } from "../lib/referenceOptions";
import { nachObenMelden } from "../lib/nachObenMelden";
import { useAction } from "../lib/useAction";
import type { ActionMessage } from "../lib/useAction";
import { SCHOOL_TYPES } from "../lib/labels";
import { LANGUAGES } from "../lib/languages";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import { PublisherAdmin } from "./PublisherAdmin";
import type {
  BookType, CreateSeriesUnitDto, PublisherResponse, SchoolType, SeriesUnitResponse, SubjectResponse,
  TextbookSeriesResponse,
} from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";

/** Anzeigename je Buchtyp; Lehrbuch ist der Regelfall und bleibt darum ohne Chip in der Unit-Zeile. */
const BOOK_TYPE_LABEL: Record<BookType, string> = {
  Textbook: "Lehrbuch", Workbook: "Arbeitsheft", TeacherGuide: "Lehrerhandreichung",
};

/** Band und Buchtyp, mit denen das Formular für eine **neue** Unit startet – Band als Formular-String. */
export type NewUnitDefaults = { grade: string; bookType: BookType };

/*
 * Exportiert für den Test: reine Wertearbeit, ohne gefälschtes `fetch` prüfbar.
 */
/**
 * Vorbelegung für eine neue Unit aus der **letzten** Unit derselben Reihe (remark #2 vom 2026-08-14).
 * Band und Buchtyp identifizieren das Buch, aus dem die Unit stammt, und mussten am Anfang jeder Sitzung
 * neu gesetzt werden: `submit` trägt sie nach dem Anlegen bereits weiter (siehe dort), aber das **erste**
 * Formular nach dem Aufklappen startete leer – obwohl die Reihe ihre Bände längst kennt. Die letzte Unit
 * ist die richtige Vorlage, weil der Server nach Band sortiert (`SeriesUnitsController.cs:42`): wer eine
 * Unit anlegt, arbeitet das Buch von vorn nach hinten durch.
 * <p>
 * Das ersetzt **keine** Band-Ebene im Modell – Band und Unit liegen bewusst auf einer Ebene (B-63).
 * </p>
 */
export function newUnitDefaults(units: SeriesUnitResponse[]): NewUnitDefaults {
  const last = units.length > 0 ? units[units.length - 1] : undefined;
  return { grade: last?.grade?.toString() ?? "", bookType: last?.bookType ?? "Textbook" };
}

/** Die Filterzeile über der Reihenliste, so wie die Formularfelder sie halten: durchweg Strings. */
export type SeriesFilterValues = {
  search: string; subjectId: string; publisherId: string; schoolTypes: SchoolType | ""; grade: string;
};

/*
 * Exportiert für den Test: die Abbildung ist reine Wertearbeit und darum ohne gefälschtes `fetch` prüfbar.
 */
/**
 * Übersetzt die Filterzeile in die Abfrage für `api.textbookSeries`. Der Punkt, an dem es schiefgehen kann,
 * ist die **leere** Auswahl: ein `<select>` ohne Wahl liefert `""`, und `Number("")` ist `0` – als
 * `subjectId=0` ginge daraus ein Filter auf ein Fach hervor, das es nicht gibt, und die Liste bliebe ohne
 * ersichtlichen Grund leer. Leer muss darum `undefined` werden, damit der Parameter ganz wegfällt.
 */
export function seriesFilterQuery(v: SeriesFilterValues) {
  return {
    search: v.search || undefined,
    subjectId: v.subjectId ? Number(v.subjectId) : undefined,
    publisherId: v.publisherId ? Number(v.publisherId) : undefined,
    schoolTypes: v.schoolTypes || undefined,
    grade: v.grade ? Number(v.grade) : undefined,
  };
}

/*
 * Exportiert für den Test: reine Wertearbeit, und die Aussage ist zu wichtig, um in einem JSX-Ausdruck zu
 * verschwinden.
 */
/**
 * Steht irgendein Filter der Zeile? Entscheidet, was eine leere Liste bedeutet (B-173).
 *
 * Ohne diese Unterscheidung sagte die Seite "Noch kein Lehrwerk. **Lege unten eines an.**", auch wenn die
 * Reihe längst existierte und nur ausgefiltert war - eine falsche Handlungsanweisung, die eine Dublette im
 * geteilten Katalog erzeugt.
 */
export function istGefiltert(v: SeriesFilterValues): boolean {
  return Boolean(v.search || v.subjectId || v.publisherId || v.schoolTypes || v.grade);
}

/*
 * Exportiert für den Test: der Weg von der Unit ins Anlege-Formular ist reine Adressen-Arbeit und darum
 * ohne gefälschtes `fetch` prüfbar – die Kaskade dahinter deckt `e2e/creator-lehrwerk-weg.spec.ts` ab.
 */
/**
 * Der Link von einer Unit zum Anlegen einer Übung. Die Auswahl **reist als Query mit** (Frontend-Regel),
 * sonst stünde im Zielformular wieder die erste Reihe.
 *
 * `subjectId` geht mit, obwohl das Formular das Fach nur als Filter der Reihenliste braucht: ohne den Wert
 * stünde dort eine gewählte Reihe über einem leeren Fach – und der erste Griff ins Fach-Feld setzt Reihe
 * und Unit zurück. Der Aufrufer stellt sicher, dass die Reihe ein Fach hat (siehe `UnitPanel`).
 */
export function createExerciseHref(series: TextbookSeriesResponse, seriesUnitId: number): string {
  const q = new URLSearchParams();
  if (series.subjectId != null) q.set("subjectId", String(series.subjectId));
  q.set("seriesId", String(series.id));
  q.set("seriesUnitId", String(seriesUnitId));
  return `/vater/exercises/neu?${q}`;
}

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
  const [subjectId, setSubjectId] = useState("");
  const [publisherId, setPublisherId] = useState("");
  const [schoolTypes, setSchoolTypes] = useState<SchoolType | "">("");
  const [grade, setGrade] = useState("");
  const list = useAsync<TextbookSeriesResponse[]>(
    () => api.textbookSeries(seriesFilterQuery({ search: applied, subjectId, publisherId, schoolTypes, grade })),
    [applied, subjectId, publisherId, schoolTypes, grade]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const publishers = useAsync<PublisherResponse[]>(() => api.publishers(), []);
  const [open, setOpen] = useState<number | null>(null);
  /*
   * Der Meldungsplatz der Seite (B-173). Er MUSS hier liegen und nicht in der Zeile: Ändert ein Speichern
   * einen Wert, nach dem gefiltert oder sortiert wird, verlässt die Zeile das Ergebnis und nimmt ihre
   * eigene Bestätigung mit - ein geglücktes Speichern sah dann aus wie ein Löschen.
   */
  const [meldung, setMeldung] = useState<ActionMessage | null>(null);
  const meldungsplatz = useRef<HTMLDivElement>(null);
  /*
   * Der Preis des einen Platzes, und er ist gemessen: Er steht **unter** der Tabelle, und die Liste hat
   * kein Paging. Bei dreißig Reihen liegt ein Fehler, der beim Speichern in Zeile 3 entsteht, außerhalb
   * des Sichtfelds — der Knopf entgraut, sonst passiert sichtbar nichts. Vorher stand er direkt unter dem
   * Formular, in das der Creator gerade tippt.
   *
   * `block: "nearest"` scrollt darum **nur, wenn nötig**. Ein **Fehler** holt den Platz immer; eine
   * **Bestätigung** nur, wenn ihr Absender weit weg sitzt (B-232: das Verlags-Vokabular, zwei Abschnitte
   * tiefer – dort sah der Creator nur den Knopf verschwinden). Aus einem Formular in oder direkt unter der
   * Tabelle scrollt sie nicht (B-381): wer Units hintereinander anlegt, verlöre sonst bei jedem Speichern sein
   * offenes Formular aus dem Bild. Ob weit weg, sagt der Absender über `meldeVonWeitWeg`, nicht die Seite.
   */
  const holeBestaetigung = useRef(false);
  const melde = useCallback((m: ActionMessage | null) => { holeBestaetigung.current = false; setMeldung(m); }, []);
  const meldeVonWeitWeg = useCallback((m: ActionMessage | null) => { holeBestaetigung.current = true; setMeldung(m); }, []);
  useEffect(() => {
    if (meldung && (!meldung.ok || holeBestaetigung.current))
      meldungsplatz.current?.scrollIntoView({ block: "nearest" });
  }, [meldung]);
  const gefiltert = istGefiltert({ search: applied, subjectId, publisherId, schoolTypes, grade });

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
          {/* Das Fach steht vor dem Verlag: es ist die Facette, nach der ein Vater sucht („was gibt es für
              Englisch"), und der Freitext deckt es bewusst nicht ab – der sucht nur Name, Slug und Verlag. */}
          <select aria-label="Fach-Filter" value={subjectId} onChange={(e) => setSubjectId(e.target.value)}>
            <option value="">– alle Fächer –</option>
            {subjects.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
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

        <ListenAusfall was="Die Reihen" fehler={list.error} laedt={list.loading} onRetry={list.reload} schluessel={JSON.stringify([applied, subjectId, publisherId, schoolTypes, grade])} />
        {/* Die Optionslisten der Formulare (B-172): scheitert einer dieser Abrufe, fehlt im
            Bearbeiten-Formular die Auswahl – ohne diese Zeilen war das nirgends zu sehen. Neben der
            Liste, nicht an ihrer Stelle: die Reihen sind ja da. */}
        <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />
        <ListenAusfall was="Die Verlage" fehler={publishers.error} laedt={publishers.loading} onRetry={publishers.reload} />
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
          <table className="table">
            <thead><tr><th>Reihe</th><th>Fach</th><th>Schulart</th><th>Band</th><th>Units</th><th /></tr></thead>
            <tbody>
              {list.data?.map((s) => (
                <SeriesRow
                  key={s.id} series={s} subjects={subjects.data ?? []} publishers={publishers.data ?? []}
                  open={open === s.id}
                  onToggle={() => { melde(null); setOpen(open === s.id ? null : s.id); }}
                  onChanged={list.reload} onMeldung={melde}
                />
              ))}
              {list.data?.length === 0 && (
                <tr><td colSpan={6} className="muted">
                  {gefiltert
                    ? "Keine Reihe passt zu dieser Filterung. Setze die Filter zurück, um alle zu sehen."
                    : "Noch kein Lehrwerk. Lege unten eines an."}
                </td></tr>
              )}
            </tbody>
          </table>
        )}
        <div ref={meldungsplatz}><StatusBanner message={meldung} /></div>
      </section>

      <NewSeries
        subjects={subjects.data ?? []} publishers={publishers.data ?? []} onMeldung={melde}
        onPublisherCreated={publishers.reload} onCreated={list.reload}
      />
      {/* Auch das Vokabular meldet an den Platz der Seite (B-224) – vorher war es der letzte Platz, an dem
          noch Server-Meldungen standen. */}
      <PublisherAdmin onChanged={() => { publishers.reload(); list.reload(); }} onMeldung={meldeVonWeitWeg} />
    </>
  );
}

function SeriesRow({ series, subjects, publishers, open, onToggle, onChanged, onMeldung }: {
  series: TextbookSeriesResponse;
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  open: boolean;
  onToggle: () => void;
  onChanged: () => void;
  /**
   * Setzt den Meldungsplatz der Seite – oder räumt ihn mit `null`.
   *
   * **Ein** Prop für alle drei Anlässe (B-197): Formular öffnen, Handlung beginnen, Erfolg melden. Der
   * Vorgänger hatte zwei (`onMessage` fürs Setzen, `onOeffnen` fürs Räumen), und genau daran ist die
   * vierte Räum-Stelle entstanden: Alle drei existierenden hingen am *Öffnen*, keine am *Absenden* – so
   * stand ein grünes „Gespeichert." über einer roten Fehlermeldung. Ein Prop mit `null` als Wert kann
   * man nicht halb verdrahten.
   */
  onMeldung: (m: ActionMessage | null) => void;
}) {
  const action = useAction();
  // Lokal, nicht im seiten-globalen `open`: das ist der Akkordeon-Platz der Units, und beide Bereiche
  // sollen sich nicht gegenseitig zuklappen (B-123, Entscheidung 3 – Muster `UnitPanel`).
  const [editing, setEditing] = useState(false);

  function bearbeitenUmschalten() {
    onMeldung(null);
    setEditing(!editing);
  }

  async function remove() {
    if (!confirmAction(
      `Reihe „${series.name}" wirklich löschen? Die ${series.unitCount} Unit(s) gehen mit. `
      + "Lehrbücher der Kinder und Profile verlieren nur die Zuordnung und bleiben nutzbar.")) return;
    // Räumen VOR dem Löschen: Sonst bliebe die Bestätigung der vorigen Handlung über der gerade
    // verschwundenen Zeile stehen und läse sich als Löschbestätigung (B-196).
    onMeldung(null);

    // Der Name MUSS mit: Die Zeile, auf die sich die Meldung bezieht, ist ja gerade verschwunden.
    const ok = await nachObenMelden(action, onMeldung,
      () => api.deleteTextbookSeries(series.id), `Reihe „${series.name}" gelöscht.`);
    if (ok === null) return;
    onChanged();
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
                aria-expanded={editing} onClick={bearbeitenUmschalten}>
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
      {/* Diese Zeile trug bis B-204 einen ZWEITEN Meldungsplatz, und der war die Ursache: Nur `remove`
          schrieb hinein, geräumt wurde er von keinem der beiden Umschalter, und ein gescheitertes
          Löschen stand darum rot unter der Zeile, während jede spätere Handlung ihr grünes
          „Gespeichert." in den Platz der Seite legte. Fünf Wege führten dort hinein — der fünfte über
          eine ganz andere Zeile, deren Löschen gelingt. Jetzt meldet auch der Fehlschlag nach oben.
          **Für die Zeile** war das die Lösung — die Datei trug danach aber weiter zwei Plätze an den
          FORMULAREN (`SeriesForm`, `UnitForm`), und über die lief derselbe Widerspruch ein zweites Mal
          (B-212). Seither gilt der Schnitt für die ganze Datei: Was vom Server kommt, reist nach oben
          (`nachObenMelden`); am Formular bleibt nur, was der Bildschirm selbst festgestellt hat. Von den
          vier `action.message`-Plätzen der Datei sind zwei ganz verschwunden (`NewSeries`), und die zwei
          verbliebenen tragen nur noch Eingabeprüfungen — die verlassen ihr Formular nie, weil bei ihnen
          nichts passiert ist. Damit gilt B-197s Regel konstruktiv statt über einen Räum-Aufruf je Tür.
          Nebenwirkung, die den a11y-Fund miterledigt: der Seitenplatz rendert seine Live-Region
          unbedingt (`StatusBanner`), dieser hier entstand mit seinem Text und blieb darum stumm. */}
      {editing && (
        <tr>
          <td colSpan={6}>
            <SeriesForm
              series={series} subjects={subjects} publishers={publishers} onSaved={onChanged}
              onMeldung={onMeldung}
            />
          </td>
        </tr>
      )}
      {open && (
        <tr>
          <td colSpan={6}>
            {!series.isOwn && <SeriesEigentumsHinweis series={series} />}
            <UnitPanel series={series} onChanged={onChanged} onMeldung={onMeldung} />
          </td>
        </tr>
      )}
    </>
  );
}

/**
 * Warum eine nicht eigene Reihe nicht änderbar ist – mit den zwei Fällen unterschieden, die B-154 für das
 * Fach eingeführt hat (`CatalogAdmin.tsx`): „hat jemand anderes angelegt" wäre bei einer Reihe aus dem
 * Grundbestand falsch – sie gehört niemandem, und im Seed sind das alle (B-159). `== null` deckt `null` und
 * ein fehlendes Feld gleichermaßen (der Vertrag gibt `ownerAdultId` optional heraus).
 *
 * Exportiert nur für den Test: `SeriesRow` ruft beim Aufklappen die Units ab, dieses Bauteil ist reine
 * Prop-Arbeit.
 */
export function SeriesEigentumsHinweis({ series }: { series: TextbookSeriesResponse }) {
  return (
    <p className="muted">
      {series.ownerAdultId == null
        ? "Diese Reihe gehört zum Grundbestand – du kannst sie verwenden, aber niemand kann sie ändern."
        : "Diese Reihe hat jemand anderes angelegt – du kannst sie verwenden, aber nicht ändern."}
    </p>
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
export function SeriesForm({ series, subjects, publishers, onSaved, onMeldung }: {
  series: TextbookSeriesResponse;
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  onSaved: () => void;
  /**
   * Der Meldungsplatz der Seite. **Pflicht** seit B-212, und der frühere `?` war der Grund, warum es
   * schiefging: Was vom Server kommt — Erfolg **und** Fehler — geht dorthin, weil die Zeile aus der
   * gefilterten Abfrage fallen kann (B-173) und ein zweiter Platz von niemandem geräumt werden kann als
   * von dem Formular, in dem er steckt. Hier unten bleibt nur, was der Bildschirm **selbst** festgestellt
   * hat: „Nichts geändert." — dabei ist nichts passiert, das die Zeile überdauern könnte. (Ein leerer Name
   * meldet seit B-233 nichts mehr: der Knopf ist gesperrt, das Label sagt „Pflicht".)
   *
   * `null` räumt den Platz. Das Formular ruft es zu Beginn jedes Absendens (B-197): Sonst stünde die
   * Bestätigung des vorigen Versuchs noch da, während dieser läuft.
   */
  onMeldung: (m: ActionMessage | null) => void;
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
    // Pflichtfeld leer: der Knopf ist gesperrt (B-233), also auch kein Absenden per Enter. Die Zeile bleibt
    // als stiller Riegel – VOR dem Räumen, denn ein Versuch, der nicht stattfindet, räumt nichts.
    if (!form.name.trim()) return;
    // Der Platz gilt ab jetzt nicht mehr – egal wie dieser Versuch ausgeht (B-197).
    onMeldung(null);

    const dto = seriesPatch(geladen.current, form);
    // Kein leerer PATCH: er wäre erfolgreich und folgenlos, und „Gespeichert." wäre dann eine Lüge.
    if (dto === null) { action.succeed("Nichts geändert."); return; }

    /*
     * Die Bestätigung geht NACH OBEN – der Platz der Seite ist seit B-212 immer da (B-173) – und NICHT zusätzlich
     * hierher: Ändert das Speichern einen Wert, nach dem gefiltert oder sortiert wird, verlässt die Zeile
     * das Ergebnis und nimmt ihren eigenen Banner mit. Beides gleichzeitig zu zeigen wäre kein
     * „doppelt hält besser", sondern derselbe Satz zweimal auf einem Bildschirm (vier E2E sind daran
     * gebrochen, gemessen am 2026-09-01).
     *
     * **Und seit B-212 gilt dasselbe für den Fehler.** „Nur der Erfolg reist" ließ ein rotes
     * „Name schon vergeben" hier unten stehen, wo es niemand mehr erreichen konnte: Der Creator machte an
     * einer anderen Reihe weiter, deren Meldung in den Platz der Seite ging, und beides stand
     * gleichzeitig im Blickfeld. Am Formular bleibt jetzt nur, was der Bildschirm SELBST festgestellt hat
     * („Nichts geändert.") – das verlässt die Zeile nie, weil dabei nichts passiert ist.
     */
    const aktualisiert = await nachObenMelden(action, onMeldung,
      () => api.updateTextbookSeries(series.id, dto), "Gespeichert.");
    if (aktualisiert === null) return;
    // Das Formular bleibt offen, und darum MUSS der Ladezustand aus der ANTWORT nachgezogen werden:
    // sonst rechnete der nächste Diff derselben Sitzung gegen einen veralteten Bezugspunkt und schickte
    // Felder erneut oder gar nicht.
    //
    // Die zweite Hälfte der alten Begründung ist seit B-173 überholt und darum entfernt: Sie sagte, ein
    // Schließen ließe „den StatusBanner mitsamt der Bestätigung verschwinden". Das trägt der Banner hier
    // nicht mehr – die Bestätigung geht an den Meldungsplatz der Seite, weil die Zeile auch bei OFFENEM
    // Formular verschwinden kann (aus der Filterung fallen). Stehen bleibt die Begründung oben, und die
    // genügt.
    geladen.current = seriesFormValues(aktualisiert);
    setForm(geladen.current);
    onSaved();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <FieldLabel htmlFor={`se-name-${id}`} topic="seriesName">
            Name der Reihe <span className="muted">(Pflicht)</span>
          </FieldLabel>
          <input id={`se-name-${id}`} value={form.name} onChange={(e) => up("name", e.target.value)} />
          {/* Nur der WERT – die Erklärung dazu steht im `HelpTopic` und darf nicht zweimal dastehen
              (`frontend/CLAUDE.md`: der Text steht nie am Feld). Den Wert kann das Popover nicht
              liefern, es kennt die Reihe nicht. */}
          <span className="sub">Kurzname <code>{series.slug}</code></span>
        </div>
        {/* Verlag und Fach über `ReferenceSelect` (B-172): beide Optionslisten kommen aus einem eigenen,
            unabhängig scheiternden Abruf, und beide Felder speisen einen `clear…`-Schalter. Die
            Sonderoptionen (Freitext bzw. „nicht geladen") leitet das Bauteil aus dem GELADENEN Stand ab –
            nicht aus `form`, damit sie stehen bleiben, während der Nutzer etwas anderes probiert. */}
        <div className="field">
          <label htmlFor={`se-publisher-${id}`}>Verlag</label>
          <ReferenceSelect
            id={`se-publisher-${id}`} value={form.publisherId} onChange={(v) => up("publisherId", v)}
            loadedId={series.publisherId} loadedName={series.publisherName}
            options={publisherOptions(publishers)} emptyLabel="– keine Angabe –"
          />
        </div>
        <div className="field">
          <FieldLabel htmlFor={`se-subject-${id}`} topic="seriesSubject">Fach</FieldLabel>
          <ReferenceSelect
            id={`se-subject-${id}`} value={form.subjectId} onChange={(v) => up("subjectId", v)}
            loadedId={series.subjectId} loadedName={series.subjectName}
            options={subjectOptions(subjects)} emptyLabel="– keine Angabe –"
            freetextValue={FREETEXT_SUBJECT}
          />
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
          aria-label={`Speichern: „${series.name}"`} disabled={action.busy || !form.name.trim()}>
          {action.busy ? "Speichere…" : "Speichern"}
        </button>
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}

/**
 * Die Units einer Reihe: Band, Buchtyp, Bezeichnung und – der Kern – der Stoff.
 *
 * Von hier führt auch der Weg zur **Übung**: seit B-106 hängt jede Übung zwingend an einer Unit, und die
 * Unit ist damit die natürliche Stelle, an der man eine anlegt („+ Übung" reicht Fach/Reihe/Unit als Query
 * durch). Ohne diesen Weg musste man sich die Auswahl im Anlege-Formular ein zweites Mal zusammenklicken.
 */
function UnitPanel({ series, onChanged, onMeldung }: {
  series: TextbookSeriesResponse;
  onChanged: () => void;
  /**
   * Räumt bzw. setzt den Meldungsplatz der Seite (B-197). Das Unit-Formular sitzt als aufgeklappte Zeile
   * **in** der Tabelle, der Platz steht direkt darunter — ein roter Fehler hier und ein grünes
   * „Gespeichert." dort sind benachbart, nicht seitenweit getrennt.
   */
  onMeldung: (m: ActionMessage | null) => void;
}) {
  const units = useAsync<SeriesUnitResponse[]>(() => api.seriesUnits(series.id), [series.id]);
  const [editing, setEditing] = useState<number | null>(null);
  /*
   * Übungen darf man auch in einer **fremden** Reihe anlegen – anders als Unit-Bearbeiten prüft
   * `POST …/units/{id}/<typ>` nur, dass die Unit existiert. Der Knopf hängt darum NICHT an `isOwn`.
   *
   * Woran er hängt, ist das **Fach**: eine Reihe ohne gesetztes `subjectId` darf keine Übung tragen
   * (B-106 T-01, `series_without_subject`). Ein Freitext-Fach zählt dabei als „keins" – es steht in
   * `subjectName`, nicht als Katalog-Bezug. Ohne diese Schranke führte der Knopf in ein Formular, das
   * erst beim Absenden mit einem 400 abbricht.
   */
  const canHostExercises = series.subjectId != null;

  return (
    <>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>Units in „{series.name}"</h4>
      {!canHostExercises && (
        <p className="muted">
          Diese Reihe hat <strong>kein Fach</strong> aus dem Katalog – so kann sie keine Übungen tragen.
          Setze es unter „Reihe bearbeiten", dann steht an jeder Unit der Weg zur Übung.
        </p>
      )}
      <ListenAusfall was="Die Units" fehler={units.error} laedt={units.loading} onRetry={units.reload} schluessel={series.id} />
      {units.showPlaceholder ? <div className="loading">Lade…</div> : units.data === null ? null : units.data.length === 0 ? (
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
                  <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
                    {canHostExercises && (
                      // Sichtbarer Text zuerst im `aria-label`, Kontext hinten (WCAG 2.5.3 „Label in Name",
                      // wie in der Reihen-Zeile): sonst löst eine Spracheingabe („Übung") den Link nicht aus.
                      // Der Unit-Name gehört trotzdem hinein – bei zehn Units hört ein Screenreader sonst
                      // zehnmal dasselbe.
                      <Link
                        to={createExerciseHref(series, u.id)} className="btn ghost small"
                        style={{ width: "auto", textDecoration: "none", textAlign: "center" }}
                        aria-label={`+ Übung zu „${u.label}"`}
                      >+ Übung</Link>
                    )}
                    {series.isOwn && (
                      <button
                        type="button" className="btn ghost small" style={{ width: "auto" }}
                        aria-label={`${u.label} bearbeiten`}
                        // Öffnen räumt den Platz der Seite (B-197) – wie `onToggle` und `bearbeitenUmschalten` (B-224).
                        onClick={() => { onMeldung(null); setEditing(editing === u.id ? null : u.id); }}
                      >{editing === u.id ? "Schließen" : "Bearbeiten"}</button>
                    )}
                  </td>
                </tr>
                {editing === u.id && (
                  <tr>
                    <td colSpan={4}>
                      <UnitForm onMeldung={onMeldung}
                        seriesId={series.id} subjectId={series.subjectId ?? null} unit={u}
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

      {/* Erst wenn der Ladeversuch durch ist: `UnitForm` liest seine Vorbelegung im `useState`-Anfangswert,
          also genau einmal beim Einhängen. Stünde das Formular schon während des Ladens da, startete es
          leer und die nachgelieferte Vorbelegung käme nie an – sichtbar wäre nur, dass sie nicht wirkt.
          Das Tor ist darum `showPlaceholder` und NICHT `data !== null`: Scheitert das Lesen der Units,
          bleibt `data` für immer `null`, und mit ihm verschwand das Anlegen-Formular mitsamt jedem Weg
          zurück – ein misslungenes Lesen nahm dem Vater eine Fähigkeit, die davon nicht abhängt.
          Die Vorbelegung ist bloß Bequemlichkeit (Klasse und Buchart der letzten Unit); `[]` liefert die
          neutrale Fassung, mit der das Formular auch für eine Reihe ohne Units startet. */}
      {series.isOwn && !units.showPlaceholder && (
        <div style={{ marginTop: 10 }}>
          <h4 className="h-section" style={{ fontSize: "1rem" }}>Unit hinzufügen</h4>
          <UnitForm onMeldung={onMeldung}
            seriesId={series.id} subjectId={series.subjectId ?? null} defaults={newUnitDefaults(units.data ?? [])}
            onDone={() => { units.reload(); onChanged(); }}
          />
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
export function UnitForm({ seriesId, subjectId, unit, defaults, onDone, onMeldung }: {
  seriesId: number;
  /** Das Fach der Reihe – die Grammatik-Themen (B-155) stehen nur für dieses zur Wahl; ohne Fach keine. */
  subjectId: number | null;
  unit?: SeriesUnitResponse;
  defaults?: NewUnitDefaults;
  onDone: () => void;
  /**
   * Räumt den Meldungsplatz der Seite zu Beginn jeder Handlung (B-197). **Pflicht** — ein `?`-Prop kann
   * man weglassen, und genau daran ist diese Stelle beim ersten Anlauf durchgerutscht.
   */
  onMeldung: (m: ActionMessage | null) => void;
}) {
  // Die Vorbelegung gilt nur beim ANLEGEN, und die Fallunterscheidung hängt am Vorhandensein von `unit`,
  // nicht an seinen Werten: eine bestehende Unit ohne Band soll leer bleiben. Über `unit?.grade ?? defaults`
  // erbte sie beim Bearbeiten den Band der Nachbar-Unit – ein stiller Datenwechsel beim blossen Öffnen.
  const initial = unit
    ? { grade: unit.grade?.toString() ?? "", bookType: unit.bookType }
    : { grade: defaults?.grade ?? "", bookType: defaults?.bookType ?? ("Textbook" as BookType) };
  const [form, setForm] = useState({
    label: unit?.label ?? "", grade: initial.grade,
    bookType: initial.bookType, topics: unit?.topics ?? [] as string[],
    topicInput: "", grammar: unit?.grammar ?? "", vocabularyNotes: unit?.vocabularyNotes ?? "",
    grammarTopicIds: unit ? [...unit.grammarTopicIds] : [] as number[],
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
    // Pflichtfeld leer: Knopf gesperrt (B-233); stiller Riegel vor dem Räumen, wie in `SeriesForm`.
    if (!form.label.trim()) return;
    // Der Meldungsplatz der Seite gilt ab jetzt nicht mehr — egal wie dieser Versuch ausgeht (B-197).
    onMeldung(null);
    const dto: CreateSeriesUnitDto = {
      label: form.label.trim(),
      grade: form.grade.trim() === "" ? null : Number(form.grade),
      bookType: form.bookType,
      topics: form.topics,
      grammar: form.grammar.trim() || null,
      vocabularyNotes: form.vocabularyNotes.trim() || null,
      grammarTopicIds: form.grammarTopicIds,
    };
    /*
     * Nach OBEN, und beim Ändern ist das der ganze Punkt (B-187): `onDone()` unten hängt das
     * Bearbeiten-Formular aus — und mit ihm den Banner, in dem die Bestätigung bis hierher stand. Sie war
     * nie lange genug da, um gelesen zu werden. Dieselbe Wurzel wie B-151, nur an einem dritten Formular.
     *
     * Das Anlege-Formular bleibt zwar montiert und hätte seine Meldung behalten. Es bekommt trotzdem
     * denselben Weg: Zwei Plätze für dieselbe Sorte Meldung sind der Defekt aus B-212, und eine Ausnahme
     * „nur beim Anlegen" wäre genau die Sorte Regel, die niemand im Kopf behält.
     */
    const ok = await nachObenMelden(action, onMeldung, () => (unit
      ? api.updateSeriesUnit(seriesId, unit.id, dto)
      : api.createSeriesUnit(seriesId, dto)), unit ? "Gespeichert." : "Unit hinzugefügt.");
    if (ok === null) return;
    if (!unit) setForm((f) => ({ ...f, label: "", topics: [], grammar: "", vocabularyNotes: "", grammarTopicIds: [] }));
    onDone();
  }

  async function remove() {
    if (!unit) return;
    if (!confirmAction(`Unit „${unit.label}" löschen? Lehrbücher, die darauf zeigen, verlieren nur die Angabe.`)) return;
    // Nach dem Dialog, nicht davor: Ein Abbruch darf den Platz nicht anrühren (dieselbe Reihenfolge
    // wie beim Löschen einer Reihe).
    onMeldung(null);
    // Der Name MUSS mit (B-196, Entscheidung 1): Die Zeile, auf die sich die Meldung bezieht, ist gerade
    // verschwunden. Vorher war dieser Weg vollkommen stumm — der Vater las am Verschwinden der Zeile ab,
    // ob der Server zugestimmt hatte (B-200).
    const ok = await nachObenMelden(action, onMeldung,
      () => api.deleteSeriesUnit(seriesId, unit.id), `Unit „${unit.label}" gelöscht.`);
    if (ok !== null) onDone();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor={`unit-label-${id}`}>Bezeichnung <span className="muted">(Pflicht)</span></label>
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
      {/* Das Thema als Vokabular neben dem Freitext (B-155): der Freitext bleibt für den KI-Creator, die Themen
          sind das, worüber ein Betreuer die Übungen der Unit später quer über Werke findet. */}
      <GrammarTopicPicker subjectId={subjectId} topicIds={form.grammarTopicIds}
        onChange={(ids) => up("grammarTopicIds", ids)} disabled={action.busy} id={`unit-grammar-topics-${id}`} />
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
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }}
          disabled={action.busy || !form.label.trim()}>
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
function NewSeries({ subjects, publishers, onPublisherCreated, onCreated, onMeldung }: {
  subjects: SubjectResponse[];
  publishers: PublisherResponse[];
  onPublisherCreated: () => void;
  onCreated: () => void;
  /** Der Meldungsplatz der Seite — auch dieses Formular hat keinen eigenen mehr (B-212). */
  onMeldung: (m: ActionMessage | null) => void;
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
    onMeldung(null);
    // Neutral formuliert wie bei der Reihe: der Slug macht das idempotent, ein zweiter Versuch mit
    // demselben Namen legt nichts an, sondern liefert den bestehenden Verlag zurück. Der Name kommt
    // darum aus der ANTWORT und nicht aus dem Feld — deshalb die Funktion statt eines festen Textes.
    const created = await nachObenMelden(publisherAction, onMeldung,
      () => api.createPublisher({ name }), (p) => `„${p.name}" steht im Vokabular.`);
    if (created === null) return;
    onPublisherCreated();
    setForm((f) => ({ ...f, publisherId: String(created.id), newPublisher: "" }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) return;
    onMeldung(null);
    /*
     * Auch dieses Formular meldet nach oben (B-212). Es schließt zwar nie, seine Bestätigung ginge also
     * nicht verloren — aber ein zweiter Platz erzeugt denselben Widerspruch von der anderen Seite: Ein
     * grünes „… steht im Katalog" stünde hier, während ein gescheitertes Speichern an einer Unit sein Rot
     * in den Platz der Seite legt. **Ein** Platz, ohne Ausnahme, ist die Regel, die man im Kopf behält.
     *
     * Die Meldung nennt den Namen **des Servers**: gleicher Slug = gleiche Reihe, ein zweites Anlegen
     * liefert die bestehende zurück – und die kann anders geschrieben sein als das Feld.
     */
    const created = await nachObenMelden(action, onMeldung, () => api.createTextbookSeries({
      name: form.name.trim(),
      publisherId: form.publisherId ? Number(form.publisherId) : null,
      subjectId: form.subjectId ? Number(form.subjectId) : null,
      schoolTypes: form.schoolTypes === "None" ? null : form.schoolTypes,
      sourceLanguage: form.sourceLanguage || null,
      targetLanguage: form.targetLanguage || null,
      notes: form.notes.trim() || null,
    }), (s) => `„${s.name}" steht im Katalog. Jetzt die Units mit ihrem Stoff anlegen.`);
    if (created === null) return;
    setForm({ ...form, name: "", notes: "" });
    onCreated();
  }

  return (
    <section>
      <h3 className="h-section">Lehrwerk hinzufügen</h3>
      <form className="form-grid" style={{ alignItems: "end" }} onSubmit={submit}>
        <div className="field">
          {/* Der Grund für den gesperrten Knopf steht am Feld, nicht in einem `title` am Knopf (B-160, B-225). */}
          <label htmlFor="ns-name">Reihe <span className="muted">(Pflicht)</span></label>
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
        {/* Ohne Namen gesperrt, wie der Verlags-Knopf unten (B-225): vorher war er klickbar und `submit`
            kehrte stumm zurück – seit B-212 ohne einen Platz, der etwas hätte sagen können. */}
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy || !form.name.trim()}>
          {action.busy ? "Lege an…" : "Reihe anlegen"}
        </button>
      </form>
      <p className="sub" style={{ marginTop: 8 }}>
        Gleicher Name = gleiche Reihe: ein zweites Anlegen liefert die bestehende zurück, statt eine
        Dublette in den geteilten Katalog zu schreiben.
      </p>
      {/* Kein eigener Meldungsplatz mehr (B-212): Was vom Server kommt, steht am Platz der Seite. Hier
          bleibt nichts übrig — dieses Formular meldet keine Eingabeprüfung (die `return`s in `submit` und
          `addPublisher` schützen nur vor programmatischen Absendungen: ein gesperrter Default-Submit-Knopf
          unterbindet auch Enter im Feld), beide Knöpfe sind ohne Eingabe gesperrt (B-225). */}

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
    </section>
  );
}
