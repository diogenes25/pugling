---
tags: [typ/story, status/abgenommen, bereich/training, rolle/supervisor, rolle/student]
aliases: [Rückmeldung zeigt spätere Antwort]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-26.md#runde-3--rollengang-an-der-kopie
grund: ""
ersetzt_durch: []
entgangen_bei: [B-628]
nachgeschaut: ""
---

# B-1027 · Der Rückmeldungs-Eingang zeigt eine Antwort, die das Kind erst nach der Meldung gegeben hat

Beobachtet an der Kopie der echten DB (2026-11-26, Rollengang Vater): Theodor tippte am 25.11. um 21:40:12 „amazong“, meldete
um 21:40:17 „zu streng“ und tippte um 21:40:56 „amazing“ (richtig). Der Eingang `GET supervisor/feedback` zeigt dem Vater
**Lösung „amazing“, Antwort „amazing“, zu streng** – er kann die Meldung nicht beurteilen und hält den Bewerter für kaputt.

Ursache: `ExerciseFeedbackService.ChildAnswerAsync` liest beim Abruf die **jüngste** gespeicherte Antwort zur Karte, ohne
Zeitgrenze. Jede Antwort nach der Meldung verdrängt die gemeldete.

## Akzeptanzkriterien

1. Die gezeigte Antwort einer Übungs-Meldung ist die letzte Antwort zur Karte **bis zur Meldung** (`LastReportedAt`); eine
   spätere Antwort ändert sie nicht. *Rot, wenn* die Zeitgrenze in der Abfrage der Antwort-Historie fehlt.
2. Dasselbe für eine Testantwort, die nach der Meldung abgegeben wurde. *Rot, wenn* die Zeitgrenze in der Abfrage der
   Testversuche fehlt.

## Verlauf

- **2026-11-26** — angelegt aus dem Rollengang (Vater, Kopie), `in-arbeit` im Sprint 193.
- **2026-11-26** — gebaut: `ChildAnswerAsync` liest Test- und Übungsantworten nur bis `LastReportedAt`; Vertragsdoku
  (`FeedbackDtos.cs`, Service-Summaries) nachgezogen.
  - AK 1, Übungsantwort nach der Meldung → `ExerciseFeedbackTests.Meldung_zeigt_die_Uebungsantwort_vor_der_Meldung_nicht_eine_spaetere`,
    Mutante M1 (Zeitgrenze der Historie weg) → rot.
  - AK 2, Testantwort nach der Meldung → `ExerciseFeedbackTests.Meldung_zeigt_keine_Testantwort_nach_der_Meldung`, Mutante M2
    (Zeitgrenze der Tests weg) → rot.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M2 | `ExerciseFeedbackService.cs:161` | red | red |
  | M1 | `ExerciseFeedbackService.cs:172` | red | red |

  Review (`pugling-reviewer`): Grenze richtig für Testmeldungen; für zusammengelegte Übungsmeldungen geht die erste Antwort
  verloren → eigene Story B-1029 (bewusst ausgelassen, das Sprint-Ziel hält ohne sie).
- **2026-11-26** — `abgenommen` mit Commit `e18934f0`. Rollengang Vater an der Kopie: Meldung 9 zeigt „amazong“. Suite 2623/2623.
