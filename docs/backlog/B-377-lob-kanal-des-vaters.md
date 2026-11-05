---
tags: [typ/story, status/idee, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Lob-Kanal, Lob schicken]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/baobab-vergleich-plan.md#wo-baobab-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-377 · Ein Lob-Kanal: der Vater schickt Lob, und Erfolge des Kindes erreichen den Vater

Baobab verspricht Eltern Entlastung: ermutigen und gratulieren statt kontrollieren. Unser Vater-Web
zeigt vor allem Pflicht, Zielverfehlung und Malus; einen Weg, das Kind für etwas Gelungenes zu loben,
gibt es nicht (keine Fundstelle zu Lob oder Ermutigung in `backend/Pugling.Api`, geprüft am
2026-11-05). Die Idee: der Supervisor schickt dem Kind aus einer Erfolgszeile heraus ein kurzes Lob,
das die Sohn-Arcade beim nächsten Öffnen zeigt, und das Vater-Dashboard hebt Erfolge (Ziel erreicht,
Klausur bestanden, Mission erfüllt) eigens hervor. Ergänzt die Kernidee „Pflicht mit Konsequenz", ohne
sie aufzugeben. Zu klären: ob Lob nur Text ist oder auch Münzen tragen darf (dann berührt es die
Wallet-Concurrency-Regel), und ob mehrere Supervisor getrennt sichtbar loben.

## Verlauf

- **2026-11-05** — angelegt (Quelle: Baobab-Vergleich, Etappe 2 des Übernahmeplans).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Lob ist Text ohne Münzen; (2) jedes Lob trägt den Namen des Supervisors, die Arcade zeigt es einmal beim nächsten Öffnen; (3) Kurztext mit Obergrenze plus vorgefertigte Sätze; (4) das Vater-Dashboard hebt Erfolge hervor. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
