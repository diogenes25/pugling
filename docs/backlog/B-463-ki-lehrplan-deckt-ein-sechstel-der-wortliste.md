---
tags: [typ/story, status/idee, bereich/training, rolle/creator, rolle/supervisor]
aliases: [KI-Lehrplan zu dünn, Positions-Vorgaben des Agenten]
status: idee
prio: P1
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-463 · Der KI-Lehrplan übt 20 von 124 Wörtern der Unit, ohne Karteikasten und nur Englisch → Deutsch

Lehrplan 3 („Lehrplan: Unit 1 This is London“, Lauf vom 2026-11-10 23:16) ist das Ergebnis nach dem Umbau aus
[B-460](B-460-ki-aufgaben-programm-entscheidet.md). Stand in der Datenbank:

- Zwei Vokabel-Positionen mit je 10 Wörtern (Übungen 26 und 27). Die Unit hat 124 Wörter. Bei 14 Tagen Laufzeit
  bleiben 104 Wörter der Unit ungeübt, obwohl in zehn Tagen der Vokabeltest ansteht.
- `useLeitner: false`, `newItemsPerDay: 0`, `requireTypedTest: false`: kein Karteikasten, keine Tagesdosis
  (B-04), und der Test ist eine Selbsteinschätzung.
- Richtung `front-to-back`: Das Kind sieht das englische Wort und nennt das deutsche. Für einen Vokabeltest
  der Klasse 7 zählt die Gegenrichtung mit richtiger Schreibung.
- Ein Lückentext, keine Position zu question tags, obwohl das Profil sie als neuen Stoff der Unit nennt.
- Der Plan ist inaktiv (Absicht: „Bitte prüfen und dann aktivieren“). Daneben liegt der ältere Plan 2 mit
  gleichem Titel. Der Sohn sieht nach dem Anmelden keinen Plan (`GET student/study-plans` → `[]`).

Die von Hand geladenen Übungen der Unit (13–23: vier Wortlisten mit 124 Wörtern in beide Richtungen, zwei
Zuordnungen, drei Lückentexte, Grammatik, Übersetzung) hängen an keinem Plan. Erst ein von Hand gebauter Plan
auf der Kopie machte sie spielbar.

Zu klären: Soll der Agent bei vorhandener Wortliste die bestehenden Übungen der Unit einplanen (ganze
Wortliste, Karteikasten, Tagesdosis so, dass sie bis zum Planende durch ist) statt neue Zehner-Scheiben zu
erzeugen? Welche Vorgaben setzt er an Vokabel-Positionen?

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
