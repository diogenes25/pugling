---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf H – Mischrunde über alle Pläne

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Mischrunde aus B-218 bauen: fällige Karten über alle spielbaren Pläne in einer freiwilligen Runde, mit
Kasten der Herkunftsposition, Münzen unter dem gemeinsamen Bonus-Deckel und einem Einstieg in der Sohn-App.

Auftrag: „Starte 3 Nachtläufe" – Lauf H, gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den
Freigaben 1–11. Paket fest vorgegeben: B-764, B-765, B-766 in dieser Reihenfolge; nichts außerhalb. Eigener Worktree
(Branch `worktree-agent-a0aab32232d36b515`, Basis `a782b4b2`), parallel zu den Läufen G und I. Story-Ids nur aus
B-920 … B-939, Sprints 130–132, API-Port 5221, Vite 5194; :5200 (Heimnetz, echte DB) bleibt unberührt.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/` aufgenommen (Paket ist vorgegeben). Alle drei Stories sind `geschaetzt` +
`Wunsch` – baubar nach Freigabe 8. Ihre Entscheidungen stammen aus dem Vorbereitungslauf vom 2026-11-20 (Sonnet,
„stellvertretend, unbestätigt"); die Stories tragen sie als nummerierte `## Entscheidungen`, also gilt Freigabe 8 und
nicht Freigabe 9. Vorbedingung B-660 ist `abgenommen`. Die fremde Arbeit an `SohnApp.tsx` (B-766 Ist-Stand) ist
seit dem 2026-11-20 committet (Worktree sauber an `a782b4b2`).

| Sprint | Stories | Thema |
| --- | --- | --- |
| 130 | B-764, B-765 | Mischrunde im Backend: Auswahl, Lauf, Wertung, Münzen |
| 131 | B-766 | Mischrunde in der Sohn-App |

Zwei Sprints statt einem, weil Backend und Oberfläche getrennt prüfbar sind und der Fünfer-Zähler je Sprint so nicht
von drei Stories gemeinsam gespeist wird.

## Sprint 130 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn kann nach seinen Tageszielen per API eine Mischrunde starten, die fällige, schon gelernte
Karten aus mehreren Plänen zieht (keine mit offener Pflicht), sie beantworten, und jede Karte rückt im Kasten ihrer
eigenen Position – mit Münzen nur für geprüfte Treffer und höchstens bis zum Deckel, den er mit dem Bonus-Training teilt.
**Umfang:** B-764 (M, backend, Migration), B-765 (L, backend).
**Entwickler-Brief:** Quelle der Wahrheit für „offen/gesperrt" und „offene Pflicht" ist **eine** Tagessicht
(`PositionProgressService.ComputeChildDayAsync`): die Sperre liest sie über `ExtraTrainingService.GateOf`, die Auswahl
schließt jede Position mit `Cadence != None && !GoalMet` aus (Woche = laufende Periode). Neue Tabellen `MixedReviewRuns`
und `MixedReviewCards` (Migration `MixMixedReviewRuns`), ein offener Lauf je Kind per gefiltertem Unique-Index. Gewertet
wird nur über die geteilten Bausteine: `ExercisePreviewService.CheckAsync` (gruppiert nach Übung und Stufe) und
`PositionPlayService.ApplyReview` auf der `PositionItemProgress` der Herkunftsposition – der
`PositionPracticeController` bleibt unberührt, ebenso die Aufbau-Stufen-Logik (gelesen nur über `PlayedExercise`).
Senke der Münzen ist `ExtraTrainingService.CoinsTodayAsync`: sie summiert beide Quellen, damit Bonus-Training und
Mischrunde denselben Deckel sehen. Testweg: `MischrundeAuswahlTests`, `MischrundeWertungTests`, ein Fall in
`ExtraTrainingTests`, Mutanten-Probe je Fall, `pugling-reviewer`.

## Iteration 130 — umgesetzt

**Backend (API-First):** Migration `20261123220000_MixMixedReviewRuns` (zwei neue Tabellen, nur additiv);
`MixedReviewService` mit Auswahl, Start/Fortsetzen und Abgabe; `MixedReviewController` unter
`student/children/{childId}/mixed-review` (`GET`, `POST runs`, `POST runs/{runId}/submit`); Fehlercodes
`mixed_review_empty`/`_completed`/`_stale` samt deutschen Texten in `frontend/src/lib/api.ts` und i18n;
`ExtraTrainingService.GateOf` herausgezogen, `CoinsTodayAsync` über beide Quellen; Contract und Client. Kein
Vertragsbruch. Die Aufbau-Stufen-Logik ist nur gelesen (`PlayedExercise`, `PlayedExerciseId`, `TestTargetStage`).

**Verifikation:** Suite (Release) vor dem Review-Fix 2203/2203, nach dem letzten Fix 2204/2204; neue Klassen
`MischrundeAuswahlTests` (12) und `MischrundeWertungTests` (17), gemeinsame Hilfen `MischrundeTestBase`.
Mutanten-Probe nach der letzten Änderung: B-764 12 Mutanten (10 rot am benannten Test, 1 erwartet grün, 1 abweichend –
begründet im Verlauf), B-765 19/19 rot. Wächter `ProblemTextGuardTests` einmal rot (3 Codes ohne deutschen Text) und
behoben. In beiden Release-Läufen meldet xUnit nach „Passed!“ einen `Test Assembly Cleanup Failure`
(`TestPipelineException`) ohne fehlgeschlagenen Test – außerhalb des Diffs, nicht untersucht, ohne Zählung.

**Review:** `pugling-reviewer` (ein Lauf für beide Stories): keine 🔴; 🟡 1 behoben (eine in Runde 1 verfehlte Karte
zahlte in Runde 2 desselben Tages – Lösung abgeschrieben); 🟢 ohne Zählung: Leitner bei der Abgabe neu geprüft,
Kommentar richtiggestellt, „ohne Antwort = übersprungen" als offener Punkt zurückgestellt (B-765 Verlauf).

**Fehlerzähler Sprint 130: 3** – (1) roter Wächter `ProblemTextGuardTests`, (2) Review-🟡 „Zahlung nach gezeigter
Lösung", (3) Rollengang-Fund „heute gelernte/getestete Karte zahlt". Unter 5, der Lauf geht weiter.

## Runde 130 — Re-Review / Abnahme

**Rollengang (Sohn-Sitz, Live-Probe gegen die laufende API auf Port 5221, Wegwerf-DB):** Vater legt Kind, Fach, Reihe,
Unit, Vokabelübung und einen Plan mit Leitner-Tagespflicht an; der Sohn lernt die vier Wörter in einer echten
Lernrunde (Lernkarten, Fremdwort aus dem Gedächtnis), besteht den Test, öffnet die Mischrunde (vorher
`extra_training_locked`), startet, setzt fort, gibt ab (4/4), doppelt abgeben wird abgewiesen, die Bonus-Übersicht
zeigt dieselbe Tagessumme, danach ist nichts mehr fällig; der Vater liest die Übersicht, darf aber nicht starten. Der
erste Gang fand die Zahlung für heute gelernte und heute getestete Wörter (12 Münzen fürs Abschreiben) – behoben,
zweiter Gang nach dem Fix: 4/4 richtig, Kästen 1→2 mit Fälligkeit in zwei Tagen, 0 Münzen. Ein Browser-Rollengang
fällt für ein reines Backend-Increment aus (Chrome-Extension gilt als nicht verfügbar); die Oberfläche kommt mit
Sprint 131 und trägt dort die E2E.

- **Sohn:** abgenommen – „Ich kann nach meinen Zielen alles Fällige aus allen Fächern in einer Runde wiederholen, und
  ich krieg Münzen nur für Wörter, die ich mir wirklich gemerkt habe." Vorbehalt: Münzen gibt es erst für Wörter, die
  vor heute gelernt wurden – am Lerntag selbst bringt die Mischrunde nur den Kasten voran.
- **Vater (Regressionszeuge):** liest die Übersicht, kann nicht für das Kind spielen; die Pflicht bleibt unberührt
  (Tests `Runde_Beruehrt_Keine_Pflicht`, `Position_Mit_Offener_Pflicht_Gibt_Keine_Karte`); der Deckel gilt für alle
  Zusatzrunden zusammen.
- **Creator (Regressionszeuge):** kein Pfad der Creator-Ebene im Diff; Suite grün.

## Retrospektive (Sprint 130)

**Nachschau:** `bash .claude/scripts/backlog-index.sh` → „Nachgeschaut: 558 von 590 abgenommenen" (Index danach
zurückgesetzt, `INDEX.md` wird nicht gepflegt). Ohne `nachgeschaut` unter den jüngsten Abnahmen: B-863 (Sprint 125,
Nachtlauf E), B-840 und B-700 (Aufbau-Stufen). B-863 nachgesehen – Prüfpunkt und Ergebnis in ihrem Verlauf, kein
Befund, `nachgeschaut: 2026-11-24`. B-840 und B-700 bewusst **nicht** angefasst: Aufbau-Stufen-Bereich, an dem eine
andere Sitzung arbeitet (Parallel-Regel 1); ihre Nachschau bleibt für den Haupt-Checkout.

**Was die eigenen Tore durchgelassen haben:** Die Akzeptanzkriterien von B-765 kannten „zweimal am selben Tag zahlen"
(Zahl-Tor) – aber nicht „zahlen, nachdem die Lösung heute schon gezeigt wurde". Diese Familie hat in diesem Sprint drei
Zugänge gehabt (Ergebnis einer Vorrunde, Lernkarte, Testergebnis); der Reviewer fand den ersten, der Rollengang den
zweiten und dritten. Kein Tor des Repos fragt bei einer neuen Münzquelle nach den Stellen, an denen das Kind die Lösung
schon gesehen hat.

**Vorschlag (H1), nicht gelandet (Freigabe 3):** in `pm-loop` Step 5 eine Zeile: „Eine neue Münzquelle bekommt je Stelle,
an der das Kind die Lösung am selben Tag schon gesehen haben kann (Lernkarte, Testergebnis, Ergebnis einer früheren
Runde), einen Test ‚Treffer danach zahlt nichts'." Billiger als ein Tor; ein Tor müsste die Schreiber von
`ChildPointsEntries` und die Lösungs-zeigenden Antworten kennen, das kostet mehr, als es fängt. Bleibt ungedeckt:
Lösungen, die außerhalb der App gezeigt werden (Buch).
