---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor, rolle/student]
aliases: [Umfang Nur neue nach B-419, Scope New fragt nie]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-435 · Eine Position mit Umfang „nur neue" zeigt ihre Wörter seit B-419 nur noch – abgefragt werden sie nie

Der Umfang `ItemScope.New` filtert die Runde auf Atome ohne `IntroducedAt` (`PositionPlayService.ScopeMatch`). Bis B-419
wurde ein neues Wort dort einmal getippt abgefragt und fiel danach aus dem Umfang. Seit B-419 ist die erste Begegnung eine
Kennenlernkarte, die `IntroducedAt` setzt – die Abfrage-Runde danach findet das Wort nicht mehr im Umfang, „Jetzt
abfragen" führt in „Nichts fällig".

Beim Bau von B-419 (Lauf AE) beim Lesen von `ScopeMatch` aufgefallen, nicht gemessen; der Assistent setzt den Umfang nicht,
erreichbar ist er über die Plan-Seite. Die Frage für den Dialog: heißt „neu" künftig „noch nie abgefragt" (dann wäre die
Bedingung `ReviewCount == 0` statt `IntroducedAt`), oder bleibt „nur neue" ein reines Kennenlernen?

## Verlauf

- **2026-11-08** — angelegt beim Bau von B-419 (Lauf AE, Sprint 22).
