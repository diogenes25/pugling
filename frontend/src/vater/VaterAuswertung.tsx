import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import { ListenAusfall } from "../components/ListenAusfall";
import { formatDate, formatDateTime, heuteImLerntag } from "../lib/datum";
import type { ChildResponse, DailyEvaluation, EvaluationProfile, Paged, ProblemWord, ProfileWindow } from "../lib/types";

/**
 * Die Auswertung eines Kindes (B-733): je Tag und Fach, was gelernt wurde und wo es hakte – mit dem *Warum* (Fehlerart),
 * nicht nur einer Wortliste. Alles kommt vom Server: abgeschlossene Tage sind dort gespeichert und ändern sich nicht mehr,
 * der heutige Tag ist live. Ein Tag ohne Zeile (Sonntag, Pause, nicht geübt und keine Pflicht) ist eine **Lücke**, keine
 * Null – darum zeichnet der Verlauf jeden Tag des Zeitraums und markiert die Lücken ausdrücklich.
 */

/** Anzeigename je Fehlerart (Server-Enum `AnswerErrorKind`). Ein unbekannter Schlüssel erscheint roh statt gar nicht. */
export const FEHLERART: Record<string, string> = {
  Accent: "Akzent",
  Typo: "Tippfehler",
  Synonym: "Synonym",
  WrongWord: "falsches Wort",
};

export const fehlerartText = (kinds: Record<string, number>): string =>
  Object.entries(kinds)
    .sort(([a, x], [b, y]) => y - x || a.localeCompare(b))
    .map(([kind, n]) => `${FEHLERART[kind] ?? kind} ${n}×`)
    .join(", ");

/** Ein Tag im Verlauf: seine Zeilen je Fach, oder keine – dann ist er eine Lücke. */
export interface VerlaufTag {
  day: string;
  rows: DailyEvaluation[];
}

/**
 * Jeder Tag von `to` rückwärts bis `from` (beide ISO-Daten, einschließlich), mit den Zeilen des Servers. Tage ohne Zeile
 * bleiben drin und tragen eine leere Liste: die Oberfläche zeigt sie als Lücke, nie als Nulltag (AK 7).
 */
export function verlaufTage(rows: DailyEvaluation[], from: string, to: string): VerlaufTag[] {
  const byDay = new Map<string, DailyEvaluation[]>();
  for (const r of rows) byDay.set(r.day, [...(byDay.get(r.day) ?? []), r]);
  const tage: VerlaufTag[] = [];
  // Eine Zeile des Servers geht nie verloren: liegt sie hinter `to`, wächst das Fenster bis zu ihr.
  const last = [to, ...byDay.keys()].sort().reverse()[0];
  const end = new Date(`${last}T00:00:00Z`);
  for (let d = end; d >= new Date(`${from}T00:00:00Z`); d = new Date(d.getTime() - 86_400_000)) {
    const day = d.toISOString().slice(0, 10);
    tage.push({ day, rows: byDay.get(day) ?? [] });
  }
  return tage;
}

/**
 * Das Fenster des Verlaufs: 28 Lerntage bis heute, „heute" in deutscher Zeit wie am Server (B-730) – nicht der UTC-Tag,
 * sonst fehlte zwischen Mitternacht und 1 Uhr der laufende Tag und der älteste würde als Lücke gezeichnet.
 */
export function verlaufFenster(jetzt: Date = new Date()): { from: string; to: string } {
  const to = heuteImLerntag(jetzt);
  const from = new Date(Date.parse(`${to}T00:00:00Z`) - 27 * 86_400_000).toISOString().slice(0, 10);
  return { from, to };
}

const minuten = (seconds: number) => (seconds === 0 ? "–" : `${Math.max(1, Math.round(seconds / 60))} min`);

export function VaterAuswertung() {
  const childId = Number(useParams().childId);
  const child = useAsync<ChildResponse>(() => api.child(childId), [childId]);
  return (
    <>
      <div className="row" style={{ marginBottom: 8 }}>
        <h2 className="h-section">Auswertung{child.data && !child.stale ? ` · ${child.data.name}` : ""}</h2>
        <Link to={`/vater/kind/${childId}`} className="btn ghost small"
          style={{ marginLeft: "auto", textDecoration: "none" }}>← Kind</Link>
      </div>
      <ListenAusfall was="Die Angaben zum Kind" fehler={child.error} laedt={child.loading} onRetry={child.reload} schluessel={childId} />
      {/* Ein anderes Kind ist eine andere Auswertung: nichts vom vorigen bleibt stehen (B-509). */}
      <Profil key={`p${childId}`} childId={childId} />
      <Verlauf key={`v${childId}`} childId={childId} />
      <Problemwoerter key={`w${childId}`} childId={childId} />
    </>
  );
}

function Profil({ childId }: { childId: number }) {
  const profile = useAsync<EvaluationProfile>(() => api.childEvaluationProfile(childId), [childId]);
  return (
    <section className="card" aria-labelledby="profil-titel" style={{ marginBottom: 16 }}>
      <h3 id="profil-titel" className="h-sub">Profil je Fach</h3>
      <ListenAusfall was="Das Profil" fehler={profile.error} laedt={profile.loading && profile.data === null}
        onRetry={profile.reload} schluessel={childId} />
      {profile.data && profile.data.subjects.length === 0 && (
        <p className="muted">Noch keine ausgewerteten Tage in den letzten 28 Tagen.</p>
      )}
      {profile.data && profile.data.subjects.length > 0 && (
        <table className="table" aria-label="Profil">
          <thead>
            <tr><th>Fach</th><th>Zeitraum</th><th>Lerntage</th><th>Beherrschung</th><th>Tempo</th>
              <th>Belastbarkeit</th><th>Häufigste Fehler</th></tr>
          </thead>
          <tbody>
            {profile.data.subjects.flatMap((s) =>
              ([["7 Tage", s.last7Days], ["28 Tage", s.last28Days]] as [string, ProfileWindow][]).map(([label, w]) => (
                <tr key={`${s.subjectId ?? 0}-${label}`}>
                  <td>{s.subjectName || "ohne Fach"}</td>
                  <td>{label}</td>
                  <td>{w.learningDays}</td>
                  <td>{w.correctAnswers + w.wrongAnswers === 0 ? "–" : `${w.masteryPercent} %`}</td>
                  <td title="gefestigte Wörter je Lerntag">{w.learningDays === 0 ? "–" : `${w.tempoPerLearningDay} / Tag`}</td>
                  <td title="Tage mit allen Tagespflichten erfüllt">
                    {w.resiliencePercent == null ? "–" : `${w.resiliencePercent} % (${w.dutyDaysMet}/${w.dutyDays})`}
                  </td>
                  <td>{w.topErrorKinds.length === 0 ? "–"
                    : w.topErrorKinds.map((k) => `${FEHLERART[k.kind] ?? k.kind} ${k.count}×`).join(", ")}</td>
                </tr>
              )))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function Verlauf({ childId }: { childId: number }) {
  // Fenster einmal je Kind gebildet und an den Server gegeben: Gezeichnetes und Geliefertes kommen aus denselben Werten.
  const [{ from, to: today }] = useState(() => verlaufFenster());
  const rows = useAsync<DailyEvaluation[]>(() => api.childEvaluations(childId, { from, to: today }), [childId, from, today]);
  const tage = rows.data ? verlaufTage(rows.data, from, today) : [];
  return (
    <section className="card" aria-labelledby="verlauf-titel" style={{ marginBottom: 16 }}>
      <h3 id="verlauf-titel" className="h-sub">Verlauf der letzten 28 Tage</h3>
      <ListenAusfall was="Der Verlauf" fehler={rows.error} laedt={rows.loading && rows.data === null}
        onRetry={rows.reload} schluessel={childId} />
      {rows.data && (
        <table className="table" aria-label="Verlauf">
          <thead>
            <tr><th>Tag</th><th>Fach</th><th>Neu</th><th>Gefestigt</th><th>Zurückgefallen</th><th>Richtig / Falsch</th>
              <th>Übungszeit</th><th>Pflicht</th><th>Fehlerarten</th></tr>
          </thead>
          <tbody>
            {tage.flatMap((t) => t.rows.length === 0
              ? [(
                <tr key={t.day} className="muted" data-luecke="true">
                  <td>{formatDate(t.day)}</td>
                  <td colSpan={8}>{t.day === today ? "heute noch nichts" : "Lücke – nicht geübt, keine Tagespflicht"}</td>
                </tr>
              )]
              : t.rows.map((r) => (
                <tr key={`${t.day}-${r.subjectId ?? 0}`}>
                  <td>{formatDate(r.day)}{r.live ? " (heute, läuft)" : ""}</td>
                  <td>{r.subjectName || "ohne Fach"}</td>
                  <td>{r.newWords}</td>
                  <td>{r.consolidatedWords}</td>
                  <td>{r.fellBackWords}</td>
                  <td>{r.correctAnswers} / {r.wrongAnswers}</td>
                  <td>{minuten(r.practicedSeconds)}</td>
                  <td>{r.dutiesMet + r.dutiesMissed === 0 ? "–"
                    : r.dutiesMissed === 0 ? `✅ ${r.dutiesMet}`
                      : `${r.live ? "offen" : "❌ verpasst"} ${r.dutiesMissed}${r.dutiesMet ? ` · ✅ ${r.dutiesMet}` : ""}`}</td>
                  <td>{fehlerartText(r.errorKinds) || "–"}</td>
                </tr>
              )))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function Problemwoerter({ childId }: { childId: number }) {
  const [skip, setSkip] = useState(0);
  const words = useAsync<Paged<ProblemWord>>(() => api.childProblemWords(childId, { skip, take: PAGE_SIZE }), [childId, skip]);
  return (
    <section className="card" aria-labelledby="problem-titel">
      <h3 id="problem-titel" className="h-sub">Problemwörter der letzten 28 Tage</h3>
      <ListenAusfall was="Die Problemwörter" fehler={words.error} laedt={words.loading && words.data === null}
        onRetry={words.reload} schluessel={childId} />
      {words.data && words.data.total === 0 && <p className="muted">Keine falsche Antwort in den letzten 28 Tagen.</p>}
      {words.data && words.data.total > 0 && (
        <>
          <table className="table" aria-label="Problemwörter">
            <thead>
              <tr><th>Wort</th><th>Übersetzung</th><th>Falsch / Richtig</th><th>Warum</th><th>Zuletzt falsch</th>
                <th>Box</th></tr>
            </thead>
            <tbody>
              {words.data.items.map((w) => (
                <tr key={w.vocabularyId}>
                  <td>{w.front}</td>
                  <td>{w.back}</td>
                  <td>{w.wrongAnswers} / {w.correctAnswers}</td>
                  <td>{fehlerartText(w.errorKinds) || "–"}</td>
                  <td>{w.lastWrongAnswer ? <>„{w.lastWrongAnswer}“ · </> : null}{formatDateTime(w.lastWrongAt)}</td>
                  <td>{w.box} · {w.masteryPercent} %</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pager skip={skip} take={PAGE_SIZE} total={words.data.total} onSkip={setSkip} busy={words.loading || words.stale} />
        </>
      )}
    </section>
  );
}
