import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { type ActionState, useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { ExerciseRating, ExerciseSummary } from "../lib/types";

/*
 * Bewertungen geteilter Übungen (B-12).
 *
 * Der Katalog ist gemeinsam, und ohne ein Qualitätssignal sieht eine sorgfältig gebaute Übung genauso aus
 * wie eine hingeworfene. Bewerten dürfen darum die, die fremdes Material **nutzen** – nicht die, die es
 * verwalten: deren Sterne würden das Signal nur aufblähen (der Server antwortet dann
 * `403 cannot_rate_own_exercise`, hier fehlt das Formular gleich ganz).
 */

/** Durchschnitt mit einer Nachkommastelle, deutsch formatiert: 3,5. */
export function formatAverage(average: number): string {
  return average.toLocaleString("de-DE", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
}

/** Kurztext für die Listenzeile: „★ 3,5 (2 Bewertungen)" bzw. „noch unbewertet". */
export function ratingSummary(average: number | null | undefined, count: number): string {
  if (count === 0 || average == null) return "noch unbewertet";
  return `★ ${formatAverage(average)} (${count} ${count === 1 ? "Bewertung" : "Bewertungen"})`;
}

/** Sterne als Zeichenkette – fünf Plätze, gefüllt bis `stars`. */
export function starString(stars: number): string {
  return "★".repeat(stars) + "☆".repeat(Math.max(0, 5 - stars));
}

/**
 * Alle Bewertungen einer Übung plus – für fremde Übungen – das eigene Formular. `onChanged` frischt die
 * Liste auf, damit Durchschnitt und Anzahl in der Zeile nachziehen.
 */
export function ExerciseRatingPanel({ exercise, onChanged }: { exercise: ExerciseSummary; onChanged: () => void }) {
  const ratings = useAsync<ExerciseRating[]>(() => api.exerciseRatings(exercise.id), [exercise.id]);
  const mine = ratings.data?.find((r) => r.isMine) ?? null;
  // Die Rückmeldung lebt hier und nicht im Formular: das Formular wird nach dem Speichern neu aufgesetzt,
  // und mit ihm verschwände sonst die Meldung „gespeichert".
  const action = useAction();

  function changed() {
    ratings.reload();
    onChanged();
  }

  return (
    <div style={{ marginTop: 8, fontSize: 13 }}>
      {ratings.error && <div className="banner err">{ratings.error}</div>}
      {ratings.loading && ratings.data === null && <div className="loading">Lade…</div>}
      {ratings.data && (ratings.data.length === 0
        ? <p className="muted" style={{ margin: "4px 0" }}>Noch niemand hat diese Übung bewertet.</p>
        : (
          <ul aria-label="Bewertungen" style={{ listStyle: "none", padding: 0, margin: "4px 0" }}>
            {ratings.data.map((r) => (
              <li key={r.creatorId} style={{ marginBottom: 4 }}>
                <span aria-label={`${r.stars} von 5 Sternen`}>{starString(r.stars)}</span>{" "}
                <strong>{r.creatorName}</strong>{r.isMine && <span className="muted"> (du)</span>}
                {r.comment && <> – {r.comment}</>}
              </li>
            ))}
          </ul>
        ))}

      {exercise.isOwner
        ? <p className="muted" style={{ margin: "4px 0" }}>Deine eigene Übung kannst du nicht bewerten – das tun die, die sie nutzen.</p>
        : ratings.data && (
          // Neu aufgesetzt, sobald sich die eigene Bewertung ändert: das Formular zeigt dann den gespeicherten Stand.
          <RatingForm key={mine ? `${mine.stars}|${mine.comment ?? ""}|${mine.updatedAt}` : "neu"}
            exerciseId={exercise.id} title={exercise.title} mine={mine} action={action} onChanged={changed} />
        )}
      <StatusBanner message={action.message} />
    </div>
  );
}

function RatingForm({ exerciseId, title, mine, action, onChanged }: {
  exerciseId: number; title: string; mine: ExerciseRating | null; action: ActionState; onChanged: () => void;
}) {
  const [stars, setStars] = useState<number>(mine?.stars ?? 0);
  const [comment, setComment] = useState(mine?.comment ?? "");

  async function save(e: React.FormEvent) {
    e.preventDefault();
    if (stars < 1) { action.fail("Bitte 1 bis 5 Sterne wählen."); return; }
    const text = comment.trim();
    if (await action.run(() => api.rateExercise(exerciseId, stars, text === "" ? null : text), "Bewertung gespeichert.")) onChanged();
  }

  async function remove() {
    if (await action.run(() => api.removeExerciseRating(exerciseId), "Bewertung gelöscht.")) onChanged();
  }

  return (
    <form onSubmit={save} style={{ marginTop: 6 }}>
      <fieldset style={{ border: "none", padding: 0, margin: 0 }}>
        <legend style={{ fontWeight: 600 }}>Deine Bewertung für „{title}"</legend>
        <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
          {[1, 2, 3, 4, 5].map((n) => (
            <label key={n} className="row" style={{ gap: 4, alignItems: "center" }}>
              <input type="radio" name={`stars-${exerciseId}`} value={n} checked={stars === n}
                onChange={() => setStars(n)} />
              {n} {n === 1 ? "Stern" : "Sterne"}
            </label>
          ))}
        </div>
      </fieldset>
      <div className="field" style={{ marginTop: 6 }}>
        <label htmlFor={`rating-comment-${exerciseId}`}>Kommentar <span className="muted">(optional)</span></label>
        <textarea id={`rating-comment-${exerciseId}`} rows={2} maxLength={2000} value={comment}
          onChange={(e) => setComment(e.target.value)} />
      </div>
      <div className="row" style={{ gap: 8, marginTop: 6 }}>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {mine ? "Bewertung ändern" : "Bewertung speichern"}
        </button>
        {mine && (
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy}
            onClick={remove}>Bewertung löschen</button>
        )}
      </div>
    </form>
  );
}
