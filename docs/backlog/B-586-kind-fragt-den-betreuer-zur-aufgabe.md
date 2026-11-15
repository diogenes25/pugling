---
tags: [typ/story, status/idee, bereich/training, rolle/student, rolle/supervisor]
aliases: [Frage an den Betreuer, Hausaufgaben-Frage, Kind fragt]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/sofatutor-vergleich-plan.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-586 · Das Kind stellt zur aktuellen Aufgabe eine Frage an seine Betreuer und bekommt die Antwort in der App

sofatutors teuerstes Merkmal ist ein Mensch, der Fragen zur Aufgabe beantwortet (Hausaufgaben-Chat mit
Lehrkräften, auch per Foto, erklärend statt lösend). Pugling hat den Menschen schon – den Vater –, aber
keinen Kanal: Hängt Friedrich an „quel, quelle, quels, quelles" (8 Antworten, 0 richtig, siehe
[B-536](B-536-aufbau-uebungen-statt-versagen.md)), kann er nur weiter falsch tippen oder aufhören. Das
Erfassungs-Widget mit Kontext-Mitschnitt gibt es (`backend/Pugling.Api/Controllers/RemarksController.cs:12`),
es ist aber ein Dev-Werkzeug und in der Sohn-App nur im Entwicklungsmodus eingebunden
(`frontend/src/sohn/SohnApp.tsx:174`); ein Nachrichten-Modell zwischen Kind und Betreuer gibt es in
`backend/Pugling.Api/Models/` nicht.

Die Idee: an der laufenden Karte ein Knopf „Frage an Papa". Das Kind schreibt einen Satz; der Server
hängt Position, Übung, Item und die letzten Antworten des Kindes an (wie beim Widget, aber serverseitig
erhoben). Die Frage erscheint bei **allen** Betreuern des Kindes (Multi-Supervisor), einer antwortet, die
Antwort sieht das Kind beim nächsten Öffnen an der Position. Kein LLM im Backend: wer will, lässt sich
beim Creator offline einen Antwortentwurf machen und prüft ihn.

Zu klären beim Ausformulieren:

- **Anti-Cheat:** Im Klausur-Modus und im Positions-Test gesperrt (sonst ein Lösungskanal per Vater);
  der Kontext für den Vater darf `Expected` enthalten, die Rückgabe ans Kind nur die Antwort des Vaters.
- **Pflicht:** Hält eine offene Frage die Pflicht an, oder läuft der Malus weiter? Empfehlung: läuft
  weiter, die Frage ist Hilfe, kein Aufschub – sonst wird sie zum Ausweg aus der Pflicht.
- **Belohnung:** Bringt eine beantwortete Frage, nach der die Karte sitzt, etwas? (Friedrichs Hebel,
  [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md).)
- **Abgrenzung** zum Lob-Kanal [B-377](B-377-lob-kanal-des-vaters.md): andere Richtung, möglicherweise
  derselbe Nachrichten-Unterbau.
- **Foto der Heftseite** als Anhang (wie bei sofatutor) erst in einem zweiten Schritt; Medien-Upload
  vom Kind ist neu.

## Verlauf

- **2026-11-15** — angelegt (Quelle: sofatutor-Vergleich, Etappe 1 des Übernahmeplans; Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-546 auf B-586: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-557). Inhalt unverändert.
