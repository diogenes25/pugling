---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT verlinkt neue Paare nicht]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
---

# B-392 · Eine geänderte Übersetzung legt neue Paare nicht im Speicher an

`ExerciseControllerBase.Update` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:454`) ruft nur
`NormalizeConfig`, nicht `NormalizeConfigAsync` – das tut nur `Create` (`:367`). Für die Übersetzung heißt das: ein Paar ohne
`vocabularyId` wird beim `PUT` unverlinkt gespeichert, `_self` bleibt `null`, und das Paar landet nie im Vokabelspeicher.

**Die Oberfläche löst das selbst aus:** `frontend/src/vater/exerciseConfig.tsx:294` setzt die Id auf `null`, sobald Quelle oder Ziel
bearbeitet wurden, mit dem Kommentar „dann muss der Server neu auflösen (er legt es an bzw. findet es)" – der Server tut es beim
`PUT` nicht. Auch die Klassen-Summary des `TranslationController` („created in the store and linked on save") stimmt beim Ändern
nicht.

Am Code bestätigt (Stand `e0fdd3b`). Beim Ausformulieren zu prüfen: ob andere Typen mit `NormalizeConfigAsync` betroffen sind, und
dass die Vokabelübung (Abgleich über `AfterSaveAsync`) unberührt bleibt.

## User Story

Als **Creator** möchte ich, dass ein Übersetzungspaar, dessen Text ich ändere, wieder im Vokabelspeicher steht und verlinkt ist –
so wie beim Anlegen. Sonst verliert jede Korrektur eines Satzes still seinen Speicher-Eintrag.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** `Update` ruft `NormalizeConfigAsync` wie `Create`. Am Code geprüft: nur die Übersetzung überschreibt
   den Schritt, kein anderer Typ ändert sein Verhalten beim `PUT`.
2. **(Bau)** Der Aufruf steht **nach** `UpdateConflictAsync`: der Schritt darf schon Speicher-Einträge schreiben, und ein abgewiesener
   `PUT` soll nichts halb schreiben.

## Akzeptanzkriterien

1. `PUT` einer Übersetzung mit einem Paar ohne `vocabularyId` → die Antwort trägt eine `vocabularyId` und `_self`, und `usage` dieser
   Vokabel listet die Übung.
2. Die vorher verlinkte Vokabel ist danach nicht mehr verwendet (löschbar).

## Schätzung

`XS`, `wo: backend`, eine Zeile im Basis-Controller plus Summary; Integrationsfall neben den B-386-Fällen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu B-386 (Lauf W, Retro Sprint 8; frischer `pugling-reviewer`, 🟡). Älter als B-386
  (der `PUT` hat nie normalisiert), darum ohne `entgangen_bei`.
- **2026-11-05** — ausformuliert, gegrillt (`art: Defekt`), geschätzt und gebaut in Lauf W, Sprint 9. AK 1 →
  `CatalogReadDeleteTests.cs:197-202` (Id, `_self`, Usage), AK 2 → `:203` (alte Vokabel → `204`). Rote Probe: der Aufruf in `Update`
  entfällt → rot in `:197` (das Paar hat keine Id), 1 von 19, kompiliert.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴; Reihenfolge geprüft (nach dem Aufruf kann nichts mehr abweisen, die Übung ist bis dahin unverändert), kein anderer Typ betroffen, die Klassen-Summary der Übersetzung stimmt jetzt auch beim Ersetzen. 🟢 Parameter `subjectId` der Basis hieß falsch (übergeben wird `seriesId`) – umbenannt, **Zähler Sprint 9: 1 von 5**. 🟡 zu B-393 siehe dort.
- **2026-11-05** — **abgenommen.** Volle Suite 1090/1090 nach dem letzten Edit; Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein frisch gestartetes Backend mit der Änderung (danach nur die Umbenennung eines Parameters). **Grenze:** keine Spec ändert den Text eines Übersetzungspaars im Browser – der Weg „Paar bearbeiten → Speichern → neu verlinkt“ ist nur durch den Integrationsfall belegt, nicht geklickt. Vater und Sohn unberührt (nur der Creator-`PUT`). Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `c429792`.
- **2026-11-05** — **Nachschau** (Lauf X, Retro Sprint 10; frischer `pugling-reviewer`, `git show c429792`, 1098/1098 an HEAD). Kein 🔴;
  ein abgewiesener `PUT` schreibt nichts halb, kein anderer Typ betroffen, der Editor-Kommentar stimmt jetzt. **Entgangen (🟡):** der
  eigentliche Klickweg – eine gemischte Liste (ein Paar behält seine Id, das bearbeitete bekommt eine neue) und „findet es“ (geänderter
  Text trifft einen vorhandenen Eintrag) – hat keinen Test → [B-395](B-395-uebersetzung-put-mischliste-ungetestet.md). **Berichtigt:**
  oben steht „AK 2 → `:203`“; diese Zusicherung ist auch ohne den Fix grün (ein unverlinktes Paar gibt die alte Vokabel ebenso frei) –
  sie ist Gegenprobe, kein Beleg; AK 2 hat damit keinen Test, der allein fällt (in B-395 mit aufgenommen). 🟢 hingenommen: zwei
  getrennte Speichervorgänge (älteres Muster, verwaiste Einträge wären löschbar); `= default` am Token des virtuellen Hooks (älter).
  🟢 Sprachwechsel im Editor behält die alte Bindung → [B-396](B-396-uebersetzung-sprachwechsel-behaelt-bindung.md).
