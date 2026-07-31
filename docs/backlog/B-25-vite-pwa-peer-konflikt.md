---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [vite-plugin-pwa Peer-Konflikt]
status: idee
prio: P3
art: Aufräumen
quelle: memory/codequalitaet-gates.md
unverifiziert: true
---

# B-25 · Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` lösen

Umgangen, nicht gelöst: die Installation läuft nur mit `--legacy-peer-deps`. Das ist eine tickende
Abhängigkeit — beim nächsten frischen Clone oder CI-Runner ohne den Schalter bricht es.

**Ungeprüft:** ob inzwischen eine Version existiert, die `vite@8` als Peer akzeptiert.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
