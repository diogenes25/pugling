---
tags: [typ/story, status/idee, bereich/klassenarbeit, bereich/auswertung, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Probearbeit auf Papier, Foto-Korrektur, Papier-Probearbeit]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/rockettutor-vergleich-plan.md#wo-rockettutor-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-582 · Eine Probearbeit wird auf Papier geschrieben, fotografiert und mit Note zurückgemeldet

RocketTutor gibt eine Probearbeit als PDF aus. Der Schüler löst sie auf Papier, lädt ein Foto hoch und
bekommt Punkte und Note. Pugling prüft nur am Bildschirm, die Schule prüft auf Papier, und Friedrichs
Schulformat ([B-524](B-524-vokabeltest-wie-in-der-schule.md)) zeigt, dass gerade das freie Schreiben von
Hand zählt. Die Idee: die Probeklausur aus [B-249](B-249-probeklausur-mit-notenprognose.md) lässt sich
auch als druckbares Blatt ausgeben. Die Korrektur übernimmt **nicht** das Backend beim Kind. Der Vater
korrigiert selbst oder lässt ein Creator-seitiges Werkzeug (Claude liest das Foto, wie im Skill
`lehrwerk-aufbereiten`) einen Vorschlag machen, den er prüft. Er trägt dann Ergebnis je Aufgabe ein, und
dieselbe Notenrechnung wie in B-249 greift. Zu klären: ob das Blatt nur aus Übungen mit eindeutiger
Antwort bestehen darf, wie die Antworten je Aufgabe eingetragen werden (Haken je Item oder Punktzahl), ob
die Papierprobe für die Prognose zählt wie die Bildschirmprobe, und ob das lokale Vision-Modell Handschrift
überhaupt lesen kann ([B-459](B-459-vision-modell-fuer-buchseiten.md): gemma4:e2b reicht für Buchseiten
nicht).

## Verlauf

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 3 des Übernahmeplans; Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)).
- **2026-11-15** — Prioritäts-Beleg von außen (sofatutor-Vergleich): sofatutor bewirbt Arbeitsblätter zum Ausdrucken mit Lösungsschlüssel als Vorbereitung auf Klassenarbeiten „unter Prüfungsbedingungen"; korrigiert wird dort vom Kind selbst am Schlüssel. Bestätigt die Papierform, nicht die Selbstkorrektur – die bleibt beim Vater. Vergleich in `docs/sofatutor-vergleich-plan.md` (Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-542 auf B-582: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
