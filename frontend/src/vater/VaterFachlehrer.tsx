import { Fragment, useRef, useState } from "react";
import { FieldLabel } from "../components/InfoHint";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import {
  applySeriesChange, FIELD_FALLBACKS, isDerived as derived,
  type DerivableField, type DerivableValues,
} from "./seriesDerivation";
import { profileFormValues, profilePatch } from "./profilePatch";
import { FREETEXT_SUBJECT, subjectFormValue } from "./subjectField";
import type {
  CreateCreatorProfileDto, CreatorProfileResponse, SchoolType, SubjectResponse, TextbookSeriesResponse,
} from "../lib/types";

/** Die Übungstypen, die der KI-Creator selbst entwerfen kann (Manifest-Schlüssel des Servers). */
const TYPES = ["Vocabulary", "Cloze", "Translation", "Grammar"] as const;

/**
 * Die Schulart, wie das Pulldown sie tragen kann. Eine gespeicherte KOMBINATION ("Realschule, Gymnasium")
 * ist dort nicht darstellbar und wird zu „für alle" (`None`) – dieselbe Normalisierung braucht der
 * Ladezustand des Diffs, sonst sähe das erste Speichern einen Unterschied, den niemand gemacht hat.
 */
function formSchoolTypes(profile: CreatorProfileResponse): SchoolType {
  return (profile.schoolTypes && SCHOOL_TYPES.includes(profile.schoolTypes as SchoolType)
    ? profile.schoolTypes : "None") as SchoolType;
}

/**
 * Die Fachlehrer: <b>Creator-Profile</b>. Ein Profil ist keine Einstellung, sondern eine Rolle – „Englisch,
 * Klasse 7–8, Gymnasium, Lehrwerk Access". Der KI-Creator übernimmt sie, wenn er Übungen entwirft:
 *
 * * **Fach/Schulart/Klassenstufen** entscheiden, zu welchem Kind das Profil überhaupt passt.
 * * **Die Buchreihe** ist der stärkste Treffer: sie sagt, dass dieser Lehrer das konkrete Material kennt.
 * * **Rolle und Didaktik** gehen als Text in den Auftrag – sie prägen den Stil, dürfen aber die festen
 *   Inhalts-Regeln des Creators nicht aufweichen (der Server stellt sie deshalb *vor* den Regelblock).
 *
 * Nicht zu verwechseln mit „Mein Konto": das ist der eigene Zugang, hier stehen die Lehrer-Rollen, die
 * jedes Kind treffen können.
 */
export function VaterFachlehrer() {
  const [includeInactive, setIncludeInactive] = useState(false);
  const list = useAsync<CreatorProfileResponse[]>(
    () => api.creatorProfiles({ includeInactive }), [includeInactive]);
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const series = useAsync<TextbookSeriesResponse[]>(() => api.textbookSeries(), []);
  const [editing, setEditing] = useState<number | null>(null);

  return (
    <>
      <section>
        <h2 className="h-section">Fachlehrer</h2>
        <p className="sub">
          Ein Profil ist die Rolle, in der der KI-Creator Übungen entwirft. Zu einem Kind wird daraus das
          bestpassende gewählt – am schwersten wiegt die <strong>gemeinsame Buchreihe</strong>. Pflegen
          lässt sie sich unter <em>Lehrwerke</em>.
        </p>

        <label className="row" style={{ gap: 6, marginBottom: 10 }}>
          <input
            type="checkbox" checked={includeInactive} style={{ width: "auto" }}
            onChange={(e) => setIncludeInactive(e.target.checked)}
          />
          <span>Stillgelegte Profile mitzeigen</span>
        </label>

        {list.error && <div className="banner err">{list.error}</div>}
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
          <table className="table">
            <thead><tr><th>Profil</th><th>Fach</th><th>Klassen</th><th>Lehrwerk</th><th /></tr></thead>
            <tbody>
              {list.data?.map((p) => (
                <Fragment key={p.id}>
                  <tr>
                    <td>
                      {p.name}
                      {!p.active && <span className="pill mag" style={{ marginLeft: 6 }}>stillgelegt</span>}
                      {p.persona && <div className="muted" style={{ fontSize: 13 }}>{p.persona}</div>}
                    </td>
                    <td>
                      {p.subjectName ?? <span className="muted">fachneutral</span>}
                      {p.schoolTypes !== "None" && <div className="muted" style={{ fontSize: 13 }}>{p.schoolTypes}</div>}
                    </td>
                    <td>{gradeRange(p)}</td>
                    <td>{p.seriesName ?? <span className="muted">werkunabhängig</span>}</td>
                    <td style={{ textAlign: "right" }}>
                      {p.isOwn ? (
                        <button
                          type="button" className="btn ghost small" style={{ width: "auto" }}
                          aria-label={`${p.name} bearbeiten`}
                          onClick={() => setEditing(editing === p.id ? null : p.id)}
                        >{editing === p.id ? "Schließen" : "Bearbeiten"}</button>
                      ) : <span className="muted">fremd</span>}
                    </td>
                  </tr>
                  {editing === p.id && (
                    <tr>
                      <td colSpan={5}>
                        <ProfileForm
                          profile={p} subjects={subjects.data ?? []} series={series.data ?? []}
                          onDone={() => { setEditing(null); list.reload(); }}
                        />
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
              {list.data?.length === 0 && (
                <tr><td colSpan={5} className="muted">
                  Noch kein Fachlehrer. Ohne Profil arbeitet der KI-Creator als Generalist – er kennt
                  dann weder Schulzweig noch Lehrwerk.
                </td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      <section>
        <h3 className="h-section">Fachlehrer anlegen</h3>
        <ProfileForm subjects={subjects.data ?? []} series={series.data ?? []} onDone={list.reload} />
      </section>
    </>
  );
}

function gradeRange(p: CreatorProfileResponse): React.ReactNode {
  if (p.gradeMin == null && p.gradeMax == null) return <span className="muted">alle</span>;
  if (p.gradeMin != null && p.gradeMax != null) {
    return p.gradeMin === p.gradeMax ? `Klasse ${p.gradeMin}` : `Klasse ${p.gradeMin}–${p.gradeMax}`;
  }
  return p.gradeMin != null ? `ab Klasse ${p.gradeMin}` : `bis Klasse ${p.gradeMax}`;
}

// Die drei ableitbaren Felder und die Regel dahinter liegen in `seriesDerivation.ts` – dort sind sie
// mit Vitest abgedeckt (B-126), hier wären sie es nur über einen nachgebauten Bildschirm.

/**
 * Der Hinweis „aus dem Lehrwerk übernommen" unter einem abgeleiteten Feld.
 *
 * Die Region bleibt im DOM, auch wenn nichts zu melden ist – die Bedingung steht **in** ihr, nie um sie
 * herum. Viele Screenreader sagen nur an, was in eine *bereits vorhandene* Live-Region hineinwächst; eine
 * Region, die zusammen mit ihrem Text entsteht, bleibt stumm (WCAG 2.2 SC 4.1.3). Dieselbe Begründung wie
 * bei `StatusBanner` – und der Grund, warum das eine Komponente ist statt dreier Kopien: die Attribute
 * sind schnell getippt, die Leer-Regel ist schnell vergessen (B-132).
 */
function DerivedHint({ active }: { active: boolean }) {
  return (
    // `live-slot` nimmt den `gap` der `.field` zurück, solange nichts zu melden ist – die Begründung und
    // die Zahl stehen in index.css direkt unter dem `gap`, den sie ausgleichen (B-132).
    <span className={`muted live-slot${active ? " on" : ""}`} role="status" aria-live="polite"
      style={{ fontSize: 13 }}>
      {active ? "aus dem Lehrwerk übernommen" : ""}
    </span>
  );
}

/**
 * Ein Formular für Anlegen und Ändern. Die Klassenstufen sind bewusst optional: ein leeres Feld heißt
 * „unterrichtet jede Stufe" – und genau dann bekommt das Profil beim Matching *keine* Stufen-Punkte,
 * damit der Generalist den Fachlehrer nicht schlägt.
 */
export function ProfileForm({ profile, subjects, series, onDone }: {
  profile?: CreatorProfileResponse;
  subjects: SubjectResponse[];
  series: TextbookSeriesResponse[];
  onDone: () => void;
}) {
  const [form, setForm] = useState({
    name: profile?.name ?? "",
    subjectId: profile ? subjectFormValue(profile) : "",
    schoolTypes: profile ? formSchoolTypes(profile) : ("None" as SchoolType),
    gradeMin: profile?.gradeMin?.toString() ?? "",
    gradeMax: profile?.gradeMax?.toString() ?? "",
    seriesId: profile?.seriesId?.toString() ?? "",
    sourceLang: profile?.sourceLang ?? FIELD_FALLBACKS.sourceLang,
    targetLang: profile?.targetLang ?? FIELD_FALLBACKS.targetLang,
    persona: profile?.persona ?? "",
    didactics: profile?.didactics ?? "",
    active: profile?.active ?? true,
  });
  const [types, setTypes] = useState<string[]>(profile?.defaultTypes ?? []);
  // "berührt" heißt „vom Nutzer selbst geändert" – erst das unterscheidet ein leeres Feld von einem Feld,
  // das nur die Vorgabe `en`/`de` trägt (B-67, Entscheidung 1).
  const [touched, setTouched] = useState<Set<DerivableField>>(new Set());
  // Die Werte, mit denen ein BESTEHENDES Profil geöffnet wurde. Sie sind so wenig zu überschreiben wie
  // ein berührtes Feld – der Creator hat sie in einer früheren Sitzung gesetzt, nur weiß `touched` davon
  // nichts. Bei einem neuen Profil bewusst `undefined`: dort soll die Vorgabe `en`/`de` gerade weichen.
  const [loaded] = useState<DerivableValues | undefined>(() => profile && {
    // Dieselbe Darstellung wie im Formular, seit ein Freitext-Fach dort den Sentinel trägt (B-148).
    // Liefen die beiden auseinander, verlöre Fall 3 von `applySeriesChange` („der geladene Wert bleibt")
    // genau für dieses Fach seine Wirkung – ein Reihenwechsel überschriebe es dann still.
    subjectId: subjectFormValue(profile),
    sourceLang: profile.sourceLang ?? FIELD_FALLBACKS.sourceLang,
    targetLang: profile.targetLang ?? FIELD_FALLBACKS.targetLang,
  });
  // Der Bezugspunkt des PATCH-Diffs – bewusst NICHT `loaded`: das trägt die drei ableitbaren Felder für
  // B-126, und zwei Regeln an einem Wert heißt, dass eine Änderung für die eine die andere verstellt.
  //
  // Nicht nachgezogen, aus demselben Grund wie im Lehrbuch-Formular: `onDone` schließt das Formular beim
  // Speichern, es kann also nicht veralten. Bleibt es je offen, muss die Antwort hier einfließen.
  const geladen = useRef(profile
    ? profileFormValues(profile, formSchoolTypes(profile), FIELD_FALLBACKS.sourceLang, FIELD_FALLBACKS.targetLang)
    : null);
  const action = useAction();
  const id = profile ? `p${profile.id}` : "new";

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  function touch(field: DerivableField) {
    setTouched((t) => new Set(t).add(field));
  }

  // Die gewählte Reihe speist die Ableitung. Trägt SIE ein Freitext-Fach, steuert sie zum Fach-Feld
  // nichts bei (`derivableValues` liefert dafür ""), und das Pulldown bleibt in Ruhe (B-67,
  // Entscheidung 2). Nicht zu verwechseln mit dem Freitext-Fach des Profils selbst: das trägt seit
  // B-148 den Sentinel und ist als gesperrte Option sichtbar.
  const chosenSeries = series.find((s) => String(s.id) === form.seriesId);
  // Computed statt in eigenem State gehalten: ein zweites, von Hand gepflegtes Set könnte von der
  // Formular-/Reihenwahl abdriften. Die Regel selbst steht in `seriesDerivation.ts`.
  const isDerived = (field: DerivableField) => derived(field, form, chosenSeries, touched);

  /** Beim Wählen einer Reihe: abgeleitete Felder folgen ihr, selbst gesetzte nie. */
  function deriveFromSeries(seriesId: string) {
    const next = series.find((s) => String(s.id) === seriesId);
    setForm((f) => {
      // Vorige Reihe aus DEMSELBEN Stand lesen wie die Formularwerte, nicht aus dem Render-Closure.
      const previous = series.find((s) => String(s.id) === f.seriesId);
      // `seriesId` hinter den Spread: dann ist es gleichgültig, was die Regel je zurückgibt.
      return { ...f, ...applySeriesChange(f, touched, previous, next, loaded), seriesId };
    });
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { action.fail("Der Name fehlt."); return; }
    const min = form.gradeMin.trim() === "" ? null : Number(form.gradeMin);
    const max = form.gradeMax.trim() === "" ? null : Number(form.gradeMax);
    if (min != null && max != null && min > max) {
      action.fail("Die untere Klassenstufe darf nicht über der oberen liegen.");
      return;
    }

    if (profile && geladen.current) {
      // Beim Ändern nur das Geänderte, entschieden per Vergleich gegen den Ladezustand statt aus dem
      // Momentanwert – sonst kostete jedes Speichern den Fachnamen eines gelöschten Fachs (B-148).
      const patch = profilePatch(geladen.current, { ...form, defaultTypes: types });
      if (patch === null) { action.succeed("Nichts geändert."); return; }

      if (!await action.run(() => api.updateCreatorProfile(profile.id, patch), "Gespeichert.")) return;
      onDone();
      return;
    }

    const subject = subjects.find((s) => String(s.id) === form.subjectId);
    const dto: CreateCreatorProfileDto = {
      name: form.name.trim(),
      subjectId: form.subjectId ? Number(form.subjectId) : null,
      subjectName: subject?.name ?? null,
      // „für alle" ist der Enum-Wert `None`, nicht `null`: ein `null` gilt im PATCH als „nicht angegeben"
      // und hätte die bisherige Einschränkung stehen gelassen.
      schoolTypes: form.schoolTypes,
      gradeMin: min,
      gradeMax: max,
      seriesId: form.seriesId ? Number(form.seriesId) : null,
      sourceLang: form.sourceLang.trim() || null,
      targetLang: form.targetLang.trim() || null,
      persona: form.persona.trim() || null,
      didactics: form.didactics.trim() || null,
      defaultTypes: types,
      active: form.active,
    };

    // Anlegen: kein Bezugspunkt, also auch kein Schalter – ein neues Profil hat nichts zu leeren.
    if (!await action.run(() => api.createCreatorProfile(dto), "Fachlehrer angelegt.")) return;
    setForm({ ...form, name: "", persona: "", didactics: "" });
    setTypes([]);
    // Gehört zum Formularzustand, den das Anlegen verwirft: bliebe es stehen, leitete der nächste
    // Eintrag genau die Felder nicht mehr ab, die beim vorigen von Hand geändert wurden (B-126).
    setTouched(new Set());
    onDone();
  }

  async function remove() {
    if (!profile) return;
    if (!confirmAction(
      `Fachlehrer „${profile.name}" löschen? Bereits erzeugte Übungen bleiben erhalten – `
      + "das Profil ist die Werkbank, nicht der Besitzer.")) return;
    if (await action.run(() => api.deleteCreatorProfile(profile.id))) onDone();
  }

  return (
    <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
      <div className="form-grid" style={{ alignItems: "end" }}>
        <div className="field">
          <label htmlFor={`fl-name-${id}`}>Name des Profils</label>
          <input
            id={`fl-name-${id}`} value={form.name} onChange={(e) => up("name", e.target.value)}
            placeholder="Englisch 8 Gymnasium – Access"
          />
        </div>
        <div className="field">
          <FieldLabel htmlFor={`fl-subject-${id}`} topic="profileSubject">Fach</FieldLabel>
          <select
            id={`fl-subject-${id}`} value={form.subjectId}
            onChange={(e) => { up("subjectId", e.target.value); touch("subjectId"); }}
          >
            <option value="">– fachneutral –</option>
            {/* Der Freitext-Zustand als eigene, nicht wählbare Option (B-148, Muster aus B-143).
                Bedingung und Beschriftung kommen BEIDE aus `profile`, dem geladenen Stand – aus einer
                Quelle können sie nicht auseinanderlaufen, und die Option bleibt stehen, während der
                Nutzer „– fachneutral –" probiert. */}
            {profile?.subjectId == null && profile?.subjectName && (
              <option value={FREETEXT_SUBJECT} disabled>{profile.subjectName} (Freitext)</option>
            )}
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <DerivedHint active={isDerived("subjectId")} />
        </div>
        <div className="field">
          <label htmlFor={`fl-school-${id}`}>Schulart</label>
          <select id={`fl-school-${id}`} value={form.schoolTypes} onChange={(e) => up("schoolTypes", e.target.value as SchoolType)}>
            <option value="None">– für alle –</option>
            {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`fl-min-${id}`}>Klasse von</label>
          <input
            id={`fl-min-${id}`} type="number" min={1} max={13} value={form.gradeMin}
            onChange={(e) => up("gradeMin", e.target.value)} placeholder="7"
          />
        </div>
        <div className="field">
          <label htmlFor={`fl-max-${id}`}>Klasse bis</label>
          <input
            id={`fl-max-${id}`} type="number" min={1} max={13} value={form.gradeMax}
            onChange={(e) => up("gradeMax", e.target.value)} placeholder="8"
          />
        </div>
        <div className="field">
          <label htmlFor={`fl-series-${id}`}>Lehrwerk</label>
          <select id={`fl-series-${id}`} value={form.seriesId} onChange={(e) => deriveFromSeries(e.target.value)}>
            <option value="">– werkunabhängig –</option>
            {series.map((s) => (
              <option key={s.id} value={s.id}>{s.name}{s.publisherName ? ` (${s.publisherName})` : ""}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`fl-src-${id}`}>Lernsprache</label>
          <input
            id={`fl-src-${id}`} value={form.sourceLang} placeholder="en"
            onChange={(e) => { up("sourceLang", e.target.value); touch("sourceLang"); }}
          />
          <DerivedHint active={isDerived("sourceLang")} />
        </div>
        <div className="field">
          <label htmlFor={`fl-tgt-${id}`}>Muttersprache</label>
          <input
            id={`fl-tgt-${id}`} value={form.targetLang} placeholder="de"
            onChange={(e) => { up("targetLang", e.target.value); touch("targetLang"); }}
          />
          <DerivedHint active={isDerived("targetLang")} />
        </div>
      </div>

      <div className="field">
        <label htmlFor={`fl-persona-${id}`}>Rolle <span className="muted">(wie soll sich der Creator verstehen?)</span></label>
        <textarea
          id={`fl-persona-${id}`} rows={2} value={form.persona} onChange={(e) => up("persona", e.target.value)}
          placeholder="Du bist Englischlehrer an einem bayerischen Gymnasium."
        />
      </div>
      <div className="field">
        <label htmlFor={`fl-didactics-${id}`}>Didaktische Vorgaben</label>
        <textarea
          id={`fl-didactics-${id}`} rows={2} value={form.didactics} onChange={(e) => up("didactics", e.target.value)}
          placeholder="Kurze Sätze, maximal zwölf Wörter. Progression wie im Lehrwerk."
        />
        <span className="sub">
          Gilt über einen einzelnen Auftrag hinaus. Die Inhalts-Regeln des Creators (Stoff ist gesetzt,
          Interessen kleiden nur ein) bleiben davon unberührt.
        </span>
      </div>

      <fieldset className="field" style={{ border: 0, padding: 0, margin: 0 }}>
        <legend className="muted" style={{ fontSize: 14 }}>Bevorzugte Übungstypen</legend>
        <div className="row" style={{ gap: 8, flexWrap: "wrap" }}>
          {TYPES.map((t) => (
            <button
              key={t} type="button" className={`pill toggle-pill ${types.includes(t) ? "lime" : ""}`}
              aria-pressed={types.includes(t)}
              onClick={() => setTypes(types.includes(t) ? types.filter((x) => x !== t) : [...types, t])}
            >{t}</button>
          ))}
        </div>
      </fieldset>

      <label className="row" style={{ gap: 6 }}>
        <input
          type="checkbox" checked={form.active} style={{ width: "auto" }}
          onChange={(e) => up("active", e.target.checked)}
        />
        <span>Aktiv <span className="muted">(stillgelegte Profile werden keinem Kind vorgeschlagen)</span></span>
      </label>

      <div className="row" style={{ gap: 8 }}>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Speichere…" : profile ? "Speichern" : "Fachlehrer anlegen"}
        </button>
        {profile && (
          <button
            type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
            disabled={action.busy} onClick={remove}
          >Löschen</button>
        )}
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </form>
  );
}
