---
tags: [typ/story, status/idee, bereich/katalog, bereich/medien, rolle/creator, rolle/student]
aliases: [Interessenbasierte Übungen, Zielgruppe statt Kind]
status: idee
prio: P2
art: Wunsch
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
unverifiziert: true
---

# B-46 · Übungen entstehen für ein Interessenprofil, nicht für ein bestimmtes Kind

Die fachliche Regel: Ein Kind gibt seine **Interessen** an; der Creator baut Übungen **für ein Kind mit
diesen Interessen** — nicht für dieses eine Kind. Kommt ein zweites Kind mit demselben Profil, bekommt es
die vorhandenen Übungen. Ein Mädchen, das Einhörner mag, bekommt Übungen mit Einhörnern; ein Junge mit
Vorliebe für Roboter bekommt beim Verb „jump" den springenden Roboter. **Interessenbasiert, nicht
kindbasiert.**

Bei den **Bildern** ist genau das schon gebaut: die geteilte Interessen-Taxonomie (`InterestTag`, von
Bildern *und* Kindern referenziert) und der `MediaSelector` wählen aus vielen Darstellungen desselben
Motivs die passende je Kind ([docs/medien-bilder.md](../medien-bilder.md), „ein Motiv, viele Bilder").
Bei den **Übungen** fehlt die Entsprechung an zwei Stellen:

1. **Die Übung trägt keine Zielgruppe.** `Exercise` hat Klassenstufe, Schulart, Quelle, Kategorie — aber
   kein Merkmal „für welches Interessenprofil eingekleidet". Eine für Einhörner geschriebene Übung ist im
   Katalog von einer für Roboter geschriebenen nicht unterscheidbar, also für das zweite passende Kind
   auch nicht auffindbar.
2. **Der KI-Creator brieft auf eine `ChildId`**, nicht auf ein Profil: er lädt Kind, Interessen,
   Lehrbücher und Schwachwörter über die Supervisor-/Student-API
   ([Briefing/BriefingBuilder.cs](../../backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs)). Das
   Ergebnis landet dann im **globalen** Katalog (`Exercise` hat keine `ChildId`) — ohne Spur, wofür es
   gemacht war. Nebenwirkung: Ein reines Lehrer-Konto kann so gar keine eingekleidete Übung erzeugen, weil
   es die Betreuungsprüfung am Kind nicht besteht.

Beim Ausformulieren zu klären (Material für die Grill-Runde):

- **Welcher Interessen-Träger gilt?** Am `Child` liegen heute *zwei*: `Interests` als Freitext-JSON (füttert
  den LLM-Prompt) und die gewichteten `ChildInterest`-Zeilen auf der `InterestTag`-Taxonomie (füttert die
  deterministische Bildwahl). Wiederverwendbare Übungen brauchen die **taxonomische** Seite; der Freitext
  wäre als Schlüssel untauglich. Wer sie **pflegt**, klärt [B-50](B-50-kind-beschreibt-sich-selbst.md)
  (heute der Supervisor, künftig das Kind selbst) — die Frage nach dem Geschlecht muss in beiden Stories
  dieselbe Antwort bekommen.
- **Gehört das Geschlecht zur Zielgruppe** oder nur zur Einkleidung? Das Beispiel „Übungen für Mädchen"
  legt ein eigenes Merkmal nahe — mit den bekannten Kosten einer harten Filterung.
- **Wie kommt die Passung an den Supervisor?** Als Filter beim Zuweisen („passt zu den Interessen dieses
  Kindes"), als Sortierung, oder erst im Auto-Generator ([B-18](B-18-auto-lehrplan-generator.md))?
- **Wie eng darf getroffen werden?** Ein Profil aus fünf Tags trifft fast nie exakt; es braucht eine
  Bewertung wie beim `CreatorProfileService` (harte Ausschlüsse, dann Punkte) statt Gleichheit.

**Voraussichtlich XL — Teilungsfall** (siehe README): Zielgruppen-Merkmal am Katalog, Umbau des
Agenten-Briefings und die Sicht im Vater-Web sind unabhängig auslieferbar. Verwandt:
[B-19](B-19-schuelerprofil-ki-lehrplan.md), [B-21](B-21-ki-creator-foerdermodus.md),
[B-18](B-18-auto-lehrplan-generator.md).

**Diese Story geht [B-09](B-09-lehrer-hausaufgaben.md) voraus**: Die Hausaufgabe des Lehrers nennt nur
Fach und Kapitel — dass daraus je Kind ein *anderes* Übungsset wird, ist genau das hier Fehlende.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung; die fachliche Regel kommt vom Nutzer,
  der Ist-Stand ist nur angelesen und beim Ausformulieren zu belegen).
