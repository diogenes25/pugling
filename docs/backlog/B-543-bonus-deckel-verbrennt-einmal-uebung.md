---
tags: [typ/story, status/geschaetzt, bereich/gamification, rolle/student]
aliases: [Bonus-Deckel verbrennt Einmal-Übung]
status: geschaetzt
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachschau zu B-533 in der Retro von Sprint 69 (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-533]
wartet_auf: ""
---

# B-543 · Nach erreichtem Bonus-Deckel verbrennt eine Einmal-Übung für 0 Münzen

Das Bonus-Training ([B-533](B-533-bonus-training-extra-muenzen.md)) deckelt die Münzen je Tag. Der Deckel wirkt aber
erst bei der Abgabe: `ExtraTrainingService.StartAsync` prüft ihn nicht (`ExtraTrainingService.cs:78-93`),
`SubmitAsync` kappt die Münzen auf den Rest und setzt die Runde trotzdem auf `CompletedAt` (`:144-153`), und damit
gilt die Übung dauerhaft als erledigt (`:256`). Die Oberfläche lässt sie weiter starten (`SohnBonus.tsx:103`, `:124`)
und verspricht dabei „Tagesdeckel erreicht – … Münzen gibt es morgen wieder“ (`de.ts:160`).

**Szenario:** 30/30 Münzen erreicht, das Kind spielt eine Einmal-Übung im Wert von 4 Münzen → 0 Münzen, die Übung ist
für immer weg. Bei 1 Münze Rest zahlt sie 1 statt 4. Bei `BonusDailyCoinCap = 0` verbrennt jede Einmal-Übung. Kein
Betrug, aber unfair, und gegen B-533 Entscheidung 4 („eine Übung zahlt einmal“). Kein Test deckt den Fall.

**Randfall (geringe Wirkung, gleiche Stelle):** Gewertet wird gegen die *aktuellen* Items (`:110`, `:137`), nicht
gegen die beim Start festgehaltenen; das gespeicherte `ItemCount` wird nirgends benutzt. Ändert der Autor die Übung
zwischen Start und Abgabe, zählen neue Items als falsch.

## User Story

Als **Sohn** möchte ich, dass eine Bonus-Übung, die nur einmal zahlt, nicht an einem Tag verbraucht wird, an dem mein
Münz-Deckel schon voll ist, **damit** ich ihre Münzen morgen wirklich bekomme – und als **Vater** möchte ich, dass
mein Deckel dabei ein Deckel bleibt.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, Stand nach Sprint 69, `afadf18d`):

- **Start ohne Deckel-Prüfung:** `StartAsync` (`backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:63-111`)
  prüft Sperre (`:66-69`), Pool (`:71-73`) und `Done` (`:80-82`), nie `CoinsTodayAsync` gegen den Deckel. Eine offene
  Runde wird fortgesetzt (`:75-77`).
- **Abgabe kappt und verbraucht:** `SubmitAsync` wertet (`:137-140`), rechnet `left = max(0, cap - coinsBefore)` und
  `coins = min(earned, left)` (`:142-146`) und setzt danach **immer** `CompletedAt` (`:150-153`). Die Antwort trägt die
  Lösungen je Item (`ExtraItemOutcome … Expected`, `:175-177`).
- **Verbraucht heißt für immer:** `finished = !repeatable && (done > 0 || open…) && !open…` (`:256`); eine Übung ist
  wiederholbar genau dann, wenn sie vom Typ Vocabulary ist (`:247`).
- **Deckel:** `CapOf(child.BonusDailyCoinCap) ?? opts.DailyCoinCap` (`:202`); `0` heißt „zahlt nichts“ (B-533
  Entscheidung 3).
- **Fehlercodes:** `ExtraTrainingLocked`, `ExtraExerciseDone`, `ExtraRunCompleted` in
  `backend/Pugling.Api/Errors/ApiErrors.cs:211-215`; deutsche Texte in `frontend/src/lib/api.ts:90-93`
  (`GERMAN_PROBLEM_TEXT`) und `frontend/src/i18n/de.ts:407-408` / `en.ts:391-392` (`sohn.problem.*`);
  `arcadeSpielweg.test.tsx:134-170` verlangt für jeden Code einen deutschen Satz.
- **Oberfläche:** `capReached` wird aus der Übersicht gerechnet (`frontend/src/sohn/SohnBonus.tsx:72`), die Karte
  „Tagesdeckel erreicht – üben darfst du weiter, Münzen gibt es morgen wieder.“ steht darüber (`:103`, Text
  `de.ts:160`), und jede nicht erledigte Zeile bietet „Spielen“/„Weiterspielen“ (`:125-130`). Die Lauf-Seite
  `SohnBonusRun` startet beim Einhängen (`:171`), zeigt einen Fehler in einer fokussierten Box (`:163`) und hält die
  gegebenen Antworten je Runde lokal (`loadAnswers`, `:174`); sind alle da, gibt sie sofort ab (`:175`), gelöscht
  werden sie erst nach einer erfolgreichen Abgabe (`:189`).
- **Tests:** `ExtraTrainingTests.cs:181-220` (`Der_Tagesdeckel_Kappt_Die_Bonus_Muenzen`) prüft den **Teil**-Deckel an
  einer Einmal-Übung (26/30, 6 verdient → 4 gebucht); der volle Deckel und `0` sind ungetestet. `TestClock.Advance`
  (`backend/Pugling.Api.Tests/TestClock.cs:37`) erlaubt den Tageswechsel im Test.
- **Randfall:** `BuildRun` und `preview.CheckAsync` lesen die Items der Übung **jetzt** (`:110`, `:137`);
  `ExtraTrainingRun.ItemCount` wird geschrieben (`:92`), aber nirgends gelesen.

## Die echte Lücke

Der Deckel ist ein Schloss am Ausgang statt am Eingang: er hält die Münzen auf, aber nicht den Verbrauch. Für eine
Vokabelübung ist das egal (sie darf wieder), für eine Einmal-Übung heißt „Münzen gibt es morgen wieder“ das Gegenteil
dessen, was passiert.

## Offene Punkte

1. ~~Wo greift die Sperre: beim Start, bei der Abgabe oder bei beiden?~~ → Entscheidung 1 und 2.
2. ~~Was geschieht beim **Teil**-Deckel (Rest 1, Übung wert 4)?~~ → Entscheidung 3.
3. ~~Wie sagt die Oberfläche es?~~ → Entscheidung 4.
4. ~~Gilt das auch für Vokabelübungen?~~ → Entscheidung 5.
5. ~~Randfall „gewertet gegen aktuelle Items“: mitnehmen?~~ → Entscheidung 6 (zurückgestellt).

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
Unverändert gelten die ausdrücklichen Nutzer-Entscheidungen aus B-533: der Deckel bleibt (E3), eine Einmal-Übung
zahlt einmal und schon der Start verbraucht sie (E4), gewertet wird am Ende der Runde (E8).

1. **Eine Einmal-Übung startet nicht bei erreichtem Deckel.** Ist der Tagesdeckel voll (`coinsToday >= cap`, auch
   `cap = 0`), antwortet `POST …/extra-training/{exerciseId}/runs` für eine nicht wiederholbare Übung
   `409 extra_cap_reached` – **auch** für eine schon offene Runde (sonst spielte das Kind sie zu Ende und liefe erst bei
   der Abgabe gegen die Wand). `extra_exercise_done` geht vor (endgültig schlägt vorläufig). Die Meldung nennt das
   Schloss und den Weg hinaus: *„Today's bonus coin cap is reached. This exercise pays only once – play it tomorrow,
   when coins come back.“* *Begründung:* B-533 E4 sagt „der Start verbraucht sie“ – also muss die Sperre vor dem Start
   sitzen. *Kosten:* ein neuer Code samt drei Texten (`api.ts`, `de.ts`, `en.ts`); kein Vertragsbruch (Codes sind
   additiv, `contract.ts` wird neu erzeugt).
2. **Wird der Deckel zwischen Start und Abgabe voll, wird die Runde nicht verbraucht.** `SubmitAsync` antwortet für
   eine Einmal-Runde bei `left == 0` ebenfalls `409 extra_cap_reached` – **vor** dem Werten: die Runde bleibt offen,
   nichts wird gebucht, und die Antwort trägt **keine** Lösungen. *Begründung:* würde gewertet und die Lösung
   gezeigt, ohne zu verbrauchen, könnte das Kind morgen dieselben Fragen mit bekannter Lösung für volle Münzen
   abgeben – das widerspräche B-533 E8. Die Lauf-Seite behält die Antworten lokal (`SohnBonus.tsx:174-175`, gelöscht
   nur nach Erfolg), morgen setzt „Weiterspielen“ die Runde fort und gibt sie ab. *Kosten:* die Arbeit von heute zahlt
   erst morgen; geht der lokale Speicher verloren, spielt das Kind die Runde morgen neu.
3. **Teil-Deckel bleibt, wie er ist: es zahlt der Rest, und die Übung ist verbraucht.** *Begründung:* der Deckel ist
   die ausdrückliche Grenze des Vaters (B-533 E3); das Kind sieht vor dem Start „Heute verdient: x / y“
   (`SohnBonus.tsx:80-84`) und entscheidet selbst. Eine Einmal-Übung ganz abzuweisen, sobald sie mehr wert ist als der
   Rest, machte große Übungen (z. B. 20 Wörter getippt = 40 Münzen bei Deckel 30) **nie** spielbar. Der bestehende Test
   `Der_Tagesdeckel_Kappt_Die_Bonus_Muenzen` hält genau diesen Fall und bleibt grün. *Kosten:* bei Rest 1 zahlt eine
   4er-Übung 1 – die Story löst den 0-Münzen-Fall, nicht jeden Verlust.
4. **Die Oberfläche bietet den Start nicht an und sagt es richtig.** Bei erreichtem Deckel zeigt eine nicht
   wiederholbare, nicht erledigte Zeile statt „Spielen“/„Weiterspielen“ den Satz „Zahlt nur einmal – morgen wieder“
   (kein Link); der Text der Deckel-Karte wird ehrlich: *„Tagesdeckel erreicht – Vokabeln üben darfst du weiter, ohne
   Münzen. Übungen, die nur einmal zahlen, warten bis morgen.“* Gerechnet wird aus der Übersicht (`capReached`,
   `e.repeatable`, `e.done`), der Vertrag bleibt. *Begründung:* API-First – der Server erzwingt, die Oberfläche
   verschweigt keinen Knopf, der nur zu einem 409 führte (Muster „gesperrter Knopf nennt den Grund“). *Kosten:* zwei
   i18n-Schlüssel je Sprache.
5. **Vokabelübungen sind nicht betroffen.** Sie dürfen bei vollem Deckel weiter starten und zahlen 0; ihr Durchgang
   zählt trotzdem (die Form wird schwerer). *Begründung:* B-533 E4 – „Tippen aus dem Gedächtnis darf immer wieder
   Münzen bringen“, also verbrennt dort nichts; Üben ohne Münzen ist ausdrücklich erlaubt. *Kosten:* keine.
6. **Randfall (Wertung gegen aktuelle Items) wird zurückgestellt.** *Begründung:* er ist unabhängig vom Deckel, trifft
   nur eine vom Autor zwischen Start und Abgabe geänderte Übung, und eine echte Lösung braucht einen Item-Schnappschuss
   an der Runde – eine Migration, die diese S-Story sprengte; ein Vergleich über `ItemCount` erkennte eine Änderung bei
   gleicher Anzahl nicht. *Kosten:* Entscheidung 2 verlängert das Fenster (eine Runde kann über Nacht offen bleiben),
   die Wirkung bleibt gering. Beim Bau als eigene Story anlegen (README → „Ein Fund beim Bauen wird eine eigene
   Story“), Verweis hier im `## Verlauf`.

## Akzeptanzkriterien

1. Bei 30/30 antwortet der Start einer Einmal-Übung `409 extra_cap_reached`; die Übersicht führt sie danach weiter
   als nicht erledigt (`done: false`). *Rot, wenn* die Prüfung in `StartAsync` fehlt (dann `200`).
2. Bei Kind-Deckel `0` startet keine Einmal-Übung (`409 extra_cap_reached`). *Rot, wenn* die Prüfung den Kind-Deckel
   übergeht und nur die Vorgabe liest, oder `>` statt `>=` vergleicht.
3. Eine offene Einmal-Runde wird bei vollem Deckel nicht fortgesetzt (`409 extra_cap_reached`). *Rot, wenn* die
   Prüfung nur vor dem Anlegen einer neuen Runde sitzt.
4. Eine Vokabelübung startet bei 30/30 weiter (`200`). *Rot, wenn* die Prüfung nicht nach `repeatable` unterscheidet.
5. Wird der Deckel zwischen Start und Abgabe voll, antwortet die Abgabe `409 extra_cap_reached`, die Runde bleibt
   offen (`CompletedAt == null`), keine `ChildPointsEntry` entsteht und die Antwort enthält kein `expected`; am
   nächsten Tag (`TestClock.Advance`) zahlt dieselbe Abgabe die vollen Münzen. *Rot, wenn* die Abgabe weiter kappt und
   verbraucht (dann `200` mit `coinsAwarded: 0`) oder erst nach dem Werten prüft.
6. Der Teil-Deckel zahlt den Rest und verbraucht (bestehender Test bleibt grün). *Rot, wenn* die neue Abgabe-Sperre
   schon bei `left < earned` greift.
7. Der neue Code hat einen deutschen und englischen Satz (`arcadeSpielweg.test.tsx`). *Rot, wenn* ein Eintrag fehlt.
8. Bei vollem Deckel zeigt die Zeile einer Einmal-Übung keinen „Spielen“-Link, sondern „Zahlt nur einmal – morgen
   wieder“; eine Vokabelzeile behält „Spielen“; die Deckel-Karte trägt den neuen Text. *Rot, wenn* `BonusZeile` den
   Link ohne Blick auf `capReached`/`repeatable` rendert.

## Schätzung

**Größe: S** – eine Prüfung an zwei Stellen in `ExtraTrainingService.cs`, ein `ApiErrors`-Eintrag, fünf
Backend-Fälle, `BonusZeile` + zwei Texte je Sprache + ein Problem-Satz, ein Frontend-Fall. Anker B-01.
`wo: beides`, `migration: nein` (kein Schema; Entscheidung 6 hält die Migration draußen), `vertragsbruch: nein`
(neuer Fehlercode ist additiv; `contract.ts`-Union wird neu erzeugt, kein Feld ändert sich).

**Risiken:**

- **Lösungen bei der 409-Abgabe** (Entscheidung 2): die Sperre muss **vor** `preview.CheckAsync` sitzen; AK 5 prüft
  das fehlende `expected`.
- **Reihenfolge der Prüfungen beim Start:** Sperre (`extra_training_locked`) → Pool (`404`) → `extra_exercise_done` →
  `extra_cap_reached`. Ein falscher Platz meldete einer erledigten Übung „morgen wieder“.
- **`ConcurrencyStamp`:** die neue Abgabe-Sperre schreibt nichts, darf also früh zurückkehren – die Wallet-Invariante
  (jeder abbuchende Pfad bumpt den Stempel) ist nicht berührt; der Deckel-Rest wird wie bisher nach dem Laden des
  Kindes gelesen.
- **Auto-Abgabe der Lauf-Seite** (`SohnBonus.tsx:175`): morgen gibt sie beim Einhängen sofort ab – erwünscht; heute
  landet sie in der Fehlerbox mit dem neuen Satz, die Antworten bleiben liegen.
- **Uhr:** „heute“ ist UTC (`Today`, `:45`), wie überall im Bonus-Training.

**Angriffsplan** (Backend zuerst):

1. `ApiErrors.ExtraCapReached` (`extra_cap_reached`, 409) mit `/// <summary>`.
2. Fünf Fälle in `ExtraTrainingTests.cs` (AK 1–5) schreiben, gegen den Ist-Code rot sehen.
3. `StartAsync`: nach `Done`-Prüfung, vor Anlegen **und** Fortsetzen: Einmal-Übung + Deckel voll → 409.
   `SubmitAsync`: nach der Sperre, vor dem Werten: Einmal-Runde (`run.Exercise.Type != Vocabulary`) + `left == 0`
   → 409. Den Deckel-Rest dafür vor das Werten ziehen.
4. Grün inkl. `Der_Tagesdeckel_Kappt_Die_Bonus_Muenzen` (AK 6); rote Probe je Fall am benannten Zweig.
5. Frontend: `extra_cap_reached` in `api.ts`, `de.ts`, `en.ts`; `BonusZeile` bekommt `capReached`; Texte
   `bonus.capReached` neu, `bonus.oneShotTomorrow` neu; Vitest-Fall; `contract.ts` neu erzeugen.
6. E2E `bonus.spec.ts` als Rollengang-Zeuge.

**Testweg:** Integrationstests `backend/Pugling.Api.Tests/ExtraTrainingTests.cs` (AK 1–6, neue Fälle neben
`Der_Tagesdeckel_Kappt_Die_Bonus_Muenzen`, Tageswechsel über `TestClock.Advance`); Vitest
`frontend/src/sohn/arcadeSpielweg.test.tsx` (AK 7, bestehende Schleife über alle Codes) und neu
`frontend/src/sohn/SohnBonus.test.tsx` (AK 8, `SohnBonus` mit gemockter Übersicht bei `coinsToday == dailyCoinCap`).
E2E `frontend/e2e/bonus.spec.ts` und `frontend/e2e/bonus-deckel.spec.ts` als Regressions- und Rollengang-Zeugen.

## Verlauf

- **2026-11-13** — angelegt aus der Nachschau zu B-533 (`pugling-reviewer`, Retro Sprint 69), `entgangen_bei: [B-533]`.
- **2026-11-13** — `ausformuliert`: am Code belegt (Stand nach Sprint 69); die Lauf-Seite hält die Antworten lokal
  bis zur erfolgreichen Abgabe, der Teil-Deckel ist schon getestet, der volle Deckel und `0` nicht.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–6 (Randfall zurückgestellt); Stakeholder-Entscheidung im Auftrag des
  Nutzers (Backlog-Lauf 2026-11-13), B-533 E3/E4/E8 unangetastet.
- **2026-11-13** — `geschaetzt`: S, beides, ohne Migration und Vertragsbruch.
