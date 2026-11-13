---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Stilles Weiterrücken bei Netzfehler]
status: geschaetzt
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Review zu B-526, Etappe 2 (frontend-reviewer, 2026-11-13) – bestand schon vor B-526"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-532 · Ein Netzfehler beim Bewerten lässt eine getippte Karte still weiterrücken

Scheitert im Üben der Aufruf, der eine Antwort bewertet (Netz weg, Serverfehler außer „Plan nicht mehr dran“),
schluckt `judge()` in `SohnPractice.tsx` den Fehler und rückt zur nächsten Karte. Das Kind hat getippt, bekommt
kein Blatt, keine Punkte und keinen Hinweis; die Karte zählt nicht in die Genauigkeit und ist für den Server nie
beantwortet worden. Der Kommentar dort nennt die Bewertung „idempotent genug“ – das stimmt für den Server, aber nicht
für das, was das Kind sieht. Seit B-526 fällt es stärker auf, weil sonst jede getippte Antwort ein Blatt bekommt.
Nicht in B-526 mitgenommen: es bestand vorher, und die Abhilfe ist eine eigene Entscheidung.

## User Story

Als **Sohn** möchte ich merken, wenn meine Antwort nicht angekommen ist, und sie noch einmal schicken können, **damit**
eine Antwort, die ich gewusst habe, nicht still verloren geht – und ich der Runde vertraue, dass zählt, was ich tue.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- `frontend/src/sohn/SohnPractice.tsx:201-262` (`judge`): der innere `catch` (`:253-259`) behandelt nur
  `ApiError` mit Code `plan_inactive` (Runde endet, `:255-257`). Jeder andere Fehler wird geschluckt; danach ruft
  `:261` `next()`, weil `ran` wahr, `planStopped` und `halted` falsch sind. Der Kommentar `:203-207` begründet das mit
  „idempotent genug“.
- Das gilt für alle drei Kartenarten: getippt/Auswahl (Blatt bliebe aus, `setGeprueft` zählt nicht), Selbsteinschätzung
  und Kennenlernkarte (`:459`).
- Das Weiterrücken ist **gewollt und getestet**: `frontend/src/sohn/SohnPractice.test.tsx:84-97` („rückt auch nach einer
  gescheiterten Bewertung weiter“, `Error("offline")`) und `:101-117` (B-333: `ApiError(403, …, "forbidden")`). Herkunft:
  B-330, Entscheidung 5 (autonom im Nachtlauf) – eine Runde soll bei einem anderen Fehler als `plan_inactive` **nicht
  enden**. Ob die Antwort dabei verloren geht, war dort nicht Gegenstand.
- Server-Seite: `PositionPracticeController.Review` (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:332-515`)
  vergibt Punkte höchstens einmal je Karte und Tag (`alreadyScoredToday`, `:397`) und rückt den Server-Cursor bei jeder
  Bewertung um eins (`:495-498`). Ein zweites Senden derselben Antwort zahlt also nicht doppelt.

## Die echte Lücke

„Die Runde läuft weiter“ (B-330/5) und „die Antwort geht verloren“ sind zwei Dinge; heute bekommt das Kind das zweite
gratis zum ersten. Es fehlt ein Zustand „nicht angekommen“, aus dem das Kind selbst entscheidet: noch einmal senden oder
weiter.

## Offene Punkte

1. ~~Halten mit „Noch einmal senden“ oder still weiter wie bisher?~~ → Entscheidung 1.
2. ~~Nur getippte Karten oder jede Bewertung?~~ → Entscheidung 2.
3. ~~Kollidiert das mit B-330, Entscheidung 5, und den Fällen aus B-333?~~ → Entscheidung 3.
4. ~~Automatisch wiederholen (Retry im Hintergrund)?~~ → Entscheidung 4.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Scheitert die Bewertung (außer `plan_inactive`), hält die Karte an** und zeigt in der Antwortzone eine Meldung
   (`role="alert"`, Text über `errorMessage`) mit zwei Knöpfen: **„Noch einmal senden“** (dieselbe Nutzlast) und
   **„Überspringen“** (das heutige `next()`). Eine getippte Antwort bleibt im Feld stehen. *Begründung:* das Kind muss
   wahrnehmen, dass nichts gezählt hat – sonst wirkt die Runde unfair, gerade seit jede getippte Antwort ein Blatt
   bekommt (B-526). *Kosten:* ein zusätzlicher Zustand im Bildschirm, zwei neue Texte (de/en).
2. **Gilt für jede Bewertung**, auch Selbsteinschätzung und Kennenlernkarte. *Begründung:* eine Regel; auch ein
   „Gewusst!“ bewegt den Kasten, und ein verlorenes Kennenlernen lässt das Wort neu erscheinen. *Kosten:* bei der
   Kennenlernkarte ist der Halt etwas mehr Reibung als nötig.
3. **B-330/5 bleibt gewahrt:** die Runde endet nicht, „Überspringen“ ist das bisherige Weiterrücken. Die beiden
   Testfälle `SohnPractice.test.tsx:84` und `:101` werden umgeschrieben (Meldung steht, nach „Überspringen“ kommt
   „zwei“) – sie behaupten heute das stille Weiterrücken, das diese Story abschafft. B-330/5 war eine autonome
   Entscheidung, keine des Nutzers. *Kosten:* zwei Testfälle ändern ihre Aussage.
4. **Kein automatisches Wiederholen.** Ein Netz, das weg ist, kommt nicht in Millisekunden wieder; ein Retry im
   Hintergrund verschiebt nur die Meldung. *Kosten:* ein Klick mehr bei einem kurzen Wackler.

## Akzeptanzkriterien

1. Scheitert die Bewertung einer getippten Karte mit `Error("offline")`, steht eine Meldung mit „Noch einmal senden“
   und „Überspringen“ da, die Antwort steht noch im Feld, die nächste Karte ist **nicht** zu sehen. *Rot, wenn* `next()`
   wieder nach jedem Fehlschlag läuft.
2. „Noch einmal senden“ ruft die Bewertung mit derselben `itemIndex`/`givenAnswer` erneut; gelingt sie, folgt das Blatt
   wie sonst. *Rot, wenn* der Knopf eine leere oder andere Nutzlast schickt (Assertion auf den zweiten Aufruf).
3. „Überspringen“ zeigt die nächste Karte („zwei“). *Rot, wenn* der Knopf fehlt oder nicht weiterschaltet.
4. Ein `ApiError(403, …, "forbidden")` verhält sich wie AK 1 (nicht wie `plan_inactive`); `plan_inactive` beendet die
   Runde wie bisher (`SohnPractice.test.tsx:119`). *Rot, wenn* der Fang auf `instanceof ApiError` oder `status === 403`
   verkürzt wird (die B-333-Mutanten).
5. Ein doppeltes Enter während des erneuten Sendens schickt nur einen Aufruf (bestehende `useAction`-Sperre, B-284).

## Schätzung

**Größe: S** – ein neuer Zustand in `SohnPractice.tsx` (gescheiterte Nutzlast merken, Meldung mit zwei Knöpfen),
zwei i18n-Schlüssel in `frontend/src/i18n/de.ts`/`en.ts`, vier bis fünf Fälle in `SohnPractice.test.tsx`.
`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.

**Risiken:**
- Kam die erste Bewertung beim Server an und nur die Antwort ging verloren, zählt das erneute Senden ein zweites Mal:
  kein doppelter Punkt (`alreadyScoredToday`), aber ein zweiter `ReviewEvent`/Item-Verlaufseintrag und ein Server-Cursor,
  der eine Karte weiter steht. Der Cursor zählt nur für `/next` (Fortsetzen); hingenommen.
- Der Fokus muss auf die Meldung bzw. „Noch einmal senden“ gehen – sonst entsteht die Lücke aus B-531 neu. Beim Bau
  mit B-531 abstimmen (gleicher Effekt).

**Angriffsplan:** (1) AK-Fälle in `SohnPractice.test.tsx` schreiben, die zwei alten Fälle umschreiben, rot sehen;
(2) Zustand `failed: { card, payload, error } | null` in `SohnPractice.tsx`, `catch` setzt ihn statt still weiterzulaufen;
(3) Meldung in der Antwortzone mit den zwei Knöpfen, Texte de/en; (4) rote Proben je AK; (5) `frontend-reviewer`.

**Testweg:** Vitest `frontend/src/sohn/SohnPractice.test.tsx` (AK 1–5). Kein E2E: einen Netzausfall mitten in der
Runde stellt die E2E-Umgebung nicht zuverlässig her; der Rollengang prüft die Meldung von Hand (DevTools offline).

## Verlauf

- 2026-11-13 · `idee` – Fund beim Review von B-526; Prio P2 ist ein Vorschlag.
- 2026-11-13 · `ausformuliert` – am Code belegt; das Weiterrücken ist durch B-330/5 und B-333 gewollt und getestet.
- 2026-11-13 · `gegrillt` – Entscheidungen 1–4; Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13).
- 2026-11-13 · `geschaetzt` – S, frontend, ohne Migration und Vertragsbruch.
