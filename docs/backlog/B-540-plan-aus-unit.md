---
tags: [typ/story, status/gegrillt, bereich/lehrplan, rolle/supervisor, rolle/creator]
aliases: [Plan aus Unit, Kapitel-Plan in einem Schritt, Vier-Wochen-Plan]
status: gegrillt
prio: P1
art: Wunsch
quelle: B-537 (Teil c, Entscheidungen 8–10)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-540 · Plan aus Unit: ein Kapitel wird in einem Schritt zum Vier-Wochen-Plan

Teil c von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Ist-Stand am Code (Punkte 1, 4 und 5) und
Begründungen stehen dort. **Braucht [B-539](B-539-ein-aktiver-plan-je-fach.md)** (Fach und Unit am Plan).

## User Story

Als *Vater* möchte ich zu Beginn einer neuen Unit mit einem Schritt einen Vier-Wochen-Plan bekommen – tägliche
Vokabeln, wöchentliche Grammatik, der Rest als Bonus –, damit mein Kind das Kapitel verlässlich durcharbeitet, ohne
dass ich jede Übung einzeln plane. Als *Creator* (KI-Agent) möchte ich nur den Katalog der Unit füllen und den
Plan vom Server bauen lassen.

## Ist-Stand am Code

Siehe [B-537 → Ist-Stand](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md#ist-stand-am-code), Punkte 1, 4 und 5.

## Entscheidungen

Übernommen aus B-537: **8** (Vokabeln täglich, Rest wöchentlich, Mathe eine tägliche Übung), **9**
(Server-Endpunkt, Agent füllt nur den Katalog), **10** (aktuelle Unit nur vorwärts).

## Akzeptanzkriterien

1. `POST supervisor/children/{id}/plans/from-unit` mit Unit (optional Startdatum, Intensität) legt einen Plan über
   28 Tage an, Fach und Unit gesetzt.
2. Jede Vokabel-Übung der Unit wird tägliche Pflicht mit einer Tagesdosis, die die Wortliste über die Laufzeit
   verteilt; jede andere prüfbare Übung wird wöchentliche Pflicht.
3. Hat die Unit keine Vokabel-Übung, wird ihre erste prüfbare Übung (nach Reihenfolge) täglich, die übrigen
   wöchentlich.
4. Übungen der Unit, die nicht Position werden, erscheinen im Bonus-Training des Kindes.
5. Eine Unit ohne prüfbare Übung liefert einen Fehler mit eigenem `code`, kein leerer Plan.
6. Wird der Plan aktiv, steht die aktuelle Unit des Lehrbuchs auf seiner Unit – aber nur, wenn sie in der Reihe
   (`Grade`, `OrderIndex`) nach der bisherigen liegt.
7. Der Vater-Wizard bietet „Plan aus Unit" und ruft den Endpunkt.
8. Der KI-Agent `plan` erzeugt Übungen in den Katalog der Unit und ruft danach den Endpunkt; Vorgabe-Laufzeit 28
   Tage, Plan weiter inaktiv angelegt.

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
