---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Alles gibt Münzen, Zusatzübungen mit Münzen]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18 beim Ansehen des Pfads (Testjunge, Child 4)"
grund: ""
ersetzt_durch: []
unverifiziert: true
---

# B-657 · Jede Übung gibt Münzen – auch die Zusatzübungen ohne Pflicht

Der Vater am 2026-11-18: „Alles muss Münzen geben.“ Die Startseite zeigt die Fächer seit dem Umbau als
aufklappbare Reiter mit „🪙 noch N“ für die offenen Ziele und die freien Positionen getrennt als
„Zusatzübungen“ – der Gedanke dahinter: wer mehr Münzen will, geht in die Zusatzübungen. Das zahlt heute
nichts: seit [B-538](B-538-bonus-muenzregeln-form-und-degression.md) bucht eine Position **ohne Pflicht**
keine Münzen (`PositionPracticeController`, `paysCoins = PositionProgressService.HasDutyOn(…)`), damit neben dem
gedeckelten Bonus-Training kein zweiter, ungedeckelter Münzweg vor der Pflicht offensteht. Am Knoten steht
darum „frei · ohne Münzen“.

Gemessen am Ledger von Child 4 (2026-11-16…18): Ziel-Münzen (`Goal`, 15–20 je Ziel) kommen an; Antwort-Münzen
(`Base`/`Combo`) gab es zuletzt am 2026-11-16 – seither keine, auch nicht auf Pflicht-Positionen. Das ist der
zweite Teil des Eindrucks „Tagesmissionen geben keine Münzen“ und vor dem Ausformulieren am Code zu klären
(`scored` hat viele Ausschlüsse: Lernkarten-Stufen, Selbsteinschätzung, `eased`, schon heute gewertet).

Zu entscheiden: Zahlen Kür-Positionen Münzen (gedeckelt? erst nach der Pflicht? degressiv wie das
Bonus-Training?), und soll das Kind die Antwort-Münzen beim Üben sehen? Berührt den Anti-Cheat-Grund von B-538.
