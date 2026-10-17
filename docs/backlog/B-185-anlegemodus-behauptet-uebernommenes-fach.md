---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [DerivedHint widerspricht dem Feld, abgeleitetes Fach ohne Option, ProfileForm anlegen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-172, Fund des frontend-reviewer
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-185 · Beim Anlegen behauptet der Hinweis ein übernommenes Fach, das das Feld nicht zeigt

Im **Anlege**-Modus von `ProfileForm` (`frontend/src/vater/VaterFachlehrer.tsx`) setzt `deriveFromSeries`
über `applySeriesChange` das Fach aus der gewählten Reihe, und `DerivedHint` meldet „aus dem Lehrwerk
übernommen". Scheitert dabei `GET creator/subjects`, während die Reihenliste geladen hat, gibt es zu diesem
Wert keine Option: der `<select>` fällt auf „– fachneutral –", der Hinweis daneben behauptet das Gegenteil,
und der POST trägt die `subjectId` trotzdem mit.

## User Story

Als **Creator** möchte ich beim Anlegen eines Fachlehrers im Fach-Feld genau das Fach sehen, das die
gewählte Reihe beisteuert — auch wenn die Fächerliste gerade nicht geladen werden konnte —, damit Hinweis,
Feld und das, was gespeichert wird, dasselbe sagen.

## Ist-Stand am Code

Nachgesehen am 2026-10-17.

- **Ableitung:** `VaterFachlehrer.tsx:241-249` (`deriveFromSeries`) → `seriesDerivation.ts:79-100`
  (`applySeriesChange`), Fall 4: „sonst → nur füllen, wenn die neue Reihe etwas sagt". Die Regel kennt die
  Optionsliste nicht und soll sie nicht kennen (reine Regel, B-126).
- **Feld:** `VaterFachlehrer.tsx:336-343` — `ReferenceSelect` mit `loadedId={profile?.subjectId}`
  `loadedName={profile?.subjectName}`; beim Anlegen ist `profile` `undefined`, also gibt es keine
  Sonderoption (der Kommentar Z. 333-335 begründet das: „ein neues Profil hat keinen Wert, der fehlen
  könnte" — das stimmt für den *geladenen*, nicht für den *abgeleiteten* Wert).
- **Hinweis:** `DerivedHint active={isDerived("subjectId")}` (Z. 343), `isDerived`
  (`seriesDerivation.ts:53-61`) vergleicht Formularwert mit dem Reihenwert — ist wahr, sobald die Ableitung
  griff. Er sagt also die Wahrheit über den **Zustand**, während das Feld sie nicht zeigt.
- **Seite:** `VaterFachlehrer.tsx:68` rendert `subjects.error` als Banner „Fächer: …" — der Ausfall ist auf
  der Seite sichtbar, nur das Feld widerspricht ihm.
- **POST:** `:276-280` — `subjectId: Number(form.subjectId)`, `subjectName: subject?.name ?? null`; ohne
  Fächerliste ist `subject` `undefined`, der Name geht als `null` mit. Seit B-142 leitet der Server den
  Namen beim Schreiben aus der Id ab — kein Datenschaden, nur die Lüge.
- **Reihe trägt den Namen:** `TextbookSeriesResponse.subjectId`/`subjectName` (`contract.ts:26119-26121`)
  — die Beschriftung der Sonderoption ist ohne Vertragsänderung da.
- **Sprachfelder:** `VaterFachlehrer.tsx:382-397` sind `<input>`, keine `<select>` — kein Optionsdrift
  möglich. Die zweite Frage aus der Idee ist damit am Code beantwortet: nein.
- **Bearbeiten-Modus:** Fall 3 von `applySeriesChange` schützt nur den *geladenen* Wert. Ein geladenes
  fachneutrales Profil, dessen Reihe gewechselt wird, läuft in Fall 4 — dieselbe Lüge wie beim Anlegen.
  Die Idee hatte „dort passiert nichts" behauptet; das gilt nur für Fall 3.

## Die echte Lücke

`ReferenceSelect` kennt nur *einen* Wert, der außerhalb der Liste liegen kann (den geladenen). Beim Anlegen
— und beim Bearbeiten eines fachneutralen Profils — liefert die Reihe einen zweiten: den abgeleiteten. Er
hat eine Id und einen Namen, und er fehlt dem Feld.

## Offene Punkte

1. ~~Soll der Hinweis schweigen, wenn zum abgeleiteten Wert keine Option existiert, oder die Ableitung gar
   nicht greifen?~~ → Entscheidung 1 (weder noch).
2. ~~Trifft derselbe Fall die Sprachfelder?~~ → nein, `<input>` (Ist-Stand).

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **Die Sonderoption folgt dem Zustand: Trägt das Formular den abgeleiteten Wert und kennt die Liste ihn
   nicht, speist die gewählte Reihe `loadedId`/`loadedName`; sonst wie bisher das Profil.** *Begründung:*
   Die beiden verworfenen Wege reparieren je die falsche Hälfte. Den Hinweis schweigen zu lassen ließe das
   Feld „fachneutral" zeigen, während der POST `subjectId` trägt — die Lüge bliebe, nur ohne Zeugen. Die
   Ableitung nicht greifen zu lassen koppelte eine reine Regel an einen Ladezustand und nähme dem Creator
   das Fach, das sein Lehrwerk nun einmal hat. Die dritte Form zeigt, was ist: „Englisch (nicht geladen)",
   wählbar, mit dem wahren Hinweis daneben. *Kosten:* Die Props heißen weiter `loaded*`, bedeuten an dieser
   Aufrufstelle aber „bekannt, aber nicht in der Liste" — ein Kommentar trägt das; das Bauteil ändert sich
   nicht. Nur wenn **beide** fehlen (geladenes Profil-Fach *und* abgeleitetes Fach nicht in der Liste),
   gewinnt das abgeleitete, und der Weg zurück zum Profil-Fach führt über das Zuklappen — ein Rand des
   Randes, hier benannt statt gelöst.
2. **Der Hinweis bleibt, wie er ist.** Er war nie falsch über den Zustand, nur das Feld widersprach ihm.
   *Kosten:* keine.

## Akzeptanzkriterien

1. Anlege-Modus, Fächerliste leer (Abruf gescheitert), Reihe mit Fach gewählt: das Fach-Feld zeigt
   „<Fach> (nicht geladen)" als gewählte Option, der Hinweis „aus dem Lehrwerk übernommen" steht daneben.
   *Beleg:* Vitest in `VaterFachlehrer.test.tsx` („ProfileForm – Ableitung aus dem Lehrwerk": neuer Fall
   mit `subjects=[]`), rot vor dem Fix (Feldwert `""` statt der Id). E2E `fachlehrer-faecher-fallen-aus.spec.ts`.
2. Das abgeschickte Profil trägt die `subjectId` der Reihe. *Beleg:* dieselbe E2E prüft den POST-Rumpf.
3. Ist die Fächerliste da, gibt es keine Sonderoption (Verhalten unverändert). *Beleg:* bestehende Fälle
   `VaterFachlehrer.test.tsx:25-46` bleiben grün.
4. Ein geladenes Profil mit Fach, das die Liste nicht kennt, zeigt weiterhin **sein** Fach als Sonderoption
   (B-172-Verhalten). *Beleg:* bestehender Fall `:110` („zeigt das geladene Fach, wenn die Fächerliste es
   nicht kennt") bleibt grün.

## Schätzung

**Größe: XS** — eine berechnete Konstante und zwei Props an einer Aufrufstelle, ein Vitest-Fall, eine
kurze E2E. Anker: B-02.

**Risiken:** Die Bedingung „abgeleitet *und* nicht in der Liste" muss beide Hälften tragen, sonst
verdrängt sie im Bearbeiten-Modus die Profil-Sonderoption (AK 4 hält das).

**Angriffsplan:** (1) Vitest-Fall rot, (2) Aufrufstelle, (3) E2E.

**Testweg:** `vitest run src/vater/VaterFachlehrer.test.tsx`, `playwright test
e2e/fachlehrer-faecher-fallen-aus.spec.ts`, dann die ganze Suite.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu B-172. Der Weg über
  `applySeriesChange` ist vom Reviewer am Code belegt, den Fall selbst habe ich nicht gefahren — darum
  `unverifiziert`.
- **2026-10-17** — **idee → ausformuliert** im Nachtlauf (Sprint 1): Ist-Stand belegt; die Sprachfelder
  sind `<input>` (keine Drift), der Bearbeiten-Modus ist **mit** betroffen (Fall 4), anders als die Idee
  behauptete.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1: zwei Entscheidungen.
- **2026-10-17** — **gegrillt → geschaetzt**: XS, frontend, keine Migration, kein Vertragsbruch.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 1. Rollengang: Creator auf `/vater/fachlehrer` (Fächerliste abgeschossen → Reihe „Green Line 1 (Klett)" gewählt → Fach-Feld zeigt „Englisch (nicht geladen)" gewählt, Wert 1, drei Hinweise „aus dem Lehrwerk übernommen", Seitenbanner „Fächer: …"). Rote Probe: VaterFachlehrer.test 1/1 (`''` statt `'1'`). E2E `fachlehrer-faecher-fallen-aus.spec.ts` grün (POST-`subjectId` = Feldwert). Belege: tsc -b sauber · vitest run 380/380 (47 Dateien) · playwright test 69/70 im vollen Lauf – der eine rote Fall (`lehrwerke.spec.ts`) war eine Nebenwirkung der neuen `fachlehrer-faecher-fallen-aus.spec.ts` in der geteilten DB (aktives Englisch-Profil ohne Grenzen passt zu jedem Kind), behoben (Profil wird stillgelegt angelegt), danach die betroffenen Specs gemeinsam grün · `frontend-reviewer` gelaufen, **kein Blocker**, ein Fund mit Codewirkung (Leer-Satz und Zählzeile des Trefferbereichs hingen noch an `fehler` statt am Gedächtnis – behoben, Vitest ergänzt) · Skill `web-design-guidelines` (Freigabe 7): ein Fund (fokussierter Kasten ohne projektweiten Fokusring – `.banner[tabindex]:focus-visible` ergänzt) · **Browser-Rollengang in Chrome (Freigabe 6)** gegen Backend mit Wegwerf-DB und `npm run dev`, Abrufe über einen Fetch-Wrapper in der Seite abgeschossen bzw. verzögert, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`. Commit `d834a8e`.
