---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/vater, rolle/sohn]
aliases: [radioGroupKeys Restkanten, Wartebild Streak nach dem Verlassen]
status: abgenommen
prio: P4
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf N, Teil 1 (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: []
wartet_auf: ""
---

# B-356 · Restkanten an Radiogruppen und Wartebild (Nachschau über `b73bcde`)

Fünf 🟢 der Nachschau über B-350/B-352, keiner eine Entgleitung:

1. **`radioGroupKeys` klickt die schon gewählte Option erneut** (`frontend/src/lib/ui.ts:56-57`), wenn der Schritt auf
   sie zeigt (Home auf der ersten, End auf der letzten, eine einzige freie Option). Im Assistenten setzt das
   `setTouchedMode(true)`, solange „Bestehendes Kind" gesperrt ist – die spät ankommende Liste belegt dann nicht mehr vor
   (B-177). Ein Mausklick tut dasselbe. Weg: `if (to === at) return;`.
2. **`preventDefault` an Pfeiltasten trägt keine Zusicherung** (`lib/ui.test.ts`): die Mutante „ohne `preventDefault`"
   bliebe grün; im Browser scrollten Pfeil oben/unten und Home/End dann die Seite.
3. **Der Wächter nimmt eine ganze Datei aus, sobald ein natives Radio darin steht** (`radioGroups.test.ts:21`); eine
   Knopf-Gruppe neben nativen Radios (`PlanPositions.tsx`) bliebe ungeprüft. Dazu nicht erkannt: `role={"radio"}`.
4. **Nach dem Verlassen frischt `finish` nur das Guthaben auf** (`SohnTest.tsx:68`), nicht die Serie in der Kopfzeile.
5. **Der Fall „fertiger Versuch" prüft `celebrate` nur negativ** (`SohnTest.test.tsx:333-334`), ohne festzustellen, dass
   `submitTest` erreicht wurde – ein zusätzliches `await` vor dem Absenden machte ihn leer.

## User Story

Als **Vater** möchte ich, dass eine Taste in einer Auswahlgruppe nur dann etwas wählt, wenn sie die Wahl ändert, und als
**Sohn**, dass die Kopfzeile nach einer verlassenen, aber abgegebenen Klausur meine Serie zeigt – damit beide Bedienungen
halten, was sie ansagen, und ihre Tests das auch festhalten.

## Ist-Stand am Code (Lauf P, Stand `299d216`)

1. `frontend/src/lib/ui.ts:55-56` fokussiert und klickt `radios[to]` auch bei `to === at`; `VaterWizard.tsx:419-421` setzt im
   `onClick` `setTouchedMode(true)` – der Effekt `:159` belegt danach nicht mehr vor.
2. `lib/ui.test.ts` prüft `defaultPrevented` nur für Tab (`false`), für keine Pfeiltaste.
3. `radioGroups.test.ts:21` gibt `[]` für jede Datei mit `type="radio"`; `PlanPositions.tsx:571/581` ist die einzige solche.
   `role={"radio"}` matcht `role="radio(group)?"` nicht.
4. `SohnTest.tsx:68` ruft nach dem Verlassen nur `refreshWallet()`; `setStreak` (`:73`) nur im Weg mit Ergebnis.
5. `SohnTest.test.tsx:332` prüft `celebrate` negativ, `submitTest` gar nicht.

## Die echte Lücke

Zwei kleine Verhaltensfehler (1, 4) und drei fehlende Zusicherungen (2, 3, 5) – alle wie beschrieben.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`if (to === at) return;` nach `preventDefault`.** Die Taste gehört weiter der Gruppe (kein Scrollen), wählt aber nichts
   neu. *Kosten:* keine – der Fokus steht schon dort.
2. **Der Wächter urteilt je Gruppe** (vom `role="radiogroup"` bis zur nächsten); eine Gruppe mit nativen Radios ist
   ausgenommen, jede andere braucht den Helfer am eigenen öffnenden Tag. `role={"…"}` wird vorher auf die Attribut-Form
   gefaltet. *Kosten:* „Gruppe" ist eine Textstrecke, kein Syntaxbaum – eine native Gruppe hinter einer Knopf-Gruppe ohne
   eigene `radiogroup` dazwischen nähme die Knopf-Gruppe mit aus (heute nirgends).
3. **Nach dem Verlassen frischt `finish` Guthaben und Serie auf**, mehr nicht (kein Ergebnis, keine Feier).

## Akzeptanzkriterien

1. Home auf der ersten, End auf der letzten Option und ein Pfeil bei einer einzigen freien Option klicken nichts.
2. Pfeil oben/unten, Home und End sind `defaultPrevented`.
3. Eine Knopf-Gruppe ohne Helfer neben einer nativen Gruppe und eine Gruppe in Ausdrucks-Form werden gemeldet.
4. Nach dem Verlassen mit fertigem Versuch steht die Serie aus `overview` in der Kopfzeile.
5. Der Fall „fertiger Versuch" fällt, wenn `submitTest` nicht erreicht wird.

## Schätzung

`S`, frontend, keine Migration, kein Vertragsbruch. Testweg: `lib/ui.test.ts`, `radioGroups.test.ts`, `SohnTest.test.tsx`,
E2E-Gesamtlauf als Regression.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu Lauf N, Teil 1 (`frontend-reviewer` über `b73bcde`, 🟢 ×5). Aus Code
  gelesen.
- **2026-11-03** — Lauf P, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Proben: AK 1
  **1 failed** (erwartet 1, `[ 'A', 'C' ]` statt `[]`); AK 3 **2 failed** (erwartet 2); AK 4 **1 failed** (erwartet 1, an
  `setStreak`, die `submitTest`-Zeile davor hielt). AK 2 und AK 5 vor jeder Mutante grün (Testlücke). Mutanten: „ohne
  `preventDefault`" **4 failed** (AK 2); „`await` vor der Abgabe" **1 failed** an `submitTest` (AK 5); AK 1/3/4 tragen ihre
  roten Proben. Vitest **619/619**, `tsc -b` sauber.
- **2026-11-03** — Review `frontend-reviewer`: 🟡 die Scroll-Hälfte von Entscheidung 1 trug keinen Test – die naheliegende Mutante „Wächter `to === at` vor `preventDefault`" blieb grün, weil der AK-1-Fall nur Wahl und Fokus prüfte und der `preventDefault`-Fall nie auf `to === at` traf. Behoben: AK-1-Fall prüft `defaultPrevented` an Home auf der ersten und am Pfeil bei einer einzigen Option; Mutante **1 failed** (zählt). 🟢 der Wächter prüfte nicht, wo der Helfer sitzt (Anker am Gruppen-Tag) → Selbsttest „Helfer an einem Kind", Mutante „ohne Anker" **1 failed** (ohne Zählung). Englische Kommentare im deutsch kommentierten Umfeld auf Deutsch umgestellt. Vitest **619/619**.
- **2026-11-03** — Abnahme Lauf P, Teil 1: Vitest **619/619**, `tsc -b` sauber, E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Vater maschinell – die Radiogruppen des Vater-Webs fahren die Specs mit Tastatur und Klick (`radiogruppen`-Wächter über alle Quellen, Assistent in seinen Specs); Sohn über `SohnTest.test.tsx` (Verlassen mit fertigem Versuch, Kopfzeile bekommt die Serie) und die Klausur-Specs im Gesamtlauf; Creator Regressionszeuge. Gerätecheck aus B-352 bleibt offen (Screenreader). **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf P, Teil 2 (frischer `frontend-reviewer`, `git show bffb583 -- frontend`, read-only): **Entgleitung** → [B-360](B-360-guthaben-nach-verlassener-klausur-ungetestet.md) (🟡: die Guthaben-Hälfte von Entscheidung 3 ohne Zusicherung, im selben Sprint behoben).** Geprüft: `radioGroupKeys` – Mutanten „Wächter davor", „nur first/last", „nur Home" fallen an `ui.test.ts:68-83`; `finish` nach dem Verlassen – ein Fehlschlag von `overview` landet im `catch` des Aufrufers, keine unbehandelte Ablehnung; Radiogruppen-Wächter – „ganze Datei ausnehmen", „ohne Anker", „ohne Faltung" je rot, `PlanPositions.tsx:571` einzige native Gruppe. Ist-Stand-Zeilen um eins verschoben → berichtigt (🟢, ohne Zählung).
