---
tags: [typ/story, status/idee, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Testantwort ohne Fehlerart]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#iteration-193--umgesetzt
grund: ""
ersetzt_durch: []
entgangen_bei: [B-732]
nachgeschaut: ""
---

# B-1030 · Eine falsch getippte Testantwort trägt auf dem echten Weg keine Fehlerart

Gefunden beim Bau der E2E `auswertung-verlauf` (Sprint 193, 2026-11-26), bewusst nicht in Sprint 193 behoben: die Auswertung
(B-733) zeigt korrekt „–“, wo keine Art gespeichert ist; der Fehler liegt auf der Schreibseite von B-732.

**Weg:** Position über `POST supervisor/study-plans/{id}/positions` (Stufe 4, Leitner, Tagespflicht), die zwei Wörter im Üben
über Lernkarten eingeführt, dann `POST …/tests` und `…/submit` im Ganzen mit `dog…x` (Tippfehler) und `cat…` statt „Katze“.
Der Test fragt das Fremdwort und erwartet „Hund“/„Katze“. Beide Antworten sind falsch, getippt gespeichert (`givenAnswer` steht in
der Historie) – `ErrorKind` ist bei **beiden** `null`.

**Gegenprobe:** `FehlerartTests.Test_Speichert_Die_Art_An_Testzeile_Und_Verlauf` (Position per `SeedLeitnerPosition`, Wörter vorab
eingeführt, der Test fragt das Fremdwort) speichert die Art; ein nachgestellter Fall dort mit angehängtem Buchstaben ergab `Typo`.
Unterschied also im Aufbau (Richtung der Testkarte, Einführung über Lernkarten oder die über die API angelegte Position) – noch nicht
eingegrenzt. `AnswerErrorClassifier.ClassifyAsync` gibt `null` nur bei richtig, Zahl oder Auswahlstufe zurück.

**Wirkung:** der Vater sieht in der Auswertung für Testfehler kein Warum; die Fehlerarten-Zähler des Profils fehlen für Tests.
