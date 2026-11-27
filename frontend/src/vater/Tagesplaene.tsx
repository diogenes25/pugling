import { StatusBanner } from "../components/StatusBanner";
import { ListenAusfall } from "../components/ListenAusfall";
import { api } from "../lib/api";
import { formatDate, formatDateTime } from "../lib/datum";
import type { ChildResponse, DailyPlan } from "../lib/types";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";

/*
 * Der Tagesplaner (B-734): je Lerntag ab 6 Uhr ein eingefrorener Plan aus festen Regeln – fällige Wiederholungen zuerst,
 * Gescheitertes eine Stufe leichter, Boden und Deckel aus den Leitplanken. Im **Schattenmodus** sieht nur der Vater die
 * Pläne; sie wirken nicht auf Pflicht, Malus und die App des Kindes. Die Woche im Schatten ist die Messung, ob die Regeln
 * plausibel planen – darum steht an jeder Aufgabe ihre Begründung, und jeder Plan sagt ausdrücklich, dass er nicht wirkt.
 */

/** Was der Schalter dem Vater sagt, wenn er nicht schalten kann – die Regel, mit der auch der Server ablehnt. */
export function planerSchalterHinweis(child: ChildResponse): string | null {
  const komplett = child.dailyTasksMin != null && child.dailyTasksMax != null
    && child.dailyNewWordsMin != null && child.dailyNewWordsMax != null;
  return komplett ? null : "Erst die Leitplanken setzen – ohne Boden und Deckel plant der Tagesplaner nicht.";
}

/**
 * Die Zeile über einem Plan: ob er wirkt. Ein Schattenplan heißt nie „wirksam“. Erschöpfend über den Modus: kommt mit
 * B-736 ein wirksamer Modus dazu, zwingt `tsc` hier zu einer Entscheidung, statt dass er still als Schatten beschriftet wird.
 */
export function planWirkung(plan: DailyPlan, childName: string): string {
  switch (plan.mode) {
    case "Shadow": return `Schattenplan – wirkt nicht auf Pflicht, Malus und die App von ${childName}.`;
    case "Off": return "Plan ohne Wirkung.";
    default: { const unbekannt: never = plan.mode; return unbekannt; }
  }
}

/** Der Status des Planers in Worten – erschöpfend aus demselben Grund wie {@link planWirkung}. */
function planerStatus(mode: ChildResponse["plannerMode"]): string {
  switch (mode) {
    case "Off": return "aus";
    case "Shadow": return "im Schattenmodus";
    default: { const unbekannt: never = mode; return unbekannt; }
  }
}

export function TagesplanerSection({ child, onSaved }: { child: ChildResponse; onSaved: () => void }) {
  const an = child.plannerMode !== "Off";
  const action = useAction();
  const hinweis = planerSchalterHinweis(child);
  // Die Pläne hängen am Schalter: nach dem Einschalten entsteht der heutige Plan beim ersten Lesen. Ausgeschaltet heißt
  // `null`, nicht „leer“: `useAsync` behält das Ergebnis über den Wechsel des Schalters, und ein `[]` aus dem ausgeschalteten
  // Stand stünde als „Noch kein Plan“ da, während der Server den heutigen gerade anlegt – bei einem Ladefehler für immer.
  // Ein Wechsel des Kindes hängt die Fläche neu ein (`key` im Kind-Hub), Daten eines anderen Kindes stehen hier nie.
  const plans = useAsync(() => (an ? api.childDailyPlans(child.id) : Promise.resolve(null)), [child.id, an]);
  const missing = useAsync(() => (an ? api.childMissingDailyPlans(child.id) : Promise.resolve(null)), [child.id, an]);
  const planListe = plans.data;
  const fehlTage = missing.data;

  async function schalten() {
    if (an && !confirmAction(`Tagesplaner für ${child.name} ausschalten? Es entstehen keine neuen Pläne mehr.`)) return;
    const ok = await action.run(() => api.updateChild(child.id, { plannerMode: an ? "Off" : "Shadow" }),
      an ? "Tagesplaner ausgeschaltet." : "Tagesplaner im Schattenmodus eingeschaltet.");
    if (ok) onSaved();
  }

  return (
    <section>
      <h3 className="h-section">Tagesplaner</h3>
      <p className="sub">
        Der Tagesplaner stellt ab 6 Uhr den Lerntag von {child.name} zusammen: fällige Wiederholungen zuerst, was am letzten
        Lerntag schiefging eine Stufe leichter, neue Wörter im Rahmen der Leitplanken. Im Schattenmodus siehst nur du die
        Pläne – sie ändern nichts an Pflicht, Malus und der App von {child.name}.
      </p>
      <p>
        Status: <strong>{planerStatus(child.plannerMode)}</strong>
        {an && child.plannerSince && <> seit {formatDate(child.plannerSince)}</>}.
      </p>
      <button type="button" className={an ? "btn ghost small" : "btn small"} style={{ width: "auto" }}
        disabled={action.busy || (!an && hinweis !== null)} onClick={schalten}>
        {an ? "Tagesplaner ausschalten" : "Im Schattenmodus einschalten"}
      </button>
      {!an && hinweis && <p className="sub">{hinweis}</p>}
      <StatusBanner message={action.message} />

      {an && (
        <>
          <ListenAusfall was="Die Tage ohne Plan" fehler={missing.error} laedt={missing.loading} onRetry={missing.reload} schluessel={child.id} />
          {fehlTage && fehlTage.length > 0 && (
            <p className="banner info">
              Ohne Plan (der Server lief nicht): {fehlTage.map((d) => formatDate(d)).join(", ")}. Diese Tage zählen
              wie eine Pause – kein Malus, die Serie bleibt stehen.
            </p>
          )}
          <ListenAusfall was="Die Tagespläne" fehler={plans.error} laedt={plans.loading} onRetry={plans.reload} schluessel={child.id} />
          {plans.loading && planListe === null && <div className="loading">Lade…</div>}
          {planListe && planListe.length === 0 && (
            <p className="muted">Noch kein Plan – er entsteht an einem Lerntag ab 6 Uhr.</p>
          )}
          {(planListe ?? []).map((plan) => (
            <article key={plan.day} className="card" aria-label={`Tagesplan ${formatDate(plan.day)}`} style={{ marginTop: 12 }}>
              <h4 style={{ margin: "0 0 4px" }}>{formatDate(plan.day)}</h4>
              <p className="sub" style={{ margin: 0 }}>
                {planWirkung(plan, child.name)} Erstellt {formatDateTime(plan.createdAt)}; neue Wörter: {plan.newWordsBudget}.
              </p>
              {plan.warnings.map((w) => <p key={w} className="sub">⚠ {w}</p>)}
              {plan.tasks.length > 0 && (
                <table className="table" aria-label={`Aufgaben ${formatDate(plan.day)}`}>
                  <thead>
                    <tr><th>#</th><th>Fach</th><th>Übung</th><th>Wiederholungen</th><th>Neue Wörter</th><th>Warum</th></tr>
                  </thead>
                  <tbody>
                    {plan.tasks.map((t) => (
                      <tr key={t.order}>
                        <td>{t.order}</td>
                        <td>{t.subjectName || "–"}</td>
                        <td>{t.exerciseTitle}</td>
                        <td>{t.dueReviews}</td>
                        <td>{t.newWords}</td>
                        <td>{t.reason}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </article>
          ))}
        </>
      )}
    </section>
  );
}
