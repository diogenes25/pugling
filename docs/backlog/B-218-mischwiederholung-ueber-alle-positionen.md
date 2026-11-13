---
tags: [typ/story, status/idee, bereich/training, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Mischwiederholung, Mastery Challenge, Kind-weite Wiederholung]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-218 · Eine Mischwiederholung über alle Positionen des Kindes

Leitner läuft bei uns **pro Position**: fällige Karten werden nur in der Übungsrunde genau dieser
Position wieder vorgelegt. Khan Academy legt daneben alle 12 Stunden eine „Mastery Challenge" aus sechs
Fragen zu drei Skills vor, gewählt nach Zeit seit der letzten Wiederholung und aktueller Stufe; zwei
Fehler je Skill senken die Stufe, einer wird verziehen. Die Idee: eine kind-weite Wiederholungsrunde
aus den fälligsten Items aller spielbaren Positionen, zeitlich gedeckelt, mit derselben
Server-Autorität wie die Positions-Runde. Abzugrenzen von [B-04](B-04-adaptiver-vokabel-pool.md), das
den Pool **innerhalb** einer Position adaptiv macht.

## Ist-Stand am Code (2026-11-13, Teilbefund)

- Eine Mischwiederholung gibt es nicht (kein Treffer für „Mixed"/„Mischwiederholung" in Controllern, Services, Arcade).
- Der Baustein für die Auswahl existiert schon: der plan-übergreifende Lernstand je Vokabel-Item
  (`ChildVocabularyProgressController.cs:24`, Filter `onlyWeak` = Beherrschung unter 50 %, `maxBox`). Gespielt wird
  aber nur **pro Position** – eine Runde über mehrere Positionen bräuchte eine eigene Sitzung, die Punkte und Leitner
  der jeweiligen Position bucht (offene Frage für das Ausformulieren).

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (alle Student-Routen hängen an `positions/{id}`). Haken: die Anti-Cheat-Sperre „ein aktiver Plan“ (`plan_inactive`) macht daraus faktisch eine plan-weite Runde – „kind-weit“ hieße über inaktive Pläne hinweg. Grundlage: `ItemProgress` (`ItemProgressService.cs:66`). Mit B-04/B-219 zusammen grillen.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Mischwiederholung läuft **plan-weit**, also über alle Positionen des aktiven Plans. Das passt zur Anti-Cheat-Sperre „ein aktiver Plan je Kind“ und braucht keine Ausnahme. (2) Sie ist ein **Angebot mit Münzen**: freiwillig, bringt Münzen wie eine Übungsrunde, kein Malus, und sie zählt nicht auf die Positions-Pflichten. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-18); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-13** — Teilbefund am Code (Tageslauf, letzte Minuten): Auswahl-Baustein vorhanden, Spiel-Sitzung über Positionen fehlt.
- **2026-11-13** — Backlog-Abgleich gegen den Code: **gegen [B-533](B-533-bonus-training-extra-muenzen.md) abgrenzen, bevor ausformuliert wird.** Das Bonus-Training (abgenommen, `ce9b9a8d`) hat schon die Form aus Entscheidung 2 – freiwillig, Münzen, kein Malus, zählt nicht auf Pflichten –, bewegt aber bewusst keine Leitner-Box (B-533 Entscheidung 7) und wählt nach Units statt nach fälligen Items. Zu klären: eigene Runde mit eigenem Tagesdeckel und Anti-Betrug, oder Ausbau des Bonus-Trainings um einen Modus „fällige Items“ – sonst entstehen zwei Münz-Kürrunden mit verschiedenen Regeln.
