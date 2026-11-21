---
tags: [typ/story, status/in-arbeit, bereich/rueckmeldung, rolle/student]
aliases: [Rückmeldung nach „Später“ gesperrt]
status: in-arbeit
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
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

1. ~~**Welche Regel gilt am Server?** (a) Signal vom Client, (b) Quelle „Test“ im Request, (c) Knopf im Üben verbergen, (d) Sperre
   entfernen~~ → Entscheidung 1 (d).
2. ~~**Wird der Fehlercode mit entfernt?**~~ → Entscheidung 2.
3. ~~**Ändert sich B-628 nachträglich?**~~ → Entscheidung 3.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

Keine Malus- oder Münzregel ist berührt. Berührt wird eine **abgenommene** Entscheidung bzw. ihr Akzeptanzkriterium:
B-623 AK 4 und B-628 AK 3 verlangen die Server-Sperre; sie entfallen (siehe Entscheidung 3). Entscheidung 5 von B-623
selbst („im Test nur auf dem Ergebnisblatt, im Üben jederzeit“) bleibt unverändert und wird erst wirklich eingelöst.

1. **Die Server-Sperre `feedback_during_test` entfällt (Weg d).** `ExerciseFeedbackController.Create` prüft nicht mehr auf
   einen offenen Versuch (`ExerciseFeedbackController.cs:65-69`), `ExerciseFeedbackService.TestRunningAsync` (`:24`) wird
   gelöscht. *Begründung:* Die Sperre schützt nichts. Der Testbildschirm bietet den Knopf nie an (`KartenMeldung` steht nur
   im Ergebnisblatt, `SohnTest.tsx:393`, `:406`), die Oberfläche hält Entscheidung 5 also selbst ein; und eine Meldung hat
   nach B-623 Entscheidung 6 keine Wirkung auf Lernstand, Punkte, Pflicht, Malus oder Cursor und gibt keine Lösung zurück
   (`FeedbackReceipt(Ids)`, `FeedbackDtos.cs:18`). Die Sperre trifft damit in der echten App nur das Üben bei einem mit
   „Später“ verlassenen Test (`SohnTest.tsx:238-243`, offener Versuch gewollt: `PositionTestsController.cs:130-136`) –
   genau den Fall, den Entscheidung 5 erlauben wollte. *Kosten:* Der Server erzwingt „kein Rückmelden im Test“ nicht mehr;
   ein Client, der es dennoch schickt (ein künftiger Knopf im Test, ein Skript), wird angenommen. *Verworfene
   Alternativen:* (a) braucht ein Signal vom Client und ist als Server-Regel schwach; (b) braucht ein neues Request-Feld
   (Vertragsänderung), und kein heutiger Aufrufer würde es setzen, weil der Test den Knopf nicht anbietet; (c) verbirgt im
   Üben den Knopf bei offenem Test, braucht dort eine neue Kenntnis über den Test (heute liefert `TestAttemptsLeft` nur
   Restversuche, kein „offen“-Flag) und nimmt dem Kind genau die Meldung, die Entscheidung 5 im Üben „jederzeit“ wollte.
   *Ausgeschlossener Fall (gegen die User Story gerechnet):* Die Story will, dass ein Kind beim Üben melden kann und keine
   Absage zu einem Test bekommt, den es nicht vor sich hat. Mit (d) bekommt es auch dann keine Absage, wenn der Test
   tatsächlich offen am Bildschirm steht – dort gibt es den Knopf nicht, der Fall tritt nur über die rohe API ein und ist
   harmlos. Fällt später doch ein Knopf im Test an, ist (b) der Weg, und er ist dann zu bauen, wenn der Knopf da ist.
   Rückfallempfehlung, falls der Vater eine Server-Sperre will: (b).
2. **Der Fehlercode wird mit entfernt, auch in den Texten.** *Begründung:* Ein toter Code im Vertrag lockt den nächsten Leser
   zu einer Sperre, die es nicht mehr gibt. Hand zu entfernen: `ApiErrors.cs:217`, `de.ts:512`, `en.ts:488`, `api.ts:101`,
   `chromeText.tsx:57` samt Kommentar, der Capture-Schritt „Meldung während des Tests“ in `DocsCaptureTests.cs:1043-1048`.
   Erzeugt, nicht von Hand: `docs/openapi/v1.json` (`ContractDocumentTests`, CI hält den Diff, `ci.yml:122`), die
   Beispiele `docs/api-examples/feedback.md` und `index.md` (`DocsCaptureTests`) und `frontend/src/lib/contract.ts` (nicht
   eingecheckt, `ci.yml:194`). *Kosten:* Die Codeliste im Vertragsdokument schrumpft um einen Wert; ein Client, der auf
   `feedback_during_test` verzweigt, läuft ins Leere – es gibt außer der Oberfläche keinen.
3. **B-623 und B-628 werden nicht umgeschrieben.** Beide sind abgenommen. *Begründung:* Die Änderung gehört hierher; im
   Verlauf dieser Story steht, dass B-623 AK 4 und B-628 AK 3 (Server-Sperre) **ersetzt** sind und ihre Tests
   (`ExerciseFeedbackTests.cs:135`, `:317`) umgeschrieben bzw. gelöscht werden. *Kosten:* Wer B-628 allein liest, findet
   noch das Kriterium; der Verweis steht dort nicht. Die Vermerke in B-623 und B-628 setzt der Bau-Lauf beim Abnehmen.

**Überlappung:** [B-623](B-623-feedback-knopf-an-der-karte.md) (AK 4) und [B-628](B-628-rueckmeldung-backend.md) (AK 3)
sind abgenommen und werden hiermit in einem Kriterium ersetzt, nicht in einer Entscheidung; B-629 (Frontend, Knopf im
Ergebnisblatt) bleibt unverändert.

## Akzeptanzkriterien

Jedes Kriterium nennt die Änderung, die es rot macht. Testklasse `ExerciseFeedbackTests`, Frontend-Probe in
`frontend/e2e/rueckmeldung.spec.ts`.

1. Hat das Kind den Test zu einer Position mit „Später“ verlassen (offener Versuch von heute), wird eine Meldung aus dem
   Üben dieser Position angenommen (`201`, eine Zeile). Rot, solange die Sperre in `ExerciseFeedbackController.cs:65-69`
   steht. Der Test `Waehrend_des_Tests_gesperrt_nach_dem_Abgeben_erlaubt` (`:135`) wird dafür umgeschrieben (Versuch
   starten, **nicht** abgeben, melden, `201`), weil er heute die Absage verlangt; `Offener_Test_von_gestern_sperrt_nicht` (`:317`) geht darin auf und wird gelöscht.
2. Dieselbe Meldung lässt den offenen Versuch unberührt: `CompletedAt` bleibt `null`, Antworten und Zähler des Versuchs
   sind vorher gleich nachher (`TestAttempts` im Snapshot des Falls `Meldung_aendert_weder_Kasten_noch_Wallet_noch_Cursor`,
   `:122`). Rot bei der Probe „`ReportAsync` stempelt den offenen Versuch (`CompletedAt`)“. Ohne diese Probe wäre das
   Kriterium eine Absichtserklärung, denn heute fasst `ReportAsync` keinen Versuch an.
3. Das Ergebnisblatt bietet die Meldung weiter je Karte an, der Testbildschirm nicht. Der zweite Teil hält schon
   `rueckmeldung.spec.ts:93-96`. Neu wird der erste: ein Schritt nach der Abgabe, der den Knopf „Rückmeldung“ auf dem
   Ergebnisblatt erwartet; Rot, wenn `KartenMeldung` aus `TestResult` (`SohnTest.tsx:393`, `:406`) entfällt – heute fängt
   das kein Test (nicht gefunden).
4. `feedback_during_test` ist aus `ApiErrors`, den Texten (`de.ts`, `en.ts`, `api.ts`, `chromeText.tsx`) und dem Capture
   verschwunden; Vertragsdokument und Beispiele sind neu erzeugt und committet. **Absichtserklärung:** Rot wird nur ein
   Rest, der Code und Dokument auseinanderlaufen lässt (der CI-Diff auf `docs/openapi/v1.json`, die Beispiel-Prüfung in
   `DocsCaptureTests`); dass nichts mehr referenziert wird, prüft kein Tor.

## Schätzung

**Größe: S** — viele Fundstellen, aber jede klein: eine Prüfung und eine Service-Methode fallen weg, ein Fehlercode samt
vier Texten geht, zwei Tests ändern sich, ein E2E-Schritt kommt dazu. Kein Umbau von Logik. Gegen M (Batch-Pfad im
`MediaSelector`) gehalten: kein neuer Pfad, nur Entfernen.

- **`migration: nein`** — kein Schema berührt (`ExerciseFeedback` bleibt wie es ist).
- **`vertragsbruch: nein`** — `Pugling.Contracts` bleibt unverändert: `CreateFeedbackDto` und `FeedbackReceipt` ändern sich
  nicht, und `feedback_during_test` steht nur in `ApiErrors` (`backend/Pugling.Api/Errors/ApiErrors.cs:217`), nicht im
  Vertrags-Projekt. Nachgesehen, ob das Entfernen eines Fehlercodes brechend ist: der Code ist Teil des generierten
  `code`-Enums in `docs/openapi/v1.json:44493` und schrumpft um einen Wert. Das ist formal kein Bruch – eine Antwort mit
  diesem Code kommt nicht mehr, und `Pugling.Client`/`Agent.Creator` verzweigen nicht darauf (keine Fundstelle in
  `backend/` außerhalb von `Pugling.Api`/Tests). Brechend wäre es nur für einen fremden Client, der den Code behandelt;
  den gibt es außer der Oberfläche nicht (Entscheidung 2). `ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry` hält
  Registry und Enum gleich, zieht also von allein mit.
- **`wo: beides`** — Backend zuerst (Sperre, Code, Tests, erzeugte Doku), danach die Frontend-Texte und der E2E-Schritt.

### Risiken

- **Erzeugte Artefakte driften:** `docs/openapi/v1.json`, `docs/api-examples/feedback.md` und `index.md` sowie
  `backend/Pugling.Api/OpenApi/openapi-examples.generated.json` (Zeile 1961 trägt das Beispiel) hängen am
  Capture-Schritt `DocsCaptureTests.cs:1043-1048`. Erst den Schritt löschen, dann Tests laufen lassen (sie schreiben die
  Dateien neu), dann committen – sonst rotes CI-Diff.
- **Frontend-Reste:** `de.ts:512`, `en.ts:488`, `lib/api.ts:101`, `lib/chromeText.tsx:57` müssen alle fallen, sonst steht ein
  Text für einen Code, den es nicht gibt; ob `chromeText.tsx` gegen das generierte `contract.ts` typisiert ist und der
  Build deshalb von allein meckert, ist nicht nachgesehen.
- **Nebenfund B-586:** Die Story B-586 (gegrillt) nennt `TestRunningAsync` und `feedback_during_test` in ihrem Ist-Stand
  (`B-586-kind-fragt-den-betreuer-zur-aufgabe.md:41-42`, `:70`). Nach diesem Bau stimmt das dort nicht mehr; B-586 ist
  vor ihrem Bau gegen den Code neu zu lesen.
- **Zwei abgenommene Kriterien werden ersetzt** (B-623 AK 4, B-628 AK 3): im Verlauf der beiden Stories zu vermerken
  (Entscheidung 3, setzt der Bau-Lauf).

### Angriffsplan

1. Backend: Sperre in `ExerciseFeedbackController.Create` (`:65-69`) und `ExerciseFeedbackService.TestRunningAsync`
   (`:24`) entfernen; `ApiErrors.FeedbackDuringTest` (`:217`) löschen.
2. Tests: `ExerciseFeedbackTests` umschreiben (AK 1, AK 2), `Offener_Test_von_gestern_sperrt_nicht` löschen;
   Capture-Schritt in `DocsCaptureTests.cs:1043-1048` streichen.
3. Läufe: `ContractDocumentTests` und `DocsCaptureTests` erzeugen `docs/openapi/v1.json`, die Beispiele und das
   generierte Katalog-JSON neu; Diff lesen, er darf nur den einen Code und das eine Beispiel zeigen.
4. Frontend: vier Textstellen entfernen; E2E-Schritt (AK 3) ergänzen.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- `ExerciseFeedbackTests` (`backend/Pugling.Api.Tests/ExerciseFeedbackTests.cs`): `Waehrend_des_Tests_gesperrt_nach_dem_Abgeben_erlaubt`
  (`:135`) wird zu „Versuch gestartet, nicht abgegeben, gemeldet, `201`“ – **vorher rot**, weil die Sperre heute
  `409` liefert; fällt nur durch das Entfernen von `ExerciseFeedbackController.cs:65-69`. AK 2 (offener Versuch bleibt
  unberührt) hat im Ausgangszustand keine rote Probe und wird mit der Probe „`ReportAsync` stempelt `CompletedAt`“
  gemessen, wie im Kriterium verlangt.
- `ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry` und der CI-Diff auf `docs/openapi/v1.json` (`ci.yml:122`) halten AK 4
  nur halb – im Kriterium als Absichtserklärung beschriftet.
- `frontend/e2e/rueckmeldung.spec.ts` (ein Test vorhanden, Zeile 27): neuer Schritt nach der Abgabe, der den Knopf
  „Rückmeldung“ auf dem Ergebnisblatt erwartet (AK 3); die Probe „`KartenMeldung` fällt aus `TestResult`“ ist im Bau zu
  messen, vorher fängt sie nichts.
- Vor der Abnahme: `pugling-reviewer` und `frontend-reviewer`; Rollengang als Sohn (Test öffnen, „Später“, üben, melden)
  – eine E2E, die den Weg fährt, zählt.

## Verlauf

- 2026-11-19 · idee · aufgenommen aus der Nachschau von Sprint 88 (B-628/B-629); am Code bestätigt. Bewusst nicht in
  Sprint 96 behoben, weil das Sprint-Ziel (Shop je Kind) ohne den Fix erfüllt ist.
- 2026-11-19 · ausformuliert · Ist-Stand mit `Datei:Zeile` neu am Code belegt; neu gefunden: der Test bietet den Knopf nie
  an, die Server-Sperre schützt daher nichts und trifft nur das Üben; Weg (d) „Sperre entfernen“ als Empfehlung
  ergänzt, Akzeptanzkriterien als Entwurf.
- 2026-11-19 · gegrillt · Vorbereitungslauf (Sonnet), stellvertretend und unbestätigt: Server-Sperre `feedback_during_test`
  entfällt (Weg d), Fehlercode samt Texten wird entfernt, B-623 AK 4 und B-628 AK 3 (beide abgenommen) sind damit
  ersetzt und werden nicht umgeschrieben; Akzeptanzkriterien final, Kriterium 5 als Absichtserklärung beschriftet.
- 2026-11-19 · geschaetzt · Vorbereitungslauf (Sonnet): S, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (Fehlercode nur in `ApiErrors` und im erzeugten Enum, kein Client verzweigt darauf); Risiko: B-586 zitiert die Sperre
  in seinem Ist-Stand.
- **2026-11-19** — Entscheidung 1 (Weg d, Server-Sperre entfällt) vom Nutzer bestätigt; die Stellvertreter-Marke gilt damit für diese Entscheidung nicht mehr.
- **2026-11-21** — Nachtlauf, Sprint 99: `in-arbeit` und gebaut. Die stellvertretenden Entscheidungen 1–3 aus dem
  Vorbereitungslauf trägt der Lauf als Defekt (Freigabe 1). Gegen den Code nachgelesen, sie stimmen.
  - **Backend:** Sperre und `TestRunningAsync` entfernt, `ApiErrors.FeedbackDuringTest` gelöscht, Capture-Schritt
    gestrichen; Vertrag und Beispiele neu erzeugt (nur der Code, zwei Beispiele, die Summary und das 409 fallen weg).
  - **Frontend:** vier Texte entfernt; `rueckmeldung.spec.ts` Schritt 5b (Weg des Sohnes: Test mit „Später weiter“
    verlassen, im Üben melden) und Schritt 6 (Ergebnisblatt).
  - **AK 1 →** `ExerciseFeedbackTests.Offener_Test_sperrt_die_Meldung_aus_dem_Ueben_nicht`, rot vorher: erwartet 201,
    gemessen 409 `feedback_during_test`. Dazu E2E 5b.
  - **AK 2 →** derselbe Test, Schnappschuss des Versuchs (CompletedAt, Zähler, Passed, Cursor, Ergebniszeilen).
  - **AK 3 →** E2E Schritt 6.
  - **AK 4:** Absichtserklärung; die Suche nach Resten in Backend, Frontend, Client und Agent ergab keine Fundstelle.
  - Proben:

| Probe | Stelle | erwartet | gemessen |
| --- | --- | --- | --- |
| Sperre zurück (drei Backend-Dateien aus `HEAD`) | `ExerciseFeedbackController.cs` | E2E 5b rot | rot, `rueckmeldung.spec.ts:110` „Danke, ist angekommen!“ fehlt |
| `KartenMeldung` aus dem Ergebnisblatt | `SohnTest.tsx:393`, `:406` | E2E 6 rot | rot, `:118` |
| `ReportAsync` stempelt den offenen Versuch | `ExerciseFeedbackService.cs:36` | AK-2-Test rot | rot, 1 failed von 11 |

  - **Review `pugling-reviewer`:**
    - 🟡 Die Swagger-Summary versprach noch die Absage → umformuliert (Kommentar, ohne Zählung).
    - 🟡 `[ProducesResponseType(409)]` ohne 409-Pfad → entfernt (gezählt, Sprint 99: 1).
    - 🟢 Der Schnappschuss ohne Cursor und Ergebnisse → ergänzt (ohne Zählung).
    - 🟢 B-586 nennt die entfernte Sperre als Vorbild → dort vermerkt.
  - Kein Fund zum Anti-Cheat: Die Quittung enthält nur Ids, der Hinweis nur die Aufgabe.
  - Backend 1931/1931, Vitest 1301/1301.
