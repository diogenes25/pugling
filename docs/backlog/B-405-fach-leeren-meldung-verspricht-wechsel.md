---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Ausweg Fachwechsel überdehnt]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-aa
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-401]
---

# B-405 · Die Meldung beim Leeren des Fachs schlägt einen Fachwechsel vor, der genauso gesperrt sein kann

Die 409 beim Leeren (`TextbookSeriesController.cs` um `:204-206`, seit B-401) endet mit „…or switch to another subject instead". Das gilt
nur, wenn keine Übung und keine Unit mehr Themen oder Kategorien des alten Fachs trägt – sonst sperrt auch der Wechsel
(`Thema_An_Einer_Uebung_Sperrt_Den_Fachwechsel`). Der deutsche Text aus B-403 („oder wähle ein anderes Fach") übernimmt dieselbe
Überdehnung, ebenso der Test-Summary von `Fach_Leeren_Mit_Uebungen_Wird_Gesperrt`.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Der Ausweg „anderes Fach" bleibt im englischen Satz, aber **mit seiner Bedingung** („works once no
   exercise or unit carries grammar topics or categories of the current subject"); der deutsche Satz (B-403) nennt ihn gar nicht, weil er
   aus der Sperre selbst nie herausführt. Der Test-Summary nennt die Bedingung.

## Akzeptanzkriterien

| Fall | Beleg | Rote Probe |
|---|---|---|
| englischer Satz nennt die Bedingung | `ReiheFachwechselTests.Fach_Leeren_Mit_Uebungen_Wird_Gesperrt` (`once no unit carries grammar topics and no exercise`; berichtigt in B-406) | gegen den alten Satz gemessen: rot („Not found") |
| deutscher Satz verspricht keinen Wechsel | `errorMessage.test.ts` (`not.toContain("anderes Fach")`) | gegen den alten Satz gemessen: rot (Fall fiel an der ersten Zusicherung) |

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Z, Sprint 15 (Lauf AA, Retro Sprint 16; frischer `pugling-reviewer`,
  `git show 176eaf5`, 🟡). Increment von Lauf Z: **kein Limit 2** für Lauf AA. Weil B-403 im laufenden Sprint denselben Satz baut, kommt
  sie in denselben Sprint.
- **2026-11-05** — gebaut in Lauf AA, Sprint 16 (gemeinsam mit B-403, derselbe Satz). Volle Suite 1115/1115, E2E-Spec 1 passed.
- **2026-11-05** — Delta-Review: 🟢 der englische Satz unterstellte Units Kategorien → „no unit carries grammar topics and no exercise
  carries grammar topics or categories", Zusicherung nachgezogen und gegen den vorigen Satz gemessen: rot. Ohne Zählung (🟢).
- **2026-11-05** — **abgenommen.** Rollengang Creator: Integrationsfall (englischer Satz) und Vitest (deutscher Satz); die Anzeige im Formular deckt dieselbe E2E wie B-403. Volle Suite 1115/1115, Playwright 117 passed nach dem letzten Edit; Vater und Sohn Regressionszeugen. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `9f395d2`.
- **2026-11-05** — **Nachschau** (Lauf AA, Retro Sprint 17; frischer `frontend-reviewer`, `git show 9f395d2`): geprüft Belegzeilen gegen Tests, Texte gegen Server (beide Varianten) und Oberfläche (Themen-Picker an Unit und Dialog, „Art" nicht editierbar, Löschen erreichbar). **Entgangen:** → [B-406](B-406-fachwechsel-meldung-loeschen-kann-gesperrt-sein.md).
