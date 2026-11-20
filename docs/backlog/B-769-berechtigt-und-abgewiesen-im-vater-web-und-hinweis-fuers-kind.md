---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/supervisor, rolle/student, bereich/frontend]
aliases: [Meldung berechtigt Oberfläche]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-627"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-769 · Berechtigt und abgewiesen im Vater-Web, Hinweis für das Kind, Beschriftung der Gegenbuchung

Die Oberfläche zu [B-627](B-627-berechtigte-meldung-bucht-nach.md): zwei Knöpfe an der Meldung des Vater-Webs, ein Hinweis an das Kind, der sagt, was nicht neu bewertet wird, und die richtige Beschriftung einer positiven Gegenbuchung.
**Baut nach B-767 und B-768.**

## User Story

Als **Supervisor** möchte ich an der Meldung „berechtigt“ oder „abgewiesen“ wählen; als **Student** möchte ich wissen, was die Bestätigung geändert hat und was nicht.

## Ist-Stand am Code

- Vater-Seite der Meldungen: `frontend/src/vater/VaterRueckmeldungen.tsx`, Panel am Ersteller: `ExerciseFeedbackPanel.tsx`; E2E `frontend/e2e/rueckmeldung.spec.ts`.
- Das Kind sieht den Hinweis nach „erledigt“ genau einmal (`frontend/src/sohn/KartenMeldung.tsx`, Test `ExerciseFeedbackTests.Kind_sieht_den_Hinweis_nach_erledigt_genau_einmal`).
- Beschriftung der Art `GoalPenalty`: `frontend/src/lib/labels.ts:19`, `i18n/de.ts:173`, `i18n/en.ts:161` („Pflicht gerissen (Malus)“) – unabhängig vom Vorzeichen.
- **Fremde, ungecommittete Arbeit am 2026-11-20:** `frontend/src/sohn/KartenMeldung.test.tsx` und weitere Dateien unter `frontend/src/sohn/` (LearningDad); den Kind-Hinweis erst nach dem Committen dieser Arbeit anfassen.

## Die echte Lücke

Knöpfe, Hinweis und Vorzeichen-Beschriftung; das Backend liefert die Wege.

## Entscheidungen

*Aus B-627 abgeleitet, stellvertretend, unbestätigt (Vorbereitungslauf 2026-11-20, Sonnet).*

1. **„Berechtigt“ nur bei einer Meldung mit Bezug** (B-627 Entscheidung 1): ohne Bezug bleibt nur „Erledigt“ (B-627 Entscheidung 2: Üben ohne gespeicherte Antwort).
2. **Der Hinweis an das Kind nennt, was nicht nachgebucht wird** (B-627 Entscheidung 7): Kasten, Missionen und eine geöffnete Kiste. *Kosten:* ein weiterer Satz.
3. **Ein positiver `GoalPenalty`-Eintrag heißt anders als der Malus** („Malus zurückgenommen“), nach Vorzeichen (B-627 Entscheidung 3).

## Akzeptanzkriterien

Neu aus den Entscheidungen; B-627 hatte keine Oberflächen-Kriterien.

1. Eine Meldung mit Bezug zeigt „Berechtigt“ und „Abgewiesen“, eine ohne Bezug nur „Erledigt“. *Rot, wenn der Knopf auch ohne Bezug erscheint (Vitest an `VaterRueckmeldungen`).*
2. Die Knöpfe sind beim Senden gesperrt (zweimal klicken sendet einmal). *Rot, wenn `disabled={busy}` fehlt.*
3. Der Hinweis an das Kind nach „berechtigt“ nennt, dass Kasten und Missionen nicht neu gebucht werden. *Rot, wenn der Satz fehlt.*
4. Ein positiver Eintrag der Art `GoalPenalty` erscheint als „Malus zurückgenommen“, ein negativer weiter als „Pflicht gerissen (Malus)“. *Rot, wenn beide gleich beschriftet sind.*

## Schätzung

**Größe: S** – wie `childId` aus dem Test-Pfad ziehen (B-01): zwei Knöpfe, ein Satz, eine Beschriftung nach Vorzeichen.

- **`wo: frontend`, `migration: nein`, `vertragsbruch: nein`** (alle Wege kommen aus B-767 und B-768).
- **Risiken:** (1) die Beschriftung hängt heute nur an der Art (`labels.ts`), nicht am Vorzeichen: die Kontoansicht muss den Betrag mitgeben; (2) fremde Arbeit an `frontend/src/sohn/`.

### Angriffsplan

1. Typen aus dem erzeugten Vertrag; i18n Deutsch und Englisch.
2. Knöpfe in `VaterRueckmeldungen` (`useAction`, `StatusBanner`), Hinweistext in `KartenMeldung`, Beschriftung nach Vorzeichen in der Kontoansicht.
3. Vitest, E2E, `frontend-reviewer`.

### Testweg

- `VaterRueckmeldungen.test.tsx` (neu, falls nicht vorhanden): AK 1, 2; `KartenMeldung.test.tsx`: AK 3; Test an der Kontoansicht: AK 4.
- E2E `frontend/e2e/rueckmeldung.spec.ts` um den Weg „Test falsch gewertet, Meldung, berechtigt, Kontostand“ erweitern; das ist der Rollengang als Vater und als Sohn.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-20 · aus B-627 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch. Baut nach B-767 und B-768; fremde Arbeit an `frontend/src/sohn/` abwarten.
