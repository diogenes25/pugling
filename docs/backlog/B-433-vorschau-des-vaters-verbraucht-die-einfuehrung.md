---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/supervisor, rolle/student]
aliases: [Vater-Übung verbraucht Einführung, Supervisor-Runde führt ein]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-nacht.md
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-433 · Spielt der Vater eine Übungsrunde des Kindes, sind dessen neue Wörter danach „schon gesehen"

Seit B-419 ist die erste Begegnung eines Wortes eine Kennenlernkarte – gemerkt am Positions-Fortschritt
(`PositionItemProgress.IntroducedAt`). Der Vater darf die Übungsendpunkte des Kindes mitspielen (Vorschau/Nachtrag,
`PositionPracticeController` ohne Rollenzweig im `Review`). Spielt er eine Runde durch, setzt sein Review das
`IntroducedAt` – das Kind bekommt die Karte danach sofort getippt, ohne sie je gesehen zu haben.

Vom `pugling-reviewer` in Lauf AE als 🟢 gemeldet, bewusst nicht im Sprint: das Sprint-Ziel betraf den Weg des Kindes.

**Zweite Folge (Nachschau Sprint 22):** der Server entscheidet „Einführung" beim Review aus der Datenbank, nicht aus der
ausgelieferten Karte. Wird ein Wort zwischen `/cards` und dem Review woanders eingeführt (zweiter Tab, Vater-Runde),
wertet der Server das „Weiter" der Kennenlernkarte (ohne Antwort) als getippte, **falsche** Antwort und setzt die Karte
einen Kasten zurück – das Kind sieht davon nichts.

## User Story

Als **Vater** möchte ich eine Übungsrunde meines Sohnes ansehen oder nachtragen können, ohne ihm das Kennenlernen seiner
neuen Wörter wegzunehmen, **damit** er jedes neue Wort zuerst gezeigt bekommt, bevor es abgefragt wird – und als **Sohn**
möchte ich, dass ein „Weiter“ auf einer Kennenlernkarte nie als falsche Antwort zählt, **damit** die Runde fair bleibt.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- **Erste Folge:** `backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388-394` setzt beim ersten
  Kontakt `IntroducedAt = session.Day` und `DueOn` – ohne Rollenprüfung. Der Controller trennt die Rollen sonst inline:
  `:66` (fremder Tag nur für den Supervisor), `:339` (Plan-Sperre nur für den Studenten).
- **Vorbild am Code:** der Test tut genau das schon richtig. `PositionTestsController.cs:220-222` führt unter Tagesdosis
  nur ein, wenn `User.IsStudent()` („The supervisor's preview must not move the child's learning state“), und `:334`
  bricht `IntroduceAnsweredAsync` für einen Supervisor-Versuch ab. Das Üben ist die einzige Stelle ohne diese Wand.
- Die Einführung liest der Server aus der DB: `PositionPlayService.IntroducedCardsAsync`
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:107-110`, nur Zeilen mit `IntroducedAt`), daraus
  `IsIntroduction`/`CardStage` (`:98-100`, `:121-128`). Eine Karte mit gesetztem `IntroducedAt` ist für das Kind
  keine Kennenlernkarte mehr.
- **Zweite Folge:** `Review` bestimmt die Stufe der Karte beim Bewerten neu (`PositionPracticeController.cs:362-367`).
  Ist das Atom inzwischen eingeführt, ist die Stufe getippt; die antwortlose Nutzlast der Kennenlernkarte (Frontend
  `judge(card, {})`, `frontend/src/sohn/SohnPractice.tsx:459`) wird dann über `grader.Matches(null, item)` gewertet
  (`:382-384`), und eine leere Antwort ist nie richtig (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:29`).
  `ApplyReview` setzt den Kasten auf 1 und zählt `ReviewCount` hoch (`PositionPlayService.cs:450-465`); der Item-Verlauf
  bucht „falsch“ (`PositionPracticeController.cs:447-451`).
- **Nachtrag ist gewollt:** ein Supervisor darf einen anderen Tag spielen (`:66`), und seine Bewertungen buchen Kasten
  und Punkte für das Kind (`:453-484`). Das bleibt hier unangetastet.

## Die echte Lücke

Das Üben fehlt in der Rollenwand, die der Test schon hat: **nur die eigene Begegnung des Kindes führt ein.** Und der
Server unterscheidet „keine Antwort, weil nichts gefragt war“ nicht von „leere Antwort auf eine Frage“.

## Offene Punkte

1. ~~Soll eine Vater-Runde den Lernstand des Kindes überhaupt schreiben, oder ist der Nachtrag genau dafür da?~~ →
   Entscheidung 1 (Einführung nein) und 3 (der Rest zurückgestellt).
2. ~~Was gilt für die antwortlose Bewertung einer Karte, die zwischen `/cards` und `review` eingeführt wurde?~~ →
   Entscheidung 2.
3. ~~`art`: Frage oder Defekt?~~ → Entscheidung 4.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Eine Übungsrunde des Supervisors führt kein Wort ein.** `IntroducedAt`/`DueOn` beim ersten Kontakt
   (`PositionPracticeController.cs:390`) nur, wenn `User.IsStudent()` – wie im Test (`PositionTestsController.cs:221`,
   `:334`). *Begründung:* das Kennenlernen ist eine Begegnung des Kindes; was der Vater sieht, hat der Sohn nicht
   gesehen. Eine Regel für Üben und Test. *Kosten:* bewertet der Vater ein nie eingeführtes Atom eines Typs ohne
   Kennenlern-Stufe, bekommt es Kasten und Fälligkeit, bleibt aber „nicht eingeführt“, bis das Kind es selbst trifft –
   hingenommen, der Weg ist selten.
2. **Eine Bewertung ganz ohne Antwort (`GivenAnswer` leer und `WasKnown` null) auf ein Atom, das heute eingeführt und
   noch nie geprüft wurde (`IntroducedAt == session.Day`, `ReviewCount == 0`), ist kein Urteil** – sie wird behandelt
   wie die Kennenlernkarte (keine Punkte, kein Kasten, Verlauf ohne „falsch“), der Cursor rückt weiter. *Begründung:*
   die Karte, die das Kind sah, verlangte keine Antwort; ein Urteil darüber wäre unfair und für das Kind unsichtbar.
   Die Bedingung ist eng genug für die Anti-Cheat-Seite: ein heute eingeführtes Wort steht ohnehin in Kasten 1, eine
   Abwertung nähme ihm nichts, und Punkte gab es dafür nie. *Kosten:* eine zusätzliche Bedingung im `Review`; ein
   absichtlich leer abgeschicktes getipptes Wort am Einführungstag wird nicht als falsch gebucht (es bleibt fällig).
3. **Zurückgestellt: ob eine Vater-Runde Kasten und Punkte des Kindes überhaupt schreiben soll.** Der Nachtrag eines
   anderen Tages (`:66`) ist eine bewusste Funktion; ihn zu ändern ist eine eigene Produktfrage und nicht nötig, um
   diese Lücke zu schließen.
4. **`art: Defekt`** statt `Frage`: beide Folgen sind falsches Verhalten jetzt, und Entscheidung 1 folgt einer Regel,
   die der Code an anderer Stelle schon hat. `entgangen_bei` bleibt leer: der Fund war beim Review von B-419 bekannt und
   bewusst zurückgestellt, nicht durchgerutscht. *Kosten:* keine.

## Akzeptanzkriterien

1. Spielt der Supervisor eine Lern-Runde der Position und klickt eine Kennenlernkarte durch, liefert `/cards` dem Kind
   danach dieselbe Karte weiterhin als Kennenlernkarte; `IntroducedAt` des Atoms bleibt `null`. *Rot, wenn* die
   Rollenprüfung an `PositionPracticeController.cs:390` fehlt.
2. Spielt das Kind dieselbe Karte, wird sie eingeführt wie bisher (bestehende Fälle in `ErstbegegnungTests` bleiben
   grün). *Rot, wenn* die Prüfung versehentlich beide Rollen ausschließt.
3. Holt das Kind `/cards` (Karte = Kennenlernkarte), wird das Atom danach anderswo eingeführt (zweite Sitzung des Kindes),
   und das Kind schickt die antwortlose Bewertung: Kasten, `ReviewCount` und `DueOn` sind unverändert, `wasCorrect` ist
   `false` ohne Abwertung, kein Item-Verlaufseintrag „falsch“, der Cursor rückt weiter. *Rot, wenn* Entscheidung 2 fehlt
   (dann `ReviewCount == 1`).
4. Eine antwortlose Bewertung auf ein Atom, das an einem früheren Tag eingeführt wurde oder schon geprüft ist, wird wie
   bisher als falsch gewertet. *Rot, wenn* die Bedingung von Entscheidung 2 auf „jede leere Antwort“ ausgeweitet wird.

## Schätzung

**Größe: S** – zwei Bedingungen in `PositionPracticeController.Review` (Rollenwand an `:390`, antwortlose Bewertung
eines heute eingeführten Atoms vor `:382`), vier Integrationstests. `wo: backend`, `migration: nein` (keine
Schemaänderung), `vertragsbruch: nein` (Antwortform unverändert).

**Risiken:**
- Entscheidung 2 berührt die Wertungslogik des Übens, die mehrere Pfade teilt (Mengen-Modus `:369-380`,
  Selbsteinschätzungs-Leiter `:367`, `:404`). Die neue Bedingung muss vor `wasCorrect` greifen und darf den Mengen-Modus
  nicht ändern (dort kommt immer eine Antwort).
- Die geteilte Wahrheit „ein Atom ist eingeführt“ wird nun an drei Stellen rollenabhängig geschrieben (Üben, Test-Start,
  Test-Antwort); ein späterer vierter Schreiber muss die Wand kennen. Ein Kommentar am Üben verweist auf den Test.

**Angriffsplan:** (1) Integrationstests AK 1–4 in `backend/Pugling.Api.Tests/ErstbegegnungTests.cs`, rot gegen den
Ist-Code (AK 1, 3); (2) Rollenwand an `:390`; (3) Bedingung „kein Urteil“ vor der Wertung; (4) rote Probe je AK am
benannten Ort; (5) `pugling-reviewer`; (6) E2E `erstbegegnung.spec.ts` als Regressionszeuge des Sohns.

**Testweg:** `backend/Pugling.Api.Tests/ErstbegegnungTests.cs` (AK 1–4); E2E `frontend/e2e/erstbegegnung.spec.ts` als
Rollengang des Sohns; `/smoke-test` nicht nötig (kein neuer Endpunkt).

## Verlauf

- **2026-11-08** — angelegt aus dem Review von B-419 (Lauf AE, Sprint 22).
- **2026-11-08** — ergänzt um die zweite Folge aus der Nachschau zu Sprint 22 (Lauf AE).
- **2026-11-13** — `ausformuliert`: am Code belegt; Vorbild für die Rollenwand im Test gefunden
  (`PositionTestsController.cs:220-222`, `:334`).
- **2026-11-13** — `gegrillt`: Entscheidungen 1–4, `art` von `Frage` auf `Defekt`; Stakeholder-Entscheidung im Auftrag
  des Nutzers (Backlog-Lauf 2026-11-13).
- **2026-11-13** — `geschaetzt`: S, backend, ohne Migration und Vertragsbruch.
