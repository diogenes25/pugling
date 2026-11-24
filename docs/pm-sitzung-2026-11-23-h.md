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

## Sprint 131 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn findet im Bonus-Training einen Einstieg zur Mischrunde, sobald Karten fällig sind, spielt
Karten aus mehreren Fächern in einer Runde und sieht am Ende, was sie gebracht hat und wo sein Tagesdeckel steht – und
warum es keine Münzen gab, wenn es keine gab.
**Umfang:** B-766 (M, frontend).
**Entwickler-Brief:** Quelle der Wahrheit bleibt der Server (B-764/B-765); die Oberfläche liest `MixedReviewOverview`,
startet über `POST runs` und gibt am Stück ab. Einstieg nur im offenen Zweig von `SohnBonus`, er liest die Sperre trotzdem
selbst. Start als Mutation (`useAction`, `disabled={busy}`, `StatusBanner`), die gestartete Runde reist als Router-State
und wird beim Lesen verbraucht. Spielseite nach dem Muster `SohnBonusRun` (Ref-Gate, `sessionStorage` je Runde, Fokus
B-671/B-778). Typen nur als `S[…]`-Aliase aus dem erzeugten Vertrag. Testweg: Vitest `SohnMischrunde.test.tsx`, E2E
`mischrunde.spec.ts` als Rollengang (Ausweichports 5221/5194), `frontend-reviewer`.

## Iteration 131 — umgesetzt

**Frontend:** `SohnMischrunde.tsx` (Einstieg, Spielseite, Ergebnis), Route `bonus/misch`, Client und Typen, i18n `mix`
de/en, Mock in `SohnBonus.test.tsx`. **Verifikation:** `tsc -b` grün; Vitest 1396/1396; Mutanten 10/10 rot am benannten
Test; E2E `mischrunde` + vier Bonus-Specs 5 passed. Der Wächter `liveRegions.test.ts` war einmal rot (Live-Region nach
`return null`) und ist behoben. Ein Fokus-Fall in `SohnBonus.test.tsx` war in einem Paketlauf einmal rot und ließ sich
nicht reproduzieren (allein 34/34, im Paket dreimal grün) – Code dort unberührt, ohne Zählung.

**Review:** `frontend-reviewer`: keine 🔴, drei 🟡 behoben (Router-State nicht verbraucht; falscher Grund bei nur
selbst eingeschätzten Treffern; Spielseite ohne Vitest), 🟢 teils übernommen (B-766 Verlauf).

**Fehlerzähler Sprint 131: 4** – roter Wächter `liveRegions` und die drei 🟡. Unter 5.

## Runde 131 — Re-Review / Abnahme

**Rollengang (Sohn, E2E `frontend/e2e/mischrunde.spec.ts` gegen den echten Server, Wegwerf-DB, Ports 5221/5194):** zwei
Pläne in zwei Fächern, die Wörter wie im Spiel gelernt und den Test bestanden; im Bonus-Training steht „4 fällige Karten
aus all deinen Plänen – freiwillig, ohne Malus“, die Runde mischt beide Fächer, das Ergebnis sagt „4 / 4 richtig“,
„Heute verdient: 0 / 90 🪙“ und warum es keine Münzen gab (heute gelernt), danach ist der Einstieg weg.

- **Sohn:** abgenommen – „Ich seh im Bonus-Training, dass was fällig ist, spiel beide Fächer in einem Rutsch, und am Ende
  steht da, warum's heute keine Münzen gab." Bleibt fürs Gerät: wie sich eine volle Runde mit zehn Karten anfühlt.
- **Vater (Regressionszeuge):** die Bonus-Specs (Deckel je Kind, voller Deckel, Satz je Karte) laufen unverändert grün;
  kein Vater-Pfad im Diff.
- **Creator (Regressionszeuge):** kein Pfad im Diff; Vitest gesamt grün.

## Retrospektive (Sprint 131)

**Nachschau:** Sprint 130 (B-764, B-765) nachgesehen – Prüfpunkte und Ergebnis in beiden Verläufen; kein Befund, keine
Entgleitung; `nachgeschaut: 2026-11-24` gesetzt. Ein Fund des Frontend-Reviews (das Ergebnis trägt keinen Grund) liegt am
Vertrag, nicht an der Wertung, und ist als möglicher späterer Vertragsschritt notiert.

**Was die eigenen Tore durchgelassen haben:** zweimal derselbe Kniff: ein Wert, der eine Seite verlässt und später
zurückkommt (Router-State im Verlauf; das Ergebnis, das einen Grund herleitet, den der Server nicht nennt). Der Wächter
`liveRegions` hat seinen Fall sofort gefangen – das Tor funktioniert.

**Vorschlag (H2), nicht gelandet (Freigabe 3):** eine Zeile in `frontend/CLAUDE.md`: „Router-State, der eine Mutation
mitbringt (gestarteter Lauf, Antwort), wird beim Lesen per `replace` verbraucht – der Browserverlauf hält ihn sonst über
die Mutation hinaus." Kein Tor: die Stellen mit `location.state` sind wenige und lassen sich nicht mechanisch von harmlosen
unterscheiden.

## Offene Roadmap · Konkreter Änderungsstand

Das Paket ist fertig: B-764, B-765, B-766 `abgenommen`. Der Lauf endet hier (Auftrag: nichts außerhalb der Liste).

**Zur Entscheidung am Morgen** (keine davon blockiert, alle in den Verläufen der Stories):

1. B-765 – Erweiterung des Zahl-Tors: eine Karte zahlt nur, wenn das Kind ihr heute noch nicht begegnet ist (eingeführt,
   wiederholt oder im Test gefragt). Folge: am Lerntag selbst bringt die Mischrunde nur den Kasten voran.
2. B-765 – „Karte ohne Antwort zählt als falsch" (Muster des Bonus-Trainings) oder „übersprungen"? Offener Punkt fürs
   Grillen.
3. B-765 – eine selbst eingeschätzte Karte bewegt den Kasten wie die Selbstauskunft im Üben und zahlt nichts (das
   Bonus-Training zahlt dort 1).
4. B-764 – Auslegungen (a)–(f), vor allem „nur eingeführte und eingeprägte Karten" und „ein geteiltes Wort einmal je
   Runde".
5. Retro-Vorschläge (H1) und (H2) – landen oder verwerfen.

**Commits (Branch `worktree-agent-a0aab32232d36b515`, Basis `a782b4b2`, ungepusht):** `64c14761` B-764 · `1e3b42f7`
B-765 · `d00b56d4` Sprint-130-Abnahme · `41c720d0` B-766 · danach der Abschluss-Commit dieses Protokolls.
