---
tags: [typ/story, status/idee, bereich/training, rolle/creator, rolle/supervisor]
aliases: [Heftseite zur Übung, Scan zur Übung]
status: idee
prio: P2
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

# B-375 · Eine fotografierte Heftseite wird zum Übungsentwurf, den der Vater vor dem Zuweisen prüft

Baobabs Kern ist: das Kind fotografiert seine Lektion, und die KI macht daraus Lernzettel, Karteikarten
und Quizze – nach den Definitionen der Lehrkraft und passend zur nächsten Arbeit. Unser KI-Creator
(`backend/Pugling.Agent.Creator`) erzeugt Übungen aus Briefing und Lehrwerks-Unit, nicht aus einer
Vorlage. [B-251](B-251-foto-der-vokabelliste-in-den-store.md) nimmt nur Vokabellisten in den Speicher
auf. Die Idee: ein Modus des Konsolen-Agenten nimmt Foto oder PDF einer Heftseite oder eines
Arbeitsblatts und legt daraus Übungsentwürfe der bestehenden Typen an; der Vater sieht Quelle und
Entwurf nebeneinander, korrigiert und weist erst dann zu. Das schließt die Lücke, die ein
Baobab-Rezensent benennt (nicht prüfen können, ob der Scan alles erfasst hat). Bleibt in der
Creator-Ebene: kein LLM im Backend, kein Zugriff des Kindes. Gelesen wird wie in B-251 mit einem
lokalen Vision-Modell in Ollama (dort entschieden in G-8). Zu klären: Abgrenzung zu B-251 (Teil davon
oder Nachfolger) und wo die Prüfung stattfindet (Testmodus der Übung oder eigene Entwurfs-Ansicht).

## Verlauf

- **2026-11-05** — angelegt (Quelle: Baobab-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Nachfolger von B-251 auf demselben Vision-Weg; (2) Prüfung ohne neue Oberfläche: `--dry-run` zeigt erkannten Text und Entwurf, angelegt wird ohne Zuweisung, der Vater probiert im Testmodus; (3) nur Typen, die der Agent heute kann. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
