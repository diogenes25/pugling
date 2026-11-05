---
tags: [typ/story, status/idee, bereich/auth, rolle/supervisor]
aliases: [Name vor PIN ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#retrospektive
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-372]
---

# B-379 · Die Vorrangfolge „leerer Name vor zu kurzer PIN" an `auth/me` ist ungetestet

B-372 behauptet in Entscheidung 2: „ein leerer Name mit zu kurzer PIN [antwortet] weiter mit dem Namen". Beide Fälle
antworten `400 validation_error` und unterscheiden sich nur im `detail`. Der Mutant „PIN-Prüfung vor der Namensprüfung"
(`AuthController.cs:165` hinter `:170`) läuft durch jeden Test – gefunden in der Nachschau zu `46e3871` (frischer
`pugling-reviewer`, 🟡). Reparatur: ein Fall in `AccountSelfServiceTests` mit `{ name = "  ", pin = "12" }`, der
`detail == "Name must not be empty."` zusichert.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf S, Teil 3 (Sprint 1 der Sitzung 2026-11-05).
