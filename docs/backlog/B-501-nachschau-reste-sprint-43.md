---
tags: [typ/story, status/abgenommen, bereich/lernen, rolle/student]
aliases: [Vergleiche neben Normalize, AnswerVariants NBSP, ergänzte Lösung verrät sich über die Form]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "backend, frontend, .claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 43 in der Retro von Sprint 44)
unverifiziert: false
entgangen_bei: [B-497]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-501 · Nachschau-Reste Sprint 43: Stellen, die noch anders vergleichen als `Normalize`

Nachschau von Sprint 43 (frischer `pugling-reviewer` an `0ffa5e0`, per `git archive`, 1405/1405). Kein 🔴/🟡; sieben 🟢,
alle Stellen, die weiter am Leerzeichen oder anders als `StageMechanics.Normalize` vergleichen:

1. `AnswerVariants` trennt Bedeutungen nur an `" / "` mit normalem Leerzeichen (`AnswerVariants.cs:63/67/114`):
   `"Aha! / Verstehe."` wird nicht zerlegt, „Aha" ist dann falsch.
2. Die ergänzte Lösung verrät sich über ihre **Form** (`BuiltInExerciseTypes.cs:244`): Lücke `"Went."` + Bank
   `["go","gone"]` → einzige Kachel mit Punkt und Großbuchstabe. Groß/Klein ist eine Produktfrage.
3. U+FEFF: der Editor trimmt es (`exerciseConfig.tsx:137-141`), .NET nicht – zwei gleich aussehende Knöpfe wie beim NBSP.
4. Der KI-Creator vergleicht Ablenker nur nach Groß/Klein (`ClozeStrategy.cs:55-60`, `:115-116`, `:125`): `went.` zählt
   als Ablenker und fällt am Server mit `went` zusammen; ein NBSP-Ablenker gleich einer Alternative bleibt eine zweite
   richtige Kachel.
5. `ClozeMarkup.ContainsWord` und `laden.py contains_word` erkennen eine Lösung über ein NBSP im Text nicht.
6. `fieldHelp.ts:251` (`questionChoices`) behauptet „Satzzeichen am Ende" allgemein; zusammengelegt werden nur `. ? ! …`
   (und Apostroph/Leerraum, die nicht genannt sind).
7. `laden.py:105-106`: Docstring von `graded()` sagt noch „repeated spaces".

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Punkt 1: Bedeutungen trennt ein `/` mit **beliebigem** Leerraum davor und danach (wie `Normalize` Leerraum sieht).
2. Punkt 3: `Normalize` entfernt U+FEFF (der Editor tut es schon).
3. Punkt 5: `ClozeMarkup.ContainsWord` erkennt eine mehrteilige Lösung über jeden Leerraum im Text (NBSP).
4. Punkt 6: der Feldhilfe-Text `questionChoices` nennt, was wirklich zusammengelegt wird (Schlusszeichen `. ? ! …`,
   Apostroph-Schreibweise, Leerraum).
5. **Bewusst offen:** Punkt 2 (Groß/Klein verrät die ergänzte Lösung – Produktfrage), Punkt 4 (Ablenker des KI-Creators
   vergleichen anders – bräuchte `Normalize` im Vertrags-Projekt, ein eigener Umbau), Punkt 5 für `laden.py` und
   Punkt 7 (Docstring in `laden.py`).

## Akzeptanzkriterien

1. „Aha!" + NBSP + „/ Verstehe." – „Aha" zählt. 2. U+FEFF vor einem Wort ist kein Unterschied. 3. „was" + NBSP +
„playing" im Text verrät die Lösung „was playing".

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 43, Retro Sprint 44).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 60).
- **2026-11-13** — gebaut (Sprint 60). AK 1 → `Schraegstrich_Trennt_Auch_Mit_Geschuetztem_Leerzeichen` (A1 → rot); AK 2 →
  `Unsichtbares_FEFF_Ist_Kein_Unterschied` (A2 → rot); AK 3 → `Loesung_mit_geschuetztem_Leerzeichen_im_Text…` (A3 → rot).
  Review Backend: nur 🟢 (Kommentar zur Leerraum-Regel berichtigt). Die Fallliste aus B-493 hält die Regel seitdem auf allen
  drei Seiten.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Sprint 63, frische `pugling-reviewer`/`frontend-reviewer` an `e37f65e`, gelesen, Vitest 102/102). Geprüft: CI-Schritt läuft (python3, Arbeitsverzeichnis, Pfade), xUnit-Leser findet die Liste, alle drei Leser stimmen Schritt für Schritt überein, `Map`-Aufrufer laden die Übung. Kein 🔴; Reste → [B-521](B-521-nachschau-reste-sprint-60-62.md).
