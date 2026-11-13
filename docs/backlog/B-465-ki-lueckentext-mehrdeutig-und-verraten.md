---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator, rolle/student]
aliases: [KI-Cloze Qualität, Übung 28]
status: geschaetzt
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
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

## User Story

Als **Sohn** möchte ich Grammatik-Lückentexte zum past progressive bekommen, deren Lücken eine sprachlich begründete
Lösung haben, damit ich die Zeitform übe statt zu raten. Als **Vater** möchte ich, dass der KI-Creator solche Texte
überhaupt durchbringt, damit der Plan eine Grammatik-Position bekommt.

## Ist-Stand am Code (2026-11-13, gegen den aktuellen Stand nachgeprüft)

Alle Stellen in `backend/Pugling.Agent.Creator/Drafting/`. Die Zeilenangaben der ersten Ausformulierung (`:79-82`,
`:84-90`) waren durch den Schultest-Umbau (B-524) verrutscht und sind hier berichtigt.

- **Mangel 2 (Auswahlliste hinter der Lücke)** ist inzwischen eine Regel: `ClozeStrategy.cs:105-110` verwirft eine runde
  Klammer hinter einer Lücke, die `,` `/` `;` oder „ or " enthält – „{{4}} (Pokémon, friend, match, game)" fiele heute.
- **Mangel 3 (Lösung in einer späteren Klammer)**: dieselbe Regel fängt „(excited, surprised, worried, scared)" als
  Auswahl; eine Lösung in einer *einzelnen* Grundform-Klammer an anderer Stelle bliebe unbemerkt, weil
  `ClozeMarkup.ReadableText` (`ClozeMarkup.cs:111-112`) die Grundform-Hinweise vor der Verrats-Prüfung herausnimmt.
- **Verrats-Regel**: `ClozeStrategy.cs:112-118` – jede Lösung, die als eigenes Wort (mehrteilig als Wortfolge,
  `ClozeMarkup.ContainsWord`, `ClozeMarkup.cs:85-91`) im lesbaren Text steht, ist ein Verstoß. Sie unterscheidet nicht,
  ob die Lücke eine Grundform-Klammer trägt.
- **Mangel 1 (Titel ≠ Schwerpunkt)**: `DraftRules.Title` (`DraftRules.cs:46-52`) prüft nur leer, Länge und Dublette.
  **Mangel 4 (mehrdeutige Zeitform)**: das Markup kennt Alternativen (`[didn't|did not]`, `ClozeMarkup.cs:8`), verlangt
  sie aber nirgends. Beides ist Sprachurteil.
- Bestehende Tests: `backend/Pugling.Api.Tests/CreatorAgentTests.cs:1120` (verratene Lösung + Auswahlliste + Ablenker;
  Lücke 3 trägt dort eine *Listen*-Klammer) und `:3788` (B-501, geschütztes Leerzeichen).

## Die echte Lücke

Die Verrats-Regel ist für **Grammatik-Lücken** falsch geschnitten. Bei „{{3}} (be)" mit Lösung `was` ist die Aufgabe die
Form, nicht das Wort; dass `was` in einem Text über das past progressive noch fünfmal steht, verrät nichts, ist nach
`:116` aber ein Verstoß. Folge (2026-11-11): kein Text zum Thema übersteht die Reparatur-Runde, der Plan bekommt keine
Grammatik-Position. Für **Vokabel-Lücken** (ohne Grundform-Klammer) ist die Regel richtig und bleibt.

## Offene Punkte

1. ~~Verrats-Regel: nur mehrteilige Lösungen, nur Inhaltswörter, oder `was running` nur als Ganzes?~~ → Entscheidung 1.
2. ~~Mangel 4: Alternativen vom Modell verlangen oder Testmodus des Vaters?~~ → Entscheidung 2 (zurückgestellt).
3. ~~Mangel 1: Titel aus dem Grammatik-Thema der Unit vorgeben?~~ → Entscheidung 3 (zurückgestellt).

## Entscheidungen

1. **Die Verrats-Prüfung gilt nur für Lücken ohne gültige Grundform-Klammer.** Trägt eine Lücke genau eine Grundform
   (keine Auswahlliste nach `:107-110`), ist sie eine Formaufgabe und wird von `:112-118` übersprungen. Ausnahme: im
   Schultest-Modus (`request.SchoolTest`, B-524) bleibt die Prüfung für alle Lücken, weil die Klammer dort das Listenwort
   nennt und die Lücke eine Vokabel abfragt. Begründung: trennt nach dem, was die Lücke abfragt, statt über Wortlisten
   („Hilfsverben ausnehmen") zu raten, die bei `went`/`had` schon wieder versagen; eine Listen-Klammer bleibt ungültig und
   damit geprüft (der Test `:1120` bleibt grün). Kosten: ein Grammatiktext darf dieselbe Form anderswo zeigen, das Kind
   könnte abschreiben – bei einer Formaufgabe mit gegebener Grundform hingenommen.
2. **Mangel 4 bleibt Prüfung des Vaters im Testmodus – zurückgestellt.** Begründung: „felt neben was feeling" ist
   Sprachurteil; keine Regel kann es prüfen, und ein Prompt-Satz ließe sich nicht als Regressionstest rot machen.
   Kosten: das Kind kann an einer mehrdeutigen Lücke weiter Punkte verlieren, bis der Vater sie im Testmodus sieht und
   eine Alternative (`[was feeling|felt]`) nachträgt.
3. **Mangel 1 keine Regel – zurückgestellt.** Begründung: Sprachurteil; einen Titel aus dem Grammatik-Thema vorzugeben ist
   ein Umbau der Pipeline (B-460-Linie), nicht diese Defekt-Reparatur. Kosten: falsche Titel bleiben bis zur Sichtung
   durch den Vater möglich.

Alle drei: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. Ein Entwurf „Yesterday I {{1}} (be) at home. She was reading …" mit Lösung `was` und genug Ablenkern wird
   **angenommen**. *Rot macht ihn:* die heutige Regel `:112-118` ohne Ausnahme (Regressionstest des Defekts).
2. Eine Vokabel-Lücke ohne Klammer, deren Lösung (`horse`) sonst im Text steht, wird weiter **abgelehnt** („die Lösung
   'horse' steht außerhalb der Klammern"). *Rot macht ihn:* eine Ausnahme, die die Prüfung ganz abschaltet.
3. Eine Lücke mit **Listen**-Klammer bleibt doppelt verletzt (Auswahl **und** Verrat), wie in `CreatorAgentTests.cs:1120`.
   *Rot macht ihn:* eine Ausnahme, die jede Klammer statt nur einer gültigen Grundform zählt.
4. Im Schultest-Modus wird eine Lücke mit Grundform-Klammer, deren Lösung sonst im Text steht, weiter abgelehnt.
   *Rot macht ihn:* eine Ausnahme ohne Prüfung von `request.SchoolTest`.
5. Live-Nachweis (kein Tor): ein `plan`-Lauf gegen eine **Kopie** der echten DB bringt für eine Unit mit past progressive
   eine Grammatik-Position durch; Ergebnis im `## Verlauf`. Zugleich Beleg für B-460 AK 6.

## Schätzung

**Größe: S** — eine Bedingung in der Verrats-Schleife von `ClozeStrategy.Validate` plus vier Testfälle.

- **Risiken:** Die Regel wird *gelockert* – deshalb die Gegenfälle AK 2–4 mitschreiben. Die Listen-Bedingung aus `:108`
  darf nicht ein zweites Mal formuliert werden, sonst driften „gültige Grundform" und „Auswahl" auseinander; als kleiner
  geteilter Helfer.
- **Angriffsplan (Backend/Agent):** (1) AK 1–4 in `CreatorAgentTests.cs` rot; (2) in `Validate` die Lücken mit gültiger
  Grundform aus `ClozeMarkup.FormHints` bilden und in der Schleife `:115-118` überspringen, außer `request.SchoolTest`;
  (3) englischer Kommentar mit dem Warum; (4) Live-Lauf AK 5 über den Skill `ki-creator` gegen eine DB-Kopie.
- **Testweg:** `backend/Pugling.Api.Tests/CreatorAgentTests.cs` (Pipeline mit gestubbtem Chat, Muster `:1120`), dann
  `dotnet test Pugling.sln -c Release`; AK 5 per `ki-creator plan` gegen
  `scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>` – nie gegen die laufende echte DB.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — neuer Befund beim `plan`-Lauf auf der echten Datenbank (`qwen2.5:14b-instruct`): beide Lückentexte
  zu past/present progressive scheiterten nach der Reparatur-Runde an der Regel „Lösung steht außerhalb der
  Klammern im Text“ – für `were`, `was`, `went`, `had`. Diese Formen kommen in einem Text über das past progressive
  zwangsläufig mehrfach vor; die Regel verwirft damit fast jeden Text zu diesem Grammatikthema. Ein Entwurf hatte
  zusätzlich keine Ablenker. Folge: Der Plan bekam keine Grammatik-Position.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Mängel 2/3 sind inzwischen großteils Regel, 1/4 Sprachurteil; der wirksame Defekt ist die zu strenge Verrats-Regel (Befund 2026-11-11). Drei offene Punkte.
- **2026-11-13** — Ist-Stand gegen den aktuellen Code nachgeprüft (Zeilen nach B-524 berichtigt), User Story und
  „Die echte Lücke" ergänzt; `unverifiziert` entfernt (Backlog-Lauf).
- **2026-11-13** — gegrillt: Verrats-Prüfung nur für Lücken ohne gültige Grundform-Klammer (außer Schultest); Mangel 1
  und 4 bleiben Testmodus des Vaters. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
  [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: S, backend (Agent), keine Migration, kein Vertragsbruch.
