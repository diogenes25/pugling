---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/training, rolle/student]
aliases: [Nach dem Test üben, Diese Wörter üben]
status: ausformuliert
wartet_auf: Entscheidung des Nutzers (Offener Punkt 3)
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
  **Berichtigt 2026-11-08 (Lauf AE, Review):** das stimmt nur für den *plan-übergreifenden* Lernstand
  (`ItemProgress`). Die Übungsrunde liest die *Positions*-Fälligkeit (`PositionItemProgress.DueOn`,
  `PositionPlayService.DueItemIndicesAsync`), und die setzt der Test **nicht** (`PositionTestsController.cs:403` ruft
  `RecordAsync`, kein `ApplyReview`). Ein im Test falsches Wort, das in der Übung richtig war, ist also nicht fällig;
  sind alle Wörter so, führt der Knopf in „Nichts fällig" mit einem Knopf zurück zum Test.

## Die echte Lücke

Ein Knopf.

## Offene Punkte

1. ~~Knopf oder Übergabe der Fehlerliste?~~ → Entscheidungen 1 und 2.
2. ~~Reihenfolge der Knöpfe?~~ → Entscheidung 1.
3. **Neu (2026-11-08, Lauf AE):** Wohin führt der Knopf, wenn die Fehler des Tests in der Position nicht fällig sind?
   Drei Wege, keiner ohne Produktfolge: (a) die Übung aus dem Ergebnis im Info-Modus öffnen (alle Wörter, aber ohne
   Rückmeldung – die Arcade spielt den Info-Modus heute gar nicht); (b) eine falsche Test-Antwort setzt die
   Positions-Karte zurück in Kasten 1, fällig heute (ändert die Leitner-Regel: der Test steuerte dann die Übung);
   (c) der Knopf heißt „Üben" und verspricht nicht „diese Wörter" (ehrlich, führt aber weiter ins Leere). Nicht vom
   Nachtlauf entschieden (Freigabe 8: eine Entscheidung, die die Story nicht hergibt).

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
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-08** — Lauf AE (Sprint 22, [Protokoll](../pm-sitzung-2026-11-08-nacht.md)): gebaut (Knopf in `TestResult`,
  Vitest, E2E-Abschnitt), dann vom `frontend-reviewer` widerlegt (🟡): die Prämisse im Ist-Stand war falsch, der Test
  setzt die Positions-Fälligkeit nicht (Belegstelle oben). Die E2E war nur grün, weil dasselbe Wort schon in der
  Übungsrunde falsch war. Nach Freigabe 8 angehalten: der Code ist **zurückgenommen** (kein Knopf, der ins Leere führen
  kann), Stufe zurück auf `ausformuliert`, Offener Punkt 3 für den Dialog.
