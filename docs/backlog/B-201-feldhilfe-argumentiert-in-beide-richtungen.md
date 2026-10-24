---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [aenderst du sie bleibt die Aenderung, Feldhilfe pointsGoalMet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 2
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-191]
wartet_auf: ""
nachgeschaut: 2026-10-23
---

# B-201 · Die Feldhilfe zu den Zielpunkten argumentiert seit Sprint 2 in beide Richtungen

## User Story

Als **Vater** möchte ich, dass der Hilfetext eines Feldes beschreibt, was das Feld tut, damit ich mich
nicht auf ein Verhalten verlasse, das es an dieser Stelle nie gab.

## Ist-Stand am Code

`frontend/src/lib/fieldHelp.ts` (Eintrag `pointsGoalMet`) sagt:

> „Beim Hinzufügen einer Übung steht hier deren Empfehlung – **änderst du sie, bleibt die Änderung**;
> bereits angelegte Positionen berührt eine spätere Empfehlung nie."

**Beide Hälften gelten seit `bd6994a` nicht mehr gleichzeitig.** Der Reviewer hat den Reset
(`setPunkteGetippt(false)` beim Anlegen) ausdrücklich **mit der ersten Hälfte begründet** — und genau
dieser Reset macht die zweite Hälfte über die Positionsgrenze hinweg unwahr: Die Änderung bleibt
*innerhalb* einer Position, nicht darüber hinaus.

**Und eine zweite Ungenauigkeit, älter:** Derselbe Hilfetext wird über `topic="pointsGoalMet"` auch im
**Assistenten** gerendert (`VaterWizard.tsx`). Dort trifft „Beim Hinzufügen einer Übung steht hier deren
Empfehlung" **nie** zu — der Assistent schickt immer seine Intensitäts-Zahl und erbt nichts. Das ist
Altbestand aus B-45 (der Vorbehalt wurde damals nur bei `exercisePoints` nachgetragen) und hängt an der
offenen Frage [B-186](B-186-assistent-und-planseite-setzen-zielpunkte-verschieden.md).

## Die echte Lücke

Ein Hilfetext, der an zwei Orten gerendert wird und nur an einem stimmt — plus ein Satz, den eine spätere
Reparatur zur Hälfte aufgehoben hat, ohne ihn anzufassen.

## Offene Punkte

1. Nur die zweite Hälfte präzisieren, oder den Text je Ort trennen? Empfehlung: **erst B-186 klären** —
   solange offen ist, ob der Assistent überhaupt erben soll, wäre eine getrennte Fassung eine Antwort auf
   eine Frage, die niemand entschieden hat.
2. Trägt `HelpTopic` überhaupt zwei Fassungen desselben Begriffs? `fieldHelp.ts` ist ausdrücklich gebaut,
   damit **eine** Formulierung je Begriff existiert — zwei Fassungen wären ein Bruch mit seiner
   Begründung. Nachsehen, nicht vermuten.

## Akzeptanzkriterien

1. Der Hilfetext `pointsGoalMet` beschreibt auf der Plan-Seite nur, was dort gilt: die Empfehlung wird
   beim Hinzufügen vorbelegt, eine Änderung gilt für **diese** Position.
2. Im Assistenten behauptet derselbe oder ein eigener Text nichts, was dort nicht zutrifft – gemäß der
   Antwort auf [B-186](B-186-assistent-und-planseite-setzen-zielpunkte-verschieden.md).
3. `feldhilfe.spec.ts` (oder ein Vitest) sichert den neuen Wortlaut je Ort zu.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Freigabe 1), auf Grundlage der Nutzer-Entscheidung zu B-186 (im Assistenten gewinnt die Intensität).

1. **Ein Text für beide Orte**, der beide Orte benennt – wie `exercisePoints` in derselben Datei. `fieldHelp.ts` hält eine
   Formulierung je Begriff (Punkt 2); zwei Fassungen wären der Bruch, den die Datei verhindern soll. *Kosten:* der Text ist
   einen Satz länger und spricht an jedem Ort auch über den anderen.
2. **Test als Vitest** über den Text, nicht als E2E je Ort: beide Orte rendern dieselbe Konstante. *Kosten:* ob das ⓘ am
   Feld hängt, prüft weiter `feldhilfe.spec.ts` (unverändert grün).

## Schätzung

- **groesse:** XS · **wo:** frontend · **migration:** nein · **vertragsbruch:** nein. **Testweg:** `src/lib/fieldHelp.test.ts`, Rollengang Assistent.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 2. `entgangen_bei: [B-191]` für die erste
  Hälfte — sie wurde durch jene Story unwahr. Die zweite Hälfte ist älter und gehört zu B-186.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`fieldHelp.ts:54-55`, Reset `PlanPositions.tsx:512,632`, Render im Assistenten `VaterWizard.tsx:590`). Ist das XS-Bauticket der Antwort auf B-186 – beide in einer Sitzung entscheiden, B-201 danach autonom bauen.
- **2026-10-23** — **Blocker gefallen** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): B-186 ist entschieden (im Assistenten gewinnt die Intensität). Damit ist die Richtung klar – die Feldhilfe sagt auf der Plan-Seite, dass die Empfehlung vorbelegt wird und eine Änderung für diese Position gilt, und behauptet im Assistenten nichts davon. Autonom grillbar, XS, Paket NP-12.
- **2026-10-23** — vierter Nachtlauf, Sprint 9 (NP-12, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): gegrillt, geschätzt, gebaut. Neuer Text: „Auf der Plan-Seite steht hier beim Hinzufügen einer Übung deren Empfehlung; was du änderst, gilt für diese eine Position. Der Assistent belegt den Wert stattdessen aus der gewählten Intensität vor." Rote Probe (Auflage 5): `fieldHelp.test.ts` gegen den alten Text – erwartet 2 rot / 1 grün (der dritte Fall ist ein Wächter gegen Verlust beim Umschreiben) → gemessen **2 rot an `:16` und `:22`, 1 grün**. `frontend-reviewer`: ein Nit („setzt" klang endgültig, es ist eine Vorbelegung) → umformuliert samt Regex (gezählt, Sprint-Zähler). Vitest **409/409**. **Rollengang im Browser** (Freigabe 6, Wegwerf-DB `pugling_walk5.db`, Server nach dem letzten Produkt-Edit gestartet): Assistent ohne Eingabe wählt weiter „Bestehendes Kind / Sohn" vor; Durchlauf bis „Feinschliff", das ⓘ an „Punkte je Ziel" zeigt den neuen Text; `GET /openapi/v1.json` trägt Beispiele an 87 Operationen (der Katalog lädt über den neuen Lesepfad). Eingaben über die echten Handler der Seite (Klicks der Extension kamen nach dem Wiederverbinden nicht an). Playwright voll: **82 passed**. Abgenommen; Commit: `90387d5`.
- **2026-10-23** — Nachschau zu Sprint 9 (fünfter Nachtlauf, [Protokoll](../pm-sitzung-2026-10-24-nacht.md)), frischer Prüfer: Text gegen Code nachgelesen – Assistent `VaterWizard.tsx:235` (`setPointsGoalMet(it.points)`), mitgeschickt `:366`, Server `PlanPositionsController.cs:184` (`dto.PointsGoalMet ?? …`); Plan-Seite `PlanPositions.tsx:194`, `:632`, `:512` – hält. Rote Probe „2 rot an `:16`/`:22`" nachvollzogen. Kein Verhaltensbefund; Doku-Nit: der Kommentar `PlanPositions.tsx:505-510` zitiert die Feldhilfe noch im alten Wortlaut (inhaltlich richtig).
