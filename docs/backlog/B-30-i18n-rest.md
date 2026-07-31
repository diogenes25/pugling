---
tags: [typ/story, status/idee, bereich/doku]
aliases: [i18n-Rest, Ledger-Texte]
status: idee
prio: P3
art: Aufräumen
quelle: memory/api-fehlermeldungen-englisch.md
unverifiziert: true
---

# B-30 · i18n-Rest: Ledger-Texte, Platzhalter, interne Exceptions

Die Fehlermeldungen sind englisch. Bewusst deutsch geblieben sind die `ScoringService`-Buchungstexte
(`Reason`), Content-Platzhalter wie `(Vokabel '…' fehlt)` in `ExerciseContentResolver` und interne
Exceptions.

**Ungeprüft und die eigentliche Frage:** Buchungstexte sieht das **Kind** in der App. Englisch wäre dort
womöglich falsch — dann ist das keine i18n-Aufgabe, sondern eine Lokalisierungs-Entscheidung. Grenzt an
B-08 (XML-Docs), ist aber ein anderer Textkorpus.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
