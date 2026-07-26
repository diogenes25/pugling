import { useEffect, useState } from "react";
import { api } from "./api";
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

/** Nachschlage-Sicht auf das Manifest; `null`, solange es lädt. */
export interface ExerciseTypeLookup {
  all: ExerciseTypeManifest[];
  byType: Map<string, ExerciseTypeManifest>;
  /** Anzeigename des Typs; unbekannte Typen behalten ihren Schlüssel (besser als eine Lücke). */
  label: (type: string) => string;
  /** Routen-Segment der Autoren-CRUD; `null`, wenn der Server den Typ nicht kennt. */
  route: (type: string) => string | null;
}

export function useExerciseTypes(): ExerciseTypeLookup | null {
  const [all, setAll] = useState<ExerciseTypeManifest[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    // Ein fehlgeschlagenes Manifest darf die Seite nicht blockieren: dann bleiben Namen die Schlüssel und
    // die Routen null (Bearbeiten/Löschen deaktiviert) – lesbar statt kaputt.
    loadExerciseTypes().then((m) => { if (!cancelled) setAll(m); }).catch(() => { if (!cancelled) setAll([]); });
    return () => { cancelled = true; };
  }, []);

  if (all === null) return null;
  const byType = new Map(all.map((m) => [m.type, m]));
  return {
    all,
    byType,
    label: (type) => byType.get(type)?.label ?? type,
    route: (type) => byType.get(type)?.authoringRoute ?? null,
  };
}
