import { useCallback, useEffect, useState } from "react";
import { api, errorMessage } from "./api";
import type { ExerciseTypeManifest } from "./types";

/**
 * Das Typ-Manifest als geteilte Quelle für **Routen-Segment und Anzeigename** eines Übungstyps.
 *
 * Vorher standen beide in drei hartkodierten Tabellen (Anlegen, Filterleiste, Positionsliste). Das ging
 * zwangsläufig auseinander: der Server führt zwölf Typen, das UI kannte sechs, und die Route weicht vom
 * Schlüssel ab (Aufsatz → `essays`). Wer den Namen eines Typs anzeigt oder seine CRUD-Route baut, fragt
 * darum hier – nicht in einer Kopie.
 *
 * Bewusst **nicht** manifest-gesteuert: die Editoren selbst. Welche Felder ein Lückentext braucht, steht
 * nicht im Manifest und ließe sich auch nicht generisch erzeugen; die kennt `vater/exerciseConfig.tsx`.
 */

// Modul-Cache: das Manifest ist für die Sitzung konstant (es beschreibt den Server-Build). Ohne den Cache
// lüde jede Komponente, die einen Typnamen anzeigt, die Liste erneut.
let pending: Promise<ExerciseTypeManifest[]> | null = null;

export function loadExerciseTypes(): Promise<ExerciseTypeManifest[]> {
  // Bei einem Fehlschlag den Cache leeren, damit ein späterer Aufruf es erneut versucht.
  pending ??= api.exerciseTypes().catch((e) => { pending = null; throw e; });
  return pending;
}

/**
 * Nachschlage-Sicht auf das Manifest.
 *
 * Sie ist **immer** da, auch während des Ladens und nach einem Fehlschlag – mit leerer Liste. `label()`
 * fällt dann auf den Schlüssel zurück, `route()` auf `null` (Anlegen/Bearbeiten gesperrt statt gegen
 * eine geratene Route zu posten): „lesbar statt kaputt". Neu seit B-215 ist, dass der Ausfall dabei
 * **aussprechbar** ist (`error`) und einen Weg zurück hat (`reload`). Vorher schluckte der Hook den
 * Fehlschlag, und „Server kennt null Typen" war von „Abruf gescheitert" nicht zu unterscheiden – der
 * Creator saß vor gesperrten Knöpfen ohne Grund.
 */
export interface ExerciseTypeLookup {
  all: ExerciseTypeManifest[];
  byType: Map<string, ExerciseTypeManifest>;
  /** Anzeigename des Typs; unbekannte Typen behalten ihren Schlüssel (besser als eine Lücke). */
  label: (type: string) => string;
  /** Routen-Segment der Autoren-CRUD; `null`, wenn der Server den Typ nicht kennt – oder das Manifest noch lädt bzw. sein Abruf scheiterte. */
  route: (type: string) => string | null;
  /** Wahr, solange ein Abruf läuft – der erste wie jede Wiederholung. */
  loading: boolean;
  /** Der Fehlschlag des letzten Abrufs; `null`, solange geladen wird oder er gelang. */
  error: string | null;
  /** Lädt das Manifest neu; der Modul-Cache ist nach einem Fehlschlag ohnehin leer. */
  reload: () => void;
}

export function useExerciseTypes(): ExerciseTypeLookup {
  const [all, setAll] = useState<ExerciseTypeManifest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [nonce, setNonce] = useState(0);
  const reload = useCallback(() => setNonce((n) => n + 1), []);

  useEffect(() => {
    let cancelled = false;
    // `error` und `loading` im selben Schritt – dieselbe Choreographie wie `useAsync`. Wer ein Banner
    // daran hängt, nimmt `useGemerkterFehler` (B-216), sonst verschwindet es unter dem Wiederhol-Knopf.
    setLoading(true);
    setError(null);
    loadExerciseTypes()
      .then((m) => { if (!cancelled) setAll(m); })
      .catch((e: unknown) => { if (!cancelled) setError(errorMessage(e)); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [nonce]);

  const byType = new Map(all.map((m) => [m.type, m]));
  return {
    all,
    byType,
    label: (type) => byType.get(type)?.label ?? type,
    route: (type) => byType.get(type)?.authoringRoute ?? null,
    loading,
    error,
    reload,
  };
}
