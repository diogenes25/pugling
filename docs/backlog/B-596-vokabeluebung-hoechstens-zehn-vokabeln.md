---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Zehn Vokabeln, Obergrenze Vokabelübung]
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

# B-596 · Eine Vokabelübung hat höchstens 10 Vokabeln

Regel des Vaters: Eine Vokabelübung soll nicht mehr als 10 Vokabeln tragen. Heute gibt es keine Grenze. In
der echten DB haben die zehn Buch-Wortlisten mit Richtung `both` im Schnitt 25 Wörter, die größte 44
(read-only gezählt am 2026-11-16). Vorschlag aus der Diskussion: Der Server lehnt beim **Anlegen und
Ändern** mehr als 10 Items ab (`validation_error`). KI-Creator und `laden.py` teilen lange Listen selbst
auf (z. B. „Part A (1/3)“). Bestehende Übungen bleiben, bis jemand sie bearbeitet, denn automatisches
Aufteilen würde den Leitner-Fortschritt an den Karten zerreißen. Zu klären: Ob ein Ändern einer
Bestandsübung mit 44 Wörtern dann abgewiesen wird oder nur nicht wachsen darf, wie das Verhältnis zur
Tagesdosis ist (die schon die neuen Wörter je Tag begrenzt) und ob der Item-Endpunkt
(`…/vocabulary/{id}/items`) dieselbe Grenze trägt.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
