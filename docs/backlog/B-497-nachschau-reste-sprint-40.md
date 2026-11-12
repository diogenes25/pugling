---
tags: [typ/story, status/idee, bereich/lernen, rolle/student]
aliases: [angehängte Lösung ungetrimmt, NBSP in Auswahl, Nachschau-Reste Sprint 40]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend, frontend, .claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 40 in der Retro von Sprint 41)
unverifiziert: false
entgangen_bei: [B-491, B-490]
grund: ""
ersetzt_durch: []
---

# B-497 · Nachschau-Reste Sprint 40: die angehängte Lösung ist ungetrimmt, und NBSP macht zwei gleiche Knöpfe

Nachschau von Sprint 40 (frischer `pugling-reviewer` an `b521cd9`, per `git archive` exportiert, 1396/1396). Kein 🔴/🟡;
vier 🟢, die ersten zwei mit Fall:

1. **Die ergänzte Lösung wird nicht getrimmt** (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:242` Wortbank,
   `:495-496` Verstehensfrage). `Tiles` trimmt nur die Einträge des Autors; die Ergänzung kommt roh aus
   `GradedAnswers()`, und der Server trimmt `Gap.Answer`/`Question.Answer` beim Speichern nicht. Lücke `" went"` + Bank
   `["go","gone"]` → Pool `[" went","go","gone"]`: das Leerzeichen sortiert die Lösung immer nach vorn und verrät sie –
   genau was die Sortierung aus B-487 verhindern soll. Bewertet wird richtig (`Normalize` trimmt).
2. **NBSP:** `StageMechanics.Normalize` teilt nur am Leerzeichen, der Editor (`exerciseConfig.tsx:136-139`) an jedem
   Leerraum. Antwort `"in York"` + Möglichkeit `"in York"` mit NBSP: der Editor warnt nicht, der Server hängt die Antwort
   an – zwei gleich aussehende Knöpfe, einer richtig. Ebenso legt `Tiles` NBSP-Dubletten nicht zusammen.
3. **Zweite richtige Kachel über eine Alternative:** `Tiles` legt nur nach `Normalize` zusammen; eine Kachel gleich
   einer `alternative` einer Lücke bleibt eine zweite richtige (B-487 hält das als gewollt fest). Der Lader nennt das auf
   dem `markup`-Weg einen Fehler, bei der handgeschriebenen `wordBank` prüft er nur die Form.
4. **Doku:** `.claude/skills/lehrwerk-aufbereiten/FORMAT.md:128-130` nennt die neue Formregel der `wordBank` nicht;
   `frontend/src/lib/fieldHelp.ts:249-251` (`questionChoices`) sagt „in dieser Reihenfolge" ohne Zusammenlegen und
   Anhängen. Für den Lader gibt es keinen bleibenden Test (B-492).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 40, Retro Sprint 41 im Nachtlauf).
