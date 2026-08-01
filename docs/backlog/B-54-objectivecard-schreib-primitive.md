---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/qualitaet]
aliases: [ObjectiveCard ohne useAction, Etappe ohne Erfolgsmeldung]
status: ausformuliert
prio: P3
art: Aufräumen
quelle: docs/backlog/B-26-e2e-in-ci.md
---

# B-54 · `ObjectiveCard` geht an den Schreib-Primitiven vorbei

Die Ziel-Karte im Vater-Web schreibt an `useAction`/`StatusBanner` vorbei: sie baut ihr eigenes
`try/catch` mit lokalem `err`-State. Folge – **eine geglückte Mutation meldet gar nichts.** Anlegen,
Ändern und Löschen einer Etappe (Key Result) laufen wortlos durch; nur der Fehler wird sichtbar.
Aufgefallen beim Umschreiben des E2E-Abschnitts in [B-26](B-26-e2e-in-ci.md): der alte, tote Abschnitt
konnte noch auf „Lernziel angelegt." prüfen, der neue hat keine Meldung mehr, auf die er prüfen könnte.

## User Story

Als **Vater**, der eine Etappe an einem großen Ziel nachträgt, möchte ich dieselbe Rückmeldung bekommen
wie überall sonst im Vater-Web – damit ich nicht aus dem Erscheinen einer Tabellenzeile schließen muss,
ob mein Klick angekommen ist, und damit ein zweiter Klick nicht zwei Etappen anlegt.

## Ist-Stand am Code

- `VaterZiele.tsx:209-212` – eigenes `act()` mit `setErr(null)` / `try … catch (e) { setErr(errorMessage(e)) }`
  statt `action.run(fn, okText)`. Ausgabe nur über `{err && <div className="banner err">}` (`:249`).
- Betroffen sind darüber drei Schreibpfade: `createKeyResult` (`:266`), `updateKeyResult` (`:237`) und
  `deleteKeyResult` (`:240`).
- Die Ebene darüber macht es richtig: `Objectives` (`:164`) nimmt `useAction` und rendert einen
  `StatusBanner` (`:180`) – Stilllegen und Löschen eines Ziels melden also, das Bearbeiten seiner Etappen
  nicht. Dieselbe Seite, zwei Verhalten.
- Regel dazu: [frontend/CLAUDE.md](../../frontend/CLAUDE.md) bzw.
  [memory/frontend-schreib-primitive](../obsidian.md) – „`useAction` + `StatusBanner` für **jede** Mutation".

## Die echte Lücke

Nicht „ein Banner fehlt", sondern: **kein Rückkanal für Erfolg** an der einzigen Stelle, an der ein Ziel
überhaupt erreichbar gemacht wird. Dazu kommt die Wiedereintritts-Sperre aus
[B-43](B-43-frontend-komponententests.md)/E5, die genau an `useAction` hängt – dieser Pfad bekäme sie
nicht mit. Ein Doppelklick auf „Etappe übernehmen" legt heute zwei Etappen an.

## Offene Punkte

1. **Zusammen mit E5 bauen oder danach?** Die Sperre aus B-43 wirkt je `useAction`-Instanz; solange dieser
   Pfad daran vorbeigeht, ist er von ihr nicht gedeckt. **Empfehlung:** danach, aber unmittelbar – E5
   ändert das Primitiv, diese Story ändert nur den Aufrufer.
2. Gehört das mit [B-49](B-49-sohn-app-schreib-primitive.md) („Die Sohn-App benutzt die geteilten
   Schreib-Primitive nicht") in **eine** Story? **Empfehlung:** nein – andere Fläche, andere Rolle, und
   B-49 ist ungeprüft.

## Akzeptanzkriterien

1. Die drei Schreibpfade der Karte (`createKeyResult`, `updateKeyResult`, `deleteKeyResult`) laufen über
   `useAction`; das eigene `try/catch` mit `err`-State ist weg.
2. Jeder der drei meldet **Erfolg** über einen `StatusBanner` – nicht nur den Fehler.
3. `vater-von-null.spec.ts` prüft nach „Etappe übernehmen" die Erfolgsmeldung, nicht nur das Erscheinen der
   Zeile. (Heute äußert sich ein fehlgeschlagener Aufruf als Timeout auf die Zeile statt als lesbare
   Meldung — genau die Diagnose, die der Abschnitt vor dem Umbau hatte.)
4. Ein Doppelklick auf „Etappe übernehmen" legt **eine** Etappe an — greift, sobald die Sperre aus
   [B-43](B-43-frontend-komponententests.md) im Primitiv sitzt.

## Verlauf

- **2026-08-01** — angelegt aus dem `frontend-reviewer`-Befund zu B-26/E0 (Befund 6), am Code belegt.
