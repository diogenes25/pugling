---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [EF-Migration schreibt CRLF in Check-Constraints]
status: idee
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Bau von B-530 im Backlog-Lauf 2026-11-13 (docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-546 · `migrations add` unter Windows schreibt `\r\n` in Check-Constraint-Strings

Beim Anlegen der Migration `20261113224441_CardShownAt` (B-530) hat EF unter Windows in drei
Check-Constraint-Strings des Snapshots `\r\n` statt des `\n` der bestehenden Kette geschrieben; von Hand
zurückgesetzt. Jede künftige `migrations add` auf Windows braucht dieselbe Korrektur, und kein Tor fängt sie: der
Unterschied ist für `HasPendingModelChanges` unsichtbar, erzeugt aber einen lauten Snapshot-Diff und – je nach
Plattform des nächsten Laufs – eine scheinbare Modelländerung (vgl. Memory „Lokal Windows, CI Linux“).

Zu klären: ein Nachlauf-Skript zu `migrations add` (normalisiert die Strings), oder ein Wächter in `SchemaGuardTests`,
der `\r` in Check-Constraint-SQL des Snapshots ablehnt.

## Verlauf

- **2026-11-13** — angelegt aus dem Bau von B-530.
