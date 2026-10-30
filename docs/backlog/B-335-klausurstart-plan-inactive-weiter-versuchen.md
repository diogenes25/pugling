---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [SohnTest.start merkt sich plan_inactive nicht]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf J, Teil 2 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-330]
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-335 · Scheitert der erste Schritt nach dem Klausurstart an `plan_inactive`, bietet die Arcade „Weiter versuchen" an

`SohnTest.tsx:83-88`: `start()` ist der einzige Fang, der `planStopped` nicht setzt. Gelingt `startTest` (die
`attemptId` steht), antwortet aber der unmittelbar folgende `nextTest` bzw. `submitTest` mit `403 plan_inactive`, zeigt
die Arcade „Weiter versuchen" – und das holt nur denselben `403`. Das Fenster ist klein (Deaktivierung zwischen zwei
direkt aufeinanderfolgenden Aufrufen, oder die UTC-Tagesgrenze am `EndDate`). Aus Code gelesen (🟢 der Nachschau), nicht
gemessen. Vermutete Behebung: `setPlanStopped(isPlanInactive(e))` auch dort, mit einem Fall, der `nextTest` nach
erfolgreichem Start scheitern lässt.

## User Story

Als Sohn, dessen Plan gerade pausiert wurde, als ich die Klausur öffne, möchte ich keinen „Weiter versuchen"-Knopf sehen,
der nur wieder an dieselbe Wand führt.

## Ist-Stand (Lauf K, am Code belegt)

`SohnTest.tsx` `start()`: der Fang setzt nur `error` (`:83-88`, vor dem Fix); `resume()` und `answerAndAdvance` setzen
zusätzlich `planStopped = isPlanInactive(e)`. Der Knopf steht bei `attemptId !== null && !planStopped`. **Gemessen** (rote
Probe unten): `startTest` gelingt, die erste `nextTest` antwortet `403 plan_inactive` → „Weiter versuchen" steht da.

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. **Derselbe Satz wie in `resume()`**: `setPlanStopped(isPlanInactive(e))` im Fang von `start()`. Ein vorübergehender Fehler
   an derselben Stelle bietet „Weiter versuchen" weiter an – der Versuch läuft ja schon. *Kosten:* keine.

## Akzeptanzkriterien

1. Start gelingt, erste Frage → `plan_inactive`: Meldung, kein „Weiter versuchen".
2. Start gelingt, erste Frage → `500`: „Weiter versuchen" steht und holt die Frage.

## Schätzung

`XS`, Frontend; zwei Fälle in `SohnTest.test.tsx`.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 02ce632`) in Lauf K; 🟢, ohne
  Zählung, zurückgestellt (nicht billig: braucht einen eigenen Fall mit gestaffelter Mock-Antwort).
- **2026-10-29** — Lauf K, Teil 2: ausformuliert, autonom gegrillt, geschätzt, gebaut. Rote Probe **1 failed / 10 passed**
  (erwartet 1 – das Gegenstück aus AK 2 ist vorher grün, es hält den Fix nach unten ab). Mutante „jeder Fehler beim Start
  stoppt" (`setPlanStopped(true)`) **1 failed** (AK-2-Fall). Danach `SohnTest.test.tsx` **11/11**.
- **2026-10-29** — `frontend-reviewer`: keine Funde an B-335; beide Fälle trennen, was sie behaupten (Mutanten benannt).
- **2026-10-29** — Abnahme Lauf K, Teil 2: volle Suite **1017/1017**, Vitest **584/584**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Sohn über seine Arcade-Specs (Übung, Klausur, Buchstaben ordnen) plus die Komponentenfälle mit Knoten-Identität; Creator über `schnellanlage-ohne-dublette`; Vater Regressionszeuge. Was ein echter Screenreader ansagt, prüft keine Maschine – **Gerätecheck offen**: einmal mit NVDA/VoiceOver eine Übungsrunde und eine fortgesetzte Klausurfrage anhören. Reviews: `frontend-reviewer`, Delta-Review (ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 1 (frischer `frontend-reviewer`, `git show 448785c -- frontend/`, HEAD unverändert): **sauber**. Geprüft: der `catch` in `start()` (`SohnTest.tsx:83-90`) setzt `planStopped = isPlanInactive(e)` wie `resume()` (`:119`) und `answerAndAdvance` (`:137`), deckt auch ein scheiterndes `finish()`/`submitTest` in `start()`; Rücksetzen bei Neustart (`:75`); Knopfbedingung (`:161`) gegen beide AK; je Test die tötende Mutante benannt (`setPlanStopped` aus `start()` entfernt bzw. fest `true`). Vitest der vier Dateien **48/48**.
