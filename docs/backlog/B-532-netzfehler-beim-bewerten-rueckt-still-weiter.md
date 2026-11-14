---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Stilles Weiterrücken bei Netzfehler]
status: abgenommen
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
nachgeschaut: 2026-11-14
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
- 2026-11-13 · `in-arbeit` – Bau im Backlog-Lauf (Frontend), Worktree `backlog-lauf`.
- 2026-11-13 · `in-arbeit` – gebaut: Zustand `failed: { payload, text } | null` in `SohnPractice.tsx`; der `catch` von
  `judge()` setzt ihn (außer `plan_inactive`) und hält an, statt `next()` zu rufen. Die Antwortzone zeigt dann die Meldung
  (`role="alert"`, Text über `errorMessage`) mit „Noch einmal senden“ (dieselbe Nutzlast, Fokus dorthin) und
  „Überspringen“ (`next()`); eine getippte Antwort bleibt im gesperrten Feld, Enter im Feld schickt währenddessen nichts.
  Neue Schlüssel `practice.resend`/`practice.skip` in `de.ts`/`en.ts`. Die zwei Fälle aus B-330/B-333 sind umgeschrieben
  (Halt statt stillem Weiterrücken), dazu AK 2, 3, 5 und ein Fall für die Kennenlernkarte (Entscheidung 2). Rote Probe
  (`mutant-probe.py`, Build `echo ok`, Test Vitest der Datei):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | AK1: `halted = false` im Fang (next() nach Fehlschlag) | SohnPractice.tsx:277 | red | red |
  | AK1: kein Fokus auf „Noch einmal senden“ | SohnPractice.tsx:197 | red | red |
  | AK2: erneutes Senden mit `{}` | SohnPractice.tsx:483 | red | red |
  | AK3: „Überspringen“ ohne Wirkung | SohnPractice.tsx:484 | red | red |
  | AK4: Fang auf `instanceof ApiError` verkürzt | SohnPractice.tsx:271 | red | red |
  | AK4: Fang auf `status === 403` verkürzt | SohnPractice.tsx:271 | red | red |
  | AK5: erneutes Senden an der Sperre vorbei | SohnPractice.tsx:483 | red | red |
  | E2: Halt nur für getippte Antworten | SohnPractice.tsx:274 | red | red |

  Gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13; Grundlinie danach grün. Kein E2E (Testweg); der Rollengang
  prüft die Meldung von Hand (DevTools offline).
- 2026-11-13 · `in-arbeit` – Review-Funde (frontend-reviewer) behoben: (1) `Antwortflaeche` steht mit und ohne Meldung an
  derselben Stelle im Baum (`gesperrt={action.busy || !!failed}`, Meldung und Knöpfe darunter) – vorher baute React sie
  neu auf, und eine gewählte Option verlor `aria-pressed`. (2) `gesperrt` macht das Feld jetzt `readOnly` (nicht
  `disabled`, der Fokus bleibt), `LetterBoxes`/`LetterTiles` bekommen ein `gesperrt` (readOnly bzw. `aria-disabled`,
  kein Legen/Zurücknehmen); trifft auch `SohnTest`/`SohnBonus` während `busy` – dort nimmt das Feld ohnehin nichts an.
  Neue Fälle: Selbsteinschätzung hält an (Entscheidung 2), Option bleibt markiert, Feld nur lesbar. Rote Probe:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Fläche baut neu auf (`key` je Meldung) | SohnPractice.tsx:479 | red | red |
  | Feld ohne `readOnly` | Lektion.tsx:170 | red | red |
  | `gesperrt` ohne `failed` | SohnPractice.tsx:479 | red | red |
  | Urteils-Knöpfe neben der Meldung | SohnPractice.tsx:483 | red | red |

  Die 13 Mutanten der ersten Tabelle am umgebauten Stand neu gemessen (AK 2/3/5 jetzt :504/:505): alle 13 rot,
  Grundlinie danach grün. Gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13. Vitest 934/934, `tsc -b` 0.
- **2026-11-13** — **abgenommen** mit Commit `699b02a7` (Sprint 69, Backlog-Lauf 2026-11-13). Verifikation: Backend 1555/1555 grün (dotnet test Pugling.sln -c Release), Vitest 937/937, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `frontend-reviewer` – zwei Sollte-Funde (Neuaufbau der Antwortfläche, Feld nicht wirklich gesperrt) im selben Sprint behoben und mit Tests belegt. Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184) nach dem letzten Edit – lektion, erstbegegnung, karten-umdrehen, full-flow, test-durchgefallen-ueben, uebung-abbruch, pfad, bonus, artikel-pflicht: **10 passed**. Vater und Creator sind Regressionszeugen (Suite grün, kein Pfad ihrer Ebene im Diff außer der Vater-Wand von B-433).
