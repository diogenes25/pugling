import { useEffect, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { SUPERVISOR_RELATIONS, WEEKDAYS, supervisorRelationLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type {
  ChildPause, ChildResponse, SubjectResponse, SupervisorLink, SupervisorRelation, TimetableEntry, UpdateChildDto, Weekday,
} from "../lib/types";
import { AddPanel } from "../components/AddPanel";
import { ListenAusfall } from "../components/ListenAusfall";
import { formatDate, formatDateRange, heuteImLerntag } from "../lib/datum";
import { InfoHint } from "../components/InfoHint";

/*
 * Zwei Seiten des Kind-Profils, die bisher nur über die API erreichbar waren.
 *
 * **Betreuer** ist die Multi-Supervisor-Idee des Produkts: ein Kind hat Vater, Mutter, Oma – alle mit
 * denselben Steuerrechten und **einem gemeinsamen Wallet**. Ohne diese Oberfläche war das Konzept im
 * Produkt unsichtbar, obwohl das Datenmodell darauf gebaut ist.
 *
 * **Stundenplan** ist übungsunabhängiges Profilwissen: an welchem Tag welches Fach ansteht. Er beantwortet
 * die Frage „worauf sollte heute der Schwerpunkt liegen?", ohne dass ein Lehrplan sie stellen muss.
 */

/**
 * Die Betreuer dieses Kindes. Hinzufügen geht über die **Id** des anderen Betreuers: er muss schon
 * ein eigenes Konto haben (Registrierung unter `/vater`), denn er soll sich anmelden können – ein bloßer
 * Name wäre kein Zugang.
 */
export function SupervisorsSection({ childId, childName }: { childId: number; childName: string }) {
  const links = useAsync<SupervisorLink[]>(() => api.childSupervisors(childId), [childId]);
  const [supervisorId, setSupervisorId] = useState("");
  const [relation, setRelation] = useState<SupervisorRelation>("Mother");
  const action = useAction();

  async function add(e: React.FormEvent) {
    e.preventDefault();
    const id = Number(supervisorId);
    if (!id) { action.fail("Bitte die Id des Betreuers angeben."); return; }
    if (!await action.run(() => api.addChildSupervisor(childId, id, relation), "Betreuer hinzugefügt.")) return;
    // Erst nach dem Erfolg räumen: bei einer abgelehnten Id soll sie zum Korrigieren stehen bleiben.
    setSupervisorId("");
    links.reload();
  }

  async function remove(link: SupervisorLink) {
    if (!confirmAction(
      `${link.supervisorName} die Betreuung von ${childName} entziehen? Der Zugang zu Plänen, Konto und `
      + "Shop endet damit. Käufe und Freigaben, die diese Person ausgestellt hat, bleiben bestehen.")) return;
    if (await action.run(() => api.removeChildSupervisor(childId, link.supervisorId))) links.reload();
  }

  return (
    <section>
      <h3 className="h-section">Betreuer {links.data ? `(${links.data.length})` : ""}</h3>
      <p className="sub">
        Mehrere Erwachsene können {childName} betreuen – mit denselben Rechten und <strong>einem gemeinsamen
        Punktekonto</strong>. Nur das <em>Einlösen</em> bleibt ausstellergebunden: freigeben darf, wer die
        Belohnung in seinen Shop gestellt hat.
      </p>

      <ListenAusfall was="Die Betreuer" fehler={links.error} laedt={links.loading} onRetry={links.reload} schluessel={childId} />
      {links.data && (
        <table className="table">
          <thead><tr><th>Name</th><th>Rolle</th><th>Seit</th><th /></tr></thead>
          <tbody>
            {links.data.map((l) => (
              <tr key={l.supervisorId}>
                <td>{l.supervisorName} <span className="muted">(#{l.supervisorId})</span></td>
                <td>{supervisorRelationLabel(l.relation)}</td>
                <td className="muted">{formatDate(l.createdAt)}</td>
                <td style={{ textAlign: "right" }}>
                  {/* Der letzte Betreuer bleibt: ohne ihn wäre das Kind für niemanden erreichbar (Server: 400). */}
                  {links.data!.length > 1 && (
                    <button type="button" className="btn ghost small" style={{ width: "auto" }}
                      aria-label={`${l.supervisorName} entfernen`} disabled={action.busy} onClick={() => remove(l)}>
                      Entfernen
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <AddPanel label="Weiteren Betreuer">
        <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap", }} onSubmit={add}>
          <div className="field" style={{ maxWidth: 160 }}>
            <label htmlFor="sup-id">Id des Betreuers</label>
            <input id="sup-id" inputMode="numeric" autoComplete="off" value={supervisorId}
              onChange={(e) => setSupervisorId(e.target.value.replace(/\D/g, ""))} placeholder="z. B. 4" />
          </div>
          <div className="field" style={{ maxWidth: 160 }}>
            <label htmlFor="sup-rel">Rolle</label>
            <select id="sup-rel" value={relation} onChange={(e) => setRelation(e.target.value as SupervisorRelation)}>
              {SUPERVISOR_RELATIONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </div>
          {/* Sprechendes Label, nicht bloß „Hinzufügen": auf dieser Seite gibt es mehrere Hinzufügen-Knöpfe
              (Interessen, Stundenplan), und ein Screenreader liest nur die Beschriftung vor. */}
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
            Betreuer hinzufügen
          </button>
        </form>
        <p className="sub" style={{ marginTop: 6 }}>
          Der andere Betreuer braucht ein <strong>eigenes Konto</strong>: er registriert sich unter
          <code> /vater</code> und nennt dir seine Id.
        </p>
      </AddPanel>
      <StatusBanner message={action.message} />
    </section>
  );
}

/** Der Stundenplan: je Wochentag die Fächer. Ein Fach kann pro Tag nur einmal stehen (Server: 409). */
export function TimetableSection({ childId, subjects }: { childId: number; subjects: SubjectResponse[] }) {
  const entries = useAsync<TimetableEntry[]>(() => api.childTimetable(childId), [childId]);
  const [subjectId, setSubjectId] = useState<number | "">("");
  const [dayOfWeek, setDayOfWeek] = useState<Weekday>("Monday");
  const [timeOfDay, setTimeOfDay] = useState("");
  const action = useAction();

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (subjectId === "") { action.fail("Bitte ein Fach wählen."); return; }
    const ok = await action.run(() => api.addTimetableEntry(
      childId, { subjectId: Number(subjectId), dayOfWeek, timeOfDay: timeOfDay || null }));
    if (!ok) return;
    setTimeOfDay("");
    entries.reload();
  }

  async function remove(entry: TimetableEntry) {
    if (await action.run(() => api.removeTimetableEntry(childId, entry.id))) entries.reload();
  }

  // Nach Wochentag gruppieren: der Plan wird als Woche gelesen, nicht als flache Liste.
  const byDay = WEEKDAYS
    .map((d) => ({ ...d, items: (entries.data ?? []).filter((e) => e.dayOfWeek === d.value) }))
    .filter((d) => d.items.length > 0);

  return (
    <section>
      <h3 className="h-section">Stundenplan</h3>
      <p className="sub">
        Welche Fächer an welchem Tag anstehen. Das ist Profilwissen, kein Lehrplan – es hilft dir (und dem
        KI-Creator) zu entscheiden, worauf heute der Schwerpunkt liegt.
      </p>

      <ListenAusfall was="Die Stundenplan-Einträge" fehler={entries.error} laedt={entries.loading} onRetry={entries.reload} schluessel={childId} />
      {entries.data && (byDay.length === 0
        ? <p className="muted">Noch nichts eingetragen.</p>
        : (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {byDay.map((d) => (
              <div key={d.value} className="row" style={{ gap: 8, alignItems: "baseline", flexWrap: "wrap" }}>
                <span style={{ minWidth: 110, fontWeight: 600 }}>{d.label}</span>
                {d.items.map((e) => (
                  <span key={e.id} className="token">
                    {e.subjectName}{e.timeOfDay ? ` · ${e.timeOfDay}` : ""}
                    <button type="button" aria-label={`${e.subjectName} am ${d.label} entfernen`}
                      disabled={action.busy} onClick={() => remove(e)}>×</button>
                  </span>
                ))}
              </div>
            ))}
          </div>
        ))}

      {/* Erst mit geladenem Bestand: der Leerfall öffnet das Formular gleich (AddPanel liest `defaultOpen` nur einmal). */}
      {/* Bewusst kein Panel nach gescheitertem Laden: der ListenAusfall oben bietet „Erneut laden", danach erscheint es. */}
      {entries.data && (
        <AddPanel label="Neuer Stundenplan-Eintrag" defaultOpen={entries.data.length === 0}>
          <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap", }} onSubmit={add}>
            <div className="field" style={{ maxWidth: 200 }}>
              <label htmlFor="tt-subject">Fach</label>
              <select id="tt-subject" value={subjectId}
                onChange={(e) => setSubjectId(e.target.value === "" ? "" : Number(e.target.value))}>
                <option value="">– wählen –</option>
                {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 160 }}>
              <label htmlFor="tt-day">Wochentag</label>
              <select id="tt-day" value={dayOfWeek} onChange={(e) => setDayOfWeek(e.target.value as Weekday)}>
                {WEEKDAYS.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
              </select>
            </div>
            <div className="field" style={{ maxWidth: 140 }}>
              <label htmlFor="tt-time">Uhrzeit <span className="muted">(optional)</span></label>
              <input id="tt-time" autoComplete="off" value={timeOfDay}
                onChange={(e) => setTimeOfDay(e.target.value)} placeholder="z. B. 1./2. Stunde" />
            </div>
            <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
              Eintragen
            </button>
          </form>
        </AddPanel>
      )}
      <StatusBanner message={action.message} />
    </section>
  );
}

/**
 * Pausen des Kindes (B-731): von Datum bis Datum, auch rückwirkend für heute. In einer Pause kostet keine Pflicht
 * Münzen und die Serie steht – sie zählt weder mit, noch reißt sie. Der Sonntag ist ohnehin frei. Ein schon gebuchter
 * Malus wird nicht erstattet (Rückgabe von Hand über „Münzen“).
 */
export function PauseSection({ childId, childName }: { childId: number; childName: string }) {
  const pauses = useAsync<ChildPause[]>(() => api.childPauses(childId), [childId]);
  const today = heuteImLerntag();
  const [from, setFrom] = useState(today);
  const [until, setUntil] = useState(today);
  const [note, setNote] = useState("");
  const action = useAction();
  const valid = from !== "" && until !== "" && from <= until;

  async function add(e: React.FormEvent) {
    e.preventDefault();
    if (!valid) { action.fail("„Bis“ darf nicht vor „Von“ liegen."); return; }
    if (await action.run(() => api.addChildPause(childId, { from, until, note: note.trim() || null }), "Pause eingetragen.")) {
      setNote("");
      pauses.reload();
    }
  }

  async function remove(pause: ChildPause) {
    if (!confirmAction(`Pause ${formatDateRange(pause.from, pause.until)} löschen? Ab dann zählen diese Tage wieder.`)) return;
    if (await action.run(() => api.removeChildPause(childId, pause.id), "Pause gelöscht.")) pauses.reload();
  }

  return (
    <section>
      <h3 className="h-section">Pausen</h3>
      <p className="sub">
        Krank, Ferien, Klassenfahrt: In einer Pause kostet {childName} keine Pflicht Münzen, und die Serie steht still.
        Sonntags ist ohnehin frei. Ein schon abgebuchter Malus kommt nicht von selbst zurück – den gibst du unter „Münzen“
        zurück.
      </p>
      <ListenAusfall was="Die Pausen" fehler={pauses.error} laedt={pauses.loading} onRetry={pauses.reload} schluessel={childId} />
      {pauses.data && (pauses.data.length === 0
        ? <p className="muted">Keine Pause eingetragen.</p>
        : (
          <ul aria-label="Eingetragene Pausen" style={{ listStyle: "none", padding: 0, margin: 0 }}>
            {pauses.data.map((p) => (
              <li key={p.id} className="row" style={{ gap: 8, alignItems: "baseline", flexWrap: "wrap" }}>
                <span style={{ fontWeight: 600 }}>{formatDateRange(p.from, p.until)}</span>
                {p.note && <span className="muted">{p.note}</span>}
                <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy}
                  aria-label={`Pause ${formatDateRange(p.from, p.until)} löschen`} onClick={() => remove(p)}>Löschen</button>
              </li>
            ))}
          </ul>
        ))}
      {pauses.data && (
        <AddPanel label="Neue Pause" defaultOpen={pauses.data.length === 0}>
          <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }} onSubmit={add}>
            <div className="field" style={{ maxWidth: 170 }}>
              <label htmlFor="pause-from">Von</label>
              <input id="pause-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
            </div>
            <div className="field" style={{ maxWidth: 170 }}>
              <label htmlFor="pause-until">Bis (einschließlich)</label>
              <input id="pause-until" type="date" value={until} onChange={(e) => setUntil(e.target.value)}
                aria-invalid={!valid || undefined} />
            </div>
            <div className="field" style={{ maxWidth: 240 }}>
              <label htmlFor="pause-note">Notiz <span className="muted">(optional)</span></label>
              <input id="pause-note" autoComplete="off" maxLength={200} value={note} onChange={(e) => setNote(e.target.value)}
                placeholder="z. B. krank" />
            </div>
            <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy || !valid}>
              Pause eintragen
            </button>
          </form>
          {!valid && <p className="sub">„Bis“ darf nicht vor „Von“ liegen.</p>}
        </AddPanel>
      )}
      <StatusBanner message={action.message} />
    </section>
  );
}

/** Die fünf Leitplanken in der Reihenfolge der Maske, mit den Grenzen, die auch der Server prüft. */
const LIMIT_FIELDS = [
  { key: "dailyTasksMin", id: "limit-tasks-min", label: "Pflichtaufgaben am Tag – mindestens", min: 1, max: 50 },
  { key: "dailyTasksMax", id: "limit-tasks-max", label: "Pflichtaufgaben am Tag – höchstens", min: 1, max: 50 },
  { key: "dailyNewWordsMin", id: "limit-words-min", label: "Neue Wörter am Tag – mindestens", min: 1, max: 200 },
  { key: "dailyNewWordsMax", id: "limit-words-max", label: "Neue Wörter am Tag – höchstens", min: 1, max: 200 },
  { key: "penaltyCoinsPerMissedTask", id: "limit-penalty", label: "Malus je verpasster Pflichtaufgabe (🪙)", min: 0, max: 1000 },
] as const;

type LimitKey = (typeof LIMIT_FIELDS)[number]["key"];

/** Was die Maske prüft, bevor sie schickt – dieselben Regeln, mit denen der Server ablehnt (B-731 AK 6). */
export function leitplankenFehler(values: Record<LimitKey, number>): string | null {
  for (const f of LIMIT_FIELDS) {
    const v = values[f.key];
    if (!Number.isInteger(v) || v < f.min || v > f.max) return `${f.label}: erlaubt sind ganze Zahlen von ${f.min} bis ${f.max}.`;
  }
  if (values.dailyTasksMin > values.dailyTasksMax) return "Bei den Pflichtaufgaben liegt „mindestens“ über „höchstens“.";
  if (values.dailyNewWordsMin > values.dailyNewWordsMax) return "Bei den neuen Wörtern liegt „mindestens“ über „höchstens“.";
  return null;
}

const limitValues = (child: ChildResponse) => Object.fromEntries(
  LIMIT_FIELDS.map((f) => [f.key, child[f.key]?.toString() ?? ""])) as Record<LimitKey, string>;

/**
 * Die Leitplanken des Tagesplaners (B-731): je Kind Boden und Deckel für Pflichtaufgaben und neue Wörter am Tag und der
 * Malus je verpasster Pflichtaufgabe. Der Vater setzt den Rahmen, keine Tagesmengen. Die Zahlen wirken erst mit dem
 * Planer – die Maske sagt das, damit niemand nach dem Speichern eine Wirkung sucht. Angezeigt wird immer der Stand des
 * Servers: nach dem Speichern lädt die Seite das Kind neu, und die Felder ziehen den gelesenen Stand nach. Ohne
 * Neu-Einhängen (B-1020): ein `key` aus den Werten nahm mit dem Formular auch die Bestätigung aus dem DOM.
 */
export function LeitplankenSection({ child, onSaved }: { child: ChildResponse; onSaved: () => void }) {
  const [values, setValues] = useState<Record<LimitKey, string>>(() => limitValues(child));
  // Nur wenn sich der Serverstand der Leitplanken ändert – ein Neuladen des Kindes aus anderem Grund (Bonus, Turm)
  // darf eine ungespeicherte Eingabe nicht überschreiben.
  const serverStand = LIMIT_FIELDS.map((f) => child[f.key] ?? "").join("|");
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { setValues(limitValues(child)); }, [serverStand]);
  const action = useAction();
  const stored = LIMIT_FIELDS.some((f) => child[f.key] != null);
  const allFilled = LIMIT_FIELDS.every((f) => values[f.key].trim() !== "");
  const numbers = Object.fromEntries(LIMIT_FIELDS.map((f) => [f.key, Number(values[f.key])])) as Record<LimitKey, number>;
  const fehler = allFilled ? leitplankenFehler(numbers) : "Bitte alle fünf Zahlen eintragen.";

  async function save(e: React.FormEvent) {
    e.preventDefault();
    if (fehler) { action.fail(fehler); return; }
    const dto: UpdateChildDto = { ...numbers };
    if (await action.run(() => api.updateChild(child.id, dto), "Leitplanken gespeichert.")) onSaved();
  }

  async function clear() {
    if (!confirmAction(`Alle fünf Leitplanken von ${child.name} entfernen? Der Tagesplaner hat dann keinen Rahmen mehr.`)) return;
    if (await action.run(() => api.updateChild(child.id, { clearPlannerLimits: true }), "Leitplanken entfernt.")) onSaved();
  }

  return (
    <section>
      <h3 className="h-section">Leitplanken des Tagesplaners</h3>
      <p className="sub">
        Der Rahmen, in dem der Tagesplaner den Tag von {child.name} zusammenstellt: wie viele Pflichtaufgaben und neue
        Wörter am Tag mindestens und höchstens anstehen, und was eine verpasste Pflichtaufgabe kostet. Die Zahlen wirken
        erst, wenn der Tagesplaner kommt; heute ändern sie noch nichts an Pflicht und Malus.
      </p>
      <form onSubmit={save}>
        {/* Eine Erklärung für die Gruppe statt fünf gleicher ⓘ an jedem Feld: der Begriff ist derselbe. */}
        <div role="group" aria-labelledby="limits-group">
          {/* Die id sitzt am Wort, nicht an der Zeile: sonst hieße die Gruppe auch „Erklärung zu …“ (B-236). */}
          <span className="label-row group-label">
            <span id="limits-group">Leitplanken</span> <InfoHint topic="plannerLimits" />
          </span>
          <div className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap" }}>
            {LIMIT_FIELDS.map((f) => (
              <div key={f.key} className="field" style={{ maxWidth: 230, marginBottom: 0 }}>
                <label htmlFor={f.id}>{f.label}</label>
                <input id={f.id} type="number" min={f.min} max={f.max} step={1} inputMode="numeric"
                  value={values[f.key]} onChange={(e) => setValues((v) => ({ ...v, [f.key]: e.target.value }))} />
              </div>
            ))}
          </div>
        </div>
        <div className="row" style={{ gap: 8, marginTop: 8 }}>
          <button type="submit" className="btn small" style={{ width: "auto" }} disabled={action.busy || fehler !== null}>Speichern</button>
          {stored && (
            <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy} onClick={clear}>
              Leitplanken entfernen
            </button>
          )}
        </div>
      </form>
      {fehler && allFilled && <p className="sub">{fehler}</p>}
      {!stored && <p className="muted">Noch keine Leitplanken gesetzt.</p>}
      <StatusBanner message={action.message} />
    </section>
  );
}
