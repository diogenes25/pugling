---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Rollenwahl beim Login, Creator nur im Web]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-663 · Beim Einloggen zwischen „Vater" und „Creator" wählen

Ein Konto trägt heute Creator **und** Supervisor zugleich, die Oberfläche mischt beides. Der Vater will
zwei getrennte Profile:

- **Vater** (Rolle `Supervisor`): Fortschritt der Kinder und der Shop, also Einlösen der gekauften
  Artikel – das, was [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) beschreibt. Muss auf dem Handy
  gehen.
- **Creator**: Übungen erstellen und ansehen. Das ist so komplex, dass es auf dem Handy nicht handhabbar
  ist – **nur Web**. Außerdem erledigen das ohnehin die KI-Agenten (`Pugling.Agent.Creator`); der Mensch
  sieht hier eher nach, als dass er baut.

Beim Ausformulieren gegen den Code zu klären: Wie routet `LoginResponse.role` heute (ein Konto mit beiden
Rollen bekommt `Supervisor`), und reicht eine reine Frontend-Wahl, oder soll das Token nur die gewählte
Rolle tragen? Was heißt „nur Web" konkret – Creator-Bereich auf schmalen Bildschirmen ausblenden oder nur
nicht dafür optimieren?

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
