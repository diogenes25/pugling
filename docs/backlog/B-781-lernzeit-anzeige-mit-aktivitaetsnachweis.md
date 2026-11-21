---
tags: [typ/story, status/idee, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Lernzeit-Timer, aktive Minuten]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Wunsch des Vaters, Sitzung 2026-11-21"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-781 · Lernzeit-Anzeige: wie viele Minuten das Kind heute aktiv geübt hat – ohne dass sich Zeit absitzen lässt

Der Vater möchte einen sichtbaren Timer, der zeigt, wie viele Minuten das Kind an den Übungen sitzt. Die
Ursprungsidee war eine Pflicht von einer halben Stunde am Tag; sie scheiterte daran, dass sich Zeit
**absitzen** lässt. Die Anzeige muss darum **aktive** Arbeit zählen – Zeit, in der das Kind erkennbar
antwortet, nicht Zeit, in der die Seite offen ist.

Erster Blick in den Code (beim Ausformulieren nachzuprüfen): Die Messung existiert schon halb. Der Server
summiert `PracticeSession.ActiveSeconds` aus einem Heartbeat und kappt je Meldung auf 120 s
(`PositionPracticeController.cs:269-273`); `MetricsService.cs:42` macht daraus die Metrik
`MinutesPracticed`. **Aber** das Frontend meldet alle 12 s pauschal `active: true`
(`SohnPractice.tsx:155`) – gemessen wird also die Verweildauer auf der Übungsseite, nicht Aktivität.
Angezeigt wird die Zahl dem Kind (soweit gesehen) nicht.

Spannung zur Planung: Der Tagesplaner hat entschieden, Boden und Deckel in **Mengen, nicht Minuten**
festzulegen ([B-644](B-644-tagesplaner-statt-fester-lauf.md), Entscheidung 3). Diese Story ändert daran
nichts, solange die Minuten nur **angezeigt** werden; wird daraus wieder eine Minuten-Pflicht, ist das eine
Revision jener Entscheidung und gehört in die Grill-Runde.

Kandidaten für „aktiv“ (zu entscheiden): Zeit nur zwischen zwei Antworten zählen und je Antwort deckeln
(z. B. höchstens 60 s je Karte), Leerlauf ohne Eingabe/Fokus nicht melden, die Zählung serverseitig aus den
`ReviewEvent`-Zeitstempeln ableiten statt dem Heartbeat zu glauben (Selbstbetrug-Regel der CLAUDE.md).

## Verlauf

- **2026-11-21** — angelegt (Quelle: Wunsch des Vaters in der Sitzung).
