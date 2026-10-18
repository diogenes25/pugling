---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [TextbookForm Unit-Ladefenster, achte ReferenceSelect-Stelle, B-229 Prämisse]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 3, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-229]
wartet_auf: ""
---

# B-235 · Das Unit-Feld im Lehrbuch-Formular sagt „(nicht geladen)“, solange die Units erst laden

B-229 hat am Kind-Feld des Plan-Formulars „(lädt…)" von „(nicht geladen)" getrennt – und begründet, warum nur
dort: „Bei den sieben anderen `ReferenceSelect`-Stellen laden die Listen auf Seitenebene, bevor das Formular
aufgeht." Für sechs stimmt das. Für die **Unit im Lehrbuch-Formular** nicht: ihre Liste lädt im Formular selbst,
und beim Öffnen steht dort dasselbe Bild, das B-229 am Kind-Feld als Defekt eingestuft hat.

## User Story

Als **Vater** möchte ich beim Bearbeiten eines Lehrbuchs unter dem Kind-Hub sehen, dass die Units noch laden –
und „nicht geladen" nur lesen, wenn die Liste wirklich nicht kam.

## Ist-Stand am Code

Nachgesehen am 2026-10-18 (Nachschau durch einen frischen `frontend-reviewer`, von mir gegengelesen).

- `frontend/src/vater/ChildMaterialSection.tsx:192-193`: `const units = useAsync(() => … api.seriesUnits(seriesId),
  [seriesId])` steht **in** `TextbookForm` (ab `:153`), das per `{editing === b.id && <TextbookForm …>}`
  (`:97-100`) erst beim Klick auf „Bearbeiten" montiert. `form.seriesId` ist beim Bearbeiten aus dem Buch
  vorbelegt (`:185-186`) – der Unit-Abruf fliegt also beim Montieren.
- `:315-328`: `<ReferenceSelect loadedId={eigeneUnit ? book?.currentUnitId : null} … options={unitOptions(
  units.error ? [] : units.data ?? [], seriesId)} …>` – während des ersten Abrufs ist `units.data === null`,
  `options` leer, `eigeneUnit` wahr (gleiche Reihe) → `specialOption` liefert „<Unit> (nicht geladen)"
  (`ReferenceSelect.tsx:85`) als **gewählte** Option, neben einer Leer-Option, die „– lädt… –" sagt (`:326`).
  Zwei Beschriftungen, die sich widersprechen; die sichtbare ist die des Ausfalls. Kein `laedt` übergeben.
- Szenario: Vater öffnet unter `/vater/kind/:id` ein Lehrbuch mit Reihe und aktueller Unit → „Bearbeiten" → bis
  die Units da sind, steht „Unit 3 (nicht geladen)". Bei realer Latenz sichtbar, bei langsamer Verbindung
  sekundenlang.
- Die anderen sechs Stellen (`ChildMaterialSection` Fach/Reihe aus Props, `VaterFachlehrer` Fach/Lehrwerk,
  `VaterLehrwerke` Verlag/Fach) laden tatsächlich auf Seitenebene.

## Die echte Lücke

B-229 hat die Regel ans Bauteil gelegt („damit alle acht Stellen sie bekommen können"), aber nur eine Stelle
angeschlossen – und die Behauptung, die übrigen bräuchten es nicht, war gezählt, nicht nachgesehen. Der
`frontend-reviewer` zu Sprint 3 meldete „sieben andere Aufrufstellen unverändert" und prüfte dasselbe nicht.

## Offene Punkte

1. `laedt={units.showPlaceholder}` am Unit-`ReferenceSelect` (`:315`), wie im Plan-Formular – `showPlaceholder`,
   nicht `loading`, aus demselben Grund (nach Fehlschlag „(nicht geladen)"). Der B-195-Reihenwechsel ist
   unberührt (dort ist `eigeneUnit` falsch → keine Sonderoption). Die Leer-Option „– lädt… –" (`:326`) wird damit
   redundant zur Sonderoption, kann aber stehen bleiben (sie gilt auch für den Reihenwechsel ohne gespeicherte
   Unit). *Empfehlung: genau das, eine Zeile plus E2E oder Vitest am Formular.*

## Akzeptanzkriterien

1. Während der ersten Unit-Ladung zeigt das Unit-Feld „<Unit> (lädt…)" und ist gesperrt; danach die Unit regulär.
   *Beleg:* E2E am Lehrbuch-Formular mit verzögertem `…/units`-Abruf, im Fenster gemessen (Muster
   `plan-kinderliste-laedt-noch.spec.ts`), oder Vitest an `TextbookForm`, falls es sich ohne gefälschtes `fetch`
   rendern lässt.
2. Nach einem Fehlschlag unverändert „(nicht geladen)" mit Banner; der Reihenwechsel (B-195) unverändert.
3. Die Story B-229 trägt im Verlauf den Hinweis, dass ihre Prämisse für diese Stelle falsch war (erledigt am
   2026-10-18).

## Verlauf

- **2026-10-18** — angelegt aus der **Nachschau** zu Sprint 3 des Nachtlaufs (Commit `5a91d27`),
  `entgangen_bei: B-229`. Direkt `ausformuliert`: Ist-Stand mit `Datei:Zeile` belegt. **Der Fund beendet den
  Lauf nach Sprint 4** (Limit 2 des Backlog-Laufs: Defekt im Increment eines Sprints derselben Nacht) – dieselbe
  Lage wie B-227 am Ende des ersten Laufs.
