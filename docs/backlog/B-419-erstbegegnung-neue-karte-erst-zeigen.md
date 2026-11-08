---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Erstbegegnung, Neue Karte erst zeigen, Kennenlernen je Karte]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-sohn
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-419 · Erstbegegnung: eine nie gesehene Karte wird in der Übungsrunde erst gezeigt, dann abgefragt

Im Rollengang vom 2026-11-08 bekam der Sohn in einem frisch angelegten Plan („Klassenarbeit vorbereiten",
Assistent-Standard) als erste Karte „la ville" mit einem leeren Antwortfeld – ein Wort, das er nie gesehen
hatte. Fünf Karten, fünf Fehlversuche, **0 Münzen**, danach der Test mit 20 %. Die App prüft neue Wörter,
bevor sie sie je gezeigt hat. Die Anzeigestufe „Beide zeigen (Kennenlernen)" existiert, hängt aber an der
**Position und am Tag**, nicht an der Karte – und der Assistent setzt für das Ziel „Klassenarbeit" ab Tag 1
die Tipp-Stufe.

## User Story

Als *Sohn* möchte ich ein neues Wort **einmal mit Lösung sehen**, bevor ich es tippen muss, damit ich beim
Üben lernen kann statt zu raten – und damit die erste Runde einer neuen Position nicht aus fünf roten
Toasts besteht.

## Ist-Stand am Code

- **Anzeigestufe ist eine Eigenschaft des Tages, nicht der Karte.** `PositionPracticeController.cs:309`
  (`var displayOnly = type.IsDisplayOnlyStage(stage)`) und `:117` (Karten-Projektion reicht
  `type.IsDisplayOnlyStage(stage)` als `DisplayOnly` durch). `stage` kommt aus
  `PositionPlayService.StageForDay` (`PositionPlayService.cs:56-64`): `StageSchedule` → `Position.Stage` →
  `Exercise.DefaultStage` → Typ-Default (`ExerciseTypeBase.cs:32`, SelfAssess).
- **Der Assistent setzt die Tipp-Stufe ab Tag 1.** `VaterWizard.tsx:46` (`Klassenarbeit: … stage: 4, typed:
  true`), `:358` schreibt `stage: defaultStage` in jede Position. Einen `StageSchedule` schreibt der
  Assistent nicht (kein Treffer für `stageSchedule` in `frontend/src/vater/VaterWizard.tsx`).
- **Der Server kennt die Erstbegegnung schon.** `PositionPracticeController.cs:329-333`: beim ersten Kontakt
  wird `IntroducedAt`/`DueOn` gesetzt. `:340`: eine Anzeigestufe zählt als geübt, aber nie als Treffer
  (`scored = … && !displayOnly`) – „Kennenlernen darf nicht der billigste Weg zu Münzen sein".
- **Der Vertrag trägt das Feld.** `PracticeDtos.cs:73` (`bool DisplayOnly = false` an der Karte); das
  Frontend rendert Anzeigekarten bereits (`SohnPractice.tsx:343-344` zeigt `card.reveal`, `:380` der
  Knopf „Weiter" statt Prüfen; `reviewFeedback` liefert für `displayOnly` `"none"`, `:38`).
- **Bewertung ist exakt.** `AnswerGrader.cs:14-15` vergleicht nach `StageMechanics.Normalize`
  (`StageMechanics.cs:31-32`: Trim, Kleinschreibung, Leerzeichen) – „Stadt" ≠ „die Stadt" (→ [B-421](B-421-antwort-toleranz-artikel.md)).
- **Gemessen im Rollengang** (Wegwerf-DB, Plan 2, Position 17, Stufe 4): Runde 1 = 5 Karten `Neue Aufgabe`,
  je leeres Feld, Lösung nur als 1,6-s-Toast (→ [B-420](B-420-loesung-blitzt-als-toast.md)), Rundenende
  „+0 🪙 · 5 Karten"; Test danach 1/5 (20 %), Bestehensgrenze 80 %.

## Die echte Lücke

Nicht die Stufenmechanik fehlt, sondern die **Regel „erster Kontakt = Anzeigekarte"** auf Kartenebene. Alles
darunter ist da: das Feld im Vertrag, die Anzeige im Frontend, die Nicht-Wertung im Server, `IntroducedAt`.
Was fehlt, ist eine Zeile Entscheidung im Server beim Bauen der Karte und ein Rundenende, das sagt, dass
jetzt abgefragt wird.

## Offene Punkte

1. ~~Position-Stufe oder Karte?~~ → Entscheidung 1.
2. ~~Doppelt in derselben Runde (zeigen, dann fragen) oder zwei Runden?~~ → Entscheidung 2.
3. ~~Was ist mit dem Test?~~ → Entscheidung 3.
4. Darf der Kennenlern-Tag am Tagesziel vorbei ohne Malus enden? → **zurückgestellt**, eigene Frage
   [B-432](B-432-erster-tag-einer-position-ohne-malus.md); diese Story ändert an Ziel und Malus nichts.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Die Erstbegegnung ist eine Eigenschaft der Karte, nicht der Position.** Eine Karte, deren Item noch keinen
   `PositionItemProgress` hat **oder** dessen `IntroducedAt` leer ist, wird in der **Übungsrunde** als
   `DisplayOnly` ausgespielt – unabhängig von der Stufe des Tages. Begründung: so wirkt die Regel in jedem
   Plan, auch in den schon angelegten, ohne dass ein Vater eine Stufenleiter versteht; `StageForDay` bleibt
   unverändert für alle schon eingeführten Karten. Kosten: eine Bedingung in der Karten-Projektion
   (`PositionPracticeController.cs:117` und `:309`) braucht den Fortschritt des Items – er wird für die
   Fälligkeit ohnehin geladen (`PositionPlayService` „never seen counts as due").
2. **Zeigen und Fragen sind zwei Runden, nicht eine.** Die Runde mit Anzeigekarten endet wie heute; weil der
   Server beim ersten Kontakt `DueOn = session.Day` setzt (`:332`), sind dieselben Items sofort wieder fällig,
   und „ÜBEN (n fällig)" führt in die Abfrage-Runde, die wie bisher Punkte zahlt (`NewContentPoints` beim
   ersten gewerteten Review, `ScoringService.cs:112-113`). Begründung: die Sitzungs-`Order` ist eingefroren
   und je Item eindeutig (`cards/{itemIndex}`), ein Doppel-Eintrag bräche den Kartenschlüssel. Kosten: am
   ersten Tag einer Position zwei Runden statt einer; das ist der Preis für „erst lesen, dann prüfen".
3. **Der Test bleibt unberührt.** Er prüft nach der Einführung und ist nach Runde 1 sinnvoll; `StageNotTestable`
   (`PositionTestsController.cs:169`) greift weiter nur bei einer Anzeigestufe des Tages. Kosten: keine.
4. **Das Rundenende benennt die Einführung.** Statt „+0 🪙 · 5 Karten" sagt der Abschluss „5 neue Wörter
   kennengelernt" und bietet „Jetzt abfragen" (neue Übungsrunde) vor „Weiter zum Test" an. Dafür zählt der
   Server im Ende-Response die eingeführten Karten (additives Feld, kein Vertragsbruch). Kosten: ein Feld im
   End-DTO, ein Zweig im Ende-Bildschirm.
5. **Keine neue Stufe, kein neues Positionsfeld, keine Migration.** Die Anzeigestufe `ShowBoth` bleibt als
   Positions-Stufe erhalten (für Vokabellisten zum reinen Durchlesen); diese Story fügt ihr nur den
   Kartenfall hinzu. Kosten: zwei Wege zu `DisplayOnly`, dokumentiert am Feld.

## Akzeptanzkriterien

1. Eine Position (Vokabelkarten, Stufe 4) mit fünf nie gespielten Items: die erste Übungsrunde liefert fünf
   Karten mit `displayOnly: true` und `reveal` gesetzt; das Review dieser Karten bucht **0** Punkte, setzt
   `IntroducedAt` und `DueOn = heute`.
2. Die zweite Übungsrunde am selben Tag liefert dieselben Items mit `displayOnly: false`; ein richtiger
   Treffer bucht `NewContentPoints` (Standard 10) – der erste gewertete Review zählt als „neu".
3. Ein Item mit gesetztem `IntroducedAt` wird auf einer Tipp-Stufe **nie** als Anzeigekarte ausgespielt
   (Regression: bestehende Pläne spielen wie heute).
4. Auf einer Positions-Stufe `ShowBoth` ändert sich nichts (alle Karten `displayOnly`, kein Test).
5. Das Rundenende nennt die Zahl der eingeführten Karten („5 neue Wörter kennengelernt") und bietet „Jetzt
   abfragen" an; nach einer Runde ohne Einführung bleibt der heutige Bildschirm.
6. E2E: neuer Plan per Assistent → erste Karte zeigt „→ die Stadt" statt eines leeren Antwortfelds; zweite
   Runde fragt „la ville" mit Antwortfeld.

## Schätzung

**Größe: M** — Backend: eine Bedingung an zwei Stellen der Karten-Projektion plus ein additives Zählfeld im
Ende-Response; Frontend: ein Zweig im Ende-Bildschirm mit neuem Knopf; dazu Integrationstests, Vitest und eine
E2E-Spec. Kein Schema, kein Vertragsbruch (additiv).

**Risiken:** (1) Der Set-Modus (ungeordnete Listen, `setMode`) hat kein Item-je-Karte – dort greift die Regel
nicht (nur Typen mit `PositionItemProgress` je Karte; `displayOnly` wird dort wie heute aus der Stufe
abgeleitet). (2) Der Reihenfolge-Zusatz „Neue bevorzugt" (`OrderStrategy`) mischt eingeführte und neue Karten –
die Regel gilt je Karte, nicht je Runde, also kein Konflikt. (3) `alreadyScoredToday` (`:339`) darf die zweite
Runde nicht blockieren: ein Anzeige-Review setzt `LastReviewedAt` **nicht** – prüfen und testen.

**Angriffsplan (Backend zuerst):**

1. `PositionPracticeController`: Helfer `IsIntroduction(prog)` (kein Fortschritt oder `IntroducedAt == null`);
   in der Karten-Projektion (`:117`) und im Review (`:309`) `displayOnly = typStufe || IsIntroduction`. Nur
   für Typen, deren Karte ein Item mit eigenem Fortschritt trägt.
2. Ende-Response: additives Feld `IntroducedCount` (Reviews der Sitzung mit `DisplayOnly`-Karte zählen).
3. Integrationstests in `PositionPracticeFlowTests`: AK 1–4 je als Fall; Mutante je Fall (Bedingung
   entfernen → AK 1 rot; `IntroducedAt` nicht setzen → AK 2 rot).
4. OpenAPI/Client nachziehen (`gen:contract`), `Pugling.Client` einzeilig.
5. Frontend `SohnPractice.tsx`: Ende-Bildschirm liest `introducedCount`, zeigt die Zeile und „Jetzt abfragen"
   (startet eine neue Sitzung, derselbe Weg wie der Üben-Knopf der Basis); i18n de/en.
6. Vitest `SohnPractice.test.tsx` (Ende-Zweig), E2E `erstbegegnung.spec.ts` (AK 6).

**Testweg:** `PositionPracticeFlowTests` (Integration), `SohnPractice.test.tsx` (Vitest), neue Spec
`frontend/e2e/erstbegegnung.spec.ts` als Rollengang Sohn; `.claude/scripts/mutant-probe.py` nach dem letzten Edit.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang aller drei Rollen an der laufenden App
  ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)); Ist-Stand am Code belegt, Entscheidungen vom PM unter
  dem Auftrag „nachtlauffähig machen" getroffen (Vetorecht beim Nutzer), geschätzt. Bewusst **nicht** mit B-420
  und B-421 zusammengelegt: drei Ursachen desselben Erlebnisses, drei getrennte Reparaturen.
