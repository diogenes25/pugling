import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { SUPERVISOR_RELATIONS, WEEKDAYS, supervisorRelationLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { SubjectResponse, SupervisorLink, SupervisorRelation, TimetableEntry, Weekday } from "../lib/types";

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

      {links.error && <div className="banner err">{links.error}</div>}
      {links.data && (
        <table className="table">
          <thead><tr><th>Name</th><th>Rolle</th><th>Seit</th><th /></tr></thead>
          <tbody>
            {links.data.map((l) => (
              <tr key={l.supervisorId}>
                <td>{l.supervisorName} <span className="muted">(#{l.supervisorId})</span></td>
                <td>{supervisorRelationLabel(l.relation)}</td>
                <td className="muted">{new Date(l.createdAt).toLocaleDateString()}</td>
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

      <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap", marginTop: 10 }} onSubmit={add}>
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

      {entries.error && <div className="banner err">{entries.error}</div>}
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

      <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap", marginTop: 10 }} onSubmit={add}>
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
      <StatusBanner message={action.message} />
    </section>
  );
}
