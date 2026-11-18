---
tags: [typ/story, status/gegrillt, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Hausaufgaben im Vater-Web, Schulzugang einrichten]
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

# B-655 · Vater-Web: WebUntis-Zugang je Kind einrichten, Hausaufgaben ansehen, Übung daraus erstellen

Dies ist der Oberflächen-Teil aus [B-651](B-651-webuntis-hausaufgaben-als-pflicht.md). Die Story hängt an der
API aus [B-654](B-654-webuntis-connector-backend.md) und wird erst nach ihr gebaut (API-First).

## User Story

Als **Supervisor** möchte ich im Vater-Web den WebUntis-Zugang eines Kindes einrichten und prüfen und seine
Hausaufgaben sehen. Als **Creator** möchte ich aus einer Hausaufgabe direkt eine Übung erstellen, ohne den
Text abzuschreiben.

## Ist-Stand am Code

Steht mit Belegen (`Datei:Zeile`) in [B-651 → Ist-Stand am Code](B-651-webuntis-hausaufgaben-als-pflicht.md#ist-stand-am-code)
(Stand `main` 2adb3744, 2026-11-18). Vor dem Bauen gegen den dann aktuellen Code prüfen.

## Entscheidungen

Übernommen aus [B-651 → Entscheidungen](B-651-webuntis-hausaufgaben-als-pflicht.md#entscheidungen). Hier
tragen vor allem 2 (das Geheimnis kann nur geschrieben werden, die Oberfläche zeigt nur „eingerichtet“),
4 (Abruf beim Öffnen) und 8 (Fach als Text plus `subjectId?`).

## Akzeptanzkriterien

Übernommen aus B-651, Nummer 6.

1. Im Vater-Web kann der Zugang je Kind eingerichtet und die Hausaufgabenliste angesehen werden. Aus einer
   Aufgabe führt ein Weg zur Übungserstellung, deren Felder mit Fach und Aufgabentext vorbefüllt sind.

## Verlauf

- **2026-11-18** — angelegt durch Teilen von B-651 beim Schätzen (Vater: Variante A, drei Stories nach
  Schicht); Entscheidungen aus B-651 übernommen, damit auf `gegrillt`.
