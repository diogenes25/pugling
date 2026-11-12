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
