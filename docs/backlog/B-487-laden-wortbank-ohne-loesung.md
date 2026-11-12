---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [handgeschriebene wordBank ohne Lösung, Wortbank im Selbsttest ungeprüft]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 36)
unverifiziert: false
entgangen_bei: [B-483]
grund: ""
ersetzt_durch: []
---

# B-487 · Eine handgeschriebene Wortbank ohne die Lösung geht durch Prüfung und Selbsttest

Nachschau von Sprint 36 (frischer `pugling-reviewer` an `4a2e170`; `check()`/`build_config()` offline aufgerufen,
kein Server). B-483 hat die Wortbank neben `markup` verboten, weil ihr eine Lösung fehlen könnte – den zweiten Weg,
den `FORMAT.md` und die Fehlermeldung ausdrücklich empfehlen (`text`/`gaps`/`wordBank` von Hand), aber nicht.

1. **`{"text":"I {{1}}.","gaps":[{"index":1,"answer":"went"}],"wordBank":["go","gone"]}` besteht `check()`**
   (gemessen). Der Server prüft nicht, dass die Bank jede Lösung enthält (`ClozeConfig.WordBank` ist eine Liste), und
   der Selbsttest sieht es auf keiner Stufe: die Bank ist reine Anzeige (`Choices`), gewertet wird nur der Text. Das
   Kind spielt Lückentexte standardmäßig auf der Wortbank-Stufe (`DefaultStage => TranslationWordBank`) und bekommt
   eine Lücke, die es nicht lösen kann, während der Lader grün meldet.

## Reste aus derselben Nachschau (🟢)

- `"distractors": null` neben `markup` geht still durch (die Formprüfung läuft nur bei `is not None`); das Kind tippt
  dann frei, obwohl `FORMAT.md` „Liste von Wörtern" verspricht.
- `"distractors": []` baut eine Bank nur aus Lösungen (bei einer Lücke eine einzige Kachel).
- Die Doppel-Kachel-Bereinigung in `build_config` ignoriert nur Groß/Klein; der Vergleich faltet auch Apostrophe und
  Schlusszeichen (`[qu'il]` + Ablenker `qu’il` = zwei richtige Kacheln); ein Ablenker gleich einer Alternative einer
  Lücke (`[went|did go]` + `did go`) ist ein richtiger „Ablenker".

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 36 im Backlog-Lauf).
