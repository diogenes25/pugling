---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [laden.py automatisch testen, Vorschlag CQ]
status: idee
prio: P2
art: Aufräumen
groesse: ""
wo: ".claude/skills/lehrwerk-aufbereiten, .github/workflows"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Vorschlag (CQ), Sprint 34; vom Nutzer am 2026-11-12 freigegeben)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-492 · Der Lehrwerk-Lader bekommt einen automatischen Test in CI

`laden.py` hat außer dem Tor über seine Feldliste (`LadenFieldListTests`) keinen automatischen Test. Sein Verhalten –
`check()`, `build_config()`, `graded()`, der Selbsttest über alle Übungen – wurde in B-475, B-483 und B-487 je mit einer
Offline-Probe im Scratchpad geprüft, die nicht ins Repo kam. Drei Durchrutscher dieses Laufs lagen im Lader (B-483,
B-487, B-491); keiner hätte eine bleibende Probe gebraucht, um gefunden zu werden, aber jede Probe verschwand mit ihrer
Sitzung.

**Freigabe:** der Nutzer hat am 2026-11-12 „Ja, in CI" gewählt. Ein neues Tor – darum ein eigener Sprint, und der
Lauf hält danach, damit der Nutzer es sieht.

## Skizze

- Ein Test neben dem Lader (Python, `unittest` aus der Standardbibliothek – kein neues Paket), der `laden.py` per
  `importlib` lädt und `check()`/`build_config()`/`graded()` offline aufruft; die Fälle aus den Proben von B-483 und
  B-487 sind der Anfang.
- CI ruft ihn auf ([.github/workflows/ci.yml](../../.github/workflows/ci.yml)); `mutant-probe.py` kann ihn dann als
  `test`-Kommando nehmen statt einer Probe im Scratchpad.
- Liest die Fallliste aus [B-493](B-493-fallliste-antwortvergleich.md), sobald es sie gibt.

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
