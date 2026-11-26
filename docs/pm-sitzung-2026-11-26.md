# PM-Sitzung: Abnahme der Lerntage (B-731/B-732 und ihre Review-Defekte)

**Datum:** 2026-11-26  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die im Nachtlauf O gebauten und im Merge-Review nachgebesserten Lerntage (B-731), die Fehlerart (B-732) und
ihre drei Review-Defekte (B-1020…B-1022) gegen die laufende App an einer Kopie der echten DB abnehmen – oder benennen,
was fehlt.

## Runde 1 — Vorstellung & Feedback

Ausgangslage aus [pm-sitzung-2026-11-25-o](pm-sitzung-2026-11-25-o.md): fünf Stories `in-arbeit`, gebaut und committet
(`f4df2984`, `48550d7c`), offen jeweils nur Review über den Fix und Rollengang. Keine neue Anmerkung unter
`docs/anmerkungen/` (Export-Stand 2026-08-14, beide erledigt). Rollengang an der Kopie auf `:5201`.

Der Rollengang dieser Runde lief an der Kopie (Stand `144639e0`, Migrationen beim Start eingespielt) über die API; die
Oberfläche deckt die E2E `lerntage-und-pause` ab (Wegwerf-DB, echte UI). Konten: Vater = Konto 1, Sohn = Friedrich
(Kind 3), Lehrer = Konto 2, fremder Vater = Konto 3.

### Feedback Creator (O-Ton) — Regressionszeuge

Keine Creator-Fläche im Diff. „Für mich hat sich nichts bewegt – meine Übungen spielen wie vorher.“ Beleg: kein
`creator/*`-Pfad in `f4df2984`/`48550d7c`, Suite grün.

### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3

- Gefällt: „Ich trage ‚krank‘ für heute und morgen ein, und sofort droht Friedrichs Tag nichts mehr an – alle 46 Positionen
  `owesDuty: false`, auch die Wochenpflichten 167/168, die vorher `true` standen.“ Der Lehrer bekommt 403, der Demo-Vater
  404, „bis vor von“ ein klares 400.
- Stört: nichts Neues am eigenen Weg. Die Fragen aus Lauf O (B-1023 Hausaufgabe in der Pause) liegen weiter bei ihm.
- Top-3: (1) Lerntage abnehmen, (2) B-1023 entscheiden, (3) — keine weitere aus diesem Gang.

### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

- Mega: „Wenn Papa mich krankmeldet, steht da kein ‚−20 Münzen‘ mehr.“
- Beobachtet an der Kopie: zwei falsch getippte Antworten („eine Jahreszeitt“ statt „une saison“) stehen als `WrongWord`
  in `ItemReviewEvents` – die Fehlerart entsteht beim Antworten (B-732).
- Nervt (Fund des Reviews, nachgestellt): ein Plan, den er noch nie geöffnet hat, droht mit Malus, den der Server nie
  bucht (B-535: abgerechnet wird erst ab dem ersten Spiel im Plan). Offen als Frage B-1024.

## PM-Synthese & Priorisierung (→ Entwickler)

Zwei Reviewer (`pugling-reviewer` über `f4df2984`, `frontend-reviewer` über beide Commits) und der Gang ergeben:

| Prio | Fund | Größe | Wo | Entscheidung |
|---|---|---|---|---|
| P1 | `owesDuty` ohne das Erst-Spiel-Tor (B-535): nie gespielter Plan droht Malus an | XS | backend | gebaut, zurückgenommen → Frage B-1024 |
| P1 | Entscheidung 2 von B-1021 (Zählfenster ab dem ersten Pflichttag) ohne Test | XS | backend | Test ergänzt |
| P2 | `PlanDueForPeriod` in `owesDuty` ohne Test | XS | backend | Test ergänzt |
| P2 | Gruppe „Leitplanken“ heißt für Screenreader auch „Erklärung zu …“ (B-236) | XS | frontend | in B-1020 behoben |
| P2 | E2E-Banner-Probe hängt am Timing | XS | frontend | Wartepunkt „Leitplanken entfernen“ |
| — | toter Zweig `DutyUntil` in `OwesDutyOn` | — | backend | kommentiert (spiegelt die Abrechnung), nicht entfernt |
| — | Aufräum-Vorschläge (eslint-disable, feste id, doppelter Titel, E2E für `owesDuty:false`) | — | frontend | nicht blockierend, nicht gebaut |

**Roter Faden:** Die Arcade droht nur, was der Server bucht – jede Schranke der Abrechnung steht auch in `owesDuty`.

## Sprint 192 — Ziel & Umfang

**Sprint-Ziel (Sohnsicht):** An einem Tag, an dem mir nichts abgezogen werden kann – Sonntag, Pause, eine Woche, in der Papa
den Plan erst mittendrin eingeschaltet hat –, zeigt mir die App weder Frist noch Malus. Widerlegt, wenn
`GET student/children/{id}/today` in einem dieser Fälle `owesDuty: true` meldet. (Der nie geöffnete Plan stand zuerst im Ziel;
er ist eine Produktfrage, B-1024 – siehe Iteration.)

**Umfang:** B-1020, B-1021, B-1022 (Nacharbeit aus dem Review) und die Abnahme von B-731, B-732.

**Entwickler-Brief:** *Quelle der Wahrheit* `SettleClosedPeriodsAsync`; `OwesDutyOn` übernimmt ihre Schranken. *Guards*
unverändert. *Migration* keine. *Testweg* `LerntageTests` (+2), `LerntagePanels.test.tsx` (+1), E2E `lerntage-und-pause`, `pfad`.

## Iteration 192 — umgesetzt

- **Backend:** Test für Entscheidung 2 von B-1021 und für `PlanDueForPeriod` in `owesDuty`; `OwesDutyOn` benennt die eine
  bewusste Abweichung von der Abrechnung (Erst-Spiel-Tor), die Vertragsdoku ebenso. Das Tor selbst war gebaut (erst mit
  `FirstPlayDaysAsync` je Tag – rot in `SchreibwegMessungTests`, eine zweite Tagessicht-Abfrage –, dann aus der vorhandenen
  Abfrage) und wurde zurückgenommen, als `e2e/pfad.spec.ts` zeigte, dass jeder frische Plan dann ohne Frist erscheint: das
  entscheidet der Vater (B-1024), nicht der Lauf.
- **Frontend:** Gruppenname „Leitplanken“ ohne den ⓘ-Namen (B-236); E2E wartet auf „Leitplanken entfernen“ statt nur auf die
  `GET`-Antwort.
- **Mutanten** (Endstand, `.claude/scripts/mutant-probe.py`):

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| M2 `PlanDueForPeriod` aus `OwesDutyOn` | `PositionProgressService.cs:455` | red | red |
| M3 Zählfenster nur am Einführungstag | `PositionProgressService.cs:883` | red | red |
| M4 Gruppen-`id` an der Zeile (von Hand, altes Markup) | `ChildCarePanels.tsx:357` | red | red |

  Die einzeilige Fassung von M4 (doppelte `id`) blieb grün – happy-dom löst die zweite auf; kein Beleg, darum von Hand mit
  dem vollständigen alten Markup gemessen.

**Verifikation am Endstand `4e70c583`:** Backend-Suite 2609/2609 (Release; ein Zwischenlauf unter Last hatte drei rote, die
einzeln grün liefen – `SchreibwegMessungTests.Messung_Verlauf`, `OpenApi_Schema…`, `Vertragsdokument…`), `vitest` 1514/1514,
`tsc -b` grün, E2E `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). `pugling-reviewer` über den Endstand: kein Fund.

## Runde 2 — Re-Review / Abnahme

Server der Kopie nach der letzten Änderung neu gestartet (`kopie-server.sh`, frische Kopie, `:5201`).

- **Vater — abgenommen.** „Pause für heute eingetragen: alle acht Pflichten meines Sohns drohen nichts mehr; Pause gelöscht:
  alles wieder scharf. Der Lehrer kommt nicht ran, der Demo-Vater sieht das Kind nicht.“ Die Maske (Pause + fünf Leitplanken)
  fährt die E2E `lerntage-und-pause` mit build + read-back. Offen bei ihm: B-1023 und neu B-1024 (beides Fragen).
- **Sohn — abgenommen.** „Krank heißt: keine Drohung.“ Seine getippten Fehler stehen mit Art in der Datenbank:
  `la rentree` → *Akzent* (27×), `savoi`/`quix` → *Tippfehler*, `eine Jahreszeitt` → *falsches Wort*; keine falsche getippte
  Antwort ohne Art. Gefühl und Klang nicht berührt.
- **Creator — Regressionszeuge.** Kein `creator/*`-Pfad im Diff, Suite grün; mehr hat er nicht getan.

Abgenommen: B-731, B-732, B-1020, B-1021, B-1022.

## Retrospektive

**Nachschau:** B-377, B-593, B-710, B-711, B-712 (Läufe M/N) – nachgesehen hat sie das Merge-Review vom 2026-11-26 und die
Funde direkt behoben (`61a11f55`, `5446640c`), ohne Defekt-Story. Nachgeholt: **B-1025** (`entgangen_bei: B-712`, „keine Lösung“
nur in einer Schreibweise) und **B-1026** (`entgangen_bei: B-593`, zu weiter Fang → 404, Vorlesename ohne sichtbaren Text);
`nachgeschaut: 2026-11-26` an allen fünf. B-624 war schon am 2026-11-25 nachgesehen.

**Was die eigenen Tore durchließen bzw. fingen:**

- Gefangen hat alles ein Tor, nichts kam von außen: das Erst-Spiel-Tor (Review-Vorschlag) machte zuerst
  `SchreibwegMessungTests` rot (zweite Tagessicht-Abfrage), in der zweiten Fassung `e2e/pfad.spec.ts` (jeder frische Plan ohne
  Frist). Die Schwäche lag vor den Toren: ein Review-Fund der Form „Verhalten ändern *oder* Ausnahme dokumentieren“ wurde als
  Reparatur gelesen, obwohl er sichtbares Verhalten des Sohns entschied.
- Werkzeug: eine einzeilige Mutante mit doppelter `id` blieb grün (happy-dom löst die zweite auf) – eine „grüne Mutante“ ist
  keine Entwarnung, das alte Markup musste vollständig zurück.

**Der eine Mechanismus:** Regel in `pm-loop` Step 5 (Prosa im treibenden Skill, keine resident geladene Datei): ein Fund
„ändern oder dokumentieren“, der rollen-sichtbares Verhalten betrifft, wird dokumentiert und als `Frage` abgelegt. Kein Tor –
das Urteil „sichtbar für eine Rolle“ ist nicht mechanisch zu fassen; die E2E bleibt das Netz dahinter. Kein neues Tor, also
kein Halt aus diesem Grund.

**Bewusst ungedeckt:** Ob eine nachgeholte Nachschau (Merge-Review statt Retro) ihre Funde als Story ablegt, prüft weiterhin
niemand; der Index zählt nur `nachgeschaut`.

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

Siehe `docs/backlog/`. Für den Vater liegen zwei Fragen: **B-1023** (Hausaufgabe in der Pause) und **B-1024** (Drohung vor dem
ersten Spiel). Danach ist B-644 die nächste Etappe (B-733 Tagesauswertung baut auf der jetzt gespeicherten Fehlerart auf).

Änderungsstand: `4e70c583` (Code), danach ein Doku-Commit (Backlog, Protokoll, `pm-loop`-Regel). Ungepusht.

**Nachtrag (2026-11-26):** Der Vater hat B-1024 entschieden – ein noch nie geöffneter Plan droht weiter Frist und Malus
an. B-1024 `verworfen` mit Grund.

**Nachtrag 2 (2026-11-26):** Der Vater hat B-1023 entschieden – eine Hausaufgabe mit Fälligkeit in einer Pause kostet keinen
Malus. Gebaut und abgenommen in `9cf14491` (Suite 2611/2611, Review ohne Fund, Rollengang mangels Hausaufgaben-UI durch den
Integrationstest ersetzt). Keine Frage aus diesem Sprint ist mehr offen.

## Runde 3 — Rollengang an der Kopie

Neuer Gang an einer frischen Kopie (`kopie-server.sh`, `:5201`, Stand `3855ab9e`). Keine neue Anmerkung unter `docs/anmerkungen/`.
Konten: Vater = Konto 1, Friedrich = Kind 3, Theodor = Kind 1.

### Feedback Creator (O-Ton) — Baut sich gut / Fehlt-nervt / Top-3

- Beobachtet: acht offene „zu streng“-Meldungen der Kinder seit dem 19.11., keine bearbeitet. Drei davon („turn smt. up/down“,
  „turn to smb“) betreffen Platzhalter-Kürzel, die seit B-990 angenommen werden – die Kinder liefen auf dem älteren Heimnetz-Stand.
- „Ich arbeite die Meldungen im Rückmeldungs-Eingang ab; das ist meine Sache, nicht die der App.“ Top-3: (1) Meldungen richtig
  angezeigt bekommen (siehe Vater), (2) B-630 KI-Creator liest Rückmeldungen, (3) –.

### Feedback Vater (O-Ton) — Gefällt / Stört-fehlt / Top-3

- Stört (beobachtet, **Defekt**): „Theodor meldet ‚zu streng‘ bei *amazing*, und da steht Lösung *amazing*, Antwort *amazing*.“
  Tatsächlich tippte er „amazong“ (21:40:12), meldete (21:40:17) und tippte danach richtig (21:40:56); der Eingang zeigt die
  jüngste Antwort statt der gemeldeten → **B-1027**.
- Stört: „Bei Friedrich steht *une côte* mit 0 % unter den schwachen Wörtern – er hat es 36 von 38 Mal gewusst, einmal ‚un côte‘.
  Warum es hakt, sehe ich nirgends; ich sehe nur eine Prozentzahl.“ Die Fehlerart wird seit B-732 gespeichert, aber keine Fläche
  zeigt sie → **B-733** (Tagesauswertung, Problemwörter mit Fehlerart).
- Top-3: (1) B-1027, (2) B-733, (3) offene Entscheidungen B-743 (AK 2), B-752 (Fairness), B-780 (Münzen).

### Feedback Sohn (O-Ton) — Mega / Nervt / Top-3

- Friedrichs Tag an der Kopie: 7 offene Pflichten, keine Drohung an freien Tagen, nichts Neues am eigenen Weg.
- Nervt (aus den Meldungen): „Ich hab ‚smt.‘ geschrieben wie in der Schule“ – erledigt durch B-990, sobald das Heimnetz neu startet.
- Top-3: keine neue Story aus diesem Gang.

## PM-Synthese & Priorisierung (Runde 3)

| Prio | Fund | Größe | Wo | Entscheidung |
|---|---|---|---|---|
| P1 | Rückmeldung zeigt eine Antwort nach der Meldung (B-1027) | XS | backend | Sprint 193 |
| P1 | Vater sieht nicht, warum ein Wort hakt; Tagesauswertung fehlt (B-733) | L | beides | Sprint 193 |
| P2 | acht offene Meldungen | — | Bedienung | beim Vater, keine Story |
| — | B-743, B-752, B-780 | — | — | Fragen an den Nutzer |

**Roter Faden:** Der Vater urteilt über das, was das Kind wirklich getan hat – die gemeldete Antwort, die Fehlerart je Wort, der Tag.

## Sprint 193 — Ziel & Umfang

**Sprint-Ziel (Vatersicht):** Ich sehe je Kind, Fach und Tag, was gelernt wurde und welche Wörter mit welcher Fehlerart hakten, und
eine Meldung zeigt mir genau die Antwort, über die sich mein Kind beschwert hat. Widerlegt, wenn der Eingang eine spätere Antwort
zeigt, ein Pausentag als Null erscheint oder ein Problemwort ohne seine gespeicherte Art.

**Umfang:** B-1027, B-733.

**Entwickler-Brief:** *Quelle der Wahrheit* `ItemReviewEvent` (Antwort, Fehlerart, `BoxBefore`), `ItemProgress.IntroducedAt`,
`PracticeSession.ActiveSeconds`, `PositionProgressService.ComputeDayAsync` (Pflicht), `DutyCalendar` (freie Tage). Abgeschlossene
Tage werden einmal gespeichert (`DailyEvaluation`, Wasserstand am Kind), nie neu gerechnet. *Guards* Supervisor-Gate, Ownership-
Filter, kein Student-Typ. *Migration* eine (Tabelle + Wasserstand). *Testweg* `ExerciseFeedbackTests` (+2), `TagesauswertungTests`,
Vitest der Maske, E2E `auswertung-verlauf`.

## Iteration 193 — umgesetzt

- **B-1027 (Backend):** `ExerciseFeedbackService.ChildAnswerAsync` liest Test- und Übungsantworten nur bis `LastReportedAt`;
  Vertragsdoku nachgezogen. Zwei Fälle in `ExerciseFeedbackTests`.
- **B-733 (Backend):** Tabellen `DailyEvaluations` + `EvaluatedDays` (Migration `Tagesauswertung`, rein additiv), Dienst
  `DailyEvaluationService` (ein abgeschlossener Tag wird einmal gerechnet und gespeichert, heute live), drei Endpunkte unter
  `supervisor/children/{id}/evaluations`, Client, Reflexions-Tor `Auswertungstypen_Erreichen_Keinen_Studenten`.
- **B-733 (Frontend):** Seite `/vater/kind/:id/auswertung` (Link „🔎 Auswertung“ im Kind-Hub): Profil je Fach (7/28 Tage),
  Verlauf mit ausdrücklichen Lücken, Problemwörter mit Fehlerart und letzter getippter Antwort.
- **Review-Funde eingearbeitet:** (🔴) angezeigte Kennenlern-Karten, Lernkarten und erleichterte Abrufe zählten als falsche
  Antworten und Problemwörter – jetzt zählt nur eine Antwort mit Urteil (`BoxBefore`), für Zeilen von vor B-732 getippt und keine
  Lernkarte; Test über die echte Sohn-API. (🟡) „neu“ aus dem Einführungstag der Position statt aus dem plan-übergreifenden
  Stempel. (🔴 Frontend) das Fenster rechnete „heute“ in UTC – jetzt `heuteImLerntag()`, Fenster an den Server, keine Zeile geht
  verloren. Dokumentiert statt gebaut: abgeschaltete Pläne fehlen beim ersten Nachrechnen.
- **Bewusst ausgelassen (eigene Stories):** B-1028 (Antworten des Vaters landen im Lernstand des Kindes – Schreibseite, älter als
  B-733) und B-1029 (zwei Meldungen zu zwei Übungsantworten verschmelzen).

**Verifikation am Endstand:** Backend-Suite 2623/2623 (Release, 9 min 41 s; ein früherer Lauf brauchte 20 min, ohne dass ein
einzelner Test hing – nicht dieser Änderung zuzuordnen, gemessen mit `--blame-hang`), Vitest 1518/1518, `tsc -b` grün, E2E
`auswertung-verlauf`, `pfad`, `lerntage-und-pause`, `meldung-berechtigt` passed (Ports 5244/5284). Mutanten: B-1027 2/2 rot,
B-733 22 rot wie erwartet + 2 erwartet grün (Tabellen in den Stories). Reviews: `pugling-reviewer` und `frontend-reviewer`, Funde
eingearbeitet bzw. als B-1028, B-1029 abgelegt.

## Runde 4 — Re-Review / Abnahme

Kopie-Server nach der letzten Änderung frisch gestartet (`kopie-server.sh`, frische Kopie, Migration `Tagesauswertung` beim Start
eingespielt – zugleich die Migrationsprobe an echten Daten).

- **Vater — abgenommen.** „Theodors Meldung zu *amazing* zeigt jetzt ‚amazong‘ – genau das, was er getippt hat; ‚zu streng‘ war es
  nicht.“ Auswertung für Friedrich: zwölf Tage mit Zeilen, Sonntag 22.11. geübt ohne Pflicht, 25./26.11. verpasste Pflicht ohne
  Übung als Zeile, Tage vor dem Planstart als Lücke. Problemwörter: „une côte – 2 falsch / 36 richtig, zuletzt ‚un côte‘“ – „jetzt
  sehe ich, dass es nur der Artikel ist“. Erster Aufruf 2,2 s, danach 0,13 s. Lehrer 403, Sohn 403. **Benannt offen:** gefestigt und
  Tempo stehen an allen alten Tagen auf 0, weil die Historie vor B-732 keine Box trägt → B-1031; die Fehlerart fehlt an Testantworten
  → B-1030. Die Oberfläche (Verlauf mit Lücke, Problemwort mit getippter Antwort) fährt die E2E `auswertung-verlauf`.
- **Sohn — Regressionszeuge.** Kein Student-Pfad im Diff; sein Tag lädt an der Kopie (200), die Auswertungspfade antworten ihm 403;
  `pfad`-E2E grün.
- **Creator — Regressionszeuge.** Kein Creator-Pfad im Diff; Suite grün.

Abgenommen: B-1027, B-733.

## Retrospektive

**Nachschau:** B-1023 (Commit `9cf14491`) und B-732 nachgesehen. B-1023: der zweite Leser (`HomeworkResponse.PenaltyCoins`) ist schon
als Hinweis an B-763 vermerkt, keine Oberfläche zeigt ihn – kein neuer Defekt. B-732: an echten Wegen trägt eine getippte
Testantwort keine Art → **B-1030** (`entgangen_bei: B-732`), dazu B-1027 (`entgangen_bei: B-628`, aus dem Gang). `nachgeschaut` an
B-1023 und B-732. Index: 589 von 636.

**Was durchrutschte bzw. gefangen wurde:**

- Gefangen vom Review, nicht von den eigenen Tests: meine Tests säten Antwortzeilen von Hand und verfehlten, dass die Übung Zeilen
  ohne Urteil schreibt (Kennenlern-Karte, Lernkarte). Erst der Leser-gegen-Schreiber-Abgleich des Reviewers fand es. Jetzt trägt ein
  Fall den echten Schreibweg.
- Gefangen vom Frontend-Review: „heute“ als UTC-Tag – trotz vorhandenem `heuteImLerntag()` mit genau dieser Warnung.
- Gefangen von einem Tor: `leerfall.test.ts` (Leerfall am Bestand statt an der Seite).
- **Durchgerutscht bis zur Handprüfung:** eine abgebrochene Mutanten-Probe ließ Mutante C9 im Quelltext stehen; die Sperrdatei
  blockierte den nächsten Lauf, sagte aber nichts über die Zeile – und ihr eigener Rat („die Datei löschen“) hätte den einzigen
  Hinweis darauf gelöscht.

**Der eine Mechanismus (neues Tor):** `mutant-probe.py` liest beim Start die Mutantendatei aus einer vorhandenen Sperre und bricht ab,
solange eine ihrer Mutanten noch im Quelltext steht; die Meldung nennt Datei, Zeile und Sollwert – auch in der Meldung „eine andere
Probe läuft“, damit das Löschen der Sperre den Hinweis nicht mitnimmt. Von Hand rot (verwaist und frisch) und grün (zurückgesetzt)
belegt. **Neues Tor → der Lauf hält hier**, der Nutzer sieht es zuerst.

**Bewusst ungedeckt:** Ein Test, der den Lesern einer Historie nur von Hand gesäte Zeilen gibt, bleibt möglich; das fängt weiterhin
nur ein Review.

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

Siehe `docs/backlog/`. Aus diesem Sprint neu: B-1030 (P2, Fehlerart am Testweg), B-1031 (P2, Tempo vor B-732), B-1028 (P2, Antworten
des Vaters im Lernstand), B-1029 (P3). Danach geht B-644 mit B-734 (Planer im Schattenmodus) weiter, das das Profil aus B-733 liest.
Beim Vater liegen weiter die Fragen B-743 (AK 2), B-752 (Fairness am Gerät), B-780 (Münzen).

**Nachtrag (2026-11-26):** B-1030 nachgestellt und **verworfen** – kein Defekt. Der Test fragt ein am selben Tag eingeführtes Wort
auf der Auswahlstufe ab (auch mit `requireTypedTest`), und eine Auswahl trägt nach B-732 bewusst keine Fehlerart. Die E2E hatte per
API Freitext an eine Auswahlkarte geschickt. Fehlerarten entstehen an Wiederholungstagen; die E2E zeigt am ersten Tag zu Recht „–“.
Die Retro-Nachschau zu B-732 bleibt damit ohne Fund.
