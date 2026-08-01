---
tags: [typ/story, status/idee, bereich/katalog, bereich/training, rolle/creator, rolle/supervisor]
aliases: [Punkte-Empfehlung, RewardPoints]
status: idee
prio: P2
art: Wunsch
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
unverifiziert: true
---

# B-45 · Die Punkte-Empfehlung des Creators soll der Supervisor übernehmen können

`Exercise.RewardPoints` ([Models/LearnEntities.cs](../../backend/Pugling.Api/Models/LearnEntities.cs))
wird über die Creator-API geschrieben und gelesen, ist im Create/Update-DTO **Pflicht** — und **kein
einziger Bewertungspfad liest es**. Gepunktet wird ausschließlich über die `PlanPosition`
(`PointsGoalMet`, `NewContentPoints`, Combo/Speed, `PenaltyCoins`). Ein Creator, der dort 15 einträgt,
bewirkt heute nichts und bekommt darüber auch keine Rückmeldung.

Gewollt ist **nicht**, das Feld zu löschen, sondern seine fehlende zweite Hälfte: Der Creator gibt eine
**Empfehlung** ab, der Supervisor kann sie beim Anlegen einer Position **übernehmen**. Das ist dasselbe
Hybrid-Muster, das `DefaultStage`, `DefaultItemCount`, `DefaultUseLeitner` und `SuggestedBonus` schon
tragen („die Position erbt, solange sie nicht selbst überschreibt"). Damit steht das Feld auch nicht mehr
im Widerspruch zur Ebenen-Trennung: Empfehlen ist Inhaltsarbeit, Festlegen bleibt Steuerung.

Beim Ausformulieren zu klären: welche Positions-Punktgröße die Empfehlung meint (Zielbelohnung,
Basispunkte je neuem Inhalt oder beides), ob sie wie die Geschwister still vererbt wird oder im Vater-Web
als sichtbarer Vorschlag mit „übernehmen"-Knopf erscheint, und ob das Feld dabei von Pflicht auf optional
wechselt (dann Vertragsänderung).

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung; das tote Feld ist belegt, die
  Ausgestaltung nicht).
