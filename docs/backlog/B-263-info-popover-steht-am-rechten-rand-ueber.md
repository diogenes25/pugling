---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/supervisor]
aliases: [ⓘ-Popover am rechten Rand, Feldhilfe scrollt die Seite seitlich]
status: in-arbeit
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Messung beim Bau von B-68 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md, Sprint 10)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Fortsetzung Sprint 10 (unterbrochen: Rechner aus)"
---

# B-263 · Ein aufgeklapptes ⓘ am rechten Rand lässt die Seite seitlich scrollen

## User Story

Als Vater am Tablet möchte ich die Erklärung zu einem Feld aufklappen können, ohne dass die Seite seitlich
verrutscht oder die Erklärung abgeschnitten ist.

## Ist-Stand am Code

`.info-hint-pop` ist am Knopf verankert (`position: absolute; left: -6px`, `frontend/src/index.css`) und bis zu
`min(320px, 78vw)` breit; `InfoHint` (`frontend/src/components/InfoHint.tsx`) setzt keine Lage. Steht das ⓘ nahe
am rechten Rand, ragt das Popover über. Gemessen beim Bau von B-68 bei 768 px (Wegwerf-Spec, Seed-Daten): an
sieben ⓘ auf `/vater/exercises/neu` (2), `/vater/kind/1`, `/vater/shop`, `/vater/rewards` (2) und `/vater/vocab`
steht das Popover **91 bis 213 px** über den Rand von `.vater-main` – ohne B-68 scrollt dann die ganze Seite
seitlich, mit B-68s `overflow-x: auto` an `.vater-main` würde es dort abgeschnitten.

## Die echte Lücke

Das Popover kennt den Platz nicht, in dem es aufgeht.

## Offene Punkte

1. ~~Verschieben, umklappen (rechtsbündig) oder per Portal aus dem Rahmen heben?~~ → Entscheidung 1.

## Entscheidungen

1. **Beim Öffnen messen und nach links verschieben, so weit es übersteht – nie über den linken Rand hinaus.**
   Begründung: hält die gewohnte Lage (links am Knopf), wo Platz ist; eine Portal-Lösung bräuchte eigene
   Lageberechnung beim Scrollen. Grenze ist der sichtbare Streifen: das Fenster, verengt um jeden Vorfahren
   mit beschnittenem Überlauf (Tabellen-Rahmen, `.vater-main`). Kosten: ein `useLayoutEffect` je Öffnen; ein
   Fenster-Resize bei offenem Popover wird nicht nachgeführt (es schließt beim nächsten Klick ohnehin).
   Autonom gegrillt (`Defekt`, Freigabe 1).

## Akzeptanzkriterien

1. Bei 768 px steht kein aufgeklapptes ⓘ des Vater-Webs über den sichtbaren Rand von `.vater-main` – weder
   seitlich noch unten (dort klappt es über den Knopf, wenn oben Platz ist) –, und das Dokument wird dabei nicht
   breiter als das Fenster.
2. Wo Platz ist, bleibt das Popover in seiner bisherigen Lage.

## Schätzung

XS, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg: `responsive-vater.spec.ts` (B-68) klappt jedes
sichtbare ⓘ der 22 Routen auf und misst beides.

## Verlauf

- **2026-10-24** — angelegt beim Bau von B-68 (Sprint 10, NP-5); im selben Sprint behoben nach Freigabe 3 (gezählt:
  Fund 1). Autonom gegrillt und geschätzt.
- **2026-10-24** — gebaut: `InfoHint.tsx` misst beim Öffnen (`useLayoutEffect`), verschiebt per `marginLeft` und klappt nach oben (`.info-hint-pop.up`), wenn `.vater-main` es unten abschnitte; die angewandte Lage wird beim Messen herausgerechnet (Review-Fund). Rote Proben siehe B-68. `frontend-reviewer` siehe B-68. **Unterbrochen (Rechner aus)**, Stufe `in-arbeit`.
