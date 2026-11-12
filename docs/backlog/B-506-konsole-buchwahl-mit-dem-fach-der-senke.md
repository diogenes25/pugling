---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [Buchwahl mit Buchfach statt Reihenfach, mehrere Lehrbücher nennt unpassendes Profil]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 45 in der Retro von Sprint 46)
unverifiziert: false
entgangen_bei: [B-498, B-500]
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 45, Retro Sprint 46). Punkt 1 am Code bestätigt (`:595`).
