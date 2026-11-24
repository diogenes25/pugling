# PM-Sitzung: Nachtlauf K – Meldung zur Test-Antwort mit Ausgang

**Datum:** 2026-11-24  ·  **Moderation:** PM (Nachtlauf K, unbeaufsichtigt, Freigaben 1–11 aus [nachtlauf.md](nachtlauf.md))
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Das Paket B-767, B-768, B-769: eine Meldung zeigt auf die beanstandete Test-Antwort, der Vater erledigt sie mit
„berechtigt“ oder „abgewiesen“, und „berechtigt“ rechnet den Test nach und bucht Pflicht, Belohnung und Malus nach.

Einer von drei parallelen Nachtläufen (J, K, L), im Worktree `nachtlauf-k` auf Branch `nachtlauf/2026-11-24-k`, abgezweigt
von `main` 5993c86b. Eigene Nummern: Stories B-970–B-979, Sprints ab 151.

## Runde 1 — Auswahl

Kein frischer Rollengang zu Beginn: Das Paket ist im Auftrag vorgegeben. Kein neuer Befund unter `docs/anmerkungen/`.

Alle drei Stories stehen auf `geschaetzt`, `art: Wunsch` – baubar nach Freigabe 8. Ihre Entscheidungen tragen die Marke
„stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt“; sie wurden dort auf Anweisung des
Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“, B-627 Verlauf) so getroffen. Wie in den Läufen G und H
baue ich sie und halte die Marke im Abschlussbericht fest. Jüngste Nachprüfung-Zeilen: keine vorhanden.

Vor dem Bau gegen den Code geprüft (Stand 5993c86b): `ExerciseFeedback` ohne Antwort-Bezug, `FeedbackStatus` zweiwertig,
die Zusammenführung ohne Bezug (`ExerciseFeedbackService.cs:42-44`), zwei Lesestellen von `Passed`
(`PositionProgressService.cs:235`, `MetricsService.cs:33`), Strafzeile ohne Rücknahme, `EvaluateAndAwardAsync` ohne
Stamp-Bump. Die Zeilennummern der Stories waren verschoben, die Behauptungen stimmten.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 151 | B-767, B-768, B-769 | Meldung zur Test-Antwort mit Ausgang, vom Bezug bis zur Beschriftung der Rücknahme |

## Sprint 151 — Ziel & Umfang

**Sprint-Ziel:** Der Vater gibt seinem Sohn bei einer im Test zu streng gewerteten Antwort mit einem Klick recht, und
danach gilt der Test als bestanden: die Tagespflicht ist erfüllt, ein gebuchter Malus kommt als „Malus zurückgenommen“
zurück, eine ausgebliebene Ziel-Belohnung wird einmal nachgebucht, und der Sohn liest, was nicht neu gebucht wird.

**Umfang:** B-767 (M, backend), B-768 (L, backend), B-769 (S, frontend).

**Entwickler-Brief:**

- **Quelle der Wahrheit:** der Bezug wird beim Absenden der Meldung eingefroren (`ExerciseFeedback.TestItemResultId`), der
  Ausgang ist ein eigenes Feld (`Outcome`), die Korrekturspur steht am Versuch (`CorrectedItems`,
  `PassedAfterCorrection`), die Rücknahme-Marke an der Strafzeile (`RefundedAt`, `RefundedByFeedbackId`).
- **Senke:** `FeedbackCorrectionService.JudgeAsync` – der eine Weg, über den ein Urteil gespeichert und eine Korrektur
  gebucht wird. „Bestanden“ liest jeder buchende oder zählende Weg über `TestAttempt.CountsAsPassed`.
- **Guards:** „berechtigt“ nur mit Bezug (`409 feedback_without_test_answer`) und endgültig (`409 feedback_justified`);
  ein Urteil nur mit `Done` (400); ein fremder Supervisor `404`, das Kind `403`. Wallet-Invariante: ein Bump in der
  Transaktion, auch auf dem nur zubuchenden Weg.
- **Migrationen (nur angehängt):** `20261124201543_FeedbackAnswerOutcome` (B-767), `20261124202234_FeedbackCorrection`
  (B-768). Beim Merge nach `main` gegebenenfalls neu erzeugen, damit die Kette linear bleibt.
- **Testweg:** `ExerciseFeedbackTests` (vier neue Fälle), `MeldungBuchtNachTests` (acht Fälle),
  `MeldungBuchtNachMigrationTests`, Vitest an `VaterRueckmeldungen`, `KartenMeldung`, `labels`, und die neue E2E-Spec
  `e2e/meldung-berechtigt.spec.ts` als Rollengang.

## Iteration 151 — umgesetzt

- **Backend (API-First):** `ExerciseFeedback.TestItemResultId`/`Outcome`, Bezug beim Absenden eingefroren, Zusammenführung
  je Antwort; `UpdateFeedbackDto.Outcome` (PATCH: `null` behält, `Open` löscht) mit `CheckChange`; Senke
  `FeedbackCorrectionService.JudgeAsync` (Spur, Rücknahme mit Marke an der Strafzeile, fehlende Belohnung über
  `AddGoalRewardIfDueAsync`, ein Stamp-Bump in einer Transaktion); `TestAttempt.CountsAsPassed` in Pflicht und
  Missionszahl; `AttemptDetail` zeigt die Spur; zwei neue 409-Codes. Migrationen `20261124201543_FeedbackAnswerOutcome`,
  `20261124202234_FeedbackCorrection` (nur nullbare Spalten, keine Rückfüllung).
- **Frontend:** „Berechtigt“/„Abgewiesen“ im Vater-Web (Rückfrage vor „Berechtigt“, endgültig), Hinweissatz an das Kind,
  „Malus zurückgenommen“ nach Vorzeichen in beiden Konten, deutsche Texte der 409-Codes.
- **Verifikation:** .NET 2356/2356 (Release, nach dem letzten Code-Edit), Vitest 1448/1448, `tsc` sauber, E2E 2/2
  (`meldung-berechtigt`, `rueckmeldung`, Ports 5221/5191), Mutanten 20/20 backend und 6/6 Vitest rot am benannten Test
  (Tabellen in B-767, B-768, B-769). Ein erster voller Lauf stand 2355/2356: `ContractDocumentTests` lief unter der Last
  dreier paralleler Läufe in den HTTP-Timeout (keine Code-Änderung, im nächsten Lauf grün); `SohnBonus.test.tsx` fiel
  einmal unter Last und lief allein 34/34 grün.
- **Review:** `pugling-reviewer`: 🟡 Versuchsansicht ohne Spur → behoben; 🟢 Kommentar am Stufenaufstieg → ergänzt; 🟢
  Urteile parallel → B-971; AK-4-Lesart und Testkommentar → Verlauf bzw. Kommentar. `frontend-reviewer`: 🔴 deutsche Texte
  der 409-Codes fehlten → ergänzt; 🟡 „Abbrechen“ der Rückfrage ungeprüft → Fall ergänzt; 🟡 `confirm` nicht gestubbt →
  `vi.stubGlobal`; 🟢 „Berechtigt“ an richtig gewerteter Antwort → B-970.

**Fehlerzähler Sprint 151: 5** (gezählt, weil Code oder Tests geändert): die vier Review-Funde oben (🔴 409-Texte, 🟡
`AttemptDetail`, 🟡 Abbrechen-Fall, 🟡 `confirm`-Stub) und ein eigener Fund beim Prüfen: die E2E-Spec las die Buchungen aus
`items` statt `entries` (Spec korrigiert, danach zusätzlich „vorher keine Belohnung“ zugesichert). Nicht gezählt: zwei
Testfälle, die beim ersten Schreiben falsch annahmen (die zweite Test-Antwort ist auf einer niedrigeren Stufe nicht
„richtig“; `vi.spyOn(window.confirm)`), weil sie vor dem ersten Grün des Increments lagen – streng gelesen wären es 7.
Über 5 liegt der Zähler nur in dieser strengen Lesart; das Paket ist ohnehin durch, der Lauf endet hier.

## Runde 151 — Re-Review / Abnahme

Server: von Playwright nach dem letzten Edit gestartet (Wegwerf-DB, Ports 5221/5191); Migrationsprobe an einer Kopie der
echten DB auf :5222, danach beendet.

- **Vater – abgenommen:** „Die Meldung zeigt mir seine Antwort ‚eastward‘ und dass der Test sie falsch gewertet hat. Ich
  klicke ‚Berechtigt‘, bestätige, und das Tagesziel ist nachgebucht – 15 Münzen, vorher keine. Wieder öffnen kann ich sie
  danach nicht, das ist richtig so.“ Gefahren durch `e2e/meldung-berechtigt.spec.ts`; die Malus-Rücknahme ist durch
  `MeldungBuchtNachTests` belegt, nicht im Browser (ein Malus braucht eine abgeschlossene Vortags-Periode).
- **Sohn – abgenommen:** „Nach dem Test konnte ich sagen, dass das zu streng war, und am nächsten Login steht da, dass ich
  recht hatte und was nicht neu gebucht wird.“ Gefahren durch dieselbe Spec. Ob sich das fair anfühlt, bleibt ein Blick
  am Gerät.
- **Creator – Regressionszeuge:** die Zählung der Gründe (`SummaryAsync`) ist unverändert, `Ersteller_sieht_gezaehlte_Gruende_ohne_Kind_und_Text`
  grün, `rueckmeldung.spec.ts` Schritt 3 grün.

**Stand:** B-767, B-768, B-769 `abgenommen` (`fcb04f56`).

## Retrospektive (Sprint 151)

**Nachschau:** Sprint 140 (B-954, `ddf6d1d6`) – Prüfpunkt: `WalletService.cs:99` filtert `Amount > 0 && EarnedCoinKinds`;
gegen die neue positive `GoalPenalty` aus B-768 gerechnet (zählt nicht als „Münzen heute“, gemessen in
`Malus_wird_durch_eine_positive_Gegenbuchung_zurueckgenommen`). Kein Befund, `nachgeschaut: 2026-11-24` gesetzt.
Index: Nachgeschaut 575 von 613 vor diesem Lauf (`backlog-index.sh`).

**Was die eigenen Tore durchgelassen haben:** (1) Die Versuchsansicht las nach der Korrektur weiter den alten Befund. Ich
hatte „jeden Leser von `Passed`“ gegrept, aber nur die buchenden und zählenden umgestellt und die Ausgaben als „Anzeige“
abgetan; der Reviewer fand sie. (2) Die zwei neuen 409-Codes hatten keinen deutschen Text. Das Tor
`ProblemTextGuardTests` hätte es gefangen, der erste volle Lauf aber lief, während ich die Texte schon nachtrug. (3) Die
Rückfrage vor „Berechtigt“ war ohne Fall „Abbrechen“. Von außerhalb der Schleife kam nichts.

**Vorgeschlagener Mechanismus (nur Vorschlag, Freigabe 3):** (DB) eine Zeile in pm-loop Step 4 neben der
Aufrufer-Regel: „Steht eine abgeleitete Wahrheit neben einem gespeicherten Befund (Korrekturspur, Rücknahme), entscheidet
jede **Ausgabe** des Befunds (DTO) wie jeder Leser – grep die Response-Records, nicht nur die Abfragen.“ Kein Tor, weil
sich „abgeleitete Wahrheit“ nicht mechanisch erkennen lässt.

**Bewusst ungedeckt:** die Malus-Rücknahme im Browser (nur Integrationstest); zwei gleichzeitige Urteile (B-971); der
Text bei „berechtigt“ an einer richtig gewerteten Antwort (B-970); Wochenpflicht mit Wechsel der Taktung zwischen Strafe
und Korrektur (nur gelesen, nicht gemessen).

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Das Paket B-767–B-769 ist durch; der Lauf endet hier.

1. **Zum Umstoßen (Nutzer):** alle Entscheidungen von B-767/B-768/B-769 sind stellvertretend (Vorbereitungslauf
   2026-11-20); dazu die zwei Ableitungen dieses Laufs: „berechtigt“ ist endgültig (`409 feedback_justified`), und eine
   Korrektur nimmt ein Bestehen nie weg (bei inzwischen angehobener Schwelle).
2. **Vor dem Heimnetz-Start:** die Migrationen `FeedbackAnswerOutcome` und `FeedbackCorrection` beim Merge nach `main`
   gegebenenfalls neu erzeugen (parallele Läufe J/L hängen eigene an); an der Kopie geprobt.
3. B-970 (Frage) und B-971 (Defekt, P3) als Ideen.

## Konkreter Änderungsstand (für Review)

`fcb04f56` – Code, Tests, Vertrag, generierte Doku, E2E. Danach ein Doku-Commit: B-767–B-769, B-970, B-971, B-954
(Nachschau), dieses Protokoll, Index.
