---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/student]
aliases: [Weiter zum Test auf ShowBoth, Rest von B-114 im Übungsbildschirm]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: Browser-Rollengang von B-114 (nachträglich, 2026-08-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-114]
wartet_auf: ""
nachgeschaut: ""
---

# B-117 · Nach der Übungsrunde bietet der Bildschirm einen Test an, den es für diese Stufe nicht gibt

B-114 hat die Tageskarte (`SohnHome.tsx`) korrekt auf `testable` umgestellt: eine `ShowBoth`-Position ohne
Leitner zeigt dort nur „DURCHSPIELEN", keinen TEST-Knopf. Der **Übungsbildschirm selbst**
(`SohnPractice.tsx`) kennt `testable` aber gar nicht und bietet nach der Runde weiterhin einen Test-Knopf
an — derselbe Fehlerklasse wie B-114, nur an einer anderen Stelle, die deren Rollengang nicht abgedeckt hat.

## User Story

Als Sohn möchte ich nach einer durchgespielten Kennenlern-Runde nicht in einen Knopf laufen, der eine
Fehlerseite zeigt, damit sich das Spiel nicht wie kaputt anfühlt.

## Ist-Stand am Code

- `frontend/src/sohn/SohnPractice.tsx:197` — Phase `"done"` („RUNDE FERTIG!"): Knopf „🎯 Weiter zum Test"
  navigiert immer zu `/sohn/test/${positionId}`, unabhängig von der Stufe.
- `frontend/src/sohn/SohnPractice.tsx:184` — Phase `"empty"` („Nichts fällig 🎉"): derselbe Knopf als
  „🎯 Zum Test", ebenfalls unbedingt.
- Klick führt zu `PositionTestsController`, das mit `400 stage_not_testable` antwortet
  (`ApiErrors.StageNotTestable`); das Frontend zeigt dabei den rohen, unübersetzten `detail`-Text direkt
  im Bild („This stage is a free display stage and cannot be tested.") — kein eigener Fehlerzustand.
- `SessionResponse` (`backend/Pugling.Contracts/Student/PracticeDtos.cs:7`) trug bislang kein Feld, an dem
  die Komponente die Testbarkeit der laufenden Sitzung hätte ablesen können; `PracticeCard.DisplayOnly`
  existiert nur, solange mindestens eine Karte geladen ist — in der `"empty"`-Phase ist `cards` leer.
- Gefunden im **Browser-Rollengang** von B-114 (Chrome-Extension war während des Nachtlaufs nicht
  verbunden, nachträglich verfügbar geworden): Tageskarte korrekt, aber nach dem Durchspielen der Runde
  erschien der Test-Knopf trotzdem — Klick bestätigte die Fehlerseite live.

## Die echte Lücke

`testable` war bisher nur eine Eigenschaft der **Tagesübersicht** (`PositionStatus`), nicht der
**laufenden Sitzung** selbst. Jede Stelle, die serverseitig weiß, ob eine Stufe heute prüfbar ist, muss das
auch dem Client mitgeben, der einen Test-Knopf zeigen könnte — B-114 hat das nur für eine von zwei Stellen
getan.

## Entscheidungen

1. **`SessionResponse` bekommt ein `Testable`-Feld**, serverseitig aus derselben Regel wie
   `PositionStatus.Testable` (jetzt als gemeinsamer Helfer `PositionProgressService.IsTestable(pos, plan,
   day)`, vorher nur inline in `ComputeDayAsync`). Begründung: die Sitzung ist der einzige Ort, der in
   beiden betroffenen Phasen (`done`, `empty`) sicher vorhanden ist — `cards` ist in `empty` leer.
   **Kosten:** `SessionResponse` wächst um ein Pflichtfeld (kein Vertragsbruch, nur eine Erweiterung);
   `PositionPracticeController.GetSession` lädt neu `PlanPosition`+`Exercise`+`StudyPlan` mit.
2. **Beide Knöpfe (`done`, `empty`) gaten auf `session.testable`.** Ist er `false`, verschwindet der
   Test-Knopf ersatzlos — es gibt für eine Anzeigestufe keinen sinnvollen Ersatzweg, „Zur Basis" bleibt.
   **Kosten:** keine.

## Akzeptanzkriterien

1. `POST .../practice-sessions` und `GET .../practice-sessions/{id}` liefern `testable: false` für eine
   `ShowBoth`-Position ohne Leitner, `true` für eine getippte Stufe.
2. Nach einer Runde auf einer solchen Position zeigt der Bildschirm keinen Test-Knopf.
3. Bei „Nichts fällig" auf einer solchen Position zeigt der Bildschirm ebenfalls keinen Test-Knopf.

## Schätzung

**Größe: S** — ein Contract-Feld, ein neuer Service-Helfer (Refactor einer bestehenden Inline-Formel),
zwei EF-Includes, zwei Frontend-Bedingungen. Kein Vertragsbruch, keine Migration.

**Angriffsplan** (Backend zuerst): `PositionProgressService.IsTestable` extrahieren und in
`ComputeDayAsync` wiederverwenden → `SessionResponse.Testable` ergänzen → `PositionPracticeController.Map`
non-static machen und darüber berechnen, `GetSession`/`Start` mit der nötigen Navigation versorgen →
Frontend: `SohnPractice.tsx` beide Knöpfe auf `session.current?.testable` gaten.

**Testweg:** `PositionPracticeFlowTests.cs` — rote Probe zuerst (zwei neue Fälle, `testable` fehlte am
Vertrag komplett → `KeyNotFoundException`), dann grün: `ShowBoth_PracticeSessionTraegtTestableFalse`
(inkl. Re-Read derselben Sitzung) und die Gegenprobe `GetippteStufe_PracticeSessionTraegtTestableTrue`.
Frontend: manueller Rollengang im Browser (kein Komponententest nachgebaut — die Bedingung selbst ist
trivial, das Risiko lag im fehlenden Server-Feld).

## Verlauf

- **2026-08-05** — gefunden beim nachträglichen Browser-Rollengang von B-114 (die Chrome-Extension war
  während des vorangehenden Nachtlaufs nicht verbunden). Sofort ausformuliert, gegrillt und geschätzt im
  selben Zug — auf ausdrücklichen Wunsch des Nutzers direkt gebaut, keine separate Grill-Sitzung.
- **2026-08-05** — **gebaut wie geplant.** Rote Probe zuerst: beide neuen Tests scheiterten gegen den
  Vorzustand mit `KeyNotFoundException` auf `testable` (das Feld existierte am Vertrag schlicht nicht),
  grün nach dem Fix. Backend: `PositionProgressService.IsTestable` extrahiert und in `ComputeDayAsync`
  wiederverwendet (keine Verhaltensänderung dort, nur Entdopplung); `SessionResponse.Testable` ergänzt;
  `PositionPracticeController.Map` non-static, `GetSession` lädt `PlanPosition.Exercise`/`.StudyPlan` mit,
  `Start` hängt die schon geladene Position direkt an. **734/734** Backend grün.
