---
tags: [typ/story, status/idee, bereich/auth, bereich/doku]
aliases: [Father als Tabellenname]
status: idee
prio: P3
art: Aufräumen
quelle: docs/lehrer-konto-plan.md
unverifiziert: true
---

# B-32 · `Father` heißt noch `Father`, obwohl die Zeile `Adult` ist

Fachlich ist die Nicht-Kind-Zeile ein **`Adult`** (sie trägt auch ein Lehrer-Konto ohne Betreuungsauftrag);
Tabelle und interne Namen tragen teils noch den alten Namen.

**Ungeprüft — Überschneidung zu klären:** **E11 des DB-Umbaus** zieht Tabellennamen auf die DbSet-Namen und
benennt genau solche internen Reste um (`FatherOwnsChildAsync`, `EnsureForFatherAsync`, `demoFather`). Beim
Ausformulieren zuerst prüfen, ob nach E11 überhaupt etwas übrig bleibt — dann wird diese Story `verworfen`.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft), Überschneidung mit E11 vermerkt.
