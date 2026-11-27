---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/supervisor]
aliases: [Restpunkte B-1037]
status: idee
prio: P2
art: Defekt
wo: backend
quelle: B-1037
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-1041 · Restpunkte aus dem Review von B-1037 (Malus je Tagesaufgabe)

Der `pugling-reviewer` fand im Review von [B-1037](B-1037-malus-je-tagesaufgabe-und-rueckstand.md) (Nachtlauf P, 2026-11-27) mehr Punkte, als der
Lauf beheben durfte: mit dem sechsten gezählten Fund endete der Nachtlauf (Freigabe 3). Diese Punkte sind **nicht** gebaut.

## 🟡 (zählten bzw. hätten gezählt)

1. **Ein Teilfall von B-736 AK 5 trägt nichts.** In `TagespflichtScharfTests.Tag_Ohne_Plan_Pause_Und_Heute_Kosten_Nichts` hat „Freitag scharf
   ohne Plan“ keine Aufgabenzeile; die Zusicherung liest nur `DailyTaskPenalties` und kann für diesen Tag nie fallen. Ergänzen: kein
   `GoalPenalty` im Kassenbuch für Freitag (fängt die alte Positions-Abrechnung an einem scharfen Tag ohne Plan).
2. **Preis eines vergangenen Tages.** `ComputeDayAsync` zeigt an jeder Aufgabe den **heutigen** Betrag des Kindes; ändert der Vater ihn nach
   der Buchung, zeigen Tagesansicht und `DailyTaskPenalty.Points` verschiedene Zahlen. Entweder den gebuchten Betrag zeigen, wo gebucht ist,
   oder die Lücke bewusst stehen lassen (dann in B-1037 vermerken).

## 🟢 (ohne Zählung)

- Die Erstattung in `FeedbackCorrectionService` nennt `pos.Exercise?.Title`, die Buchung den Schnappschuss `task.ExerciseTitle` – auf einem
  Aufbau-Weg zwei verschiedene Übungen. Den Schnappschuss nehmen.
- Die Zusammenfassung von `SettleClosedPeriodsAsync` sagt noch „no scheduler; login, shop purchase“ – seit B-1037 ruft auch der Zeitgeber.
- Der Zeitgeber rechnet für scharfe Kinder ab, ohne vorher `classTestPrep.ReconcileAsync` zu rufen wie Login und Kauf.
- Die Erstattungsbedingung `IsTaskMetAsync` hat keinen eigenen Fall (bei einer Tagesposition sind Periode und Aufgabentag gleich; ein Fall mit
  Wochenposition trüge sie).
- Kein Wettlauf-Test Login gegen Zeitgeber (der Code ist nach dem Muster von `SettleClosedPeriodsAsync` gesichert).

## Verlauf

- **2026-11-27** — angelegt aus dem Review von B-1037 im Nachtlauf P, nach dem Halt am Fünfer-Zähler.
