---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [Buchwahl mit Buchfach statt Reihenfach, mehrere Lehrbücher nennt unpassendes Profil]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 45 in der Retro von Sprint 46)
unverifiziert: false
entgangen_bei: [B-498, B-500]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-506 · Nachschau-Reste Sprint 45: die Konsole wählt Buch und Profil noch nicht mit den Eingaben der Senke

Nachschau von Sprint 45 (frischer `pugling-reviewer` an `1965741`, gelesen, Build grün, Tests nicht gefahren). Zwei 🟡,
zwei 🟢 – alle dieselbe Form wie das 🟡 aus dem Review von Sprint 45 und Retro-Vorschlag (DB): eine Funktion der Senke
(`ProfileFit`) wird an zweiter Stelle mit anderen Eingaben gerufen, als die Senke selbst bildet (Fach = Reihe ?? Profil).

1. 🟡 `backend/Pugling.Agent.Creator/AgentCommands.cs:595` (`SeriesOfChildsBookAsync`) prüft
   `ProfileFit.Teaches(profile, b.SubjectId, b.SubjectName)` mit dem Fach des **Buchs**, nicht dem der Reihe. Nichts
   bindet beide (`TextbooksController.cs:69-85`, `:120-125` prüfen nur, dass das Fach existiert). Abweichend in drei
   Fällen: Buchfach als Freitext auf einer Reihe mit Fach-Id (Konsole: „kein Lehrbuch in Englisch", Briefing nähme das
   Profil); Buchfach ≠ Reihenfach (Konsole wählt die Reihe, das Briefing lehnt das Profil ab – das B-498-Symptom durch
   eine andere Tür); Reihe ohne Fach (Briefing nimmt jedes Profil mit Fach, Konsole verlangt das Buchfach).
2. 🟡 `AgentCommands.cs:638-642`: „mehrere Lehrbücher mit Profil" nennt je Buch `g.First()` (höchste Punktzahl), nicht
   das passende Profil (`:648-653` macht das nur für ein Buch). Der Nutzer tippt die genannte Id und läuft in die
   zweite Sperre von `ProfileFit`.
3. 🟢 `:650-651`: Reihe ohne Fach, ein Profil ohne `SubjectId` mit der höchsten Punktzahl → Abbruch an „hat kein Fach",
   obwohl ein schwächer bewerteter Fachlehrer passte. Der Test (`CreatorAgentTests.cs` ~1980) setzt nur die andere
   Reihenfolge auf.
4. 🟢 B-500 AK 1, zweite Hälfte („ein eigenes Profil behält beide Auswege") ohne Test: `Profil_auf_einer_Reihe_mit_fremdem_Fach_nennt_das_Profil_als_Ursache`
   (~1836) prüft kein „korrigiere"; ein immer fremder Zweig in `ProfileFit.cs:30-33` bliebe grün.

Vorschlag des Reviewers: das Prädikat „passt zum Fach dieser Reihe" (`series.SubjectId is null ? profile.SubjectId is not null : Teaches(…)`)
als Methode in `ProfileFit` (z. B. `FitsSubjectOf(profile, series)`), die beide Konsolen-Wege und das Briefing rufen.

## User Story

Als **Creator** am Konsolen-Agenten möchte ich, dass die Konsole ein Buch oder Profil, das ich nicht genannt habe, nur so
wählt, wie das Briefing es danach annimmt – damit ich nie ein Profil oder eine Id vorgesetzt bekomme, an der der nächste
Aufruf scheitert.

## Ist-Stand am Code (2026-11-12)

- Die Senke (`BriefingBuilder.ResolveProfileAsync` → `ProfileFit.Problem`) vergleicht das Profil mit dem Fach der Übung,
  das `BuildRequestAsync` (`AgentCommands.cs:532`) als `series.SubjectId ?? profile.SubjectId` bildet; eine Reihe ohne
  Fach verlangt ein Profil mit Fach-Id (sonst „hat kein Fach").
- `SeriesOfChildsBookAsync` (`:595`) vergleicht mit `b.SubjectId`/`b.SubjectName` des **Buchs** – das Buchfach ist an das
  Reihenfach nicht gebunden.
- `BestProfileForChildAsync`: der Mehrfach-Zweig (`:638-642`) nennt `g.First()`; der Einzel-Zweig (`:650-653`) nimmt bei
  einer Reihe ohne Fach `candidates[0]` ungeprüft.
- `Profil_auf_einer_Reihe_mit_fremdem_Fach_nennt_das_Profil_als_Ursache` prüft kein „korrigiere".

## Akzeptanzkriterien

1. Ein Profil ohne Reihe nimmt mit `--child` nur das Buch, dessen **Reihe** es nach der Regel des Briefings passt:
   (a) Buchfach A auf einer Reihe in B, Profil für A → „kein Lehrbuch", nicht eine Reihe, die das Briefing ablehnt;
   (b) dasselbe Buch, Profil für B → läuft (Exit 0);
   (c) Reihe ohne Fach, Profil mit Fach-Id → läuft; (d) Reihe ohne Fach, Profil nur mit Freitext-Fach → „kein Lehrbuch".
2. „mehrere Lehrbücher mit Profil" nennt je Buch das Profil, das zur Reihe passt, nicht die höchste Punktzahl.
3. Ein Buch, Reihe ohne Fach, ein fachloses Profil vor einem schwächer bewerteten Fachlehrer → der Fachlehrer spricht.
4. Ein eigenes, gebundenes Profil mit fremdem Fach behält „korrigiere" in der Meldung.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Sprint 47 auf Zuruf)

1. **Ein Prädikat in `ProfileFit`** (`FitsSubjectOf(profile, series)`), gebildet aus den Eingaben der Senke (Fach der
   Reihe, ohne eines das des Profils). Alle drei Konsolen-Wege rufen es; `Teaches` wird privat. Kosten: die Konsole lädt
   je Buch die Reihe (ein GET je verschiedener Reihe).
2. **Reihe ohne Fach zählt für jedes Profil mit Fach-Id** – so entscheidet die Senke (B-495). Kosten: ein Kind mit einem
   Buch auf einer fachlosen Reihe hat für jedes Fachprofil ohne Reihe dieses Buch zur Wahl; zwei solche Bücher sind die
   bestehende Fehlbedienung „mehrere Lehrbücher", kein stilles Greifen.
3. **Kein Abgleich Buchfach ↔ Reihenfach am Server** – das wäre ein Vertragsbruch für einen Konsolen-Defekt; das Buchfach
   bleibt Anzeige.
4. **(Review) Ein Buch ohne passendes Profil bleibt in der Mehrfachliste, aber markiert** („passt nicht zum Fach seiner
   Reihe"): fallen lassen hieße, bei einem verbleibenden Buch wählte die Punktzahl das Fach (B-481). Die Meldung „kein
   Lehrbuch" sagt jetzt „auf einer Reihe in …" – im Fall 1a hat das Kind ein Buch, nur nicht auf einer passenden Reihe.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 45, Retro Sprint 46). Punkt 1 am Code bestätigt (`:595`).
- **2026-11-12** — `ausformuliert` → `gegrillt` → `geschaetzt` (S): Sprint 47 auf Zuruf; AK 1–4, drei Entscheidungen.
- **2026-11-12** — `in-arbeit` → `abgenommen` (Sprint 47). Belege je AK-Fall (`backend/Pugling.Api.Tests/CreatorAgentTests.cs`):
  - AK 1a Buchfach A auf Reihe in B, Profil A → `Buchfach_ohne_Reihenfach_ist_kein_Buch_des_Profils`, M1 → rot
  - AK 1b dasselbe Buch, Profil B → `Reihenfach_entscheidet_die_Buchwahl`, M1 → rot
  - AK 1c fachlose Reihe, Profil mit Fach-Id → `Reihe_ohne_Fach_nimmt_den_Fachlehrer` (über `briefing`, B-508), M1/M2 → rot
  - AK 1d fachlose Reihe, Freitext-Profil → `Reihe_ohne_Fach_nimmt_kein_Freitext_Profil`, M1/M3 → rot
  - AK 2 Mehrfachliste nennt das passende Profil → `Mehrere_Buecher_nennen_je_Buch_das_passende_Profil`, M4 → rot;
    Buch ohne passendes Profil markiert → `Mehrere_Buecher_markieren_ein_Buch_ohne_passendes_Profil`, M7 → rot
  - AK 3 fachlose Reihe, fachloses Profil vorn → `…_spricht_der_Fachlehrer_auch_schwaecher_bewertet`, M5 (auch M2/M3) → rot
  - AK 4 eigenes gebundenes Profil → „korrigiere" in `Profil_auf_einer_Reihe_mit_fremdem_Fach_nennt_das_Profil_als_Ursache`, M6 → rot
  - Bestandstest `Werkunabhaengiges_Profil_ohne_eindeutiges_Buch_ist_eine_Fehlbedienung("zwei Buecher")` trug die alte Regel
    (zweites Buch auf einer Reihe eines fremden Fachs, nur das Buch nannte das Profilfach); Aufbau auf eine Reihe im Profilfach
    umgestellt – der Fall „Buchfach ≠ Reihenfach zählt nicht" steht jetzt in AK 1a.

  Mutanten-Probe (`python -I .claude/scripts/mutant-probe.py`, nach der letzten Änderung; ein erster Lauf war ungültig,
  weil der Reviewer parallel dieselben Projekte baute – „kompiliert nicht", Grundlinie danach rot, verworfen):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1 Buchwahl am Buchfach (AK 1a-d) | AgentCommands.cs:598 | red | red |
  | M2 fachlose Reihe passt nie (AK 1c, 3) | ProfileFit.cs:45 | red | red |
  | M3 fachlose Reihe passt immer (AK 1d, 3) | ProfileFit.cs:45 | red | red |
  | M4 je Buch die höchste Punktzahl (AK 2) | AgentCommands.cs:648 | red | red |
  | M5 fachlose Reihe: Punktzahl entscheidet (AK 3) | AgentCommands.cs:667 | red | red |
  | M6 eigenes Profil immer fremd (AK 4) | ProfileFit.cs:30 | red | red |
  | M7 unpassendes Buch unmarkiert (Review 🟡) | AgentCommands.cs:649 | red | red |

  Gemessen an `427450f + Arbeitsbaum`, 2026-11-12 23:15; Grundlinie nach dem Zurückspielen grün; jede Mutante fiel an den
  genannten Tests (`fails`). `dotnet test Pugling.sln -c Release` 1417/1417. `old-phrases.py`: keine verschwundene Wendung.
  Review (`pugling-reviewer`): kein 🔴; 🟡 Mehrfachliste bot bei keinem passenden Profil eine scheiternde Id → behoben
  (Entscheidung 4); 🟢 „kein Lehrbuch" unwahr im Fall 1a und veralteter Doc-Kommentar → behoben; 🟢 kein Tabellentest, der
  `FitsSubjectOf` gegen `Problem` hält (Gleichheit hängt an Konvention) → nicht gebaut, Retro Sprint 47.
  Rollengang: Creator – die Konsole lief in jedem Fall gegen den echten In-Process-Server (`CreatorAgentTests`, Modell
  gefälscht); ein Lauf mit Ollama gegen `:5200` entfiel (echte DB). Vater/Sohn Regressionszeugen: Suite grün, kein Pfad im Diff.
  Commit `6411497`.
- **2026-11-13** — nachgeschaut (Retro Sprint 48, frischer `pugling-reviewer` an `6411497` + `5a5cdd5`, gelesen). Geprüft:
  Profil-Id der automatischen Wahl erreicht das Briefing über den Zweig des benannten Profils (`BuildRequestAsync:501`,
  `BriefingBuilder.cs:252`); `FitsSubjectOf` gegen `Problem` über alle zehn Kombinationen von `ProfileFitTests` von Hand
  nachgerechnet – gleich; AK 1a/1b/1c/2/3/4 je gegen die alte Regel durchgedacht – rot. Funde → [B-510](B-510-buchwahl-bietet-reihe-ohne-fach-zum-schreiben.md):
  🟡 Buchwahl zählt eine Reihe ohne Fach, an der seit B-508 jedes schreibende Verb endet; 🟢 AK 1d trägt M1 nicht;
  🟢 Kommentar „keine zweite Prüfung".
