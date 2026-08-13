---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Art gegen Typ, Vokabeln heißt zweimal etwas anderes]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-157 (Grill-Runde 2026-08-13, Entscheidung 5)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-163 · „Art" und „Typ" tragen dieselben Wörter — nebeneinander in derselben Filterleiste

Die Übungssuche hat zwei unabhängige Achsen, und beide benutzen dasselbe Vokabular. Ein Nutzer, der nach
„Vokabeln" filtern will, hat zwei Felder zur Auswahl und keinen Hinweis, welches er meint.

Beobachtet beim Grillen von [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md): Der **Übungstyp**
heißt „Vokabeln" (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:18`) und „Leseverständnis"
(`BuiltInExerciseTypes.cs:22`); die **Art** (`ExerciseCategory`) heißt im Seed „Vokabeln"
(`Data/Seed.cs:605`, `:997`) und „Leseverstehen" (`:999`). Im Vater-Web stehen die beiden Pulldowns als
„– alle Arten –" und „– alle Typen –" direkt nebeneinander (`ExerciseFilterBar.tsx`, und ebenso im
Assistenten).

Die Achsen sind sachlich verschieden und beide berechtigt: der **Typ** ist das Lernverfahren und kommt aus
dem Server-Manifest, die **Art** ist ein freier Ordnungsbegriff je Fach. Nur ihre Beschriftungen sagen das
nicht.

**Dringlichkeit mit Verfallsdatum:** B-157s Entscheidung 1 macht die Seed-Arten fail-closed — danach kann
**niemand** sie mehr umbenennen. Eine Entzerrung muss also im **Seed** passieren, und je später sie kommt,
desto mehr bestehende Datenbanken tragen die kollidierenden Namen weiter.

Beim Ausformulieren zu klären: ob die Art umbenannt wird (etwa „Wortschatz" / „Lesen"), der Typ, oder ob
die Beschriftungen der beiden Filter die Achse deutlicher machen müssen statt der Werte — und ob es weitere
Kollisionen als die zwei gefundenen gibt (Grammatik? die Mathe-Arten?).

## Verlauf

- **2026-08-13** — angelegt beim Grillen von
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) (Entscheidung 5). **Bewusst nicht dort
  mitgenommen:** B-157 ist eine Eigentums-Story, ihr Ziel ist ohne die Entzerrung erfüllt, und eine
  Umbenennung von Produktinhalt daran zu hängen hätte ihre Akzeptanzkriterien unscharf gemacht.
  `unverifiziert: true`, obwohl die vier Fundstellen am Code belegt sind: die **fachliche** Frage (welche
  Achse weicht aus, oder reichen die Beschriftungen?) ist nicht erhoben, und ob es weitere Kollisionen gibt,
  ist nicht ausgezählt.
- **2026-08-13** — Prio **P3 → P2** und damit vorgezogen, auf Entscheid des Nutzers. Der Grund ist nicht
  gestiegene Wichtigkeit, sondern ein **geschlossenes Fenster**: Mit der Abnahme von
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) am selben Tag sind die sieben Seed-Arten
  fail-closed — `PATCH` liefert für **jeden** `403 not_owner`. Eine Entzerrung der Namen ist damit nur noch
  über `Data/Seed.cs` möglich und wirkt ausschließlich auf **frische** Datenbanken; jede bestehende trägt die
  kollidierenden Namen dauerhaft. Je später die Story kommt, desto mehr Datenbanken sind das.
  `unverifiziert: true` bleibt: die vier Fundstellen sind am Code belegt, aber die **fachliche** Frage
  (welche Achse weicht aus — oder reichen die Beschriftungen der Filter?) ist nicht erhoben, und ob es
  weitere Kollisionen als die zwei gefundenen gibt, ist nicht ausgezählt.
