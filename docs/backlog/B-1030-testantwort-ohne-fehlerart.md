---
tags: [typ/story, status/verworfen, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Testantwort ohne Fehlerart]
status: verworfen
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-26.md#iteration-193--umgesetzt
grund: "Kein Defekt: der Test fragt ein am selben Tag eingeführtes Wort auf der Auswahlstufe (6) ab, auch mit requireTypedTest; eine Auswahl trägt nach B-732 bewusst keine Art (FehlerartTests.Richtig_Auswahl_Und_Selbsteinschaetzung_Tragen_Keine_Art). Die E2E hatte per API Freitext an eine Auswahlkarte geschickt. Gemessen an: Position über die Vater-API, Stufe 4, mit und ohne requireTypedTest, Einführung über Lernkarten, Abgabe im Ganzen."
ersetzt_durch: []
entgangen_bei: []
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

## Verlauf

- **2026-11-26** — nachgestellt als Integrationstest (Weg wie oben): beide Testzeilen auf `StageValue = 6` (Auswahl), mit und ohne
  `requireTypedTest`. `AnswerErrorClassifier` gibt für eine Auswahlstufe `null` zurück – so entschieden in B-732. `verworfen`;
  der Nachstell-Test ist nicht übernommen (er hätte die falsche Regel festgeschrieben). Offen bleibt nur, dass eine Auswahl
  in der Auswertung als Fehlantwort ohne Art steht – das ist die Regel, kein Fehler.
