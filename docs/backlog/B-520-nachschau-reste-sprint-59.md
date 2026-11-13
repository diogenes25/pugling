---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 59, TZ undefined]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 59 in der Retro von Sprint 61/62)
unverifiziert: false
entgangen_bei: [B-517]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-520 · Nachschau-Reste Sprint 59: eine Zone, die als „undefined" zurückkommt, und eine Zusicherung ohne Wirkung

Nachschau von Sprint 59 (frischer `frontend-reviewer` an `8485eab`, gelesen, Vitest in Berlin, UTC, Los Angeles, Pago Pago
und Kiritimati per `spawnSync` – Git Bash reicht `TZ=America/…` nicht an Node durch).

1. 🟡 `datum.test.ts`: `process.env.TZ = vorher` mit `vorher === undefined` schreibt „undefined" – Node rechnet den Rest der
   Datei in `Etc/Unknown` (wie UTC). Lokal unter Windows ist TZ nie gesetzt.
2. 🟡 `formatDate("2026-11-08")` unter Kiritimati kann nicht rot werden: `TAG` ist beim Import gebaut und hält dessen Zone.
3. 🟢 Zwei ältere Tests (`T12:00:00Z`) fallen östlich von UTC+12.
4. 🟢 Lücken im `ROH`-Wächter (`{ date }`, Template, JSX-Text, `aria-label`) und im Steuerzeichen-Tor (`index.html`,
   `*.mjs`, `*.json`; Mindestzahl je Wurzel) – benannt, offen.

## Entscheidungen

1. Eine Hilfsfunktion `inZone` setzt die Zone und stellt sie wieder her – bei vorher leerer Variable per `delete`.
2. Die wirkungslose `formatDate`-Zeile entfällt, mit Begründung im Kommentar; die Regel hält `formatDateIn(…, Kiritimati)`.
3. Die älteren Tests nehmen 06:00 UTC – derselbe Tag in jeder bewohnten Zone.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 59, Retro Sprint 61/62) und gebaut im Sprint 62: `datum.test.ts` 22/22.
  Kein Mutant: alle drei Punkte sind Testaufbau; Punkt 2 ist eine entfernte Zeile. Abgenommen mit dem Commit des Sprints.
- **2026-11-13** — nachgeschaut (Sprint 63, frische `pugling-reviewer`/`frontend-reviewer` an `e37f65e`, gelesen, Vitest 102/102). Geprüft: CI-Schritt läuft (python3, Arbeitsverzeichnis, Pfade), xUnit-Leser findet die Liste, alle drei Leser stimmen Schritt für Schritt überein, `Map`-Aufrufer laden die Übung. Kein 🔴; Reste → [B-521](B-521-nachschau-reste-sprint-60-62.md).
