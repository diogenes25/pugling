---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Üben nach dem Test landet in „Nichts fällig"]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Sprint 64, Review B-430)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-522 · „▶ Üben" nach einem verpatzten Test landet bei Leitner-Positionen oft in „Nichts fällig 🎉"

Gefunden vom `frontend-reviewer` an [B-430](B-430-nach-test-diese-woerter-ueben.md) (c), dort bewusst als Grenze
angenommen. Bei `useLeitner=true` zeigt die Übungsrunde nur fällige Karten; wer erst übt und dann testet, hat heute
nichts mehr fällig. Das Kind, eben durchgefallen, sieht „Nichts fällig 🎉 – Mach den Test oder komm später wieder."
und „🎯 Zum Test" – bei 0 Restversuchen führt das in `test_attempts_exhausted` (`SohnPractice.tsx:291-299`).

Zu klären (Dialog, `art: Wunsch`): (1) Knopf nur zeigen, wenn die Position heute etwas fällig hat (Server liefert ein
Flag, `SohnTest` reicht es durch); (2) ein eigener Leertext nach einer Niederlage ohne 🎉 und ohne verbrauchten Test;
(3) beides. Variante (b) aus B-430 (Test stuft herab) bleibt ausgeschlossen, solange sie Münzen farmbar macht.

## Verlauf

- **2026-11-13** — angelegt aus dem Review von B-430 (c), Tageslauf Sprint 64.
