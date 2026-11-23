---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf D – die gestufte Position spielt ihre Stufe

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den Aufbau-Weg aus B-700 vom Katalog bis in den Spielpfad tragen – B-848 zuerst, danach was davon abhängt.

Auftrag: „starte einen nachtlauf“ – gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangsstand `main` `4781af38`, sauber. Story-Ids dieses Laufs: ab B-870.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/`. Offen sind 96 Stories. Die autonom grillbaren (`Defekt`/`Aufräumen`) sind nur
noch B-860 (vom letzten Lauf bewusst liegen gelassen: Wahl Sperre vs. DB-Regel) und B-863 (deutsche Kommentare in Tests).
Baubar nach Freigabe 8 sind die geschätzten Wünsche; der P1-Faden mit vom Nutzer bestätigten Entscheidungen ist der
Aufbau-Weg: **B-848** (Position spielt ihre Stufe, L) ist die Wurzel, B-849 (Aufstieg) und B-841 (Oberfläche) warten
ausdrücklich auf sie. Die B-644-Kette (B-731 ff.) bleibt liegen wie im letzten Lauf begründet.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 121 | B-848 | Aufbau-Weg im Spielpfad: gespielte Übung, Lernstand je Übung, Start-Stufe |

## Sprint 121 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn spielt an einer Position, deren Start-Stufe der Vater auf eine Vorstufe gesetzt hat, in Üben und
Test die Vorstufe – mit ihrem Typ, ihrem eigenen Lernstand und ihrer Buchseite –, und der Vater sieht dabei das Ziel und
„Stufe n von m“.
**Umfang:** B-848 (L, backend, Migration).
**Entwickler-Brief:** Quelle der Wahrheit ist `PositionPlayService.PlayedExercise(pos)` – eine Naht für Inhalt, Typ,
Config, Vorgabe-Stufe, Item-Zahl und die Lernstand-Zeilen (`PlayedExerciseId`). Sie liest die neue Navigation
`Exercise.Prerequisites`; `null` heißt „nicht geladen“ und wirft auf einer gestuften Position (Tor gegen vergessene
Includes, statt still das Ziel zu spielen). Migration `BuildLevelPlayed`: `PlanPositions.CurrentBuildLevel`,
`PositionItemProgress.ExerciseId` (gefüllt aus der Position, FK Restrict), Schlüssel `(Position, Übung, Index)`. Guards:
Start-Stufe nur mit passender Vorstufe, keine Tages-/Wochenpflicht auf einem Weg; eine gespielte Vorstufe ist nicht
löschbar (409 statt 500). Testweg: `AufbauStufePlayTests` (AK 1–7), `AufbauStufePlayMigrationTests` (AK 8).
