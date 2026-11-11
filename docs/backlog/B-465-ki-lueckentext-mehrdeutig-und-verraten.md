---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, rolle/student]
aliases: [KI-Cloze Qualität, Übung 28]
status: idee
prio: P2
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

# B-465 · Ein KI-Lückentext nach dem Umbau: Titel passt nicht, Auswahl steht im Satz, Zeitformen ohne Alternative

Übung 28 („Unit 1 · Present Progressive und Past Progressive“, Lauf 2026-11-10 23:16, also mit `ClozeMarkup`)
besteht ihren Selbsttest mit 100 % und hat vier fachliche Mängel, die kein Tor sieht:

1. Der Titel nennt present progressive, der Text übt simple past gegen past progressive.
2. Auswahllisten stehen im Fließtext: „I saw a {{4}} (Pokémon, friend, match, game)“, „{{8}} (and, but, so)“.
   Lücke 4 hat damit keine sprachlich begründete Lösung, nur eine geratene.
3. Der letzte Satz verrät sich selbst: „I {{10}} (feel) a bit worried (excited, surprised, worried, scared)“.
4. Mehrdeutige Zeitformen ohne Alternative: Lücke 7 erwartet „was running“ (nach „Suddenly, it ran away. I …
   after it“ ist „ran“ die natürliche Form), Lücke 10 erwartet „was feeling“ („felt“ ist mindestens gleich gut,
   feel ist ein Zustandsverb). Das Kind lernt hier eine falsche Regel und verliert dafür Punkte.

Zu klären: Welche dieser Fälle sind als Regel prüfbar (Klammer mit Kommaliste hinter einer Lücke; Lösung als
ganzes Wort in einer späteren Klammer; Titel nennt ein Grammatik-Thema, das nicht Schwerpunkt des Teils ist),
und was bleibt Prüfung durch den Vater im Testmodus? Gehört in die Abnahme von
[B-460](B-460-ki-aufgaben-programm-entscheidet.md) (AK 6, Live-Lauf).

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — neuer Befund beim `plan`-Lauf auf der echten Datenbank (`qwen2.5:14b-instruct`): beide Lückentexte
  zu past/present progressive scheiterten nach der Reparatur-Runde an der Regel „Lösung steht außerhalb der
  Klammern im Text“ – für `were`, `was`, `went`, `had`. Diese Formen kommen in einem Text über das past progressive
  zwangsläufig mehrfach vor; die Regel verwirft damit fast jeden Text zu diesem Grammatikthema. Ein Entwurf hatte
  zusätzlich keine Ablenker. Folge: Der Plan bekam keine Grammatik-Position.
