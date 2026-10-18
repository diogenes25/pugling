---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [PlanEditForm Ladefenster, Sonderoption während des Ladens]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-194]
wartet_auf: ""
---

# B-229 · Das Kind-Feld im Plan-Formular sagt „(nicht geladen)“, solange die Liste erst lädt

`PlanEditForm` (`frontend/src/vater/VaterPlanDetail.tsx`) montiert beim Klick auf „Bearbeiten" und lädt
die Kinderliste selbst. Solange `children.data === null`, ist die Optionsliste leer, und die Sonderoption
„<Name> (nicht geladen)" aus B-194 ist die einzige Option – für die Dauer des Abrufs auch im
**Erfolgsfall**. Wörtlich wahr, liest sich aber als Ausfall. Bei den sieben anderen `ReferenceSelect`-Stellen
laden die Listen auf Seitenebene, bevor das Formular aufgeht.

## User Story

Als **Vater** möchte ich beim Öffnen des Plan-Formulars sehen, dass die Kinderliste noch lädt – und „nicht
geladen" nur dann lesen, wenn sie wirklich nicht gekommen ist –, damit ich nicht nach einem Ausfall suche, den
es nicht gibt.

## Ist-Stand am Code

Nachgesehen am 2026-10-17.

- `frontend/src/vater/VaterPlanDetail.tsx:120`: `const children = useAsync<ChildResponse[]>(() =>
  api.children(), [])` **im Formular**, das erst beim Klick auf „Bearbeiten" montiert (`:71`).
- `:151-155`: `<ReferenceSelect … loadedId={plan.childId} loadedName={childName} options={(children.data ??
  []).map(…)} />` – während der ersten Ladung ist `options` leer, also greift `specialOption`
  (`ReferenceSelect.tsx:66-72`) und beschriftet „<Name> (nicht geladen)". Kein `disabled`, kein Ladehinweis.
- `ReferenceSelect` kennt den Ladezustand der Liste nicht; seine Props (`:22-50`) tragen Wert, geladenen
  Stand, Optionen, Leer-Option, Freitext-Sentinel, `disabled`.
- `useAsync.showPlaceholder` (`useAsync.ts:9-26`) ist genau „erster Versuch noch nicht abgeschlossen" – die
  Bedingung für eine Fläche mit Formular (B-184).
- E2E `plan-kinderliste-faellt-aus.spec.ts` prüft den **Fehlschlag** („(nicht geladen)" + Banner); kein Test
  prüft das Ladefenster im Erfolgsfall.

## Die echte Lücke

„Die Liste kennt den Wert nicht" hat zwei Ursachen – noch nicht da, oder nicht gekommen – und das Bauteil
beschriftet beide gleich. Die zweite Ursache ist die, die B-194 benennen wollte; die erste liest sich wie
sie, ist aber der Normalfall jedes Öffnens.

## Offene Punkte

1. ~~Feld sperren, solange die erste Ladung läuft, oder die Liste auf Seitenebene heben?~~ → Entscheidungen 1–2.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **`ReferenceSelect` bekommt `laedt?: boolean`:** solange wahr und der geladene Wert nicht in der Liste
   steht, heißt die Sonderoption „<Name> (lädt…)" und das Feld ist gesperrt. *Begründung:* Die Beschriftung
   soll die Ursache nennen, und die Regel gehört ans Bauteil, damit alle acht Stellen sie bekommen können – nicht
   nur die, an der es heute auffällt (dasselbe Argument wie B-172, Entscheidung 2). Gesperrt, weil eine Wahl aus
   einer Liste, die noch nicht da ist, keine Wahl ist. *Kosten:* eine Prop, ein Vitest-Fall; die sieben anderen
   Stellen reichen sie nicht – ihre Listen laden auf Seitenebene vor dem Formular, das Fenster ist dort praktisch
   zu. Wer es dort schließen will, hat jetzt den Weg.
2. **Die Kinderliste bleibt im `PlanEditForm`**, sie wird nicht auf die Seite gehoben. *Begründung:* Das
   Formular ist der einzige Verbraucher; eine Seitenladung kostete jeden Plan-Aufruf einen Abruf, den nur der
   Bearbeiten-Fall braucht, und `plan-kinderliste-faellt-aus.spec.ts` hängt an der Reihenfolge „Seite steht,
   dann fällt die Liste aus". *Kosten:* das Fenster bleibt und wird benannt statt versteckt.
3. **Gespeist aus `children.showPlaceholder`, nicht aus `loading`.** *Begründung:* Nach einem Fehlschlag ist
   `showPlaceholder` falsch → „(nicht geladen)" + Banner, B-194 unverändert; `loading` allein zöge „erste
   Ladung" und „Neuladen" zusammen (B-184, die Platzhalter-Regel für Flächen mit Formular). *Kosten:* keine.

## Akzeptanzkriterien

1. Während der ersten Ladung zeigt das Kind-Feld genau eine Option „<Name> (lädt…)" und ist gesperrt.
   *Beleg:* Vitest `ReferenceSelect.test.tsx` (`laedt` ohne passende Option → Label und `disabled`); E2E
   `plan-kinderliste-laedt-noch.spec.ts` mit um 2 s verzögerter Liste, gemessen im Fenster (Frist < Verzögerung).
2. Nach erfolgreicher Ladung steht das Kind als reguläre Option „<Name> (#id)", keine Sonderoption, Feld
   bedienbar. *Beleg:* dieselbe E2E nach der Antwort.
3. Nach einem Fehlschlag unverändert „(nicht geladen)" mit Banner. *Beleg:* `plan-kinderliste-faellt-aus.spec.ts`
   bleibt grün.
4. Mit `laedt` und **passender** Option bleibt das Feld unverändert (kein Sonderlabel) – ein `reload` mit
   stehender Liste darf nichts umbeschriften. *Beleg:* Vitest.

## Schätzung

**Größe: S** — eine Prop am Bauteil (~6 Zeilen), eine Zeile im Formular, zwei Vitest-Fälle, eine E2E. Anker:
zwischen B-02 (XS) und B-01 (S); die E2E macht den Unterschied.

**Risiken:** Die E2E muss im Fenster messen (Frist < Verzögerung), sonst sieht sie nur den Zustand danach.
Der Name des Kindes kommt aus `…/children/1` (lädt weiter) – nur die **Liste** (`…/supervisor/children`
ohne Pfad dahinter) wird verzögert, wie in der B-194-Spec.

**Angriffsplan:** (1) Vitest rot, (2) `ReferenceSelect.tsx`, (3) `VaterPlanDetail.tsx`, (4) E2E, (5) Suite.

**Testweg:** `vitest run src/components/ReferenceSelect.test.tsx` (erwartet rot 1/1: Label „(nicht geladen)"
statt „(lädt…)"), `playwright test e2e/plan-kinderliste-laedt-noch.spec.ts` (erwartet rot 1/1 an der
„lädt"-Zeile), danach die ganze Suite.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 1 des Nachtlaufs (Commit `d834a8e`),
  `entgangen_bei: B-194`.
- **2026-10-17** — **idee → ausformuliert** im zweiten Lauf der Nacht: Ist-Stand belegt, `unverifiziert: false`.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1: drei Entscheidungen; `laedt` am
  Bauteil, Liste bleibt im Formular, gespeist aus `showPlaceholder`.
- **2026-10-17** — **gegrillt → geschaetzt**: S, frontend, keine Migration, kein Vertragsbruch.
- **2026-10-18** — **geschaetzt → abgenommen** im Nachtlauf (zweiter Lauf der Nacht 2026-10-17, Sprint 3).
  **Rote Proben (erwartet/gemessen):** `ReferenceSelect.test.tsx:124` 1/1 („Englisch (nicht geladen)" statt
  „Englisch (lädt…)"), E2E `plan-kinderliste-laedt-noch.spec.ts:39` 1/1 („Sohn (nicht geladen)" statt
  `/\(lädt…\)$/` binnen 1,2 s). Belege: `tsc -b` sauber · `vitest run` 389/389 · `plan-kinderliste-faellt-aus`
  weiter grün (AK 3) · `frontend-reviewer`: kein Fund am Bauteil (sieben andere Aufrufstellen unverändert,
  Freitext und „(nicht geladen)" sperren das Feld nicht); ein Nit an der E2E (Banner-Zusicherung mit
  Standardfrist trug nichts → Frist 1,2 s, Zähler-relevant) · Skill `web-design-guidelines` (Freigabe 7): kein
  Fund – Label vorhanden, „(lädt…)" endet auf „…". **Browser-Rollengang in Chrome (Freigabe 6) als Vater im
  Plan-Detail:** Kinderliste per Fetch-Wrapper um 2,5 s verzögert, „Bearbeiten" → im Fenster gemessen: genau
  eine Option „Sohn (lädt…)", Feld gesperrt, kein Banner; danach „Sohn (#1)", Feld bedienbar. Commit: siehe
  Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`, „Zweiter Lauf".
