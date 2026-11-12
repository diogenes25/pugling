---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 29, IntroducedCount Pool]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 29/30)
entgangen_bei: [B-04, B-451]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-478 · Nachschau-Reste Sprint 29: „eingeführt" nach dem Verkleinern, Dosis-Sperre über die Übung umgehbar, zwei Kommentare

Vier kleine Funde der Nachschau von Sprint 29 (gelesen, nicht nachgestellt):

1. **`IntroducedCount` nach dem Verkleinern des Pools falsch** (`PositionProgressService.cs:182-183, 204`): die
   gruppierte Abfrage filtert nicht auf `ItemIndex < poolSize`, sie deckelt nur mit `Math.Min`. 60 Wörter eingeführt,
   der Vater senkt `ItemCount` auf 50, von den ersten 50 sind 30 eingeführt → das Kind liest 50/50, die Zeile „noch vor
   dir" verschwindet.
2. **Entscheidung 9 von B-04 über die Übung umgehbar**: geprüft wird nur beim Anlegen/Ändern der Position
   (`PlanPositionsController.cs:173, 253`). Stellt der Creator eine zugewiesene Liste später auf `ordered: false`
   (`ExerciseControllerBase.cs:472`), behält die Position ihre Dosis.
3. **Entscheidung 5 von B-04 nur halb**: `IntroducedCount` sieht nur das Kind (`SohnHome.tsx:141-143`), der Vater
   dieselbe Zahl nur im Positions-Report (`introducedItems`). Entscheidungstext oder „Heute"-Karte angleichen.
4. **Zwei Kommentare**: `VierWochenTests.cs:181` widerspricht seiner Zusicherung bei `:196`; der Blockkommentar in
   `PlanPositionsController.cs:87-111` steht seit `595b429` über den Dosis-Prüfungen statt über `TimeSlotProblem`.

## User Story

Als **Sohn** möchte ich, dass „so viele Wörter kenne ich schon" stimmt und dass eine Liste, deren Einträge ich in
beliebiger Reihenfolge nennen darf, mir keine richtige Antwort als falsch anrechnet; als **Vater** möchte ich
Kommentare lesen, die zum Code passen.

## Ist-Stand am Code

Wie oben, je Punkt mit `Datei:Zeile`; Pool = Indizes `[0, poolSize)` (`PositionPlayService.cs:168`, `:171`).
Die Dosis wird an vier Stellen gelesen: Runde (`PositionPlayService.DueItemIndicesAsync`), Review-Sperre
(`PositionPracticeController.cs:352`), Testportion und Einführung im Test (`PositionTestsController.cs:185`, `:220`).

## Offene Punkte

1. ~~Punkt 2: die Config-Änderung des Creators ablehnen oder die Dosis zur Spielzeit ignorieren?~~ → Entscheidung 2.
2. Punkt 3 (Zahl auch beim Vater auf der „Heute"-Karte) – **zurückgestellt**: das ist eine neue Anzeige, also ein
   Wunsch; er gehört in den Dialog, nicht in einen Defekt-Lauf. Dazu gehört: der Positions-Report des Vaters zählt
   „eingeführt" über alle Wörter (`PositionReportService.cs:59`), nicht über den Pool.

## Entscheidungen

1. **Gezählt wird je Position nur, was im Pool liegt** (`ItemIndex < poolSize`); die Abfrage bleibt eine für den
   ganzen Plan und bringt die Indizes mit. Kosten: Zeilen statt Gruppen aus der DB – eine Position hat höchstens
   ein paar hundert Wörter.
2. **Die Dosis wirkt zur Spielzeit nicht, solange die Übung als Menge wertet** (`PositionPlayService.DailyDose`, eine
   Stelle für alle vier Leser). Begründung: die Übung gehört dem Creator, der Plan dem Vater – den Creator an einer
   Position zu sperren, die er nicht sieht, wäre eine Sperre über Rollen hinweg. Das Ablehnen beim Zuweisen
   (B-04, 9) bleibt. Kosten: die gespeicherte Dosis bleibt stehen und wirkt wieder, wenn die Liste geordnet wird; die
   Plan-Seite zeigt sie weiter an. Damit der Vater die Position trotzdem bearbeiten kann, prüft das Ändern die Menge
   nur, wenn die Dosis selbst geändert wird.
3. **Kommentare**: `VierWochenTests.cs` beschreibt die drei gespielten Tage; die Dosis-Prüfungen stehen über dem
   Blockkommentar der Zeitfenster, nicht darunter.

## Akzeptanzkriterien

1. Sechs Wörter eingeführt (0, 1, 6–9), Pool auf 5 verkleinert → `introducedCount` 2, `poolSize` 5.
2. Eine geordnete Liste mit Dosis 2, danach vom Creator auf „beliebige Reihenfolge" gestellt: die Übungsrunde zeigt
   alle 6 Einträge, der Test fragt alle 6, und der Teststart führt nichts im Voraus ein.

## Schätzung

- **S**, `wo: backend`, keine Migration, kein Vertragsbruch. Testweg: `DosisResteTests`, volle Suite.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 29/30 im Backlog-Lauf, `pugling-reviewer`).
- **2026-11-12** — ausformuliert, gegrillt, geschätzt (Defekt, autonom); Punkt 3 zurückgestellt (Wunsch).
- **2026-11-12** — in-arbeit. Rote Proben (`DosisResteTests`): AK 1 erwartet 2, gemessen 5; AK 2 erwartet 6, gemessen 2.
  Fix: Zählung im Pool, `DailyDose` an allen vier Lesestellen; Kommentare berichtigt.
- **2026-11-12** — Mutanten (`1f537a5 + Arbeitsbaum`, 01:42), je Lesestelle der geteilten Bedingung eine Zeile:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Zählung ohne Pool-Filter | PositionProgressService.cs:205 | red | red |
  | Runde mit roher Dosis | PositionPlayService.cs:187 | red | red |
  | Review-Sperre mit roher Dosis | PositionPracticeController.cs:352 | green | green |
  | Testportion mit roher Dosis | PositionTestsController.cs:185 | red | red |
  | Einführung im Test mit roher Dosis | PositionTestsController.cs:220 | red | red |

  Die Review-Sperre ist **äquivalent**: ohne Dosis enthält die Runde alle Einträge, die Sperre kann nicht greifen –
  gemessen grün, erwartet grün, kein Beleg nötig. Grundlinie nach dem Zurückspielen grün.
- **2026-11-12** — `pugling-reviewer`: kein 🔴. 🟡 nach dem Umstellen auf „beliebige Reihenfolge" ließ sich die Position
  nicht mehr speichern – das Formular schickt die gespeicherte Dosis mit, und die Mengen-Prüfung schlug an. **Fund 2
  des Sprints 32 (zählt)**: beim Ändern prüft sie nur noch eine Dosis, die *jetzt* gesetzt wird (Entscheidung 2 um
  diese Kosten ergänzt). 🟡 die Probe zu AK 1 fing einen Grenzfehler `<=` nicht – **Fund 3 (zählt)**: Index 5 mit
  eingeführt. 🟢 die Review-Sperre ist nicht allgemein äquivalent, nur am ersten Tag (dann ist alles fällig) – die
  Zeile unten ist danach benannt. 🟢 der Positions-Report des Vaters zählt „eingeführt" über alle Wörter, nicht den
  Pool – gehört zum zurückgestellten Punkt 3.
- **2026-11-12** — Mutanten nach der letzten Änderung (`1f537a5 + Arbeitsbaum`, 01:58):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Zählung ohne Pool-Filter | PositionProgressService.cs:205 | red | red |
  | Grenze mitgezählt (`<=`) | PositionProgressService.cs:205 | red | red |
  | Runde mit roher Dosis | PositionPlayService.cs:187 | red | red |
  | Review-Sperre mit roher Dosis | PositionPracticeController.cs:352 | green | green |
  | Testportion mit roher Dosis | PositionTestsController.cs:185 | red | red |
  | Einführung im Test mit roher Dosis | PositionTestsController.cs:220 | red | red |
  | PATCH prüft die gespeicherte Dosis | PlanPositionsController.cs:257 | red | red |
  | PATCH prüft nie | PlanPositionsController.cs:257 | red | red |

  Review-Sperre: im Test ist am ersten Tag alles fällig, die Runde enthält alle Einträge – die Mutante ist dort nicht
  unterscheidbar (an späteren Tagen wiese sie ein eingeführtes, nicht fälliges Wort ab, wie jede Position ohne Dosis
  es annimmt). Grundlinie nach dem Zurückspielen grün.
- **2026-11-12** — **abgenommen.** Commit `d98e697`. Volle Suiten nach der letzten Änderung: Backend 1330/1330, Vitest 749/749, E2E 137 passed im zweiten Lauf (der erste: 136 passed, 1 failed in `creator-lehrwerk-weg.spec.ts` – berührt weder Dosis noch Assistent, einzeln und 3× parallel grün, Ursache nicht gemessen). Review-Zähler Sprint 32: 3 von 5. Rollengang: Sohn und Vater über die API (`DosisResteTests`, als Kind gespielt, als Vater bearbeitet); `ziel-vier-wochen.spec.ts` (Dosis-Weg im Browser) grün in der vollen Suite.
