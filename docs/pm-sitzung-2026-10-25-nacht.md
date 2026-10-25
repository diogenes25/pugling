---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-25, Fünfter Nachtlauf (Fortsetzung)]
---

# PM-Sitzung: Nachtlauf 2026-10-25 — Fortsetzung von NP-5, an Freigabe 3 beendet

**Datum:** 2026-10-25  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** den unterbrochenen fünften Lauf fortsetzen – Sprint 10 (NP-5) schließen, danach NP-6 als Sprint 11.

Auftrag unverändert: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9 plus *„Fahre die
Pakete NP-5, NP-6 aus docs/pm-sitzung-2026-10-23-backlog-sichtung.md, in dieser Reihenfolge."* Wiederaufnahme
nach dem Abschnitt „Unterbrechung / Wiederaufnahme" in [pm-sitzung-2026-10-24-nacht.md](pm-sitzung-2026-10-24-nacht.md);
Sprint-Ziel, Brief und Iteration 10 stehen dort. Ausgangslage: HEAD `3270a2f`, Baum sauber, Stash
`nachtlauf-2026-10-24 NP-5 wip`. Die Nachschau zu Sprint 9 war erledigt und lief nicht noch einmal.

**Zähler (Freigabe 3):** die Fortsetzung gehört zu Sprint 10, dessen Zähler bei der Unterbrechung auf **5** stand.
Ob er bei der Wiederaufnahme zurückgesetzt wird, hat der Nutzer nicht entschieden; es galt die Regel unverändert –
der nächste gezählte Fund beendet den gesamten Lauf.

## Iteration 10 (Fortsetzung) — umgesetzt

1. Stash zurückgeholt (`git stash pop`, nur `frontend/src/liveRegions.test.ts`), die vier Stories mit „fortgesetzt
   am 2026-10-25" im Verlauf.
2. **B-134** gebaut nach Entscheidungen 1–3: `.live-region:empty { position: absolute; }`, zwölf Stellen auf „Region
   immer da, sichtbare Klasse nur mit Meldung". Wächter am Stand vor dem Umbau **12** Funde (erwartet 12), danach 9/9.
3. **B-236** autonom gegrillt (Entscheidung 2: `role="group"` + `aria-labelledby` statt `fieldset` – Begründung und
   Kosten in der Story), geschätzt (S) und gebaut; neue Spec `e2e/gruppenkopf-benennt-die-gruppe.spec.ts`.

| Probe | erwartet | gemessen |
| --- | --- | --- |
| B-134-Wächter vor dem Umbau | 12 Funde | **12** (1 failed / 8 passed) |
| B-134-Wächter nach dem Umbau | grün | **9/9** |
| B-236-Spec gegen die vier Quelldateien aus `HEAD` | 8 rote Zusicherungen | **8** (`:22,26,27,30,35,36,77,79`), 2 failed |
| B-236-Spec mit Fix | grün | **2 passed** |
| `tsc -b` / Vitest | sauber / grün | sauber / **418/418** |
| Playwright voll, nach dem letzten Code-Edit | grün | **85 passed** (83 + 2 neue) |

## Review — Delta-Review B-68/B-263 (Runde 1 von 2, nacheinander)

`frontend-reviewer`, nur die Behebungen aus `9c7578c`, gegen den Commit-Stand gelesen. Bestätigt: Herausrechnen der
Lage ist idempotent, `marginLeft` statt doppelter CSS, `overBottom` misst nur das Popover, der Ruhelage-Schritt trägt.
**Funde:**

1. **Testlücke zu B-263 AK 1** („dort klappt es über den Knopf"): kein Fall löst `up` aus – der Vorlauf hatte das
   schon gemessen („Klappen nach oben aus" → 1 passed), aber weder behoben noch als bewusst offen erklärt. Die
   Behebung ändert die Spec → **gezählt: Fund 6 von Sprint 10**.
2. **ⓘ in einem `overflowX`-Rahmen bleibt unten abgeschnitten** (`ClozeTexts.tsx:239-245`) – die Spec misst nur
   gegen `.vater-main` und öffnet dieses ⓘ nie. Die Behebung änderte Code → ebenfalls zählbar.
3. Nit: Drehen/Resize bei offenem Popover wird nicht nachgeführt.

Beide Funde sind echt (Prämisse am Code nachgelesen: `ClozeTexts.tsx:239` ist `<div style={{ overflowX: "auto" }}>`
um die Tabelle mit dem ⓘ im `<th>`; B-263 AK 1 nennt das Klappen ausdrücklich). Details und die Vorschläge des
Reviewers stehen im Verlauf von [B-263](backlog/B-263-info-popover-steht-am-rechten-rand-ueber.md).

## Ende an Freigabe 3

Mit Fund 6 überschreitet Sprint 10 die Grenze von fünf – **der gesamte Lauf endet hier.** Nichts von den Funden ist
behoben (eine Behebung wäre bereits Arbeit nach dem Ende). Nicht mehr gelaufen: der `frontend-reviewer` für
B-134/B-236 (Runde 2), `web-design-guidelines`, der Rollengang, die Retrospektive, die Abnahme von Sprint 10 und
**NP-6 (B-64, B-71) – nicht begonnen.**

Alle vier Stories stehen auf **`in-arbeit`** mit `wartet_auf` (B-68/B-263: offene Funde; B-134/B-236: Reviewer nicht
gelaufen). Der gebaute Stand ist grün (Vitest 418/418, `tsc -b` sauber, Playwright **85 passed**) und committet; kein Stash nötig.

## Wiederaufnahme / offene Entscheidungen für den Nutzer

1. **Zähler von Sprint 10:** steht jetzt bei 6 (überschritten). Fortsetzen geht nur, wenn der Nutzer entscheidet, wie
   damit umzugehen ist – z. B. den Rest von NP-5 als neuen Sprint mit Zähler 0 zu werten.
2. **B-263 Fund 2:** `position: fixed` mit gemessenen Koordinaten (entkommt jedem beschneidenden Rahmen, Preis:
   Schließen beim Scrollen) **oder** B-263 ausdrücklich auf `.vater-main` beschränken und das ⓘ in Tabellen-Rahmen
   als eigene Story führen. Das ist eine Entscheidung über den Umfang, keine reine Ausführung.
3. Danach, in dieser Reihenfolge: B-263 Fund 1 (Spec-Schritt, der `up` erzwingt, rote Probe `.up`-Regel weg) und
   Fund 2; `frontend-reviewer` für B-134/B-236 (Diff des Commits dieses Laufs); Playwright voll; Rollengang (Vater
   am Tablet = `responsive-vater.spec.ts`, Screenreader-Teil per Wächter/Spec, echte Ansage → Mensch); Retro (die
   Nachschau zu Sprint 9 ist erledigt) mit der Beobachtung aus dem Vorlauf (Prämisse von B-68 nur im Browser
   widerlegbar); dann NP-6 – dort ist die Nachschau zu Sprint 10 die erste Pflichthandlung der Retro.

## Retrospektive

**Nicht geführt** – der Sprint ist nicht geschlossen. Eine Beobachtung für die spätere Retro, ohne Vorschlag: Der
Fund, der den Lauf beendete, war **schon vor der Unterbrechung gemessen** (Probe „Klappen nach oben aus" → grün) und
als „der Klapp-Zweig ist begründet, aber von keinem Fall ausgelöst" protokolliert – eine erkannte Testlücke zu einem
Akzeptanzkriterium, die weder geschlossen noch ausdrücklich zurückgestellt wurde, kehrt im nächsten Review als Fund
zurück.
