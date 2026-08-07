---
tags: [typ/story, status/abgenommen, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Rollennamen, Vater ist keine Ebene]
status: abgenommen
prio: P2
art: Aufräumen
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: Sitzung 2026-07-31 (Rollen-Abgleich Creator/Supervisor/Student)
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-44 · Grundprinzip auf Supervisor/Student umschreiben — „Vater" ist keine Ebene

[docs/grundprinzip.md](../grundprinzip.md) nennt die Ebenen „Creator / **Vater** / **Kind**", der Code
nennt sie durchgehend `Creator`/`Supervisor`/`Student`. Damit stehen im Startdokument der Architektur drei
Namensachsen unsortiert nebeneinander: die **Rolle** (Creator/Supervisor/Student — JWT-Claim, Routenpräfix,
Ordner), die **Entität** (`Adult`/`Child` — `fid`/`cid`, `children/{childId}`) und die **Familiensprache**
(Vater/Sohn — Oberfläche, Seed). Wer das Dokument liest, hält „Vater" und „Supervisor" für Synonyme; sie
sind es nicht: ein Vater ist *ein* Account mit *zwei* Rollen (Creator **und** Supervisor) auf *einer*
`Adult`-Zeile, ein Lehrer-Konto trägt nur Creator.

## User Story

Als Leser von `docs/grundprinzip.md` (Entwickler, Modell, neuer Mitarbeiter) möchte ich, dass die drei
Ebenen durchgehend mit ihren Rollen-Namen (Creator/Supervisor/Student) benannt sind, damit ich „Vater"
nicht für den Namen einer Architekturschicht halte, sondern für das, was er ist — eine mögliche
Rollenkombination auf einem `Adult`-Konto.

## Ist-Stand am Code

**Der Code trennt die drei Achsen bereits sauber; nur die Doku hinkt hinterher.**

- Rollen sind Claims/Routen/Ordner, entkoppelt vom Login: `ApiRoutes.Creator/Supervisor/Student`,
  `Controllers/{Tier}`, `Services/{Creator,Supervisor,Student,Shared}` (root
  [CLAUDE.md](../../CLAUDE.md), Abschnitt „Architektur").
- „Nur Vater"/„als Vater" (Berechtigung) übersetzt im Code-Doku-Glossar selbst auf **„supervisor only"**,
  nicht auf „father" — der Gate liegt auf `Roles.Supervisor`, nicht auf der Verwandtschaft
  ([docs/translate.md:72](../translate.md)). „Vater" (wirklich der Vater) bleibt dagegen bewusst **father**
  (`SupervisorRelation.Father`, [docs/translate.md:69](../translate.md)).
- `CreatorProfilesController.List` (`GET creator/profiles/match`) nimmt `[FromQuery] int childId` entgegen
  und prüft `access.SupervisorOwnsChildAsync(User, childId, ct)`
  (`backend/Pugling.Api/Controllers/Creator/CreatorProfilesController.cs:73,76`). Der Satz „Der Creator
  weiß nichts von einzelnen Kindern" (`docs/grundprinzip.md:35f.`) stimmt also für die **Entität**
  (`Exercise` trägt keine `ChildId`), nicht mehr uneingeschränkt für den **Creator-Arbeitsplatz** — dieser
  eine Endpunkt liest kindbezogen und ist ownership-geschützt. `docs/endpunkt-beziehungen.md:107-110`
  dokumentiert das bereits korrekt („liegt auf der Creator-Route … prüft zusätzlich die Betreuung").
- **Root [CLAUDE.md](../../CLAUDE.md)** benutzt „Vater"/„Sohn" nur einleitend-narrativ ( „**Vater** steuert
  … **Sohn** lernt mit Spaß.") und stellt im selben Satz klar: „Drei Ebenen (Creator/Supervisor/Student)".
  Kein Header, keine Tabelle nennt dort eine Ebene „Vater" — **kein Fund**, keine Änderung nötig.
- **[docs/endpunkt-beziehungen.md](../endpunkt-beziehungen.md)** nutzt „Vater"/„Sohn" nur in konkreten
  Durchstich-Beispielen (`#### 1) Vater legt einen aktiven Lehrplan für Sohn 1 an`, Zeile 134) — das sind
  benannte Instanzen einer Familie in einem Walkthrough, keine Ebenen-Definition. Die Kapitelüberschriften
  selbst heißen „Lehrplan ↔ Kind" (Entität), nicht „Lehrplan ↔ Vater". **Kein Fund im selben Sinn wie
  `grundprinzip.md`.**
- **[docs/rollen-doku.md](../rollen-doku.md)** ist bereits das Positiv-Beispiel: durchgehend
  Creator/Supervisor/Student, „Vater" fällt genau einmal als Aside („Heute trägt der Vater technisch meist
  auch diese Rolle, fachlich bleibt sie aber getrennt vom Supervisor.", Zeile 19) — exakt das Muster, das
  `grundprinzip.md` fehlt.

**In `docs/grundprinzip.md` selbst (34 Treffer „Vater"/„Kind", 0 Treffer „Sohn") liegt die Vermischung an
vier Stellen konzentriert, der Rest der Treffer folgt ihnen nur:**

1. Frontmatter-Alias `Creator-Vater-Kind` (Zeile 3) — kodiert die falsche Achse direkt im Obsidian-Alias.
2. Die Überblickstabelle (Zeilen 19–23): Spalte „Ebene" trägt `1. Creator` / `2. Vater` / `3. Kind` — Zeile
   1 benennt die **Rolle**, Zeile 2+3 die **Familie**. Die Spalte „Rolle" ist dadurch selbst inkonsistent:
   sie trägt bei Creator eine Umschreibung („Ersteller von Inhalten"), bei Vater den echten Rollennamen
   („Supervisor"), bei Kind wieder eine Umschreibung („Schüler") statt „Student".
3. Die H2-Überschriften Zeile 55 (`## Ebene 2 · Vater — die Steuerung`) und Zeile 82
   (`## Ebene 3 · Kind — das Lernen`) — im Kontrast zu Zeile 31 (`## Ebene 1 · Creator — der Inhalt`), die
   korrekt den Rollennamen trägt.
4. Der Fließtext unter beiden Abschnitten sowie „Der Loop an einem konkreten Beispiel" (Zeilen 94–107) und
   „Warum diese Trennung wichtig ist" (Zeilen 126–136) übernehmen die Ebenen-Namen aus den Überschriften und
   sprechen durchgehend vom „Vater"/„Kind" als Akteur der Ebene — dort, wo Ebene 1 im selben Duktus
   „Creator" sagen würde.

Der Abschnitt „Technische Umsetzung" (Zeilen 111–124) **im selben Dokument** ist bereits korrekt
(„Ein Student, mehrere Supervisor", `ApiRoutes.Creator/Supervisor/Student`) — der Bruch verläuft also
mitten durch die Datei, zwischen dem erzählenden und dem technischen Teil.

## Die echte Lücke

Nicht „die Doku widerspricht dem Code" in der Breite, sondern **eine einzige Datei**, deren *strukturelle*
Namen (Frontmatter-Alias, Tabellen-Spalte „Ebene", zwei von drei H2-Titeln) die Familiensprache statt der
Rollennamen tragen — während ihr eigener „Technische Umsetzung"-Abschnitt, `docs/rollen-doku.md` und
`docs/endpunkt-beziehungen.md` es bereits richtig machen. Die Lücke ist also **schmaler** als die Idee
vermutete: kein Bereich, sondern ein Dokument, und darin vier konkrete Fundstellen statt eines diffusen
Nebeneinanders.

## Offene Punkte

~~Muss root `CLAUDE.md` mit angefasst werden?~~ → siehe Entscheidung 1 (nein, bereits konform).

~~Muss `docs/endpunkt-beziehungen.md` mit angefasst werden?~~ → siehe Entscheidung 2 (nein, bereits
konform — nutzt Vater/Sohn nur als benannte Instanzen in einem Walkthrough).

~~Was passiert mit dem Satz „Der Creator weiß nichts von einzelnen Kindern" angesichts
`creator/profiles/match?childId=`?~~ → siehe Entscheidung 3.

~~Wie wird der Frontmatter-Alias `Creator-Vater-Kind` behandelt?~~ → siehe Entscheidung 4.

~~Bleibt „Vater"/„Kind" irgendwo im Fließtext von `grundprinzip.md` stehen?~~ → siehe Entscheidung 5.

~~Braucht es einen automatisierten Test, der die Rollennamen in zentralen Docs erzwingt?~~ → siehe
Entscheidung 6 (nein, mit Begründung).

## Entscheidungen

1. **Root `CLAUDE.md` bleibt unverändert.** Begründung: Es benennt keine Ebene „Vater" — der Satz „Vater
   steuert … Sohn lernt" ist Produkt-Pitch, direkt gefolgt von „Drei Ebenen (Creator/Supervisor/Student)".
   Das deckt sich mit der eigenen Konvention des Projekts („Vater bleibt richtig, wo ein Vater gemeint
   ist … in der Oberfläche"). Kosten: keine — kein Fund, keine Änderung.
2. **`docs/endpunkt-beziehungen.md` bleibt unverändert.** Begründung: Es nutzt „Vater"/„Sohn" nur in
   konkreten Durchstich-Beispielen mit durchnummerierten Instanzen („Vater legt … für Sohn 1 an"), nicht
   als Namen einer Architekturschicht; seine Kapiteltitel heißen bereits „Lehrplan ↔ Kind". Kosten: keine.
3. **Der Satz „Der Creator weiß nichts von einzelnen Kindern" bekommt eine Fußnote, wird aber fachlich
   nicht neu verhandelt.** Begründung: Die Entität `Exercise` trägt weiterhin keine `ChildId` — der Satz
   stimmt für den Katalog. Er stimmt nicht mehr uneingeschränkt für den Creator-*Arbeitsplatz*
   (`creator/profiles/match?childId=`, `CreatorProfilesController.cs:73,76`). Die fachliche Neuverhandlung
   dieses Grenzfalls ist explizit [B-46](B-46-interessenbasierte-uebungen.md) zugewiesen — B-44 löst nur
   die Terminologie, nicht das Modell. Kosten: eine Fußnote/ein Halbsatz mit Verweis auf B-46; kein
   inhaltlicher Umbau des Abschnitts.
4. **Der Alias `Creator-Vater-Kind` wird durch `Creator-Supervisor-Student` ersetzt, nicht ergänzt.**
   Begründung: Der zusammengesetzte Alias kodiert exakt die falsche Achsen-Vermischung, die die Story
   auflösen soll — ihn stehen zu lassen würde Obsidian weiter auf den alten Namen suchen lassen. Die
   Auffindbarkeit über das Einzelwort „Vater" geht nicht verloren: der Begriff bleibt im Fließtext von
   `grundprinzip.md` selbst (Entscheidung 5), in `docs/rollen-doku.md` und in `docs/endpunkt-beziehungen.md`
   stehen, Obsidian-Volltextsuche findet ihn dort weiter. Kosten: keine — reine Alias-Zeile.
5. **Im Fließtext bleibt „Vater"/„Kind" an genau zwei Stellen bewusst stehen, überall sonst wird auf
   Supervisor/Student umgestellt.** Begründung: (a) der Satz „Der Vater ist der **Supervisor**." (Zeile 57)
   bleibt als *einmalige, ausdrückliche Gleichsetzung* zu Beginn von Ebene 2 erhalten — nach diesem Muster
   von `docs/rollen-doku.md:19` orientiert sich der Leser weiter über die Familiensprache, ohne dass sie
   zur Ebenen-Bezeichnung wird; (b) der Abschnitt „Der Loop an einem konkreten Beispiel" (Zeilen 94–107)
   bleibt bei „Vater"/„Kind", weil er — wie der Durchstich in `endpunkt-beziehungen.md` — eine **konkrete
   Instanz** durchspielt, keine Ebenen-Definition. Überall sonst (Tabelle, zwei H2-Titel, „Warum diese
   Trennung wichtig ist") wird auf Supervisor/Student umgestellt. Kosten: die Umsetzung muss diese
   Unterscheidung (Definition vs. konkretes Beispiel) beim Schreiben treffen, statt stumpf zu ersetzen —
   etwas mehr Sorgfalt beim Bauen, aber kein zusätzlicher Umfang.
6. **Kein automatisierter Test.** Begründung: Es gibt keinen mechanischen Drift-Vektor wie bei Code
   (Copy-Paste einer Signatur) — die Story behebt eine einmalige Fehlbenennung in genau einer Datei, nicht
   ein wiederkehrendes Muster über viele Dateien (die Gegenprobe hier hat gezeigt: `CLAUDE.md`,
   `endpunkt-beziehungen.md` und `rollen-doku.md` sind bereits sauber). Ein reflexiver Test, der z. B.
   `## Ebene \d · (Vater|Kind)` in `grundprinzip.md` verbietet, wäre auf diese eine Datei zugeschnitten und
   bräche bei jeder legitimen zukünftigen Nennung von „Vater" im erlaubten Sinn (Entscheidung 5) — ein
   Wächter, der sein eigenes Feld nicht sauber von seiner Ausnahme trennen kann, ist keiner. Die
   CI-Markdown-Lint-Stufe („Markdown-Lint") bleibt die einzige mechanische Prüfung. Kosten: keine, aber
   auch keine Regressionssicherung — vertretbar, weil reine Prosa und einmalig.

## Akzeptanzkriterien

1. `docs/grundprinzip.md`, Frontmatter: Alias `Creator-Vater-Kind` ist ersetzt durch
   `Creator-Supervisor-Student`.
2. Die Überblickstabelle benennt die Ebenen 1–3 durchgehend `Creator`/`Supervisor`/`Student`; die Spalte
   „Rolle" trägt für alle drei denselben Stil (entweder überall der Rollenname oder überall die
   Umschreibung — nicht gemischt wie heute).
3. Die H2-Überschriften heißen `## Ebene 2 · Supervisor — die Steuerung` und
   `## Ebene 3 · Student — das Lernen`.
4. Im Fließtext von Ebene 2, Ebene 3 und „Warum diese Trennung wichtig ist" ist die Ebene als Akteur
   `Supervisor`/`Student` benannt; „Vater"/„Kind" taucht dort höchstens als eingeführte Gleichsetzung
   („Der Vater ist der Supervisor.") oder als Verwandtschaftsangabe auf, nie mehr als alleinstehender
   Ebenen-Name.
5. „Der Loop an einem konkreten Beispiel" bleibt bei „Vater"/„Kind" (konkrete Instanz, siehe Entscheidung
   5) — unverändert.
6. Der Satz zum Creator und einzelnen Kindern trägt einen Verweis/eine Fußnote auf die
   `creator/profiles/match?childId=`-Ausnahme und auf B-46, ohne den fachlichen Anspruch des Satzes für die
   Katalog-Entität selbst zu verändern.
7. `docs/grundprinzip.md` bleibt `markdownlint-cli2`-grün (Leerzeilen um Überschriften/Tabellen,
   Tabellen-Pipes).
8. `docs/grundprinzip.md`, `docs/endpunkt-beziehungen.md` (Root-`CLAUDE.md` ohnehin unverändert) sind die
   einzigen Dateien, die diese Story anfasst — `docs/endpunkt-beziehungen.md` nur, falls die Fußnote aus
   Kriterium 6 dort eine spiegelnde Ein-Satz-Ergänzung braucht, sonst auch das unverändert.

## Schätzung

**Größe: XS** — Anker: reine Wort-/Titel-Ersetzung in einer einzigen Markdown-Datei (Frontmatter-Alias,
eine Tabellenzeile, zwei H2-Titel, Fließtext dreier Abschnitte), kein Code, keine Migration, kein
Vertragsbruch, kein neuer Test. Vergleichbar mit dem XS-Anker aus dem README (zwei Sätze plus Prüfung),
nur ohne die Prüfung, weil hier keine mechanische möglich/sinnvoll ist (Entscheidung 6).

- **`wo`: doku** — reine Textarbeit an `docs/grundprinzip.md`.
- **`migration`: nein** — kein Schema betroffen.
- **`vertragsbruch`: nein** — kein Contract/Client/Frontend betroffen.
- **Risiken**: gering. Größtes Risiko ist eine zu grobe Suchen-Ersetzen-Aktion, die auch die bewusst
  stehenbleibenden Stellen (Entscheidung 5) mit umschreibt und damit den Loop-Abschnitt unnötig verändert —
  dagegen hilft, die vier Fundstellen aus „Ist-Stand am Code" einzeln abzuarbeiten statt global zu
  ersetzen.
- **Angriffsplan**: (1) Frontmatter-Alias ändern. (2) Überblickstabelle: Ebene-Spalte + Rolle-Spalte
  vereinheitlichen. (3) Zwei H2-Titel umbenennen. (4) Fließtext der beiden Ebenen-Abschnitte und „Warum
  diese Trennung wichtig ist" durchgehen, Supervisor/Student statt Vater/Kind als Akteur, mit den zwei
  bewussten Ausnahmen aus Entscheidung 5. (5) Fußnote zum Creator/`childId`-Satz ergänzen, Verweis auf
  B-46. (6) `markdownlint-cli2` laufen lassen (CI-Job „Markdown-Lint" spiegelt das).
- **Testweg**: keiner automatisiert nötig (Entscheidung 6) — reine Doku, keine Assertions möglich, die
  nicht selbst wieder Prosa vorschreiben würden. Verifikation ist die Lektüre gegen die
  Akzeptanzkriterien plus grüner `markdownlint-cli2`-Lauf.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Rollen-Abgleich in der Sitzung, Nutzer bestätigt).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code und gegen `CLAUDE.md`, `rollen-doku.md`,
  `endpunkt-beziehungen.md` recherchiert — die Lücke ist schmaler als vermutet (nur `grundprinzip.md`, vier
  konkrete Fundstellen statt eines diffusen Nebeneinanders); autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — gegrillt: sechs offene Punkte in nummerierte Entscheidungen mit Begründung und Kosten
  überführt (Scope auf eine Datei begrenzt, zwei bewusste Ausnahmen für Vater/Kind im Fließtext, kein
  automatisierter Test); autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe XS, `wo: doku`, `migration: nein`, `vertragsbruch: nein`, Angriffsplan
  und Testweg (kein automatisierter Test, Begründung siehe Entscheidung 6) festgelegt; autonom getroffen,
  Nutzerauftrag 2026-08-04.
- **2026-08-05** — gebaut (Nachtlauf 2, Sprint 1): Frontmatter-Alias auf `Creator-Supervisor-Student`
  geändert; Überblickstabelle auf Creator/Supervisor/Student vereinheitlicht; beide H2-Titel auf
  „Supervisor"/„Student" umbenannt; Fließtext von Ebene 2/3 und „Warum diese Trennung wichtig ist" auf
  Supervisor/Student als Akteur umgestellt, mit den zwei bewussten Ausnahmen aus Entscheidung 5 (der Satz
  „Der Vater ist der Supervisor." zu Beginn von Ebene 2, und „Der Loop an einem konkreten Beispiel"
  unverändert); Fußnote zum Creator/`childId`-Satz mit Verweis auf B-46 ergänzt. `docs/grundprinzip.md`
  ist die einzige geänderte Datei (AK8 — keine Fußnote in `endpunkt-beziehungen.md` nötig, da AK6 keine
  Ergänzung dort über die neue Fußnote in `grundprinzip.md` hinaus verlangte). `markdownlint-cli2` →
  **0 Issues**.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `docs/grundprinzip.md` weiterhin `Supervisor`/
  `Student` statt `Vater`/`Kind` als Ebenennamen führt — hält (`grundprinzip.md:31,60,87`). Kein Fund.
