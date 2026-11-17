---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, rolle/creator, rolle/student, lerntechnik/vokabeln]
aliases: [Vorgabe je Lücke, Gap.Prompt, deutsche Vorgabe am Lückentext]
status: geschaetzt
prio: P1
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
---

# B-633 · Lückentext: eine eigene deutsche Vorgabe je Lücke

Teil A aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md) (Nacharbeit eines Schultests). Eine
Lücke soll ihre Vorgabe selbst tragen können, also den deutschen Satz der Lehrerin („Während des Films schlief ich
ein.“) oder das deutsche Wort („Richtung Osten“). Bisher kommt der Hinweis an einer Lücke nur aus dem
Vokabelspeicher und ist dort nur das deutsche Wort.

## User Story

Als *Vater* möchte ich an jeder Lücke die deutsche Vorgabe aus dem Test eintragen, damit mein Sohn genau die
Aufgabe sieht, die er in der Schule hatte.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`), aus B-632 übernommen:

- `Gap(Index, Answer, Alternatives, VocabKey, Form, Distractors)` hat kein Feld für eine Vorgabe
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:103-104`).
- Eine Lücke mit `VocabKey` bekommt als Hinweis die Übersetzung aus dem Speicher, eine Lücke ohne Schlüssel
  keinen Hinweis (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:180-192`;
  `ClozeExerciseType.ItemsOf` setzt `Hint: null`, `backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:220-227`).

## Die echte Lücke

Es gibt keinen Ort für eine deutsche Vorgabe, die vom Autor stammt und nicht aus dem Vokabelspeicher.

## Entscheidungen

Aus B-632 (Entscheidungen 1, 6 und 7): Bei einem Satz tippt das Kind nur das Zielwort in einen englischen Satz mit
Lücke, und die deutsche Vorgabe steht darüber. Bei einem Einzelwort ist die Lücke der ganze Text. Die deutschen
Vorgaben werden wörtlich aus dem Test übernommen, die englischen Rahmensätze neu geschrieben.

## Akzeptanzkriterien

1. `Gap` hat ein optionales Feld `Prompt`. Ist es gesetzt, ist es der Hinweis der Lücke, sonst gilt wie bisher die
   Übersetzung aus dem Speicher.
2. Der Lückentext-Editor des Vaters zeigt und speichert die Vorgabe je Lücke. Beim Wiedereinlesen steht sie wieder da.
3. Eine Lücke ohne Satz (Text nur `{{1}}`) mit Vorgabe wird als Einzelwort-Aufgabe gespielt.
4. Die Vorgabe ist eine Übersetzung und keine Lösung. Ihr Feldname fällt nicht unter die Lösungsfeld-Regel
   (`ConventionGuardTests`).

## Schätzung

**S.** Ein additives Vertragsfeld, ein Zweig im Resolver und ein Feld im Editor.

- **`migration: nein`:** `Gap` liegt im JSON der Übung, nicht in einer Spalte.
- **`vertragsbruch: nein`:** Das Feld ist optional, alte Payloads bleiben gültig. Damit `unknown_field` nicht greift,
  muss das Feld trotzdem in `Pugling.Client` und im Editor ankommen.
- **Angriffsplan:**
  1. `Gap.Prompt` im Vertrag.
  2. `ResolveClozeRefsAsync` und `ClozeExerciseType.ItemsOf`: Die Vorgabe schlägt den Speicher.
  3. Der Editor `frontend/src/vater/ClozeTexts.tsx`.
- **Testweg:**
  - In `ClozePlayTests.cs`: Die Vorgabe erscheint als Hinweis, mit Speicherschlüssel und ohne.
  - Vitest für den Editor.
  - Das Lösungsfeld-Tor bleibt grün.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil A), auf `geschaetzt` übernommen: Ist-Stand, Entscheidungen und Schätzung
  stammen aus B-632.
