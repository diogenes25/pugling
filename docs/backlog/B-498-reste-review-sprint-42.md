---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [Freitext-Fach in der Reihenwahl der Konsole]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 42)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-498 · Die Konsole entscheidet „Profil passt zum Fach" für Freitext-Profile noch anders als das Briefing

Gefunden im `pugling-reviewer` zu Sprint 42 (🟢, ohne Zählung zurückgestellt). Seit B-495 lehnt `ProfileFit` ein
Freitext-Profil (`SubjectId == null`, `SubjectName` gesetzt) eines anderen Fachs ab. Zwei Stellen davor entscheiden
dieselbe Frage noch nur über die Fach-Id:

1. `SeriesOfChildsBookAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs:592-606`) zählt für ein Freitext-Profil
   „Latein" alle Bücher des Kindes: bei einem Englisch-Buch wird dessen Reihe gewählt und das Briefing lehnt ab (kein
   Kreis, aber die passende Meldung „kein Lehrbuch in Latein" bei `:602` wird nie erreicht); bei mehreren Büchern bietet
   „wähle mit --series" auch Reihen an, die das Briefing ablehnt. Vorschlag: den Namensvergleich aus
   `ProfileFit.TeachesAnother` als einen Helfer teilen.
2. `profiles --child … --subject X` (`AgentCommands.cs:242`) listet Freitext-Profile eines anderen Fachs als Treffer
   (Server-Filter `CreatorProfileService.cs:57`).

Dazu zwei Reste aus B-495: `ProfileFit` gibt den CLI-Tipp „lass --series/--series-unit weg" auch direkten
Pipeline-Aufrufern; die Summary des B-308-Tests nennt nicht, dass die Plan-Hälfte allein nichts beweist.

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 42, Nachtlauf).
