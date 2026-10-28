---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Reload nach Plan-Löschen zeigt Study plan not found]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-326 · Üben und Klausur nehmen nach einem Reload die ungeprüfte Plan-Id

`frontend/src/sohn/SohnTest.tsx:37/70` und `SohnPractice.tsx:49` lesen weiter `useSohn().planId`, das
[B-254](B-254-weg-zeigt-englischen-planfehler.md) nur für Startseite und Weg über `useChosenPlan` prüft. Wird nach dem
Löschen oder Deaktivieren des Plans `/sohn/test/<id>` bzw. `/sohn/practice/<id>` neu geladen (Reload, Zurück,
Lesezeichen), steht wieder „Study plan not found. (Ref: …)“ da – auch in der deutschen Arcade englisch (404 ohne deutsche
Fassung). Normal führt der Weg über die Startseite, die die Id korrigiert. Außerhalb der AKs von B-254, darum
`entgangen_bei` leer.

## User Story

Als Kind möchte ich nach einem Neuladen nicht vor einer englischen Serverzeile stehen, wenn mein Plan inzwischen weg
ist – sondern auf der Basis, die mir zeigt, was ich jetzt spielen kann.

## Ist-Stand am Code

`SohnPractice.tsx` (`useOncePerKey`-Fehlerzweig) und `SohnTest.tsx` (`start`) zeigen jeden Start-Fehler als Meldung.
Der Server antwortet für einen fremden oder gelöschten Plan `404` mit `code: not_found` und „Study plan not found.“
(`PlanOwnershipFilter.cs:24`); der Code ist generisch und hat keinen Satz in `useErrorMessage`. **Gemessen**: neue Fälle
gegen den Ausgangsstand **2 failed** (je Bildschirm die Serverzeile statt der Basis).

## Akzeptanzkriterien

1. `404` beim Start von Üben oder Klausur führt zur Basis (`replace`), ohne Meldung.
2. Jeder andere Start-Fehler bleibt sichtbar wie bisher (B-320).

## Entscheidungen

1. Weiterleiten statt eines eigenen Satzes: die Basis korrigiert die Wahl ohnehin (`useChosenPlan`), ein Satz „gibt es
   nicht mehr“ bräuchte noch einen Knopf dorthin. Kosten: das Kind erfährt nicht, *warum* es auf der Basis steht.
2. Nur beim **Start**: ein 404 mitten im Versuch (`resume`, Antworten) bleibt eine Meldung – dort hat das Kind gerade
   gespielt, ein stiller Sprung wäre verwirrender. Kosten: dieser seltene Fall bleibt englisch.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
- **2026-10-28** — in Lauf I, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) unter Freigabe 1 ausformuliert, gegrillt, geschätzt (XS) und gebaut. Rote Probe: **2 failed** (Serverzeile statt Basis), danach grün. Mutante „jeder Fehler zur Basis“ → je Bildschirm **1 failed**.
- **2026-10-28** — Review (`frontend-reviewer`, Mutanten in einer Kopie gemessen): keine Schleife (die Basis navigiert nie
  von selbst, `useChosenPlan` schreibt die korrigierte Id über `setPlanId` nach `localStorage`); deaktivierter Plan ist
  `plan_inactive`, nicht 404, und bleibt eine Meldung. Beide Fälle je Bildschirm mit Mutante rot. 🟢 ohne Zählung,
  zurückgestellt: `replace` trägt keinen Fall (Mutante `replace: false` grün – der Verlauf ist in `MemoryRouter` nur
  über einen eigenen Beobachter lesbar); eine Plan-Id aus einer fremden Familie oder älteren DB bekommt **403**
  (`PlanOwnershipFilter`), nicht 404, und zeigt auf Üben/Klausur weiter die Serverzeile – außerhalb von AK 1, die Basis
  korrigiert sie ebenso.
- **2026-10-28** — Vitest **569/569**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`, E2E nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
