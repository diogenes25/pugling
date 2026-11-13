---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [laden.py automatisch testen, Vorschlag CQ]
status: abgenommen
prio: P2
art: Aufräumen
groesse: "S"
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

## Entscheidungen

1. `test_laden.py` neben dem Lader, `unittest` aus der Standardbibliothek, lädt `laden.py` per `importlib`; Fälle aus
   B-483/B-487/B-491 (Lückentext-Klammern, Fehlermeldungen, Wortbank ohne doppelte Kacheln) und die Fallliste (B-493).
2. CI ruft ihn im Backend-Job nach dem Build (`python3 -m unittest discover …`).
3. Ein neues Tor – nach `README` hält der Lauf danach; stellvertretend entschieden: der Lauf endet ohnehin mit diesem
   Sprint (6-Stunden-Rahmen).

## Akzeptanzkriterien

1. Der Test läuft lokal und in CI ohne Server. 2. Ein `graded()`, das vom Server abweicht, ist rot.

## Verlauf

- **2026-11-12** — angelegt nach der Entscheidung des Nutzers über die Retro-Vorschläge des Backlog-Laufs.
- **2026-11-13** — ausformuliert und geschätzt S (Tageslauf, Sprint 62; Freigabe des Nutzers vom 2026-11-12 liegt vor).
- **2026-11-13** — gebaut (Sprint 62). `test_laden.py` (4 Tests: Fallliste, Klammern, Fehler, Wortbank), CI-Schritt
  „Lehrwerk-Lader (Python)". R2 (`graded()` ohne U+FEFF) → rot (von Hand gemessen: `mutant-probe.py` erkennt das
  `OK` von `unittest` nicht als grün – Werkzeug-Lücke, benannt).
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
