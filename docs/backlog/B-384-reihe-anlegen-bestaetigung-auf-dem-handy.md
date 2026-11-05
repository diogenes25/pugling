---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Reihe anlegen auf dem Handy]
status: idee
prio: P3
art: Frage
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-384 · Prüfauftrag: sieht man „steht im Katalog" auf dem Handy, wenn man unten auf „Reihe anlegen" drückt?

B-381 hat entschieden: eine Bestätigung aus einem Formular „in oder direkt unter der Tabelle" scrollt nicht zum Meldungsplatz.
„Lehrwerk hinzufügen" (`NewSeries`) zählt dazu, weil der Platz direkt darüber steht. Auf einem schmalen Bildschirm ist das
Formular aber hoch (sieben Felder untereinander): wer unten auf „Reihe anlegen" drückt, sieht womöglich nur das Namensfeld
leer werden und nicht die grüne Meldung – dasselbe Muster wie B-232. Zu messen bei 393 × 830 (Playwright-Vorgabe); trägt
die Messung den Fall, scrollt auch `NewSeries` über `meldeVonWeitWeg`. Eine belegte Antwort „sichtbar, nichts zu tun" ist
zulässig.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-381 (Lauf U, Sprint 3, `frontend-reviewer`, 🟢).
