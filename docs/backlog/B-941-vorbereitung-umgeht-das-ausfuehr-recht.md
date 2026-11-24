---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "beides"
migration: nein
vertragsbruch: nein
quelle: "Review von B-752 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, pugling-reviewer 🟡)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-24"
---

# B-941 · Die Vorbereitungs-Pflicht legt Schlagwort-Übungen ohne Ausführ-Recht als Position an

## User Story

Als Creator möchte ich, dass zurückgezogenes Material („stoppt neue Zuweisungen") auch über ein Schlagwort nicht neu zur
Pflicht wird, damit mein Zurückziehen hält. Als Vater möchte ich sehen, wenn eine Übung deshalb aus der Vorbereitung fällt.

## Ist-Stand am Code

- Direktes Zuweisen prüft das Ausführ-Recht (`Controllers/Supervisor/KlassenarbeitenController.cs:385-389`,
  `Auth/ExercisePermissionService.cs:44-50`: `ExecutePublic` oder irgendein Grant des handelnden Erwachsenen).
- Die Vorbereitung legt jede Bündel-Übung an, ohne zu prüfen (`Services/Shared/ClassTestPrepService.cs:124-135`
  `NewCandidatesAsync`, verwendet von `StartAsync` :183 und `CardsPerDayAsync` :116). Sie läuft beim Login des Kindes, also
  ohne Erwachsenen-Principal.
- Schlagwörter dürfen fremdes Material tragen (`TagsController.cs:163-167`).
- `KlassenarbeitResponse` trägt `PrepCardsPerDay`/`PrepBlockedReason`, aber nichts über ausgelassene Übungen
  (`Pugling.Contracts/Supervisor/ClassTestDtos.cs:17-20`).

## Die echte Lücke

Der Start einer Vorbereitung ist eine neue Zuweisung, prüft aber das Ausführ-Recht nicht.

## Entscheidungen

1. **Nur ausführbare zählen** (Nutzer, 2026-11-24): Kandidat ist eine Bündel-Übung nur, wenn sie `ExecutePublic` ist oder
   einen Grant für einen Betreuer des Kindes (`SupervisorLink`) trägt – dieselbe Regel wie beim Zuweisen, nur ohne Principal.
   Gilt für direkt zugewiesene Übungen ebenso (nachträglich zurückgezogen = keine neue Zuweisung).
2. Bereits angelegte Positionen bleiben (Zurückziehen stoppt nur Neues).
3. Die Vater-Sicht nennt die Zahl: additives Feld `PrepNotExecutableCount` in `KlassenarbeitResponse`, die Zelle zeigt
   „N ohne Ausführ-Recht ausgelassen".

## Akzeptanzkriterien

1. Eine nicht öffentliche Übung ohne Grant eines Betreuers im Schlagwort einer Arbeit wird beim Start keine Position.
2. Dieselbe Übung mit Grant eines Betreuers des Kindes wird Position; ein Grant nur eines fremden Erwachsenen reicht nicht.
3. `prepCardsPerDay` vor dem Start zählt die ausgelassene Übung nicht.
4. `prepNotExecutableCount` nennt die Zahl der ausgelassenen Bündel-Übungen (0 ohne).
5. Die Vater-Zelle zeigt den Hinweis bei Zahl > 0 und nicht bei 0.

## Schätzung

- **Risiken:** Ein Grant eines *fremden* Erwachsenen darf nicht reichen. Die Ausführbarkeit gehört an **eine** Stelle (Senke
  `NewCandidatesAsync`), nicht getrennt an Start und Zählung.
- **Angriffsplan:** Filter in `NewCandidatesAsync` (Senke für Start und Zählung) → Zählung → Feld im Vertrag und in der
  Projektion des Controllers → Contract neu erzeugen → `VorbereitungsZelle`.
- **Testweg:** Integrationstests in `VorbereitungsPflichtTests` (AK 1–4), Vitest in `VaterClassTests.test.tsx` (AK 5).

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
- **2026-11-24** — ausformuliert, mit dem Nutzer entschieden (Dialog der PM-Sitzung), geschätzt; Protokoll [pm-sitzung-2026-11-24.md](../pm-sitzung-2026-11-24.md).
- **2026-11-24** — `geschaetzt` → `in-arbeit`, gebaut (Sprint 135). Belege je Fall:
  - AK 1, nicht öffentlich, Grant nur eines Fremden → keine Position → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:562`, Mutanten B941-anygrant (jeder Grant zählt) und B941-sink (Senke ungefiltert) → rot
  - AK 2, Grant eines Betreuers → Position → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:626`, Mutante B941-grant (Grant-Klausel entfernt) → rot
  - AK 2, öffentlich ohne Grant eines Betreuers → Position → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:626`, Mutante B941-public (`ExecutePublic` entfernt) → rot (erste Messung grün: die öffentliche Übung trug zugleich den Grant des Vaters; Test darauf umgestellt, neu gemessen rot)
  - AK 3, `prepCardsPerDay` vor dem Start ohne ausgelassene Übung → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:562` (Zusicherung 1), Mutante B941-sink → rot
  - AK 4, `prepNotExecutableCount` = 1 → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:562`, Mutante B941-count (`return 0`) → rot; Grenzen (in Plan, Testtag, abgesagt) → `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:605`, Mutanten R3-inplan, R3-day, R3-status → rot
  - AK 5, Hinweis bei > 0, Singular, nicht bei 0 → `frontend/src/vater/VaterClassTests.test.tsx:111`, Mutanten B941-cell (Hinweis nie) und B941-cell0 (`>= 0`) → rot
  - Review-Fund 🟡 (pugling-reviewer): Bündel nur aus zurückgezogenem Material fror die Arbeit ein → behoben (Anspruch nur mit ausführbarer Übung), `backend/Pugling.Api.Tests/VorbereitungsPflichtTests.cs:584`, Mutante R1-claim (alte Leer-Prüfung) → rot. Bewusst so gelassen und im Code begründet: eine laufende Position reicht weiter bis zu einer späteren Arbeit desselben Bündels (keine neue Zuweisung).
  - Mutanten gemessen mit `.claude/scripts/mutant-probe.py` nach der letzten Code-Änderung der Logik (13 + 4 Mutanten, alle rot; Grundlinie danach grün; Tabelle im Protokoll pm-sitzung-2026-11-24.md).
- **2026-11-24** — `in-arbeit` → `abgenommen` (Sprint 135): Commit `1b92cd02`; Backend-Suite 2309/2309 grün (Release, nach der letzten Code-Änderung), Frontend `vitest` 1426/1426 und `tsc -b` grün, Mutanten-Probe 17/17 rot; `pugling-reviewer` (ein 🟡 behoben, siehe B-941) und `frontend-reviewer` (keine 🔴/🟡) gelaufen. Rollengang maschinell: E2E `klassenarbeit-stoff-dicht`, `klassenarbeit-vorbereitung`, `klassenarbeit-countdown`, `pfad` 4 passed (Ports 5231/5204, Server von Playwright nach der letzten Änderung gestartet).
