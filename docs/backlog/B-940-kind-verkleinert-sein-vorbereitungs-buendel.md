---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: "Review von B-752 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, pugling-reviewer 🟡)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-24"
---

# B-940 · Das Kind kann das Bündel seiner Vorbereitungs-Pflicht über Schlagwörter verkleinern

## User Story

Als Vater möchte ich, dass der Stoff einer geplanten Klassenarbeit nicht vom Kind verkleinert werden kann, damit die
Vorbereitungs-Pflicht (B-752) wirklich den Stoff abfragt, den ich festgelegt habe.

## Ist-Stand am Code

- Schlagwörter gehören dem Kind; `FindOwnedAsync` lässt jeden durch, dem das Kind gehört – auch das Kind selbst
  (`backend/Pugling.Api/Controllers/Creator/TagsController.cs:40-46`).
- `UntagExercise` löst eine Verknüpfung ohne weitere Prüfung (`TagsController.cs:192-204`).
- `Delete` löscht das ganze Schlagwort; die Verknüpfung zur Klassenarbeit fällt per Cascade mit
  (`TagsController.cs:113-123`, `Data/PuglingDbContext.cs:1071`). Derselbe Weg, nur gröber – in der Ursprungsmeldung nicht genannt.
- Das Bündel liest die Schlagwörter live (`Services/Shared/ClassTestBundle.cs`), vor dem Start (`NewCandidatesAsync`) und
  danach in `AlignSpansAsync` (`Services/Shared/ClassTestPrepService.cs:236`).
- Eine Oberfläche für das Kind gibt es nicht (`frontend/src/lib/api.ts` kennt kein `DELETE creator/tags/{id}/exercises`);
  ein Kind-Token erreicht den Endpunkt trotzdem.

## Die echte Lücke

Zwei Schreibwege eines Kind-Tokens (Verknüpfung lösen, Schlagwort löschen) verkleinern das Bündel einer geplanten Arbeit.

## Entscheidungen

1. **Sperre bei geplanter Arbeit** (Nutzer, 2026-11-24): Ein Kind darf eine Übungs-Verknüpfung nicht lösen und das
   Schlagwort nicht löschen, solange es an einer Klassenarbeit mit Status `Planned` hängt. Erwachsene bleiben frei.
   Kosten: ein Fehlercode, eine Abfrage je Schreibweg; keine Migration. Verworfen: „nur Vater-Verknüpfungen zählen"
   (Migration) und „Kind darf nie lösen" (nimmt das Aufräumen eigener Schlagwörter).
2. Vokabel-Verknüpfungen bleiben frei: das Bündel zählt nur Übungen.
3. Die Meldung nennt die Arbeit und nur den Schritt, der die Sperre aufhebt: die Arbeit ist geschrieben oder abgesagt, oder
   ein Betreuer nimmt das Schlagwort aus ihr.

## Akzeptanzkriterien

1. Kind-Token, Schlagwort an geplanter Arbeit: `DELETE creator/tags/{t}/exercises/{e}` → `409 tag_locked_by_class_test`,
   die Verknüpfung bleibt.
2. Kind-Token, Schlagwort an geplanter Arbeit: `DELETE creator/tags/{t}` → `409 tag_locked_by_class_test`, das Schlagwort bleibt.
3. Kind-Token, Schlagwort nur an geschriebener bzw. abgesagter Arbeit: beide Wege `204`.
4. Vater-Token, Schlagwort an geplanter Arbeit: beide Wege `204`.

## Schätzung

- **Risiken:** Die Sperre darf nicht an Erwachsenen hängen (Vater plant um). Die Meldung darf keinen Weg nennen, den das Kind
  nicht gehen kann.
- **Angriffsplan:** `ApiErrors.TagLockedByClassTest` (409) → ein Helfer in `TagsController`, der für Studenten die geplante
  Arbeit zum Schlagwort sucht → in `UntagExercise` und `Delete` vor dem Entfernen.
- **Testweg:** Integrationstest `VorbereitungsBuendelSperreTests` (AK 1–4), Mutanten je Weg und je Bedingung.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
- **2026-11-24** — ausformuliert, mit dem Nutzer entschieden (Dialog der PM-Sitzung), geschätzt; Protokoll [pm-sitzung-2026-11-24.md](../pm-sitzung-2026-11-24.md).
- **2026-11-24** — `geschaetzt` → `in-arbeit`, gebaut (Sprint 135). Belege je Fall:
  - AK 1, Kind löst Übung aus Schlagwort geplanter Arbeit → `backend/Pugling.Api.Tests/VorbereitungsBuendelSperreTests.cs:71`, Mutante B940-untag (Sperre in `UntagExercise` entfernt) → rot
  - AK 2, Kind löscht Schlagwort geplanter Arbeit → `backend/Pugling.Api.Tests/VorbereitungsBuendelSperreTests.cs:80`, Mutante B940-delete (Sperre in `Delete` entfernt) → rot
  - AK 3, Arbeit geschrieben/abgesagt × beide Wege (4 Zeilen) → `backend/Pugling.Api.Tests/VorbereitungsBuendelSperreTests.cs:94`, Mutante B940-status (Statusbedingung entfernt) → rot
  - AK 4, Vater × beide Wege → `backend/Pugling.Api.Tests/VorbereitungsBuendelSperreTests.cs:109`, Mutante B940-adult (Sperre auch für Erwachsene) → rot
  - Fehlercode `tag_locked_by_class_test` ohne deutschen Text: Ausnahme mit Grund in `ProblemTextGuardTests` (nur ein Kind-Token löst ihn aus, das Kind hat keine Schlagwort-Oberfläche). Die Meldung nennt nur die Schritte, die die Sperre aufheben (`pugling-reviewer`: in Ordnung). Vokabel-Verknüpfungen bleiben frei (Entscheidung 2).
  - Mutanten gemessen mit `.claude/scripts/mutant-probe.py` nach der letzten Code-Änderung der Logik (13 + 4 Mutanten, alle rot; Grundlinie danach grün; Tabelle im Protokoll pm-sitzung-2026-11-24.md).
- **2026-11-24** — `in-arbeit` → `abgenommen` (Sprint 135): Commit `1b92cd02`; Backend-Suite 2309/2309 grün (Release, nach der letzten Code-Änderung), Frontend `vitest` 1426/1426 und `tsc -b` grün, Mutanten-Probe 17/17 rot; `pugling-reviewer` (ein 🟡 behoben, siehe B-941) und `frontend-reviewer` (keine 🔴/🟡) gelaufen. Rollengang maschinell: E2E `klassenarbeit-stoff-dicht`, `klassenarbeit-vorbereitung`, `klassenarbeit-countdown`, `pfad` 4 passed (Ports 5231/5204, Server von Playwright nach der letzten Änderung gestartet).
