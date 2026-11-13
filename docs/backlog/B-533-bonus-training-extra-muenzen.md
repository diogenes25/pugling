---
tags: [typ/story, status/in-arbeit, bereich/gamification, bereich/frontend, rolle/student]
aliases: [Bonus-Training, Extra-Übungen, Extra-Münzen]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: "Nutzer im Chat 2026-11-13: neben dem Lernpfad Extra-Übungen für Extra-Münzen, ohne Durchklicken"
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-533 · Bonus-Training: Extra-Übungen zum aktuellen Thema für Extra-Münzen

Der Lernpfad (B-525) erfüllt das Tagessoll. Danach gab es für den Sohn keinen Weg mehr, mit zusätzlichem Üben
Münzen zu verdienen: das Üben zahlt nur fällige Karten und jede nur einmal am Tag (`PositionPracticeController.cs`,
`due && !alreadyScoredToday`). Gewünscht ist ein Anreiz zum Weiterüben, der **kein Betrug** sein darf: keine
Übung, die man sich einmal merkt und dann durchklickt, und keine leichten Formen, mit denen man sich Münzen
erklickt.

## User Story

Als **Sohn** möchte ich nach meiner Tagespflicht weitere Übungen zu meinem aktuellen Thema machen und dafür
Extra-Münzen bekommen, damit sich Mehr-Üben lohnt. Als **Vater** möchte ich, dass diese Münzen nur für echte
Arbeit fließen.

## Entscheidungen

Gegrillt im Chat am 2026-11-13 (drei Fragen, jeweils die Empfehlung gewählt); der Rest stellvertretend entschieden.

1. **Erst die Pflicht, dann die Kür.** Das Bonus-Training öffnet erst, wenn die Tagespflicht jedes spielbaren
   Plans erfüllt ist (ein Plan ohne Pflicht zählt als erfüllt). Davor ist es auf dem Pfad sichtbar, aber gesperrt
   (`409 extra_training_locked`). Kosten: ein Kind, das die Pflicht nicht schafft, sieht die Münzen nur.
2. **Selbsteinschätzung zahlt nichts.** Karteikarten-Runden sind Aufwärmen; Münzen gibt es nur für geprüfte
   Treffer (Auswahl 1, Tippen 2 je Treffer, Konfiguration `Gamification:ExtraTraining`).
3. **Tagesdeckel**, einstellbar (`DailyCoinCap`, Vorgabe 30, `null` = ohne Deckel). Zurzeit über die
   Server-Konfiguration, nicht je Kind im Vater-Web.
4. **Eine Übung zahlt einmal.** Eine Übung ohne Vokabeln hat genau einen Durchgang; schon der Start verbraucht
   sie (eine offene Runde wird fortgesetzt, nie ersetzt). Eine Vokabelübung darf wiederholt werden, fragt aber mit
   jedem abgeschlossenen Durchgang schwerer: 2× Karteikarte, 2× Auswahl (ab 4 Wörtern), danach dauerhaft Tippen.
   Tippen aus dem Gedächtnis darf immer wieder Münzen bringen – das ist Arbeit (Wortlaut des Nutzers).
5. **Das Thema** sind die Lehrwerk-Units, aus denen die Positionen der spielbaren Pläne stammen, plus die aktuelle
   Unit der Lehrbücher des Kindes; dazu jede freigegebene (`ExecutePublic`) oder zugewiesene Übung dieser Units.
6. **Bekanntes fliegt raus.** Eine Übung ohne Vokabeln, die Position in irgendeinem Plan des Kindes ist (oder war),
   steht nicht im Pool: das Kind kennt sie vom Pfad.
7. **Unabhängig vom Plan.** Eine Bonus-Runde bewegt keine Leitner-Box und kein Planziel; sie schreibt eine eigene
   Zeile `ExtraTrainingRun` und bucht `PointKind.ExtraTrainingCoins`.
8. **Gewertet wird am Ende der Runde**, wie im Test: keine Rückmeldung je Karte, damit eine Runde nicht Karte für
   Karte verrät, was richtig war. Reihenfolge der Karten und der Auswahl-Optionen werden je Runde neu gemischt –
   die geteilte Projektion legt die richtige Option sonst nach dem Index fest, und das lernte man als Position.

## Akzeptanzkriterien

1. Vor erfüllter Pflicht: `GET …/extra-training` meldet `unlocked: false`, `lockReason: duty_open` und die offenen
   Pflichten; ein Start antwortet `409 extra_training_locked`. Auf dem Pfad steht der Einstieg gesperrt.
2. Eine Vokabelübung fragt in den Durchgängen 1–2 als Karteikarte (0 Münzen), 3–4 als Auswahl (1 je Treffer),
   danach getippt (2 je Treffer).
3. Eine Übung ohne Vokabeln ist nach dem Start verbraucht: ein zweiter Start setzt dieselbe Runde fort, nach der
   Abgabe antwortet er `409 extra_exercise_done`, eine zweite Abgabe `409 extra_run_completed`.
4. Eine Übung ohne Vokabeln aus dem Lernpfad steht nicht im Pool.
5. Der Tagesdeckel kappt die Buchung (`capReached: true`).
6. Der Vater liest die Übersicht, spielt aber nicht (`403`).

## Schätzung

**Größe: L** – neue Tabelle mit Migration (reines Anlegen, keine Bestandszeilen), Service, Controller, Vertrag,
Client, neuer Arcade-Bildschirm. Risiken: Münz-Ökonomie (Deckel und Rate sind Schätzwerte), Nebenläufigkeit beim
Deckel (gelöst über den `ConcurrencyStamp` des Kindes). Testweg: `ExtraTrainingTests` (6 Fälle, je mit roter Probe),
E2E `e2e/bonus.spec.ts`.

## Bekannte Grenzen

- Eine Einmal-Übung zeigt ihre Fragen beim Start; wer die Runde verlässt, findet beim Fortsetzen dieselben Fragen
  wieder und könnte dazwischen nachschlagen. Eine Antwort je Karte beim Server (wie im Test) schlösse das – nicht
  gebaut. Die schon gegebenen Antworten merkt sich nur der Browser (`sessionStorage`).
- Die Reihenfolge „Kind zuerst laden“ in `SubmitAsync` (gegen die doppelt gebuchte Abgabe) hält kein Test: der
  In-Process-Server verschränkt zwei Anfragen nicht, die Mutante blieb 3 von 3 Läufen grün. Der Test
  `Eine_Doppelte_Abgabe_Bucht_Nur_Einmal` ist darum als Absichtserklärung beschriftet.
- Die Einstufung „Wortkasten zahlt als Auswahl“ (Entscheidung aus dem Review) ist ohne eigenen Test.
- Die Sperre auf dem Pfad rechnet der Client für den gewählten Plan nach (`dutyDone || goalsTotal === 0`); bei
  mehreren spielbaren Plänen könnte sie vom Server abweichen – heute ist genau einer spielbar.
- Der Deckel und die Raten sind Server-Konfiguration, nicht je Kind im Vater-Web einstellbar.
- Bonus-Runden schreiben keinen plan-übergreifenden Item-Lernstand (`ItemProgress`); der Vater sieht sie nur im
  Münz-Verlauf.

## Verlauf

- 2026-11-13: Im Chat gegrillt (Entscheidungen 1–3), gebaut: Backend mit 6 Integrationstests (alle 5 roten Proben
  fallen am richtigen Test), Frontend mit E2E `bonus.spec.ts` (grün, zusammen mit `pfad`/`lektion`).
- 2026-11-13: Review (`pugling-reviewer`, `frontend-reviewer`) eingearbeitet: die doppelte Abgabe buchte zweimal
  (Kind jetzt zuerst geladen), eine aufgesparte Runde zahlte an einem Tag ohne erfüllte Pflicht (Abgabe prüft die
  Sperre und bucht gegen den heutigen Deckel), Wortkasten zahlte den Tipp-Preis, `FullRunTouchedActions` 276 → 279;
  im Frontend Fehlerbild nach gelungener Abgabe, Verlassen während der Abgabe, `role="alert"`, Fortsetzen bei der
  richtigen Karte. Drei neue Integrationstests (9 gesamt), E2E um das Fortsetzen ergänzt (rote Probe fällt in der
  richtigen Zeile). Volle Suite 1535/1535 grün, Vitest 923/923. Migration gegen eine Kopie der echten DB geprobt.
