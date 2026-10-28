---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [CoversRequiredWords null ungetestet, B-318 ohne Pflichtwort]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H Teil 1 in Lauf H, Teil 2 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-318, B-306]
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-321 · Die `null`-Fälle aus B-306/B-318 lesen den Pflicht-Wortschatz nie

Jede Übungsstrategie liest die Elemente des Modellentwurfs an drei Stellen: `NoDuplicates`, `CoversRequiredWords` und die
Regelschleife. `DraftRules.CoversRequiredWords` steigt ohne Pflicht-Wortschatz sofort aus
(`backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:92`), und die Fälle aus [B-318](B-318-null-element-ohne-fall-in-drei-strategien.md)
und [B-306](B-306-null-element-in-uebungsstrategien.md) setzen keinen. Die Mutanten `i!.Front`
(`VocabularyStrategy.cs:45`), `i!.Source` (`TranslationStrategy.cs:43`), `t!.Prompt`/`t!.Answer` (`GrammarStrategy.cs:45`)
und `g!.Answer` (`ClozeStrategy.cs:50`) blieben grün – mit `--words` oder `--use-weak` endete so ein Entwurf wieder in
einer `NullReferenceException`.

## User Story

Als Entwickler möchte ich, dass die `null`-Fälle jede Lesestelle tragen, auch die, die nur mit Pflicht-Wortschatz läuft.

## Ist-Stand am Code

Siehe oben (Stand `c195ef2`).

## Akzeptanzkriterien

1. Je Strategie fällt ein `null`-Fall, wenn das `?.` im `CoversRequiredWords`-Lambda zu `!.` wird.

## Entscheidungen

1. Ein Pflichtwort je Zeile, das die echten Elemente abdecken – sonst käme eine zweite Verletzung „Pflichtwort fehlt“
   dazu, und der Fall prüfte etwas anderes. Kosten: ein Theorie-Parameter mehr.

## Schätzung

XS, `backend` (Tests), keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu Lauf H, Teil 1 (`c195ef2`, frischer `pugling-reviewer`, 🟡) in Lauf H,
  Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); im selben Sprint gebaut. Theorie und Lückentext-Fall bekommen
  ein Pflichtwort („the horse“ exakt, „horse“, „feed“, „horse“ exakt). Dass die Mutanten vorher grün blieben, hat der
  Prüfer am Code hergeleitet (Ausstieg in `DraftRules.cs:92`), nicht gemessen. Gemessen danach: die vier Mutanten
  (`i!.Front`, `i!.Source`, `t!.Prompt`/`t!.Answer`, `g!.Answer`) → je **1 failed**, Kontrolle **4 passed**. Suite
  **1001/1001 `exit=0`**. Rollengang: Agent ohne Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — nachgeschaut (Lauf I, frischer `pugling-reviewer` am Commit `f6a2347`): `DraftRules.CoversRequiredWords` liest ohne Pflicht-Wortschatz nicht, mit ihm jedes Element – die Bedingung kommt über `Request(words:)` → `BriefingBuilder` wirklich an; je Zeile das Pflichtwort durch ein echtes Element gedeckt; Mutanten `i!.Front`, `i!.Source`, `t!.Prompt`, `g!.Answer` je rot hergeleitet; Filterlauf **6/6 passed**. Kein Befund.
