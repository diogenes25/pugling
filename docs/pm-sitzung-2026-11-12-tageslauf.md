# PM-Sitzung: Tageslauf – so viele Stories wie möglich in sechs Stunden

**Datum:** 2026-11-12/13  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Das Backlog in sechs Stunden so weit abarbeiten wie möglich, nach `pm-loop` und `docs/backlog/README.md`.

## Auftrag und Freigaben

Auftrag des Nutzers (2026-11-12, wörtlich): „Arbeite in den nächsten 6 h soviele Tasks wie möglich ab. Übernehme auch
meine Rolle bei Fragen im Prozess und beantworte diese nach deinem eigenem Wissen und optimal zum Projektziel."

Gelesen als Backlog-Lauf mit den Freigaben aus [nachtlauf.md](nachtlauf.md) **plus** einer weiteren: wo das Verfahren
„im Dialog" verlangt (Retro-Vorschläge, Halt nach einem neuen Tor, `Frage`/`Wunsch` grillen), entscheide ich an Stelle
des Nutzers – jede solche Entscheidung steht hier als **„(stellvertretend)"**, damit sie sich umstoßen lässt. Was ich
trotzdem nicht stellvertretend tue: Push, Neustart des Heimnetz-Servers, Messungen gegen die echte `pugling.db`.

Branch `auto/fertigstellung`, Start bei `5a5cdd5`.

## Stellvertretende Entscheidungen zu den offenen Retro-Vorschlägen

| Vorschlag | Entscheidung | Grund |
| --- | --- | --- |
| (CT)+(CY) Sätze derselben Art greppen | **gelandet** (pm-loop Step 5, ein Satz) | dreimal Nachschau-Fund derselben Klasse |
| (CU) Mutanten in `Debug` | **gelandet** (Step 5, mit (DD)) | gemessen: das Test-Tor sperrt die Release-DLLs |
| (CV)+(CZ) Ausweg gegen alle Sperren und das Recht des Lesers | **gelandet** (Step 5: „that this reader may take") | Sprint 48 fand es ein weiteres Mal (Grundbestand) |
| (DD) Reviewer und Mutanten nie gleichzeitig | **gelandet** (Step 5) | Sprint 47: Tabelle UNGÜLTIG |
| (CW) Leerfall nennt die gemessene Größe | verworfen als eigene Regel | B-505 legt den Leerfall an den Bestand (Tor `leerfall.test.ts`); die Größe nennt der Satz dann ohnehin |
| (CX) rote Probe liest die Meldung | verworfen | `mutant-probe.py` hat dafür `fails` – die Probe muss am genannten Test fallen |
| (DA) Tor gegen „du" in Supervisor-Codes | aufgeschoben | neues Tor ohne Anlass seit Sprint 44; bei nächstem Fund messen |
| (DC) Tor gegen `as unknown as` in Fixtures | verworfen | Teil-Fixtures brauchen den Cast; ein Tor erzwänge vollständige Attrappen ohne Gewinn |

## Sprint 48 — Ziel & Umfang

**Sprint-Ziel:** Der Creator erfährt an der Konsole vor dem ersten Modellaufruf, dass eine Reihe ohne Fach keine Übung
aufnimmt – mit einem Ausweg, den er nehmen darf.
**Umfang:** [B-508](backlog/B-508-konsole-reihe-ohne-fach-endet-am-400.md) (Refinement idee → geschaetzt, autonom gegrillt,
`art: Defekt`).
**Entwickler-Brief:** Senke = `ExerciseControllerBase.Create` (`series_without_subject`). `BuildRequestAsync(…, writes)`
stoppt jedes schreibende Verb, Trockenlauf eingeschlossen; `briefing` behält den Rückfall auf das Profilfach. Die Meldung
an einer Stelle. Keine Migration, kein Vertragsbruch.

## Iteration 48 — umgesetzt

- `BuildRequestAsync` mit `writes`; `ProfileFit.NoSubject` (drei Zweige: eigene Reihe → Vater-Web, Grundbestand → nur
  `--series`, fremd → Eigentümer oder `--series`). Tests: Theory über sechs Verb-Varianten, Briefing, fremde Reihe,
  Grundbestand.
- Verifikation: `dotnet test Pugling.sln -c Release` 1428/1428; Mutanten M1–M7 rot je am genannten Test, Grundlinie grün
  (Tabelle in B-508). Review: 🟡 Grundbestands-Reihe bekam „bitte ihren Eigentümer" → behoben (Zähler 1 von 5); zwei 🟢
  behoben ohne Zählung.

## Runde 1 — Rollengang / Abnahme (Sprint 48)

- **Creator — abgenommen:** jeder AK als Konsolenaufruf gegen den echten In-Process-Server (Modell gefälscht). Kein Lauf
  mit Ollama gegen `:5200` – dort lebt die echte DB.
- **Vater, Sohn — Regressionszeugen:** Suite grün, kein Pfad ihrer Ebenen im Diff.
- Commit `7c17e07`.

## Retrospektive (Sprint 48)

**Nachschau:** Sprint 47 (`6411497` + `5a5cdd5`, B-506) von einem frischen `pugling-reviewer`, gelesen. **Ein 🟡**, zwei 🟢
→ [B-510](backlog/B-510-buchwahl-bietet-reihe-ohne-fach-zum-schreiben.md) (P2: die Buchwahl zählt eine Reihe ohne Fach,
an der seit B-508 jedes schreibende Verb endet). Am Code bestätigt. `nachgeschaut` an B-506.

**Was die eigenen Tore durchgelassen haben:** Sprint 48 selbst: der Grundbestand im Ausweg – (CZ) zum dritten Mal. Und
die Nachschau zeigt, dass meine Entscheidung 4 in B-508 („Buchwahl bewusst nicht mitgebaut") genau der (DB)-Fall war:
eine neue Sperre der Senke, deren Eingaben die Konsole an anderer Stelle wählt.

**Gelandet (ein Mechanismus, Prosa, kein Tor):** die stellvertretend entschiedenen Vorschläge als ein Absatz in
`pm-loop` Step 5 – vor allem (CZ) „Ausweg, den *dieser* Leser nehmen darf". Kein neues Tor → der Lauf geht weiter.

**Bewusst ungedeckt:** dass eine neue Sperre der Senke ihre Wähler mitnimmt, hält kein Tor; B-510 baut es für diesen Fall.

## Sprint 49 — Ziel & Umfang

**Sprint-Ziel:** Was der Vater im Vater-Web liest, gehört zum Kind und zum Bestand, die er gerade sieht – kein Name, keine
Zeile, keine Zahl und kein „noch keine …" aus einem anderen Kind oder einer Seite hinter dem Bestand.
**Umfang:** [B-509](backlog/B-509-lernstand-kopfzahl-und-zeilen-des-vorigen-kindes.md),
[B-505](backlog/B-505-leerfall-an-der-seite-statt-am-bestand.md), [B-507](backlog/B-507-nachschau-reste-sprint-45-frontend.md)
(alle Refinement idee → geschaetzt, autonom gegrillt, `art: Defekt`).
**Entwickler-Brief:** Zwei Senken. (1) Der `Pager` korrigiert eine Seite hinter dem Bestand selbst; jeder geblätterte
Leerfall liest `total`. (2) Im Lernstand ist ein anderes Kind eine andere Liste (`key={childId}`), innerhalb eines Kindes
gilt „sperren, nicht ausblenden". Kein Backend.

## Iteration 49 — umgesetzt

- `Pager` springt auf die letzte gültige Seite; neun Leerfälle auf `total` (sechs aus der Story, `VaterExercises` und die
  Suchliste `VaterClassTests` fand erst der neue Wächter `src/leerfall.test.ts`).
- Lernstand: `WordList key={childId}`, Kopfzahl und Pager an `stale`; nach dem Review auch Kopfzeile (Name nur ohne
  `stale`, gescheiterter Abruf mit `ListenAusfall`) und Katalog (`key={childId}`).
- Vorschau „nur ansehen" ohne Ausweg; zwei Tests geschärft; `confirm` im Shop-Test gestubbt.
- Verifikation: Vitest 810/810, `tsc -b` sauber, Mutanten F1–F13 + H1–H4 17/17 rot (nach der letzten Änderung, Vitest),
  Playwright 140/140 auf 5201/5174 (vor H1–H4). Ein Probe-Lauf brach beim Zurückschreiben ab (`OSError 22`, Datei kurz
  gesperrt) und ließ die Mutante H3 stehen – bemerkt am `grep`, zurückgesetzt, Probe wiederholt.
- Review: zwei 🟡 außerhalb des Diffs (dieselbe Klasse, dieselbe Seite) im Sprint behoben, ohne Zählung; 🟢 Wächter-Muster
  verbreitert. Zähler 0 von 5.

## Runde 2 — Rollengang / Abnahme (Sprint 49)

- **Vater — abgenommen:** E2E-Suite gegen echten Server und echtes UI (140/140), Lernstand/Pager/Leerfälle als
  Komponententests mit gefälschtem Abruf. Was ein Mensch noch klicken müsste: Kinderwechsel per URL im Lernstand bei
  langsamem Netz.
- **Creator, Sohn — Regressionszeugen:** Suiten grün; der Sohn liest keine der geänderten Listen.
- Commit `8a5f20a`.

## Retrospektive (Sprint 49)

**Nachschau:** Sprint 48 (`7c17e07`, B-508) von einem frischen `pugling-reviewer`, gelesen. **Ein 🟡**, ein 🟢 →
[B-511](backlog/B-511-gebundenes-profil-reihe-ohne-fach-ausweg-im-kreis.md) (P2: ein an die fachlose Reihe gebundenes
Profil macht „--series" zum Kreis). `nachgeschaut` an B-508.

**Was die eigenen Tore durchgelassen haben:** die Story B-505 zählte acht Stellen, es waren zehn – eine andere
Schreibweise derselben Frage. Gefunden hat sie erst ein Tor über den Quelltext, nicht das Lesen. Das Muster ist dasselbe
wie bei (CY): die Klasse greppen, nicht die Fundstellen der Story abarbeiten.

**Gelandet (ein Mechanismus, ein neues Tor):** `src/leerfall.test.ts` – jede Quelle mit `<Pager` fragt den Bestand, nicht
die Seite, in fünf Schreibweisen (Selbsttest). Ein neues Tor hält den Lauf nach `README` an; **stellvertretend
entschieden: weiter**, weil das Tor nur festhält, was der Sprint gebaut hat, und keine weitere Arbeit prägt.

**Bewusst ungedeckt:** die `stale`-Klasse außerhalb der Listen (Kopfzeilen, Detailflächen) hat weiter kein Tor.

## Sprint 50 — Ziel & Umfang

**Sprint-Ziel:** Kein Ausweg, den die Konsole einem Creator nennt, endet an einer zweiten Sperre derselben Senke (Server:
Reihe ohne Fach; Briefing: Profil passt nicht).
**Umfang:** [B-510](backlog/B-510-buchwahl-bietet-reihe-ohne-fach-zum-schreiben.md),
[B-511](backlog/B-511-gebundenes-profil-reihe-ohne-fach-ausweg-im-kreis.md) (beide Durchrutscher aus Nachschauen, autonom
gegrillt).
**Entwickler-Brief:** die Wähler (`SeriesOfChildsBookAsync`, Mehrfachliste) bekommen `writes`, die Eingabe der neuen
Sperre ((DB)); ein genanntes, an eine fachlose Reihe gebundenes Profil endet vor der Ortswahl.

## Sprint 51 — Ziel & Umfang

**Sprint-Ziel:** Was Doku und Tests über den Server sagen, stimmt – Swagger, Anweisungsdatei und ein Test, der unter Last
nicht flackert.
**Umfang:** [B-416](backlog/B-416-loeschen-summary-und-ziel-id-ungeprueft.md),
[B-417](backlog/B-417-concurrency-conflict-doch-erreichbar.md), [B-438](backlog/B-438-pinhasher-zeittest-flackert-unter-last.md).
[B-442](backlog/B-442-capture-wochenmission-haengt-am-wochentag.md) wurde angefangen und **zurückgestellt**: die Ursache sitzt
im Seed, der mit `DateTime.UtcNow` statt der Test-Uhr datiert – größer als ein Maskieren in der Capture.
[B-446](backlog/B-446-vokabel-tipp-zeigt-den-artikel-des-fragewortes.md) bewusst nicht angefasst: ob der Tipp die Frage
wiederholt, hängt daran, ob `Word` den Artikel enthält – am Datenstand zu klären.

## Iteration 50/51 — umgesetzt

- B-510/B-511 wie im Brief; Hilfetext unter `--series` berichtigt. B-416: Summary + Doku, Test mit Objective-Id (erster Lauf
  fiel an der eigenen Vorbedingung – gleiche Ids in der geteilten Test-DB – daher ein Wegwerf-Ziel mit zwei Etappen);
  B-417: ein Satz; B-438: verschränkt gemessen.
- Verifikation: `dotnet test Pugling.sln -c Release` 1434/1434 (ein Lauf unter Last, 3 min); Mutanten P1–P4, Q1, Q2, R1, S1
  8/8 rot nach der letzten Änderung.
- Review: 🟡 genanntes gebundenes Profil + `--series` lief in „lass --series weg" → Vorprüfung vor der Ortswahl (Zähler 1
  von 5); drei 🟢 offen gelassen (in B-510 benannt).

## Runde 3 — Rollengang / Abnahme (Sprint 50/51)

- **Creator — abgenommen:** Konsolenaufrufe gegen den echten In-Process-Server; Swagger trägt das neue Summary.
- **Vater, Sohn — Regressionszeugen:** Suite grün.
- Commits `2917449` (Sprint 50), `82fdc4c` (Sprint 51).

## Retrospektive (Sprint 50/51)

**Nachschau:** Sprint 49 (`8a5f20a`) von einem frischen `frontend-reviewer`, gelesen, Vitest 45/45. **Ein 🟡**, zwei 🟢 →
[B-512](backlog/B-512-leerfall-waechter-uebersieht-formen.md) (das neue Tor `leerfall.test.ts` kannte nur fünf Formen; die
Ternäre schlüpfte durch). `nachgeschaut` an B-505, B-507, B-509.

**Was die eigenen Tore durchgelassen haben:** ein Tor, das bekannte *Formen* sucht, ist so breit wie die Fantasie seines
Autors; sein Selbsttest prüfte nur, was das Muster schon kannte. Der Review des Sprints hatte die Lücke angedeutet
(„`!items.length`"), ich habe drei Formen nachgezogen statt das Muster umzudrehen.

**Gelandet:** kein weiterer Mechanismus – B-512 dreht das Tor selbst um (jede Seitenlänge ist ein Fund, außer an der einen
erlaubten Stelle) und gehört in Sprint 52. Argument: die Lehre „Tore nach Positivliste statt nach Fehlerformen" ist eine
Bauweise des einen Tors, keine Regel, die eine beliebige Änderung prägt; ein zweites Tor dieser Art gibt es nicht.

**Bewusst ungedeckt:** Zwischenvariablen (`const rows = x.data?.items`) sieht auch das umgedrehte Tor nicht.

## Sprint 52 — Ziel & Umfang

**Sprint-Ziel:** Was Editor, Arcade und Vater-Listen zeigen, stimmt mit dem gespeicherten Stand – kein verlorener Haken, kein
Fokus auf `body`, keine erfundene Zahl, kein Leerfall-Wächter mit Lücken.
**Umfang:** [B-441](backlog/B-441-editor-liest-articlerequired-nicht-zurueck.md), [B-443](backlog/B-443-fokus-wege-der-sohn-runde.md),
[B-456](backlog/B-456-nachschau-reste-sprint-28.md), [B-512](backlog/B-512-leerfall-waechter-uebersieht-formen.md).
Abgeteilt: [B-513](backlog/B-513-reihe-filter-der-uebungsliste-filtert-nicht.md) (Reihe-Filter braucht einen Suchparameter).

## Sprint 53 — Ziel & Umfang

**Sprint-Ziel:** Der Umfang einer Übung in der Katalogzeile sagt die Wahrheit – „leer" ist 0, ein unlesbarer Umfang fällt auf.
**Umfang:** [B-448](backlog/B-448-umfang-nachschau-reste.md).

## Iteration 52/53 — umgesetzt

- Sprint 52: Rücklesen `articleRequired`; Fokus nach „Jetzt abfragen" (nach Review auch auf Karten ohne Eingabefeld);
  `gemesseneZahl`; Tooltip im Manifest-Ausfall; `leerfall.test.ts` umgedreht. Vitest 816/816, Mutanten T1–T7 7/7,
  Playwright 140/140 (vor T7). Review: 🟡 Fokus nur bei getippten Karten → behoben (Zähler 1).
- Sprint 53: Fang mit Log in `ItemCountOf`, Leer-Test. Suite 1436/1436, Mutanten U1–U3 3/3. Review: nur 🟢.

## Runde 4 — Rollengang / Abnahme (Sprint 52/53)

- **Vater, Sohn — abgenommen:** E2E 140/140 gegen echten Server und UI, Fokus/Listen als Komponententests.
  Mensch-Check offen: ob die Tastatur nach „Jetzt abfragen" am Handy wirklich aufspringt (iOS öffnet sie nach `await`
  oft nicht) – Geräte-Vorbehalt, kein Defekt.
- **Creator — abgenommen (53):** Katalogsuche gegen den In-Process-Server.
- Commits `59fc0a8` (52), `14be0fa` (53).

## Retrospektive (Sprint 52/53)

**Nachschau:** Sprint 50/51 (`2917449`, `82fdc4c`) von einem frischen `pugling-reviewer`, gelesen. **Zwei 🟡**, ein 🟢 →
[B-514](backlog/B-514-ausweg-series-fuer-anders-gebundenes-profil.md) (P2: an eine andere Reihe gebundenes Profil bekommt
„--series"; `writes &&` der B-511-Vorprüfung ohne Test). `nachgeschaut` an B-510, B-511, B-416, B-417, B-438. Index:
**403 von 421**.

**Was die eigenen Tore durchgelassen haben:** dieselbe Meldung (`NoSubject`) zum vierten Mal in Folge: B-508 → B-511 → B-514,
jedes Mal ein Fall der Kombination „welches Profil × welche Reihe", den ich beim Lesen des Satzes nicht durchgespielt hatte.
Meine Tests prüften, was der Satz *sagt*, nicht, ob der Rat *funktioniert*.

**Gelandet (ein Mechanismus, Prosa im treibenden Skill):** `pm-loop` Step 5 – „A way out is proven by a test that takes it
(runs the advised call) and passes every lock". Kein neues Tor → weiter. Zusätzlich lässt der Review von B-514 die
Kombinationen einmal vollständig aufzählen.

**Bewusst ungedeckt:** ein Tor, das jede Meldung mit Ausweg ausführt, gibt es nicht – die Auswege sind Text.

## Sprint 54 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn sieht jedes Datum in der Sprache seiner Arcade und als den Tag, den der Server meint; die Tests
dazu greifen auf jeder Maschine, und „beste Bewertung zuerst" ist gegen „schlechteste zuerst" belegt.
**Umfang:** [B-449](backlog/B-449-zeitzonen-tests-greifen-nur-in-new-york.md), [B-447](backlog/B-447-arcade-zeigt-iso-daten.md),
[B-412](backlog/B-412-beste-zuerst-richtung-unbelegt.md).

## Sprint 55 — Ziel & Umfang

**Sprint-Ziel:** Jeder Ausweg, den die Konsole an einer Reihe ohne Fach nennt, führt in einem Schritt durch beide Sperren.
**Umfang:** [B-514](backlog/B-514-ausweg-series-fuer-anders-gebundenes-profil.md).

## Iteration 54/55 — umgesetzt

- Sprint 54: `formatDateIn`, `augenblick` exportiert, gemeinsame Lese-Hälfte `lies` (DateOnly als Mittag UTC),
  `arcadeDatum` an vier Arcade-Stellen, Arcade-Wächter; Bewertungs-Spec mit drei Übungen. Vitest 820/820, Playwright
  140/140, Mutanten D1–D6 + E1 (D1b/D4 begründet nicht rot). Review: 🟡 zwei Tests nur zonenabhängig → strukturell
  behoben (Zähler 1).
- Sprint 55: `NoSubject(series, namedProfile)` mit vier Zweigen, je ein Schritt durch beide Sperren; zwei Tests führen den
  Rat aus (Retro-Regel aus 52/53) – das fand sofort einen Fehler im eigenen Testaufbau. Suite 1438/1438, Mutanten 4/4.
  Review des ersten Entwurfs: 🟡 „oder anderes Profil" ließ den Nutzer auf derselben Reihe (Zähler 1) → neu gefasst.

## Runde 5 — Rollengang / Abnahme (Sprint 54/55)

- **Sohn — abgenommen (54):** E2E 140/140; Datumswerte als Komponententests in beiden Sprachen.
- **Vater — abgenommen (54):** Planbau-Spec gegen echten Server, Reihenfolge 5 → 2 → unbewertet.
- **Creator — abgenommen (55):** Konsolenaufrufe gegen den echten In-Process-Server, Ratschläge ausgeführt.
- Commits `edaceab` (54), `637d868` (55).

## Retrospektive (Sprint 54/55)

**Nachschau:** Sprint 52 (`59fc0a8`, frischer `frontend-reviewer`, Vitest 68/68) und Sprint 53 (`14be0fa`, frischer
`pugling-reviewer`) – **zum ersten Mal in diesem Lauf kein 🟡**: drei 🟢 → [B-515](backlog/B-515-nachschau-reste-sprint-52.md),
Sprint 53 sauber. `nachgeschaut` an B-441, B-443, B-456, B-512, B-448.

**Was die eigenen Tore durchgelassen haben:** im Review von Sprint 54 zwei Tests, die nur in bestimmten Zeitzonen griffen –
also genau der Fehler, den die Story beheben sollte, eine Ebene tiefer. Gefunden vom Review, nicht von der Probe: die
Mutanten-Probe lief auf einem Berliner Rechner.

**Gelandet:** kein neuer Mechanismus. Argument: die Lehre „eine zonenabhängige Probe misst die Zone des Rechners" steht als
Gedächtnis-Lehre bereits fest (`lokal-windows-ci-linux`), und die strukturelle Reparatur (Zone als Parameter, eine
Lese-Hälfte) macht weitere Fälle dieser Art an dieser Stelle unmöglich.

## Sprint 56 — B-430 (Wunsch), gebaut und zurückgenommen

**Sprint-Ziel:** Nach einem verpatzten Test übt der Sohn mit einem Knopf genau die eben falschen Wörter.
**Umfang:** [B-430](backlog/B-430-nach-test-diese-woerter-ueben.md) – Offener Punkt 3 stellvertretend als (b) entschieden
(falsche Test-Antwort → Kasten 1, fällig heute).
**Ergebnis:** gebaut, Tests und Mutanten 5/5 rot, dann vom Review widerlegt: 🔴 ein absichtlich verpatzter erster Versuch
macht Wörter fällig, deren Üben Münzen bringt (nach der Leiter-Woche täglich über den ganzen Pool); 🟡 der zweite Versuch
des Tages fragt die herabgestuften Wörter leichter ab. **Zurückgenommen, nichts committet** – die Kinder lernen auf der
echten DB, und betrugssichere Punkte sind eine Bedingung des Vaters. (b) bräuchte eine Markierung „vom Test
herabgestuft, unbezahlt bis zum ersten Aufstieg" (Migration) und eine Regel gegen den leichteren zweiten Versuch.
Empfehlung an den Nutzer: (c) – Knopf „▶ Üben" ohne Leitner-Eingriff. Bleibt im Dialog.

## Sprint 57 — Ziel & Umfang

**Sprint-Ziel:** Ein Filter, den der Vater wählt, filtert – und was die Liste dazu sagt (Zahl, Fokus), gehört zum Gezeigten.
**Umfang:** [B-513](backlog/B-513-reihe-filter-der-uebungsliste-filtert-nicht.md), [B-515](backlog/B-515-nachschau-reste-sprint-52.md).
**Ergebnis:** `seriesId` am Katalog (API-First), Übungsliste, Planbau-Filter (Review-🟡, Zähler 1), Ziel-Auswahl; Zahl je Filter,
Fokus bei leerer Runde. Suite 1439/1439, Vitest 825/825, Playwright 140/140, Mutanten 8/8. Commit `9553854`.

## Sprint 58 — Ziel & Umfang

**Sprint-Ziel:** Was die Konsole dem Creator sagt, stimmt und führt nie in eine zweite Sperre; die Grammatik-Reihenfolge
überhört keinen Marker.
**Umfang:** [B-482](backlog/B-482-nachschau-reste-sprint-33.md), [B-516](backlog/B-516-nosubject-nur-sperre-und-ihre-aufhebung.md).
**Ergebnis:** `NoSubject` nur noch Sperre + Aufhebung; GrammarOrder (Trenner, Zwillinge, Label mit Trenner am Anfang –
Review-🟡, Zähler 1); zwei Bücher derselben Reihe sind eine Fehlbedienung. Suite 1441/1441, Mutanten 6/6. Commit `022dfcf`.

## Runde 6 — Rollengang / Abnahme (Sprint 57/58)

- **Vater — abgenommen (57):** E2E 140/140 (Planbau, Übungsliste) gegen echten Server und UI.
- **Sohn — abgenommen (57, B-515):** Komponententests; E2E grün.
- **Creator — abgenommen (58):** Konsolenaufrufe gegen den In-Process-Server, jeder genannte Ausweg ausgeführt.

## Retrospektive (Sprint 56–58)

**Nachschau:** Sprint 54 (`edaceab`, frischer `frontend-reviewer`): **🔴** + zwei 🟡 → [B-517](backlog/B-517-datum-tests-in-utc-und-folgetag.md)
(Zeitstempel-Tests in der UTC-CI blind, Folgetag östlich von UTC+11, Arcade-Stellen ohne Test). Sprint 55 (`637d868`,
frischer `pugling-reviewer`): drei 🟡 → [B-516](backlog/B-516-nosubject-nur-sperre-und-ihre-aufhebung.md). `nachgeschaut`
an B-449, B-447, B-412, B-514.

**Was die eigenen Tore durchgelassen haben:**
- Die Ratschläge von `NoSubject` zum fünften Mal. Antwort diesmal nicht ein weiterer Zweig, sondern weniger Rat (B-516) –
  und die Regel in pm-loop Step 5 („nur die Aufhebung dieser Sperre nennen, sonst den Rat ausführen").
- Ein **Steuerzeichen** (U+0008) im Muster des Arcade-Wächters aus Sprint 54: aus `\b` in einem Python-Heredoc wurde ein
  Backspace. Der Wächter sah im Editor richtig aus und griff in zwei von vier Formen nie; weder Selbsttest noch Review noch
  Nachschau sahen es – gefunden erst beim Bau von B-517, weil dieselbe Falle ein zweites Mal zuschlug.
- Mutanten-Proben auf einem Berliner Rechner messen keine UTC-CI (B-517, zum zweiten Mal nach B-449).

**Gelandet (ein Mechanismus, ein neues Tor):** `frontend/src/steuerzeichen.test.ts` – keine Frontend-Quelle trägt ein
Zeichen unter U+0020 außer Tab/LF/CR (Selbsttest, rote Probe C1). Ein neues Tor hält den Lauf nach `README` an;
**stellvertretend entschieden: weiter**, weil es nur Quellen liest und keine weitere Arbeit prägt.

**Bewusst ungedeckt:** Backend-Quellen und `e2e/*.ts` prüft das Tor nicht; die Falle (Python-Heredoc) bleibt eine
Werkzeug-Lehre im Gedächtnis.

## Sprint 59 — Ziel & Umfang

**Sprint-Ziel:** Jedes Datum, das Vater und Sohn sehen, ist der Tag, den der Server meint – in jeder Zone – und die Tests
dazu greifen in der UTC-CI genauso wie in Berlin.
**Umfang:** [B-517](backlog/B-517-datum-tests-in-utc-und-folgetag.md) (Durchrutscher aus Sprint 54).
**Ergebnis:** `alsUtcText` als Text geprüft, DateOnly überall in UTC, Zone zur Laufzeit gesetzt, wo ein Fehler nur
außerhalb von UTC sichtbar ist; Arcade-Wächter gegen rohes ISO. Beim Bau entdeckt: der committete Wächter aus Sprint 54
trug zwei Backspace-Zeichen. Vitest 832/832, Playwright 140/140, Mutanten rot. Commit `8485eab`, Doku-Nachzug `acb7898`.

## Sprint 60 — Ziel & Umfang

**Sprint-Ziel:** Was der Vater über die Tagesdosis liest, ist die Dosis, die das Spiel anwendet; und was der Server als
„dieselbe Antwort" sieht, sieht er an jeder Stelle gleich.
**Umfang:** [B-480](backlog/B-480-nachschau-reste-sprint-32.md), [B-501](backlog/B-501-nachschau-reste-sprint-43.md).
**Ergebnis:** `PositionResponse.DailyDoseInForce` (additiv, API-First) aus der einen Regel; Plan-Zeile und Assistent sagen
die wirksame Dosis; Bedeutungs-Trenner an jedem Leerraum, U+FEFF im Vergleich, NBSP in `ContainsWord`, Feldhilfe-Text.
Reviews: Frontend 🟡 Klammertext nannte einen Typ statt der Server-Regel (behoben, Zähler 1); Backend nur 🟢.

## Sprint 61 — Ziel & Umfang

**Sprint-Ziel:** Nachschau-Reste von Sprint 57/58 und eine E2E-Zusicherung, die fallen kann.
**Umfang:** [B-518](backlog/B-518-nachschau-reste-sprint-57.md), [B-519](backlog/B-519-nachschau-reste-sprint-58.md),
[B-479](backlog/B-479-nachschau-reste-sprint-31.md) (Punkt 1).
**Ergebnis:** Ziel-Wähler sagt, was gespeichert wird; `planbauKriterien` mit erschöpfendem Test; Fokus-Test im
Fehlerfall; Meldung nennt die Herkunft der Reihe; ‚‘ als Trenner; E2E misst den echten Scroller (rot gemessen).

## Retrospektive (Sprint 59–61)

**Nachschau:** Sprint 57 (`9553854`, frischer `frontend-reviewer`): drei 🟡 → [B-518](backlog/B-518-nachschau-reste-sprint-57.md).
Sprint 58 (`022dfcf`, frischer `pugling-reviewer`): ein 🟡 → [B-519](backlog/B-519-nachschau-reste-sprint-58.md).
`nachgeschaut` an B-513, B-515, B-482, B-516.

**Was die eigenen Tore durchgelassen haben:** zweimal eine Verdrahtung, deren reine Funktion getestet war, die Stelle des
Aufrufs aber nicht (Planbau-Kriterien, Ziel-Wähler) – das Muster `artenPending` trägt nur, solange die Funktion den
ganzen Zustand umfasst. Und: das Werkzeug verarbeitet Escapes in Bash-Aufrufen eine Stufe vorher (`\b` → Backspace,
`\u00A0` → NBSP) – zweimal heute, einmal schon in Sprint 45 (B-499). Das Steuerzeichen-Tor fängt Steuerzeichen,
nicht unsichtbare Leerzeichen.

**Gelandet:** kein weiterer Mechanismus in dieser Retro (ein Tor kam in Sprint 58). Die Werkzeug-Lehre steht im
Gedächtnis (`nachtlauf-werkzeug-lehren`): Muster und Escapes nur per Write-Werkzeug-Datei oder `sed` mit Byte-Mustern.

**Bewusst ungedeckt:** NBSP/U+FEFF als Literal im Quelltext prüft kein Tor (in Testdaten sind sie gewollt).

## Sprint 62 — Ziel & Umfang (zwei vom Nutzer freigegebene Tore)

**Sprint-Ziel:** Ob zwei Antworten gleich sind, entscheiden Server, Vater-Web und Lehrwerk-Lader nach derselben,
einmal aufgeschriebenen Fallliste – und der Lader hat überhaupt einen Test, der in CI läuft.
**Umfang:** [B-492](backlog/B-492-lader-test-in-ci.md), [B-493](backlog/B-493-fallliste-antwortvergleich.md) (Freigabe des
Nutzers vom 2026-11-12: „Ja, in CI", „Ja, mit drei Lesern").
**Ergebnis:** `docs/fallliste-antwortvergleich.json` (17 Fälle), drei Leser (xUnit, Vitest, Python), CI-Schritt
„Lehrwerk-Lader (Python)". **Beim ersten Lauf rot:** Lader und Vater-Web behandelten U+FEFF anders als der Server seit
B-501 von heute früh – die Drift, für die die Liste gebaut wurde, nachgewiesen im selben Sprint. Beide nachgezogen.
Zwei neue Tore → nach `README` Halt; der Lauf endet ohnehin hier (6-Stunden-Rahmen).

## Retrospektive (Sprint 61/62) und Abschluss des Laufs

**Nachschau:** Sprint 59 (`8485eab`, frischer `frontend-reviewer`, in fünf Zonen per `spawnSync` gemessen – Git Bash
reicht `TZ=America/…` nicht an Node durch): zwei 🟡 → [B-520](backlog/B-520-nachschau-reste-sprint-59.md), im Sprint 62
behoben (`inZone` stellt eine leere Zone per `delete` wieder her; eine Zusicherung ohne Wirkung entfernt). Sprint 60
wird mit diesem Lauf nicht mehr nachgeschaut – erste Pflichthandlung des nächsten Laufs.

**Was die eigenen Tore durchgelassen haben:** die Zeitzone ist die wiederkehrende Falle des Tages (B-449 → B-517 →
B-520): jedes Mal eine Messung, die nur auf dieser Maschine trug. Gelernt: Zonen nur im Prozess setzen (`inZone`), nie
über die Shell auf Windows; ein beim Import gebauter Formatierer hält die Zone seines Imports.

**Gelandet:** die Fallliste (B-493) ist selbst der Mechanismus dieser Retro – ein Tor über drei Leser einer Regel, das im
selben Sprint eine echte Drift fand. Zwei neue Tore → Halt nach `README`; der 6-Stunden-Lauf endet hier.

## Offene Roadmap nach dem Tageslauf

Durable Liste: [docs/backlog/](backlog/INDEX.md). Reihenfolge-Vorschlag:
1. **Nachschau Sprint 60** (B-480, B-501) und Sprint 61/62 – erste Pflichthandlung.
2. **B-430 im Dialog:** (b) widerlegt (Münzen-Farmen, leichterer zweiter Versuch); Empfehlung (c) „▶ Üben" ohne Eingriff in
   den Karteikasten.
3. **B-442** (Doku-Capture am Wochentag – Seed datiert mit `DateTime.UtcNow`), **B-446** (Tipp = Artikel des Frageworts?
   am Datenstand klären).
4. Retro-Vorschlag ohne Entscheidung: (DA) Tor gegen „du" in Supervisor-Codes – weiter aufgeschoben.

## Konkreter Änderungsstand (für Review)

Commits des Laufs auf `auto/fertigstellung` (nichts gepusht): `7c17e07`, `60750a4`, `8a5f20a`, `68328e6`, `2917449`,
`82fdc4c`, `f982ae0`, `59fc0a8`, `14be0fa`, `edaceab`, `f8ed922`, `637d868`, `9553854`, `022dfcf`, `8485eab`, `acb7898` und
der Abschluss-Commit der Sprints 60–62. **Die Änderungen wirken bei den Kindern erst nach einem Neustart des
Heimnetz-Servers** (`scripts/heimnetz.ps1`); keine neue Migration in diesem Lauf.

## Sprint 63 — Nachschau 60–62 und B-442 (nach dem geplanten Abschluss, Zeit war noch da)

**Nachschau:** Sprints 60–62 (`e37f65e`) von zwei frischen Reviewern: kein 🔴, ein 🟡 (`inZone` stellt unter Windows die
Zone nicht wieder her), mehrere 🟢 → [B-521](backlog/B-521-nachschau-reste-sprint-60-62.md), im selben Sprint gebaut
(Fallliste 21 Fälle, Phrasen-Erkennung an jedem Leerraum, Python-Schritt in CI hinter `dotnet test`). Mutanten S1, S2 rot.
**B-442** am Code bis zur Ursache verfolgt (`MarkIntroduced` datiert „gestern", montags die Vorwoche) und behoben –
nicht an einem Montag gemessen, im Story-Verlauf so benannt. **B-446** am Datenstand geprüft: `NounInfo` beschreibt je
nach Eintrag Frage- oder Lösungswort – Modellfrage, im Dialog.
Suite 1467/1467, Vitest grün, Python 4/4, Playwright 140/140 (14,0 min, Ausweichports). Damit sind alle Sprints dieses Laufs nachgeschaut außer Sprint 63 selbst.

## Abschluss des Laufs (04:15)

Kein baubarer Kandidat mehr ohne Dialog: [B-47](backlog/B-47-deploy-artefakt-smoke.md) wartet laut eigener
Entscheidung 1 auf den scharfen `workflow_run` des Azure-Deploys; [B-465](backlog/B-465-ki-lueckentext-mehrdeutig-und-verraten.md)
braucht einen Live-Lauf am Modell und eine Entscheidung, welche Mängel Regel und welche Testmodus-Prüfung sind.
Index: 52 offen, 440 abgenommen, nachgeschaut 425. **Beim Nutzer:** B-430 (Empfehlung (c)), B-446 (welche Seite
`NounInfo` beschreibt), B-480 Punkt 3, B-501 Punkte 2/4; Heimnetz-Server neu starten, damit die Söhne die
Änderungen bekommen; nichts gepusht (Branch `auto/fertigstellung`).
