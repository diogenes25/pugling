---
tags: [typ/story, status/gegrillt, bereich/agent, rolle/creator]
aliases: [KI-Creator aus Hausaufgabe, create --homework]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-651
grund: ""
ersetzt_durch: []
---

# B-656 · KI-Creator: Übung aus einer WebUntis-Hausaufgabe erzeugen (`homework`, `create --homework`)

Dies ist der KI-Creator-Teil aus [B-651](B-651-webuntis-hausaufgaben-als-pflicht.md). Die Story hängt an der
API aus [B-654](B-654-webuntis-connector-backend.md). Die Methoden dafür in `Pugling.Client` entstehen hier.

## User Story

Als **Creator** möchte ich mit dem KI-Creator aus einer offenen Hausaufgabe meines Kindes eine Übung im
Katalog erzeugen. Den Übungstyp wähle ich selbst; in den Plan übernehme ich sie wie bisher mit `assign`.

## Ist-Stand am Code

Steht mit Belegen (`Datei:Zeile`) in [B-651 → Ist-Stand am Code](B-651-webuntis-hausaufgaben-als-pflicht.md#ist-stand-am-code)
(Stand `main` 2adb3744, 2026-11-18). Vor dem Bauen gegen den dann aktuellen Code prüfen.

## Entscheidungen

Übernommen aus [B-651 → Entscheidungen](B-651-webuntis-hausaufgaben-als-pflicht.md#entscheidungen). Hier
tragen vor allem 8 (die Reihe kommt über `subjectId`) und 9 (Hausaufgabe als Eingabe der bestehenden
`create`-Pipeline, C# entscheidet).

## Akzeptanzkriterien

Übernommen aus B-651, Nummer 7.

1. Der KI-Creator listet mit `homework --child <id>` die offenen Aufgaben. `create --type <Typ> --child <id>
   --homework <id>` erzeugt die Übung in der Reihe, die zum zugeordneten Fach gehört. Eine Aufgabe ohne
   zugeordnetes Fach bricht mit einer verständlichen Meldung ab und erzeugt nichts.

## Verlauf

- **2026-11-18** — angelegt durch Teilen von B-651 beim Schätzen (Vater: Variante A, drei Stories nach
  Schicht); Entscheidungen aus B-651 übernommen, damit auf `gegrillt`.
