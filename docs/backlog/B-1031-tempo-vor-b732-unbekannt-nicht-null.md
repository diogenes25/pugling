---
tags: [typ/story, status/idee, bereich/auswertung, rolle/supervisor]
aliases: [Tempo vor B-732]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#runde-4--re-review--abnahme
grund: ""
ersetzt_durch: []
---

# B-1031 · Gefestigt und Tempo sind für Tage vor B-732 unbekannt, nicht null

Beobachtet im Abnahmegang von Sprint 193 an der Kopie der echten DB (2026-11-26): Friedrich und Theodor zeigen in der Auswertung
(B-733) an jedem Tag „gefestigt 0“, „zurückgefallen 0“ und im Profil „Tempo 0 / Tag“ – obwohl Friedrich am 24.11. 198 Antworten
richtig hatte. Grund: diese Zahlen brauchen die Box vor der Antwort (`ItemReviewEvent.BoxBefore`, B-732); alle bisherigen Zeilen
sind älter und tragen keine. Die Zahl ist nicht falsch gerechnet, liest sich für den Vater aber wie „kein Fortschritt“.

Zu entscheiden: die Tageszeile merkt sich, ob ihre Antworten eine Box trugen, und Verlauf/Profil zeigen sonst „–“ statt 0 – oder
eine Erklärung am Feld („gezählt ab dem 26.11.“). Wächst sich binnen 28 Tagen nach dem Heimnetz-Neustart aus.
