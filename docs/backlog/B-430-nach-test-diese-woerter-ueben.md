---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/training, rolle/student]
aliases: [Nach dem Test üben, Diese Wörter üben]
status: geschaetzt
prio: P3
art: Wunsch
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-sohn
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-430 · Nach einem nicht bestandenen Test gibt es nur „Nochmal versuchen" – keinen Weg zum Üben

Im Rollengang vom 2026-11-08 fiel der Sohn mit 20 % durch; der Ergebnisbildschirm listete die vier falschen
Wörter und bot „Nochmal versuchen" und „Zur Basis". Der naheliegende Schritt – genau diese Wörter üben – fehlt;
der Weg führt über die Basis und den Üben-Knopf, und dazwischen ist die Liste der Fehler weg.

## User Story

Als *Sohn* möchte ich nach einem verpatzten Test direkt **„Diese Wörter üben"** drücken, damit ich die Fehler
übe, solange ich sie noch vor Augen habe, statt blind einen zweiten Versuch zu starten.

## Ist-Stand am Code

- `SohnTest.tsx:389-391`: unter dem Ergebnis stehen nur `tryAgain` (wenn `attemptsRemaining > 0`) und
  `progress.toHome`.
- Die Übungsrunde einer Position legt mit `OrderStrategy` „Schwächste zuerst" (Standard, Rollengang: Plan-Detail
  „Schwächste zuerst") die eben falschen Items nach vorn – ein Link auf `/sohn/practice/:positionId` genügt, es
  braucht keine Übergabe der Fehlerliste.
- Der Test schreibt je Antwort den Lernstand (`PositionTestsController.Submit` → `ItemProgressService`,
  `backend/Pugling.Api/CLAUDE.md`), also kennt die nächste Übungsrunde die Fehler.

## Die echte Lücke

Ein Knopf.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Knopf „▶ Diese Wörter üben" bei nicht bestandenem Test**, vor „Nochmal versuchen"; Ziel
   `/sohn/practice/:positionId` (die Runde ordnet selbst „Schwächste zuerst"). Kosten: ein Knopf, i18n de/en.
2. **Keine Übergabe der Fehlerliste**, kein neuer Endpunkt: der Server kennt den Lernstand schon. Kosten: wenn die
   Position „Zufällig" oder „Reihenfolge" sortiert, kommen die Fehler nicht zuerst – akzeptiert; der Knopf heißt
   dann weiter „üben", nicht „genau diese".

## Akzeptanzkriterien

1. Ergebnis nicht bestanden → Knopf „Diese Wörter üben" sichtbar, führt zu `/sohn/practice/:positionId`.
2. Ergebnis bestanden → Knopf fehlt.
3. Vitest `SohnTest.test.tsx`/`TestResult.test.tsx`: beide Fälle; Mutante „Bedingung umdrehen" → rot.

## Schätzung

**Größe: XS** — ein Knopf mit Link, zwei i18n-Schlüssel, zwei Vitest-Fälle. **Testweg:** `TestResult.test.tsx`;
Rollengang Sohn als Fall in `full-flow.spec.ts` oder `erstbegegnung.spec.ts` (B-419).

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt.
