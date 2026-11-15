---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf als PO und Stakeholder

**Datum:** 2026-11-15  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (Friedrich/Theodor, Student) · Entwickler
**Ziel:** Offene Stories im Index auf höchstens 0,7 × N senken (alles im Index gezählt), mindestens 30 % von N in
Sprints eingearbeitet; Stakeholder-Rolle beim Grillen ausdrücklich übertragen, auch für `Wunsch` und `Frage`.

## Auftrag und Ausgangswert

- Branch `auto/backlog-lauf-2026-11-15` im Worktree `.claude/worktrees/backlog-lauf-1115`, abgezweigt vom HEAD des
  Haupt-Checkouts (`auto/fertigstellung`, `28a6e99a`).
- Der erste Indexlauf im Haupt-Checkout meldete **62 offen**. Die Zahl ist falsch als Ausgangswert:
  `auto/fertigstellung` lag 3 Commits vor und **31 Commits hinter** `main` – ihm fehlten die Sprints 73–76 des Laufs
  vom 2026-11-14. Der Lauf-Branch muss am Ende per Fast-Forward auf `main`, trägt `main` also ohnehin. Darum zuerst
  `main` hineingemergt (`d39770b1`, Konflikte nur in B-249 und dem Index) und **dann** gezählt:
  **N = 28 offen** (493 abgenommen, 35 verworfen). Mit 62 wäre das Ziel ohne eine Zeile Arbeit erfüllt gewesen.
- Zielmarken: offen am Ende **≤ 19** (0,7 × 28 = 19,6); eingearbeitet **≥ 9** (30 % von 28 = 8,4).
- Entscheidungen, die ich als Stakeholder treffe, tragen den Vermerk „Stakeholder-Entscheidung im Auftrag des
  Nutzers". Frühere ausdrückliche Entscheidungen des Nutzers bleiben.
- Parallele Sitzung: der Haupt-Checkout trägt ungesicherte Änderungen am Skill `lehrwerk-aufbereiten` und an
  `docs/api-examples/catalog.md` – Stories, die diese Dateien berühren, stelle ich zurück.

## Abgleich gegen den Code

Vier Agenten haben die 28 offenen Stories parallel gegen den Code geprüft. Ergebnis:

| Urteil | Stories |
| --- | --- |
| gilt | B-552, B-538, B-536, B-539, B-553 (vorher B-540), B-547, B-219, B-218, B-248, B-249, B-250, B-221, B-376, B-31, B-39, B-410, B-09, B-377, B-555, B-252, B-223 |
| teilweise erledigt, Kern gilt | B-220 (`PenaltyCoins` schon im Vertrag), B-556 (B-529 liefert die Erklärung für Regel-Aufgaben), B-557 (Konsole kann `--use-weak` + `assign`, es fehlt der Griff im Vater-Web), B-369 (Zeilen gewandert) |
| veraltet, neu zuschneiden | B-47 (Azure-Weg tot; der gefahrene Betrieb ist `scripts/heimnetz.ps1`) |
| **verworfen** | **B-554** (geteilt – Sammel-Story ohne eigene Arbeit, Muster B-247) · **B-251** (gegenstandslos durch B-470, gemessen in B-459) |

### Buchhaltungs-Funde beim Abgleich

- **Doppelte Ids.** `auto/fertigstellung` hatte B-540…B-544 neu vergeben, während `main` dieselben Nummern im Lauf
  vom 2026-11-14 abgenommen hatte. Umgezogen: B-540 → **B-553** (Plan aus Unit), B-541 → **B-554**, B-542 → **B-555**,
  B-543 → **B-556**, B-544 → **B-557**; Verweise in B-537, B-248, B-249, B-09, `docs/obsidian.md` und
  `docs/rockettutor-vergleich-plan.md` nachgezogen. Kein Tor sieht doppelte Ids – Vorschlag für die Retro.
- **Konfliktmarken.** Mein Merge-Commit `d39770b1` trug unaufgelöste Marken in B-09 und B-248: die Merge-Ausgabe war
  per `tail` abgeschnitten, und `git add -A` hat die Dateien als gelöst markiert. Behoben in `d41df00a`.

Nach dem Abgleich: **26 offen**.

### Zurückgestellt (nicht in diesem Lauf)

- **B-31** – wartet auf ein echtes Handy.
- **B-536** – XL, Karte; geht in den Dialog.
- **B-09, B-248, B-249, B-555** – L/XL mit Migration, hängen aneinander (Klassenarbeit-Kette) bzw. an der Teilung.
- **B-218, B-220, B-250** – Queue-Kette, hängt an B-248.
- **B-221, B-376** – Gems/Auszeichnungen, Migration; nachrangig zu den P1.
- **B-223** – Vertragsbruch an `PracticeCard`; nachrangig.
- **B-369** – L, Teilung nötig (Vater-Web/Arcade); würde erst eine Story mehr erzeugen.
- Parallele Sitzung im Haupt-Checkout arbeitet am Skill `lehrwerk-aufbereiten` – keine der gewählten Stories berührt ihn.

## Refinement

Drei Agenten treiben je drei Kandidaten bis `geschaetzt` (nur Doku, getrennte Dateien): Vater-Plan (B-552, B-539,
B-553), Vater-Sicht (B-557, B-39, B-377), Sohn/Entwickler (B-219, B-47, B-556). B-538, B-547, B-252 und B-410 waren
schon geschätzt.
