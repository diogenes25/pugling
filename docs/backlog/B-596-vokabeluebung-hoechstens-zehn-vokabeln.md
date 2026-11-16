---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Zehn Vokabeln, Obergrenze Vokabelübung, Untergrenze Vokabelübung, Mini-Übung]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-596 · Eine Vokabelübung hat 5 bis 10 Vokabeln

Regel des Vaters: Eine Vokabelübung soll nicht mehr als 10 Vokabeln tragen. Heute gibt es keine Grenze. In
der echten DB haben die zehn Buch-Wortlisten mit Richtung `both` im Schnitt 25 Wörter, die größte 44
(read-only gezählt am 2026-11-16). Vorschlag aus der Diskussion: Der Server lehnt beim **Anlegen und
Ändern** mehr als 10 Items ab (`validation_error`). KI-Creator und `laden.py` teilen lange Listen selbst
auf (z. B. „Part A (1/3)“). Bestehende Übungen bleiben, bis jemand sie bearbeitet, denn automatisches
Aufteilen würde den Leitner-Fortschritt an den Karten zerreißen. Zu klären: Ob ein Ändern einer
Bestandsübung mit 44 Wörtern dann abgewiesen wird oder nur nicht wachsen darf, wie das Verhältnis zur
Tagesdosis ist (die schon die neuen Wörter je Tag begrenzt) und ob der Item-Endpunkt
(`…/vocabulary/{id}/items`) dieselbe Grenze trägt. Aus [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md)
(Entscheidung 8): Zählt die Grenze **Wörter oder Karten**? Eine Liste auf „beides“ spielt je Wort zwei Karten,
10 Wörter sind dort 20 Karten in der Tagesdosis.

**Erweiterung (Vater, 2026-11-16): mindestens 5, höchstens 10 Wörter je Vokabelübung.** Es gibt Übungen mit nur
2–3 Wörtern. Solche Mini-Übungen entstehen vermutlich an Seitengrenzen beim Fotografieren: Übung 38 „Unité 2 ·
Wörter Atelier A – Anfang (S. 175)“ hat 4 Wörter. Vorschläge zum Grillen, nicht entschieden: Gezählt werden
**Wörter, nicht Karten**. Ein zu kleiner Rest wird mit der Nachbarliste derselben Unit zusammengelegt oder
zurückgehalten, bis die Liste vollständig ist, statt eine eigene Übung zu werden. Bestehende Listen
aufzuteilen zerreißt den Leitner-Stand. Bei Friedrich (Child 3) ist es gerade billig, denn sein Plan 8 hat am
2026-11-16 bei null angefangen.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — offene Frage „Wörter oder Karten?“ aus dem Grillen von B-597 übernommen.
- **2026-11-16** — um die Untergrenze 5 erweitert (Vater im Chat), Titel auf „5 bis 10“ gezogen.
