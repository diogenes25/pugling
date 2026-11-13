---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, rolle/student]
aliases: [KI-Cloze Qualität, Übung 28]
status: ausformuliert
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

## Ist-Stand am Code (2026-11-13, gelesen in `ClozeStrategy.cs`)

- **Mangel 2 (Auswahlliste hinter der Lücke)** ist inzwischen eine Regel: `ClozeStrategy.cs:79-82` verwirft eine runde
  Klammer hinter einer Lücke, die `,` `/` `;` oder „ or " enthält – „{{4}} (Pokémon, friend, match, game)" fiele heute.
- **Mangel 3 (Lösung in einer späteren Klammer)**: dieselbe Regel fängt „(excited, surprised, worried, scared)" als
  Auswahl; eine Lösung in einer *einzelnen* Grundform-Klammer an anderer Stelle bliebe unbemerkt, weil `ReadableText`
  die Grundform-Hinweise vor der Verrats-Prüfung (`:84-90`) herausnimmt.
- **Mangel 1 (Titel ≠ Schwerpunkt)** und **Mangel 4 (mehrdeutige Zeitform)**: keine Regel; beides ist Sprachurteil.
- **Befund 2026-11-11 (Verrats-Regel zu streng)**: `:87-90` verwirft jede Lösung, die als eigenes Wort sonst im Text
  steht – bei Hilfsverben des past progressive (`was`, `were`) fast jeder Text. Das ist die eigentliche Folge für den
  Sohn: der Plan bekam keine Grammatik-Position.

## Offene Punkte (für das Grillen, `art: Defekt` – darf ich allein grillen)

1. Verrats-Regel: nur Lösungen **mit mehr als einem Wort** oder nur **Inhaltswörter** prüfen (Hilfsverben/Artikel
   ausnehmen) – oder ein mehrteiliges `was running` nur als Ganzes verraten lassen?
2. Mangel 4: Alternativen (`felt` neben `was feeling`) vom Modell verlangen und als `alternatives` speichern, oder bleibt
   das die Testmodus-Prüfung des Vaters?
3. Mangel 1: nicht als Regel (Sprachurteil) – Titel aus dem Grammatik-Thema der Unit vorgeben (C# entscheidet, B-460)?

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — neuer Befund beim `plan`-Lauf auf der echten Datenbank (`qwen2.5:14b-instruct`): beide Lückentexte
  zu past/present progressive scheiterten nach der Reparatur-Runde an der Regel „Lösung steht außerhalb der
  Klammern im Text“ – für `were`, `was`, `went`, `had`. Diese Formen kommen in einem Text über das past progressive
  zwangsläufig mehrfach vor; die Regel verwirft damit fast jeden Text zu diesem Grammatikthema. Ein Entwurf hatte
  zusätzlich keine Ablenker. Folge: Der Plan bekam keine Grammatik-Position.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Mängel 2/3 sind inzwischen großteils Regel, 1/4 Sprachurteil; der wirksame Defekt ist die zu strenge Verrats-Regel (Befund 2026-11-11). Drei offene Punkte.
