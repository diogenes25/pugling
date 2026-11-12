---
tags: [typ/story, status/idee, bereich/lernen, rolle/student]
aliases: [Vergleiche neben Normalize, AnswerVariants NBSP, ergänzte Lösung verrät sich über die Form]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend, frontend, .claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 43 in der Retro von Sprint 44)
unverifiziert: false
entgangen_bei: [B-497]
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 43, Retro Sprint 44).
