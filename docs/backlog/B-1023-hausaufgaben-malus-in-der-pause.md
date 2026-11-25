---
tags: [typ/story, status/idee, bereich/gamification, rolle/supervisor]
aliases: []
status: idee
prio: P2
art: Frage
unverifiziert: true
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
---

# B-1023 · Soll eine Hausaufgabe, die in einer Pause fällig wird, weiter Malus kosten?

Fund des `pugling-reviewer` im Sprint 191 (B-731), als Frage an den Vater abgelegt: `HomeworkService.SettleAsync`
(`backend/Pugling.Api/Services/Shared/HomeworkService.cs:155–186`) bucht den Hausaufgaben-Malus auch, wenn der Fälligkeitstag in
einer Pause liegt („krank Mo–Fr“, Hausaufgabe Mittwoch fällig → Abzug beim nächsten Login). B-731 Entscheidung 4 sagt „in der
Pause kein Malus“, ohne die Hausaufgaben (B-760) zu nennen – ob sie dazugehören, ist eine Produktentscheidung, keine
Reparatur. Kosten beider Antworten: ja → ein Krankheitstag erlässt auch die Schul-Hausaufgabe; nein → der Vater erstattet von Hand.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
