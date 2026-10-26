---
tags: [typ/protokoll, bereich/pm]
aliases: [Läufe nach der Morgen-Entscheidung 2026-10-26]
---

# PM-Sitzung: Läufe A–E nach der Morgen-Entscheidung 2026-10-26

**Datum:** 2026-10-26  ·  **Moderation:** PM (unbeaufsichtigt nach dem Dialog)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** die liegengebliebenen Sprints 10, 12 und 7 schließen, danach NP-3, NP-7 und NP-9 fahren, nacheinander.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–9. Grundlage ist der Abschnitt
[Morgen-Entscheidung 2026-10-26](pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26) (Commit `732e4ce`):
Jede Nacharbeit läuft mit **neuem Zähler (0)**. Die Retro-Regel zu Abgrenzungs-Testdaten ist seit `732e4ce` in
pm-loop Step 5 gelandet und gilt hier erstmals als Pflichtschritt. Ausgangslage: HEAD `732e4ce`, Baum sauber.

## Lauf A — Fortsetzung Sprint 10 (NP-5)

**Sprint-Ziel (unverändert aus dem fünften Lauf):** *Der Vater kann das Vater-Web am Tablet bedienen, ohne dass
eine Seite seitlich scrollt oder eine Feld-Erklärung abgeschnitten ist, und ein Screenreader kündigt Meldungen und
Kästchen-Gruppen an.* Umfang: B-68, B-263, B-134, B-236.

**Zähler (Freigabe 3): 0** zu Beginn. Fund 1 und 2 aus dem Delta-Review vom 2026-10-25 sind die Nacharbeit selbst und
werden nicht neu gezählt.

### Iteration — B-263 nach Entscheidung 2 (`position: fixed`)

`InfoHint.tsx` rechnet `left`/`top` aus dem Knopf, begrenzt durch das Fenster. Das Popover klappt nach oben, wenn
unten im Fenster kein Platz ist, und schließt bei `resize` sowie bei `scroll`, wenn sich der Knopf bewegt hat.
`.info-hint-pop` ist `position: fixed`. Neue Spec `e2e/feldhilfe-lage.spec.ts`. `responsive-vater.spec.ts` misst gegen
das Fenster plus Treffertest an den Ecken.

Zwei Messungen auf dem Weg, je mit Konsequenz (Einzelheiten im Verlauf von B-263):

1. Ein Schließen bei *jedem* `scroll` schloss das Popover gleich nach dem Öffnen (3 von 4 Tests rot), denn das
   Scrollen, das den Knopf ins Bild holt, kommt erst im nächsten Frame an. Seither wird mit der Lage des Knopfs
   verglichen.
2. Der Treffertest mit dem obersten Element 3 px vom Eck meldete Verdecktes, wo nichts abgeschnitten war:
   Scrollbalken-Trefferzone von `.vater-main` und `border-radius`. Seither `elementsFromPoint` mit 16 px Abstand.

| Probe | erwartet | gemessen |
| --- | --- | --- |
| HEAD-Stand gegen `feldhilfe-lage` | 3 rot | **3 failed** (`:69` Treffertest, `:93` Klappen, `:109` Schließen) |
| `up = false` | Klappen rot | **2 failed** (Klappen + Tabelle, die bei 1024 px ebenfalls klappt) |
| ohne `scroll`- / `resize`-Listener | je rot | **`:109`** / **`:114`** |
| Schließen bei jedem `scroll` | rot | `feldhilfe-lage` grün, **`responsive-vater` 1 failed** (`:70`) |
| ohne Klemmen nach links | rot | **22** `overRight`-Meldungen |
| beide Specs / Vitest / `tsc -b` | grün | **4 passed** / **428/428** / sauber |

**Abgrenzungen im Testaufbau (Pflichtschritt, pm-loop Step 5):** *oben statt unten*: Die Vorbedingung `untenZuEng`
(`feldhilfe-lage.spec.ts:88`) trennt beide Fälle, bei „beide gleich“ fällt `:93`. *Ragt über den Rahmen hinaus*:
Die Vorbedingung `ragtHeraus` (`:68`) stellt sicher, dass der Treffertest nicht trivial grün ist. *Ruhelage statt
verschoben*: Der Schritt „Ruhelage bleibt“ (`dx: -6`) steht gegen die 22 verschobenen Popover. *Schließen nur bei
Bewegung*: Die Mutante „immer schließen“ trägt nur `responsive-vater:70`. Das ist benannt, nicht verdoppelt.

### Review (nacheinander, je Bereich)

**Zähler Lauf A (Freigabe 3): 2** nach Runde 1.

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` B-134/B-236 | `TruncationHint` hinter `&&` in `PlanPositions:621` (Filterwechsel stumm) und `VaterClassTests:231` | Region immer gerendert, Bedingung in `total` | ja |
| 2 | `frontend-reviewer` B-134/B-236 | Wächter sieht keine bedingt eingehängten Region-Bausteine; eine eingerückte innere Funktion beendet die Suche nach `return null` | `REGION_BAUSTEINE`, Kopf in Spalte 0, Klammertiefe gegen `return null` in inneren Funktionen; `RemarkWidget` als begründete Ausnahme | ja |

Proben: erweiterter Wächter gegen die alten Aufrufstellen → **2** Funde (erwartet 2). Der Spalte-0-Anker allein meldete
zusätzlich `VaterWizard:526/644` falsch, das hat die Klammertiefe ausgelöst. Mutanten „jedes `return null`“ → 3 failed,
„Kopf eingerückt“ → 2 failed. Vitest **435/435**.

Nicht gezählt, ausdrücklich zurückgestellt: die Hinweise 3–6 (🟢: Selbsttests des Wächters, `TruncationHint`-Vitest,
Name „Freigabe (Startzustand)“, Gruppen-Spec zu weit) → [B-272](backlog/B-272-live-region-und-gruppen-tests-tragen-nicht-alles.md).
Jede Behebung hätte gezählt, und das Review zu B-263 stand noch aus. Das ist dieselbe Abwägung, die der Nutzer bei
B-268/B-269 als richtig bestätigt hat.

**Runde 2 — `frontend-reviewer` zu B-263.** Zähler danach: **4**.

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 3 | `frontend-reviewer` B-263 | Weder unten noch oben Platz (Querformat): Das Popover lief aus dem Fenster und ließ sich fest stehend nicht herscrollen | `top` ins Fenster geklemmt, `max-height` + `overflow-y: auto` | ja |
| 4 | `frontend-reviewer` B-263 | Abgrenzungen ungetragen: senkrechte Schnitte < 16 px, „bleibt offen“ nur zufällig getragen, Schwelle 40 px | `imFenster` mit genauen Koordinaten, Fall Querformat, Fall „Scrollen von vor dem Öffnen“, Probe 3 px | ja |

Proben: ohne Klemmen und Klemmen bis an den Rand → je rot an `:132`, „jedes `scroll`“ → rot im neuen Fall, Schwelle
39 px → rot an `:162`. Specs **6 passed**. Zurückgestellt → [B-273](backlog/B-273-feste-feldhilfe-resize-und-layoutverschiebung.md):
`resize` ohne Prüfung (ungemessen; Chrome Android ab 108 und iOS lösen es nicht aus), Layout-Verschiebung ohne Scrollen,
die nicht prüfbare Linksrand-Regel. Doku-Funde ohne Zählung nachgezogen.

**Beobachtung:** Fund 4 gehört zur Klasse, die der seit heute geltende Pflichtschritt fangen soll. Ich hatte die
Abgrenzungen benannt, aber nur die gegen *meine* Mutanten. Eine Abgrenzung über eine Toleranz (16-px-Saum,
40-px-Scroll) hat der Schritt nicht erfasst.

**Runde 3 — Delta-Review über die Behebungen.** Zähler danach: **5** (Grenze erreicht, nicht überschritten).

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 5 | Delta-Review | Der Querformat-Fall erreichte `max-height` nie (gemessen 101–271 px gegen 344 px) | eigener Fall 768×200, Vorbedingung „höher als das Fenster“, `scrolltInSich`; ohne `max-height` rot an `:148` | ja |

Ohne Zählung: zwei Kommentare nachgezogen (`VaterClassTests`: Region erst nach dem Laden der Arbeit; Wächter-Kopf:
Lücke „`if` mit Lambda in der Bedingung“). Der Hinweis „`REGION_BAUSTEINE` ohne Selbsttest je Name“ ist zurückgestellt
→ B-272 Punkt 5, weil er als Fund 6 den gesamten Lauf beendet hätte.

### Runde — Rollengang / Abnahme

Die Chrome-Extension war nicht verbunden, darum nach Freigabe 6 als E2E. **Vater:** `responsive-vater.spec.ts` (22
Routen bei 768 px, jedes ⓘ aufgeklappt, nichts scrollt seitlich), `feldhilfe-lage.spec.ts` (Tabellen-Rahmen, Klappen,
Querformat, niedriges Fenster, Schließen), `gruppenkopf-benennt-die-gruppe.spec.ts`. Das Sprint-Ziel hält für alles,
was eine Maschine sehen kann. **Sohn, Creator:** Regressionszeugen. Volle Suite grün, die Sohn-Stellen von B-134
(`SohnPractice`, `SohnShop`) fährt `full-flow.spec.ts` mit. **Offen für einen Menschen:** die echte Screenreader-Ansage
der Live-Regionen und Gruppen-Namen, und das Popover am echten Tablet.

Verifikation: `tsc -b` sauber, Vitest **435/435**, Playwright voll nach dem letzten Code-Edit **93 passed** (5,9 min),
danach `feldhilfe-lage` mit dem nur testseitigen Zusatz **6 passed**. **B-68, B-263, B-134, B-236 abgenommen.**

### Retrospektive

**Nachschau:** Die zu Sprint 10 gehörige Nachschau (Sprint 9) ist seit dem fünften Lauf erledigt
([Protokoll 2026-10-24](pm-sitzung-2026-10-24-nacht.md)). Index: **Nachgeschaut 182 von 184**. Die zwei
unbeobachteten sind B-64 und B-71 (Sprint 11), ihre Nachschau ist die erste Pflichthandlung von Lauf B. Sprint 10
selbst ist erst jetzt geschlossen, seine Nachschau fällt darum ebenfalls in die nächste Retro.

**Was die eigenen Tore durchgelassen haben:** Der seit heute geltende Pflichtschritt (Abgrenzungen im Testaufbau) lief
und hat drei Abgrenzungen sauber belegt. Durchgelassen hat er zwei Formen, beide gemessen:

1. **Abgrenzung über eine Toleranz** (Fund 4): ein Treffertest mit 16 px Saum, eine Scroll-Probe von 40 px. Die
   Mutante „beide gleich“ ist dort keine Gleichheit, sondern eine verschobene Grenze unterhalb der Toleranz.
2. **Eine Regel, die die Testdaten nie erreichen** (Fund 5): `max-height` greift bei keinem Popover des Seeds. Die
   Vorbedingung „Grenze wird erreicht“ fehlte.

Wohlgemerkt: Das ist eine Messung an *einem* Lauf. Belegt ist, dass der Schritt diese zwei Formen nicht fängt. Wie
oft sie vorkommen, ist nicht belegt.

**Vorschlag (nur vorgeschlagen, Freigabe 3):** den Pflichtschritt in pm-loop Step 5 um einen Satz erweitern: *„Hat der
Fall eine Toleranz oder Grenze (Saum, Schwelle, Höchstwert), gehört die Mutante ‚Grenze knapp verschoben‘ dazu, und die
Vorbedingung, dass die Testdaten die Grenze überhaupt erreichen.“* Prosa, kein Tor, aus demselben Grund wie der
Schritt selbst.

**Beobachtung aus dem fünften Lauf, jetzt eingelöst:** Die erkannte Testlücke zu AK 1 von B-263 (Klappen nach oben)
ist geschlossen (`feldhilfe-lage:93`).

**Commit:** `0ecb31b` (nicht gepusht).
