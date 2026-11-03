import { useCallback, useEffect, useState } from "react";
import { ApiError, errorMessage } from "./api";
import { useAuth } from "./auth";

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  /**
   * Wahr, solange der **erste** Versuch noch nicht abgeschlossen ist – der einzige Zustand, in dem ein
   * „Lade…" die Wahrheit sagt.
   *
   * Zwei Platzhalter-Bedingungen sind richtig, und welche passt, entscheidet, was darunter sitzt (B-184):
   * eine Liste nimmt `loading && data === null`, damit ein `reload` ihre Zeilen nicht aushängt; eine
   * Fläche mit Formular nimmt dieses Feld, damit ein `reload` das Formular nicht mitsamt seinem Zustand
   * aus dem DOM nimmt. Nie richtig ist ein blankes `data === null`: Nach einer gescheiterten ersten
   * Ladung behauptet es dauerhaft „lädt noch" – neben einem Fehlerbanner, das im selben Blickfeld das
   * Gegenteil sagt.
   *
   * Es hängt darum am **abgeschlossenen Versuch**, nicht an `data === null && error === null`: ein `fn`,
   * das rechtmäßig `null` liefert (ein optionales Profil, ein Kind ohne Lehrbuch), setzt keins von
   * beidem und bekäme sonst dauerhaft denselben „Lade…" – dieselbe Lüge wie nach einem Fehler, nur ohne
   * Banner daneben, das sie verrät.
   */
  showPlaceholder: boolean;
  /**
   * Wahr, solange `data` zu **anderen** Abhängigkeiten gehört als den aktuellen (B-164) – nach einem Wechsel, bis die
   * neue Antwort da ist, und weiter, wenn sie scheitert (dann stehen die alten Daten noch). Ein `reload` macht es nicht
   * wahr: dieselbe Abfrage neu geladen ist keine andere. Das ist die Unterscheidung, die `loading` nicht trifft.
   *
   * Verglichen wird je Element (`Object.is`). Wer „abc " und „abc" als dieselbe Abfrage meint, übergibt die normalisierte
   * Form als Abhängigkeit – dann löst der Leerschlag auch keine Abfrage aus. Sperren, nicht ausblenden: wer `stale` liest,
   * lässt die Zeilen stehen und nimmt ihnen die Bedienbarkeit (B-169), sonst flackert jede Liste.
   */
  stale: boolean;
  reload: () => void;
}

/** Ob zwei Abhängigkeitslisten dieselbe Abfrage meinen – je Element, wie React selbst seine Abhängigkeiten vergleicht. */
function sameDeps(a: readonly unknown[], b: readonly unknown[]): boolean {
  return a.length === b.length && a.every((v, i) => Object.is(v, b[i]));
}

/**
 * Lädt asynchrone Daten mit Lade-/Fehlerzustand. Bei 401 (Token weg/abgelaufen)
 * wird automatisch ausgeloggt, damit der Nutzer sauber auf den Login zurückfällt.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[] = []): AsyncState<T> {
  const { signOut } = useAuth();
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [nonce, setNonce] = useState(0);
  // Ob überhaupt schon ein Versuch durch ist – die einzige Frage, die `showPlaceholder` beantwortet.
  // Bleibt danach `true`, damit ein `reload` das Formular darunter nicht aushängt.
  const [settled, setSettled] = useState(false);
  // Zu welchen Abhängigkeiten das Gezeigte gehört (B-164). Anfangs die des ersten Renders: auch ohne Daten gehört der
  // leere Zustand zu ihnen – scheitert die erste Abfrage und wechseln danach die Kriterien, läuft eine ANDERE Suche.
  const [dataDeps, setDataDeps] = useState<readonly unknown[]>(() => deps);

  const reload = useCallback(() => setNonce((n) => n + 1), []);

  useEffect(() => {
    let cancelled = false;
    const callDeps = deps;
    setLoading(true);
    setError(null);
    fn()
      .then((d) => { if (!cancelled) { setData(d); setDataDeps(callDeps); } })
      .catch((e: unknown) => {
        if (cancelled) return;
        if (e instanceof ApiError && e.status === 401) { signOut(); return; }
        setError(errorMessage(e));
      })
      .finally(() => { if (!cancelled) { setLoading(false); setSettled(true); } });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, nonce]);

  return { data, loading, error, showPlaceholder: !settled, stale: !sameDeps(dataDeps, deps), reload };
}
