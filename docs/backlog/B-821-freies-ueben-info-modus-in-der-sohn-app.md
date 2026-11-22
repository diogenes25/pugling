---
tags: [typ/story, status/idee, bereich/sohn-app, rolle/student]
aliases: [Freies Üben ohne Münzen, Info-Modus in der Sohn-App]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf B 2026-11-23, Teilung von B-660 (Entscheidung 3, Risiko 1)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-821 · Freies Üben ohne Münzen nach einer erledigten Aufgabe

Teil von [B-660](B-660-tagesaufgaben-beliebig-oft-mit-sinkenden-muenzen.md), beim Bauen abgetrennt, wie es B-660 in Risiko 1
vorsah. B-660 führt den Leerzustand des Übens ins Bonus-Training (Münzen, nur Vokabeln im Pool); offen bleibt das
„weiter üben, so oft er will“ für **jeden** Übungstyp, ohne Münzen und ohne Wirkung auf Kasten und Pflicht (B-660
Entscheidung 3: der vorhandene Modus `PlayMode.Info`).

## Warum getrennt

Der Server spielt im Info-Modus den ganzen Pool (`PositionPracticeController.cs:129`), wählt nur auf einer Lernkarten-Stufe
die Anzeigestufe (`PositionPlayService.cs:206`) und antwortet auf `review` mit `204` ohne Rückmeldung (`:569`). Auf einer
**getippten** Stufe tippte das Kind damit ins Leere: keine Lösung, kein Richtig/Falsch. Gelesen am 2026-11-22, nicht
nachgestellt.

## Offene Frage (für den Dialog, `Wunsch`)

Was zeigt das freie Üben auf einer getippten Stufe? Optionen: (a) der Server liefert im Info-Modus eine Anzeigestufe für
jeden Typ (Karteikarte zum Selbst-Einschätzen); (b) der Info-Modus prüft und zeigt die Lösung, bucht aber nichts; (c) das
freie Üben gibt es nur für Typen mit Anzeigestufe. Die Antwort ist eine Produktentscheidung (Anti-Cheat: eine Lösung, die
ohne Wirkung gezeigt wird, ist auch eine Lösung, die der Test danach fragt).

## Verlauf

- **2026-11-22** — angelegt (Nachtlauf B, Sprint B3): aus B-660 abgetrennt; der Leerzustand von B-660 bietet das freie Üben
  darum noch nicht an.
