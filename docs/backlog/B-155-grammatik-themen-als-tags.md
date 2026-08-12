---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Grammatik-Themen als Tags, Grammatik-Taxonomie, Grammatik übungsübergreifend suchen]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nutzer-Dialog 2026-08-12 (Sitzung zum Lehrwerk-Weg, Commit 4876e5a)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-155 · Grammatik als Thema, nicht als Freitext

Die Grammatik einer Unit ist heute ein Freitextfeld („Grammatik der Unit", `SeriesUnit.Grammar`) — gut für
den KI-Creator, der es liest, aber blind für jede Frage, die über *diese* Unit hinausgeht. Zwei solche
Fragen fallen im Betrieb an: Der Supervisor sucht Übungen zu einem Grammatik-Thema für einen Lehrplan,
**quer über Bücher und Klassenstufen** — heute muss er wissen, in welcher Unit welchen Werks das Thema
steckt. Und der Creator will vergleichen, **wie unterschiedliche Lehrwerke dieselbe Grammatik vermitteln**
(in welcher Klassenstufe, in welcher Reihenfolge, mit welchen Übungstypen) — dafür müssten zwei Freitexte
als „dasselbe Thema" erkennbar sein, was sie nicht sind: „Present perfect vs. simple past" und
„present perfect" sind für jede Suche zwei Dinge. Gewünscht ist darum eine **geteilte Taxonomie von
Grammatik-Themen**, gewählt statt getippt, mit denselben Eigenschaften wie das bestehende geteilte
Vokabular (`InterestTag`: slug-idempotent, jeder darf lesen und verwenden). Der Angelpunkt beim
Ausformulieren ist die Frage, **woran das Thema hängt** — an der `SeriesUnit`, an der `Exercise` oder an
beiden: die Suche muss bei der *Übung* ankommen, und eine Übung erbt heute nichts von ihrer Unit. Vorbilder
für Form und Kosten liegen im Repo (`InterestTag`, `Tag`/`TagsController` fürs Klassenarbeiten-Tagging,
`VocabularyTag` als kind-skopierter Join) — ob eines davon trägt oder eine vierte Tag-Tabelle entsteht, ist
Teil derselben Recherche. Freitext soll damit nicht verschwinden: die Unit-Notizen bleiben das Material des
KI-Creators, das Thema ist die *zusätzliche*, maschinell vergleichbare Achse.

## Verlauf

- **2026-08-12** — angelegt (Quelle: Nutzer-Dialog beim Anlegen einer Übung über den neuen
  Lehrwerk-Weg). Prio P3 vorgeschlagen, noch nicht bestätigt.
