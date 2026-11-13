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
