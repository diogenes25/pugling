---
tags: [typ/story, status/abgenommen, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Erstbegegnung, Neue Karte erst zeigen, Kennenlernen je Karte]
status: abgenommen
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
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-08** — Lauf AE, Sprint 22 ([Protokoll](../pm-sitzung-2026-11-08-nacht.md)), Commit `652e5d3`. Gebaut nach
  Angriffsplan, mit zwei benannten Abweichungen:
  - **Entscheidung 4, Mechanik (nicht Produkt):** der Server zählt die eingeführten Karten nicht im End-Response, sondern
    markiert jede Karte (`PracticeCard.Introduction`); das Frontend zählt die markierten Karten der Runde. Ein exakter
    Zähler am `/end` bräuchte ein gespeichertes Merkmal je Review (Migration, von Entscheidung 5 ausgeschlossen) oder
    eine Heuristik (Zeitfenster, `ReviewCount`), die bei Positionen ohne Leitner die zweite Runde mitzählte – vom
    `pugling-reviewer` unabhängig bestätigt. Schwäche: eine nach Neuladen fortgesetzte Runde zählt nur die gesehenen
    Karten. Der Bildschirm (Zahl + „Jetzt abfragen") ist wie entschieden.
  - **AK 6, Weg:** die Spec legt den Plan über die API an, mit Stufe 4 (genau die Stufe, die der Assistent für
    „Klassenarbeit" ab Tag 1 schreibt) statt durch den Assistenten zu klicken; Gegenstand ist das Spielen, der
    Assistent hat seine eigenen Specs.
  - **Zusätzlich aus dem Review:** Einführungen stehen in einer gemischten Runde vorn (`IntroductionsFirstAsync`),
    sonst bräche jede Kennenlernkarte zwischen zwei Abfragen die Combo; die Folgekarte nach einer Antwort außer der Reihe
    nutzt den eben eingeführten Stand; „anderes Bild" folgt der Stufe der Karte.
- **2026-11-08** — Verifikation: Backend 1135/1135, Vitest 683/683, `tsc -b` sauber, Playwright **120 passed** (volle
  Suite, nach dem letzten Edit). Fälle → Tests:
  - AK 1 (neu, Tippstufe → Kennenlernkarte mit Lösung, 0 Punkte, `IntroducedAt`/`DueOn` heute, `LastReviewedAt` leer)
    → `ErstbegegnungTests.NieGesehenesWort_KommtAufTippstufe_AlsKennenlernkarte_MitLoesung_UndOhnePunkte`, je Weg
    `/cards`, `/next`, Folgekarte im `/review`.
  - AK 2 (zweite Runde fragt, `NewContentPoints` = 10) → `ZweiteRunde_FragtDieEingefuehrtenWoerter_Ab_UndZahltNeuePunkte`.
  - AK 3 (eingeführt → wie bisher) → `EingefuehrtesWort_KommtAufTippstufe_WieBisher`; dazu die 35 Bestandstests, die
    jetzt mit eingeführten Atomen laufen.
  - AK 4 (Anzeigestufe des Tages unverändert) → `AnzeigestufeDesTages_BleibtUnveraendert_UndIstKeineEinfuehrung`.
  - Info-Modus / Typ ohne Anzeigestufe → `InfoModus_KenntKeineEinfuehrung`, `TypOhneAnzeigestufe_KenntKeineEinfuehrung`.
  - Bild-Wechsel auf einer Kennenlernkarte → `MediaSelectionTests.AnderesBild_AufKennenlernkarte_EinerTippstufe_GehtWeiter`.
  - Gemischte Runde / Antwort außer der Reihe → `GemischteRunde_…`, `AntwortAusserDerReihe_…`.
  - AK 5 (Rundenende) → `SohnPractice.test.tsx` „nennt die neuen Wörter und startet mit Jetzt abfragen eine neue Runde"
    (inkl. alte Sitzung genau einmal geschlossen, mit Rest-Sekunden), „behält nach einer Kennenlern-Stufe des Tages den
    heutigen Bildschirm", „zeigt in einer gemischten Runde … auch die verdienten Münzen".
  - AK 6 → `frontend/e2e/erstbegegnung.spec.ts` (Rollengang Sohn, frischer Plan).
- **2026-11-08** — Mutanten mit `.claude/scripts/mutant-probe.py` nach dem letzten Edit des jeweiligen Stands, alle rot am
  benannten Test, Grundlinie nach dem Zurückspielen grün:
  | Mutante | Stelle | gemessen |
  |---|---|---|
  | M1 Info-Modus nicht ausgenommen | `PositionPlayService.cs:82` | rot |
  | M2 „schon eingeführt" ignoriert | `PositionPlayService.cs:82` | rot |
  | M3 Anzeigestufe des Tages ignoriert | `PositionPlayService.cs:82` | rot |
  | M4 Typ ohne Anzeigestufe | `PositionPlayService.cs:83` | rot |
  | M5 Review wertet jede Karte als Einführung | `PositionPracticeController.cs` (Review) | rot |
  | M6 `/next` ohne Einführung | `PositionPracticeController.cs` (Next) | rot |
  | M7 Bild-Wechsel mit Tagesstufe | `PositionPracticeController.cs` (ReshuffleImage) | rot |
  | M8 Folgekarte im Review ohne Einführung | `PositionPracticeController.cs` (Review) | rot |
  | R1 Einführungen nicht vorn | `PositionPracticeController.cs:99` | rot |
  | R2 Folgekarte mit altem Stand | `PositionPracticeController.cs:469` | rot (erste Fassung `if (false)` kompilierte nicht → ersetzt, nicht gezählt) |
  | F4 Ende zählt `displayOnly` statt `introduction` | `SohnPractice.tsx` | rot |
  | F5 „Jetzt abfragen" ohne neue Runde | `SohnPractice.tsx` | rot |
  | R3 Uhr schon beim Klick zurückgesetzt | `SohnPractice.tsx:166` | rot (erste Fassung „Reset entfernt" war grün und maß die falsche Regression → ersetzt) |
  | R4 Runde nicht in den Effekt-Abhängigkeiten | `SohnPractice.tsx:143` | rot |
  | R6 gemischte Runde ohne Münzen | `SohnPractice.tsx:293` | rot |
- **2026-11-08** — Review: `pugling-reviewer` keine 🔴; 🟡 Combo-Bruch in gemischten Runden → behoben (R1); 🟡 Abweichung
  von Entscheidung 4 nicht notiert → oben notiert (Doku, ohne Zählung); 🟢 Folgekarte außer der Reihe → behoben (R2);
  🟢 Lesbarkeit `IntroductionStage` → behoben; 🟢 die Item-Historie bucht die Kennenlernkarte als „nicht gewusst"
  (`countsForMastery: false`, wie seit B-96 auf der Anzeigestufe) → **bewusst hingenommen**; 🟢 Vater-Runde verbraucht die
  Einführung → [B-433](B-433-vorschau-des-vaters-verbraucht-die-einfuehrung.md); 🟢 API-Beispiel zeigt jetzt die
  Kennenlernkarte mit `awarded: 0` → hingenommen (es ist das echte Verhalten einer frischen Position). `frontend-reviewer`:
  keine 🔴; 🟡 Rest-Sekunden der alten Sitzung (R3), 🟡 Münzen einer gemischten Runde (R6), 🟡 Rundenneustart ungetestet
  (R4) → behoben. Beim Bau aufgefallen: Umfang „nur neue" fragt nach der Einführung nie → [B-435](B-435-umfang-nur-neue-fragt-nach-der-einfuehrung-nie.md).
- **2026-11-08** — Rollengang: Sohn als E2E `erstbegegnung.spec.ts` mit **frischem** Plan (Kennenlernen → „3 neue Wörter
  kennengelernt" → Abfrage mit Antwortfeld) und `full-flow.spec.ts` (Vater legt per UI an, Sohn lernt kennen, fragt ab,
  Combo ×5, Test); Vater und Creator als Regressionszeugen (volle Suite grün, kein Weg ihrer Fläche im Diff). → `abgenommen`.
