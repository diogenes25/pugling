---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/supervisor]
aliases: []
status: abgenommen
prio: P2
art: Frage
unverifiziert: false
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-26
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

## Entscheidung

Der Vater (2026-11-26): **nein, kein Malus.** Eine Hausaufgabe, deren Fälligkeitstag in einer Pause des Kindes liegt, kostet
nichts – ein krankes Kind ist auch die Schul-Hausaufgabe los. Nur die Pause zählt; ein Fälligkeitstag am freien Sonntag bleibt
das Datum der Schule.

## Akzeptanzkriterien

1. Fällig am letzten Pausentag, nicht abgegeben → kein Malus beim nächsten Login. *Rot, wenn* die Abrechnung die Pause überliest.
2. Fällig am Tag nach der Pause → Malus wie bisher. *Rot, wenn* mehr als die Pause erlassen wird.

- **2026-11-26** — Antwort des Vaters: kein Malus in der Pause. Gebaut in `9cf14491`: `HomeworkService.SettleAsync` lässt
  Hausaufgaben mit `ChildDutyDays.InPause(DueOn)` aus. AK 1/2 → `HausaufgabenTests.Faellig_In_Der_Pause_Kostet_Keinen_Malus`
  (Theorie, zwei Zeilen); Mutanten M6 (Pause überlesen) und M7 (auch den Tag danach erlassen) → je rot. Backend-Suite 2611/2611
  (Release), `pugling-reviewer` ohne Korrektheitsfund. `abgenommen`. **Rollengang ausgefallen:** es gibt noch keine
  Hausaufgaben-Oberfläche (B-762/B-763); den Weg Vater legt Pause an → Kind meldet sich an → keine Buchung fährt der
  Integrationstest über die echte API. Hinweis des Reviews für B-763: `HomeworkResponse.PenaltyCoins` nennt für eine
  erlassene Hausaufgabe weiter den Betrag des Kindes – eine künftige Karte darf daraus kein „Malus droht“ machen.
