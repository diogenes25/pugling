import { Fragment, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { SCHOOL_TYPES } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  CreateCreatorProfileDto, CreatorProfileResponse, SchoolType, SubjectResponse, TextbookSeriesResponse,
} from "../lib/types";

/** Die Übungstypen, die der KI-Creator selbst entwerfen kann (Manifest-Schlüssel des Servers). */
const TYPES = ["Vocabulary", "Cloze", "Translation", "Grammar"] as const;

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
        {list.loading ? <div className="loading">Lade…</div> : (
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

/**
 * Ein Formular für Anlegen und Ändern. Die Klassenstufen sind bewusst optional: ein leeres Feld heißt
 * „unterrichtet jede Stufe" – und genau dann bekommt das Profil beim Matching *keine* Stufen-Punkte,
 * damit der Generalist den Fachlehrer nicht schlägt.
 */
function ProfileForm({ profile, subjects, series, onDone }: {
  profile?: CreatorProfileResponse;
  subjects: SubjectResponse[];
  series: TextbookSeriesResponse[];
  onDone: () => void;
}) {
  const [form, setForm] = useState({
    name: profile?.name ?? "",
    subjectId: profile?.subjectId?.toString() ?? "",
    schoolTypes: (profile?.schoolTypes && SCHOOL_TYPES.includes(profile.schoolTypes as SchoolType)
      ? profile.schoolTypes : "None") as SchoolType,
    gradeMin: profile?.gradeMin?.toString() ?? "",
    gradeMax: profile?.gradeMax?.toString() ?? "",
    seriesId: profile?.seriesId?.toString() ?? "",
    sourceLang: profile?.sourceLang ?? "en",
    targetLang: profile?.targetLang ?? "de",
    persona: profile?.persona ?? "",
    didactics: profile?.didactics ?? "",
    active: profile?.active ?? true,
  });
  const [types, setTypes] = useState<string[]>(profile?.defaultTypes ?? []);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const id = profile ? `p${profile.id}` : "new";

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { setMsg({ ok: false, text: "Der Name fehlt." }); return; }
    const min = form.gradeMin.trim() === "" ? null : Number(form.gradeMin);
    const max = form.gradeMax.trim() === "" ? null : Number(form.gradeMax);
    if (min != null && max != null && min > max) {
      setMsg({ ok: false, text: "Die untere Klassenstufe darf nicht über der oberen liegen." });
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

    setBusy(true); setMsg(null);
    try {
      if (profile) {
        // Beim Ändern muss „leer" ausdrücklich gesagt werden: der Server überliest `null` (= nicht
        // angegeben), sonst blieben Fach, Reihe und Klassenstufen für immer gesetzt.
        await api.updateCreatorProfile(profile.id, {
          ...dto,
          clearSubject: dto.subjectId == null,
          clearSeries: dto.seriesId == null,
          clearGradeMin: min == null,
          clearGradeMax: max == null,
        });
      } else {
        await api.createCreatorProfile(dto);
        setForm({ ...form, name: "", persona: "", didactics: "" });
        setTypes([]);
      }
      setMsg({ ok: true, text: profile ? "Gespeichert." : "Fachlehrer angelegt." });
      onDone();
    } catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
    finally { setBusy(false); }
  }

  async function remove() {
    if (!profile) return;
    if (!confirmAction(
      `Fachlehrer „${profile.name}" löschen? Bereits erzeugte Übungen bleiben erhalten – `
      + "das Profil ist die Werkbank, nicht der Besitzer.")) return;
    setBusy(true);
    try { await api.deleteCreatorProfile(profile.id); onDone(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); setBusy(false); }
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
          <label htmlFor={`fl-subject-${id}`}>Fach</label>
          <select id={`fl-subject-${id}`} value={form.subjectId} onChange={(e) => up("subjectId", e.target.value)}>
            <option value="">– fachneutral –</option>
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
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
          <select id={`fl-series-${id}`} value={form.seriesId} onChange={(e) => up("seriesId", e.target.value)}>
            <option value="">– werkunabhängig –</option>
            {series.map((s) => (
              <option key={s.id} value={s.id}>{s.name}{s.publisher ? ` (${s.publisher})` : ""}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor={`fl-src-${id}`}>Lernsprache</label>
          <input id={`fl-src-${id}`} value={form.sourceLang} onChange={(e) => up("sourceLang", e.target.value)} placeholder="en" />
        </div>
        <div className="field">
          <label htmlFor={`fl-tgt-${id}`}>Muttersprache</label>
          <input id={`fl-tgt-${id}`} value={form.targetLang} onChange={(e) => up("targetLang", e.target.value)} placeholder="de" />
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
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>
          {busy ? "…" : profile ? "Speichern" : "Fachlehrer anlegen"}
        </button>
        {profile && (
          <button
            type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
            disabled={busy} onClick={remove}
          >Löschen</button>
        )}
      </div>
      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} role="status" aria-live="polite">{msg.text}</div>}
    </form>
  );
}
