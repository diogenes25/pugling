---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/training, rolle/student]
aliases: [Nach dem Test üben, Diese Wörter üben]
status: abgenommen
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
3. ~~Wohin führt der Knopf …?~~ → Entscheidung 3. **Ursprünglich (2026-11-08, Lauf AE):** Wohin führt der Knopf, wenn die Fehler des Tests in der Position nicht fällig sind?
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

3. **Offener Punkt 3 → (c)** (2026-11-13, stellvertretend für den Nutzer im Tageslauf, Begründung im Verlauf): Knopf
   „▶ Üben" ohne das Versprechen „diese Wörter", ohne Eingriff in die Leitner-Regel; er steht bei jedem nicht
   bestandenen Test, auch ohne Restversuch – dann ist er der einzige Weg nach vorn. (b) bleibt eine eigene Story,
   falls der Vater den Test die Übung steuern lassen will (braucht Markierung + Migration, siehe Verlauf).

## Akzeptanzkriterien

Nach Entscheidung 3 gilt die Beschriftung „▶ Üben" statt „Diese Wörter üben".

1. Ergebnis nicht bestanden → Knopf „▶ Üben" sichtbar (unabhängig von den Restversuchen), führt zu
   `/sohn/practice/:positionId`; kein Versprechen „diese Wörter", kein Leitner-Eingriff.
2. Ergebnis bestanden → Knopf fehlt.
3. Vitest `SohnTest.test.tsx`/`TestResult.test.tsx`: beide Fälle; Mutante „Bedingung umdrehen" → rot.

## Schätzung

**Größe: XS** — ein Knopf mit Link, ein i18n-Schlüssel je Sprache, drei Vitest-Fälle und eine E2E-Spec. **Testweg:** `TestResult.test.tsx`;
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
- **2026-11-13** — Tageslauf (Sprint 56), Nutzerrolle stellvertretend übertragen: Offener Punkt 3 als **(b)** gebaut
  (falsche Test-Antwort → Positions-Karte Kasten 1, fällig am Testtag; nur Kind, nur Karteikasten, nur eingeführte
  Karten) samt Knopf „▶ Diese Wörter üben", Tests und Mutanten 5/5 rot – dann vom `pugling-reviewer` widerlegt und
  **zurückgenommen**, nichts committet:
  - 🔴 **Münzen farmen:** die Herabstufung stempelt kein `LastReviewedAt`; die Übung bezahlt die nun fällige Karte (`8 −
    preBox` = 7 Münzen in Kasten 1, dazu Combo/Tempo). Absichtlich Versuch 1 verpatzen, Wörter üben, Versuch 2 besteht –
    nach der 7-Tage-Leiter täglich über den ganzen Pool. Ein Stempel `LastReviewedAt = jetzt` schließt nur den Tag selbst;
    am Folgetag bezahlt die Karte trotzdem – die Schleife läuft dann über zwei Tage weiter.
  - 🟡 **Testschwierigkeit selbst steuern:** in der Leiter-Woche fragt der Test eine Karte ab Kasten 3 schriftlich; nach
    der Herabstufung fragt Versuch 2 am selben Tag sie als Auswahl.
  - 🟢 bei `Scope = Neu` und bei `ItemCount` unter dem Index führt der Knopf weiter ins Leere; „Wörter" passt nicht für
    Listen und Rechnen.
  - **Was eine Entscheidung braucht (für den Dialog):** (b) trägt nur mit einer Markierung „vom Test herabgestuft, bis zum
    ersten Aufstieg unbezahlt" (neue Spalte → Migration auf der echten DB) **und** einer Regel, die die Teststufe eines
    Tages nicht unter die des ersten Versuchs fallen lässt. Alternativ (c): Knopf „▶ Üben" ohne Versprechen, ohne
    Leitner-Eingriff. Empfehlung des PM: (c) jetzt, (b) mit Markierung als eigene Story, falls der Vater den Test die
    Übung steuern lassen will. Stufe bleibt `ausformuliert`.
- **2026-11-13** — Tageslauf (Sprint 64), **Entscheidung 3 stellvertretend: (c)**. Begründung: (b) trägt nur mit Migration
  auf der echten DB und einer neuen Teststufen-Regel – beides Produktentscheidungen mit Anti-Cheat-Folgen; (c) ist ehrlich,
  ändert keine Ökonomie und schließt die Sackgasse „nur Nochmal versuchen / Zur Basis". Gebaut: `TestResult` nimmt
  `onPractice`, der Knopf steht bei `!result.passed` (vor „Nochmal versuchen"), `SohnTest` führt nach
  `/sohn/practice/:positionId`; i18n de/en.
  - AK 1, nicht bestanden → Knopf, Klick ruft `onPractice` → `TestResult.test.tsx` („bietet nach nicht bestandenem Test"),
    Mutante U1 `onClick={onRetry}` → rot.
  - AK 2, bestanden → kein Knopf → `TestResult.test.tsx` („lässt … weg, wenn der Test bestanden ist"), U2 Bedingung `true` → rot.
  - Entscheidung 3, ohne Restversuch → Knopf steht → `TestResult.test.tsx` („auch …, wenn für heute kein Versuch mehr
    übrig ist"), U3 an `attemptsRemaining > 0` gekoppelt → rot.
  - Verdrahtung → `e2e/test-durchgefallen-ueben.spec.ts` (Selbstcheck „Nicht gewusst", Klick, URL), U4 Route
    `/sohn/test/` → rot an `toHaveURL` (von Hand gemessen; das Probe-Skript setzt keine Umgebungsvariablen vor `cd`).
  - Vitest 862/862, `tsc -b` grün, E2E-Spec 1/1.
  - `frontend-reviewer` (frisch): kein 🔴. 🟡 Leitner-Positionen landen nach dem Test oft in „Nichts fällig 🎉" mit
    „Zum Test" (bei 0 Restversuchen in `test_attempts_exhausted`) – **stellvertretend als bekannte Grenze von (c)
    angenommen**: genau das stand in Offener Punkt 3 („führt aber weiter ins Leere"); der Knopf öffnet wenigstens die
    Runde, statt keinen Weg zu bieten. Die Verbesserung (Knopf nur bei fälliger Arbeit bzw. ehrlicher Leertext nach einer
    Niederlage) liegt als [B-522](B-522-ueben-nach-test-landet-in-nichts-faellig.md). 🟢 behoben: Kommentar-Reihenfolge
    (B-62-Satz stand über dem falschen Knopf), „▶ Practise" britisch wie `home.practice`, AK und Schätzung auf (c).
  - Rollengang Sohn nach der letzten Änderung: `test-durchgefallen-ueben.spec.ts` + `lueckentext-am-handy.spec.ts` 3/3
    (Ausweichports, frischer Server). Vater/Creator Regressionszeugen: kein Pfad von ihnen im Diff, Vitest 862/862.
    **abgenommen** (Entscheidung 3 stellvertretend, Veto beim Nutzer).
