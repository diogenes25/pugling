---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [TextbookForm Unit-Ladefenster, achte ReferenceSelect-Stelle, B-229 Prämisse]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 3, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-229]
wartet_auf: ""
nachgeschaut: "2026-10-23: frischer frontend-reviewer, gegengelesen - sechs Zustaende von laedt={units.loading} am Code durchgespielt (Montage, Fehlschlag, Wechsel weg, zurueck zur eigenen Reihe, Fehlschlag->Wechsel->zurueck, zurueck waehrend der fremde Abruf laeuft); alle sieben anderen ReferenceSelect-Stellen am speisenden useAsync geprueft (Datei:Zeile im Verlauf), keine haengt an einem Schluessel; kein Defekt - aber Entscheidung 1 hielt kein Test (Mutant showPlaceholder ueberlebte 42/42), vierter Vitest-Fall ergaenzt und gemessen; Docblock 'bevor das Formular aufgeht' war ein ungemessener Mechanismus -> umformuliert"
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

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **`laedt={units.loading}`, nicht `units.showPlaceholder`** – abweichend von der Empfehlung in Punkt 1 und
   vom Plan-Formular (B-229). Begründung: Die Unit-Liste hängt an einem Schlüssel (`[seriesId]`) und lädt bei
   jedem Reihenwechsel neu; `showPlaceholder` ist nach dem **ersten** Abschluss dauerhaft falsch
   (`useAsync.ts:40-41`, „bleibt danach `true`"). Wechselt der Vater die Reihe weg und wieder zurück, ist
   `eigeneUnit` wieder wahr, die Liste lädt erneut, und mit `showPlaceholder` stünde im zweiten Ladefenster
   wieder „(nicht geladen)" – derselbe Defekt, nur eine Handlung später. B-229s Grund für `showPlaceholder`
   („nach einem Fehlschlag soll es ‚(nicht geladen)' heißen") trägt mit `loading` genauso: nach einem
   Fehlschlag ist `loading` ebenfalls falsch. Die Kinderliste im Plan-Formular hat keinen Schlüssel, dort
   sind beide Felder gleichwertig – diese Story ändert B-229 nicht. **Kosten:** Zwischen dem Render mit dem
   neuen Schlüssel und dem Effekt, der `loading` setzt, liegt genau ein Commit, in dem `loading` noch falsch
   und `data` die alte Liste ist – gefiltert leer, also für einen Frame „(nicht geladen)". Das ist die
   bekannte Zwei-Schritt-Natur von `useAsync` (Memory „Zwischen-Render", B-227) und gehört zu B-164, nicht
   hierher; ein Vitest, der das Fenster messen will, muss den Zwischen-Render nachspielen.
   *Berichtigt in der Nachschau vom selben Tag (siehe Verlauf): der Zwischen-Commit wird nicht gemalt, und
   der Fall ist ohne Zwischen-Render testbar – der vierte Fall in `TextbookForm.test.tsx` hält ihn.*
2. **Die Leer-Option „– lädt… –" (`:326`) bleibt.** Begründung: Sie deckt den Reihenwechsel ohne gespeicherte
   Unit (`eigeneUnit` falsch → keine Sonderoption), den die Sonderoption nicht erreicht. **Kosten:** Im
   Ladefenster des Bearbeiten-Formulars stehen zwei Optionen, die beide „lädt" sagen – redundant, aber
   nicht widersprüchlich; gewählt ist die Sonderoption.
3. **Doc-Kommentar am `laedt`-Prop wird erweitert**, nicht nur die Aufrufstelle. Begründung: Er sagt heute
   „`useAsync.showPlaceholder` der Liste" und „weggelassen, wo die Liste auf Seitenebene lädt" – genau der
   Satz, der B-229 zur falschen Prämisse verleitet hat. Er nennt jetzt beide Quellen und wann welche gilt.
   **Kosten:** keine.
4. **Beleg auf zwei Ebenen:** ein Vitest an `TextbookForm` (Liste per `vi.spyOn(api, "seriesUnits")` auf ein
   nie auflösendes Promise, im Fenster gemessen) **und** eine E2E-Spec nach dem Muster
   `plan-kinderliste-laedt-noch.spec.ts`. Begründung: Der Vitest trägt die Regel am Bauteil; die E2E ist der
   **Rollengang-Ersatz** – die Chrome-Extension ist in dieser Sitzung nicht verbunden (Freigabe 6). **Kosten:**
   eine Spec mehr im vollen Lauf (~15 s).

## Schätzung

**Größe XS** (Anker B-02: Zeile plus Test). `wo: frontend`, keine Migration, kein Vertrag.

**Angriffsplan:** rote Proben (Vitest, E2E) → `laedt={units.loading}` in `ChildMaterialSection.tsx` →
Doc-Kommentar in `ReferenceSelect.tsx` → Suite → Reviewer → Commit.

**Risiko:** Der eine Frame aus Entscheidung 1 (bewusst, dokumentiert). Der B-195-Reihenwechsel ist unberührt
(dort keine Sonderoption); `kind-lehrbuch-fach.spec.ts` und `TextbookForm.test.tsx` bleiben grün.

**Testweg:** `TextbookForm.test.tsx` (neuer Fall, im Fenster), `e2e/lehrbuch-units-laden-noch.spec.ts`
(verzögerter `…/units`-Abruf, Fenster-Zusicherungen mit kurzer Frist), Gegenprobe
`kind-lehrbuch-fach.spec.ts`.

## Verlauf

- **2026-10-23** — **Nachschau** (Sprint 2 desselben Laufs, frischer `frontend-reviewer`, gegengelesen): kein
  Defekt; Prüfpunkte im Feld `nachgeschaut`. **Zwei Berichtigungen:** (1) Entscheidung 1 hielt **kein Test** –
  der Mutant `laedt={units.showPlaceholder}` (B-229s Muster) überlebte alle 42 Fälle und die E2E, weil beide
  Felder beim ersten Laden wahr und nach dem Fehlschlag falsch sind; auseinander laufen sie erst beim zweiten
  Laden derselben Reihe. Die Story-Behauptung „ein Vitest müsste den Zwischen-Render nachspielen" war falsch:
  drei kontrollierte Promises (9 kommt an, 10 kommt an, 9 bleibt offen) reichen. Vierter Fall in
  `TextbookForm.test.tsx` ergänzt – **unter dem Mutanten 1 rot von 10** (erwartet Option „Unit 7 (lädt…)",
  gemessen keine), unter `loading` 10/10. (2) Die Kosten-Zeile „für einen Frame (nicht geladen)" in
  Entscheidung 1 ist zu pessimistisch: React ≥ 18 flusht Passiv-Effekte aus einem diskreten Event (`change`)
  synchron vor dem Paint, der Zwischen-Commit wird nicht gemalt. Dazu der Docblock in `ReferenceSelect.tsx`:
  „lädt auf Seitenebene, **bevor** das Formular aufgeht" war an keiner der fünf Stellen ein Mechanismus
  (keine Seite sperrt „Bearbeiten" bis zur Optionsliste) – umformuliert zu „gleichzeitig mit der Seite, in der
  Praxis vorher". Der Rest der sieben Stellen hängt an keinem Schlüssel (`VaterKind.tsx:35`,
  `ChildMaterialSection.tsx:39`, `VaterFachlehrer.tsx:41/42`, `VaterLehrwerke.tsx:132/133`,
  `VaterPlanDetail.tsx:120`) – kein zweites B-235.
- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 1). `frontend-reviewer`: keine Blocker; Entscheidung 1
  (`loading` statt `showPlaceholder`) mit sechs Zuständen durchgespielt, kein unbenannter Fehlermodus; **ein
  Fund mit Testwirkung (Zähler 2 des Sprints):** die Zeile „kein Ausfall-Banner im Fenster" fragte nach
  `getByRole("alert")` wie das Plan-Vorbild – der Units-Banner trägt keine Rolle, die Abfrage zählte immer 0;
  jetzt per Text (`/^Units: /`). Die fehlende Rolle ist B-134s Sweep, nicht diese Story. Volle Playwright-Suite
  **79/79** (4,0 min) vor dieser Korrektur, danach die drei betroffenen Specs **3/3**; Vitest **394/394**.
  Rollengang siehe unten (E2E als Ersatz, Browser nicht verbunden). Commit `7844894` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 1").
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 1): `laedt={units.loading}` in `ChildMaterialSection.tsx`,
  Doc-Kommentar am `laedt`-Prop in `ReferenceSelect.tsx` erweitert (Entscheidung 3). **Rote Proben
  (erwartet/gemessen):** Vitest `TextbookForm.test.tsx` **1 rot von 30** – Fall „(lädt…)": Option
  „Unit 7 (lädt…)" erwartet, keine gefunden (im DOM stand „Unit 7 (nicht geladen)"); der Fehlschlag-Fall war
  vor dem Fix grün und ist so beschriftet. E2E `lehrbuch-units-laden-noch.spec.ts` rot an Zeile 59: Muster
  `/\(lädt…\)$/` erwartet, gemessen „Unit 1 – Greetings (nicht geladen)". Nach dem Fix: Vitest 30/30 in der
  Datei, **394/394** gesamt; Playwright **5/5** (beide neuen Specs, `kind-lehrbuch-fach`, `lehrwerke`).
  Der bestehende Fall „zeigt die geladene Reihe und die geladene Unit, wenn ihre Listen fehlen" liest den
  Ausfall jetzt per `findByRole` – der erste Render sagt seit dem Fix „(lädt…)". Eine erste Fassung des
  neuen Falls fragte seitenweit nach „(nicht geladen)" und traf das Reihen-Feld (zu Recht „Access (nicht
  geladen)", Liste als leerer Prop) – auf `within(unit())` eingeengt.
  **Rollengang:** Chrome-Extension nicht verbunden (Freigabe 6) → die E2E-Spec ist der Rollengang aus der
  Supervisor-Rolle (eigenes Kind, Buch mit Reihe und Unit aus dem Seed, `…/units` um 2 s verzögert, im
  Fenster gemessen: „(lädt…)", gesperrt, kein Banner; danach „Unit 1 – Greetings", bedienbar).
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1). Entscheidung 1 weicht von der eigenen
  Empfehlung ab, am Code belegt (`useAsync.ts:40-41`).
- **2026-10-18** — angelegt aus der **Nachschau** zu Sprint 3 des Nachtlaufs (Commit `5a91d27`),
  `entgangen_bei: B-229`. Direkt `ausformuliert`: Ist-Stand mit `Datei:Zeile` belegt. **Der Fund beendet den
  Lauf nach Sprint 4** (Limit 2 des Backlog-Laufs: Defekt im Increment eines Sprints derselben Nacht) – dieselbe
  Lage wie B-227 am Ende des ersten Laufs.
