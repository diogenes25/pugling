---
tags: [typ/story, status/ausformuliert, bereich/rueckmeldung, rolle/student]
aliases: [Rückmeldung nach „Später“ gesperrt]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 88 in der PM-Sitzung 2026-11-19"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-628]
---

# B-685 · Ein mit „Später“ verlassener Test sperrt die Rückmeldung im Üben für den ganzen Tag

Entscheidung 5 von B-623 erlaubt die Rückmeldung „im Üben jederzeit“; nur **im** Test wartet sie bis zum Ergebnisblatt,
weil ein Knopf mitten im Test ablenkt. Die Sperre am Server prüft aber nicht, ob gerade getestet wird, sondern ob es
heute einen offenen Versuch zur Position gibt. Das Kind sieht den Knopf im Üben, schreibt seine Meldung und bekommt
erst beim Absenden die Absage.

## User Story

Als Schüler möchte ich eine Karte beim Üben melden können, auch wenn ich den Test zu dieser Übung nur mit „Später“
verlassen habe, damit ich nicht erst eine Meldung tippe und dann eine Absage zu einem Test bekomme, den ich gar nicht
mehr vor mir habe.

## Ist-Stand am Code

- **Die Sperre hängt am offenen Versuch, nicht am Testbildschirm.** `ExerciseFeedbackService.TestRunningAsync`
  (`backend/Pugling.Api/Services/Shared/ExerciseFeedbackService.cs:24`) ist wahr, sobald zur Position ein Versuch des
  Kindes (`!BySupervisor`) mit `CompletedAt == null` und `Day == heute` existiert. Ein offener Versuch von einem
  früheren Tag sperrt nicht (Kommentar `:20-23`). Einziger Aufrufer ist
  `ExerciseFeedbackController.Create` (`backend/Pugling.Api/Controllers/Student/ExerciseFeedbackController.cs:67`),
  Antwort `409 feedback_during_test` (`backend/Pugling.Api/Errors/ApiErrors.cs:217`).
- **Der Request kennt keine Quelle.** `CreateFeedbackDto(Reasons, Text, ItemIndex)`
  (`backend/Pugling.Contracts/Student/FeedbackDtos.cs:12`) trägt nichts, womit der Server Üben und Test
  unterscheiden könnte.
- **„Später“ lässt den Versuch absichtlich offen.** Der Testbildschirm legt den Versuch beim Öffnen an
  (`frontend/src/sohn/SohnTest.tsx:121`); „Später weiter“ ruft den Server nicht auf und führt nur zur Startseite
  (`SohnTest.tsx:238-243`, Kommentar dort). Der Server setzt den laufenden Versuch am selben Tag fort statt einen neuen
  anzulegen, damit ein Verlassen keinen der begrenzten Versuche verbraucht
  (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:130-136`). Der offene Versuch ist also gewollt.
- **Das Üben kennt die Sperre nicht.** Es zeigt an jeder Karte den Rückmelde-Knopf
  (`frontend/src/sohn/SohnPractice.tsx:632`, `KartenMeldung` ohne Bedingung). Eine Prüfung auf einen offenen Test steht
  dort und im Üben-Controller nicht (`PositionPracticeController.cs`: keine Fundstelle zu `TestAttempts`).
- **Der Test selbst zeigt den Knopf nie.** `KartenMeldung` steht im Test nur auf dem Ergebnisblatt
  (`SohnTest.tsx:395`, `:408`, in `TestResult`), also **nach** der Abgabe, wenn der Versuch schon abgeschlossen ist
  (`PositionTestsController.cs:461`).
- **Die Absage erscheint erst nach dem Tippen.** `KartenMeldung` fängt den Fehler über `useAction` ab und zeigt ihn in
  einem `StatusBanner` (`frontend/src/sohn/KartenMeldung.tsx:44`, `:72`, `:106`); der Text steht in
  `frontend/src/i18n/de.ts:512`. Eine Vorab-Prüfung vor dem Öffnen des Formulars gibt es nicht (nicht gefunden).
- **Der Test deckt nur den frischen Versuch.** `ExerciseFeedbackTests.Waehrend_des_Tests_gesperrt_nach_dem_Abgeben_erlaubt`
  (`backend/Pugling.Api.Tests/ExerciseFeedbackTests.cs:135`) startet den Versuch und meldet sofort; ein Fall „Test
  begonnen, verlassen, dann geübt und gemeldet“ fehlt (nicht gefunden).

## Die echte Lücke

Der Defekt ist belegt wie beschrieben: Test zu P öffnen, „Später“ tippen, P üben, Karte melden → `409`, bis das Kind
den Test abgibt oder Mitternacht UTC vorbei ist. Das Szenario liegt im Alltag nahe, weil „Später“ ausdrücklich dazu
einlädt, erst zu üben.

Schärfer gefasst, trägt die Sperre überhaupt nur an einer Stelle: Der **Test** bietet die Meldung gar nicht an
(`SohnTest.tsx:395`, nur auf dem Ergebnisblatt). Der Schutz „kein Knopf mitten im Test“ ist damit eine
Oberflächenregel, die schon die Oberfläche einhält; die Server-Sperre trifft in der echten App ausschließlich Meldungen
aus dem Üben bei offenem Test – also genau den Fall, den B-623 Entscheidung 5 erlauben wollte. Anti-Cheat trägt sie
nicht: eine Meldung hat nach B-623 Entscheidung 6 keine Wirkung auf Lernstand, Punkte, Pflicht, Malus oder Cursor und
liefert keine Lösung zurück (`FeedbackReceipt(Ids)`, `FeedbackDtos.cs:18`).

## Offene Punkte

1. **Welche Regel gilt am Server?** B-628 AK 3 („eine Meldung zu dessen Karten“) lässt sich positionsweit lesen und hat
   diese Sperre erzeugt. Wege:
   - **(a)** nur sperren, während der Testbildschirm aktiv ist: braucht ein Signal vom Client und ist darum schwach für
     eine Server-Regel.
   - **(b)** die Meldung aus dem Üben nie sperren und nur eine Meldung mit Quelle „Test“ bis zum Ergebnisblatt
     aufschieben: braucht ein neues Request-Feld (Vertragsänderung), und kein heutiger Aufrufer würde es setzen, weil der
     Test den Knopf nicht anbietet.
   - **(c)** den Knopf im Üben verbergen, solange ein Test offen ist, mit einem Satz dazu: braucht im Üben eine neue
     Kenntnis über den offenen Test (heute liefert `TestAttemptsLeft` nur die Restversuche, kein „offen“-Flag; nicht
     gefunden) und nimmt dem Kind genau die Meldung, die B-623 im Üben „jederzeit“ wollte.
   - **(d)** die Server-Sperre ganz entfernen; die Oberfläche hält die Regel „kein Knopf im Test“ ohnehin ein.

   *Empfehlung: (d).* Sie stellt B-623 Entscheidung 5 am schlichtesten her („im Üben jederzeit“), braucht weder ein neues
   Vertragsfeld noch einen neuen Zustand im Üben und verliert nichts, weil die Sperre keinen Anti-Cheat-Zweck hat
   (Entscheidung 6) und der Test den Knopf nicht zeigt. Kosten: `feedback_during_test` und B-628 AK 3 entfallen;
   die i18n-Texte (`de.ts:512`, `en.ts:488`, `api.ts:101`, `chromeText.tsx:57`) und der Fehlercode im Vertrag
   (`contract.ts:28804`) werden mit entfernt. Fällt später ein Knopf im Test doch an, ist (b) der Weg und dann zu
   bauen, wenn der Knopf da ist. Rückfallempfehlung, wenn der Vater eine Server-Sperre will: (b).
2. **Wird der Fehlercode mit entfernt?** Folgt aus Punkt 1. Bei (d) ja – ein toter Code im Vertrag lockt den nächsten
   Leser zu einer Sperre, die es nicht mehr gibt. *Empfehlung: entfernen.*
3. **Ändert sich B-628 nachträglich?** Dessen Akzeptanzkriterium 3 ist abgenommen. *Empfehlung:* nicht umschreiben,
   sondern im Verlauf dieser Story als ersetzt benennen (B-628 ist abgenommen; die Änderung gehört hierher).

## Akzeptanzkriterien (Entwurf)

1. Hat das Kind den Test zu einer Position mit „Später“ verlassen (offener Versuch von heute), wird eine Meldung aus
   dem Üben dieser Position angenommen (`201`), nicht mit `409` abgewiesen. Rot macht ihn heute die Sperre in
   `ExerciseFeedbackController.cs:67`.
2. Der Fall steht als eigener Integrationstest in `ExerciseFeedbackTests`: Versuch starten, **nicht** abgeben, dann
   melden. Der bestehende Test (`:135`) wird dafür umgeschrieben, weil er die Absage verlangt.
3. Die Meldung ändert dabei weiter weder Lernstand, Punkte, Pflicht, Malus noch den Cursor des offenen Versuchs (der
   Versuch bleibt `CompletedAt == null`, sein Cursor unverändert). Rot macht ihn jede Änderung, die eine Meldung an
   den Versuch bindet.
4. Das Ergebnisblatt bietet die Meldung weiter je Karte an; der Testbildschirm bietet sie weiterhin nicht an. Rot macht
   ihn ein `KartenMeldung` im laufenden Test.
5. Bei Empfehlung (d): `feedback_during_test` ist aus `ApiErrors`, dem Vertrag und den Texten verschwunden; die
   reflexiven Tore (Fehlercode-Liste, Texte) bleiben grün.

## Verlauf

- 2026-11-19 · idee · aufgenommen aus der Nachschau von Sprint 88 (B-628/B-629); am Code bestätigt. Bewusst nicht in
  Sprint 96 behoben, weil das Sprint-Ziel (Shop je Kind) ohne den Fix erfüllt ist.
- 2026-11-19 · ausformuliert · Ist-Stand mit `Datei:Zeile` neu am Code belegt; neu gefunden: der Test bietet den Knopf nie
  an, die Server-Sperre schützt daher nichts und trifft nur das Üben; Weg (d) „Sperre entfernen“ als Empfehlung
  ergänzt, Akzeptanzkriterien als Entwurf.
