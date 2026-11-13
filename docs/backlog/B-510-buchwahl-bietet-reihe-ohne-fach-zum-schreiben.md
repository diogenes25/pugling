---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [Buchwahl Reihe ohne Fach, mehrere Lehrbücher Reihe ohne Fach]
status: abgenommen
prio: P2
art: Defekt
groesse: "S"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 47 in der Retro von Sprint 48)
unverifiziert: false
entgangen_bei: [B-506, B-508]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-510 · Die Buchwahl der Konsole bietet zum Schreiben eine Reihe ohne Fach an

Nachschau von Sprint 47 (frischer `pugling-reviewer` an `6411497` + `5a5cdd5`, gelesen, nicht gebaut). Ein 🟡, zwei 🟢.

1. 🟡 `backend/Pugling.Agent.Creator/AgentCommands.cs` `SeriesOfChildsBookAsync` und die Mehrfachliste in
   `BestProfileForChildAsync`: seit B-506 zählt eine Reihe ohne Fach für jedes Profil mit Fach-Id (die Regel des
   Briefings). Seit B-508 lehnt `BuildRequestAsync` dieselbe Reihe für `create`/`plan`/`exam` ab. Szenario: Kind mit
   Buch auf S1 (Englisch) und Buch auf S2 (ohne Fach); `create --profile <Englischlehrer ohne Reihe> --child …`. Vor
   B-506 nahm die Konsole S1; jetzt endet sie an „mehrere Lehrbücher … wähle mit --series: S1, S2" – und S2 läuft in die
   zweite Sperre „hat kein Fach". Ein Ausweg, der an einer Sperre derselben Senke endet ((CV)). B-508 hat die Buchwahl
   bewusst nicht mitgebaut (Entscheidung 4) – das war der Durchrutscher: die Sperre der Senke kam dazu, ihre Eingaben
   nicht.
2. 🟢 `CreatorAgentTests.Reihe_ohne_Fach_nimmt_kein_Freitext_Profil` (B-506, AK 1d) trennt die alte Regel nicht von der
   neuen: der Freitext „Latein …" passt auch nicht zum Buchfach, M1 bliebe grün. Freitext = Fachname des Buchs.
3. 🟢 `ProfileFit.cs:5-8` und `backend/Pugling.Agent.Creator/CLAUDE.md` sagen „keine zweite Prüfung in der Konsole" –
   die Konsole prüft seit B-506 per `FitsSubjectOf` bei jeder automatischen Wahl; wahr ist „dieselbe Regel".

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf, Nutzerrolle übertragen):

1. **Die Wähler bekommen die Eingabe der neuen Sperre:** `SeriesOfChildsBookAsync` und `BestProfileForChildAsync` nehmen
   `writes` mit (die (DB)-Regel: eine Funktion der Senke an zweiter Stelle mit den Eingaben der Senke).
2. **Buch eines werkunabhängigen Profils:** beim Schreiben zählt eine Reihe ohne Fach nicht als Buch des Profils – sie
   trägt das Fach des Profils nur leihweise und nimmt keine Übung auf. Bleibt dann keines, aber eines ohne Fach hätte
   gepasst, endet es an `ProfileFit.NoSubject` dieser Reihe (der Ausweg nach dem Recht des Lesers), nicht an „kein
   Lehrbuch". `briefing` wählt weiter wie bisher.
3. **Mehrere Bücher mit Profil:** beim Schreiben bleibt ein Buch auf einer Reihe ohne Fach in der Liste (Wegfallen ließe
   den Score das Fach wählen, B-481), aber markiert „(Reihe ohne Fach – nimmt keine Übung auf)". Kosten: der Nutzer
   sieht eine Zeile, die er nicht wählen kann – dafür wählt nichts still an ihm vorbei.
4. Punkt 2/3 der Nachschau: AK-1d-Test mit dem Fachnamen des Buchs als Freitext; Kommentar `ProfileFit` und
   Agent-`CLAUDE.md` sagen „dieselbe Regel in Konsole und Briefing" statt „keine zweite Prüfung".

## Akzeptanzkriterien

1. Kind mit Buch auf S1 (Fach E) und Buch auf S2 (ohne Fach), `create --profile <E-Lehrer ohne Reihe> --child`: Exit 0,
   Übung in S1.
2. Dasselbe mit nur dem Buch auf S2: Fehlbedienung mit „hat kein Fach", nicht „kein Lehrbuch".
3. `briefing` mit nur dem Buch auf S2: Exit 0 (wie bisher, AK 1c von B-506).
4. Zwei Bücher mit Profil, eines auf einer Reihe ohne Fach, `create --child`: die Liste markiert es „ohne Fach".
5. AK 1d von B-506 fällt unter M1 (Vergleich mit dem Buchfach).

## Schätzung

**Größe: S** — ein Parameter durch zwei Methoden, zwei Zweige, vier Tests in `CreatorAgentTests`.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 47, Retro Sprint 48). Punkt 1 am Code gelesen
  (`AgentCommands.cs:599-611`, `:650-651`; `:535`).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 50).
- **2026-11-13** — gebaut (Sprint 50).
  - AK 1 → `Schreiben_zaehlt_ein_Buch_auf_einer_Reihe_ohne_Fach_nicht`; P1 → rot.
  - AK 2 → `Schreiben_nur_mit_Buch_ohne_Fach_nennt_die_Sperre_der_Reihe`; P2 → rot.
  - AK 3 → `Briefing_nimmt_das_Buch_ohne_Fach_weiter`; P4 (Buchwahl immer mit `writes`) → rot.
  - AK 4 → `Mehrere_Buecher_markieren_eine_Reihe_ohne_Fach`; P3 → rot.
  - AK 5 → `Reihe_ohne_Fach_nimmt_kein_Freitext_Profil` mit dem Fachnamen des Buchs; **ungemessen** (die alte
    Buchfach-Regel ist seit B-506 kein einzeiliger Mutant mehr) – getragen nur durch die geänderte Testeingabe.
  - Review (`pugling-reviewer`): kein Kreis über ein automatisch gewähltes Profil. 🟢 offen gelassen (ohne Zählung): sind
    beim Schreiben *alle* Bücher fachlos, bietet die Liste nur markierte Ids; der Rat „--series" ist für ein
    werkunabhängiges Profil weiter als nötig; zwei GET je Buch.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1434/1434; Mutanten-Probe nach der letzten Änderung (Debug,
    gefiltert) 8/8 rot je am genannten Test, Grundlinie grün (Tabelle im Protokoll, Sprint 50/51).
  - Rollengang Creator: jeder AK als Konsolenaufruf gegen den echten In-Process-Server (Modell gefälscht). Vater, Sohn:
    Regressionszeugen (Suite grün, kein Pfad ihrer Ebenen im Diff). Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 52/53, frischer `pugling-reviewer` an `2917449` + `82fdc4c`, gelesen).
  Geprüft: Probe-Ziel-Logik gegen parallele Testklassen (eigene SQLite-Datei je Klasse, Tests einer Klasse seriell);
  `concurrency_conflict`-Quellen nachgezählt; Zeittest ohne Lockvogel rot an allen drei Pfaden; AK-Tests je gegen die
  benannte Änderung. Befund: 🟢 AK-2-Test war schon vor B-510 grün (Kommentar berichtigt) → [B-514](B-514-ausweg-series-fuer-anders-gebundenes-profil.md).
