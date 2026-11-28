---
tags: [typ/story, status/idee, bereich/lehrplan, bereich/training, rolle/supervisor]
aliases: [Umschalttag scharf, Wochenziel beim Zurückschalten]
status: idee
prio: P1
art: Frage
wo: backend
quelle: B-1036
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-1039 · Umschalten zwischen Schatten und scharf: was gilt am Umschalttag und für die angebrochene Woche?

Gefunden vom `pugling-reviewer` im Review von [B-1036](B-1036-pflicht-quelle-tagesaufgabe.md) (Nachtlauf P, 2026-11-27). Beide Fälle sind heute
nicht erreichbar – die API setzt den Modus scharf erst mit [B-737](B-737-reset-zum-stichtag.md) –, darum bewusst nicht in B-1036 gebaut.
Sie verlangen eine Entscheidung des Vaters und gehören **vor** B-737.

## Die zwei Fälle (am Code belegt)

1. **Der Umschalttag geht verloren.** `PlannerSince` wird bei jedem Moduswechsel auf heute gesetzt (`ChildrenController.cs`, Zeile mit
   `child.PlannerSince = …`). Schaltet der Vater nach 6 Uhr von Schatten auf scharf, ist der heutige Plan ein Schattenplan (Momentaufnahme);
   der Tag gilt als scharf ohne Aufgaben (`ChildDutyDays.TaskPositionsOn` → leer) und schuldet nichts – die Truhe bleibt zu, bis morgen.
   Vor 6 Uhr eines scharfen Tages gibt es ebenso noch keinen Plan.
   *Optionen:* scharf gilt ab dem nächsten Lerntag (`PlannerSince` = morgen), oder der Planer ersetzt beim Umschalten den heutigen
   Schattenplan.
2. **Die angebrochene Woche beim Zurückschalten zahlt doppelt.** Eine Position mit Wochenziel, die Montag und Dienstag als Aufgabe erfüllt
   wurde, bekam zwei Tagesziel-Zeilen. Wird am Mittwoch zurückgeschaltet, rechnet der Tag wieder nach der Position: die Prüfung „Woche schon
   bezahlt?“ sucht nur Wochen-Zeilen und zahlt das Wochenziel ein zweites Mal aus der Runde vom Montag. Den Malus dieser Woche überspringt
   die alte Abrechnung dagegen ganz (`JudgedByTasks`). *Optionen:* die Woche wie den Malus auch für Zielpunkte überspringen, oder
   Tagesziel-Zeilen der Woche als „bezahlt“ zählen.

## Verlauf

- **2026-11-27** — angelegt aus dem Review von B-1036 (Nachtlauf P); Frage an den Vater, im Nachtlauf nicht entschieden (Freigabe 1).
